using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Camera の orthographicSize を Tween するノード
    /// </summary>
    [NodeInfo("Tween Camera Orthographic Size", "Built-in/Tween/Camera/Orthographic Size")]
    public sealed class TweenCameraOrthographicSizeNode : TweenFloatNode<Camera> {
        [SerializeField, Tooltip("OrthographicSize Tween 設定")]
        private FloatTween _tween = new(5.0f, 3.0f);

        /// <inheritdoc/>
        protected override FloatTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override IEnumerable<string> GetPreviewProperties(Camera target) {
            yield return PreviewPropertyPaths.Camera.OrthographicSize;
        }

        /// <inheritdoc/>
        protected override float GetBaseValue(Camera target) {
            return target.orthographicSize;
        }

        /// <inheritdoc/>
        protected override void ApplyValue(Camera target, float value) {
            target.orthographicSize = value;
        }
    }
}
