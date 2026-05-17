namespace UnityAnimationGraph {
    /// <summary>
    /// Editor Preview の AnimationMode 登録に使用する SerializedProperty path 定数
    /// </summary>
    public static class PreviewPropertyPaths {
        /// <summary>
        /// GameObject の SerializedProperty path
        /// </summary>
        public static class GameObject {
            /// <summary>GameObject.activeSelf</summary>
            public const string ActiveSelf = "m_IsActive";
        }

        /// <summary>
        /// Component の SerializedProperty path
        /// </summary>
        public static class Component {
            /// <summary>Component.enabled</summary>
            public const string Enabled = "m_Enabled";
        }

        /// <summary>
        /// Transform の SerializedProperty path
        /// </summary>
        public static class Transform {
            /// <summary>Transform.localPosition.x</summary>
            public const string LocalPositionX = "m_LocalPosition.x";
            /// <summary>Transform.localPosition.y</summary>
            public const string LocalPositionY = "m_LocalPosition.y";
            /// <summary>Transform.localPosition.z</summary>
            public const string LocalPositionZ = "m_LocalPosition.z";
            /// <summary>Transform.localRotation.x</summary>
            public const string LocalRotationX = "m_LocalRotation.x";
            /// <summary>Transform.localRotation.y</summary>
            public const string LocalRotationY = "m_LocalRotation.y";
            /// <summary>Transform.localRotation.z</summary>
            public const string LocalRotationZ = "m_LocalRotation.z";
            /// <summary>Transform.localRotation.w</summary>
            public const string LocalRotationW = "m_LocalRotation.w";
            /// <summary>Transform.localScale.x</summary>
            public const string LocalScaleX = "m_LocalScale.x";
            /// <summary>Transform.localScale.y</summary>
            public const string LocalScaleY = "m_LocalScale.y";
            /// <summary>Transform.localScale.z</summary>
            public const string LocalScaleZ = "m_LocalScale.z";
        }

        /// <summary>
        /// RectTransform の SerializedProperty path
        /// </summary>
        public static class RectTransform {
            /// <summary>RectTransform.anchoredPosition.x</summary>
            public const string AnchoredPositionX = "m_AnchoredPosition.x";
            /// <summary>RectTransform.anchoredPosition.y</summary>
            public const string AnchoredPositionY = "m_AnchoredPosition.y";
            /// <summary>RectTransform.sizeDelta.x</summary>
            public const string SizeDeltaX = "m_SizeDelta.x";
            /// <summary>RectTransform.sizeDelta.y</summary>
            public const string SizeDeltaY = "m_SizeDelta.y";
        }

        /// <summary>
        /// Color を持つ Component の SerializedProperty path
        /// </summary>
        public static class Color {
            /// <summary>Color.r</summary>
            public const string R = "m_Color.r";
            /// <summary>Color.g</summary>
            public const string G = "m_Color.g";
            /// <summary>Color.b</summary>
            public const string B = "m_Color.b";
            /// <summary>Color.a</summary>
            public const string A = "m_Color.a";
        }

        /// <summary>
        /// uGUI の SerializedProperty path
        /// </summary>
        public static class UI {
            /// <summary>CanvasGroup.alpha</summary>
            public const string CanvasGroupAlpha = "m_Alpha";
            /// <summary>Image.fillAmount</summary>
            public const string ImageFillAmount = "m_FillAmount";
        }

        /// <summary>
        /// AudioSource の SerializedProperty path
        /// </summary>
        public static class AudioSource {
            /// <summary>AudioSource.volume</summary>
            public const string Volume = "m_Volume";
            /// <summary>AudioSource.pitch</summary>
            public const string Pitch = "m_Pitch";
        }

        /// <summary>
        /// Camera の SerializedProperty path
        /// </summary>
        public static class Camera {
            /// <summary>Camera.fieldOfView</summary>
            public const string FieldOfView = "field of view";
            /// <summary>Camera.orthographicSize</summary>
            public const string OrthographicSize = "orthographic size";
        }

        /// <summary>
        /// Light の SerializedProperty path
        /// </summary>
        public static class Light {
            /// <summary>Light.color.r</summary>
            public const string ColorR = "m_Color.r";
            /// <summary>Light.color.g</summary>
            public const string ColorG = "m_Color.g";
            /// <summary>Light.color.b</summary>
            public const string ColorB = "m_Color.b";
            /// <summary>Light.color.a</summary>
            public const string ColorA = "m_Color.a";
            /// <summary>Light.intensity</summary>
            public const string Intensity = "m_Intensity";
        }

        /// <summary>
        /// PlayableDirector の SerializedProperty path
        /// </summary>
        public static class PlayableDirector {
            /// <summary>PlayableDirector.playableAsset</summary>
            public const string PlayableAsset = "m_PlayableAsset";
            /// <summary>PlayableDirector.timeUpdateMode</summary>
            public const string DirectorUpdateMode = "m_DirectorUpdateMode";
        }
    }
}
