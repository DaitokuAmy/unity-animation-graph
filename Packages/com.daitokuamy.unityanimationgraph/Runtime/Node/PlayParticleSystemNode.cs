using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// ParticleSystem を手動シミュレーションで再生するノード
    /// </summary>
    [NodeInfo("Play Particle System", "Built-in/Play Particle System")]
    public sealed class PlayParticleSystemNode : ActionNode<ParticleSystem> {
        [SerializeField, Tooltip("ParticleSystem の duration を実行時間として使用するか")]
        private bool _autoDuration = true;
        [SerializeField, Min(0.0f), Tooltip("Auto Duration が無効な場合の実行時間")]
        private float _duration = 1.0f;

        /// <inheritdoc/>
        protected override IEnumerable<string> GetPreviewProperties(ParticleSystem particleSystem) {
            yield return "autoRandomSeed";
            yield return "randomSeed";
        }

        /// <inheritdoc/>
        protected override float CalculateDuration(int seed, ParticleSystem particleSystem, IAnimationGraphBlackboard blackboard) {
            if (!_autoDuration) {
                return Mathf.Max(0.0f, _duration);
            }

            var main = particleSystem.main;
            return Mathf.Max(0.0f, main.duration);
        }

        /// <inheritdoc/>
        protected override void Enter(int seed, ParticleSystem particleSystem, IAnimationGraphBlackboard blackboard, IActionNodeState state) {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var randomSeed = unchecked((uint)seed);
            particleSystem.useAutoRandomSeed = false;
            particleSystem.randomSeed = randomSeed == 0u ? 1u : randomSeed;
            particleSystem.Simulate(0.0f, true, true, true);
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, ParticleSystem particleSystem, float localTime, float calculatedDuration, IAnimationGraphBlackboard blackboard, IActionNodeState state) {
            var duration = calculatedDuration;
            if (duration <= 0.0f) {
                var main = particleSystem.main;
                duration = Mathf.Max(0.0f, main.duration);
            }

            var simulationTime = Mathf.Max(0.0f, localTime);
            if (duration > 0.0f) {
                simulationTime = Mathf.Min(simulationTime, duration);
            }

            if (simulationTime <= 0.0f) {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            particleSystem.Simulate(simulationTime, true, true, true);
        }

        /// <inheritdoc/>
        protected override void Cancel(int seed, ParticleSystem particleSystem, IAnimationGraphBlackboard blackboard, IActionNodeState state) {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
