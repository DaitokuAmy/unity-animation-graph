using System.Collections.Generic;
using UnityAnimationGraph;
using UnityEngine;

namespace Sample {
    /// <summary>
    /// Sample 用に target の localPosition を動かす ActionNode
    /// </summary>
    [NodeInfo("サンプル移動", "Sample/サンプル移動")]
    public sealed class SampleMoveLocalPositionNode : ActionNode<Transform> {
        [SerializeField, Min(0.0f), Tooltip("移動にかける時間")]
        private float _duration = 1.0f;
        [SerializeField, Min(0.0f), Tooltip("移動開始前の待機時間")]
        private float _delay;
        [SerializeField, Tooltip("移動開始時の localPosition")]
        private Vector3 _from;
        [SerializeField, Tooltip("移動終了時の localPosition")]
        private Vector3 _to = Vector3.up;

        /// <inheritdoc/>
        protected override IEnumerable<string> GetPreviewProperties(Transform target) {
            yield return "m_LocalPosition.x";
            yield return "m_LocalPosition.y";
            yield return "m_LocalPosition.z";
        }

        /// <inheritdoc/>
        protected override float CalculateDuration(int seed, Transform target, IAnimationGraphBlackboard blackboard) {
            return Mathf.Max(0.0f, _duration);
        }

        /// <inheritdoc/>
        protected override float CalculateDelay(int seed, Transform target, IAnimationGraphBlackboard blackboard) {
            return Mathf.Max(0.0f, _delay);
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, Transform target, float localTime, float calculatedDuration, IAnimationGraphBlackboard blackboard) {
            var progress = calculatedDuration <= 0.0f ? 1.0f : Mathf.Clamp01(localTime / calculatedDuration);
            target.localPosition = Vector3.LerpUnclamped(_from, _to, progress);
        }
    }
}
