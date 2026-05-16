using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Editor MVP の Model として Node の参照情報を提供するクラス
    /// </summary>
    public sealed class NodeEditorModel {
        private readonly Node _node;

        /// <summary>グラフ内で一意なノード ID</summary>
        public string NodeId => _node.NodeId;
        /// <summary>ノード型</summary>
        public Type NodeType => _node.GetType();
        /// <summary>ノード名</summary>
        public string Name => _node.name ?? string.Empty;
        /// <summary>エディタ上のノード位置</summary>
        public Vector2 GraphPosition => _node.GraphPosition;
        /// <summary>後続ノード ID の一覧</summary>
        public IReadOnlyList<string> NextNodeIds => _node.NextNodeIds;
        /// <summary>参照元の Node</summary>
        internal Node Node => _node;

        /// <summary>
        /// NodeEditorModel を作成
        /// </summary>
        /// <param name="node">参照する Node</param>
        internal NodeEditorModel(Node node) {
            _node = node ?? throw new ArgumentNullException(nameof(node));
        }

        /// <summary>
        /// エディタ上のノード位置を設定
        /// </summary>
        /// <param name="graphPosition">エディタ上のノード位置</param>
        public void SetGraphPosition(Vector2 graphPosition) {
            AnimationGraphAssetUtility.SetNodeGraphPosition(_node, graphPosition);
        }
    }
}
