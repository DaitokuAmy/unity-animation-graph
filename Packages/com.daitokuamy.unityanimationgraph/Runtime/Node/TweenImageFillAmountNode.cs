using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityAnimationGraph {
    /// <summary>
    /// Image の fillAmount を Tween するノード
    /// </summary>
    [NodeInfo("Tween Image Fill Amount", "Built-in/Tween/UI/Image Fill Amount")]
    public sealed class TweenImageFillAmountNode : TweenFloatNode<Image> {
        [SerializeField, Tooltip("FillAmount Tween 設定")]
        private FloatTween _tween = new(0.0f, 1.0f);

        /// <inheritdoc/>
        protected override FloatTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override IEnumerable<string> GetPreviewProperties(Image target) {
            yield return PreviewPropertyPaths.UI.ImageFillAmount;
        }

        /// <inheritdoc/>
        protected override float GetBaseValue(Image target) {
            return target.fillAmount;
        }

        /// <inheritdoc/>
        protected override void ApplyValue(Image target, float value) {
            target.fillAmount = value;
        }
    }
}
