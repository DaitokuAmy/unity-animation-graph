using System.Collections.Generic;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// BranchNode の編集情報を提供する NodeEditorModel
    /// </summary>
    public sealed class BranchNodeEditorModel : NodeEditorModel {
        private readonly BranchNode _branchNode;

        /// <summary>false 側の後続ノード ID 一覧</summary>
        public IReadOnlyList<string> FalseNodeIds => _branchNode.FalseNodeIds;

        /// <summary>
        /// BranchNodeEditorModel を作成
        /// </summary>
        /// <param name="node">参照する BranchNode</param>
        internal BranchNodeEditorModel(BranchNode node) : base(node) {
            _branchNode = node;
        }

        /// <summary>
        /// false 側の後続ノード ID 一覧を設定
        /// </summary>
        /// <param name="falseNodeIds">設定する false 側の後続ノード ID 一覧</param>
        internal void SetFalseNodeIds(IReadOnlyList<string> falseNodeIds) {
            AnimationGraphAssetUtility.SetBranchFalseNodeIds(_branchNode, falseNodeIds);
        }
    }
}
