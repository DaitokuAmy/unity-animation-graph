using System.Collections.Generic;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// LoopNode の編集情報を提供する NodeEditorModel
    /// </summary>
    public sealed class LoopNodeEditorModel : NodeEditorModel {
        private readonly LoopNode _loopNode;

        /// <summary>ループ実行回数</summary>
        public int LoopCount => _loopNode.LoopCount;
        /// <summary>ループ内容のノード ID 一覧</summary>
        public IReadOnlyList<string> LoopNodeIds => _loopNode.LoopNodeIds;

        /// <summary>
        /// LoopNodeEditorModel を作成
        /// </summary>
        /// <param name="node">参照する LoopNode</param>
        internal LoopNodeEditorModel(LoopNode node) : base(node) {
            _loopNode = node;
        }

        /// <summary>
        /// Sets the loop execution count.
        /// </summary>
        /// <param name="loopCount">Loop execution count</param>
        internal void SetLoopCount(int loopCount) {
            AnimationGraphAssetUtility.SetLoopNodeLoopCount(_loopNode, loopCount);
        }

        /// <summary>
        /// ループ内容のノード ID 一覧を設定
        /// </summary>
        /// <param name="loopNodeIds">設定するループ内容のノード ID 一覧</param>
        internal void SetLoopNodeIds(IReadOnlyList<string> loopNodeIds) {
            AnimationGraphAssetUtility.SetLoopNodeIds(_loopNode, loopNodeIds);
        }
    }
}
