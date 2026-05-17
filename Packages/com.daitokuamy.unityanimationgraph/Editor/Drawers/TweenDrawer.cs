using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Drawer that shows Tween settings without the default foldout.
    /// </summary>
    [CustomPropertyDrawer(typeof(Tween), true)]
    internal sealed class TweenDrawer : PropertyDrawer {
        private const float Padding = 6.0f;
        private const float FieldSpacing = 2.0f;

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return Padding * 2.0f + GetChildrenHeight(property);
        }

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            GUI.Box(position, GUIContent.none, EditorStyles.helpBox);

            var contentPosition = new Rect(
                position.x + Padding,
                position.y + Padding,
                position.width - Padding * 2.0f,
                position.height - Padding * 2.0f);
            DrawChildren(contentPosition, property);

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Draws Tween child fields in serialized order.
        /// </summary>
        /// <param name="position">Draw area.</param>
        /// <param name="property">Tween property.</param>
        private void DrawChildren(Rect position, SerializedProperty property) {
            var iterator = property.Copy();
            var endProperty = iterator.GetEndProperty();
            var enterChildren = true;
            var y = position.y;
            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty)) {
                var childProperty = iterator.Copy();
                var height = EditorGUI.GetPropertyHeight(childProperty, true);
                var childPosition = new Rect(position.x, y, position.width, height);
                EditorGUI.PropertyField(childPosition, childProperty, true);
                y += height + FieldSpacing;
                enterChildren = false;
            }

            EditorGUI.indentLevel = indentLevel;
        }

        /// <summary>
        /// Calculates the total height needed for Tween child fields.
        /// </summary>
        /// <param name="property">Tween property.</param>
        /// <returns>Total child height.</returns>
        private float GetChildrenHeight(SerializedProperty property) {
            var iterator = property.Copy();
            var endProperty = iterator.GetEndProperty();
            var enterChildren = true;
            var height = 0.0f;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty)) {
                if (height > 0.0f) {
                    height += FieldSpacing;
                }

                height += EditorGUI.GetPropertyHeight(iterator, true);
                enterChildren = false;
            }

            return height;
        }
    }
}
