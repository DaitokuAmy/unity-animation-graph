using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Animation Graph 要素のクリップボード変換を提供するクラス
    /// </summary>
    internal static class AnimationGraphClipboardUtility {
        private const string ClipboardHeader = "UNITY_ANIMATION_GRAPH_CLIPBOARD:";
        private const int CurrentVersion = 1;

        [Serializable]
        private sealed class ClipboardData {
            [SerializeField] private int _version = CurrentVersion;
            [SerializeField] private ClipboardObjectData[] _nodes = Array.Empty<ClipboardObjectData>();
            [SerializeField] private ClipboardObjectData[] _signals = Array.Empty<ClipboardObjectData>();
            [SerializeField] private SignalConnectionData[] _signalConnections = Array.Empty<SignalConnectionData>();

            public int Version => _version;
            public IReadOnlyList<ClipboardObjectData> Nodes => _nodes;
            public IReadOnlyList<ClipboardObjectData> Signals => _signals;
            public IReadOnlyList<SignalConnectionData> SignalConnections => _signalConnections;

            public ClipboardData(IReadOnlyList<ClipboardObjectData> nodes, IReadOnlyList<ClipboardObjectData> signals, IReadOnlyList<SignalConnectionData> signalConnections) {
                _nodes = ToArray(nodes);
                _signals = ToArray(signals);
                _signalConnections = ToArray(signalConnections);
            }

            private static T[] ToArray<T>(IReadOnlyList<T> source) {
                var result = new T[source.Count];
                for (var i = 0; i < source.Count; i++) {
                    result[i] = source[i];
                }

                return result;
            }
        }

        [Serializable]
        private sealed class ClipboardObjectData {
            [SerializeField] private string _typeName = string.Empty;
            [SerializeField] private string _json = string.Empty;
            [SerializeField] private ObjectReferenceData[] _objectReferences = Array.Empty<ObjectReferenceData>();

            public string TypeName => _typeName;
            public string Json => _json;
            public IReadOnlyList<ObjectReferenceData> ObjectReferences => _objectReferences;

            public ClipboardObjectData(UnityEngine.Object source) {
                _typeName = source.GetType().AssemblyQualifiedName;
                _json = EditorJsonUtility.ToJson(source);
                _objectReferences = CollectObjectReferences(source);
            }
        }

        [Serializable]
        private sealed class ObjectReferenceData {
            [SerializeField] private string _propertyPath = string.Empty;
            [SerializeField] private string _globalObjectId = string.Empty;

            public string PropertyPath => _propertyPath;
            public string GlobalObjectId => _globalObjectId;

            public ObjectReferenceData(string propertyPath, string globalObjectId) {
                _propertyPath = propertyPath;
                _globalObjectId = globalObjectId;
            }
        }

        [Serializable]
        private sealed class SignalConnectionData {
            [SerializeField] private string _nodeId = string.Empty;
            [SerializeField] private string _signalId = string.Empty;
            [SerializeField] private bool _isEnter;

            public string NodeId => _nodeId;
            public string SignalId => _signalId;
            public bool IsEnter => _isEnter;

            public SignalConnectionData(string nodeId, string signalId, bool isEnter) {
                _nodeId = nodeId;
                _signalId = signalId;
                _isEnter = isEnter;
            }
        }

        /// <summary>
        /// 選択要素をシステムクリップボードへ書き込む
        /// </summary>
        /// <param name="nodeModels">コピーする Node Model 一覧</param>
        /// <param name="signalModels">コピーする Signal Model 一覧</param>
        /// <returns>コピーした要素数</returns>
        public static int Copy(IReadOnlyList<NodeEditorModel> nodeModels, IReadOnlyList<SignalEditorModel> signalModels) {
            var nodes = new List<ClipboardObjectData>();
            var copiedNodeIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < nodeModels.Count; i++) {
                var nodeModel = nodeModels[i];
                if (nodeModel?.Node == null || nodeModel.Node is StartNode || !copiedNodeIds.Add(nodeModel.NodeId)) {
                    continue;
                }

                nodes.Add(new ClipboardObjectData(nodeModel.Node));
            }

            var signals = new List<ClipboardObjectData>();
            var copiedSignalIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < signalModels.Count; i++) {
                var signalModel = signalModels[i];
                if (signalModel?.Signal == null || !copiedSignalIds.Add(signalModel.SignalId)) {
                    continue;
                }

                signals.Add(new ClipboardObjectData(signalModel.Signal));
            }

            var connections = CollectSignalConnections(nodeModels, copiedNodeIds, copiedSignalIds);
            var data = new ClipboardData(nodes, signals, connections);
            EditorGUIUtility.systemCopyBuffer = ClipboardHeader + CurrentVersion + "\n" + JsonUtility.ToJson(data);
            return nodes.Count + signals.Count;
        }

        /// <summary>
        /// システムクリップボードの要素を GraphAsset へ貼り付ける
        /// </summary>
        /// <param name="graphAsset">貼り付け先 GraphAsset</param>
        /// <param name="offset">貼り付け位置に加算する offset</param>
        /// <param name="nodes">貼り付けた Node 一覧</param>
        /// <param name="signals">貼り付けた Signal 一覧</param>
        /// <param name="errorMessage">失敗理由</param>
        /// <returns>貼り付け可能な形式の場合は true</returns>
        public static bool TryPaste(AnimationGraphAsset graphAsset, Vector2 offset, out IReadOnlyList<Node> nodes, out IReadOnlyList<Signal> signals, out string errorMessage) {
            nodes = Array.Empty<Node>();
            signals = Array.Empty<Signal>();
            if (!TryReadClipboard(out var data, out errorMessage)) {
                return false;
            }

            var sourceNodes = new List<Node>();
            var sourceSignals = new List<Signal>();
            try {
                DeserializeObjects(data.Nodes, sourceNodes);
                DeserializeObjects(data.Signals, sourceSignals);
                nodes = PasteNodes(graphAsset, sourceNodes, offset);
                signals = PasteSignals(graphAsset, sourceSignals, offset);
                RestoreSignalConnections(data.SignalConnections, sourceNodes, nodes, sourceSignals, signals);
                errorMessage = string.Empty;
                return true;
            }
            catch (Exception exception) {
                errorMessage = $"Could not paste Animation Graph items: {exception.Message}";
                return false;
            }
            finally {
                DestroyObjects(sourceNodes);
                DestroyObjects(sourceSignals);
            }
        }

        private static ObjectReferenceData[] CollectObjectReferences(UnityEngine.Object source) {
            var references = new List<ObjectReferenceData>();
            var serializedObject = new SerializedObject(source);
            var iterator = serializedObject.GetIterator();
            if (!iterator.Next(true)) {
                return references.ToArray();
            }

            do {
                if (iterator.propertyType != SerializedPropertyType.ObjectReference) {
                    continue;
                }

                var referencedObject = iterator.objectReferenceValue;
                if (referencedObject == null || referencedObject is Node || referencedObject is Signal) {
                    continue;
                }

                var globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(referencedObject);
                references.Add(new ObjectReferenceData(iterator.propertyPath, globalObjectId.ToString()));
            } while (iterator.Next(true));

            return references.ToArray();
        }

        private static List<SignalConnectionData> CollectSignalConnections(IReadOnlyList<NodeEditorModel> nodeModels, HashSet<string> copiedNodeIds, HashSet<string> copiedSignalIds) {
            var connections = new List<SignalConnectionData>();
            for (var i = 0; i < nodeModels.Count; i++) {
                var nodeModel = nodeModels[i];
                if (nodeModel == null || !copiedNodeIds.Contains(nodeModel.NodeId)) {
                    continue;
                }

                AddSignalConnections(connections, nodeModel.NodeId, nodeModel.EnterSignals, copiedSignalIds, true);
                AddSignalConnections(connections, nodeModel.NodeId, nodeModel.ExitSignals, copiedSignalIds, false);
            }

            return connections;
        }

        private static void AddSignalConnections(List<SignalConnectionData> connections, string nodeId, IReadOnlyList<Signal> signals, HashSet<string> copiedSignalIds, bool isEnter) {
            for (var i = 0; i < signals.Count; i++) {
                var signal = signals[i];
                if (signal != null && copiedSignalIds.Contains(signal.SignalId)) {
                    connections.Add(new SignalConnectionData(nodeId, signal.SignalId, isEnter));
                }
            }
        }

        private static bool TryReadClipboard(out ClipboardData data, out string errorMessage) {
            data = null;
            var buffer = EditorGUIUtility.systemCopyBuffer;
            var prefix = ClipboardHeader + CurrentVersion + "\n";
            if (string.IsNullOrEmpty(buffer) || !buffer.StartsWith(prefix, StringComparison.Ordinal)) {
                errorMessage = "Clipboard does not contain Animation Graph items";
                return false;
            }

            try {
                data = JsonUtility.FromJson<ClipboardData>(buffer.Substring(prefix.Length));
            }
            catch (Exception exception) {
                errorMessage = $"Invalid Animation Graph clipboard data: {exception.Message}";
                return false;
            }

            if (data == null || data.Version != CurrentVersion) {
                errorMessage = "Unsupported Animation Graph clipboard version";
                return false;
            }

            if (data.Nodes.Count == 0 && data.Signals.Count == 0) {
                errorMessage = "No copied item";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static void DeserializeObjects<T>(IReadOnlyList<ClipboardObjectData> source, List<T> destination) where T : ScriptableObject {
            for (var i = 0; i < source.Count; i++) {
                var objectData = source[i];
                var type = Type.GetType(objectData.TypeName, false);
                if (type == null || type.IsAbstract || !typeof(T).IsAssignableFrom(type)) {
                    throw new InvalidOperationException($"Type '{objectData.TypeName}' is not available");
                }

                var instance = (T)ScriptableObject.CreateInstance(type);
                EditorJsonUtility.FromJsonOverwrite(objectData.Json, instance);
                RestoreObjectReferences(instance, objectData.ObjectReferences);
                ClearGraphElementReferences(instance);
                destination.Add(instance);
            }
        }

        private static void RestoreObjectReferences(UnityEngine.Object target, IReadOnlyList<ObjectReferenceData> references) {
            var serializedObject = new SerializedObject(target);
            for (var i = 0; i < references.Count; i++) {
                var reference = references[i];
                var property = serializedObject.FindProperty(reference.PropertyPath);
                if (property == null || !GlobalObjectId.TryParse(reference.GlobalObjectId, out var globalObjectId)) {
                    continue;
                }

                property.objectReferenceValue = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId);
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ClearGraphElementReferences(UnityEngine.Object target) {
            var serializedObject = new SerializedObject(target);
            var iterator = serializedObject.GetIterator();
            if (!iterator.Next(true)) {
                return;
            }

            do {
                if (iterator.propertyType == SerializedPropertyType.ObjectReference
                    && (iterator.objectReferenceValue is Node || iterator.objectReferenceValue is Signal)) {
                    iterator.objectReferenceValue = null;
                }
            } while (iterator.Next(true));

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static IReadOnlyList<Node> PasteNodes(AnimationGraphAsset graphAsset, IReadOnlyList<Node> sourceNodes, Vector2 offset) {
            var pastedNodes = new List<Node>();
            var pastedNodesBySourceId = new Dictionary<string, Node>(StringComparer.Ordinal);
            for (var i = 0; i < sourceNodes.Count; i++) {
                var sourceNode = sourceNodes[i];
                var pastedNode = AnimationGraphAssetUtility.DuplicateNode(graphAsset, sourceNode, sourceNode.GraphPosition + offset);
                pastedNodes.Add(pastedNode);
                pastedNodesBySourceId[sourceNode.NodeId] = pastedNode;
            }

            for (var i = 0; i < sourceNodes.Count; i++) {
                RestoreNodeConnections(sourceNodes[i], pastedNodes[i], pastedNodesBySourceId);
            }

            return pastedNodes;
        }

        private static void RestoreNodeConnections(Node sourceNode, Node pastedNode, IReadOnlyDictionary<string, Node> pastedNodesBySourceId) {
            AnimationGraphAssetUtility.SetNodeNextNodeIds(pastedNode, RemapNodeIds(sourceNode.NextNodeIds, pastedNodesBySourceId));
            if (sourceNode is BranchNode sourceBranchNode && pastedNode is BranchNode pastedBranchNode) {
                for (var i = 0; i < sourceBranchNode.ExtensionPortCount; i++) {
                    AnimationGraphAssetUtility.SetBranchExtensionNodeIds(pastedBranchNode, i, RemapNodeIds(sourceBranchNode.GetExtensionNodeIds(i), pastedNodesBySourceId));
                }
            }

            if (sourceNode is LoopNode sourceLoopNode && pastedNode is LoopNode pastedLoopNode) {
                AnimationGraphAssetUtility.SetLoopNodeIds(pastedLoopNode, RemapNodeIds(sourceLoopNode.LoopNodeIds, pastedNodesBySourceId));
            }
            else if (sourceNode is EachNode sourceEachNode && pastedNode is EachNode pastedEachNode) {
                AnimationGraphAssetUtility.SetEachNodeIds(pastedEachNode, RemapNodeIds(sourceEachNode.EachNodeIds, pastedNodesBySourceId));
            }

            if (sourceNode is ActionNode sourceActionNode
                && pastedNode is ActionNode pastedActionNode
                && pastedNodesBySourceId.TryGetValue(sourceActionNode.TargetReference.ScopeNodeId, out var pastedScopeNode)) {
                var sourceReference = sourceActionNode.TargetReference;
                AnimationGraphAssetUtility.SetActionNodeTargetReference(pastedActionNode, new TargetReference(sourceReference.Kind, sourceReference.TargetKey, pastedScopeNode.NodeId));
            }
        }

        private static string[] RemapNodeIds(IReadOnlyList<string> sourceIds, IReadOnlyDictionary<string, Node> pastedNodesBySourceId) {
            var result = new List<string>();
            for (var i = 0; i < sourceIds.Count; i++) {
                if (pastedNodesBySourceId.TryGetValue(sourceIds[i], out var pastedNode)) {
                    result.Add(pastedNode.NodeId);
                }
            }

            return result.ToArray();
        }

        private static IReadOnlyList<Signal> PasteSignals(AnimationGraphAsset graphAsset, IReadOnlyList<Signal> sourceSignals, Vector2 offset) {
            var pastedSignals = new List<Signal>();
            for (var i = 0; i < sourceSignals.Count; i++) {
                var sourceSignal = sourceSignals[i];
                pastedSignals.Add(AnimationGraphAssetUtility.DuplicateSignal(graphAsset, sourceSignal, sourceSignal.GraphPosition + offset));
            }

            return pastedSignals;
        }

        private static void RestoreSignalConnections(IReadOnlyList<SignalConnectionData> connections, IReadOnlyList<Node> sourceNodes, IReadOnlyList<Node> pastedNodes, IReadOnlyList<Signal> sourceSignals, IReadOnlyList<Signal> pastedSignals) {
            var pastedNodesBySourceId = CreateObjectMap(sourceNodes, pastedNodes, node => node.NodeId);
            var pastedSignalsBySourceId = CreateObjectMap(sourceSignals, pastedSignals, signal => signal.SignalId);
            for (var i = 0; i < connections.Count; i++) {
                var connection = connections[i];
                if (!pastedNodesBySourceId.TryGetValue(connection.NodeId, out var node)
                    || !pastedSignalsBySourceId.TryGetValue(connection.SignalId, out var signal)) {
                    continue;
                }

                if (connection.IsEnter) {
                    AnimationGraphAssetUtility.AddEnterSignalReference(node, signal);
                }
                else {
                    AnimationGraphAssetUtility.AddExitSignalReference(node, signal);
                }
            }
        }

        private static Dictionary<string, T> CreateObjectMap<T>(IReadOnlyList<T> source, IReadOnlyList<T> pasted, Func<T, string> getId) {
            var result = new Dictionary<string, T>(StringComparer.Ordinal);
            for (var i = 0; i < source.Count && i < pasted.Count; i++) {
                result[getId(source[i])] = pasted[i];
            }

            return result;
        }

        private static void DestroyObjects<T>(IReadOnlyList<T> objects) where T : UnityEngine.Object {
            for (var i = 0; i < objects.Count; i++) {
                if (objects[i] != null) {
                    UnityEngine.Object.DestroyImmediate(objects[i]);
                }
            }
        }
    }
}
