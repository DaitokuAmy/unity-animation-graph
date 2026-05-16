using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Light の color を Tween するノード
    /// </summary>
    [NodeInfo("Tween Light Color", "Built-in/Tween/Light/Color")]
    public sealed class TweenLightColorNode : TweenColorNode<Light> {
        [SerializeField, Tooltip("Color Tween 設定")]
        private ColorTween _tween = new(Color.white, Color.red);

        /// <summary>Color Tween 設定</summary>
        public ColorTween Tween => _tween;
        /// <inheritdoc/>
        protected override ColorTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override void ApplyValue(Light target, Color value) {
            target.color = _tween.ApplyIgnoreMask(value, target.color);
        }
    }
}
