using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// Renderer Material Tween node の EditMode テスト
    /// </summary>
    public sealed class TweenRendererMaterialNodeTests {
        /// <summary>テスト対象の Target key</summary>
        private const string TargetKey = "target";
        /// <summary>テスト用 Shader name</summary>
        private const string ShaderName = "Hidden/UnityAnimationGraph/MaterialTweenTest";

        /// <summary>
        /// Float、Color、Vector node は MaterialPropertyBlock に補間値を設定
        /// </summary>
        [Test]
        public void ValueNodes_SetPreviewPropertyBlock() {
            var floatNode = ScriptableObject.CreateInstance<TweenRendererMaterialFloatNode>();
            var colorNode = ScriptableObject.CreateInstance<TweenRendererMaterialColorNode>();
            var vectorNode = ScriptableObject.CreateInstance<TweenRendererMaterialVectorNode>();
            var gameObject = new GameObject("Target", typeof(MeshRenderer));
            Material material = null;
            try {
                var renderer = gameObject.GetComponent<MeshRenderer>();
                material = new Material(Shader.Find(ShaderName));
                renderer.sharedMaterial = material;
                var context = CreateContext(renderer);
                ConfigureNode(floatNode, "_FloatValue");
                ConfigureNode(colorNode, "_ColorValue");
                ConfigureNode(vectorNode, "_VectorValue");
                SetFloatTween(floatNode, 0.0f, 2.0f);
                SetColorTween(colorNode, Color.black, Color.white);
                SetVector4Tween(vectorNode, Vector4.zero, new Vector4(2.0f, 4.0f, 6.0f, 8.0f));

                floatNode.ExecuteEvaluate(0, 1.0f, 2.0f, context);
                colorNode.ExecuteEvaluate(0, 1.0f, 2.0f, context);
                vectorNode.ExecuteEvaluate(0, 1.0f, 2.0f, context);

                var propertyBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock, 0);
                Assert.That(propertyBlock.GetFloat("_FloatValue"), Is.EqualTo(1.0f).Within(0.0001f));
                Assert.That(propertyBlock.GetColor("_ColorValue"), Is.EqualTo(new Color(0.5f, 0.5f, 0.5f, 0.5f)).Using(ColorComparer.Instance));
                Assert.That(propertyBlock.GetVector("_VectorValue"), Is.EqualTo(new Vector4(1.0f, 2.0f, 3.0f, 4.0f)).Using(Vector4Comparer.Instance));
            }
            finally {
                DestroyObject(floatNode);
                DestroyObject(colorNode);
                DestroyObject(vectorNode);
                DestroyObject(material);
                DestroyObject(gameObject);
            }
        }

        /// <summary>
        /// Texture Offset と Scale node は同じ ST property の要素を維持して補間
        /// </summary>
        [Test]
        public void TextureTransformNodes_SetPreviewPropertyBlock() {
            var offsetNode = ScriptableObject.CreateInstance<TweenRendererMaterialTextureOffsetNode>();
            var scaleNode = ScriptableObject.CreateInstance<TweenRendererMaterialTextureScaleNode>();
            var gameObject = new GameObject("Target", typeof(MeshRenderer));
            Material material = null;
            try {
                var renderer = gameObject.GetComponent<MeshRenderer>();
                material = new Material(Shader.Find(ShaderName));
                material.SetTextureOffset("_TextureValue", new Vector2(0.1f, 0.2f));
                material.SetTextureScale("_TextureValue", new Vector2(1.0f, 2.0f));
                renderer.sharedMaterial = material;
                var context = CreateContext(renderer);
                ConfigureNode(offsetNode, "_TextureValue");
                ConfigureNode(scaleNode, "_TextureValue");
                SetVector2Tween(offsetNode, Vector2.zero, new Vector2(0.4f, 0.8f));
                SetVector2Tween(scaleNode, Vector2.one, new Vector2(3.0f, 5.0f));

                offsetNode.ExecuteEvaluate(0, 1.0f, 2.0f, context);
                scaleNode.ExecuteEvaluate(0, 1.0f, 2.0f, context);

                var propertyBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock, 0);
                Assert.That(propertyBlock.GetVector("_TextureValue_ST"), Is.EqualTo(new Vector4(2.0f, 3.0f, 0.2f, 0.4f)).Using(Vector4Comparer.Instance));
            }
            finally {
                DestroyObject(offsetNode);
                DestroyObject(scaleNode);
                DestroyObject(material);
                DestroyObject(gameObject);
            }
        }

        /// <summary>
        /// Preview cleanup は対象 Material slot の PropertyBlock をクリア
        /// </summary>
        [Test]
        public void CleanupPreview_ClearsPropertyBlock() {
            var node = ScriptableObject.CreateInstance<TweenRendererMaterialFloatNode>();
            var gameObject = new GameObject("Target", typeof(MeshRenderer));
            Material material = null;
            try {
                var renderer = gameObject.GetComponent<MeshRenderer>();
                material = new Material(Shader.Find(ShaderName));
                renderer.sharedMaterial = material;
                var context = CreateContext(renderer);
                ConfigureNode(node, "_FloatValue");
                SetFloatTween(node, 0.0f, 2.0f);
                node.ExecuteEvaluate(0, 1.0f, 2.0f, context);

                ((IPreviewCleanupNode)node).CleanupPreview(context);

                var propertyBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propertyBlock, 0);
                Assert.That(propertyBlock.isEmpty, Is.True);
            }
            finally {
                DestroyObject(node);
                DestroyObject(material);
                DestroyObject(gameObject);
            }
        }

        /// <summary>
        /// Renderer を登録したテスト用 Context を生成
        /// </summary>
        /// <param name="renderer">登録する Renderer</param>
        /// <returns>生成したテスト用 Context</returns>
        private static TestAnimationGraphContext CreateContext(Renderer renderer) {
            var context = new TestAnimationGraphContext();
            context.SetTarget(TargetKey, renderer);
            return context;
        }

        /// <summary>
        /// Material Tween node の共通設定を更新
        /// </summary>
        /// <param name="node">設定対象 Node</param>
        /// <param name="propertyName">Shader property name</param>
        private static void ConfigureNode(ActionNode node, string propertyName) {
            var serializedNode = new SerializedObject(node);
            serializedNode.FindProperty("_targetKey").stringValue = TargetKey;
            serializedNode.FindProperty("_materialIndex").intValue = 0;
            serializedNode.FindProperty("_propertyName").stringValue = propertyName;
            serializedNode.FindProperty("_duration").floatValue = 2.0f;
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Float Tween の直値を設定
        /// </summary>
        /// <param name="node">設定対象 Node</param>
        /// <param name="before">Tween 開始値</param>
        /// <param name="after">Tween 終了値</param>
        private static void SetFloatTween(Node node, float before, float after) {
            var serializedNode = new SerializedObject(node);
            var tween = serializedNode.FindProperty("_tween");
            SetParameter(tween.FindPropertyRelative("_before"), before);
            SetParameter(tween.FindPropertyRelative("_after"), after);
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Color Tween の直値を設定
        /// </summary>
        /// <param name="node">設定対象 Node</param>
        /// <param name="before">Tween 開始値</param>
        /// <param name="after">Tween 終了値</param>
        private static void SetColorTween(Node node, Color before, Color after) {
            var serializedNode = new SerializedObject(node);
            var tween = serializedNode.FindProperty("_tween");
            SetParameter(tween.FindPropertyRelative("_before"), before);
            SetParameter(tween.FindPropertyRelative("_after"), after);
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Vector4 Tween の直値を設定
        /// </summary>
        /// <param name="node">設定対象 Node</param>
        /// <param name="before">Tween 開始値</param>
        /// <param name="after">Tween 終了値</param>
        private static void SetVector4Tween(Node node, Vector4 before, Vector4 after) {
            var serializedNode = new SerializedObject(node);
            var tween = serializedNode.FindProperty("_tween");
            SetParameter(tween.FindPropertyRelative("_before"), before);
            SetParameter(tween.FindPropertyRelative("_after"), after);
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Vector2 Tween の直値を設定
        /// </summary>
        /// <param name="node">設定対象 Node</param>
        /// <param name="before">Tween 開始値</param>
        /// <param name="after">Tween 終了値</param>
        private static void SetVector2Tween(Node node, Vector2 before, Vector2 after) {
            var serializedNode = new SerializedObject(node);
            var tween = serializedNode.FindProperty("_tween");
            SetParameter(tween.FindPropertyRelative("_before"), before);
            SetParameter(tween.FindPropertyRelative("_after"), after);
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Float parameter に直値を設定
        /// </summary>
        /// <param name="parameter">設定対象 parameter</param>
        /// <param name="value">設定値</param>
        private static void SetParameter(SerializedProperty parameter, float value) {
            parameter.FindPropertyRelative("_source").enumValueIndex = (int)ParameterSource.Value;
            parameter.FindPropertyRelative("_value").floatValue = value;
        }

        /// <summary>
        /// Color parameter に直値を設定
        /// </summary>
        /// <param name="parameter">設定対象 parameter</param>
        /// <param name="value">設定値</param>
        private static void SetParameter(SerializedProperty parameter, Color value) {
            parameter.FindPropertyRelative("_source").enumValueIndex = (int)ParameterSource.Value;
            parameter.FindPropertyRelative("_value").colorValue = value;
        }

        /// <summary>
        /// Vector4 parameter に直値を設定
        /// </summary>
        /// <param name="parameter">設定対象 parameter</param>
        /// <param name="value">設定値</param>
        private static void SetParameter(SerializedProperty parameter, Vector4 value) {
            parameter.FindPropertyRelative("_source").enumValueIndex = (int)ParameterSource.Value;
            parameter.FindPropertyRelative("_value").vector4Value = value;
        }

        /// <summary>
        /// Vector2 parameter に直値を設定
        /// </summary>
        /// <param name="parameter">設定対象 parameter</param>
        /// <param name="value">設定値</param>
        private static void SetParameter(SerializedProperty parameter, Vector2 value) {
            parameter.FindPropertyRelative("_source").enumValueIndex = (int)ParameterSource.Value;
            parameter.FindPropertyRelative("_value").vector2Value = value;
        }

        /// <summary>
        /// Unity Object を即時破棄
        /// </summary>
        /// <param name="target">破棄対象 Object</param>
        private static void DestroyObject(Object target) {
            if (target != null) {
                Object.DestroyImmediate(target);
            }
        }

        /// <summary>
        /// 許容誤差を考慮して Color を比較する Comparer
        /// </summary>
        private sealed class ColorComparer : IEqualityComparer<Color> {
            /// <summary>共有 Instance</summary>
            public static readonly ColorComparer Instance = new();

            /// <inheritdoc/>
            public bool Equals(Color x, Color y) {
                return Vector4.Distance(x, y) < 0.0001f;
            }

            /// <inheritdoc/>
            public int GetHashCode(Color obj) {
                return obj.GetHashCode();
            }
        }

        /// <summary>
        /// 許容誤差を考慮して Vector4 を比較する Comparer
        /// </summary>
        private sealed class Vector4Comparer : IEqualityComparer<Vector4> {
            /// <summary>共有 Instance</summary>
            public static readonly Vector4Comparer Instance = new();

            /// <inheritdoc/>
            public bool Equals(Vector4 x, Vector4 y) {
                return Vector4.Distance(x, y) < 0.0001f;
            }

            /// <inheritdoc/>
            public int GetHashCode(Vector4 obj) {
                return obj.GetHashCode();
            }
        }
    }
}
