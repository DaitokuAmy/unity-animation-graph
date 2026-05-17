using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// TweenEase の Drawer
    /// </summary>
    [CustomPropertyDrawer(typeof(TweenEase))]
    internal sealed class TweenEaseDrawer : PropertyDrawer {
        private const float ModeWidth = 120.0f;
        private const float FieldSpacing = 4.0f;

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight;
        }

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            var modeProperty = property.FindPropertyRelative("_mode");
            var easeTypeProperty = property.FindPropertyRelative("_easeType");
            var animationCurveProperty = property.FindPropertyRelative("_animationCurve");
            var contentPosition = EditorGUI.PrefixLabel(position, label);
            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var modeWidth = Mathf.Min(ModeWidth, contentPosition.width * 0.45f);
            var modePosition = new Rect(contentPosition.x, contentPosition.y, modeWidth, contentPosition.height);
            var valuePosition = new Rect(modePosition.xMax + FieldSpacing, contentPosition.y, contentPosition.width - modeWidth - FieldSpacing, contentPosition.height);
            EditorGUI.PropertyField(modePosition, modeProperty, GUIContent.none);

            if ((TweenEaseMode)modeProperty.enumValueIndex == TweenEaseMode.Curve) {
                EditorGUI.PropertyField(valuePosition, animationCurveProperty, GUIContent.none);
            }
            else {
                EditorGUI.PropertyField(valuePosition, easeTypeProperty, GUIContent.none);
            }

            EditorGUI.indentLevel = indentLevel;
            EditorGUI.EndProperty();
        }
    }
}
