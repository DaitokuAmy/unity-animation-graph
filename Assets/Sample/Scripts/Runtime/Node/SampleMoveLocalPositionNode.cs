using UnityEngine;

namespace UnityAnimationGraph.Sample {
    /// <summary>
    /// Sample 用に target の localPosition を動かす ActionNode
    /// </summary>
    [NodeInfo("サンプル移動", "Sample/サンプル移動")]
    public sealed class SampleMoveLocalPositionNode : ActionNode {
        [SerializeField, Min(0.0f), Tooltip("移動にかける時間")]
        private float _duration = 1.0f;
        [SerializeField, Min(0.0f), Tooltip("移動開始前の待機時間")]
        private float _delay;
        [SerializeField, Tooltip("移動開始時の localPosition")]
        private Vector3 _from;
        [SerializeField, Tooltip("移動終了時の localPosition")]
        private Vector3 _to = Vector3.up;

        /// <summary>移動にかける時間</summary>
        public float Duration => Mathf.Max(0.0f, _duration);
        /// <summary>移動開始前の待機時間</summary>
        public float Delay => Mathf.Max(0.0f, _delay);
        /// <summary>移動開始時の localPosition</summary>
        public Vector3 From => _from;
        /// <summary>移動終了時の localPosition</summary>
        public Vector3 To => _to;

        /// <inheritdoc/>
        protected override float CalculateDuration(int seed, IAnimationGraphContext context) {
            return Duration;
        }

        /// <inheritdoc/>
        protected override float CalculateDelay(int seed, IAnimationGraphContext context) {
            return Delay;
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context) {
            if (!context.TryGetTarget<Transform>(TargetKey, out var target)) {
                return;
            }

            var progress = calculatedDuration <= 0.0f ? 1.0f : Mathf.Clamp01(localTime / calculatedDuration);
            target.localPosition = Vector3.LerpUnclamped(_from, _to, progress);
        }
    }
}
