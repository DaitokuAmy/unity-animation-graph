using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// LoopNode の反復回数取得元
    /// </summary>
    public enum LoopCountSource {
        /// <summary>固定値</summary>
        Fixed,
        /// <summary>target collection の要素数</summary>
        Collection,
    }

    /// <summary>
    /// 指定したノード集合を繰り返し実行するノード
    /// </summary>
    public sealed class LoopNode : ScopedControlNode, IIterationScopeProvider {
        [SerializeField, Tooltip("ループ実行回数の取得元")]
        private LoopCountSource _loopCountSource;
        [SerializeField, Min(0), Tooltip("ループ開始 index")]
        private int _startIndex;
        [SerializeField, Min(0), Tooltip("固定ループ実行回数")]
        private int _loopCount = 1;
        [SerializeField, TargetKey(TargetMultiplicity.Collection), Tooltip("ループ実行回数を解決する target collection key")]
        private string _loopCountTargetKey = string.Empty;
        [SerializeField, NodeDetailField("Skip Null"), Tooltip("count target の null 要素の反復をスキップする場合は有効")]
        private bool _skipNullItems;
        [SerializeField, Tooltip("ループ内容のノード ID 一覧"), HideInInspector]
        private string[] _loopNodeIds = Array.Empty<string>();

        /// <summary>ループ実行回数の取得元</summary>
        internal LoopCountSource CountSource => _loopCountSource;
        /// <summary>ループ開始 index</summary>
        internal int StartIndex => Mathf.Max(0, _startIndex);
        /// <summary>固定ループ実行回数</summary>
        internal int LoopCount => Mathf.Max(0, _loopCount);
        /// <summary>ループ実行回数を解決する target collection key</summary>
        internal string LoopCountTargetKey => _loopCountTargetKey ?? string.Empty;
        /// <summary>count target の null 要素をスキップする場合は true</summary>
        internal bool SkipNullItems => _skipNullItems;
        /// <summary>ループ内容のノード ID 一覧</summary>
        internal IReadOnlyList<string> LoopNodeIds => _loopNodeIds ?? Array.Empty<string>();
        /// <inheritdoc/>
        internal override IReadOnlyList<string> BodyNodeIds => LoopNodeIds;

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
