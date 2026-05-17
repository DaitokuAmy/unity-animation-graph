using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// bool フラグの値に応じて進行先を選ぶ分岐ノード
    /// </summary>
    public sealed class FlagBranchNode : BranchNode {
        [SerializeField, BlackboardKey(AnimationGraphValueType.Bool), Tooltip("判定に使うフラグキー")]
        private string _flagKey = string.Empty;
        [SerializeField, Tooltip("true 側へ進む期待値")]
        private bool _expectedValue = true;

        /// <summary>判定に使うフラグキー</summary>
        internal string FlagKey => _flagKey;
        /// <summary>true 側へ進む期待値</summary>
        internal bool ExpectedValue => _expectedValue;

        /// <inheritdoc/>
        protected override bool EvaluateConditionInternal(int seed, IAnimationGraphContext context) {
            if (string.IsNullOrEmpty(_flagKey)) {
                throw new InvalidOperationException($"FlagBranchNode '{NodeId}' has empty flag key");
            }

            if (!context.TryGetBlackboardValue(_flagKey, out bool value)) {
                throw new InvalidOperationException($"FlagBranchNode '{NodeId}' could not resolve flag '{_flagKey}'");
            }

            return value == _expectedValue;
        }
    }
}
