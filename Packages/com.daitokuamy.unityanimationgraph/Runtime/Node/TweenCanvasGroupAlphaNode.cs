using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// CanvasGroup の alpha を Tween するノード
    /// </summary>
    [NodeInfo("Tween Canvas Group Alpha", "Built-in/Tween/UI/Canvas Group Alpha")]
    public sealed class TweenCanvasGroupAlphaNode : TweenFloatNode<CanvasGroup> {
        [SerializeField, Tooltip("Alpha Tween 設定")]
        private FloatTween _tween = new(1.0f, 0.0f);

        /// <inheritdoc/>
        protected override FloatTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override IEnumerable<string> GetPreviewProperties(CanvasGroup target) {
            yield return PreviewPropertyPaths.UI.CanvasGroupAlpha;
        }

        /// <inheritdoc/>
        protected override float GetBaseValue(CanvasGroup target) {
            return target.alpha;
        }

        /// <inheritdoc/>
        protected override void ApplyValue(CanvasGroup target, float value) {
            target.alpha = value;
        }
    }
}
