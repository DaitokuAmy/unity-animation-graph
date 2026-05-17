using UnityAnimationGraph;
using UnityEngine;

namespace Sample {
    /// <summary>
    /// Sample 用に通過通知を Console へ出力する Signal
    /// </summary>
    [SignalInfo("サンプルログ", "Sample/サンプルログ")]
    public sealed class SampleLogSignal : Signal {
        [SerializeField, Tooltip("Console に出力するメッセージ")]
        private string _message = "Sample Signal";
        [SerializeField, Tooltip("評価 seed をメッセージに含める")]
        private bool _includeSeed = true;

        /// <summary>Console に出力するメッセージ</summary>
        public string Message => _message ?? string.Empty;
        /// <summary>評価 seed をメッセージに含める場合は true</summary>
        public bool IncludeSeed => _includeSeed;

        /// <inheritdoc/>
        protected override void Dispatch(int seed, IAnimationGraphContext context) {
            if (_includeSeed) {
                Debug.Log($"{Message} (seed: {seed})");
                return;
            }

            Debug.Log(Message);
        }
    }
}
