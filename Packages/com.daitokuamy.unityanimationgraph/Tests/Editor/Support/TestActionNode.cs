using System.Collections.Generic;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// テスト用 action node
    /// </summary>
    internal sealed class TestActionNode : ActionNode {
        private float _duration;
        private float _delay;
        private IList<string> _events;
        private string _eventPrefix;

        /// <summary>Evaluate が呼ばれた回数</summary>
        public int EvaluateCount { get; private set; }
        /// <summary>Enter が呼ばれた回数</summary>
        public int EnterCount { get; private set; }
        /// <summary>Exit が呼ばれた回数</summary>
        public int ExitCount { get; private set; }
        /// <summary>Cancel が呼ばれた回数</summary>
        public int CancelCount { get; private set; }
        /// <summary>最後に渡された localTime</summary>
        public float LastLocalTime { get; private set; }
        /// <summary>最後に渡された duration</summary>
        public float LastDuration { get; private set; }
        /// <summary>最後に渡された seed</summary>
        public int LastSeed { get; private set; }
        /// <summary>最後に Enter に渡された seed</summary>
        public int LastEnterSeed { get; private set; }
        /// <summary>最後に Exit に渡された seed</summary>
        public int LastExitSeed { get; private set; }
        /// <summary>最後に Cancel に渡された seed</summary>
        public int LastCancelSeed { get; private set; }

        /// <summary>
        /// テスト用の時間設定を更新
        /// </summary>
        /// <param name="duration">実行時間</param>
        /// <param name="delay">開始遅延</param>
        public void Configure(float duration, float delay = 0.0f) {
            _duration = duration;
            _delay = delay;
        }

        /// <summary>
        /// 実行順の記録先を設定
        /// </summary>
        /// <param name="events">イベント記録先</param>
        /// <param name="eventPrefix">イベント名の接頭辞</param>
        public void ConfigureEvents(IList<string> events, string eventPrefix) {
            _events = events;
            _eventPrefix = eventPrefix ?? string.Empty;
        }

        /// <inheritdoc/>
        protected override float CalculateDuration(int seed, IAnimationGraphContext context) {
            return _duration;
        }

        /// <inheritdoc/>
        protected override float CalculateDelay(int seed, IAnimationGraphContext context) {
            return _delay;
        }

        /// <inheritdoc/>
        protected override void Enter(int seed, IAnimationGraphContext context) {
            EnterCount++;
            LastEnterSeed = seed;
            RecordEvent("Enter");
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context) {
            EvaluateCount++;
            LastSeed = seed;
            LastLocalTime = localTime;
            LastDuration = calculatedDuration;
            RecordEvent("Evaluate");
        }

        /// <inheritdoc/>
        protected override void Exit(int seed, IAnimationGraphContext context) {
            ExitCount++;
            LastExitSeed = seed;
            RecordEvent("Exit");
        }

        /// <inheritdoc/>
        protected override void Cancel(int seed, IAnimationGraphContext context) {
            CancelCount++;
            LastCancelSeed = seed;
        }

        /// <summary>
        /// 実行順イベントを記録
        /// </summary>
        /// <param name="eventName">イベント名</param>
        private void RecordEvent(string eventName) {
            _events?.Add($"{_eventPrefix}.{eventName}");
        }
    }
}

