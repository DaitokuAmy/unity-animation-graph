using System.Collections.Generic;

namespace UnityAnimationGraph {
    /// <summary>
    /// 所有する body scope を実行する制御ノードの基底クラス
    /// </summary>
    public abstract class ScopedControlNode : ControlNode {
        /// <summary>body scope のノード ID 一覧</summary>
        internal abstract IReadOnlyList<string> BodyNodeIds { get; }
    }
}
