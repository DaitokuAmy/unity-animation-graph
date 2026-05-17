using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// RectTransform の anchoredPosition を Tween するノード
    /// </summary>
    [NodeInfo("Tween RectTransform Anchored Position", "Built-in/Tween/UI/RectTransform Anchored Position")]
    public sealed class TweenRectTransformAnchoredPositionNode : TweenVector2Node<RectTransform> {
        [SerializeField, Tooltip("AnchoredPosition Tween 設定")]
        private Vector2Tween _tween = new(Vector2.zero, Vector2.up * 100.0f);

        /// <inheritdoc/>
        protected override Vector2Tween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override IEnumerable<string> GetPreviewProperties(RectTransform target) {
            yield return PreviewPropertyPaths.RectTransform.AnchoredPositionX;
            yield return PreviewPropertyPaths.RectTransform.AnchoredPositionY;
        }

        /// <inheritdoc/>
        protected override Vector2 GetBaseValue(RectTransform target) {
            return target.anchoredPosition;
        }

        /// <inheritdoc/>
        protected override void ApplyValue(RectTransform target, Vector2 value) {
            target.anchoredPosition = _tween.ApplyIgnoreMask(value, target.anchoredPosition);
        }
    }
}
