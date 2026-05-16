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

        /// <summary>Position Tween 設定</summary>
        public Vector3Tween Tween => _tween;
        /// <summary>Tween を適用する Transform 空間</summary>
        public Space TransformSpace => _space;
        /// <inheritdoc/>
        protected override Vector3Tween TweenSettings => _tween;

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
