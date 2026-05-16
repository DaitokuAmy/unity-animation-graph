using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// 複数ルートの合流地点を表すノード
    /// </summary>
    public sealed class JoinNode : ControlNode {
        [SerializeField, Tooltip("合流方法")]
        private JoinType _joinType = JoinType.All;

        /// <summary>合流方法</summary>
        public JoinType JoinType => _joinType;

        /// <inheritdoc/>
        protected override float CalculateDuration(int seed, IAnimationGraphContext context) {
            return 0.0f;
        }

        /// <inheritdoc/>
        protected override float CalculateDelay(int seed, IAnimationGraphContext context) {
            return 0.0f;
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context) {
        }
    }
}
