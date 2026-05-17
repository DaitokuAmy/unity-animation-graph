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
        /// <summary>Node の Enter シグナルフィールド名</summary>
        private const string EnterSignalsPropertyName = "_enterSignals";
        /// <summary>Node の Exit シグナルフィールド名</summary>
        private const string ExitSignalsPropertyName = "_exitSignals";
        /// <summary>Signal の ID フィールド名</summary>
        private const string SignalIdPropertyName = "_signalId";
        /// <summary>BranchNode の false 側後続ノード ID フィールド名</summary>
        private const string FalseNodeIdsPropertyName = "_falseNodeIds";
        /// <summary>DelayNode の待機時間フィールド名</summary>
        private const string DelayPropertyName = "_delay";
        /// <summary>LoopNode の実行回数フィールド名</summary>
        private const string LoopCountPropertyName = "_loopCount";
        /// <summary>LoopNode のループ内容ノード ID フィールド名</summary>
        private const string LoopNodeIdsPropertyName = "_loopNodeIds";
        /// <summary>JoinNode の合流方法フィールド名</summary>
        private const string JoinTypePropertyName = "_joinType";
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
        /// <summary>Blackboard 定義の Vector4 default value フィールド名</summary>
        private const string DefaultVector4ValuePropertyName = "_defaultVector4Value";

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
            serializedGraph.FindProperty(AssetGuidPropertyName).stringValue = Guid.NewGuid().ToString("N");
            serializedGraph.FindProperty(GraphSeedPropertyName).intValue = 12345;
            serializedGraph.FindProperty(RandomSeedPropertyName).boolValue = false;
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
        /// AnimationGraphAsset の RandomSeed 設定を更新
        /// </summary>
        /// <param name="graphAsset">設定対象の AnimationGraphAsset</param>
        /// <param name="randomSeed">RandomSeed を有効にする場合は true</param>
        public void SetRandomSeed(AnimationGraphAsset graphAsset, bool randomSeed) {
            var serializedGraph = new SerializedObject(graphAsset);
            serializedGraph.FindProperty(RandomSeedPropertyName).boolValue = randomSeed;
            serializedGraph.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// AnimationGraphAsset の Node 一覧を設定
        /// </summary>
        /// <param name="graphAsset">設定対象の AnimationGraphAsset</param>
        /// <param name="nodes">設定する node 一覧</param>
        public void SetNodes(AnimationGraphAsset graphAsset, params Node[] nodes) {
            var serializedGraph = new SerializedObject(graphAsset);
            var nodesProperty = serializedGraph.FindProperty(NodesPropertyName);
            nodesProperty.arraySize = nodes.Length;
            for (var i = 0; i < nodes.Length; i++) {
                nodesProperty.GetArrayElementAtIndex(i).objectReferenceValue = nodes[i];
            }

            serializedGraph.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// AnimationGraphAsset の target 定義を設定
        /// </summary>
        /// <param name="graphAsset">設定対象の AnimationGraphAsset</param>
        /// <param name="keys">設定する target key 一覧</param>
        public void SetTargetDefinitions(AnimationGraphAsset graphAsset, params string[] keys) {
            var serializedGraph = new SerializedObject(graphAsset);
            var definitionsProperty = serializedGraph.FindProperty(TargetDefinitionsPropertyName);
            definitionsProperty.arraySize = keys.Length;
            for (var i = 0; i < keys.Length; i++) {
                var definitionProperty = definitionsProperty.GetArrayElementAtIndex(i);
                definitionProperty.FindPropertyRelative(KeyPropertyName).stringValue = keys[i];
                definitionProperty.FindPropertyRelative(MonoScriptGuidPropertyName).stringValue = string.Empty;
            }

            serializedGraph.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// AnimationGraphAsset の target 定義を設定
        /// </summary>
        /// <param name="graphAsset">設定対象の AnimationGraphAsset</param>
        /// <param name="definitions">設定する target 定義一覧</param>
        public void SetTargetDefinitions(AnimationGraphAsset graphAsset, params AnimationGraphTargetDefinition[] definitions) {
            var serializedGraph = new SerializedObject(graphAsset);
            var definitionsProperty = serializedGraph.FindProperty(TargetDefinitionsPropertyName);
            definitionsProperty.arraySize = definitions.Length;
            for (var i = 0; i < definitions.Length; i++) {
                var definitionProperty = definitionsProperty.GetArrayElementAtIndex(i);
                definitionProperty.FindPropertyRelative(KeyPropertyName).stringValue = definitions[i].Key;
                definitionProperty.FindPropertyRelative(MonoScriptGuidPropertyName).stringValue = definitions[i].MonoScriptGuid;
            }

            serializedGraph.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// AnimationGraphAsset の Blackboard 定義を設定
        /// </summary>
        /// <param name="graphAsset">設定対象の AnimationGraphAsset</param>
        /// <param name="definitions">設定する Blackboard 定義一覧</param>
        public void SetBlackboardDefinitions(AnimationGraphAsset graphAsset, params AnimationGraphBlackboardDefinition[] definitions) {
            var serializedGraph = new SerializedObject(graphAsset);
            var definitionsProperty = serializedGraph.FindProperty(BlackboardDefinitionsPropertyName);
            definitionsProperty.arraySize = definitions.Length;
            for (var i = 0; i < definitions.Length; i++) {
                var definitionProperty = definitionsProperty.GetArrayElementAtIndex(i);
                definitionProperty.FindPropertyRelative(KeyPropertyName).stringValue = definitions[i].Key;
                definitionProperty.FindPropertyRelative(ValueTypePropertyName).enumValueIndex = (int)definitions[i].ValueType;
                definitionProperty.FindPropertyRelative(DefaultBoolValuePropertyName).boolValue = definitions[i].DefaultBoolValue;
                definitionProperty.FindPropertyRelative(DefaultIntValuePropertyName).intValue = definitions[i].DefaultIntValue;
                definitionProperty.FindPropertyRelative(DefaultFloatValuePropertyName).floatValue = definitions[i].DefaultFloatValue;
                definitionProperty.FindPropertyRelative(DefaultStringValuePropertyName).stringValue = definitions[i].DefaultStringValue;
                definitionProperty.FindPropertyRelative(DefaultVector2ValuePropertyName).vector2Value = definitions[i].DefaultVector2Value;
                definitionProperty.FindPropertyRelative(DefaultVector3ValuePropertyName).vector3Value = definitions[i].DefaultVector3Value;
                definitionProperty.FindPropertyRelative(DefaultColorValuePropertyName).colorValue = definitions[i].DefaultColorValue;
                definitionProperty.FindPropertyRelative(DefaultVector4ValuePropertyName).vector4Value = definitions[i].DefaultVector4Value;
            }

            serializedGraph.ApplyModifiedPropertiesWithoutUndo();
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
        /// テスト用 signal を生成
        /// </summary>
        /// <param name="signalId">シグナル ID</param>
        /// <param name="eventName">記録するイベント名</param>
        /// <param name="events">イベント記録先</param>
        /// <returns>生成した signal</returns>
        public TestSignal CreateSignal(string signalId, string eventName = null, IList<string> events = null) {
            var signal = ScriptableObject.CreateInstance<TestSignal>();
            signal.name = nameof(TestSignal);
            signal.Configure(eventName, events);
            _objects.Add(signal);

            var serializedSignal = new SerializedObject(signal);
            serializedSignal.FindProperty(SignalIdPropertyName).stringValue = signalId;
            serializedSignal.ApplyModifiedPropertiesWithoutUndo();
            return signal;
        }

        /// <summary>
        /// Node の Enter シグナル一覧を設定
        /// </summary>
        /// <param name="node">設定対象ノード</param>
        /// <param name="signals">設定するシグナル一覧</param>
        public void SetEnterSignals(Node node, params Signal[] signals) {
            var serializedNode = new SerializedObject(node);
            SetSignalArray(serializedNode.FindProperty(EnterSignalsPropertyName), signals);
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Node の Exit シグナル一覧を設定
        /// </summary>
        /// <param name="node">設定対象ノード</param>
        /// <param name="signals">設定するシグナル一覧</param>
        public void SetExitSignals(Node node, params Signal[] signals) {
            var serializedNode = new SerializedObject(node);
            SetSignalArray(serializedNode.FindProperty(ExitSignalsPropertyName), signals);
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Node の後続ノード ID 一覧を設定
        /// </summary>
        /// <param name="node">設定対象ノード</param>
        /// <param name="nextNodeIds">後続ノード ID 一覧</param>
        public void SetNextNodeIds(Node node, params string[] nextNodeIds) {
            var serializedNode = new SerializedObject(node);
            SetStringArray(serializedNode.FindProperty(NextNodeIdsPropertyName), nextNodeIds);
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
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
        /// DelayNode の待機時間を設定
        /// </summary>
        /// <param name="delayNode">設定対象 DelayNode</param>
        /// <param name="delay">待機時間</param>
        public void SetDelay(DelayNode delayNode, float delay) {
            var serializedNode = new SerializedObject(delayNode);
            serializedNode.FindProperty(DelayPropertyName).floatValue = delay;
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// LoopNode の設定を更新
        /// </summary>
        /// <param name="loopNode">設定対象 LoopNode</param>
        /// <param name="loopCount">ループ実行回数</param>
        /// <param name="loopNodeIds">ループ内容ノード ID 一覧</param>
        public void SetLoop(LoopNode loopNode, int loopCount, params string[] loopNodeIds) {
            var serializedNode = new SerializedObject(loopNode);
            serializedNode.FindProperty(LoopCountPropertyName).intValue = loopCount;
            SetStringArray(serializedNode.FindProperty(LoopNodeIdsPropertyName), loopNodeIds);
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

            if (node is LoopNode) {
                SetStringArray(serializedNode.FindProperty(LoopNodeIdsPropertyName), Array.Empty<string>());
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

        /// <summary>
        /// SerializedProperty の Signal 配列を設定
        /// </summary>
        /// <param name="property">設定対象 SerializedProperty</param>
        /// <param name="signals">設定するシグナル一覧</param>
        private void SetSignalArray(SerializedProperty property, IReadOnlyList<Signal> signals) {
            property.arraySize = signals.Count;
            for (var i = 0; i < signals.Count; i++) {
                property.GetArrayElementAtIndex(i).objectReferenceValue = signals[i];
            }
        }
    }

    /// <summary>
    /// テスト用 AnimationGraphContext
    /// </summary>
    internal sealed class TestAnimationGraphContext : IAnimationGraphContext {
        private readonly Dictionary<string, object> _blackboardValues = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Component> _targets = new(StringComparer.Ordinal);

        /// <summary>
        /// target Component を設定
        /// </summary>
        /// <param name="key">Target キー</param>
        /// <param name="target">設定する target</param>
        public void SetTarget(string key, Component target) {
            _targets[key] = target;
        }

        /// <summary>
        /// bool Blackboard 値を設定
        /// </summary>
        /// <param name="key">Blackboard キー</param>
        /// <param name="value">設定する bool 値</param>
        public void SetBlackboardValue(string key, bool value) {
            _blackboardValues[key] = value;
        }

        /// <summary>
        /// int Blackboard 値を設定
        /// </summary>
        /// <param name="key">Blackboard キー</param>
        /// <param name="value">設定する int 値</param>
        public void SetBlackboardValue(string key, int value) {
            _blackboardValues[key] = value;
        }

        /// <summary>
        /// float Blackboard 値を設定
        /// </summary>
        /// <param name="key">Blackboard キー</param>
        /// <param name="value">設定する float 値</param>
        public void SetBlackboardValue(string key, float value) {
            _blackboardValues[key] = value;
        }

        /// <summary>
        /// string Blackboard 値を設定
        /// </summary>
        /// <param name="key">Blackboard キー</param>
        /// <param name="value">設定する string 値</param>
        public void SetBlackboardValue(string key, string value) {
            _blackboardValues[key] = value ?? string.Empty;
        }

        /// <summary>
        /// Vector2 Blackboard 値を設定
        /// </summary>
        /// <param name="key">Blackboard キー</param>
        /// <param name="value">設定する Vector2 値</param>
        public void SetBlackboardValue(string key, Vector2 value) {
            _blackboardValues[key] = value;
        }

        /// <summary>
        /// Vector3 Blackboard 値を設定
        /// </summary>
        /// <param name="key">Blackboard キー</param>
        /// <param name="value">設定する Vector3 値</param>
        public void SetBlackboardValue(string key, Vector3 value) {
            _blackboardValues[key] = value;
        }

        /// <summary>
        /// Color Blackboard 値を設定
        /// </summary>
        /// <param name="key">Blackboard キー</param>
        /// <param name="value">設定する Color 値</param>
        public void SetBlackboardValue(string key, Color value) {
            _blackboardValues[key] = value;
        }

        /// <summary>
        /// Vector4 Blackboard 値を設定
        /// </summary>
        /// <param name="key">Blackboard キー</param>
        /// <param name="value">設定する Vector4 値</param>
        public void SetBlackboardValue(string key, Vector4 value) {
            _blackboardValues[key] = value;
        }

        /// <inheritdoc/>
        public T GetTarget<T>(string key) where T : Component {
            if (TryGetTarget<T>(key, out var target)) {
                return target;
            }

            throw new InvalidOperationException($"Target '{key}' is not registered");
        }

        /// <inheritdoc/>
        public bool TryGetTarget<T>(string key, out T target) where T : Component {
            if (_targets.TryGetValue(key, out var component) && component is T typedTarget) {
                target = typedTarget;
                return true;
            }

            target = null;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out bool value) {
            if (_blackboardValues.TryGetValue(key, out var objectValue) && objectValue is bool typedValue) {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out int value) {
            if (_blackboardValues.TryGetValue(key, out var objectValue) && objectValue is int typedValue) {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out float value) {
            if (_blackboardValues.TryGetValue(key, out var objectValue) && objectValue is float typedValue) {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out string value) {
            if (_blackboardValues.TryGetValue(key, out var objectValue) && objectValue is string typedValue) {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out Vector2 value) {
            if (_blackboardValues.TryGetValue(key, out var objectValue) && objectValue is Vector2 typedValue) {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out Vector3 value) {
            if (_blackboardValues.TryGetValue(key, out var objectValue) && objectValue is Vector3 typedValue) {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out Color value) {
            if (_blackboardValues.TryGetValue(key, out var objectValue) && objectValue is Color typedValue) {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out Vector4 value) {
            if (_blackboardValues.TryGetValue(key, out var objectValue) && objectValue is Vector4 typedValue) {
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
        private IList<string> _events;
        private string _eventPrefix;

        /// <summary>Evaluate が呼ばれた回数</summary>
        public int EvaluateCount { get; private set; }
        /// <summary>Enter が呼ばれた回数</summary>
        public int EnterCount { get; private set; }
        /// <summary>Exit が呼ばれた回数</summary>
        public int ExitCount { get; private set; }
        /// <summary>Cancel が呼ばれた回数</summary>
        public int CancelCount { get; private set; }
        /// <summary>最後に渡された localTime</summary>
        public float LastLocalTime { get; private set; }
        /// <summary>最後に渡された duration</summary>
        public float LastDuration { get; private set; }
        /// <summary>最後に渡された seed</summary>
        public int LastSeed { get; private set; }
        /// <summary>最後に Enter に渡された seed</summary>
        public int LastEnterSeed { get; private set; }
        /// <summary>最後に Exit に渡された seed</summary>
        public int LastExitSeed { get; private set; }
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

        /// <summary>
        /// 実行順の記録先を設定
        /// </summary>
        /// <param name="events">イベント記録先</param>
        /// <param name="eventPrefix">イベント名の接頭辞</param>
        public void ConfigureEvents(IList<string> events, string eventPrefix) {
            _events = events;
            _eventPrefix = eventPrefix ?? string.Empty;
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
        protected override void Enter(int seed, IAnimationGraphContext context) {
            EnterCount++;
            LastEnterSeed = seed;
            RecordEvent("Enter");
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context) {
            EvaluateCount++;
            LastSeed = seed;
            LastLocalTime = localTime;
            LastDuration = calculatedDuration;
            RecordEvent("Evaluate");
        }

        /// <inheritdoc/>
        protected override void Exit(int seed, IAnimationGraphContext context) {
            ExitCount++;
            LastExitSeed = seed;
            RecordEvent("Exit");
        }

        /// <inheritdoc/>
        protected override void Cancel(int seed, IAnimationGraphContext context) {
            CancelCount++;
            LastCancelSeed = seed;
        }

        /// <summary>
        /// 実行順イベントを記録
        /// </summary>
        /// <param name="eventName">イベント名</param>
        private void RecordEvent(string eventName) {
            _events?.Add($"{_eventPrefix}.{eventName}");
        }
    }

    /// <summary>
    /// テスト用 signal
    /// </summary>
    internal sealed class TestSignal : Signal {
        private IList<string> _events;
        private string _eventName;

        /// <summary>Dispatch が呼ばれた回数</summary>
        public int DispatchCount { get; private set; }
        /// <summary>最後に Dispatch に渡された seed</summary>
        public int LastSeed { get; private set; }

        /// <summary>
        /// テスト用の記録先を設定
        /// </summary>
        /// <param name="eventName">記録するイベント名</param>
        /// <param name="events">イベント記録先</param>
        public void Configure(string eventName, IList<string> events) {
            _eventName = eventName ?? string.Empty;
            _events = events;
        }

        /// <inheritdoc/>
        protected override void Dispatch(int seed, IAnimationGraphContext context) {
            DispatchCount++;
            LastSeed = seed;
            if (!string.IsNullOrEmpty(_eventName)) {
                _events?.Add(_eventName);
            }
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
