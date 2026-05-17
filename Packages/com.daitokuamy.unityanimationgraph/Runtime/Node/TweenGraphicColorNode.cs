using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityAnimationGraph {
    /// <summary>
    /// Graphic の color を Tween するノード
    /// </summary>
    [NodeInfo("Tween Graphic Color", "Built-in/Tween/UI/Graphic Color")]
    public sealed class TweenGraphicColorNode : TweenColorNode<Graphic> {
        [SerializeField, Tooltip("Color Tween 設定")]
        private ColorTween _tween = new(Color.white, Color.red);

        /// <inheritdoc/>
        protected override ColorTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override IEnumerable<string> GetPreviewProperties(Graphic target) {
            yield return PreviewPropertyPaths.Color.R;
            yield return PreviewPropertyPaths.Color.G;
            yield return PreviewPropertyPaths.Color.B;
            yield return PreviewPropertyPaths.Color.A;
        }

        /// <inheritdoc/>
        protected override Color GetBaseValue(Graphic target) {
            return target.color;
        }

        /// <inheritdoc/>
        protected override void ApplyValue(Graphic target, Color value) {
            target.color = _tween.ApplyIgnoreMask(value, target.color);
        }
    }
}
