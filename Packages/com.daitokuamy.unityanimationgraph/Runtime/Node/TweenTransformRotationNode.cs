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

        /// <summary>Rotation Tween 設定</summary>
        public Vector3Tween Tween => _tween;
        /// <summary>Tween を適用する Transform 空間</summary>
        public Space TransformSpace => _space;
        /// <inheritdoc/>
        protected override Vector3Tween TweenSettings => _tween;

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
