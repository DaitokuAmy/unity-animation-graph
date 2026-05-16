using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// 待機後に後続ノードへ流すルートノード
    /// </summary>
    public sealed class RouteNode : ControlNode {
        [SerializeField, Min(0.0f), Tooltip("後続ノードへ進む前の待機時間")]
        private float _delay;

        /// <summary>後続ノードへ進む前の待機時間</summary>
        public float Delay => Mathf.Max(0.0f, _delay);

        /// <inheritdoc/>
        protected override float CalculateDuration(int seed, IAnimationGraphContext context) {
            return 0.0f;
        }

        /// <inheritdoc/>
        protected override float CalculateDelay(int seed, IAnimationGraphContext context) {
            return Delay;
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context) {
        }
    }
}
