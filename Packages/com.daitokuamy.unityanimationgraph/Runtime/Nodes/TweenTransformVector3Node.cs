using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Transform に Vector3 Tween を適用する ActionNode の基底クラス
    /// </summary>
    public abstract class TweenTransformVector3Node : TweenActionNode<Transform, Vector3Tween, Vector3> {
        /// <inheritdoc/>
        protected override bool TryEvaluateValue(Vector3Tween tweenSettings, IAnimationGraphBlackboard blackboard, float localTime, float calculatedDuration, out Vector3 value) {
            return tweenSettings.TryEvaluate(blackboard, localTime, calculatedDuration, out value);
        }
    }
}
