using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// AnimationGraphAsset の schema 定義テスト
    /// </summary>
    public sealed class AnimationGraphAssetSchemaTests {
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
        /// target 定義は key だけで取得できる
        /// </summary>
        [Test]
        public void TryGetTargetDefinition_ReturnsDefinitionByKey() {
            var graphAsset = ScriptableObject.CreateInstance<AnimationGraphAsset>();

            try {
                SetTargetDefinitions(graphAsset, "actor", "camera");

                var result = graphAsset.TryGetTargetDefinition("camera", out var definition);

                Assert.IsTrue(result);
                Assert.That(definition.Key, Is.EqualTo("camera"));
                Assert.That(definition.MonoScriptGuid, Is.Empty);
                Assert.IsFalse(graphAsset.TryGetTargetDefinition("missing", out _));
            }
            finally {
                Object.DestroyImmediate(graphAsset);
            }
        }

        /// <summary>
        /// Blackboard 定義は key と value type を取得できる
        /// </summary>
        [Test]
        public void TryGetBlackboardDefinition_ReturnsDefinitionByKey() {
            var graphAsset = ScriptableObject.CreateInstance<AnimationGraphAsset>();

            try {
                var isLoopDefinition = new AnimationGraphBlackboardDefinition("isLoop", true);
                var speedDefinition = new AnimationGraphBlackboardDefinition("speed", 1.5f);
                SetBlackboardDefinitions(graphAsset, isLoopDefinition, speedDefinition);

                var result = graphAsset.TryGetBlackboardDefinition("speed", out var definition);

                Assert.IsTrue(result);
                Assert.That(definition.Key, Is.EqualTo("speed"));
                Assert.That(definition.ValueType, Is.EqualTo(AnimationGraphValueType.Float));
                Assert.That(definition.DefaultFloatValue, Is.EqualTo(1.5f).Within(0.0001f));
                Assert.IsTrue(graphAsset.TryGetBlackboardDefaultValue("speed", out float defaultValue));
                Assert.That(defaultValue, Is.EqualTo(1.5f).Within(0.0001f));
                Assert.IsFalse(graphAsset.TryGetBlackboardDefaultValue("speed", out int _));
                Assert.IsFalse(graphAsset.TryGetBlackboardDefinition("missing", out _));
            }
            finally {
                Object.DestroyImmediate(graphAsset);
            }
        }

        /// <summary>
        /// Blackboard default value は対応する全 value type で取得できる
        /// </summary>
        [Test]
        public void TryGetBlackboardDefaultValue_SupportsAllValueTypes() {
            var graphAsset = ScriptableObject.CreateInstance<AnimationGraphAsset>();
            var vector2DefaultValue = new Vector2(1.0f, 2.0f);
            var vector3DefaultValue = new Vector3(3.0f, 4.0f, 5.0f);
            var colorDefaultValue = new Color(0.2f, 0.4f, 0.6f, 0.8f);

            try {
                SetBlackboardDefinitions(
                    graphAsset,
                    new AnimationGraphBlackboardDefinition("flag", true),
                    new AnimationGraphBlackboardDefinition("count", 12),
                    new AnimationGraphBlackboardDefinition("speed", 1.5f),
                    new AnimationGraphBlackboardDefinition("label", "idle"),
                    new AnimationGraphBlackboardDefinition("offset", vector2DefaultValue),
                    new AnimationGraphBlackboardDefinition("position", vector3DefaultValue),
                    new AnimationGraphBlackboardDefinition("tint", colorDefaultValue));

                Assert.IsTrue(graphAsset.TryGetBlackboardDefaultValue("flag", out bool flagValue));
                Assert.IsTrue(flagValue);
                Assert.IsTrue(graphAsset.TryGetBlackboardDefaultValue("count", out int countValue));
                Assert.That(countValue, Is.EqualTo(12));
                Assert.IsTrue(graphAsset.TryGetBlackboardDefaultValue("speed", out float speedValue));
                Assert.That(speedValue, Is.EqualTo(1.5f).Within(0.0001f));
                Assert.IsTrue(graphAsset.TryGetBlackboardDefaultValue("label", out string labelValue));
                Assert.That(labelValue, Is.EqualTo("idle"));
                Assert.IsTrue(graphAsset.TryGetBlackboardDefaultValue("offset", out Vector2 offsetValue));
                Assert.That(offsetValue, Is.EqualTo(vector2DefaultValue));
                Assert.IsTrue(graphAsset.TryGetBlackboardDefaultValue("position", out Vector3 positionValue));
                Assert.That(positionValue, Is.EqualTo(vector3DefaultValue));
                Assert.IsTrue(graphAsset.TryGetBlackboardDefaultValue("tint", out Color tintValue));
                Assert.That(tintValue, Is.EqualTo(colorDefaultValue));
            }
            finally {
                Object.DestroyImmediate(graphAsset);
            }
        }

        /// <summary>
        /// target 定義配列を設定
        /// </summary>
        /// <param name="graphAsset">設定対象の AnimationGraphAsset</param>
        /// <param name="keys">設定する target key 一覧</param>
        private void SetTargetDefinitions(AnimationGraphAsset graphAsset, params string[] keys) {
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
        /// Blackboard 定義配列を設定
        /// </summary>
        /// <param name="graphAsset">設定対象の AnimationGraphAsset</param>
        /// <param name="definitions">設定する Blackboard 定義一覧</param>
        private void SetBlackboardDefinitions(AnimationGraphAsset graphAsset, params AnimationGraphBlackboardDefinition[] definitions) {
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
            }

            serializedGraph.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
