using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Light の intensity を Tween するノード
    /// </summary>
    [AnimationGraphNode("Tween Light Intensity", "Built-in/Tween/Light/Intensity")]
    public sealed class TweenLightIntensityNode : TweenFloatNode<Light> {
        [SerializeField, Tooltip("Intensity Tween 設定")]
        private FloatTween _tween = new(0.0f, 1.0f);

        /// <summary>Intensity Tween 設定</summary>
        public FloatTween Tween => _tween;
        /// <inheritdoc/>
        protected override FloatTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override void ApplyValue(Light target, float value) {
            target.intensity = value;
        }
    }
}
