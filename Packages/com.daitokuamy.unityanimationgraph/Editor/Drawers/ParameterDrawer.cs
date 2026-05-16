using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// 型別 Parameter の Drawer 基底
    /// </summary>
    internal abstract class ParameterDrawer : PropertyDrawer {
        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight * 2.0f + EditorGUIUtility.standardVerticalSpacing;
        }

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            var sourceProperty = property.FindPropertyRelative("_source");
            var blackboardKeyProperty = property.FindPropertyRelative("_blackboardKey");
            var valueProperty = property.FindPropertyRelative("_value");
            var firstLinePosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var secondLinePosition = new Rect(position.x, firstLinePosition.yMax + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);
            var sourcePosition = EditorGUI.PrefixLabel(firstLinePosition, label);
            var valuePosition = new Rect(secondLinePosition.x + EditorGUIUtility.labelWidth, secondLinePosition.y, secondLinePosition.width - EditorGUIUtility.labelWidth, secondLinePosition.height);
            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            EditorGUI.PropertyField(sourcePosition, sourceProperty, GUIContent.none);
            DrawValue(valuePosition, blackboardKeyProperty, valueProperty, (ParameterSource)sourceProperty.enumValueIndex);

            EditorGUI.indentLevel = indentLevel;
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// 直値または Blackboard key 選択欄を描画
        /// </summary>
        /// <param name="position">描画範囲</param>
        /// <param name="blackboardKeyProperty">Blackboard key SerializedProperty</param>
        /// <param name="valueProperty">直値 SerializedProperty</param>
        /// <param name="source">値の解決元</param>
        private static void DrawValue(Rect position, SerializedProperty blackboardKeyProperty, SerializedProperty valueProperty, ParameterSource source) {
            if (source == ParameterSource.Blackboard) {
                EditorGUI.PropertyField(position, blackboardKeyProperty, GUIContent.none);
                return;
            }

            EditorGUI.PropertyField(position, valueProperty, GUIContent.none);
        }
    }

    /// <summary>
    /// IntParameter の Drawer
    /// </summary>
    [CustomPropertyDrawer(typeof(IntParameter))]
    internal sealed class IntParameterDrawer : ParameterDrawer {
    }

    /// <summary>
    /// FloatParameter の Drawer
    /// </summary>
    [CustomPropertyDrawer(typeof(FloatParameter))]
    internal sealed class FloatParameterDrawer : ParameterDrawer {
    }

    /// <summary>
    /// StringParameter の Drawer
    /// </summary>
    [CustomPropertyDrawer(typeof(StringParameter))]
    internal sealed class StringParameterDrawer : ParameterDrawer {
    }

    /// <summary>
    /// Vector2Parameter の Drawer
    /// </summary>
    [CustomPropertyDrawer(typeof(Vector2Parameter))]
    internal sealed class Vector2ParameterDrawer : ParameterDrawer {
    }

    /// <summary>
    /// Vector3Parameter の Drawer
    /// </summary>
    [CustomPropertyDrawer(typeof(Vector3Parameter))]
    internal sealed class Vector3ParameterDrawer : ParameterDrawer {
    }

    /// <summary>
    /// ColorParameter の Drawer
    /// </summary>
    [CustomPropertyDrawer(typeof(ColorParameter))]
    internal sealed class ColorParameterDrawer : ParameterDrawer {
    }
}
