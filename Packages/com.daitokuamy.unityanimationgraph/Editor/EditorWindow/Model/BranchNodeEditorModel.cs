using System.Collections.Generic;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// BranchNode の編集情報を提供する NodeEditorModel
    /// </summary>
    public class BranchNodeEditorModel : NodeEditorModel {
        private readonly BranchNode _branchNode;

        /// <summary>拡張 Branch Port の数</summary>
        public int ExtensionPortCount => _branchNode.ExtensionPortCount;
        /// <summary>主 Branch Port の表示名</summary>
        public string PrimaryPortName => _branchNode.GetPortName(0);

        /// <summary>
        /// BranchNodeEditorModel を作成
        /// </summary>
        /// <param name="node">参照する BranchNode</param>
        internal BranchNodeEditorModel(BranchNode node) : base(node) {
            _branchNode = node;
        }

        /// <summary>
        /// 拡張 Branch Port の表示名を取得
        /// </summary>
        /// <param name="extensionIndex">取得する拡張 Branch Port index</param>
        /// <returns>拡張 Branch Port の表示名</returns>
        public string GetExtensionPortName(int extensionIndex) {
            return _branchNode.GetPortName(extensionIndex + 1);
        }

        /// <summary>
        /// 拡張 Branch Port の後続ノード ID 一覧を取得
        /// </summary>
        /// <param name="extensionIndex">取得する拡張 Branch Port index</param>
        /// <returns>拡張 Branch Port の後続ノード ID 一覧</returns>
        public IReadOnlyList<string> GetExtensionNodeIds(int extensionIndex) {
            return _branchNode.GetExtensionNodeIds(extensionIndex);
        }

        /// <summary>
        /// 拡張 Branch Port の後続ノード ID 一覧を設定
        /// </summary>
        /// <param name="extensionIndex">設定する拡張 Branch Port index</param>
        /// <param name="nodeIds">設定する後続ノード ID 一覧</param>
        internal void SetExtensionNodeIds(int extensionIndex, IReadOnlyList<string> nodeIds) {
            AnimationGraphAssetUtility.SetBranchExtensionNodeIds(_branchNode, extensionIndex, nodeIds);
        }
    }
}
