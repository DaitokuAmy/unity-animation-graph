using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityAnimationGraph {
    /// <summary>
    /// PlayableDirector で TimelineAsset を再生するノード
    /// </summary>
    [AnimationGraphNode("Play Timeline Asset", "Built-in/Play Timeline Asset")]
    public sealed class PlayTimelineAssetNode : ActionNode {
        [SerializeField, Tooltip("PlayableDirector に設定する TimelineAsset")]
        private TimelineAsset _timelineAsset;

        /// <summary>PlayableDirector に設定する TimelineAsset</summary>
        public TimelineAsset TimelineAsset => _timelineAsset;

        /// <inheritdoc/>
        protected override float CalculateDuration(int seed, IAnimationGraphContext context) {
            if (_timelineAsset == null) {
                return 0.0f;
            }

            var duration = _timelineAsset.duration;
            if (double.IsNaN(duration) || duration <= 0.0) {
                return 0.0f;
            }

            return duration >= float.MaxValue ? float.MaxValue : (float)duration;
        }

        /// <inheritdoc/>
        protected override float CalculateDelay(int seed, IAnimationGraphContext context) {
            return 0.0f;
        }

        /// <inheritdoc/>
        protected override void BeginPlayback(int seed, IAnimationGraphContext context) {
            if (!TryResolvePlayableDirector(context, out var playableDirector)) {
                return;
            }

            playableDirector.playableAsset = _timelineAsset;
            playableDirector.timeUpdateMode = DirectorUpdateMode.Manual;
            playableDirector.RebuildGraph();
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context) {
            if (!TryResolvePlayableDirector(context, out var playableDirector)) {
                return;
            }

            playableDirector.time = localTime;
            playableDirector.Evaluate();
        }

        /// <summary>
        /// TargetKey から PlayableDirector の解決を試行
        /// </summary>
        /// <param name="context">評価コンテキスト</param>
        /// <param name="playableDirector">解決した PlayableDirector</param>
        /// <returns>解決できた場合は true</returns>
        private bool TryResolvePlayableDirector(IAnimationGraphContext context, out PlayableDirector playableDirector) {
            playableDirector = null;
            if (context == null) {
                return false;
            }

            if (string.IsNullOrEmpty(TargetKey)) {
                return false;
            }

            if (_timelineAsset == null) {
                return false;
            }

            if (!context.TryGetTarget<PlayableDirector>(TargetKey, out playableDirector) || playableDirector == null) {
                return false;
            }

            return true;
        }
    }
}
