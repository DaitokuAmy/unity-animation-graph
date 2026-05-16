using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// ParticleSystem を手動シミュレーションで再生するノード
    /// </summary>
    [AnimationGraphNode("Play Particle System", "Built-in/Play Particle System")]
    public sealed class PlayParticleSystemNode : ActionNode {
        [SerializeField, Min(0.0f), Tooltip("ParticleSystem の duration とノード実行時間を同期する値。0 の場合は ParticleSystem の duration を使用")]
        private float _duration;

        /// <summary>ParticleSystem と同期する実行時間</summary>
        public float Duration => Mathf.Max(0.0f, _duration);

        /// <inheritdoc/>
        protected override float CalculateDuration(int seed, IAnimationGraphContext context) {
            if (Duration > 0.0f) {
                return Duration;
            }

            if (!TryResolveParticleSystem(context, out var particleSystem)) {
                return 0.0f;
            }

            var main = particleSystem.main;
            return Mathf.Max(0.0f, main.duration);
        }

        /// <inheritdoc/>
        protected override float CalculateDelay(int seed, IAnimationGraphContext context) {
            return 0.0f;
        }

        /// <inheritdoc/>
        protected override void BeginPlayback(int seed, IAnimationGraphContext context) {
            if (!TryResolveParticleSystem(context, out var particleSystem)) {
                return;
            }

            ApplySeed(particleSystem, seed);

            if (Duration > 0.0f) {
                SyncDuration(particleSystem, Duration);
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Simulate(0.0f, true, true, true);
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context) {
            if (!TryResolveParticleSystem(context, out var particleSystem)) {
                return;
            }

            ApplySeed(particleSystem, seed);

            var duration = ResolveSynchronizedDuration(particleSystem, calculatedDuration);
            SyncDuration(particleSystem, duration);

            var simulationTime = Mathf.Max(0.0f, localTime);
            if (duration > 0.0f) {
                simulationTime = Mathf.Min(simulationTime, duration);
            }

            particleSystem.Simulate(simulationTime, true, true, true);
        }

        /// <inheritdoc/>
        protected override void Cancel(int seed, IAnimationGraphContext context) {
            if (!TryResolveParticleSystem(context, out var particleSystem)) {
                return;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        /// <summary>
        /// TargetKey から ParticleSystem の解決を試行
        /// </summary>
        /// <param name="context">評価コンテキスト</param>
        /// <param name="particleSystem">解決した ParticleSystem</param>
        /// <returns>解決できた場合は true</returns>
        private bool TryResolveParticleSystem(IAnimationGraphContext context, out ParticleSystem particleSystem) {
            particleSystem = null;
            if (context == null) {
                return false;
            }

            if (string.IsNullOrEmpty(TargetKey)) {
                return false;
            }

            if (!context.TryGetTarget<ParticleSystem>(TargetKey, out particleSystem) || particleSystem == null) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 評価用シードを ParticleSystem に反映
        /// </summary>
        /// <param name="particleSystem">反映先 ParticleSystem</param>
        /// <param name="seed">評価に使用するシード</param>
        private static void ApplySeed(ParticleSystem particleSystem, int seed) {
            particleSystem.useAutoRandomSeed = false;
            particleSystem.randomSeed = ToRandomSeed(seed);
        }

        /// <summary>
        /// 評価用シードを ParticleSystem.randomSeed 用に変換
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <returns>ParticleSystem.randomSeed に設定する値</returns>
        private static uint ToRandomSeed(int seed) {
            var randomSeed = unchecked((uint)seed);
            return randomSeed == 0u ? 1u : randomSeed;
        }

        /// <summary>
        /// ParticleSystem に同期する実行時間を決定
        /// </summary>
        /// <param name="particleSystem">同期対象 ParticleSystem</param>
        /// <param name="calculatedDuration">計算済みの実行時間</param>
        /// <returns>同期する実行時間</returns>
        private float ResolveSynchronizedDuration(ParticleSystem particleSystem, float calculatedDuration) {
            if (calculatedDuration > 0.0f) {
                return calculatedDuration;
            }

            if (Duration > 0.0f) {
                return Duration;
            }

            var main = particleSystem.main;
            return Mathf.Max(0.0f, main.duration);
        }

        /// <summary>
        /// ParticleSystem.main.duration を同期
        /// </summary>
        /// <param name="particleSystem">同期対象 ParticleSystem</param>
        /// <param name="duration">同期する実行時間</param>
        private static void SyncDuration(ParticleSystem particleSystem, float duration) {
            if (duration <= 0.0f) {
                return;
            }

            var main = particleSystem.main;
            if (!Mathf.Approximately(main.duration, duration)) {
                main.duration = duration;
            }
        }
    }
}
