using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Transform の localScale を Tween するノード
    /// </summary>
    [NodeInfo("Tween Scale", "Built-in/Tween/Transform/Scale")]
    public sealed class TweenTransformScaleNode : TweenTransformVector3Node {
        [SerializeField, Tooltip("Scale Tween 設定")]
        private Vector3Tween _tween = new(Vector3.one, Vector3.one * 1.2f);

        /// <inheritdoc/>
        protected override Vector3Tween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override IEnumerable<string> GetPreviewProperties(Transform target) {
            yield return "m_LocalScale.x";
            yield return "m_LocalScale.y";
            yield return "m_LocalScale.z";
        }

        /// <inheritdoc/>
        protected override Vector3 GetBaseValue(Transform target) {
            return target.localScale;
        }

        /// <inheritdoc/>
        protected override void ApplyValue(Transform target, Vector3 value) {
            target.localScale = _tween.ApplyIgnoreMask(value, target.localScale);
        }
    }
}
