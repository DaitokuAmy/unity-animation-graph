using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// SpriteRenderer の Color を Tween するノード
    /// </summary>
    [NodeInfo("Tween Sprite Renderer Color", "Built-in/Tween/Sprite Renderer/Color")]
    public sealed class TweenSpriteRendererColorNode : TweenColorNode<SpriteRenderer> {
        [SerializeField, Tooltip("Color Tween 設定")]
        private ColorTween _tween = new(Color.white, Color.red);

        /// <inheritdoc/>
        protected override ColorTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override IEnumerable<string> GetPreviewProperties(SpriteRenderer target) {
            yield return PreviewPropertyPaths.Color.R;
            yield return PreviewPropertyPaths.Color.G;
            yield return PreviewPropertyPaths.Color.B;
            yield return PreviewPropertyPaths.Color.A;
        }

        /// <inheritdoc/>
        protected override Color GetBaseValue(SpriteRenderer target) {
            return target.color;
        }

        /// <inheritdoc/>
        protected override void ApplyValue(SpriteRenderer target, Color value) {
            target.color = _tween.ApplyIgnoreMask(value, target.color);
        }
    }
}
