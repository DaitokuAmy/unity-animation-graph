using System.Collections.Generic;
using UnityEngine;

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

    /// <summary>
    /// 解決された Transform を記録するテスト用 ActionNode
    /// </summary>
    internal sealed class TestTargetActionNode : ActionNode<Transform> {
        private IList<Transform> _targets;

        /// <summary>
        /// target の記録先を設定
        /// </summary>
        public void Configure(IList<Transform> targets) {
            _targets = targets;
        }

        /// <inheritdoc/>
        protected override float CalculateDuration(int seed, Transform target, IAnimationGraphBlackboard blackboard) {
            return 1.0f;
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, Transform target, float localTime, float calculatedDuration, IAnimationGraphBlackboard blackboard) {
            _targets?.Add(target);
        }
    }

    /// <summary>
    /// 実行単位の state を使用するテスト用 ActionNode
    /// </summary>
    internal sealed class TestStatefulActionNode : ActionNode {
        private sealed class State : IActionNodeState {
            /// <summary>state の識別子</summary>
            public int Id { get; }
            /// <summary>Enter が呼ばれた回数</summary>
            public int EnterCount { get; set; }

            /// <summary>
            /// State を生成
            /// </summary>
            /// <param name="id">state の識別子</param>
            public State(int id) {
                Id = id;
            }
        }

        private float _duration;

        /// <summary>生成した state の数</summary>
        public int CreatedStateCount { get; private set; }
        /// <summary>Enter 時の state 内呼び出し回数一覧</summary>
        public List<int> EnterCountsByState { get; } = new();
        /// <summary>Enter 時に使用した state ID 一覧</summary>
        public List<int> EnterStateIds { get; } = new();
        /// <summary>Cancel 時に使用した state ID 一覧</summary>
        public List<int> CancelStateIds { get; } = new();

        /// <summary>
        /// 実行時間を設定
        /// </summary>
        /// <param name="duration">実行時間</param>
        public void Configure(float duration) {
            _duration = duration;
        }

        /// <inheritdoc/>
        protected override IActionNodeState CreateState() {
            CreatedStateCount++;
            return new State(CreatedStateCount);
        }

        /// <inheritdoc/>
        protected override float CalculateDuration(int seed, IAnimationGraphContext context) {
            return _duration;
        }

        /// <inheritdoc/>
        protected override float CalculateDelay(int seed, IAnimationGraphContext context) {
            return 0.0f;
        }

        /// <inheritdoc/>
        protected override void Enter(int seed, IAnimationGraphContext context, IActionNodeState state) {
            var typedState = (State)state;
            typedState.EnterCount++;
            EnterCountsByState.Add(typedState.EnterCount);
            EnterStateIds.Add(typedState.Id);
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context) {
        }

        /// <inheritdoc/>
        protected override void Cancel(int seed, IAnimationGraphContext context, IActionNodeState state) {
            CancelStateIds.Add(((State)state).Id);
        }
    }
}
