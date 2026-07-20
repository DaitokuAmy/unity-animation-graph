using UnityEditor;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// PlayParticleSystemNode の Inspector
    /// </summary>
    [CustomEditor(typeof(PlayParticleSystemNode)), CanEditMultipleObjects]
    internal sealed class PlayParticleSystemNodeEditor : UnityEditor.Editor {
        private const string AutoDurationPropertyName = "_autoDuration";
        private const string DurationPropertyName = "_duration";

        /// <inheritdoc/>
        public override void OnInspectorGUI() {
            serializedObject.Update();
            var autoDurationProperty = serializedObject.FindProperty(AutoDurationPropertyName);
            var property = serializedObject.GetIterator();
            var enterChildren = true;
            while (property.NextVisible(enterChildren)) {
                enterChildren = false;
                if (property.propertyPath == DurationPropertyName
                    && autoDurationProperty != null
                    && !autoDurationProperty.hasMultipleDifferentValues
                    && autoDurationProperty.boolValue) {
                    continue;
                }

                using (new EditorGUI.DisabledScope(property.propertyPath == "m_Script")) {
                    EditorGUILayout.PropertyField(property, true);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
