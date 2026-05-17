using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Transform の position または localPosition を Tween するノード
    /// </summary>
    [NodeInfo("Tween Position", "Built-in/Tween/Transform/Position")]
    public sealed class TweenTransformPositionNode : TweenTransformVector3Node {
        [SerializeField, Tooltip("Position Tween 設定")]
        private Vector3Tween _tween = new(Vector3.zero, Vector3.up);
        [SerializeField, Tooltip("Tween を適用する Transform 空間")]
        private Space _space = Space.Self;

        /// <inheritdoc/>
        protected override Vector3Tween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override IEnumerable<string> GetPreviewProperties(Transform target) {
            yield return PreviewPropertyPaths.Transform.LocalPositionX;
            yield return PreviewPropertyPaths.Transform.LocalPositionY;
            yield return PreviewPropertyPaths.Transform.LocalPositionZ;
        }

        /// <inheritdoc/>
        protected override Vector3 GetBaseValue(Transform target) {
            return _space == Space.World ? target.position : target.localPosition;
        }

        /// <inheritdoc/>
        protected override void ApplyValue(Transform target, Vector3 value) {
            if (_space == Space.World) {
                target.position = _tween.ApplyIgnoreMask(value, target.position);
                return;
            }

            target.localPosition = _tween.ApplyIgnoreMask(value, target.localPosition);
        }
    }
}
