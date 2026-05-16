using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// SpriteRenderer の Color を Tween するノード
    /// </summary>
    [NodeInfo("Tween Sprite Renderer Color", "Built-in/Tween/Sprite Renderer/Color")]
    public sealed class TweenSpriteRendererColorNode : TweenColorNode<SpriteRenderer> {
        [SerializeField, Tooltip("Color Tween 設定")]
        private ColorTween _tween = new(Color.white, Color.red);

        /// <summary>Color Tween 設定</summary>
        public ColorTween Tween => _tween;
        /// <inheritdoc/>
        protected override ColorTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override void ApplyValue(SpriteRenderer target, Color value) {
            target.color = _tween.ApplyIgnoreMask(value, target.color);
        }
    }
}
