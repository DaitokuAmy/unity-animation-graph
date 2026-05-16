using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Transform の localScale を Tween するノード
    /// </summary>
    [AnimationGraphNode("Tween Scale", "Built-in/Tween/Transform/Scale")]
    public sealed class TweenTransformScaleNode : TweenTransformVector3Node {
        [SerializeField, Tooltip("Scale Tween 設定")]
        private Vector3Tween _tween = new(Vector3.one, Vector3.one * 1.2f);

        /// <summary>Scale Tween 設定</summary>
        public Vector3Tween Tween => _tween;
        /// <inheritdoc/>
        protected override Vector3Tween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override void ApplyValue(Transform target, Vector3 value) {
            target.localScale = _tween.ApplyIgnoreMask(value, target.localScale);
        }
    }
}
