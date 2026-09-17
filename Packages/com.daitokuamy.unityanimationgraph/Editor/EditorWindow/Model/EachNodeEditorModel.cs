using System.Collections.Generic;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// EachNode の編集情報を提供する NodeEditorModel
    /// </summary>
    public sealed class EachNodeEditorModel : ScopedControlNodeEditorModel {
        private readonly EachNode _eachNode;

        /// <summary>各要素に対して実行するノード ID 一覧</summary>
        public IReadOnlyList<string> EachNodeIds => _eachNode.EachNodeIds;
        /// <inheritdoc/>
        public override IReadOnlyList<string> BodyNodeIds => EachNodeIds;

        /// <summary>
        /// EachNodeEditorModel を作成
        /// </summary>
        /// <param name="node">参照する EachNode</param>
        internal EachNodeEditorModel(EachNode node) : base(node) {
            _eachNode = node;
        }

        /// <summary>
        /// 各要素に対して実行するノード ID 一覧を設定
        /// </summary>
        /// <param name="eachNodeIds">設定するノード ID 一覧</param>
        internal void SetEachNodeIds(IReadOnlyList<string> eachNodeIds) {
            AnimationGraphAssetUtility.SetEachNodeIds(_eachNode, eachNodeIds);
        }

        /// <inheritdoc/>
        internal override void SetBodyNodeIds(IReadOnlyList<string> bodyNodeIds) {
            SetEachNodeIds(bodyNodeIds);
        }
    }
}
