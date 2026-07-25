using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// target key を AnimationGraphAsset の target schema から選択する Drawer
    /// </summary>
    [CustomPropertyDrawer(typeof(TargetKeyAttribute))]
    internal sealed class TargetKeyAttributeDrawer : PropertyDrawer {
        /// <summary>空の target key 表示名</summary>
        private const string EmptyLabel = "<None>";
        /// <summary>target 定義に存在しない key の接尾辞</summary>
        private const string MissingSuffix = " (Missing)";

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType != SerializedPropertyType.String) {
                EditorGUI.PropertyField(position, property, label);
                EditorGUI.EndProperty();
                return;
            }

            var graphAsset = AnimationGraphPropertyDrawerUtility.FindGraphAsset(property.serializedObject.targetObjects);
            if (graphAsset == null) {
                EditorGUI.PropertyField(position, property, label);
                EditorGUI.EndProperty();
                return;
            }

            var currentValue = property.stringValue ?? string.Empty;
            var options = CreateOptions(graphAsset, currentValue, ((TargetKeyAttribute)attribute).Multiplicity);
            var displayOptions = CreateDisplayOptions(graphAsset, options);
            var selectedIndex = FindOptionIndex(options, currentValue);
            var previousShowMixedValue = EditorGUI.showMixedValue;

            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var popupPosition = EditorGUI.PrefixLabel(position, label);
            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            var nextIndex = EditorGUI.Popup(popupPosition, selectedIndex, displayOptions);
            EditorGUI.indentLevel = indentLevel;
            if (EditorGUI.EndChangeCheck()) {
                property.stringValue = options[nextIndex];
            }

            EditorGUI.showMixedValue = previousShowMixedValue;
            EditorGUI.EndProperty();
        }

        private static List<string> CreateOptions(AnimationGraphAsset graphAsset, string currentValue, TargetMultiplicity multiplicity) {
            var options = new List<string>();
            AddOption(options, string.Empty);

            var definitions = graphAsset.TargetDefinitions;
            for (var i = 0; i < definitions.Count; i++) {
                if (definitions[i].Multiplicity != multiplicity) {
                    continue;
                }

                AddOption(options, definitions[i].Key);
            }

            if (!string.IsNullOrEmpty(currentValue)) {
                AddOption(options, currentValue);
            }

            return options;
        }

        private static string[] CreateDisplayOptions(AnimationGraphAsset graphAsset, IReadOnlyList<string> options) {
            var displayOptions = new string[options.Count];
            for (var i = 0; i < options.Count; i++) {
                var option = options[i];
                if (string.IsNullOrEmpty(option)) {
                    displayOptions[i] = EmptyLabel;
                    continue;
                }

                displayOptions[i] = graphAsset.TryGetTargetDefinition(option, out _) ? option : option + MissingSuffix;
            }

            return displayOptions;
        }

        private static int FindOptionIndex(IReadOnlyList<string> options, string value) {
            for (var i = 0; i < options.Count; i++) {
                if (options[i] == value) {
                    return i;
                }
            }

            return 0;
        }

        private static void AddOption(List<string> options, string option) {
            option = option ?? string.Empty;
            for (var i = 0; i < options.Count; i++) {
                if (options[i] == option) {
                    return;
                }
            }

            options.Add(option);
        }
    }
}
