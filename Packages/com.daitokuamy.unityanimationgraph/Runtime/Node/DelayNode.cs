using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityAnimationGraph {
    /// <summary>
    /// Node that waits for a configured delay before continuing to the next nodes.
    /// </summary>
    [MovedFrom(true, "UnityAnimationGraph", "UnityAnimationGraph", "RouteNode")]
    public sealed class DelayNode : ControlNode {
        [SerializeField, Min(0.0f), Tooltip("Delay before continuing to next nodes")]
        private float _delay;

        /// <summary>Delay before continuing to next nodes.</summary>
        internal float Delay => Mathf.Max(0.0f, _delay);

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
