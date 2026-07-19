namespace UnityAnimationGraph {
    /// <summary>
    /// schedule node に関連付ける反復 scope
    /// </summary>
    internal sealed class IterationScope {
        /// <summary>scope を提供する node ID</summary>
        public string ScopeNodeId { get; }
        /// <summary>collection の解決に使用する反復 index</summary>
        public int IterationIndex { get; }
        /// <summary>外側の反復 scope</summary>
        public IterationScope Parent { get; }

        /// <summary>
        /// IterationScope を生成
        /// </summary>
        public IterationScope(string scopeNodeId, int iterationIndex, IterationScope parent) {
            ScopeNodeId = scopeNodeId ?? string.Empty;
            IterationIndex = iterationIndex;
            Parent = parent;
        }
    }
}
