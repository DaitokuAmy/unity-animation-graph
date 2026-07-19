using System.Collections.Generic;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// body scope を持つ ControlNode の編集情報を提供する基底 model
    /// </summary>
    public abstract class ScopedControlNodeEditorModel : NodeEditorModel {
        /// <summary>body scope のノード ID 一覧</summary>
        public abstract IReadOnlyList<string> BodyNodeIds { get; }

        /// <summary>
        /// ScopedControlNodeEditorModel を作成
        /// </summary>
        protected ScopedControlNodeEditorModel(ScopedControlNode node) : base(node) {
        }

        /// <summary>
        /// body scope のノード ID 一覧を設定
        /// </summary>
        internal abstract void SetBodyNodeIds(IReadOnlyList<string> bodyNodeIds);
    }
}
