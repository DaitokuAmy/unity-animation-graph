using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// SpriteRenderer の alpha を Tween するノード
    /// </summary>
    [NodeInfo("Tween Sprite Renderer Alpha", "Built-in/Tween/Sprite Renderer/Alpha")]
    public sealed class TweenSpriteRendererAlphaNode : TweenFloatNode<SpriteRenderer> {
        [SerializeField, Tooltip("Alpha Tween 設定")]
        private FloatTween _tween = new(1.0f, 0.0f);

        /// <summary>Alpha Tween 設定</summary>
        public FloatTween Tween => _tween;
        /// <inheritdoc/>
        protected override FloatTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override void ApplyValue(SpriteRenderer target, float value) {
            var color = target.color;
            color.a = value;
            target.color = color;
        }
    }
}
