using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// AnimationGraphAsset の schema 定義テスト
    /// </summary>
    public sealed class AnimationGraphAssetSchemaTests {
        /// <summary>AnimationGraphAsset の target schema フィールド名</summary>
        private const string TargetSchemaPropertyName = "_targetSchema";
        /// <summary>AnimationGraphTargetSchema の target 定義配列フィールド名</summary>
        private const string TargetDefinitionsPropertyName = "_definitions";
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

        /// <summary>
        /// target 定義は key だけで取得できる
        /// </summary>
        [Test]
        public void TryGetTargetDefinition_ReturnsDefinitionByKey() {
            var graphAsset = ScriptableObject.CreateInstance<AnimationGraphAsset>();
            var targetSchema = ScriptableObject.CreateInstance<AnimationGraphTargetSchema>();

            try {
                SetTargetDefinitions(targetSchema, "actor", "camera");
                SetTargetSchema(graphAsset, targetSchema);

                var result = graphAsset.TryGetTargetDefinition("camera", out var definition);

                Assert.IsTrue(result);
                Assert.That(definition.Key, Is.EqualTo("camera"));
                Assert.That(definition.MonoScriptGuid, Is.Empty);
                Assert.IsFalse(graphAsset.TryGetTargetDefinition("missing", out _));
            }
            finally {
                Object.DestroyImmediate(graphAsset);
                Object.DestroyImmediate(targetSchema);
            }
        }

        /// <summary>
        /// Blackboard 定義は key と value type を取得できる
        /// </summary>
        [Test]
        public void TryGetBlackboardDefinition_ReturnsDefinitionByKey() {
            var graphAsset = ScriptableObject.CreateInstance<AnimationGraphAsset>();

            try {
                var isLoopDefinition = new BlackboardDefinition("isLoop", true);
                var speedDefinition = new BlackboardDefinition("speed", 1.5f);
                SetBlackboardDefinitions(graphAsset, isLoopDefinition, speedDefinition);

                var result = graphAsset.TryGetBlackboardDefinition("speed", out var definition);

                Assert.IsTrue(result);
                Assert.That(definition.Key, Is.EqualTo("speed"));
                Assert.That(definition.ValueType, Is.EqualTo(BlackboardValueType.Float));
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
            var vector4DefaultValue = new Vector4(6.0f, 7.0f, 8.0f, 9.0f);

            try {
                SetBlackboardDefinitions(
                    graphAsset,
                    new BlackboardDefinition("flag", true),
                    new BlackboardDefinition("count", 12),
                    new BlackboardDefinition("speed", 1.5f),
                    new BlackboardDefinition("label", "idle"),
                    new BlackboardDefinition("offset", vector2DefaultValue),
                    new BlackboardDefinition("position", vector3DefaultValue),
                    new BlackboardDefinition("tint", colorDefaultValue),
                    new BlackboardDefinition("bounds", vector4DefaultValue));

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
                Assert.IsTrue(graphAsset.TryGetBlackboardDefaultValue("bounds", out Vector4 boundsValue));
                Assert.That(boundsValue, Is.EqualTo(vector4DefaultValue));
            }
            finally {
                Object.DestroyImmediate(graphAsset);
            }
        }

        /// <summary>
        /// target 定義配列を設定
        /// </summary>
        /// <param name="targetSchema">設定対象の target schema</param>
        /// <param name="keys">設定する target key 一覧</param>
        private void SetTargetDefinitions(AnimationGraphTargetSchema targetSchema, params string[] keys) {
            var serializedSchema = new SerializedObject(targetSchema);
            var definitionsProperty = serializedSchema.FindProperty(TargetDefinitionsPropertyName);
            definitionsProperty.arraySize = keys.Length;
            for (var i = 0; i < keys.Length; i++) {
                var definitionProperty = definitionsProperty.GetArrayElementAtIndex(i);
                definitionProperty.FindPropertyRelative(KeyPropertyName).stringValue = keys[i];
                definitionProperty.FindPropertyRelative(MonoScriptGuidPropertyName).stringValue = string.Empty;
            }

            serializedSchema.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// AnimationGraphAsset の target schema を設定
        /// </summary>
        /// <param name="graphAsset">設定対象の AnimationGraphAsset</param>
        /// <param name="targetSchema">設定する target schema</param>
        private void SetTargetSchema(AnimationGraphAsset graphAsset, AnimationGraphTargetSchema targetSchema) {
            var serializedGraph = new SerializedObject(graphAsset);
            serializedGraph.FindProperty(TargetSchemaPropertyName).objectReferenceValue = targetSchema;
            serializedGraph.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Blackboard 定義配列を設定
        /// </summary>
        /// <param name="graphAsset">設定対象の AnimationGraphAsset</param>
        /// <param name="definitions">設定する Blackboard 定義一覧</param>
        private void SetBlackboardDefinitions(AnimationGraphAsset graphAsset, params BlackboardDefinition[] definitions) {
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
    }
}
