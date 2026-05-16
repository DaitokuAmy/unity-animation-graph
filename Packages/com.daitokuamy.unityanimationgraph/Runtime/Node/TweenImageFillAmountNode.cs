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

        /// <summary>FillAmount Tween 設定</summary>
        public FloatTween Tween => _tween;
        /// <inheritdoc/>
        protected override FloatTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override void ApplyValue(Image target, float value) {
            target.fillAmount = value;
        }
    }
}
