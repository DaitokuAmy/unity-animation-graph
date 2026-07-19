using System.Collections.Generic;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// LoopNode の編集情報を提供する NodeEditorModel
    /// </summary>
    public sealed class LoopNodeEditorModel : ScopedControlNodeEditorModel {
        private readonly LoopNode _loopNode;

        /// <summary>ループ実行回数</summary>
        public int LoopCount => _loopNode.LoopCount;
        /// <summary>ループ実行回数の取得元</summary>
        public LoopCountSource CountSource => _loopNode.CountSource;
        /// <summary>ループ開始 index</summary>
        public int StartIndex => _loopNode.StartIndex;
        /// <summary>ループ実行回数を解決する target collection key</summary>
        public string LoopCountTargetKey => _loopNode.LoopCountTargetKey;
        /// <summary>ループ内容のノード ID 一覧</summary>
        public IReadOnlyList<string> LoopNodeIds => _loopNode.LoopNodeIds;
        /// <inheritdoc/>
        public override IReadOnlyList<string> BodyNodeIds => LoopNodeIds;

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

        /// <summary>ループ実行回数の取得元を設定</summary>
        internal void SetCountSource(LoopCountSource countSource) {
            AnimationGraphAssetUtility.SetLoopNodeCountSource(_loopNode, countSource);
        }

        /// <summary>ループ開始 index を設定</summary>
        internal void SetStartIndex(int startIndex) {
            AnimationGraphAssetUtility.SetLoopNodeStartIndex(_loopNode, startIndex);
        }

        /// <summary>count collection target key を設定</summary>
        internal void SetLoopCountTargetKey(string targetKey) {
            AnimationGraphAssetUtility.SetLoopNodeCountTargetKey(_loopNode, targetKey);
        }

        /// <summary>
        /// ループ内容のノード ID 一覧を設定
        /// </summary>
        /// <param name="loopNodeIds">設定するループ内容のノード ID 一覧</param>
        internal void SetLoopNodeIds(IReadOnlyList<string> loopNodeIds) {
            AnimationGraphAssetUtility.SetLoopNodeIds(_loopNode, loopNodeIds);
        }

        /// <inheritdoc/>
        internal override void SetBodyNodeIds(IReadOnlyList<string> bodyNodeIds) {
            SetLoopNodeIds(bodyNodeIds);
        }
    }
}
