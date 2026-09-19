using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// EachNode の要素開始間隔の決定方法
    /// </summary>
    public enum EachStaggerMode {
        /// <summary>一定間隔</summary>
        Interval,
        /// <summary>ランダムな待機時間</summary>
        RandomDelay,
    }

    /// <summary>
    /// target collection の各要素に対して指定したノード集合を並列実行するノード
    /// </summary>
    public sealed class EachNode : ScopedControlNode, IIterationScopeProvider {
        [SerializeField, NodeDetailField("Collection"), TargetKey(TargetMultiplicity.Collection), Tooltip("並列実行する target collection key")]
        private string _collectionTargetKey = string.Empty;
        [SerializeField, NodeDetailField("Skip Null"), Tooltip("collection の null 要素をスキップする場合は有効")]
        private bool _skipNullItems;
        [SerializeField, NodeDetailField("Stagger"), Tooltip("要素の開始をずらす方法")]
        private EachStaggerMode _staggerMode;
        [SerializeField, NodeDetailField("Interval"), Min(0.0f), Tooltip("要素ごとの開始間隔")]
        private float _staggerDelay;
        [SerializeField, NodeDetailField("Min Delay"), Min(0.0f), Tooltip("Each 開始位置から要素を開始するまでの最小待機時間")]
        private float _minStaggerDelay;
        [SerializeField, NodeDetailField("Max Delay"), Min(0.0f), Tooltip("Each 開始位置から要素を開始するまでの最大待機時間")]
        private float _maxStaggerDelay;
        [SerializeField, Tooltip("各要素に対して実行するノード ID 一覧"), HideInInspector]
        private string[] _eachNodeIds = Array.Empty<string>();

        /// <summary>並列実行する target collection key</summary>
        internal string CollectionTargetKey => _collectionTargetKey ?? string.Empty;
        /// <summary>collection の null 要素をスキップする場合は true</summary>
        internal bool SkipNullItems => _skipNullItems;
        /// <summary>要素ごとの開始間隔の決定方法</summary>
        internal EachStaggerMode StaggerMode => _staggerMode;
        /// <summary>要素ごとの開始間隔</summary>
        internal float StaggerDelay => Mathf.Max(0.0f, _staggerDelay);
        /// <summary>ランダムな開始間隔の最小値</summary>
        internal float MinStaggerDelay => Mathf.Max(0.0f, Mathf.Min(_minStaggerDelay, _maxStaggerDelay));
        /// <summary>ランダムな開始間隔の最大値</summary>
        internal float MaxStaggerDelay => Mathf.Max(0.0f, Mathf.Max(_minStaggerDelay, _maxStaggerDelay));
        /// <summary>各要素に対して実行するノード ID 一覧</summary>
        internal IReadOnlyList<string> EachNodeIds => _eachNodeIds ?? Array.Empty<string>();
        /// <inheritdoc/>
        internal override IReadOnlyList<string> BodyNodeIds => EachNodeIds;

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

        /// <inheritdoc/>
        protected override string Validate(NodeValidationContext context) {
            if (string.IsNullOrEmpty(CollectionTargetKey)) {
                return $"{DisplayName} has empty collection target key";
            }

            return context.TryGetTargetDefinition(CollectionTargetKey, out var definition)
                && definition.Multiplicity == TargetMultiplicity.Collection
                ? string.Empty
                : $"{DisplayName} references missing collection target '{CollectionTargetKey}'";
        }
    }
}
