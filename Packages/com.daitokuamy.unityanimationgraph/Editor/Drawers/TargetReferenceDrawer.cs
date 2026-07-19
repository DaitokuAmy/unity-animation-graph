using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// TargetReference を編集し、scope node ID を参照先 node 名として表示する Drawer
    /// </summary>
    [CustomPropertyDrawer(typeof(TargetReference))]
    internal sealed class TargetReferenceDrawer : PropertyDrawer {
        private const string KindPropertyName = "_kind";
        private const string TargetKeyPropertyName = "_targetKey";
        private const string ScopeNodeIdPropertyName = "_scopeNodeId";
        private const string LegacyTargetKeyPropertyName = "_targetKey";
        private const string MissingScopeLabel = "<Missing Scope>";
        private const string NoScopeLabel = "<None>";
        private const string NoTargetLabel = "<None>";
        private const string MissingTargetSuffix = " (Missing)";

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            if (!property.isExpanded) {
                return EditorGUIUtility.singleLineHeight;
            }

            var kindProperty = property.FindPropertyRelative(KindPropertyName);
            var kind = (TargetReferenceKind)kindProperty.enumValueIndex;
            var childCount = kind == TargetReferenceKind.CollectionItem ? 3 : 2;
            return EditorGUIUtility.singleLineHeight * (childCount + 1) + EditorGUIUtility.standardVerticalSpacing * childCount;
        }

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var linePosition = new Rect(position.x, position.y, position.width, lineHeight);
            property.isExpanded = EditorGUI.Foldout(linePosition, property.isExpanded, label, true);
            if (!property.isExpanded) {
                EditorGUI.EndProperty();
                return;
            }

            var kindProperty = property.FindPropertyRelative(KindPropertyName);
            var targetKeyProperty = property.FindPropertyRelative(TargetKeyPropertyName);
            var scopeNodeIdProperty = property.FindPropertyRelative(ScopeNodeIdPropertyName);
            MigrateLegacyTargetKey(property, kindProperty, targetKeyProperty);
            using (new EditorGUI.IndentLevelScope()) {
                linePosition.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(linePosition, kindProperty, new GUIContent("Kind"));
                if (EditorGUI.EndChangeCheck()) {
                    targetKeyProperty.stringValue = string.Empty;
                    scopeNodeIdProperty.stringValue = string.Empty;
                }

                linePosition.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
                DrawTargetKey(linePosition, property, targetKeyProperty, (TargetReferenceKind)kindProperty.enumValueIndex);

                if ((TargetReferenceKind)kindProperty.enumValueIndex == TargetReferenceKind.CollectionItem) {
                    linePosition.y += lineHeight + EditorGUIUtility.standardVerticalSpacing;
                    using (new EditorGUI.DisabledScope(true)) {
                        EditorGUI.TextField(linePosition, new GUIContent("Scope Node"), GetScopeNodeDisplayName(property, scopeNodeIdProperty));
                    }
                }
            }

            EditorGUI.EndProperty();
        }

        private static void MigrateLegacyTargetKey(SerializedProperty property, SerializedProperty kindProperty, SerializedProperty targetKeyProperty) {
            if ((TargetReferenceKind)kindProperty.enumValueIndex != TargetReferenceKind.Binding
                || !string.IsNullOrEmpty(targetKeyProperty.stringValue)) {
                return;
            }

            var legacyTargetKeyProperty = property.serializedObject.FindProperty(LegacyTargetKeyPropertyName);
            if (legacyTargetKeyProperty == null || string.IsNullOrEmpty(legacyTargetKeyProperty.stringValue)) {
                return;
            }

            targetKeyProperty.stringValue = legacyTargetKeyProperty.stringValue;
            legacyTargetKeyProperty.stringValue = string.Empty;
        }

        private static void DrawTargetKey(Rect position, SerializedProperty property, SerializedProperty targetKeyProperty, TargetReferenceKind kind) {
            var label = new GUIContent("Target Key");
            var graphAsset = AnimationGraphPropertyDrawerUtility.FindGraphAsset(property.serializedObject.targetObjects);
            if (graphAsset == null) {
                EditorGUI.PropertyField(position, targetKeyProperty, label);
                return;
            }

            var currentValue = targetKeyProperty.stringValue ?? string.Empty;
            var multiplicity = kind == TargetReferenceKind.CollectionItem
                ? TargetMultiplicity.Collection
                : TargetMultiplicity.Single;
            var options = CreateTargetKeyOptions(graphAsset, currentValue, multiplicity);
            var displayOptions = CreateTargetKeyDisplayOptions(graphAsset, options);
            var selectedIndex = FindTargetKeyIndex(options, currentValue);
            var popupPosition = EditorGUI.PrefixLabel(position, label);
            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            var nextIndex = EditorGUI.Popup(popupPosition, selectedIndex, displayOptions);
            EditorGUI.indentLevel = indentLevel;
            targetKeyProperty.stringValue = options[nextIndex];
        }

        private static List<string> CreateTargetKeyOptions(AnimationGraphAsset graphAsset, string currentValue, TargetMultiplicity multiplicity) {
            var options = new List<string> { string.Empty };
            var definitions = graphAsset.TargetDefinitions;
            for (var i = 0; i < definitions.Count; i++) {
                if (definitions[i].Multiplicity == multiplicity) {
                    AddTargetKeyOption(options, definitions[i].Key);
                }
            }

            AddTargetKeyOption(options, currentValue);
            return options;
        }

        private static string[] CreateTargetKeyDisplayOptions(AnimationGraphAsset graphAsset, IReadOnlyList<string> options) {
            var displayOptions = new string[options.Count];
            for (var i = 0; i < options.Count; i++) {
                var option = options[i];
                if (string.IsNullOrEmpty(option)) {
                    displayOptions[i] = NoTargetLabel;
                }
                else {
                    displayOptions[i] = graphAsset.TryGetTargetDefinition(option, out _)
                        ? option
                        : option + MissingTargetSuffix;
                }
            }

            return displayOptions;
        }

        private static int FindTargetKeyIndex(IReadOnlyList<string> options, string value) {
            for (var i = 0; i < options.Count; i++) {
                if (options[i] == value) {
                    return i;
                }
            }

            return 0;
        }

        private static void AddTargetKeyOption(List<string> options, string option) {
            if (string.IsNullOrEmpty(option)) {
                return;
            }

            for (var i = 0; i < options.Count; i++) {
                if (options[i] == option) {
                    return;
                }
            }

            options.Add(option);
        }

        private static string GetScopeNodeDisplayName(SerializedProperty property, SerializedProperty scopeNodeIdProperty) {
            if (scopeNodeIdProperty.hasMultipleDifferentValues) {
                return "—";
            }

            var scopeNodeId = scopeNodeIdProperty.stringValue;
            if (string.IsNullOrEmpty(scopeNodeId)) {
                return NoScopeLabel;
            }

            var graphAsset = AnimationGraphPropertyDrawerUtility.FindGraphAsset(property.serializedObject.targetObjects);
            return graphAsset != null && graphAsset.TryGetNode(scopeNodeId, out var scopeNode) && scopeNode != null
                ? scopeNode.DisplayName
                : MissingScopeLabel;
        }
    }
}
