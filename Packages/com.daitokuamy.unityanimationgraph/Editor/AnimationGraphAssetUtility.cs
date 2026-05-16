using System;
using System.Collections.Generic;
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
        /// <summary>ActionNode の target key フィールド名</summary>
        private const string ActionTargetKeyPropertyName = "_targetKey";
        /// <summary>DelayNode delay property name</summary>
        private const string DelayPropertyName = "_delay";
        /// <summary>FlagBranchNode flag key property name</summary>
        private const string FlagBranchFlagKeyPropertyName = "_flagKey";
        /// <summary>JoinNode join type property name</summary>
        private const string JoinTypePropertyName = "_joinType";
        /// <summary>LoopNode のループ実行回数フィールド名</summary>
        private const string LoopCountPropertyName = "_loopCount";
        /// <summary>BranchNode の false 側後続ノード ID フィールド名</summary>
        private const string FalseNodeIdsPropertyName = "_falseNodeIds";
        /// <summary>LoopNode のループ内容ノード ID フィールド名</summary>
        private const string LoopNodeIdsPropertyName = "_loopNodeIds";
        /// <summary>AnimationGraphAsset の asset GUID フィールド名</summary>
        private const string AssetGuidPropertyName = "_assetGuid";
        /// <summary>AnimationGraphAsset のグラフシードフィールド名</summary>
        private const string GraphSeedPropertyName = "_graphSeed";
        /// <summary>AnimationGraphAsset のランダムシードフィールド名</summary>
        private const string RandomSeedPropertyName = "_randomSeed";
        /// <summary>AnimationGraphAsset の開始ノード ID フィールド名</summary>
        private const string StartNodeIdPropertyName = "_startNodeId";
        /// <summary>AnimationGraphAsset のノード配列フィールド名</summary>
        private const string NodesPropertyName = "_nodes";
        /// <summary>AnimationGraphAsset の target 定義配列フィールド名</summary>
        private const string TargetDefinitionsPropertyName = "_targetDefinitions";
        /// <summary>AnimationGraphAsset の Blackboard 定義配列フィールド名</summary>
        private const string BlackboardDefinitionsPropertyName = "_blackboardDefinitions";
        /// <summary>schema 定義の key フィールド名</summary>
        private const string KeyPropertyName = "_key";
        /// <summary>target 定義の MonoScript GUID フィールド名</summary>
        private const string MonoScriptGuidPropertyName = "_monoScriptGuid";
        /// <summary>Blackboard 定義の value type フィールド名</summary>
        private const string ValueTypePropertyName = "_valueType";
        /// <summary>Blackboard 定義の bool default value フィールド名</summary>
        private const string DefaultBoolValuePropertyName = "_defaultBoolValue";
        /// <summary>Blackboard 定義の int default value フィールド名</summary>
        private const string DefaultIntValuePropertyName = "_defaultIntValue";
        /// <summary>Blackboard 定義の float default value フィールド名</summary>
        private const string DefaultFloatValuePropertyName = "_defaultFloatValue";
        /// <summary>Blackboard 定義の string default value フィールド名</summary>
        private const string DefaultStringValuePropertyName = "_defaultStringValue";
        /// <summary>Blackboard 定義の Vector2 default value フィールド名</summary>
        private const string DefaultVector2ValuePropertyName = "_defaultVector2Value";
        /// <summary>Blackboard 定義の Vector3 default value フィールド名</summary>
        private const string DefaultVector3ValuePropertyName = "_defaultVector3Value";
        /// <summary>Blackboard 定義の Color default value フィールド名</summary>
        private const string DefaultColorValuePropertyName = "_defaultColorValue";

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
                serializedGraph.FindProperty(RandomSeedPropertyName).boolValue = false;
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
        /// AnimationGraphAsset にノードを複製して追加
        /// </summary>
        /// <param name="graphAsset">追加先の AnimationGraphAsset</param>
        /// <param name="sourceNode">複製元の Node</param>
        /// <param name="graphPosition">エディタ上のノード位置</param>
        /// <returns>複製したノード</returns>
        public static Node DuplicateNode(AnimationGraphAsset graphAsset, Node sourceNode, Vector2 graphPosition) {
            if (graphAsset == null) {
                throw new ArgumentNullException(nameof(graphAsset));
            }

            if (sourceNode == null) {
                throw new ArgumentNullException(nameof(sourceNode));
            }

            if (sourceNode.NodeId == graphAsset.StartNodeId) {
                throw new InvalidOperationException("StartNode cannot be duplicated");
            }

            var nodeType = sourceNode.GetType();
            var node = (Node)ScriptableObject.CreateInstance(nodeType);
            node.name = sourceNode.name;

            var undoName = $"Duplicate {nodeType.Name}";
            Undo.RegisterCreatedObjectUndo(node, undoName);
            Undo.RecordObject(graphAsset, undoName);

            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(sourceNode), node);
            node.name = sourceNode.name;

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
        /// Node のエディタ上の位置を設定
        /// </summary>
        /// <param name="node">設定対象の Node</param>
        /// <param name="graphPosition">エディタ上のノード位置</param>
        public static void SetNodeGraphPosition(Node node, Vector2 graphPosition) {
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }

            var undoName = "Set Animation Graph Node Position";
            Undo.RecordObject(node, undoName);

            var serializedNode = new SerializedObject(node);
            serializedNode.FindProperty(GraphPositionPropertyName).vector2Value = graphPosition;
            serializedNode.ApplyModifiedProperties();
            EditorUtility.SetDirty(node);
        }

        /// <summary>
        /// Node の後続ノード ID 一覧を設定
        /// </summary>
        /// <param name="node">設定対象の Node</param>
        /// <param name="nextNodeIds">設定する後続ノード ID 一覧</param>
        public static void SetNodeNextNodeIds(Node node, IReadOnlyList<string> nextNodeIds) {
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }

            if (nextNodeIds == null) {
                throw new ArgumentNullException(nameof(nextNodeIds));
            }

            var undoName = "Set Animation Graph Node Connections";
            Undo.RecordObject(node, undoName);

            var serializedNode = new SerializedObject(node);
            var nextNodeIdsProperty = serializedNode.FindProperty(NextNodeIdsPropertyName);
            nextNodeIdsProperty.arraySize = nextNodeIds.Count;
            for (var i = 0; i < nextNodeIds.Count; i++) {
                nextNodeIdsProperty.GetArrayElementAtIndex(i).stringValue = nextNodeIds[i] ?? string.Empty;
            }

            serializedNode.ApplyModifiedProperties();
            EditorUtility.SetDirty(node);
        }

        /// <summary>
        /// ActionNode の target key を設定
        /// </summary>
        /// <param name="node">設定対象の ActionNode</param>
        /// <param name="targetKey">設定する target key</param>
        public static void SetActionNodeTargetKey(ActionNode node, string targetKey) {
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }

            var undoName = "Set Animation Graph Action Target";
            Undo.RecordObject(node, undoName);

            var serializedNode = new SerializedObject(node);
            serializedNode.FindProperty(ActionTargetKeyPropertyName).stringValue = targetKey ?? string.Empty;
            serializedNode.ApplyModifiedProperties();
            EditorUtility.SetDirty(node);
        }

        /// <summary>
        /// Sets the delay of a DelayNode.
        /// </summary>
        /// <param name="node">Target DelayNode</param>
        /// <param name="delay">Delay to set</param>
        public static void SetDelayNodeDelay(DelayNode node, float delay) {
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }

            var undoName = "Set Animation Graph Delay";
            Undo.RecordObject(node, undoName);

            var serializedNode = new SerializedObject(node);
            serializedNode.FindProperty(DelayPropertyName).floatValue = Mathf.Max(0.0f, delay);
            serializedNode.ApplyModifiedProperties();
            EditorUtility.SetDirty(node);
        }

        /// <summary>
        /// Sets the flag key of a FlagBranchNode.
        /// </summary>
        /// <param name="node">Target FlagBranchNode</param>
        /// <param name="flagKey">Flag key to set</param>
        public static void SetFlagBranchNodeFlagKey(FlagBranchNode node, string flagKey) {
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }

            var undoName = "Set Animation Graph Flag Key";
            Undo.RecordObject(node, undoName);

            var serializedNode = new SerializedObject(node);
            serializedNode.FindProperty(FlagBranchFlagKeyPropertyName).stringValue = flagKey ?? string.Empty;
            serializedNode.ApplyModifiedProperties();
            EditorUtility.SetDirty(node);
        }

        /// <summary>
        /// Sets the join type of a JoinNode.
        /// </summary>
        /// <param name="node">Target JoinNode</param>
        /// <param name="joinType">Join type to set</param>
        public static void SetJoinNodeJoinType(JoinNode node, JoinType joinType) {
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }

            var undoName = "Set Animation Graph Join Type";
            Undo.RecordObject(node, undoName);

            var serializedNode = new SerializedObject(node);
            serializedNode.FindProperty(JoinTypePropertyName).enumValueIndex = (int)joinType;
            serializedNode.ApplyModifiedProperties();
            EditorUtility.SetDirty(node);
        }

        /// <summary>
        /// LoopNode のループ実行回数を設定
        /// </summary>
        /// <param name="node">設定対象の LoopNode</param>
        /// <param name="loopCount">設定するループ実行回数</param>
        public static void SetLoopNodeLoopCount(LoopNode node, int loopCount) {
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }

            var undoName = "Set Animation Graph Loop Count";
            Undo.RecordObject(node, undoName);

            var serializedNode = new SerializedObject(node);
            serializedNode.FindProperty(LoopCountPropertyName).intValue = Mathf.Max(1, loopCount);
            serializedNode.ApplyModifiedProperties();
            EditorUtility.SetDirty(node);
        }

        /// <summary>
        /// BranchNode の false 側後続ノード ID 一覧を設定
        /// </summary>
        /// <param name="node">設定対象の BranchNode</param>
        /// <param name="falseNodeIds">設定する false 側後続ノード ID 一覧</param>
        public static void SetBranchFalseNodeIds(BranchNode node, IReadOnlyList<string> falseNodeIds) {
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }

            if (falseNodeIds == null) {
                throw new ArgumentNullException(nameof(falseNodeIds));
            }

            var undoName = "Set Animation Graph Branch Connections";
            Undo.RecordObject(node, undoName);

            var serializedNode = new SerializedObject(node);
            var falseNodeIdsProperty = serializedNode.FindProperty(FalseNodeIdsPropertyName);
            falseNodeIdsProperty.arraySize = falseNodeIds.Count;
            for (var i = 0; i < falseNodeIds.Count; i++) {
                falseNodeIdsProperty.GetArrayElementAtIndex(i).stringValue = falseNodeIds[i] ?? string.Empty;
            }

            serializedNode.ApplyModifiedProperties();
            EditorUtility.SetDirty(node);
        }

        /// <summary>
        /// LoopNode のループ内容ノード ID 一覧を設定
        /// </summary>
        /// <param name="node">設定対象の LoopNode</param>
        /// <param name="loopNodeIds">設定するループ内容ノード ID 一覧</param>
        public static void SetLoopNodeIds(LoopNode node, IReadOnlyList<string> loopNodeIds) {
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }

            if (loopNodeIds == null) {
                throw new ArgumentNullException(nameof(loopNodeIds));
            }

            var undoName = "Set Animation Graph Loop Connections";
            Undo.RecordObject(node, undoName);

            var serializedNode = new SerializedObject(node);
            var loopNodeIdsProperty = serializedNode.FindProperty(LoopNodeIdsPropertyName);
            loopNodeIdsProperty.arraySize = loopNodeIds.Count;
            for (var i = 0; i < loopNodeIds.Count; i++) {
                loopNodeIdsProperty.GetArrayElementAtIndex(i).stringValue = loopNodeIds[i] ?? string.Empty;
            }

            serializedNode.ApplyModifiedProperties();
            EditorUtility.SetDirty(node);
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

                if (current is LoopNode loopNode) {
                    var loopNodeIds = loopNode.LoopNodeIds;
                    for (var j = 0; j < loopNodeIds.Count; j++) {
                        if (loopNodeIds[j] != node.NodeId) {
                            continue;
                        }

                        hasReference = true;
                        break;
                    }
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

                if (current is LoopNode) {
                    var loopNodeIdsProperty = serializedNode.FindProperty(LoopNodeIdsPropertyName);
                    for (var j = loopNodeIdsProperty.arraySize - 1; j >= 0; j--) {
                        if (loopNodeIdsProperty.GetArrayElementAtIndex(j).stringValue != node.NodeId) {
                            continue;
                        }

                        loopNodeIdsProperty.DeleteArrayElementAtIndex(j);
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
        /// AnimationGraphAsset の target 定義を設定
        /// </summary>
        /// <param name="graphAsset">設定対象の AnimationGraphAsset</param>
        /// <param name="definitions">設定する target 定義一覧</param>
        public static void SetTargetDefinitions(AnimationGraphAsset graphAsset, IReadOnlyList<AnimationGraphTargetDefinition> definitions) {
            if (graphAsset == null) {
                throw new ArgumentNullException(nameof(graphAsset));
            }

            if (definitions == null) {
                throw new ArgumentNullException(nameof(definitions));
            }

            var undoName = "Set Animation Graph Target Definitions";
            Undo.RecordObject(graphAsset, undoName);

            var serializedGraph = new SerializedObject(graphAsset);
            var definitionsProperty = serializedGraph.FindProperty(TargetDefinitionsPropertyName);
            definitionsProperty.arraySize = definitions.Count;
            for (var i = 0; i < definitions.Count; i++) {
                var definitionProperty = definitionsProperty.GetArrayElementAtIndex(i);
                definitionProperty.FindPropertyRelative(KeyPropertyName).stringValue = definitions[i].Key;
                definitionProperty.FindPropertyRelative(MonoScriptGuidPropertyName).stringValue = definitions[i].MonoScriptGuid;
            }

            serializedGraph.ApplyModifiedProperties();
            EditorUtility.SetDirty(graphAsset);
        }

        /// <summary>
        /// AnimationGraphAsset の Blackboard 定義を設定
        /// </summary>
        /// <param name="graphAsset">設定対象の AnimationGraphAsset</param>
        /// <param name="definitions">設定する Blackboard 定義一覧</param>
        public static void SetBlackboardDefinitions(AnimationGraphAsset graphAsset, IReadOnlyList<AnimationGraphBlackboardDefinition> definitions) {
            if (graphAsset == null) {
                throw new ArgumentNullException(nameof(graphAsset));
            }

            if (definitions == null) {
                throw new ArgumentNullException(nameof(definitions));
            }

            var undoName = "Set Animation Graph Blackboard Definitions";
            Undo.RecordObject(graphAsset, undoName);

            var serializedGraph = new SerializedObject(graphAsset);
            var definitionsProperty = serializedGraph.FindProperty(BlackboardDefinitionsPropertyName);
            definitionsProperty.arraySize = definitions.Count;
            for (var i = 0; i < definitions.Count; i++) {
                SetBlackboardDefinitionProperty(definitionsProperty.GetArrayElementAtIndex(i), definitions[i]);
            }

            serializedGraph.ApplyModifiedProperties();
            EditorUtility.SetDirty(graphAsset);
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

            if (node is LoopNode) {
                serializedNode.FindProperty(LoopNodeIdsPropertyName).arraySize = 0;
            }

            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Blackboard 定義の SerializedProperty に値を設定
        /// </summary>
        /// <param name="definitionProperty">設定対象の SerializedProperty</param>
        /// <param name="definition">設定する Blackboard 定義</param>
        private static void SetBlackboardDefinitionProperty(SerializedProperty definitionProperty, AnimationGraphBlackboardDefinition definition) {
            definitionProperty.FindPropertyRelative(KeyPropertyName).stringValue = definition.Key;
            definitionProperty.FindPropertyRelative(ValueTypePropertyName).enumValueIndex = (int)definition.ValueType;
            definitionProperty.FindPropertyRelative(DefaultBoolValuePropertyName).boolValue = definition.DefaultBoolValue;
            definitionProperty.FindPropertyRelative(DefaultIntValuePropertyName).intValue = definition.DefaultIntValue;
            definitionProperty.FindPropertyRelative(DefaultFloatValuePropertyName).floatValue = definition.DefaultFloatValue;
            definitionProperty.FindPropertyRelative(DefaultStringValuePropertyName).stringValue = definition.DefaultStringValue;
            definitionProperty.FindPropertyRelative(DefaultVector2ValuePropertyName).vector2Value = definition.DefaultVector2Value;
            definitionProperty.FindPropertyRelative(DefaultVector3ValuePropertyName).vector3Value = definition.DefaultVector3Value;
            definitionProperty.FindPropertyRelative(DefaultColorValuePropertyName).colorValue = definition.DefaultColorValue;
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

    /// <summary>
    /// target 定義に設定できる Component 型候補
    /// </summary>
    internal readonly struct AnimationGraphTargetComponentOption {
        /// <summary>
        /// AnimationGraphTargetComponentOption を生成
        /// </summary>
        /// <param name="componentType">Component 型</param>
        /// <param name="monoScriptGuid">MonoScript GUID または組み込み Component の予約 GUID</param>
        /// <param name="menuPath">Component 選択メニュー上の表示パス</param>
        public AnimationGraphTargetComponentOption(Type componentType, string monoScriptGuid, string menuPath) {
            ComponentType = componentType;
            MonoScriptGuid = monoScriptGuid ?? string.Empty;
            MenuPath = menuPath ?? string.Empty;
        }

        /// <summary>Component 型</summary>
        public Type ComponentType { get; }
        /// <summary>MonoScript GUID または組み込み Component の予約 GUID</summary>
        public string MonoScriptGuid { get; }
        /// <summary>Component 選択メニュー上の表示パス</summary>
        public string MenuPath { get; }
    }

    /// <summary>
    /// target 定義に保存された Component 型 GUID を解決するユーティリティ
    /// </summary>
    internal static class AnimationGraphTargetScriptUtility {
        private static readonly AnimationGraphTargetComponentOption[] BuiltInComponentOptions = {
            new(typeof(Transform), "00000000000000000000000000000001", "Built-in/Transform"),
            new(typeof(RectTransform), "00000000000000000000000000000002", "Built-in/Rect Transform"),
            new(typeof(Animator), "00000000000000000000000000000003", "Built-in/Animator"),
            new(typeof(UnityEngine.UI.Graphic), "00000000000000000000000000000004", "UI/Graphic"),
            new(typeof(CanvasGroup), "00000000000000000000000000000005", "UI/Canvas Group"),
            new(typeof(Renderer), "00000000000000000000000000000006", "Rendering/Renderer"),
            new(typeof(SpriteRenderer), "00000000000000000000000000000007", "Rendering/Sprite Renderer"),
            new(typeof(Camera), "00000000000000000000000000000008", "Rendering/Camera"),
            new(typeof(Light), "00000000000000000000000000000009", "Rendering/Light"),
            new(typeof(UnityEngine.Playables.PlayableDirector), "00000000000000000000000000000010", "Timeline/Playable Director"),
            new(typeof(ParticleSystem), "00000000000000000000000000000011", "Effects/Particle System"),
            new(typeof(UnityEngine.UI.Image), "00000000000000000000000000000012", "UI/Image"),
        };

        /// <summary>
        /// MonoScript GUID から MonoScript を取得
        /// </summary>
        /// <param name="monoScriptGuid">MonoScript の asset GUID</param>
        /// <returns>GUID に対応する MonoScript</returns>
        public static MonoScript LoadMonoScript(string monoScriptGuid) {
            if (string.IsNullOrEmpty(monoScriptGuid)) {
                return null;
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(monoScriptGuid);
            return string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
        }

        /// <summary>
        /// Component 型 GUID から Component 型を取得
        /// </summary>
        /// <param name="monoScriptGuid">MonoScript GUID または組み込み Component の予約 GUID</param>
        /// <returns>GUID が指す Component 型。取得できない場合は null</returns>
        public static Type GetTargetComponentType(string monoScriptGuid) {
            if (TryGetBuiltInComponentType(monoScriptGuid, out var componentType)) {
                return componentType;
            }

            var monoScript = LoadMonoScript(monoScriptGuid);
            return GetComponentType(monoScript);
        }

        /// <summary>
        /// ObjectField に指定する target 型を取得
        /// </summary>
        /// <param name="monoScriptGuid">MonoScript GUID または組み込み Component の予約 GUID</param>
        /// <returns>GUID が指す Component 型。取得できない場合は Component 型</returns>
        public static Type GetObjectFieldType(string monoScriptGuid) {
            return GetTargetComponentType(monoScriptGuid) ?? typeof(Component);
        }

        /// <summary>
        /// Component 型選択メニューに表示する候補一覧を取得
        /// </summary>
        /// <returns>Component 型候補一覧</returns>
        public static IReadOnlyList<AnimationGraphTargetComponentOption> GetComponentOptions() {
            var monoScriptGuids = AssetDatabase.FindAssets("t:MonoScript");
            var options = new List<AnimationGraphTargetComponentOption>();
            var typeNames = new HashSet<string>(StringComparer.Ordinal);
            AddBuiltInComponentOptions(options, typeNames);
            for (var i = 0; i < monoScriptGuids.Length; i++) {
                var monoScriptGuid = monoScriptGuids[i];
                var monoScript = LoadMonoScript(monoScriptGuid);
                var componentType = GetComponentType(monoScript);
                if (componentType == null) {
                    continue;
                }

                var typeName = componentType.AssemblyQualifiedName ?? componentType.FullName ?? componentType.Name;
                if (!typeNames.Add(typeName)) {
                    continue;
                }

                options.Add(new AnimationGraphTargetComponentOption(componentType, monoScriptGuid, GetComponentMenuPath(componentType)));
            }

            options.Sort((left, right) => string.Compare(left.MenuPath, right.MenuPath, StringComparison.OrdinalIgnoreCase));
            return options;
        }

        /// <summary>
        /// Component 型 GUID の Component 表示名を取得
        /// </summary>
        /// <param name="monoScriptGuid">MonoScript GUID または組み込み Component の予約 GUID</param>
        /// <returns>Component 表示名</returns>
        public static string GetComponentDisplayName(string monoScriptGuid) {
            if (string.IsNullOrEmpty(monoScriptGuid)) {
                return "Any Component";
            }

            var componentType = GetTargetComponentType(monoScriptGuid);
            return componentType == null ? "Missing Component" : ObjectNames.NicifyVariableName(componentType.Name);
        }

        /// <summary>
        /// MonoScript から target 定義に保存する GUID を取得
        /// </summary>
        /// <param name="monoScript">GUID を取得する MonoScript</param>
        /// <returns>MonoBehaviour 型を定義する MonoScript の asset GUID</returns>
        public static string GetMonoScriptGuid(MonoScript monoScript) {
            if (monoScript == null) {
                return string.Empty;
            }

            if (GetComponentType(monoScript) == null) {
                return string.Empty;
            }

            var assetPath = AssetDatabase.GetAssetPath(monoScript);
            return string.IsNullOrEmpty(assetPath) ? string.Empty : AssetDatabase.AssetPathToGUID(assetPath);
        }

        /// <summary>
        /// target が Component 型 GUID の型に割り当て可能かを判定
        /// </summary>
        /// <param name="target">判定する target</param>
        /// <param name="monoScriptGuid">MonoScript GUID または組み込み Component の予約 GUID</param>
        /// <returns>割り当て可能な場合は true</returns>
        public static bool IsTargetAssignable(UnityEngine.Object target, string monoScriptGuid) {
            if (target == null) {
                return true;
            }

            var targetType = GetTargetComponentType(monoScriptGuid);
            return targetType == null || targetType.IsInstanceOfType(target);
        }

        private static void AddBuiltInComponentOptions(List<AnimationGraphTargetComponentOption> options, HashSet<string> typeNames) {
            for (var i = 0; i < BuiltInComponentOptions.Length; i++) {
                var option = BuiltInComponentOptions[i];
                var typeName = option.ComponentType.AssemblyQualifiedName ?? option.ComponentType.FullName ?? option.ComponentType.Name;
                if (!typeNames.Add(typeName)) {
                    continue;
                }

                options.Add(option);
            }
        }

        private static bool TryGetBuiltInComponentType(string monoScriptGuid, out Type componentType) {
            if (string.IsNullOrEmpty(monoScriptGuid)) {
                componentType = null;
                return false;
            }

            for (var i = 0; i < BuiltInComponentOptions.Length; i++) {
                var option = BuiltInComponentOptions[i];
                if (option.MonoScriptGuid != monoScriptGuid) {
                    continue;
                }

                componentType = option.ComponentType;
                return true;
            }

            componentType = null;
            return false;
        }

        private static Type GetComponentType(MonoScript monoScript) {
            if (monoScript == null) {
                return null;
            }

            var type = monoScript.GetClass();
            if (type == null || !typeof(MonoBehaviour).IsAssignableFrom(type) || type.IsAbstract || type.IsGenericType) {
                return null;
            }

            return type;
        }

        private static string GetComponentMenuPath(Type componentType) {
            var menuAttribute = Attribute.GetCustomAttribute(componentType, typeof(AddComponentMenu)) as AddComponentMenu;
            if (menuAttribute != null && !string.IsNullOrEmpty(menuAttribute.componentMenu)) {
                return menuAttribute.componentMenu;
            }

            var typePath = componentType.FullName ?? componentType.Name;
            return "Scripts/" + typePath.Replace('.', '/').Replace('+', '/');
        }
    }
}
