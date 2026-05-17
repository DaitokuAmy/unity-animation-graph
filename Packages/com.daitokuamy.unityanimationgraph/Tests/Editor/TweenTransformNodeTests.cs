using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// Transform Tween node の EditMode テスト
    /// </summary>
    public sealed class TweenTransformNodeTests {
        private const string TargetKey = "target";
        private const string ActionTargetKeyPropertyName = "_targetKey";
        private const string TweenPropertyName = "_tween";
        private const string SpacePropertyName = "_space";
        private const string DurationPropertyName = "_duration";
        private const string DelayPropertyName = "_delay";
        private const string BeforePropertyName = "_before";
        private const string AfterPropertyName = "_after";
        private const string EasePropertyName = "_ease";
        private const string IgnoreMaskPropertyName = "_ignoreMask";
        private const string SourcePropertyName = "_source";
        private const string BlackboardKeyPropertyName = "_blackboardKey";
        private const string ValuePropertyName = "_value";
        private const string RelativePropertyName = "_relative";
        private const string EaseModePropertyName = "_mode";
        private const string EaseTypePropertyName = "_easeType";

        /// <summary>
        /// Position node は EaseType で localPosition を補間する
        /// </summary>
        [Test]
        public void PositionNode_EvaluatesLocalPositionWithEaseType() {
            var node = ScriptableObject.CreateInstance<TweenTransformPositionNode>();
            var gameObject = new GameObject("Target");
            try {
                var context = CreateContext(gameObject.transform);
                SetActionTargetKey(node, TargetKey);
                SetVector3TweenDirect(node, Vector3.zero, Vector3.up, 2.0f, EaseType.EaseInQuad);

                ((INodeExecutor)node).Evaluate(0, 1.0f, 2.0f, context);

                AssertVector3(gameObject.transform.localPosition, Vector3.up * 0.25f);
            }
            finally {
                DestroyObject(node);
                DestroyObject(gameObject);
            }
        }

        /// <summary>
        /// Position node は target 解決時に node 直下の duration/delay を実行タイミングとして返す
        /// </summary>
        [Test]
        public void PositionNode_CalculatesDurationAndDelayFromNodeTiming() {
            var node = ScriptableObject.CreateInstance<TweenTransformPositionNode>();
            var gameObject = new GameObject("Target");
            try {
                var context = CreateContext(gameObject.transform);
                SetActionTargetKey(node, TargetKey);
                SetTweenTiming(node, 2.5f, 0.75f);

                var duration = ((INodeExecutor)node).CalculateDuration(0, context);
                var delay = ((INodeExecutor)node).CalculateDelay(0, context);

                Assert.That(duration, Is.EqualTo(2.5f).Within(0.0001f));
                Assert.That(delay, Is.EqualTo(0.75f).Within(0.0001f));
            }
            finally {
                DestroyObject(node);
                DestroyObject(gameObject);
            }
        }

        /// <summary>
        /// Position node は Vector3 IgnoreMask に含まれる軸を現在値で維持する
        /// </summary>
        [Test]
        public void PositionNode_EvaluatesLocalPositionWithVector3IgnoreMask() {
            var node = ScriptableObject.CreateInstance<TweenTransformPositionNode>();
            var gameObject = new GameObject("Target");
            try {
                gameObject.transform.localPosition = new Vector3(5.0f, 0.0f, 9.0f);
                var context = CreateContext(gameObject.transform);
                SetActionTargetKey(node, TargetKey);
                SetVector3TweenDirect(node, Vector3.zero, new Vector3(10.0f, 20.0f, 30.0f), 2.0f, EaseType.Linear, Vector3IgnoreMask.X | Vector3IgnoreMask.Z);

                ((INodeExecutor)node).Evaluate(0, 1.0f, 2.0f, context);

                AssertVector3(gameObject.transform.localPosition, new Vector3(5.0f, 10.0f, 9.0f));
            }
            finally {
                DestroyObject(node);
                DestroyObject(gameObject);
            }
        }

        /// <summary>
        /// Position node は World 設定時に position を補間する
        /// </summary>
        [Test]
        public void PositionNode_EvaluatesWorldPosition() {
            var node = ScriptableObject.CreateInstance<TweenTransformPositionNode>();
            var parent = new GameObject("Parent");
            var gameObject = new GameObject("Target");
            try {
                gameObject.transform.SetParent(parent.transform);
                parent.transform.position = Vector3.right * 10.0f;
                var context = CreateContext(gameObject.transform);
                SetActionTargetKey(node, TargetKey);
                SetVector3TweenDirect(node, Vector3.zero, Vector3.up * 2.0f, 2.0f, EaseType.Linear);
                SetTransformSpace(node, Space.World);

                ((INodeExecutor)node).Evaluate(0, 1.0f, 2.0f, context);

                AssertVector3(gameObject.transform.position, Vector3.up);
            }
            finally {
                DestroyObject(node);
                DestroyObject(gameObject);
                DestroyObject(parent);
            }
        }

        /// <summary>
        /// Position node は相対値の基準値を開始時点で固定する
        /// </summary>
        [Test]
        public void PositionNode_UsesCapturedBaseValueForRelativeParameters() {
            using var builder = new AnimationGraphTestBuilder();
            var gameObject = new GameObject("Target");
            try {
                gameObject.transform.localPosition = new Vector3(10.0f, 0.0f, 0.0f);
                var context = CreateContext(gameObject.transform);
                var startNode = builder.CreateStartNode("start", "move");
                var moveNode = builder.CreateNode<TweenTransformPositionNode>("move");
                SetActionTargetKey(moveNode, TargetKey);
                SetVector3TweenDirect(moveNode, Vector3.zero, Vector3.up, 2.0f, EaseType.Linear, Vector3IgnoreMask.None, true, true);
                var graphAsset = builder.CreateGraph("start", startNode, moveNode);
                var player = new AnimationGraphPlayer();
                player.SetContext(context);
                player.SetGraph(graphAsset);

                player.Play();
                player.Tick(0.5f);
                AssertVector3(gameObject.transform.localPosition, new Vector3(10.0f, 0.25f, 0.0f));

                player.Tick(0.5f);
                AssertVector3(gameObject.transform.localPosition, new Vector3(10.0f, 0.5f, 0.0f));
            }
            finally {
                DestroyObject(gameObject);
            }
        }

        /// <summary>
        /// Rotation node は Euler 角で localRotation を補間する
        /// </summary>
        [Test]
        public void RotationNode_EvaluatesLocalRotationAsEulerAngles() {
            var node = ScriptableObject.CreateInstance<TweenTransformRotationNode>();
            var gameObject = new GameObject("Target");
            try {
                var context = CreateContext(gameObject.transform);
                SetActionTargetKey(node, TargetKey);
                SetVector3TweenDirect(node, Vector3.zero, new Vector3(0.0f, 180.0f, 0.0f), 2.0f, EaseType.Linear);

                ((INodeExecutor)node).Evaluate(0, 1.0f, 2.0f, context);

                Assert.That(Quaternion.Angle(gameObject.transform.localRotation, Quaternion.Euler(0.0f, 90.0f, 0.0f)), Is.LessThan(0.01f));
            }
            finally {
                DestroyObject(node);
                DestroyObject(gameObject);
            }
        }

        /// <summary>
        /// Scale node は Blackboard 値を使って localScale を補間する
        /// </summary>
        [Test]
        public void ScaleNode_EvaluatesLocalScaleWithBlackboardValues() {
            var node = ScriptableObject.CreateInstance<TweenTransformScaleNode>();
            var gameObject = new GameObject("Target");
            try {
                var context = CreateContext(gameObject.transform);
                context.SetBlackboardValue("before", Vector3.one);
                context.SetBlackboardValue("after", Vector3.one * 3.0f);
                SetActionTargetKey(node, TargetKey);
                SetVector3TweenBlackboard(node, "before", "after", 2.0f, EaseType.Linear);

                ((INodeExecutor)node).Evaluate(0, 1.0f, 2.0f, context);

                AssertVector3(gameObject.transform.localScale, Vector3.one * 2.0f);
            }
            finally {
                DestroyObject(node);
                DestroyObject(gameObject);
            }
        }

        /// <summary>
        /// SpriteRenderer Color node は Blackboard 値を使って color を補間する
        /// </summary>
        [Test]
        public void SpriteRendererColorNode_EvaluatesColorWithBlackboardValues() {
            var node = ScriptableObject.CreateInstance<TweenSpriteRendererColorNode>();
            var gameObject = new GameObject("Target");
            try {
                var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                var context = CreateContext(spriteRenderer);
                context.SetBlackboardValue("beforeColor", Color.white);
                context.SetBlackboardValue("afterColor", Color.red);
                SetActionTargetKey(node, TargetKey);
                SetColorTweenBlackboard(node, "beforeColor", "afterColor", 2.0f, EaseType.Linear);

                ((INodeExecutor)node).Evaluate(0, 1.0f, 2.0f, context);

                AssertColor(spriteRenderer.color, new Color(1.0f, 0.5f, 0.5f, 1.0f));
            }
            finally {
                DestroyObject(node);
                DestroyObject(gameObject);
            }
        }

        /// <summary>
        /// Component target を登録したテスト用コンテキストを生成
        /// </summary>
        /// <param name="target">登録する Component</param>
        /// <returns>生成したテスト用コンテキスト</returns>
        private static TestAnimationGraphContext CreateContext(Component target) {
            var context = new TestAnimationGraphContext();
            context.SetTarget(TargetKey, target);
            return context;
        }

        /// <summary>
        /// ActionNode の TargetKey を設定
        /// </summary>
        /// <param name="node">設定対象 ActionNode</param>
        /// <param name="targetKey">設定する target key</param>
        private static void SetActionTargetKey(ActionNode node, string targetKey) {
            var serializedNode = new SerializedObject(node);
            serializedNode.FindProperty(ActionTargetKeyPropertyName).stringValue = targetKey;
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 直値を使う Vector3 Tween 設定を反映
        /// </summary>
        /// <param name="node">設定対象 node</param>
        /// <param name="before">Tween 開始時の値</param>
        /// <param name="after">Tween 終了時の値</param>
        /// <param name="duration">Tween にかける時間</param>
        /// <param name="easeType">補間カーブ種別</param>
        /// <param name="ignoreMask">更新しない要素</param>
        private static void SetVector3TweenDirect(Node node, Vector3 before, Vector3 after, float duration, EaseType easeType, Vector3IgnoreMask ignoreMask = Vector3IgnoreMask.None, bool beforeRelative = false, bool afterRelative = false) {
            var serializedNode = new SerializedObject(node);
            SetTweenTiming(serializedNode, duration, 0.0f);
            var tweenProperty = serializedNode.FindProperty(TweenPropertyName);
            SetDirectVector3Parameter(tweenProperty.FindPropertyRelative(BeforePropertyName), before, beforeRelative);
            SetDirectVector3Parameter(tweenProperty.FindPropertyRelative(AfterPropertyName), after, afterRelative);
            tweenProperty.FindPropertyRelative(IgnoreMaskPropertyName).intValue = (int)ignoreMask;
            SetEaseType(tweenProperty.FindPropertyRelative(EasePropertyName), easeType);
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Blackboard 値を使う Vector3 Tween 設定を反映
        /// </summary>
        /// <param name="node">設定対象 node</param>
        /// <param name="beforeKey">Tween 開始時の Blackboard key</param>
        /// <param name="afterKey">Tween 終了時の Blackboard key</param>
        /// <param name="duration">Tween にかける時間</param>
        /// <param name="easeType">補間カーブ種別</param>
        private static void SetVector3TweenBlackboard(Node node, string beforeKey, string afterKey, float duration, EaseType easeType) {
            var serializedNode = new SerializedObject(node);
            SetTweenTiming(serializedNode, duration, 0.0f);
            var tweenProperty = serializedNode.FindProperty(TweenPropertyName);
            SetBlackboardVector3Parameter(tweenProperty.FindPropertyRelative(BeforePropertyName), beforeKey);
            SetBlackboardVector3Parameter(tweenProperty.FindPropertyRelative(AfterPropertyName), afterKey);
            tweenProperty.FindPropertyRelative(IgnoreMaskPropertyName).intValue = (int)Vector3IgnoreMask.None;
            SetEaseType(tweenProperty.FindPropertyRelative(EasePropertyName), easeType);
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Blackboard 値を使う Color Tween 設定を反映
        /// </summary>
        /// <param name="node">設定対象 node</param>
        /// <param name="beforeKey">Tween 開始時の Blackboard key</param>
        /// <param name="afterKey">Tween 終了時の Blackboard key</param>
        /// <param name="duration">Tween にかける時間</param>
        /// <param name="easeType">補間カーブ種別</param>
        private static void SetColorTweenBlackboard(Node node, string beforeKey, string afterKey, float duration, EaseType easeType) {
            var serializedNode = new SerializedObject(node);
            SetTweenTiming(serializedNode, duration, 0.0f);
            var tweenProperty = serializedNode.FindProperty(TweenPropertyName);
            SetBlackboardColorParameter(tweenProperty.FindPropertyRelative(BeforePropertyName), beforeKey);
            SetBlackboardColorParameter(tweenProperty.FindPropertyRelative(AfterPropertyName), afterKey);
            SetEaseType(tweenProperty.FindPropertyRelative(EasePropertyName), easeType);
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Tween node の実行時間設定を反映
        /// </summary>
        /// <param name="node">設定対象 node</param>
        /// <param name="duration">Tween にかける時間</param>
        /// <param name="delay">Tween 開始前の待機時間</param>
        private static void SetTweenTiming(Object node, float duration, float delay) {
            var serializedNode = new SerializedObject(node);
            SetTweenTiming(serializedNode, duration, delay);
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Tween node の実行時間設定を SerializedObject に反映
        /// </summary>
        /// <param name="serializedNode">設定対象 SerializedObject</param>
        /// <param name="duration">Tween にかける時間</param>
        /// <param name="delay">Tween 開始前の待機時間</param>
        private static void SetTweenTiming(SerializedObject serializedNode, float duration, float delay) {
            serializedNode.FindProperty(DurationPropertyName).floatValue = duration;
            serializedNode.FindProperty(DelayPropertyName).floatValue = delay;
        }

        /// <summary>
        /// Transform Tween の適用空間を設定
        /// </summary>
        /// <param name="node">設定対象 node</param>
        /// <param name="space">適用空間</param>
        private static void SetTransformSpace(Object node, Space space) {
            var serializedNode = new SerializedObject(node);
            serializedNode.FindProperty(SpacePropertyName).enumValueIndex = (int)space;
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Vector3 直値パラメータを設定
        /// </summary>
        /// <param name="parameterProperty">設定対象 SerializedProperty</param>
        /// <param name="value">設定する Vector3 値</param>
        private static void SetDirectVector3Parameter(SerializedProperty parameterProperty, Vector3 value, bool relative = false) {
            parameterProperty.FindPropertyRelative(SourcePropertyName).enumValueIndex = (int)ParameterSource.Value;
            parameterProperty.FindPropertyRelative(ValuePropertyName).vector3Value = value;
            parameterProperty.FindPropertyRelative(RelativePropertyName).boolValue = relative;
        }

        /// <summary>
        /// Vector3 Blackboard パラメータを設定
        /// </summary>
        /// <param name="parameterProperty">設定対象 SerializedProperty</param>
        /// <param name="blackboardKey">参照する Blackboard key</param>
        private static void SetBlackboardVector3Parameter(SerializedProperty parameterProperty, string blackboardKey) {
            parameterProperty.FindPropertyRelative(SourcePropertyName).enumValueIndex = (int)ParameterSource.Blackboard;
            parameterProperty.FindPropertyRelative(BlackboardKeyPropertyName).stringValue = blackboardKey;
        }

        /// <summary>
        /// Color Blackboard パラメータを設定
        /// </summary>
        /// <param name="parameterProperty">設定対象 SerializedProperty</param>
        /// <param name="blackboardKey">参照する Blackboard key</param>
        private static void SetBlackboardColorParameter(SerializedProperty parameterProperty, string blackboardKey) {
            parameterProperty.FindPropertyRelative(SourcePropertyName).enumValueIndex = (int)ParameterSource.Blackboard;
            parameterProperty.FindPropertyRelative(BlackboardKeyPropertyName).stringValue = blackboardKey;
        }

        /// <summary>
        /// EaseType を使う補間設定を反映
        /// </summary>
        /// <param name="easeProperty">設定対象 SerializedProperty</param>
        /// <param name="easeType">補間カーブ種別</param>
        private static void SetEaseType(SerializedProperty easeProperty, EaseType easeType) {
            easeProperty.FindPropertyRelative(EaseModePropertyName).enumValueIndex = (int)TweenEaseMode.Preset;
            easeProperty.FindPropertyRelative(EaseTypePropertyName).enumValueIndex = (int)easeType;
        }

        /// <summary>
        /// Vector3 値が期待値と一致することを検証
        /// </summary>
        /// <param name="actual">実際の値</param>
        /// <param name="expected">期待値</param>
        private static void AssertVector3(Vector3 actual, Vector3 expected) {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.0001f));
        }

        /// <summary>
        /// Color 値が期待値と一致することを検証
        /// </summary>
        /// <param name="actual">実際の値</param>
        /// <param name="expected">期待値</param>
        private static void AssertColor(Color actual, Color expected) {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.0001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.0001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.0001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.0001f));
        }

        /// <summary>
        /// Unity Object を即時破棄
        /// </summary>
        /// <param name="target">破棄対象</param>
        private static void DestroyObject(Object target) {
            if (target == null) {
                return;
            }

            Object.DestroyImmediate(target);
        }
    }
}
