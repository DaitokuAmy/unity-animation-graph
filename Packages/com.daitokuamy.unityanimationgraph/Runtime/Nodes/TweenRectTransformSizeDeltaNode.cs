using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// RectTransform の sizeDelta を Tween するノード
    /// </summary>
    [AnimationGraphNode("Tween RectTransform Size Delta", "Built-in/Tween/UI/RectTransform Size Delta")]
    public sealed class TweenRectTransformSizeDeltaNode : TweenVector2Node<RectTransform> {
        [SerializeField, Tooltip("SizeDelta Tween 設定")]
        private Vector2Tween _tween = new(Vector2.one * 100.0f, Vector2.one * 200.0f);

        /// <summary>SizeDelta Tween 設定</summary>
        public Vector2Tween Tween => _tween;
        /// <inheritdoc/>
        protected override Vector2Tween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override void ApplyValue(RectTransform target, Vector2 value) {
            target.sizeDelta = _tween.ApplyIgnoreMask(value, target.sizeDelta);
        }
    }
}
