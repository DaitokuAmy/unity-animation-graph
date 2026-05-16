using UnityEngine;
using UnityEngine.UI;

namespace UnityAnimationGraph.Sample {
    /// <summary>
    /// Sample 用に target の Graphic.color を変える ActionNode
    /// </summary>
    [NodeInfo("サンプル色変更", "Sample/サンプル色変更")]
    public sealed class SampleChangeColorNode : ActionNode {
        [SerializeField, Min(0.0f), Tooltip("色変更にかける時間")]
        private float _duration = 1.0f;
        [SerializeField, Min(0.0f), Tooltip("色変更開始前の待機時間")]
        private float _delay;
        [SerializeField, Tooltip("色変更開始時の color")]
        private Color _from = Color.white;
        [SerializeField, Tooltip("色変更終了時の color")]
        private Color _to = Color.red;

        /// <summary>色変更にかける時間</summary>
        public float Duration => Mathf.Max(0.0f, _duration);
        /// <summary>色変更開始前の待機時間</summary>
        public float Delay => Mathf.Max(0.0f, _delay);
        /// <summary>色変更開始時の color</summary>
        public Color From => _from;
        /// <summary>色変更終了時の color</summary>
        public Color To => _to;

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
            if (!context.TryGetTarget<Graphic>(TargetKey, out var target)) {
                if (!context.TryGetTarget<Transform>(TargetKey, out var transform)) {
                    return;
                }

                target = transform.GetComponent<Graphic>();
                if (target == null) {
                    return;
                }
            }

            var progress = calculatedDuration <= 0.0f ? 1.0f : Mathf.Clamp01(localTime / calculatedDuration);
            target.color = Color.LerpUnclamped(_from, _to, progress);
        }
    }
}
