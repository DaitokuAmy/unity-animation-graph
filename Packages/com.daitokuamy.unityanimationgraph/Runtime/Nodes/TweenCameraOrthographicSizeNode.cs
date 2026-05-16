using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Camera の orthographicSize を Tween するノード
    /// </summary>
    [AnimationGraphNode("Tween Camera Orthographic Size", "Built-in/Tween/Camera/Orthographic Size")]
    public sealed class TweenCameraOrthographicSizeNode : TweenFloatNode<Camera> {
        [SerializeField, Tooltip("OrthographicSize Tween 設定")]
        private FloatTween _tween = new(5.0f, 3.0f);

        /// <summary>OrthographicSize Tween 設定</summary>
        public FloatTween Tween => _tween;
        /// <inheritdoc/>
        protected override FloatTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override void ApplyValue(Camera target, float value) {
            target.orthographicSize = value;
        }
    }
}
