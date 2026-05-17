using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Camera の fieldOfView を Tween するノード
    /// </summary>
    [NodeInfo("Tween Camera Field Of View", "Built-in/Tween/Camera/Field Of View")]
    public sealed class TweenCameraFieldOfViewNode : TweenFloatNode<Camera> {
        [SerializeField, Tooltip("FieldOfView Tween 設定")]
        private FloatTween _tween = new(60.0f, 45.0f);

        /// <inheritdoc/>
        protected override FloatTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override IEnumerable<string> GetPreviewProperties(Camera target) {
            yield return "field of view";
        }

        /// <inheritdoc/>
        protected override float GetBaseValue(Camera target) {
            return target.fieldOfView;
        }

        /// <inheritdoc/>
        protected override void ApplyValue(Camera target, float value) {
            target.fieldOfView = value;
        }
    }
}
