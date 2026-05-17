using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// SpriteRenderer の alpha を Tween するノード
    /// </summary>
    [NodeInfo("Tween Sprite Renderer Alpha", "Built-in/Tween/Sprite Renderer/Alpha")]
    public sealed class TweenSpriteRendererAlphaNode : TweenFloatNode<SpriteRenderer> {
        [SerializeField, Tooltip("Alpha Tween 設定")]
        private FloatTween _tween = new(1.0f, 0.0f);

        /// <inheritdoc/>
        protected override FloatTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override IEnumerable<string> GetPreviewProperties(SpriteRenderer target) {
            yield return PreviewPropertyPaths.Color.A;
        }

        /// <inheritdoc/>
        protected override float GetBaseValue(SpriteRenderer target) {
            return target.color.a;
        }

        /// <inheritdoc/>
        protected override void ApplyValue(SpriteRenderer target, float value) {
            var color = target.color;
            color.a = value;
            target.color = color;
        }
    }
}
