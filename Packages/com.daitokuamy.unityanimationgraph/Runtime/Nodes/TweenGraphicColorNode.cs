using UnityEngine;
using UnityEngine.UI;

namespace UnityAnimationGraph {
    /// <summary>
    /// Graphic の color を Tween するノード
    /// </summary>
    [AnimationGraphNode("Tween Graphic Color", "Built-in/Tween/UI/Graphic Color")]
    public sealed class TweenGraphicColorNode : TweenColorNode<Graphic> {
        [SerializeField, Tooltip("Color Tween 設定")]
        private ColorTween _tween = new(Color.white, Color.red);

        /// <summary>Color Tween 設定</summary>
        public ColorTween Tween => _tween;
        /// <inheritdoc/>
        protected override ColorTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override void ApplyValue(Graphic target, Color value) {
            target.color = _tween.ApplyIgnoreMask(value, target.color);
        }
    }
}
