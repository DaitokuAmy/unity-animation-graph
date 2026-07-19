using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// target 参照の解決方法
    /// </summary>
    public enum TargetReferenceKind {
        /// <summary>Runner binding</summary>
        Binding,
        /// <summary>反復 scope の index に対応する collection 要素</summary>
        CollectionItem,
    }

    /// <summary>
    /// binding または scope が提供する target への参照
    /// </summary>
    [Serializable]
    public struct TargetReference {
        [SerializeField, Tooltip("target 参照の解決方法")]
        private TargetReferenceKind _kind;
        [SerializeField, Tooltip("参照する target key")]
        private string _targetKey;
        [SerializeField, Tooltip("scope を提供する node ID")]
        private string _scopeNodeId;

        /// <summary>target 参照の解決方法</summary>
        public TargetReferenceKind Kind => _kind;
        /// <summary>参照する target key</summary>
        public string TargetKey => _targetKey ?? string.Empty;
        /// <summary>scope を提供する node ID</summary>
        public string ScopeNodeId => _scopeNodeId ?? string.Empty;

        /// <summary>
        /// TargetReference を生成
        /// </summary>
        /// <param name="kind">target 参照の解決方法</param>
        /// <param name="targetKey">参照する target key</param>
        /// <param name="scopeNodeId">scope を提供する node ID</param>
        public TargetReference(TargetReferenceKind kind, string targetKey, string scopeNodeId = null) {
            _kind = kind;
            _targetKey = targetKey ?? string.Empty;
            _scopeNodeId = scopeNodeId ?? string.Empty;
        }
    }
}
