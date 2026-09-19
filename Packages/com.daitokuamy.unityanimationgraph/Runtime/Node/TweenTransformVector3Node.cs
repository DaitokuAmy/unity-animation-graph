using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Transform に Vector3 Tween を適用する ActionNode の基底クラス
    /// </summary>
    public abstract class TweenTransformVector3Node : TweenVector3Node<Transform> {
        /// <inheritdoc/>
        protected override bool TryResolveActionTarget(Component source, out Transform target) {
            if (source == null) {
                target = null;
                return false;
            }

            target = source.transform;
            return true;
        }
    }
}
