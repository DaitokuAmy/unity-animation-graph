using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// 追加 Tween node の EditMode テスト
    /// </summary>
    public sealed class TweenAdditionalNodeTests {
        private const string TargetKey = "target";
        private const string ActionTargetKeyPropertyName = "_targetKey";
        private const string TweenPropertyName = "_tween";
        private const string DurationPropertyName = "_duration";
        private const string DelayPropertyName = "_delay";
        private const string BeforePropertyName = "_before";
        private const string AfterPropertyName = "_after";
        private const string EasePropertyName = "_ease";
        private const string IgnoreMaskPropertyName = "_ignoreMask";
        private const string SourcePropertyName = "_source";
        private const string ValuePropertyName = "_value";
        private const string EaseModePropertyName = "_mode";
        private const string EaseTypePropertyName = "_easeType";

        /// <summary>
        /// CanvasGroup alpha node は alpha を補間する
        /// </summary>
        [Test]
        public void CanvasGroupAlphaNode_EvaluatesAlpha() {
            var node = ScriptableObject.CreateInstance<TweenCanvasGroupAlphaNode>();
            var gameObject = new GameObject("Target");
            try {
                var canvasGroup = gameObject.AddComponent<CanvasGroup>();
                var context = CreateContext(canvasGroup);
                SetActionTargetKey(node, TargetKey);
                SetFloatTweenDirect(node, 0.0f, 1.0f, 2.0f, EaseType.Linear);

                ((INodeExecutor)node).Evaluate(0, 1.0f, 2.0f, context);

                Assert.That(canvasGroup.alpha, Is.EqualTo(0.5f).Within(0.0001f));
            }
            finally {
                DestroyObject(node);
                DestroyObject(gameObject);
            }
        }

        /// <summary>
        /// AudioSource nodes は volume と pitch を補間する
        /// </summary>
        [Test]
        public void AudioSourceNodes_EvaluateVolumeAndPitch() {
            var volumeNode = ScriptableObject.CreateInstance<TweenAudioSourceVolumeNode>();
            var pitchNode = ScriptableObject.CreateInstance<TweenAudioSourcePitchNode>();
            var gameObject = new GameObject("Target");
            try {
                var audioSource = gameObject.AddComponent<AudioSource>();
                var context = CreateContext(audioSource);
                SetActionTargetKey(volumeNode, TargetKey);
                SetActionTargetKey(pitchNode, TargetKey);
                SetFloatTweenDirect(volumeNode, 0.0f, 1.0f, 2.0f, EaseType.Linear);
                SetFloatTweenDirect(pitchNode, 1.0f, 2.0f, 2.0f, EaseType.Linear);

                ((INodeExecutor)volumeNode).Evaluate(0, 1.0f, 2.0f, context);
                ((INodeExecutor)pitchNode).Evaluate(0, 1.0f, 2.0f, context);

                Assert.That(audioSource.volume, Is.EqualTo(0.5f).Within(0.0001f));
                Assert.That(audioSource.pitch, Is.EqualTo(1.5f).Within(0.0001f));
            }
            finally {
                DestroyObject(volumeNode);
                DestroyObject(pitchNode);
                DestroyObject(gameObject);
            }
        }

        /// <summary>
        /// Light nodes は intensity と color を補間し Color IgnoreMask を反映する
        /// </summary>
        [Test]
        public void LightNodes_EvaluateIntensityAndColorWithIgnoreMask() {
            var intensityNode = ScriptableObject.CreateInstance<TweenLightIntensityNode>();
            var colorNode = ScriptableObject.CreateInstance<TweenLightColorNode>();
            var gameObject = new GameObject("Target");
            try {
                var light = gameObject.AddComponent<Light>();
                light.color = new Color(0.25f, 0.3f, 0.4f, 1.0f);
                var context = CreateContext(light);
                SetActionTargetKey(intensityNode, TargetKey);
                SetActionTargetKey(colorNode, TargetKey);
                SetFloatTweenDirect(intensityNode, 2.0f, 4.0f, 2.0f, EaseType.Linear);
                SetColorTweenDirect(colorNode, Color.white, Color.red, 2.0f, EaseType.Linear, ColorIgnoreMask.G);

                ((INodeExecutor)intensityNode).Evaluate(0, 1.0f, 2.0f, context);
                ((INodeExecutor)colorNode).Evaluate(0, 1.0f, 2.0f, context);

                Assert.That(light.intensity, Is.EqualTo(3.0f).Within(0.0001f));
                AssertColor(light.color, new Color(1.0f, 0.3f, 0.5f, 1.0f));
            }
            finally {
                DestroyObject(intensityNode);
                DestroyObject(colorNode);
                DestroyObject(gameObject);
            }
        }

        /// <summary>
        /// Camera nodes は fieldOfView と orthographicSize を補間する
        /// </summary>
        [Test]
        public void CameraNodes_EvaluateLensValues() {
            var fieldOfViewNode = ScriptableObject.CreateInstance<TweenCameraFieldOfViewNode>();
            var orthographicSizeNode = ScriptableObject.CreateInstance<TweenCameraOrthographicSizeNode>();
            var gameObject = new GameObject("Target");
            try {
                var camera = gameObject.AddComponent<Camera>();
                var context = CreateContext(camera);
                SetActionTargetKey(fieldOfViewNode, TargetKey);
                SetActionTargetKey(orthographicSizeNode, TargetKey);
                SetFloatTweenDirect(fieldOfViewNode, 40.0f, 80.0f, 2.0f, EaseType.Linear);
                SetFloatTweenDirect(orthographicSizeNode, 4.0f, 8.0f, 2.0f, EaseType.Linear);

                ((INodeExecutor)fieldOfViewNode).Evaluate(0, 1.0f, 2.0f, context);
                ((INodeExecutor)orthographicSizeNode).Evaluate(0, 1.0f, 2.0f, context);

                Assert.That(camera.fieldOfView, Is.EqualTo(60.0f).Within(0.0001f));
                Assert.That(camera.orthographicSize, Is.EqualTo(6.0f).Within(0.0001f));
            }
            finally {
                DestroyObject(fieldOfViewNode);
                DestroyObject(orthographicSizeNode);
                DestroyObject(gameObject);
            }
        }

        /// <summary>
        /// RectTransform nodes は Vector2 IgnoreMask を反映して補間する
        /// </summary>
        [Test]
        public void RectTransformNodes_EvaluateVector2WithIgnoreMask() {
            var anchoredPositionNode = ScriptableObject.CreateInstance<TweenRectTransformAnchoredPositionNode>();
            var sizeDeltaNode = ScriptableObject.CreateInstance<TweenRectTransformSizeDeltaNode>();
            var gameObject = new GameObject("Target", typeof(RectTransform));
            try {
                var rectTransform = gameObject.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(10.0f, 20.0f);
                rectTransform.sizeDelta = new Vector2(30.0f, 40.0f);
                var context = CreateContext(rectTransform);
                SetActionTargetKey(anchoredPositionNode, TargetKey);
                SetActionTargetKey(sizeDeltaNode, TargetKey);
                SetVector2TweenDirect(anchoredPositionNode, Vector2.zero, new Vector2(100.0f, 200.0f), 2.0f, EaseType.Linear, Vector2IgnoreMask.X);
                SetVector2TweenDirect(sizeDeltaNode, Vector2.one * 100.0f, new Vector2(200.0f, 300.0f), 2.0f, EaseType.Linear, Vector2IgnoreMask.Y);

                ((INodeExecutor)anchoredPositionNode).Evaluate(0, 1.0f, 2.0f, context);
                ((INodeExecutor)sizeDeltaNode).Evaluate(0, 1.0f, 2.0f, context);

                AssertVector2(rectTransform.anchoredPosition, new Vector2(10.0f, 100.0f));
                AssertVector2(rectTransform.sizeDelta, new Vector2(150.0f, 40.0f));
            }
            finally {
                DestroyObject(anchoredPositionNode);
                DestroyObject(sizeDeltaNode);
                DestroyObject(gameObject);
            }
        }

        /// <summary>
        /// SpriteRenderer alpha node は RGB を維持して alpha のみ補間する
        /// </summary>
        [Test]
        public void SpriteRendererAlphaNode_EvaluatesAlphaOnly() {
            var node = ScriptableObject.CreateInstance<TweenSpriteRendererAlphaNode>();
            var gameObject = new GameObject("Target");
            try {
                var spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.color = new Color(0.25f, 0.5f, 0.75f, 1.0f);
                var context = CreateContext(spriteRenderer);
                SetActionTargetKey(node, TargetKey);
                SetFloatTweenDirect(node, 1.0f, 0.0f, 2.0f, EaseType.Linear);

                ((INodeExecutor)node).Evaluate(0, 1.0f, 2.0f, context);

                AssertColor(spriteRenderer.color, new Color(0.25f, 0.5f, 0.75f, 0.5f));
            }
            finally {
                DestroyObject(node);
                DestroyObject(gameObject);
            }
        }

        /// <summary>
        /// uGUI nodes は Graphic color と Image fillAmount を補間する
        /// </summary>
        [Test]
        public void UguiNodes_EvaluateGraphicColorAndImageFillAmount() {
            var graphicColorNode = ScriptableObject.CreateInstance<TweenGraphicColorNode>();
            var fillAmountNode = ScriptableObject.CreateInstance<TweenImageFillAmountNode>();
            var gameObject = new GameObject("Target", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            try {
                var image = gameObject.GetComponent<Image>();
                image.color = new Color(0.2f, 0.3f, 0.4f, 0.25f);
                image.fillAmount = 0.0f;
                var context = CreateContext(image);
                SetActionTargetKey(graphicColorNode, TargetKey);
                SetActionTargetKey(fillAmountNode, TargetKey);
                SetColorTweenDirect(graphicColorNode, Color.white, Color.red, 2.0f, EaseType.Linear, ColorIgnoreMask.A);
                SetFloatTweenDirect(fillAmountNode, 0.0f, 1.0f, 2.0f, EaseType.Linear);

                ((INodeExecutor)graphicColorNode).Evaluate(0, 1.0f, 2.0f, context);
                ((INodeExecutor)fillAmountNode).Evaluate(0, 1.0f, 2.0f, context);

                AssertColor(image.color, new Color(1.0f, 0.5f, 0.5f, 0.25f));
                Assert.That(image.fillAmount, Is.EqualTo(0.5f).Within(0.0001f));
            }
            finally {
                DestroyObject(graphicColorNode);
                DestroyObject(fillAmountNode);
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
        /// 直値を使う Float Tween 設定を反映
        /// </summary>
        /// <param name="node">設定対象 node</param>
        /// <param name="before">Tween 開始時の値</param>
        /// <param name="after">Tween 終了時の値</param>
        /// <param name="duration">Tween にかける時間</param>
        /// <param name="easeType">補間カーブ種別</param>
        private static void SetFloatTweenDirect(Node node, float before, float after, float duration, EaseType easeType) {
            var serializedNode = new SerializedObject(node);
            SetTweenTiming(serializedNode, duration);
            var tweenProperty = serializedNode.FindProperty(TweenPropertyName);
            SetDirectFloatParameter(tweenProperty.FindPropertyRelative(BeforePropertyName), before);
            SetDirectFloatParameter(tweenProperty.FindPropertyRelative(AfterPropertyName), after);
            SetEaseType(tweenProperty.FindPropertyRelative(EasePropertyName), easeType);
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 直値を使う Vector2 Tween 設定を反映
        /// </summary>
        /// <param name="node">設定対象 node</param>
        /// <param name="before">Tween 開始時の値</param>
        /// <param name="after">Tween 終了時の値</param>
        /// <param name="duration">Tween にかける時間</param>
        /// <param name="easeType">補間カーブ種別</param>
        /// <param name="ignoreMask">更新しない要素</param>
        private static void SetVector2TweenDirect(Node node, Vector2 before, Vector2 after, float duration, EaseType easeType, Vector2IgnoreMask ignoreMask) {
            var serializedNode = new SerializedObject(node);
            SetTweenTiming(serializedNode, duration);
            var tweenProperty = serializedNode.FindProperty(TweenPropertyName);
            SetDirectVector2Parameter(tweenProperty.FindPropertyRelative(BeforePropertyName), before);
            SetDirectVector2Parameter(tweenProperty.FindPropertyRelative(AfterPropertyName), after);
            tweenProperty.FindPropertyRelative(IgnoreMaskPropertyName).intValue = (int)ignoreMask;
            SetEaseType(tweenProperty.FindPropertyRelative(EasePropertyName), easeType);
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 直値を使う Color Tween 設定を反映
        /// </summary>
        /// <param name="node">設定対象 node</param>
        /// <param name="before">Tween 開始時の色</param>
        /// <param name="after">Tween 終了時の色</param>
        /// <param name="duration">Tween にかける時間</param>
        /// <param name="easeType">補間カーブ種別</param>
        /// <param name="ignoreMask">更新しない要素</param>
        private static void SetColorTweenDirect(Node node, Color before, Color after, float duration, EaseType easeType, ColorIgnoreMask ignoreMask) {
            var serializedNode = new SerializedObject(node);
            SetTweenTiming(serializedNode, duration);
            var tweenProperty = serializedNode.FindProperty(TweenPropertyName);
            SetDirectColorParameter(tweenProperty.FindPropertyRelative(BeforePropertyName), before);
            SetDirectColorParameter(tweenProperty.FindPropertyRelative(AfterPropertyName), after);
            tweenProperty.FindPropertyRelative(IgnoreMaskPropertyName).intValue = (int)ignoreMask;
            SetEaseType(tweenProperty.FindPropertyRelative(EasePropertyName), easeType);
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Tween node の実行時間設定を SerializedObject に反映
        /// </summary>
        /// <param name="serializedNode">設定対象 SerializedObject</param>
        /// <param name="duration">Tween にかける時間</param>
        private static void SetTweenTiming(SerializedObject serializedNode, float duration) {
            serializedNode.FindProperty(DurationPropertyName).floatValue = duration;
            serializedNode.FindProperty(DelayPropertyName).floatValue = 0.0f;
        }

        /// <summary>
        /// Float 直値パラメータを設定
        /// </summary>
        /// <param name="parameterProperty">設定対象 SerializedProperty</param>
        /// <param name="value">設定する float 値</param>
        private static void SetDirectFloatParameter(SerializedProperty parameterProperty, float value) {
            parameterProperty.FindPropertyRelative(SourcePropertyName).enumValueIndex = (int)ParameterSource.Value;
            parameterProperty.FindPropertyRelative(ValuePropertyName).floatValue = value;
        }

        /// <summary>
        /// Vector2 直値パラメータを設定
        /// </summary>
        /// <param name="parameterProperty">設定対象 SerializedProperty</param>
        /// <param name="value">設定する Vector2 値</param>
        private static void SetDirectVector2Parameter(SerializedProperty parameterProperty, Vector2 value) {
            parameterProperty.FindPropertyRelative(SourcePropertyName).enumValueIndex = (int)ParameterSource.Value;
            parameterProperty.FindPropertyRelative(ValuePropertyName).vector2Value = value;
        }

        /// <summary>
        /// Color 直値パラメータを設定
        /// </summary>
        /// <param name="parameterProperty">設定対象 SerializedProperty</param>
        /// <param name="value">設定する Color 値</param>
        private static void SetDirectColorParameter(SerializedProperty parameterProperty, Color value) {
            parameterProperty.FindPropertyRelative(SourcePropertyName).enumValueIndex = (int)ParameterSource.Value;
            parameterProperty.FindPropertyRelative(ValuePropertyName).colorValue = value;
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
        /// Vector2 値が期待値と一致することを検証
        /// </summary>
        /// <param name="actual">実際の値</param>
        /// <param name="expected">期待値</param>
        private static void AssertVector2(Vector2 actual, Vector2 expected) {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
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
