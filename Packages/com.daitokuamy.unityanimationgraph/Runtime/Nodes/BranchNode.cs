using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// 条件に応じて進行先を選ぶ分岐ノード
    /// </summary>
    public abstract class BranchNode : ControlNode {
        [SerializeField, Tooltip("false の場合に進むノード ID 一覧"), HideInInspector]
        private string[] _falseNodeIds = Array.Empty<string>();

        /// <summary>true の場合に進むノード ID 一覧</summary>
        public IReadOnlyList<string> TrueNodeIds => NextNodeIds;
        /// <summary>false の場合に進むノード ID 一覧</summary>
        public IReadOnlyList<string> FalseNodeIds => _falseNodeIds ?? Array.Empty<string>();

        /// <summary>
        /// true 側へ進む場合は true を返す
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        /// <returns>true 側へ進む場合は true</returns>
        public bool EvaluateCondition(int seed, IAnimationGraphContext context) {
            return EvaluateConditionInternal(seed, context);
        }

        /// <inheritdoc/>
        protected override float CalculateDuration(int seed, IAnimationGraphContext context) {
            return 0.0f;
        }

        /// <inheritdoc/>
        protected override float CalculateDelay(int seed, IAnimationGraphContext context) {
            return 0.0f;
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context) {
        }

        /// <summary>
        /// true 側へ進む場合は true を返す
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        /// <returns>true 側へ進む場合は true</returns>
        protected abstract bool EvaluateConditionInternal(int seed, IAnimationGraphContext context);
    }
}
