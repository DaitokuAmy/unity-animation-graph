using System;
using System.Collections.Generic;

namespace UnityAnimationGraph {
    /// <summary>
    /// Animation Graph の評価スケジュール
    /// </summary>
    public sealed class AnimationGraphSchedule {
        /// <summary>スケジュール済みノード一覧</summary>
        public IReadOnlyList<ScheduledNode> Nodes { get; }
        /// <summary>スケジュール全体の長さ</summary>
        public float Duration { get; }

        /// <summary>
        /// AnimationGraphSchedule を生成
        /// </summary>
        /// <param name="nodes">スケジュール済みノード一覧</param>
        /// <param name="duration">スケジュール全体の長さ</param>
        internal AnimationGraphSchedule(IReadOnlyList<ScheduledNode> nodes, float duration) {
            Nodes = nodes ?? Array.Empty<ScheduledNode>();
            Duration = duration;
        }
    }
}