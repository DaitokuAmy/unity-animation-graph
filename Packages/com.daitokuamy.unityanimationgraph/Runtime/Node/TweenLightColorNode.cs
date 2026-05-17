using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Light の color を Tween するノード
    /// </summary>
    [NodeInfo("Tween Light Color", "Built-in/Tween/Light/Color")]
    public sealed class TweenLightColorNode : TweenColorNode<Light> {
        [SerializeField, Tooltip("Color Tween 設定")]
        private ColorTween _tween = new(Color.white, Color.red);

        /// <inheritdoc/>
        protected override ColorTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override IEnumerable<string> GetPreviewProperties(Light target) {
            yield return "m_Color.r";
            yield return "m_Color.g";
            yield return "m_Color.b";
            yield return "m_Color.a";
        }

        /// <inheritdoc/>
        protected override Color GetBaseValue(Light target) {
            return target.color;
        }

        /// <inheritdoc/>
        protected override void ApplyValue(Light target, Color value) {
            target.color = _tween.ApplyIgnoreMask(value, target.color);
        }
    }
}
