using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Editor MVP の Model として AnimationGraphAsset の編集操作を提供するクラス
    /// </summary>
    public sealed class AnimationGraphAssetEditorModel {
        private readonly List<NodeEditorModel> _nodes = new();
        private readonly Dictionary<string, NodeEditorModel> _nodeModelsById = new();

        private AnimationGraphAsset _graphAsset;

        /// <summary>操作対象の AnimationGraphAsset</summary>
        public AnimationGraphAsset GraphAsset => _graphAsset;
        /// <summary>操作対象の AnimationGraphAsset が設定済みの場合は true</summary>
        public bool HasGraphAsset => _graphAsset != null;
        /// <summary>開始ノード ID</summary>
        public string StartNodeId => _graphAsset?.StartNodeId ?? string.Empty;
        /// <summary>GraphAsset に含まれるノード Model 一覧</summary>
        public IReadOnlyList<NodeEditorModel> Nodes => _nodes;
        /// <summary>GraphAsset が要求する target key 定義一覧</summary>
        public IReadOnlyList<AnimationGraphTargetDefinition> TargetDefinitions => _graphAsset?.TargetDefinitions ?? Array.Empty<AnimationGraphTargetDefinition>();
        /// <summary>GraphAsset が要求する Blackboard key 定義一覧</summary>
        public IReadOnlyList<AnimationGraphBlackboardDefinition> BlackboardDefinitions => _graphAsset?.BlackboardDefinitions ?? Array.Empty<AnimationGraphBlackboardDefinition>();

        /// <summary>
        /// 操作対象の AnimationGraphAsset を設定
        /// </summary>
        /// <param name="graphAsset">操作対象の AnimationGraphAsset</param>
        public void SetGraphAsset(AnimationGraphAsset graphAsset) {
            _graphAsset = graphAsset;
            RefreshNodes();
        }

        /// <summary>
        /// 操作対象の AnimationGraphAsset を初期状態に戻す
        /// </summary>
        /// <param name="startNodePosition">開始ノードのエディタ上の位置</param>
        /// <returns>初期化時に作成した開始ノード Model</returns>
        public NodeEditorModel InitializeGraph(Vector2 startNodePosition) {
            var node = AnimationGraphAssetUtility.InitializeGraph(RequireGraphAsset(), startNodePosition);
            RefreshNodes();
            return GetOrAddNodeModel(node);
        }

        /// <summary>
        /// 操作対象の AnimationGraphAsset にノードを追加
        /// </summary>
        /// <param name="graphPosition">エディタ上のノード位置</param>
        /// <typeparam name="T">追加するノード型</typeparam>
        /// <returns>追加したノード Model</returns>
        public NodeEditorModel AddNode<T>(Vector2 graphPosition) where T : Node {
            var node = AnimationGraphAssetUtility.AddNode<T>(RequireGraphAsset(), graphPosition);
            return GetOrAddNodeModel(node);
        }

        /// <summary>
        /// 操作対象の AnimationGraphAsset にノードを追加
        /// </summary>
        /// <param name="nodeType">追加するノード型</param>
        /// <param name="graphPosition">エディタ上のノード位置</param>
        /// <returns>追加したノード Model</returns>
        public NodeEditorModel AddNode(Type nodeType, Vector2 graphPosition) {
            var node = AnimationGraphAssetUtility.AddNode(RequireGraphAsset(), nodeType, graphPosition);
            return GetOrAddNodeModel(node);
        }

        /// <summary>
        /// 操作対象の AnimationGraphAsset からノードを削除
        /// </summary>
        /// <param name="nodeModel">削除するノード Model</param>
        public void RemoveNode(NodeEditorModel nodeModel) {
            if (nodeModel == null) {
                throw new ArgumentNullException(nameof(nodeModel));
            }

            var nodeId = nodeModel.NodeId;
            AnimationGraphAssetUtility.RemoveNode(RequireGraphAsset(), nodeModel.Node);
            RemoveNodeModel(nodeModel, nodeId);
        }

        /// <summary>
        /// GraphAsset の現在状態からノード Model 一覧を再構築
        /// </summary>
        public void RefreshNodes() {
            var previousNodeModelsById = new Dictionary<string, NodeEditorModel>(_nodeModelsById);
            _nodes.Clear();
            _nodeModelsById.Clear();
            if (_graphAsset == null) {
                return;
            }

            var nodes = _graphAsset.Nodes;
            for (var i = 0; i < nodes.Count; i++) {
                var node = nodes[i];
                if (node == null || _nodeModelsById.ContainsKey(node.NodeId)) {
                    continue;
                }

                if (!previousNodeModelsById.TryGetValue(node.NodeId, out var nodeModel) || nodeModel.Node != node) {
                    nodeModel = new NodeEditorModel(node);
                }

                _nodes.Add(nodeModel);
                _nodeModelsById.Add(node.NodeId, nodeModel);
            }
        }

        /// <summary>
        /// 操作対象の AnimationGraphAsset の target 定義を設定
        /// </summary>
        /// <param name="definitions">設定する target 定義一覧</param>
        public void SetTargetDefinitions(params AnimationGraphTargetDefinition[] definitions) {
            SetTargetDefinitions((IReadOnlyList<AnimationGraphTargetDefinition>)definitions);
        }

        /// <summary>
        /// 操作対象の AnimationGraphAsset の target 定義を設定
        /// </summary>
        /// <param name="definitions">設定する target 定義一覧</param>
        public void SetTargetDefinitions(IReadOnlyList<AnimationGraphTargetDefinition> definitions) {
            AnimationGraphAssetUtility.SetTargetDefinitions(RequireGraphAsset(), definitions);
        }

        /// <summary>
        /// 操作対象の AnimationGraphAsset の Blackboard 定義を設定
        /// </summary>
        /// <param name="definitions">設定する Blackboard 定義一覧</param>
        public void SetBlackboardDefinitions(params AnimationGraphBlackboardDefinition[] definitions) {
            SetBlackboardDefinitions((IReadOnlyList<AnimationGraphBlackboardDefinition>)definitions);
        }

        /// <summary>
        /// 操作対象の AnimationGraphAsset の Blackboard 定義を設定
        /// </summary>
        /// <param name="definitions">設定する Blackboard 定義一覧</param>
        public void SetBlackboardDefinitions(IReadOnlyList<AnimationGraphBlackboardDefinition> definitions) {
            AnimationGraphAssetUtility.SetBlackboardDefinitions(RequireGraphAsset(), definitions);
        }

        /// <summary>
        /// 指定した ID に対応するノードの取得を試行
        /// </summary>
        /// <param name="nodeId">取得するノード ID</param>
        /// <param name="nodeModel">取得したノード Model</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetNode(string nodeId, out NodeEditorModel nodeModel) {
            if (_graphAsset == null) {
                nodeModel = null;
                return false;
            }

            if (!_graphAsset.TryGetNode(nodeId, out var node)) {
                nodeModel = null;
                return false;
            }

            nodeModel = GetOrAddNodeModel(node);
            return true;
        }

        /// <summary>
        /// Node に対応するノード Model を取得または作成
        /// </summary>
        /// <param name="node">対象の Node</param>
        /// <returns>Node に対応するノード Model</returns>
        private NodeEditorModel GetOrAddNodeModel(Node node) {
            if (node == null) {
                throw new ArgumentNullException(nameof(node));
            }

            if (_nodeModelsById.TryGetValue(node.NodeId, out var nodeModel) && nodeModel.Node == node) {
                return nodeModel;
            }

            if (nodeModel != null) {
                RemoveNodeModel(nodeModel, node.NodeId);
            }

            nodeModel = new NodeEditorModel(node);
            _nodes.Add(nodeModel);
            _nodeModelsById.Add(node.NodeId, nodeModel);
            return nodeModel;
        }

        /// <summary>
        /// 指定した ID のノード Model を cache から削除
        /// </summary>
        /// <param name="nodeModel">削除するノード Model</param>
        /// <param name="nodeId">削除するノード ID</param>
        private void RemoveNodeModel(NodeEditorModel nodeModel, string nodeId) {
            _nodeModelsById.Remove(nodeId);

            for (var i = _nodes.Count - 1; i >= 0; i--) {
                if (!ReferenceEquals(_nodes[i], nodeModel) && _nodes[i].NodeId != nodeId) {
                    continue;
                }

                _nodes.RemoveAt(i);
                return;
            }
        }

        /// <summary>
        /// 操作対象の AnimationGraphAsset が設定済みであることを検証
        /// </summary>
        /// <returns>操作対象の AnimationGraphAsset</returns>
        private AnimationGraphAsset RequireGraphAsset() {
            if (_graphAsset == null) {
                throw new InvalidOperationException("AnimationGraphAsset is not set");
            }

            return _graphAsset;
        }
    }
}
