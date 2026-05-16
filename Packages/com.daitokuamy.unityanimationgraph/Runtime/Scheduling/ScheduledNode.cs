namespace UnityAnimationGraph {
    /// <summary>
    /// スケジュール済みノード
    /// </summary>
    public readonly struct ScheduledNode {
        /// <summary>元ノード</summary>
        public Node Node { get; }
        /// <summary>開始時刻</summary>
        public float StartTime { get; }
        /// <summary>終了時刻</summary>
        public float EndTime => StartTime + Duration;
        /// <summary>開始遅延</summary>
        public float Delay { get; }
        /// <summary>実行時間</summary>
        public float Duration { get; }
        /// <summary>評価に使用するシード</summary>
        public int Seed { get; }
        /// <summary>安定順序</summary>
        internal int StableOrder { get; }

        /// <summary>
        /// ScheduledNode を生成
        /// </summary>
        /// <param name="node">元ノード</param>
        /// <param name="startTime">開始時刻</param>
        /// <param name="delay">開始遅延</param>
        /// <param name="duration">実行時間</param>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="stableOrder">安定順序</param>
        internal ScheduledNode(Node node, float startTime, float delay, float duration, int seed, int stableOrder) {
            Node = node;
            StartTime = startTime;
            Delay = delay;
            Duration = duration;
            Seed = seed;
            StableOrder = stableOrder;
        }
    }
}
