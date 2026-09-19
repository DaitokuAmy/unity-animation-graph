using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// EachNode の Inspector
    /// </summary>
    [CustomEditor(typeof(EachNode)), CanEditMultipleObjects]
    internal sealed class EachNodeEditor : UnityEditor.Editor {
        private const string StaggerModePropertyName = "_staggerMode";
        private const string StaggerDelayPropertyName = "_staggerDelay";
        private const string MinStaggerDelayPropertyName = "_minStaggerDelay";
        private const string MaxStaggerDelayPropertyName = "_maxStaggerDelay";

        /// <inheritdoc/>
        public override void OnInspectorGUI() {
            serializedObject.Update();
            var staggerModeProperty = serializedObject.FindProperty(StaggerModePropertyName);
            var property = serializedObject.GetIterator();
            var enterChildren = true;
            while (property.NextVisible(enterChildren)) {
                enterChildren = false;
                if (IsStaggerValueProperty(property.propertyPath)) {
                    continue;
                }

                using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script")) {
                    if (property.propertyPath == StaggerModePropertyName) {
                        EditorGUILayout.PropertyField(property, new GUIContent("Stagger"), true);
                    }
                    else {
                        EditorGUILayout.PropertyField(property, true);
                    }
                }

                if (property.propertyPath == StaggerModePropertyName) {
                    DrawStaggerValue(staggerModeProperty);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStaggerValue(SerializedProperty staggerModeProperty) {
            if (staggerModeProperty == null || staggerModeProperty.hasMultipleDifferentValues) {
                return;
            }

            if ((EachStaggerMode)staggerModeProperty.enumValueIndex == EachStaggerMode.Interval) {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(StaggerDelayPropertyName), new GUIContent("Interval"));
                return;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty(MinStaggerDelayPropertyName), new GUIContent("Min Delay"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(MaxStaggerDelayPropertyName), new GUIContent("Max Delay"));
        }

        private static bool IsStaggerValueProperty(string propertyPath) {
            return propertyPath == StaggerDelayPropertyName
                || propertyPath == MinStaggerDelayPropertyName
                || propertyPath == MaxStaggerDelayPropertyName;
        }
    }
}
