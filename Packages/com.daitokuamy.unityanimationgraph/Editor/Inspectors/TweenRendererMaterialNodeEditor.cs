using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Renderer Material Tween node の Inspector 基底クラス
    /// </summary>
    internal abstract class TweenRendererMaterialNodeEditor : UnityEditor.Editor {
        /// <summary>Material index property name</summary>
        private const string MaterialIndexPropertyName = "_materialIndex";
        /// <summary>Shader property name property name</summary>
        private const string ShaderPropertyNamePropertyName = "_propertyName";

        /// <summary>
        /// Node で選択可能な Shader property type か判定
        /// </summary>
        /// <param name="propertyType">Shader property type</param>
        /// <returns>選択可能な場合は true</returns>
        protected abstract bool IsCompatiblePropertyType(ShaderPropertyType propertyType);

        /// <inheritdoc/>
        public override void OnInspectorGUI() {
            serializedObject.Update();
            var renderer = targets.Length == 1 ? FindBoundRenderer((ActionNode)target) : null;
            var property = serializedObject.GetIterator();
            var enterChildren = true;
            while (property.NextVisible(enterChildren)) {
                enterChildren = false;
                using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script")) {
                    if (property.propertyPath == MaterialIndexPropertyName && renderer != null) {
                        DrawMaterialPopup(property, renderer);
                    }
                    else if (property.propertyPath == ShaderPropertyNamePropertyName && renderer != null) {
                        DrawShaderPropertyPopup(property, renderer, serializedObject.FindProperty(MaterialIndexPropertyName));
                    }
                    else {
                        EditorGUILayout.PropertyField(property, true);
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Renderer の Material 一覧を Popup として描画
        /// </summary>
        /// <param name="property">Material index property</param>
        /// <param name="renderer">候補の取得元 Renderer</param>
        private void DrawMaterialPopup(SerializedProperty property, Renderer renderer) {
            var materials = renderer.sharedMaterials;
            if (materials.Length == 0) {
                EditorGUILayout.PropertyField(property, true);
                return;
            }

            var labels = new string[materials.Length];
            for (var i = 0; i < materials.Length; i++) {
                var materialName = materials[i] != null ? materials[i].name : "None";
                labels[i] = $"{i}: {materialName}";
            }

            var selectedIndex = Mathf.Clamp(property.intValue, 0, materials.Length - 1);
            property.intValue = EditorGUILayout.Popup(new GUIContent("Material"), selectedIndex, labels);
        }

        /// <summary>
        /// Material の Shader property 一覧を Popup として描画
        /// </summary>
        /// <param name="property">Shader property name property</param>
        /// <param name="renderer">Material の取得元 Renderer</param>
        /// <param name="materialIndexProperty">Material index property</param>
        private void DrawShaderPropertyPopup(SerializedProperty property, Renderer renderer, SerializedProperty materialIndexProperty) {
            var materials = renderer.sharedMaterials;
            var materialIndex = materialIndexProperty?.intValue ?? -1;
            if (materialIndex < 0 || materialIndex >= materials.Length || materials[materialIndex] == null) {
                EditorGUILayout.PropertyField(property, true);
                return;
            }

            var choices = GetShaderPropertyChoices(materials[materialIndex].shader, property.stringValue);
            if (choices.Count == 0) {
                EditorGUILayout.PropertyField(property, true);
                return;
            }

            var selectedIndex = Mathf.Max(0, choices.FindIndex(choice => choice.Name == property.stringValue));
            var labels = new string[choices.Count];
            for (var i = 0; i < choices.Count; i++) {
                labels[i] = choices[i].Label;
            }

            selectedIndex = EditorGUILayout.Popup(new GUIContent("Shader Property"), selectedIndex, labels);
            property.stringValue = choices[selectedIndex].Name;
        }

        /// <summary>
        /// Node で選択可能な Shader property の一覧を取得
        /// </summary>
        /// <param name="shader">候補の取得元 Shader</param>
        /// <param name="currentPropertyName">現在の Shader property name</param>
        /// <returns>選択可能な Shader property の一覧</returns>
        private List<ShaderPropertyChoice> GetShaderPropertyChoices(Shader shader, string currentPropertyName) {
            var choices = new List<ShaderPropertyChoice> { new(string.Empty, "None") };
            if (shader != null) {
                for (var i = 0; i < shader.GetPropertyCount(); i++) {
                    var propertyType = shader.GetPropertyType(i);
                    if (!IsCompatiblePropertyType(propertyType)
                        || propertyType == ShaderPropertyType.Texture
                        && (shader.GetPropertyFlags(i) & ShaderPropertyFlags.NoScaleOffset) != 0) {
                        continue;
                    }

                    var propertyName = shader.GetPropertyName(i);
                    var description = shader.GetPropertyDescription(i);
                    choices.Add(new ShaderPropertyChoice(propertyName, $"{description} ({propertyName})"));
                }
            }

            if (!string.IsNullOrEmpty(currentPropertyName) && choices.FindIndex(choice => choice.Name == currentPropertyName) < 0) {
                choices.Insert(1, new ShaderPropertyChoice(currentPropertyName, $"{currentPropertyName} (Missing)"));
            }

            return choices;
        }

        /// <summary>
        /// Node の Binding に対応する Renderer を検索
        /// </summary>
        /// <param name="node">検索対象 Node</param>
        /// <returns>Binding に対応する Renderer。見つからない場合は null</returns>
        private Renderer FindBoundRenderer(ActionNode node) {
            if (node == null || node.TargetReference.Kind != TargetReferenceKind.Binding || string.IsNullOrEmpty(node.TargetKey)) {
                return null;
            }

            var runners = FindObjectsByType<AnimationGraphRunner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < runners.Length; i++) {
                var runner = runners[i];
                if (!ContainsNode(runner.GraphAsset, node) || !runner.TryGetTarget(node.TargetKey, out Renderer renderer)) {
                    continue;
                }

                return renderer;
            }

            return null;
        }

        /// <summary>
        /// AnimationGraphAsset が指定 Node を含むか判定
        /// </summary>
        /// <param name="graphAsset">検索対象 AnimationGraphAsset</param>
        /// <param name="node">検索する Node</param>
        /// <returns>指定 Node を含む場合は true</returns>
        private bool ContainsNode(AnimationGraphAsset graphAsset, Node node) {
            if (graphAsset == null) {
                return false;
            }

            var nodes = graphAsset.Nodes;
            for (var i = 0; i < nodes.Count; i++) {
                if (nodes[i] == node) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Shader property Popup の選択肢
        /// </summary>
        private readonly struct ShaderPropertyChoice {
            /// <summary>Shader property name</summary>
            public string Name { get; }
            /// <summary>Popup に表示する label</summary>
            public string Label { get; }

            /// <summary>
            /// ShaderPropertyChoice を生成
            /// </summary>
            /// <param name="name">Shader property name</param>
            /// <param name="label">Popup に表示する label</param>
            public ShaderPropertyChoice(string name, string label) {
                Name = name;
                Label = label;
            }
        }
    }

    /// <summary>Renderer Material Float Tween node の Inspector</summary>
    [CustomEditor(typeof(TweenRendererMaterialFloatNode)), CanEditMultipleObjects]
    internal sealed class TweenRendererMaterialFloatNodeEditor : TweenRendererMaterialNodeEditor {
        /// <inheritdoc/>
        protected override bool IsCompatiblePropertyType(ShaderPropertyType propertyType) {
            return propertyType == ShaderPropertyType.Float || propertyType == ShaderPropertyType.Range;
        }
    }

    /// <summary>Renderer Material Color Tween node の Inspector</summary>
    [CustomEditor(typeof(TweenRendererMaterialColorNode)), CanEditMultipleObjects]
    internal sealed class TweenRendererMaterialColorNodeEditor : TweenRendererMaterialNodeEditor {
        /// <inheritdoc/>
        protected override bool IsCompatiblePropertyType(ShaderPropertyType propertyType) {
            return propertyType == ShaderPropertyType.Color;
        }
    }

    /// <summary>Renderer Material Vector Tween node の Inspector</summary>
    [CustomEditor(typeof(TweenRendererMaterialVectorNode)), CanEditMultipleObjects]
    internal sealed class TweenRendererMaterialVectorNodeEditor : TweenRendererMaterialNodeEditor {
        /// <inheritdoc/>
        protected override bool IsCompatiblePropertyType(ShaderPropertyType propertyType) {
            return propertyType == ShaderPropertyType.Vector;
        }
    }

    /// <summary>Renderer Material Texture Offset Tween node の Inspector</summary>
    [CustomEditor(typeof(TweenRendererMaterialTextureOffsetNode)), CanEditMultipleObjects]
    internal sealed class TweenRendererMaterialTextureOffsetNodeEditor : TweenRendererMaterialNodeEditor {
        /// <inheritdoc/>
        protected override bool IsCompatiblePropertyType(ShaderPropertyType propertyType) {
            return propertyType == ShaderPropertyType.Texture;
        }
    }

    /// <summary>Renderer Material Texture Scale Tween node の Inspector</summary>
    [CustomEditor(typeof(TweenRendererMaterialTextureScaleNode)), CanEditMultipleObjects]
    internal sealed class TweenRendererMaterialTextureScaleNodeEditor : TweenRendererMaterialNodeEditor {
        /// <inheritdoc/>
        protected override bool IsCompatiblePropertyType(ShaderPropertyType propertyType) {
            return propertyType == ShaderPropertyType.Texture;
        }
    }
}
