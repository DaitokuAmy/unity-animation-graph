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
        /// <summary>Node の Signal Port 設定フィールド名</summary>
        private const string SignalPortsPropertyName = "_signalPorts";
        /// <summary>NodeSignalPortSettings の Enter シグナル Port 表示フラグフィールド名</summary>
        private const string SignalPortsEnterEnabledPropertyName = "_enterEnabled";
        /// <summary>NodeSignalPortSettings の Exit シグナル Port 表示フラグフィールド名</summary>
        private const string SignalPortsExitEnabledPropertyName = "_exitEnabled";
        /// <summary>Node の Signal Port 設定バージョンフィールド名</summary>
        private const string SignalPortSettingsVersionPropertyName = "_signalPortSettingsVersion";
        /// <summary>旧 Node の Enter シグナル Port 表示フラグフィールド名</summary>
        private const string LegacyEnableEnterSignalPortPropertyName = "_enableEnterSignalPort";
        /// <summary>旧 Node の Exit シグナル Port 表示フラグフィールド名</summary>
        private const string LegacyEnableExitSignalPortPropertyName = "_enableExitSignalPort";
        /// <summary>Node の Enter シグナルフィールド名</summary>
        private const string EnterSignalsPropertyName = "_enterSignals";
        /// <summary>Node の Exit シグナルフィールド名</summary>
        private const string ExitSignalsPropertyName = "_exitSignals";
        /// <summary>Signal の ID フィールド名</summary>
        private const string SignalIdPropertyName = "_signalId";
        /// <summary>Signal のグラフ位置フィールド名</summary>
        private const string SignalGraphPositionPropertyName = "_graphPosition";
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

            SaveGraphAssetIfDirty(graphAsset);

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

            SaveGraphAssetIfDirty(graphAsset);

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
        /// Signal のエディタ上の位置を設定
        /// </summary>
        /// <param name="signal">設定対象の Signal</param>
        /// <param name="graphPosition">エディタ上のシグナル位置</param>
        public static void SetSignalGraphPosition(Signal signal, Vector2 graphPosition) {
            if (signal == null) {
                throw new ArgumentNullException(nameof(signal));
            }

            var undoName = "Set Animation Graph Signal Position";
            Undo.RecordObject(signal, undoName);

            var serializedSignal = new SerializedObject(signal);
            serializedSignal.FindProperty(SignalGraphPositionPropertyName).vector2Value = graphPosition;
            serializedSignal.ApplyModifiedProperties();
            EditorUtility.SetDirty(signal);
        }

        /// <summary>
        /// Node の Enter シグナル Port 表示フラグを設定
        /// </summary>
        /// <param name="node">設定対象の Node</param>
        /// <param name="enabled">表示する場合は true</param>
        public static void SetNodeEnterSignalPortEnabled(Node node, bool enabled) {
            SetNodeSignalPortEnabled(node, SignalPortsEnterEnabledPropertyName, EnterSignalsPropertyName, enabled, "Set Animation Graph Enter Signal Port");
        }

        /// <summary>
        /// Node の Exit シグナル Port 表示フラグを設定
        /// </summary>
        /// <param name="node">設定対象の Node</param>
        /// <param name="enabled">表示する場合は true</param>
        public static void SetNodeExitSignalPortEnabled(Node node, bool enabled) {
            SetNodeSignalPortEnabled(node, SignalPortsExitEnabledPropertyName, ExitSignalsPropertyName, enabled, "Set Animation Graph Exit Signal Port");
        }

        /// <summary>
        /// Node の Enter シグナル一覧に Signal を追加
        /// </summary>
        /// <param name="graphAsset">追加先の AnimationGraphAsset</param>
        /// <param name="node">追加先の Node</param>
        /// <param name="signalType">追加する Signal 型</param>
        /// <returns>追加した Signal</returns>
        public static Signal AddEnterSignal(AnimationGraphAsset graphAsset, Node node, Type signalType) {
            return AddEnterSignal(graphAsset, node, signalType, Vector2.zero);
        }

        /// <summary>
        /// Node の Enter シグナル一覧に Signal を追加
        /// </summary>
        /// <param name="graphAsset">追加先の AnimationGraphAsset</param>
        /// <param name="node">追加先の Node</param>
        /// <param name="signalType">追加する Signal 型</param>
        /// <param name="graphPosition">エディタ上の Signal 位置</param>
        /// <returns>追加した Signal</returns>
        public static Signal AddEnterSignal(AnimationGraphAsset graphAsset, Node node, Type signalType, Vector2 graphPosition) {
            return AddSignal(graphAsset, node, signalType, graphPosition, EnterSignalsPropertyName);
        }

        /// <summary>
        /// Node の Exit シグナル一覧に Signal を追加
        /// </summary>
        /// <param name="graphAsset">追加先の AnimationGraphAsset</param>
        /// <param name="node">追加先の Node</param>
        /// <param name="signalType">追加する Signal 型</param>
        /// <returns>追加した Signal</returns>
        public static Signal AddExitSignal(AnimationGraphAsset graphAsset, Node node, Type signalType) {
            return AddExitSignal(graphAsset, node, signalType, Vector2.zero);
        }

        /// <summary>
        /// Node の Exit シグナル一覧に Signal を追加
        /// </summary>
        /// <param name="graphAsset">追加先の AnimationGraphAsset</param>
        /// <param name="node">追加先の Node</param>
        /// <param name="signalType">追加する Signal 型</param>
        /// <param name="graphPosition">エディタ上の Signal 位置</param>
        /// <returns>追加した Signal</returns>
        public static Signal AddExitSignal(AnimationGraphAsset graphAsset, Node node, Type signalType, Vector2 graphPosition) {
            return AddSignal(graphAsset, node, signalType, graphPosition, ExitSignalsPropertyName);
        }

        /// <summary>
        /// AnimationGraphAsset に未接続の Signal を追加
        /// </summary>
        /// <param name="graphAsset">追加先の AnimationGraphAsset</param>
        /// <param name="signalType">追加する Signal 型</param>
        /// <param name="graphPosition">エディタ上の Signal 位置</param>
        /// <returns>追加した Signal</returns>
        public static Signal AddSignal(AnimationGraphAsset graphAsset, Type signalType, Vector2 graphPosition) {
            if (graphAsset == null) {
                throw new ArgumentNullException(nameof(graphAsset));
            }

            ValidateSignalType(signalType);

            var signal = (Signal)ScriptableObject.CreateInstance(signalType);
            signal.name = signalType.Name;

            var undoName = $"Add {signalType.Name}";
            Undo.RegisterCreatedObjectUndo(signal, undoName);
            Undo.RecordObject(graphAsset, undoName);

            SetupSignal(signal, GenerateSignalId(graphAsset), graphPosition);
            AssetDatabase.AddObjectToAsset(signal, graphAsset);

            EditorUtility.SetDirty(signal);
            EditorUtility.SetDirty(graphAsset);

            SaveGraphAssetIfDirty(graphAsset);

            return signal;
        }

        /// <summary>
        /// AnimationGraphAsset に Signal を複製して追加
        /// </summary>
        /// <param name="graphAsset">追加先の AnimationGraphAsset</param>
        /// <param name="sourceSignal">複製元の Signal</param>
        /// <param name="graphPosition">エディタ上の Signal 位置</param>
        /// <returns>複製した Signal</returns>
        public static Signal DuplicateSignal(AnimationGraphAsset graphAsset, Signal sourceSignal, Vector2 graphPosition) {
            if (graphAsset == null) {
                throw new ArgumentNullException(nameof(graphAsset));
            }

            if (sourceSignal == null) {
                throw new ArgumentNullException(nameof(sourceSignal));
            }

            var signalType = sourceSignal.GetType();
            var signal = (Signal)ScriptableObject.CreateInstance(signalType);
            signal.name = sourceSignal.name;

            var undoName = $"Duplicate {signalType.Name}";
            Undo.RegisterCreatedObjectUndo(signal, undoName);
            Undo.RecordObject(graphAsset, undoName);

            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(sourceSignal), signal);
            signal.name = sourceSignal.name;

            SetupSignal(signal, GenerateSignalId(graphAsset), graphPosition);
            AssetDatabase.AddObjectToAsset(signal, graphAsset);

            EditorUtility.SetDirty(signal);
            EditorUtility.SetDirty(graphAsset);

            SaveGraphAssetIfDirty(graphAsset);

            return signal;
        }

        /// <summary>
        /// AnimationGraphAsset に含まれる Signal sub asset 一覧を取得
        /// </summary>
        /// <param name="graphAsset">取得対象の AnimationGraphAsset</param>
        /// <returns>Signal sub asset 一覧</returns>
        public static IReadOnlyList<Signal> GetSignals(AnimationGraphAsset graphAsset) {
            if (graphAsset == null) {
                throw new ArgumentNullException(nameof(graphAsset));
            }

            var assetPath = AssetDatabase.GetAssetPath(graphAsset);
            if (string.IsNullOrEmpty(assetPath)) {
                return Array.Empty<Signal>();
            }

            var signals = new List<Signal>();
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (var i = 0; i < assets.Length; i++) {
                if (assets[i] is Signal signal) {
                    signals.Add(signal);
                }
            }

            return signals;
        }

        /// <summary>
        /// Node の Enter シグナル一覧に Signal 参照を追加
        /// </summary>
        /// <param name="node">追加先の Node</param>
        /// <param name="signal">追加する Signal</param>
        public static void AddEnterSignalReference(Node node, Signal signal) {
            AddSignalReference(node, signal, EnterSignalsPropertyName, "Connect Animation Graph Enter Signal");
        }

        /// <summary>
        /// Node の Exit シグナル一覧に Signal 参照を追加
        /// </summary>
        /// <param name="node">追加先の Node</param>
        /// <param name="signal">追加する Signal</param>
        public static void AddExitSignalReference(Node node, Signal signal) {
            AddSignalReference(node, signal, ExitSignalsPropertyName, "Connect Animation Graph Exit Signal");
        }

        /// <summary>
        /// Node の Enter シグナル一覧から Signal 参照を削除
        /// </summary>
        /// <param name="node">削除元の Node</param>
        /// <param name="signal">削除する Signal</param>
        /// <returns>削除した場合は true</returns>
        public static bool RemoveEnterSignalReference(Node node, Signal signal) {
            return RemoveSignalReference(node, signal, EnterSignalsPropertyName, "Disconnect Animation Graph Enter Signal");
        }

        /// <summary>
        /// Node の Exit シグナル一覧から Signal 参照を削除
        /// </summary>
        /// <param name="node">削除元の Node</param>
        /// <param name="signal">削除する Signal</param>
        /// <returns>削除した場合は true</returns>
        public static bool RemoveExitSignalReference(Node node, Signal signal) {
            return RemoveSignalReference(node, signal, ExitSignalsPropertyName, "Disconnect Animation Graph Exit Signal");
        }

        /// <summary>
        /// AnimationGraphAsset から Signal を削除
        /// </summary>
        /// <param name="graphAsset">削除元の AnimationGraphAsset</param>
        /// <param name="signal">削除する Signal</param>
        public static void RemoveSignal(AnimationGraphAsset graphAsset, Signal signal) {
            if (graphAsset == null) {
                throw new ArgumentNullException(nameof(graphAsset));
            }

            if (signal == null) {
                throw new ArgumentNullException(nameof(signal));
            }

            var undoName = $"Remove {signal.name}";
            Undo.RecordObject(graphAsset, undoName);
            RemoveSignalReferences(graphAsset, signal, undoName);
            Undo.DestroyObjectImmediate(signal);
            EditorUtility.SetDirty(graphAsset);

            SaveGraphAssetIfDirty(graphAsset);
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

            var nodeSignals = CollectSignals(node.EnterSignals, node.ExitSignals);

            var serializedGraph = new SerializedObject(graphAsset);
            var nodesProperty = serializedGraph.FindProperty(NodesPropertyName);
            nodesProperty.GetArrayElementAtIndex(nodeIndex).objectReferenceValue = null;
            nodesProperty.DeleteArrayElementAtIndex(nodeIndex);
            serializedGraph.ApplyModifiedProperties();

            DestroySignals(graphAsset, nodeSignals, undoName);
            Undo.DestroyObjectImmediate(node);
            EditorUtility.SetDirty(graphAsset);

            SaveGraphAssetIfDirty(graphAsset);
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
        /// AnimationGraphAsset 内で一意なシグナル ID を生成
        /// </summary>
        /// <param name="graphAsset">生成先の AnimationGraphAsset</param>
        /// <returns>一意なシグナル ID</returns>
        private static string GenerateSignalId(AnimationGraphAsset graphAsset) {
            string signalId;
            do {
                signalId = Guid.NewGuid().ToString("N");
            }
            while (ContainsSignalId(graphAsset, signalId));

            return signalId;
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
            serializedNode.FindProperty(EnterSignalsPropertyName).arraySize = 0;
            serializedNode.FindProperty(ExitSignalsPropertyName).arraySize = 0;
            if (node is BranchNode) {
                serializedNode.FindProperty(FalseNodeIdsPropertyName).arraySize = 0;
            }

            if (node is LoopNode) {
                serializedNode.FindProperty(LoopNodeIdsPropertyName).arraySize = 0;
            }

            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 追加直後の Signal 情報を設定
        /// </summary>
        /// <param name="signal">設定対象の Signal</param>
        /// <param name="signalId">設定する Signal ID</param>
        private static void SetupSignal(Signal signal, string signalId, Vector2 graphPosition) {
            var serializedSignal = new SerializedObject(signal);
            serializedSignal.FindProperty(SignalIdPropertyName).stringValue = signalId;
            serializedSignal.FindProperty(SignalGraphPositionPropertyName).vector2Value = graphPosition;
            serializedSignal.ApplyModifiedPropertiesWithoutUndo();
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
        /// AnimationGraphAsset の dirty 状態を保存
        /// </summary>
        /// <param name="graphAsset">保存対象の AnimationGraphAsset</param>
        private static void SaveGraphAssetIfDirty(AnimationGraphAsset graphAsset) {
            var assetPath = AssetDatabase.GetAssetPath(graphAsset);
            if (string.IsNullOrEmpty(assetPath)) {
                return;
            }

            AssetDatabase.SaveAssetIfDirty(graphAsset);
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

        /// <summary>
        /// Node の Signal Port 表示フラグを設定
        /// </summary>
        /// <param name="node">設定対象の Node</param>
        /// <param name="propertyName">設定対象フィールド名</param>
        /// <param name="signalsPropertyName">無効化時に解除する Signal 配列フィールド名</param>
        /// <param name="enabled">表示する場合は true</param>
        /// <param name="undoName">Undo 名</param>
        private static void SetNodeSignalPortEnabled(Node node, string propertyName, string signalsPropertyName, bool enabled, string undoName) {
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }

            Undo.RecordObject(node, undoName);

            var serializedNode = new SerializedObject(node);
            MigrateNodeSignalPortSettings(serializedNode);
            var signalPortsProperty = serializedNode.FindProperty(SignalPortsPropertyName);
            signalPortsProperty.FindPropertyRelative(propertyName).boolValue = enabled;
            if (!enabled) {
                ClearNodeSignalReferences(serializedNode, signalsPropertyName);
            }

            ClearLegacyNodeSignalPortSettings(serializedNode);
            serializedNode.FindProperty(SignalPortSettingsVersionPropertyName).intValue = 1;
            serializedNode.ApplyModifiedProperties();
            EditorUtility.SetDirty(node);
        }

        private static void MigrateNodeSignalPortSettings(SerializedObject serializedNode) {
            var versionProperty = serializedNode.FindProperty(SignalPortSettingsVersionPropertyName);
            if (versionProperty == null) {
                return;
            }

            var legacyEnterProperty = serializedNode.FindProperty(LegacyEnableEnterSignalPortPropertyName);
            var legacyExitProperty = serializedNode.FindProperty(LegacyEnableExitSignalPortPropertyName);
            if (versionProperty.intValue != 0 && legacyEnterProperty?.boolValue != true && legacyExitProperty?.boolValue != true) {
                return;
            }

            var signalPortsProperty = serializedNode.FindProperty(SignalPortsPropertyName);
            var enterProperty = signalPortsProperty.FindPropertyRelative(SignalPortsEnterEnabledPropertyName);
            var exitProperty = signalPortsProperty.FindPropertyRelative(SignalPortsExitEnabledPropertyName);
            enterProperty.boolValue |= legacyEnterProperty?.boolValue == true;
            exitProperty.boolValue |= legacyExitProperty?.boolValue == true;
            versionProperty.intValue = 1;
        }

        private static void ClearLegacyNodeSignalPortSettings(SerializedObject serializedNode) {
            var legacyEnterProperty = serializedNode.FindProperty(LegacyEnableEnterSignalPortPropertyName);
            var legacyExitProperty = serializedNode.FindProperty(LegacyEnableExitSignalPortPropertyName);
            if (legacyEnterProperty != null) {
                legacyEnterProperty.boolValue = false;
            }

            if (legacyExitProperty != null) {
                legacyExitProperty.boolValue = false;
            }
        }

        private static void ClearNodeSignalReferences(SerializedObject serializedNode, string signalsPropertyName) {
            var signalsProperty = serializedNode.FindProperty(signalsPropertyName);
            if (signalsProperty == null) {
                return;
            }

            signalsProperty.arraySize = 0;
        }

        /// <summary>
        /// Node の Signal 一覧に Signal を追加
        /// </summary>
        /// <param name="graphAsset">追加先の AnimationGraphAsset</param>
        /// <param name="node">追加先の Node</param>
        /// <param name="signalType">追加する Signal 型</param>
        /// <param name="signalsPropertyName">追加先 Signal 配列フィールド名</param>
        /// <returns>追加した Signal</returns>
        private static Signal AddSignal(AnimationGraphAsset graphAsset, Node node, Type signalType, Vector2 graphPosition, string signalsPropertyName) {
            if (graphAsset == null) {
                throw new ArgumentNullException(nameof(graphAsset));
            }

            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }

            ValidateSignalType(signalType);

            if (!ContainsNode(graphAsset, node)) {
                throw new InvalidOperationException("Node is not contained in AnimationGraphAsset");
            }

            var signal = (Signal)ScriptableObject.CreateInstance(signalType);
            signal.name = signalType.Name;

            var undoName = $"Add {signalType.Name}";
            Undo.RegisterCreatedObjectUndo(signal, undoName);
            Undo.RecordObject(graphAsset, undoName);
            Undo.RecordObject(node, undoName);

            SetupSignal(signal, GenerateSignalId(graphAsset), graphPosition);
            AssetDatabase.AddObjectToAsset(signal, graphAsset);
            AddSignalReference(node, signal, signalsPropertyName, undoName);

            EditorUtility.SetDirty(signal);
            EditorUtility.SetDirty(node);
            EditorUtility.SetDirty(graphAsset);

            SaveGraphAssetIfDirty(graphAsset);

            return signal;
        }

        /// <summary>
        /// Node の Signal 配列に Signal 参照を追加
        /// </summary>
        /// <param name="node">追加先の Node</param>
        /// <param name="signal">追加する Signal</param>
        /// <param name="signalsPropertyName">追加先 Signal 配列フィールド名</param>
        private static void AddSignalReference(Node node, Signal signal, string signalsPropertyName, string undoName) {
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }

            if (signal == null) {
                throw new ArgumentNullException(nameof(signal));
            }

            Undo.RecordObject(node, undoName);
            var serializedNode = new SerializedObject(node);
            var signalsProperty = serializedNode.FindProperty(signalsPropertyName);
            for (var i = 0; i < signalsProperty.arraySize; i++) {
                if (signalsProperty.GetArrayElementAtIndex(i).objectReferenceValue != signal) {
                    continue;
                }

                return;
            }

            var index = signalsProperty.arraySize;
            signalsProperty.arraySize++;
            signalsProperty.GetArrayElementAtIndex(index).objectReferenceValue = signal;
            serializedNode.ApplyModifiedProperties();
            EditorUtility.SetDirty(node);
        }

        private static void ValidateSignalType(Type signalType) {
            if (signalType == null) {
                throw new ArgumentNullException(nameof(signalType));
            }

            if (!typeof(Signal).IsAssignableFrom(signalType)) {
                throw new ArgumentException($"{signalType.FullName} does not derive from Signal", nameof(signalType));
            }

            if (signalType.IsAbstract) {
                throw new ArgumentException($"{signalType.FullName} is abstract", nameof(signalType));
            }
        }

        private static IReadOnlyList<Signal> CollectSignals(IReadOnlyList<Signal> firstSignals, IReadOnlyList<Signal> secondSignals) {
            var signals = new List<Signal>();
            AddSignals(firstSignals, signals);
            AddSignals(secondSignals, signals);
            return signals;
        }

        private static void AddSignals(IReadOnlyList<Signal> sourceSignals, List<Signal> destinationSignals) {
            for (var i = 0; i < sourceSignals.Count; i++) {
                var signal = sourceSignals[i];
                if (signal == null || destinationSignals.Contains(signal)) {
                    continue;
                }

                destinationSignals.Add(signal);
            }
        }

        private static void DestroySignals(AnimationGraphAsset graphAsset, IReadOnlyList<Signal> signals, string undoName) {
            for (var i = 0; i < signals.Count; i++) {
                var signal = signals[i];
                if (signal == null) {
                    continue;
                }

                RemoveSignalReferences(graphAsset, signal, undoName);
                Undo.DestroyObjectImmediate(signal);
            }
        }

        private static void RemoveSignalReferences(AnimationGraphAsset graphAsset, Signal signal, string undoName) {
            var nodes = graphAsset.Nodes;
            for (var i = 0; i < nodes.Count; i++) {
                var node = nodes[i];
                if (node == null) {
                    continue;
                }

                var removed = RemoveSignalReference(node, signal, EnterSignalsPropertyName, undoName);
                removed |= RemoveSignalReference(node, signal, ExitSignalsPropertyName, undoName);
                if (removed) {
                    EditorUtility.SetDirty(node);
                }
            }
        }

        private static bool RemoveSignalReference(Node node, Signal signal, string signalsPropertyName, string undoName) {
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }

            if (signal == null) {
                throw new ArgumentNullException(nameof(signal));
            }

            var serializedNode = new SerializedObject(node);
            var signalsProperty = serializedNode.FindProperty(signalsPropertyName);
            var removed = false;
            for (var i = signalsProperty.arraySize - 1; i >= 0; i--) {
                if (signalsProperty.GetArrayElementAtIndex(i).objectReferenceValue != signal) {
                    continue;
                }

                if (!removed) {
                    Undo.RecordObject(node, undoName);
                }

                signalsProperty.GetArrayElementAtIndex(i).objectReferenceValue = null;
                signalsProperty.DeleteArrayElementAtIndex(i);
                removed = true;
            }

            if (removed) {
                serializedNode.ApplyModifiedProperties();
            }

            return removed;
        }

        /// <summary>
        /// AnimationGraphAsset に Node が含まれるかを判定
        /// </summary>
        /// <param name="graphAsset">判定対象の AnimationGraphAsset</param>
        /// <param name="node">判定する Node</param>
        /// <returns>含まれる場合は true</returns>
        private static bool ContainsNode(AnimationGraphAsset graphAsset, Node node) {
            var nodes = graphAsset.Nodes;
            for (var i = 0; i < nodes.Count; i++) {
                if (nodes[i] == node) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 指定した Signal ID が AnimationGraphAsset 内で使用済みかを判定
        /// </summary>
        /// <param name="graphAsset">判定対象の AnimationGraphAsset</param>
        /// <param name="signalId">判定する Signal ID</param>
        /// <returns>使用済みの場合は true</returns>
        private static bool ContainsSignalId(AnimationGraphAsset graphAsset, string signalId) {
            var nodes = graphAsset.Nodes;
            for (var i = 0; i < nodes.Count; i++) {
                var node = nodes[i];
                if (node == null) {
                    continue;
                }

                if (ContainsSignalId(node.EnterSignals, signalId) || ContainsSignalId(node.ExitSignals, signalId)) {
                    return true;
                }
            }

            var assetPath = AssetDatabase.GetAssetPath(graphAsset);
            if (string.IsNullOrEmpty(assetPath)) {
                return false;
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (var i = 0; i < assets.Length; i++) {
                if (assets[i] is Signal signal && signal.SignalId == signalId) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 指定した Signal 一覧に Signal ID が含まれるかを判定
        /// </summary>
        /// <param name="signals">判定対象の Signal 一覧</param>
        /// <param name="signalId">判定する Signal ID</param>
        /// <returns>含まれる場合は true</returns>
        private static bool ContainsSignalId(IReadOnlyList<Signal> signals, string signalId) {
            for (var i = 0; i < signals.Count; i++) {
                var signal = signals[i];
                if (signal != null && signal.SignalId == signalId) {
                    return true;
                }
            }

            return false;
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
