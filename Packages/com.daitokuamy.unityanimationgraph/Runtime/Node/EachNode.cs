using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// target collection の各要素に対して指定したノード集合を並列実行するノード
    /// </summary>
    public sealed class EachNode : ScopedControlNode, IIterationScopeProvider {
        [SerializeField, NodeDetailField("Collection"), TargetKey(TargetMultiplicity.Collection), Tooltip("並列実行する target collection key")]
        private string _collectionTargetKey = string.Empty;
        [SerializeField, NodeDetailField("Skip Null"), Tooltip("collection の null 要素をスキップする場合は有効")]
        private bool _skipNullItems;
        [SerializeField, NodeDetailField("Interval"), Min(0.0f), Tooltip("要素ごとの開始間隔")]
        private float _interval;
        [SerializeField, Tooltip("各要素に対して実行するノード ID 一覧"), HideInInspector]
        private string[] _eachNodeIds = Array.Empty<string>();

        /// <summary>並列実行する target collection key</summary>
        internal string CollectionTargetKey => _collectionTargetKey ?? string.Empty;
        /// <summary>collection の null 要素をスキップする場合は true</summary>
        internal bool SkipNullItems => _skipNullItems;
        /// <summary>要素ごとの開始間隔</summary>
        internal float Interval => Mathf.Max(0.0f, _interval);
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
