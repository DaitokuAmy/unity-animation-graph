using System;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// AnimationGraphAsset の編集操作を提供するユーティリティ
    /// </summary>
    public static class AnimationGraphAssetUtility {
        /// <summary>Node の ID フィールド名</summary>
        private const string NodeIdPropertyName = "_nodeId";
        /// <summary>Node のグラフ位置フィールド名</summary>
        private const string GraphPositionPropertyName = "_graphPosition";
        /// <summary>Node の後続ノード ID フィールド名</summary>
        private const string NextNodeIdsPropertyName = "_nextNodeIds";
        /// <summary>BranchNode の false 側後続ノード ID フィールド名</summary>
        private const string FalseNodeIdsPropertyName = "_falseNodeIds";
        /// <summary>RepeatNode の戻り先ノード ID フィールド名</summary>
        private const string RepeatNodeIdPropertyName = "_repeatNodeId";
        /// <summary>AnimationGraphAsset の asset GUID フィールド名</summary>
        private const string AssetGuidPropertyName = "_assetGuid";
        /// <summary>AnimationGraphAsset のグラフシードフィールド名</summary>
        private const string GraphSeedPropertyName = "_graphSeed";
        /// <summary>AnimationGraphAsset の開始ノード ID フィールド名</summary>
        private const string StartNodeIdPropertyName = "_startNodeId";
        /// <summary>AnimationGraphAsset のノード配列フィールド名</summary>
        private const string NodesPropertyName = "_nodes";
        /// <summary>AnimationGraphAsset の target 定義配列フィールド名</summary>
        private const string TargetDefinitionsPropertyName = "_targetDefinitions";
        /// <summary>AnimationGraphAsset の Blackboard 定義配列フィールド名</summary>
        private const string BlackboardDefinitionsPropertyName = "_blackboardDefinitions";

        /// <summary>
        /// AnimationGraphAsset を初期状態に戻す
        /// </summary>
        /// <param name="graphAsset">初期化対象の AnimationGraphAsset</param>
        /// <returns>初期化時に作成した開始ノード</returns>
        public static Node InitializeGraph(AnimationGraphAsset graphAsset) {
            return InitializeGraph(graphAsset, Vector2.zero);
        }

        /// <summary>
        /// AnimationGraphAsset を初期状態に戻す
        /// </summary>
        /// <param name="graphAsset">初期化対象の AnimationGraphAsset</param>
        /// <param name="startNodePosition">開始ノードのエディタ上の位置</param>
        /// <returns>初期化時に作成した開始ノード</returns>
        public static Node InitializeGraph(AnimationGraphAsset graphAsset, Vector2 startNodePosition) {
            if (graphAsset == null) {
                throw new ArgumentNullException(nameof(graphAsset));
            }

            var undoName = "Initialize Animation Graph";
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            Undo.RecordObject(graphAsset, undoName);

            try {
                ClearSubAssets(graphAsset);
                ClearGraphReferences(graphAsset);
                var startNode = AddNode(graphAsset, typeof(StartNode), startNodePosition);
                var serializedGraph = new SerializedObject(graphAsset);
                serializedGraph.FindProperty(AssetGuidPropertyName).stringValue = GetAssetGuid(graphAsset);
                serializedGraph.FindProperty(GraphSeedPropertyName).intValue = BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0);
                serializedGraph.FindProperty(StartNodeIdPropertyName).stringValue = startNode.NodeId;
                serializedGraph.ApplyModifiedProperties();
                EditorUtility.SetDirty(graphAsset);
                return startNode;
            }
            finally {
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        /// <summary>
        /// AnimationGraphAsset にノードを追加
        /// </summary>
        /// <param name="graphAsset">追加先の AnimationGraphAsset</param>
        /// <param name="graphPosition">エディタ上のノード位置</param>
        /// <typeparam name="T">追加するノード型</typeparam>
        /// <returns>追加したノード</returns>
        public static T AddNode<T>(AnimationGraphAsset graphAsset, Vector2 graphPosition) where T : Node {
            return (T)AddNode(graphAsset, typeof(T), graphPosition);
        }

        /// <summary>
        /// AnimationGraphAsset にノードを追加
        /// </summary>
        /// <param name="graphAsset">追加先の AnimationGraphAsset</param>
        /// <param name="nodeType">追加するノード型</param>
        /// <param name="graphPosition">エディタ上のノード位置</param>
        /// <returns>追加したノード</returns>
        public static Node AddNode(AnimationGraphAsset graphAsset, Type nodeType, Vector2 graphPosition) {
            if (graphAsset == null) {
                throw new ArgumentNullException(nameof(graphAsset));
            }

            if (nodeType == null) {
                throw new ArgumentNullException(nameof(nodeType));
            }

            if (!typeof(Node).IsAssignableFrom(nodeType)) {
                throw new ArgumentException($"{nodeType.FullName} does not derive from Node", nameof(nodeType));
            }

            if (nodeType.IsAbstract) {
                throw new ArgumentException($"{nodeType.FullName} is abstract", nameof(nodeType));
            }

            var node = (Node)ScriptableObject.CreateInstance(nodeType);
            node.name = nodeType.Name;

            var undoName = $"Add {nodeType.Name}";
            Undo.RegisterCreatedObjectUndo(node, undoName);
            Undo.RecordObject(graphAsset, undoName);

            var nodeId = GenerateNodeId(graphAsset);
            SetupNode(node, nodeId, graphPosition);
            AssetDatabase.AddObjectToAsset(node, graphAsset);
            AddNodeReference(graphAsset, node);

            EditorUtility.SetDirty(node);
            EditorUtility.SetDirty(graphAsset);

            var assetPath = AssetDatabase.GetAssetPath(graphAsset);
            if (!string.IsNullOrEmpty(assetPath)) {
                AssetDatabase.ImportAsset(assetPath);
            }

            return node;
        }

        /// <summary>
        /// AnimationGraphAsset からノードを削除
        /// </summary>
        /// <param name="graphAsset">削除元の AnimationGraphAsset</param>
        /// <param name="node">削除するノード</param>
        public static void RemoveNode(AnimationGraphAsset graphAsset, Node node) {
            if (graphAsset == null) {
                throw new ArgumentNullException(nameof(graphAsset));
            }

            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }

            if (node.NodeId == graphAsset.StartNodeId) {
                throw new InvalidOperationException("StartNode cannot be removed. Use InitializeGraph instead");
            }

            var nodes = graphAsset.Nodes;
            var nodeIndex = -1;
            for (var i = 0; i < nodes.Count; i++) {
                if (nodes[i] != node) {
                    continue;
                }

                nodeIndex = i;
                break;
            }

            if (nodeIndex < 0) {
                throw new InvalidOperationException("Node is not contained in AnimationGraphAsset");
            }

            var undoName = $"Remove {node.name}";
            Undo.RecordObject(graphAsset, undoName);

            for (var i = 0; i < nodes.Count; i++) {
                var current = nodes[i];
                if (current == null || current == node) {
                    continue;
                }

                var hasReference = false;
                var nextNodeIds = current.NextNodeIds;
                for (var j = 0; j < nextNodeIds.Count; j++) {
                    if (nextNodeIds[j] != node.NodeId) {
                        continue;
                    }

                    hasReference = true;
                    break;
                }

                if (current is BranchNode branchNode) {
                    var falseNodeIds = branchNode.FalseNodeIds;
                    for (var j = 0; j < falseNodeIds.Count; j++) {
                        if (falseNodeIds[j] != node.NodeId) {
                            continue;
                        }

                        hasReference = true;
                        break;
                    }
                }

                if (current is RepeatNode repeatNode && repeatNode.RepeatNodeId == node.NodeId) {
                    hasReference = true;
                }

                if (!hasReference) {
                    continue;
                }

                Undo.RecordObject(current, undoName);
                var serializedNode = new SerializedObject(current);
                var nextNodeIdsProperty = serializedNode.FindProperty(NextNodeIdsPropertyName);
                for (var j = nextNodeIdsProperty.arraySize - 1; j >= 0; j--) {
                    if (nextNodeIdsProperty.GetArrayElementAtIndex(j).stringValue != node.NodeId) {
                        continue;
                    }

                    nextNodeIdsProperty.DeleteArrayElementAtIndex(j);
                }

                if (current is BranchNode) {
                    var falseNodeIdsProperty = serializedNode.FindProperty(FalseNodeIdsPropertyName);
                    for (var j = falseNodeIdsProperty.arraySize - 1; j >= 0; j--) {
                        if (falseNodeIdsProperty.GetArrayElementAtIndex(j).stringValue != node.NodeId) {
                            continue;
                        }

                        falseNodeIdsProperty.DeleteArrayElementAtIndex(j);
                    }
                }

                if (current is RepeatNode) {
                    var repeatNodeIdProperty = serializedNode.FindProperty(RepeatNodeIdPropertyName);
                    if (repeatNodeIdProperty.stringValue == node.NodeId) {
                        repeatNodeIdProperty.stringValue = string.Empty;
                    }
                }

                serializedNode.ApplyModifiedProperties();
                EditorUtility.SetDirty(current);
            }

            var serializedGraph = new SerializedObject(graphAsset);
            var nodesProperty = serializedGraph.FindProperty(NodesPropertyName);
            nodesProperty.GetArrayElementAtIndex(nodeIndex).objectReferenceValue = null;
            nodesProperty.DeleteArrayElementAtIndex(nodeIndex);
            serializedGraph.ApplyModifiedProperties();

            Undo.DestroyObjectImmediate(node);
            EditorUtility.SetDirty(graphAsset);

            var assetPath = AssetDatabase.GetAssetPath(graphAsset);
            if (!string.IsNullOrEmpty(assetPath)) {
                AssetDatabase.ImportAsset(assetPath);
            }
        }

        /// <summary>
        /// AnimationGraphAsset 内で一意なノード ID を生成
        /// </summary>
        /// <param name="graphAsset">生成先の AnimationGraphAsset</param>
        /// <returns>一意なノード ID</returns>
        private static string GenerateNodeId(AnimationGraphAsset graphAsset) {
            string nodeId;
            do {
                nodeId = Guid.NewGuid().ToString("N");
            }
            while (graphAsset.TryGetNode(nodeId, out _));

            return nodeId;
        }

        /// <summary>
        /// 追加直後のノード情報を設定
        /// </summary>
        /// <param name="node">設定対象のノード</param>
        /// <param name="nodeId">設定するノード ID</param>
        /// <param name="graphPosition">エディタ上のノード位置</param>
        private static void SetupNode(Node node, string nodeId, Vector2 graphPosition) {
            var serializedNode = new SerializedObject(node);
            serializedNode.FindProperty(NodeIdPropertyName).stringValue = nodeId;
            serializedNode.FindProperty(GraphPositionPropertyName).vector2Value = graphPosition;
            serializedNode.FindProperty(NextNodeIdsPropertyName).arraySize = 0;
            if (node is BranchNode) {
                serializedNode.FindProperty(FalseNodeIdsPropertyName).arraySize = 0;
            }

            if (node is RepeatNode) {
                serializedNode.FindProperty(RepeatNodeIdPropertyName).stringValue = string.Empty;
            }

            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// AnimationGraphAsset に含まれる SubAsset を削除
        /// </summary>
        /// <param name="graphAsset">削除対象を含む AnimationGraphAsset</param>
        private static void ClearSubAssets(AnimationGraphAsset graphAsset) {
            var assetPath = AssetDatabase.GetAssetPath(graphAsset);
            if (string.IsNullOrEmpty(assetPath)) {
                throw new InvalidOperationException("AnimationGraphAsset must be saved as an asset before initialization");
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (var i = 0; i < assets.Length; i++) {
                var asset = assets[i];
                if (asset == null || asset == graphAsset || !AssetDatabase.IsSubAsset(asset)) {
                    continue;
                }

                Undo.DestroyObjectImmediate(asset);
            }
        }

        /// <summary>
        /// AnimationGraphAsset のノード参照と schema を空に戻す
        /// </summary>
        /// <param name="graphAsset">初期化対象の AnimationGraphAsset</param>
        private static void ClearGraphReferences(AnimationGraphAsset graphAsset) {
            var serializedGraph = new SerializedObject(graphAsset);
            serializedGraph.FindProperty(StartNodeIdPropertyName).stringValue = string.Empty;
            serializedGraph.FindProperty(NodesPropertyName).arraySize = 0;
            serializedGraph.FindProperty(TargetDefinitionsPropertyName).arraySize = 0;
            serializedGraph.FindProperty(BlackboardDefinitionsPropertyName).arraySize = 0;
            serializedGraph.ApplyModifiedProperties();
        }

        /// <summary>
        /// AnimationGraphAsset の Unity asset GUID を取得
        /// </summary>
        /// <param name="graphAsset">取得対象の AnimationGraphAsset</param>
        /// <returns>Unity asset GUID</returns>
        private static string GetAssetGuid(AnimationGraphAsset graphAsset) {
            var assetPath = AssetDatabase.GetAssetPath(graphAsset);
            return string.IsNullOrEmpty(assetPath) ? string.Empty : AssetDatabase.AssetPathToGUID(assetPath);
        }

        /// <summary>
        /// AnimationGraphAsset にノード参照を追加
        /// </summary>
        /// <param name="graphAsset">追加先の AnimationGraphAsset</param>
        /// <param name="node">追加するノード</param>
        private static void AddNodeReference(AnimationGraphAsset graphAsset, Node node) {
            var serializedGraph = new SerializedObject(graphAsset);
            var nodesProperty = serializedGraph.FindProperty(NodesPropertyName);
            var index = nodesProperty.arraySize;
            nodesProperty.arraySize++;
            nodesProperty.GetArrayElementAtIndex(index).objectReferenceValue = node;
            serializedGraph.ApplyModifiedProperties();
        }
    }
}
