using System.Collections.Generic;
using UnityAnimationGraph;
using UnityEngine;
using UnityEngine.UI;

namespace Sample {
    /// <summary>
    /// Sample 用に target の Graphic.color を変える ActionNode
    /// </summary>
    [NodeInfo("サンプル色変更", "Sample/サンプル色変更")]
    public sealed class SampleChangeColorNode : ActionNode<Graphic> {
        [SerializeField, Min(0.0f), Tooltip("色変更にかける時間")]
        private float _duration = 1.0f;
        [SerializeField, Min(0.0f), Tooltip("色変更開始前の待機時間")]
        private float _delay;
        [SerializeField, Tooltip("色変更開始時の color")]
        private Color _from = Color.white;
        [SerializeField, Tooltip("色変更終了時の color")]
        private Color _to = Color.red;

        /// <inheritdoc/>
        protected override IEnumerable<string> GetPreviewProperties(Graphic target) {
            yield return "m_Color.r";
            yield return "m_Color.g";
            yield return "m_Color.b";
            yield return "m_Color.a";
        }

        /// <inheritdoc/>
        protected override float CalculateDuration(int seed, Graphic target, IAnimationGraphBlackboard blackboard) {
            return Mathf.Max(0.0f, _duration);
        }

        /// <inheritdoc/>
        protected override float CalculateDelay(int seed, Graphic target, IAnimationGraphBlackboard blackboard) {
            return Mathf.Max(0.0f, _delay);
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, Graphic target, float localTime, float calculatedDuration, IAnimationGraphBlackboard blackboard) {
            var progress = calculatedDuration <= 0.0f ? 1.0f : Mathf.Clamp01(localTime / calculatedDuration);
            target.color = Color.LerpUnclamped(_from, _to, progress);
        }
    }
}
