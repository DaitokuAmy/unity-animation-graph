using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// NodeSignalPortSettings を横並びの Toggle button として表示する Drawer
    /// </summary>
    [CustomPropertyDrawer(typeof(NodeSignalPortSettings))]
    internal sealed class NodeSignalPortSettingsDrawer : PropertyDrawer {
        private const string EnterEnabledPropertyName = "_enterEnabled";
        private const string ExitEnabledPropertyName = "_exitEnabled";
        private const string SignalPortSettingsVersionPropertyName = "_signalPortSettingsVersion";
        private const string LegacyEnableEnterSignalPortPropertyName = "_enableEnterSignalPort";
        private const string LegacyEnableExitSignalPortPropertyName = "_enableExitSignalPort";
        private const string EnterSignalsPropertyName = "_enterSignals";
        private const string ExitSignalsPropertyName = "_exitSignals";
        private const float ButtonSpacing = 0.0f;

        private static readonly GUIContent EnterLabel = new("Enter");
        private static readonly GUIContent ExitLabel = new("Exit");

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight;
        }

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            var enterProperty = property.FindPropertyRelative(EnterEnabledPropertyName);
            var exitProperty = property.FindPropertyRelative(ExitEnabledPropertyName);
            if (enterProperty == null || exitProperty == null) {
                EditorGUI.PropertyField(position, property, label, true);
                EditorGUI.EndProperty();
                return;
            }

            MigrateLegacyProperties(property, enterProperty, exitProperty);

            var contentPosition = EditorGUI.PrefixLabel(position, label);
            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var buttonWidth = Mathf.Max(0.0f, (contentPosition.width - ButtonSpacing) * 0.5f);
            var enterPosition = new Rect(contentPosition.x, contentPosition.y, buttonWidth, contentPosition.height);
            var exitPosition = new Rect(enterPosition.xMax + ButtonSpacing, contentPosition.y, buttonWidth, contentPosition.height);
            var enterChanged = DrawToggleButton(enterPosition, enterProperty, EnterLabel, EditorStyles.miniButtonLeft, out var enterEnabled);
            var exitChanged = DrawToggleButton(exitPosition, exitProperty, ExitLabel, EditorStyles.miniButtonRight, out var exitEnabled);
            if (enterChanged && !enterEnabled) {
                ClearSignalReferences(property.serializedObject, EnterSignalsPropertyName);
            }

            if (exitChanged && !exitEnabled) {
                ClearSignalReferences(property.serializedObject, ExitSignalsPropertyName);
            }

            var changed = enterChanged || exitChanged;
            if (changed) {
                MarkCurrentVersion(property.serializedObject);
            }

            EditorGUI.indentLevel = indentLevel;
            EditorGUI.EndProperty();
        }

        private static bool DrawToggleButton(Rect position, SerializedProperty property, GUIContent label, GUIStyle style, out bool enabled) {
            var previousShowMixedValue = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            enabled = GUI.Toggle(position, property.boolValue, label, style);
            var changed = EditorGUI.EndChangeCheck();
            if (changed) {
                property.boolValue = enabled;
            }

            EditorGUI.showMixedValue = previousShowMixedValue;
            return changed;
        }

        private static void ClearSignalReferences(SerializedObject serializedObject, string signalsPropertyName) {
            var signalsProperty = serializedObject.FindProperty(signalsPropertyName);
            if (signalsProperty == null) {
                return;
            }

            signalsProperty.arraySize = 0;
        }

        private static void MigrateLegacyProperties(SerializedProperty property, SerializedProperty enterProperty, SerializedProperty exitProperty) {
            var serializedObject = property.serializedObject;
            var versionProperty = serializedObject.FindProperty(SignalPortSettingsVersionPropertyName);
            if (versionProperty == null) {
                return;
            }

            var legacyEnterProperty = serializedObject.FindProperty(LegacyEnableEnterSignalPortPropertyName);
            var legacyExitProperty = serializedObject.FindProperty(LegacyEnableExitSignalPortPropertyName);
            if (versionProperty.intValue != 0 && legacyEnterProperty?.boolValue != true && legacyExitProperty?.boolValue != true) {
                return;
            }

            enterProperty.boolValue |= legacyEnterProperty?.boolValue == true;
            exitProperty.boolValue |= legacyExitProperty?.boolValue == true;
            ClearLegacyProperties(serializedObject);
            versionProperty.intValue = 1;
        }

        private static void MarkCurrentVersion(SerializedObject serializedObject) {
            ClearLegacyProperties(serializedObject);
            var versionProperty = serializedObject.FindProperty(SignalPortSettingsVersionPropertyName);
            if (versionProperty != null) {
                versionProperty.intValue = 1;
            }
        }

        private static void ClearLegacyProperties(SerializedObject serializedObject) {
            var legacyEnterProperty = serializedObject.FindProperty(LegacyEnableEnterSignalPortPropertyName);
            var legacyExitProperty = serializedObject.FindProperty(LegacyEnableExitSignalPortPropertyName);
            if (legacyEnterProperty != null) {
                legacyEnterProperty.boolValue = false;
            }

            if (legacyExitProperty != null) {
                legacyExitProperty.boolValue = false;
            }
        }
    }
}
