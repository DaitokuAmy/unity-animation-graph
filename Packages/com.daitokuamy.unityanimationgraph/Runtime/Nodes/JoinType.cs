namespace UnityAnimationGraph {
    /// <summary>
    /// JoinNode の合流方法
    /// </summary>
    public enum JoinType {
        /// <summary>すべての入力を待つ</summary>
        All,
        /// <summary>最も早く到達する入力を使う</summary>
        Any,
    }
}
