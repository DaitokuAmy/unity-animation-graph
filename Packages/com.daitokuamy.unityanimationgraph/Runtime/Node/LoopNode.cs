using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// 指定したノード集合を繰り返し実行するノード
    /// </summary>
    public sealed class LoopNode : ControlNode {
        [SerializeField, Min(1), Tooltip("ループ実行回数")]
        private int _loopCount = 1;
        [SerializeField, Tooltip("ループ内容のノード ID 一覧"), HideInInspector]
        private string[] _loopNodeIds = Array.Empty<string>();

        /// <summary>ループ実行回数</summary>
        internal int LoopCount => Mathf.Max(1, _loopCount);
        /// <summary>ループ内容のノード ID 一覧</summary>
        internal IReadOnlyList<string> LoopNodeIds => _loopNodeIds ?? Array.Empty<string>();

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
    }
}
