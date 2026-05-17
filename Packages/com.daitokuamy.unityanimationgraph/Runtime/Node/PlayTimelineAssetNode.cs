using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityAnimationGraph {
    /// <summary>
    /// PlayableDirector で TimelineAsset を再生するノード
    /// </summary>
    [NodeInfo("Play Timeline Asset", "Built-in/Play Timeline Asset")]
    public sealed class PlayTimelineAssetNode : ActionNode<PlayableDirector> {
        [SerializeField, Tooltip("PlayableDirector に設定する TimelineAsset")]
        private TimelineAsset _timelineAsset;

        /// <inheritdoc/>
        protected override IEnumerable<string> GetPreviewProperties(PlayableDirector playableDirector) {
            yield return PreviewPropertyPaths.PlayableDirector.PlayableAsset;
            yield return PreviewPropertyPaths.PlayableDirector.DirectorUpdateMode;
        }

        /// <inheritdoc/>
        protected override float CalculateDuration(int seed, PlayableDirector playableDirector, IAnimationGraphBlackboard blackboard) {
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
        protected override void Enter(int seed, PlayableDirector playableDirector, IAnimationGraphBlackboard blackboard) {
            if (_timelineAsset == null) {
                return;
            }

            if (playableDirector.playableAsset != _timelineAsset) {
                playableDirector.playableAsset = _timelineAsset;
            }

            if (playableDirector.timeUpdateMode != DirectorUpdateMode.Manual) {
                playableDirector.timeUpdateMode = DirectorUpdateMode.Manual;
            }

            playableDirector.RebuildGraph();
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, PlayableDirector playableDirector, float localTime, float calculatedDuration, IAnimationGraphBlackboard blackboard) {
            if (_timelineAsset == null) {
                return;
            }

            playableDirector.time = localTime;
#if UNITY_EDITOR
            if (AnimationMode.InAnimationMode()) {
                AnimationMode.SamplePlayableGraph(playableDirector.playableGraph, 0, localTime);
                return;
            }
#endif

            playableDirector.Evaluate();
        }
    }
}
