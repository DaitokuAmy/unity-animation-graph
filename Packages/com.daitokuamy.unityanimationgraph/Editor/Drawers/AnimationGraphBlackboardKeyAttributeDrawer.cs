using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Drawer for selecting a blackboard key from an AnimationGraphAsset definition list.
    /// </summary>
    [CustomPropertyDrawer(typeof(AnimationGraphBlackboardKeyAttribute))]
    internal sealed class AnimationGraphBlackboardKeyAttributeDrawer : PropertyDrawer {
        /// <summary>Display label for an empty blackboard key.</summary>
        private const string EmptyLabel = "<None>";
        /// <summary>Suffix for a key that does not exist in blackboard definitions.</summary>
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

            var blackboardKeyAttribute = (AnimationGraphBlackboardKeyAttribute)attribute;
            var currentValue = property.stringValue ?? string.Empty;
            var options = CreateOptions(graphAsset, blackboardKeyAttribute, currentValue);
            var displayOptions = CreateDisplayOptions(graphAsset, blackboardKeyAttribute, options);
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

        private static List<string> CreateOptions(AnimationGraphAsset graphAsset, AnimationGraphBlackboardKeyAttribute blackboardKeyAttribute, string currentValue) {
            var options = new List<string>();
            AddOption(options, string.Empty);

            var definitions = graphAsset.BlackboardDefinitions;
            for (var i = 0; i < definitions.Count; i++) {
                var definition = definitions[i];
                if (!MatchesValueTypeFilter(blackboardKeyAttribute, definition)) {
                    continue;
                }

                AddOption(options, definition.Key);
            }

            if (!string.IsNullOrEmpty(currentValue)) {
                AddOption(options, currentValue);
            }

            return options;
        }

        private static string[] CreateDisplayOptions(AnimationGraphAsset graphAsset, AnimationGraphBlackboardKeyAttribute blackboardKeyAttribute, IReadOnlyList<string> options) {
            var displayOptions = new string[options.Count];
            for (var i = 0; i < options.Count; i++) {
                var option = options[i];
                if (string.IsNullOrEmpty(option)) {
                    displayOptions[i] = EmptyLabel;
                    continue;
                }

                if (!graphAsset.TryGetBlackboardDefinition(option, out var definition)) {
                    displayOptions[i] = option + MissingSuffix;
                    continue;
                }

                displayOptions[i] = MatchesValueTypeFilter(blackboardKeyAttribute, definition)
                    ? option
                    : $"{option} ({definition.ValueType})";
            }

            return displayOptions;
        }

        private static bool MatchesValueTypeFilter(AnimationGraphBlackboardKeyAttribute blackboardKeyAttribute, AnimationGraphBlackboardDefinition definition) {
            return !blackboardKeyAttribute.HasValueTypeFilter || definition.ValueType == blackboardKeyAttribute.ValueType;
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
