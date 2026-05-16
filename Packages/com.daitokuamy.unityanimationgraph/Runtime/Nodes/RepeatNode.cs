using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// 指定回数に達するまで戻り先へ繰り返すノード
    /// </summary>
    public sealed class RepeatNode : ControlNode {
        [SerializeField, Min(1), Tooltip("合計実行回数")]
        private int _repeatCount = 1;
        [SerializeField, HideInInspector]
        private string _repeatNodeId = string.Empty;

        /// <summary>body の合計実行回数</summary>
        public int RepeatCount => Mathf.Max(1, _repeatCount);
        /// <summary>繰り返し時に戻るノード ID</summary>
        public string RepeatNodeId => _repeatNodeId;

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
