using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// AnimationGraph のテスト用 graph builder
    /// </summary>
    internal sealed class AnimationGraphTestBuilder : IDisposable {
        /// <summary>Node の ID フィールド名</summary>
        private const string NodeIdPropertyName = "_nodeId";
        /// <summary>Node のグラフ位置フィールド名</summary>
        private const string GraphPositionPropertyName = "_graphPosition";
        /// <summary>Node の後続ノード ID フィールド名</summary>
        private const string NextNodeIdsPropertyName = "_nextNodeIds";
        /// <summary>BranchNode の false 側後続ノード ID フィールド名</summary>
        private const string FalseNodeIdsPropertyName = "_falseNodeIds";
        /// <summary>RepeatNode の合計実行回数フィールド名</summary>
        private const string RepeatCountPropertyName = "_repeatCount";
        /// <summary>RepeatNode の戻り先ノード ID フィールド名</summary>
        private const string RepeatNodeIdPropertyName = "_repeatNodeId";
        /// <summary>JoinNode の合流方法フィールド名</summary>
        private const string JoinTypePropertyName = "_joinType";
        /// <summary>AnimationGraphAsset のグラフシードフィールド名</summary>
        private const string GraphSeedPropertyName = "_graphSeed";
        /// <summary>AnimationGraphAsset の開始ノード ID フィールド名</summary>
        private const string StartNodeIdPropertyName = "_startNodeId";
        /// <summary>AnimationGraphAsset のノード配列フィールド名</summary>
        private const string NodesPropertyName = "_nodes";

        private readonly List<UnityEngine.Object> _objects = new();

        /// <summary>
        /// AnimationGraphAsset を生成
        /// </summary>
        /// <param name="startNodeId">開始ノード ID</param>
        /// <param name="nodes">graph に含めるノード一覧</param>
        /// <returns>生成した AnimationGraphAsset</returns>
        public AnimationGraphAsset CreateGraph(string startNodeId, params Node[] nodes) {
            var graphAsset = ScriptableObject.CreateInstance<AnimationGraphAsset>();
            graphAsset.name = "TestAnimationGraphAsset";
            _objects.Add(graphAsset);

            var serializedGraph = new SerializedObject(graphAsset);
            serializedGraph.FindProperty(GraphSeedPropertyName).intValue = 12345;
            serializedGraph.FindProperty(StartNodeIdPropertyName).stringValue = startNodeId;
            var nodesProperty = serializedGraph.FindProperty(NodesPropertyName);
            nodesProperty.arraySize = nodes.Length;
            for (var i = 0; i < nodes.Length; i++) {
                nodesProperty.GetArrayElementAtIndex(i).objectReferenceValue = nodes[i];
            }

            serializedGraph.ApplyModifiedPropertiesWithoutUndo();
            return graphAsset;
        }

        /// <summary>
        /// 指定型のノードを生成
        /// </summary>
        /// <param name="nodeId">ノード ID</param>
        /// <param name="nextNodeIds">後続ノード ID 一覧</param>
        /// <typeparam name="T">生成するノード型</typeparam>
        /// <returns>生成したノード</returns>
        public T CreateNode<T>(string nodeId, params string[] nextNodeIds) where T : Node {
            var node = ScriptableObject.CreateInstance<T>();
            node.name = typeof(T).Name;
            _objects.Add(node);
            SetNodeState(node, nodeId, nextNodeIds);
            return node;
        }

        /// <summary>
        /// StartNode を生成
        /// </summary>
        /// <param name="nodeId">ノード ID</param>
        /// <param name="nextNodeIds">後続ノード ID 一覧</param>
        /// <returns>生成した StartNode</returns>
        public StartNode CreateStartNode(string nodeId, params string[] nextNodeIds) {
            return CreateNode<StartNode>(nodeId, nextNodeIds);
        }

        /// <summary>
        /// テスト用 action node を生成
        /// </summary>
        /// <param name="nodeId">ノード ID</param>
        /// <param name="duration">実行時間</param>
        /// <param name="nextNodeIds">後続ノード ID 一覧</param>
        /// <returns>生成した action node</returns>
        public TestActionNode CreateActionNode(string nodeId, float duration, params string[] nextNodeIds) {
            var node = CreateNode<TestActionNode>(nodeId, nextNodeIds);
            node.Configure(duration);
            return node;
        }

        /// <summary>
        /// BranchNode の false 側接続を設定
        /// </summary>
        /// <param name="branchNode">設定対象 BranchNode</param>
        /// <param name="falseNodeIds">false 側後続ノード ID 一覧</param>
        public void SetFalseNodeIds(BranchNode branchNode, params string[] falseNodeIds) {
            var serializedNode = new SerializedObject(branchNode);
            SetStringArray(serializedNode.FindProperty(FalseNodeIdsPropertyName), falseNodeIds);
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// RepeatNode の設定を更新
        /// </summary>
        /// <param name="repeatNode">設定対象 RepeatNode</param>
        /// <param name="repeatCount">合計実行回数</param>
        /// <param name="repeatNodeId">戻り先ノード ID</param>
        public void SetRepeat(RepeatNode repeatNode, int repeatCount, string repeatNodeId) {
            var serializedNode = new SerializedObject(repeatNode);
            serializedNode.FindProperty(RepeatCountPropertyName).intValue = repeatCount;
            serializedNode.FindProperty(RepeatNodeIdPropertyName).stringValue = repeatNodeId;
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// JoinNode の合流方法を設定
        /// </summary>
        /// <param name="joinNode">設定対象 JoinNode</param>
        /// <param name="joinType">合流方法</param>
        public void SetJoinType(JoinNode joinNode, JoinType joinType) {
            var serializedNode = new SerializedObject(joinNode);
            serializedNode.FindProperty(JoinTypePropertyName).enumValueIndex = (int)joinType;
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <inheritdoc/>
        public void Dispose() {
            for (var i = _objects.Count - 1; i >= 0; i--) {
                UnityEngine.Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
        }

        /// <summary>
        /// ノードの共通シリアライズ状態を設定
        /// </summary>
        /// <param name="node">設定対象ノード</param>
        /// <param name="nodeId">ノード ID</param>
        /// <param name="nextNodeIds">後続ノード ID 一覧</param>
        private void SetNodeState(Node node, string nodeId, IReadOnlyList<string> nextNodeIds) {
            var serializedNode = new SerializedObject(node);
            serializedNode.FindProperty(NodeIdPropertyName).stringValue = nodeId;
            serializedNode.FindProperty(GraphPositionPropertyName).vector2Value = Vector2.zero;
            SetStringArray(serializedNode.FindProperty(NextNodeIdsPropertyName), nextNodeIds);
            if (node is BranchNode) {
                SetStringArray(serializedNode.FindProperty(FalseNodeIdsPropertyName), Array.Empty<string>());
            }

            if (node is RepeatNode) {
                serializedNode.FindProperty(RepeatNodeIdPropertyName).stringValue = string.Empty;
            }

            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// SerializedProperty の string 配列を設定
        /// </summary>
        /// <param name="property">設定対象 SerializedProperty</param>
        /// <param name="values">設定する値一覧</param>
        private void SetStringArray(SerializedProperty property, IReadOnlyList<string> values) {
            property.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++) {
                property.GetArrayElementAtIndex(i).stringValue = values[i];
            }
        }
    }

    /// <summary>
    /// テスト用 AnimationGraphContext
    /// </summary>
    internal sealed class TestAnimationGraphContext : IAnimationGraphContext {
        private readonly Dictionary<string, object> _blackboardValues = new(StringComparer.Ordinal);

        /// <summary>
        /// Blackboard 値を設定
        /// </summary>
        /// <param name="key">Blackboard キー</param>
        /// <param name="value">設定する値</param>
        /// <typeparam name="T">設定する値の型</typeparam>
        public void SetBlackboardValue<T>(string key, T value) {
            _blackboardValues[key] = value;
        }

        /// <inheritdoc/>
        public T GetTarget<T>(string key) where T : Component {
            throw new InvalidOperationException($"Target '{key}' is not registered");
        }

        /// <inheritdoc/>
        public bool TryGetTarget<T>(string key, out T target) where T : Component {
            target = null;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetBlackboardValue<T>(string key, out T value) {
            if (_blackboardValues.TryGetValue(key, out var objectValue) && objectValue is T typedValue) {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }
    }

    /// <summary>
    /// テスト用 action node
    /// </summary>
    internal sealed class TestActionNode : ActionNode {
        private float _duration;
        private float _delay;

        /// <summary>Evaluate が呼ばれた回数</summary>
        public int EvaluateCount { get; private set; }
        /// <summary>Cancel が呼ばれた回数</summary>
        public int CancelCount { get; private set; }
        /// <summary>最後に渡された localTime</summary>
        public float LastLocalTime { get; private set; }
        /// <summary>最後に渡された duration</summary>
        public float LastDuration { get; private set; }
        /// <summary>最後に渡された seed</summary>
        public int LastSeed { get; private set; }
        /// <summary>最後に Cancel に渡された seed</summary>
        public int LastCancelSeed { get; private set; }

        /// <summary>
        /// テスト用の時間設定を更新
        /// </summary>
        /// <param name="duration">実行時間</param>
        /// <param name="delay">開始遅延</param>
        public void Configure(float duration, float delay = 0.0f) {
            _duration = duration;
            _delay = delay;
        }

        /// <inheritdoc/>
        protected override float CalculateDuration(int seed, IAnimationGraphContext context) {
            return _duration;
        }

        /// <inheritdoc/>
        protected override float CalculateDelay(int seed, IAnimationGraphContext context) {
            return _delay;
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context) {
            EvaluateCount++;
            LastSeed = seed;
            LastLocalTime = localTime;
            LastDuration = calculatedDuration;
        }

        /// <inheritdoc/>
        protected override void Cancel(int seed, IAnimationGraphContext context) {
            CancelCount++;
            LastCancelSeed = seed;
        }
    }

    /// <summary>
    /// テスト用 branch node
    /// </summary>
    internal sealed class TestBranchNode : BranchNode {
        /// <summary>分岐条件の戻り値</summary>
        public bool Condition { get; set; }

        /// <inheritdoc/>
        protected override bool EvaluateConditionInternal(int seed, IAnimationGraphContext context) {
            return Condition;
        }
    }
}
