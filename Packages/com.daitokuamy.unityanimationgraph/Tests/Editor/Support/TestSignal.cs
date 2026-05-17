using System.Collections.Generic;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// テスト用 signal
    /// </summary>
    internal sealed class TestSignal : Signal {
        private IList<string> _events;
        private string _eventName;

        /// <summary>Dispatch が呼ばれた回数</summary>
        public int DispatchCount { get; private set; }
        /// <summary>最後に Dispatch に渡された seed</summary>
        public int LastSeed { get; private set; }

        /// <summary>
        /// テスト用の記録先を設定
        /// </summary>
        /// <param name="eventName">記録するイベント名</param>
        /// <param name="events">イベント記録先</param>
        public void Configure(string eventName, IList<string> events) {
            _eventName = eventName ?? string.Empty;
            _events = events;
        }

        /// <inheritdoc/>
        protected override void Dispatch(int seed, IAnimationGraphContext context) {
            DispatchCount++;
            LastSeed = seed;
            if (!string.IsNullOrEmpty(_eventName)) {
                _events?.Add(_eventName);
            }
        }
    }
}

