using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// bool フラグの値に応じて進行先を選ぶ分岐ノード
    /// </summary>
    public sealed class FlagBranchNode : BranchNode {
        [SerializeField, BlackboardKey(BlackboardValueType.Bool), NodeDetailField("Flag"), Tooltip("判定に使うフラグキー")]
        private string _flagKey = string.Empty;
        [SerializeField, NodeDetailField("Expected"), Tooltip("true 側へ進む期待値")]
        private bool _expectedValue = true;

        /// <inheritdoc/>
        protected override int EvaluateBranchPortIndex(int seed, IAnimationGraphContext context) {
            if (string.IsNullOrEmpty(_flagKey)) {
                throw new InvalidOperationException($"FlagBranchNode '{NodeId}' has empty flag key");
            }

            if (!context.TryGetBlackboardValue(_flagKey, out bool value)) {
                throw new InvalidOperationException($"FlagBranchNode '{NodeId}' could not resolve flag '{_flagKey}'");
            }

            return value == _expectedValue ? 0 : 1;
        }

        /// <inheritdoc/>
        protected override int GetExtensionPortCount() {
            return 1;
        }

        /// <inheritdoc/>
        protected override string GetPortNameInternal(int portIndex) {
            return portIndex == 0 ? "True" : "False";
        }

        /// <inheritdoc/>
        protected override string Validate(NodeValidationContext context) {
            if (string.IsNullOrEmpty(_flagKey)) {
                return $"{DisplayName} has empty flag key";
            }

            if (!context.TryGetBlackboardDefinition(_flagKey, out var definition)) {
                return $"{DisplayName} references missing Blackboard '{_flagKey}'";
            }

            return definition.ValueType == BlackboardValueType.Bool
                ? string.Empty
                : $"{DisplayName} references non-bool Blackboard '{_flagKey}'";
        }
    }
}
