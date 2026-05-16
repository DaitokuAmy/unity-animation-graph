using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Animation Graph でターゲットを操作するノードの基底クラス
    /// </summary>
    public abstract class ActionNode : Node {
        [SerializeField, AnimationGraphTargetKey, Tooltip("操作対象を解決するためのターゲットキー")]
        private string _targetKey = string.Empty;

        /// <summary>操作対象を解決するためのターゲットキー</summary>
        public string TargetKey => _targetKey;
    }
}
