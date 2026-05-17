using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Base drawer for parameter structs.
    /// </summary>
    internal abstract class ParameterDrawer : PropertyDrawer {
        private const float RelativeWidth = 86.0f;
        private const float FieldSpacing = 4.0f;

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            var valueProperty = GetValueProperty(property);
            var valueHeight = valueProperty == null ? EditorGUIUtility.singleLineHeight : EditorGUI.GetPropertyHeight(valueProperty, GUIContent.none, true);
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + valueHeight;
        }

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            var sourceProperty = property.FindPropertyRelative("_source");
            var relativeProperty = property.FindPropertyRelative("_relative");
            var valueProperty = GetValueProperty(property);
            var valueHeight = valueProperty == null ? EditorGUIUtility.singleLineHeight : EditorGUI.GetPropertyHeight(valueProperty, GUIContent.none, true);
            var firstLinePosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var secondLinePosition = new Rect(position.x, firstLinePosition.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, valueHeight);
            var sourcePosition = EditorGUI.PrefixLabel(firstLinePosition, label);
            var valuePosition = new Rect(secondLinePosition.x + EditorGUIUtility.labelWidth, secondLinePosition.y, secondLinePosition.width - EditorGUIUtility.labelWidth, secondLinePosition.height);
            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            DrawSource(sourcePosition, property, sourceProperty, relativeProperty);
            DrawValue(valuePosition, valueProperty);

            EditorGUI.indentLevel = indentLevel;
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Draws the source selector and optional relative toggle.
        /// </summary>
        /// <param name="position">Draw area.</param>
        /// <param name="property">Parameter property.</param>
        /// <param name="sourceProperty">Source selector property.</param>
        /// <param name="relativeProperty">Relative toggle property.</param>
        private void DrawSource(Rect position, SerializedProperty property, SerializedProperty sourceProperty, SerializedProperty relativeProperty) {
            if (sourceProperty == null) {
                return;
            }

            if (relativeProperty == null) {
                EditorGUI.PropertyField(position, sourceProperty, GUIContent.none);
                return;
            }

            var relativeWidth = Mathf.Min(RelativeWidth, position.width * 0.5f);
            var sourcePosition = new Rect(position.x, position.y, Mathf.Max(0.0f, position.width - relativeWidth - FieldSpacing), position.height);
            var relativePosition = new Rect(sourcePosition.xMax + FieldSpacing, position.y, relativeWidth, position.height);
            EditorGUI.PropertyField(sourcePosition, sourceProperty, GUIContent.none);
            EditorGUI.BeginChangeCheck();
            var relative = EditorGUI.ToggleLeft(relativePosition, new GUIContent("Relative"), relativeProperty.boolValue);

            if (EditorGUI.EndChangeCheck()) {
                relativeProperty.boolValue = relative;
                ApplyRelativeDefaultValue(property, relative);
            }
        }

        /// <summary>
        /// Applies a neutral direct value when Relative changes.
        /// </summary>
        /// <param name="property">Parameter property.</param>
        /// <param name="relative">Relative toggle value.</param>
        private void ApplyRelativeDefaultValue(SerializedProperty property, bool relative) {
            if (!relative || fieldInfo == null || fieldInfo.FieldType != typeof(ColorParameter)) {
                return;
            }

            var sourceProperty = property.FindPropertyRelative("_source");
            if (sourceProperty != null && (ParameterSource)sourceProperty.enumValueIndex == ParameterSource.Blackboard) {
                return;
            }

            var valueProperty = property.FindPropertyRelative("_value");
            if (valueProperty == null || valueProperty.propertyType != SerializedPropertyType.Color) {
                return;
            }

            if (valueProperty.colorValue == default(Color)) {
                valueProperty.colorValue = Color.white;
            }
        }

        /// <summary>
        /// Draws the direct value or the blackboard key for the current source.
        /// </summary>
        /// <param name="position">Draw area.</param>
        /// <param name="valueProperty">Value or blackboard key property.</param>
        private void DrawValue(Rect position, SerializedProperty valueProperty) {
            if (valueProperty == null) {
                return;
            }

            EditorGUI.PropertyField(position, valueProperty, GUIContent.none, true);
        }

        /// <summary>
        /// Finds the value field that should be edited for the selected source.
        /// </summary>
        /// <param name="property">Parameter property.</param>
        /// <returns>Value or blackboard key property.</returns>
        private SerializedProperty GetValueProperty(SerializedProperty property) {
            var sourceProperty = property.FindPropertyRelative("_source");
            var blackboardKeyProperty = property.FindPropertyRelative("_blackboardKey");
            var valueProperty = property.FindPropertyRelative("_value");

            if (sourceProperty != null && (ParameterSource)sourceProperty.enumValueIndex == ParameterSource.Blackboard) {
                return blackboardKeyProperty;
            }

            return valueProperty;
        }
    }

    /// <summary>
    /// IntParameter drawer.
    /// </summary>
    [CustomPropertyDrawer(typeof(IntParameter))]
    internal sealed class IntParameterDrawer : ParameterDrawer {
    }

    /// <summary>
    /// FloatParameter drawer.
    /// </summary>
    [CustomPropertyDrawer(typeof(FloatParameter))]
    internal sealed class FloatParameterDrawer : ParameterDrawer {
    }

    /// <summary>
    /// StringParameter drawer.
    /// </summary>
    [CustomPropertyDrawer(typeof(StringParameter))]
    internal sealed class StringParameterDrawer : ParameterDrawer {
    }

    /// <summary>
    /// Vector2Parameter drawer.
    /// </summary>
    [CustomPropertyDrawer(typeof(Vector2Parameter))]
    internal sealed class Vector2ParameterDrawer : ParameterDrawer {
    }

    /// <summary>
    /// Vector3Parameter drawer.
    /// </summary>
    [CustomPropertyDrawer(typeof(Vector3Parameter))]
    internal sealed class Vector3ParameterDrawer : ParameterDrawer {
    }

    /// <summary>
    /// Vector4Parameter drawer.
    /// </summary>
    [CustomPropertyDrawer(typeof(Vector4Parameter))]
    internal sealed class Vector4ParameterDrawer : ParameterDrawer {
    }

    /// <summary>
    /// ColorParameter drawer.
    /// </summary>
    [CustomPropertyDrawer(typeof(ColorParameter))]
    internal sealed class ColorParameterDrawer : ParameterDrawer {
    }
}
