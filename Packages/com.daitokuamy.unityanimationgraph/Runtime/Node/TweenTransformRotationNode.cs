using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Transform の rotation または localRotation を Euler 角で Tween するノード
    /// </summary>
    [NodeInfo("Tween Rotation", "Built-in/Tween/Transform/Rotation")]
    public sealed class TweenTransformRotationNode : TweenTransformVector3Node {
        [SerializeField, Tooltip("Rotation Tween 設定。値は Euler 角として扱う")]
        private Vector3Tween _tween = new(Vector3.zero, new Vector3(0.0f, 90.0f, 0.0f));
        [SerializeField, Tooltip("Tween を適用する Transform 空間")]
        private Space _space = Space.Self;

        /// <inheritdoc/>
        protected override Vector3Tween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override IEnumerable<string> GetPreviewProperties(Transform target) {
            yield return PreviewPropertyPaths.Transform.LocalRotationX;
            yield return PreviewPropertyPaths.Transform.LocalRotationY;
            yield return PreviewPropertyPaths.Transform.LocalRotationZ;
            yield return PreviewPropertyPaths.Transform.LocalRotationW;
        }

        /// <inheritdoc/>
        protected override Vector3 GetBaseValue(Transform target) {
            return _space == Space.World ? target.rotation.eulerAngles : target.localRotation.eulerAngles;
        }

        /// <inheritdoc/>
        protected override void ApplyValue(Transform target, Vector3 value) {
            if (_space == Space.World) {
                value = _tween.ApplyIgnoreMask(value, target.rotation.eulerAngles);
                target.rotation = Quaternion.Euler(value);
                return;
            }

            value = _tween.ApplyIgnoreMask(value, target.localRotation.eulerAngles);
            target.localRotation = Quaternion.Euler(value);
        }
    }
}
