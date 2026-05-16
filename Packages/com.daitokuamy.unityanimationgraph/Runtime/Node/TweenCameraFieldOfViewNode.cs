using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Camera の fieldOfView を Tween するノード
    /// </summary>
    [NodeInfo("Tween Camera Field Of View", "Built-in/Tween/Camera/Field Of View")]
    public sealed class TweenCameraFieldOfViewNode : TweenFloatNode<Camera> {
        [SerializeField, Tooltip("FieldOfView Tween 設定")]
        private FloatTween _tween = new(60.0f, 45.0f);

        /// <summary>FieldOfView Tween 設定</summary>
        public FloatTween Tween => _tween;
        /// <inheritdoc/>
        protected override FloatTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override void ApplyValue(Camera target, float value) {
            target.fieldOfView = value;
        }
    }
}
