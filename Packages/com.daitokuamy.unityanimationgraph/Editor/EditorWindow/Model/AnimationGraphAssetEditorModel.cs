using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Editor MVP の Model として AnimationGraphAsset の編集操作を提供するクラス
    /// </summary>
    public sealed class AnimationGraphAssetEditorModel {
        private const float PreviewTimeEpsilon = 0.0001f;

        /// <summary>
        /// Preview 中の Node 実行情報を集計する構造体
        /// </summary>
        private struct PreviewExecutionInfoBuilder {
            private NodePreviewExecutionState _state;
            private float _progress;
            private float _previewStartTime;

            /// <summary>
            /// 現在時刻に対する ScheduledNode の実行状態を追加
            /// </summary>
            /// <param name="scheduledNode">追加する ScheduledNode</param>
            /// <param name="previewTime">Preview の現在時刻</param>
            public void Add(ScheduledNode scheduledNode, float previewTime) {
                var nextState = NodePreviewExecutionState.None;
                var nextProgress = 0.0f;
                var previewStartTime = scheduledNode.StartTime - scheduledNode.Delay;
                if (previewStartTime - PreviewTimeEpsilon <= previewTime && previewTime < scheduledNode.StartTime - PreviewTimeEpsilon) {
                    nextState = NodePreviewExecutionState.Active;
                }
                else if (scheduledNode.Duration <= PreviewTimeEpsilon) {
                    if (Mathf.Abs(previewTime - scheduledNode.StartTime) <= PreviewTimeEpsilon) {
                        nextState = NodePreviewExecutionState.Active;
                        nextProgress = 1.0f;
                    }
                    else if (scheduledNode.StartTime < previewTime - PreviewTimeEpsilon) {
                        nextState = NodePreviewExecutionState.Completed;
                        nextProgress = 1.0f;
                    }
                }
                else if (scheduledNode.StartTime - PreviewTimeEpsilon <= previewTime && previewTime < scheduledNode.EndTime - PreviewTimeEpsilon) {
                    nextState = NodePreviewExecutionState.Active;
                    nextProgress = Mathf.Clamp01((previewTime - scheduledNode.StartTime) / scheduledNode.Duration);
                }
                else if (scheduledNode.EndTime <= previewTime + PreviewTimeEpsilon) {
                    nextState = NodePreviewExecutionState.Completed;
                    nextProgress = 1.0f;
                }

                if (nextState == NodePreviewExecutionState.Active || _state != NodePreviewExecutionState.Active && nextState == NodePreviewExecutionState.Completed) {
                    _state = nextState;
                    _progress = nextProgress;
                    _previewStartTime = previewStartTime;
                }
            }

            /// <summary>
            /// 指定時刻より前の完了状態を消去
            /// </summary>
            /// <param name="resetTime">消去境界時刻</param>
            public void ClearCompletedBefore(float resetTime) {
                if (_state != NodePreviewExecutionState.Completed || resetTime <= _previewStartTime + PreviewTimeEpsilon) {
                    return;
                }

                _state = NodePreviewExecutionState.None;
                _progress = 0.0f;
                _previewStartTime = 0.0f;
            }

            /// <summary>
            /// 表示対象の実行状態を持つか判定
            /// </summary>
            /// <returns>表示対象の実行状態を持つ場合は true</returns>
            public bool HasState() {
                return _state != NodePreviewExecutionState.None;
            }

            /// <summary>
            /// NodePreviewExecutionInfo に変換
            /// </summary>
            /// <returns>集計した NodePreviewExecutionInfo</returns>
            public NodePreviewExecutionInfo ToPreviewExecutionInfo() {
                return new NodePreviewExecutionInfo(_state, _progress);
            }
        }

        private readonly List<NodeEditorModel> _nodes = new();
        private readonly Dictionary<string, NodeEditorModel> _nodeModelsById = new();
        private readonly List<SignalEditorModel> _signals = new();
        private readonly Dictionary<string, SignalEditorModel> _signalModelsById = new();

        private AnimationGraphAsset _graphAsset;

        /// <summary>操作対象の AnimationGraphAsset</summary>
        public AnimationGraphAsset GraphAsset => _graphAsset;
        /// <summary>操作対象の AnimationGraphAsset が設定済みの場合は true</summary>
        public bool HasGraphAsset => _graphAsset != null;
        /// <summary>操作対象の AnimationGraphAsset に有効な StartNode がある場合は true</summary>
        public bool HasStartNode => _graphAsset != null && !string.IsNullOrEmpty(_graphAsset.StartNodeId) && _graphAsset.TryGetNode(_graphAsset.StartNodeId, out var node) && node is StartNode;
        /// <summary>開始ノード ID</summary>
        public string StartNodeId => _graphAsset?.StartNodeId ?? string.Empty;
        /// <summary>GraphAsset に含まれるノード Model 一覧</summary>
        public IReadOnlyList<NodeEditorModel> Nodes => _nodes;
        /// <summary>GraphAsset に含まれる Signal Model 一覧</summary>
        public IReadOnlyList<SignalEditorModel> Signals => _signals;
        /// <summary>GraphAsset が要求する target key 定義一覧</summary>
        public IReadOnlyList<TargetDefinition> TargetDefinitions => _graphAsset?.TargetDefinitions ?? Array.Empty<TargetDefinition>();
        /// <summary>GraphAsset が要求する Blackboard key 定義一覧</summary>
        public IReadOnlyList<BlackboardDefinition> BlackboardDefinitions => _graphAsset?.BlackboardDefinitions ?? Array.Empty<BlackboardDefinition>();

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
        /// 操作対象の AnimationGraphAsset から複数ノードを削除
        /// </summary>
        /// <param name="nodeModels">削除するノード Model 一覧</param>
        public void RemoveNodes(IReadOnlyList<NodeEditorModel> nodeModels) {
            if (nodeModels == null) {
                throw new ArgumentNullException(nameof(nodeModels));
            }

            for (var i = nodeModels.Count - 1; i >= 0; i--) {
                var nodeModel = nodeModels[i];
                if (nodeModel == null || !CanRemoveNode(nodeModel)) {
                    continue;
                }

                RemoveNode(nodeModel);
            }
        }

        /// <summary>
        /// 操作対象の AnimationGraphAsset に複数ノードを複製して追加
        /// </summary>
        /// <param name="nodeModels">複製するノード Model 一覧</param>
        /// <param name="offset">複製先座標に加算する offset</param>
        /// <returns>複製したノード Model 一覧</returns>
        public IReadOnlyList<NodeEditorModel> DuplicateNodes(IReadOnlyList<NodeEditorModel> nodeModels, Vector2 offset) {
            if (nodeModels == null) {
                throw new ArgumentNullException(nameof(nodeModels));
            }

            var sourceNodeModels = new List<NodeEditorModel>();
            var duplicatedNodeModels = new List<NodeEditorModel>();
            var duplicatedNodeModelsBySourceId = new Dictionary<string, NodeEditorModel>();
            for (var i = 0; i < nodeModels.Count; i++) {
                var sourceNodeModel = nodeModels[i];
                if (sourceNodeModel == null || !CanDuplicateNode(sourceNodeModel) || duplicatedNodeModelsBySourceId.ContainsKey(sourceNodeModel.NodeId)) {
                    continue;
                }

                var duplicatedNode = AnimationGraphAssetUtility.DuplicateNode(RequireGraphAsset(), sourceNodeModel.Node, sourceNodeModel.GraphPosition + offset);
                var duplicatedNodeModel = GetOrAddNodeModel(duplicatedNode);
                sourceNodeModels.Add(sourceNodeModel);
                duplicatedNodeModels.Add(duplicatedNodeModel);
                duplicatedNodeModelsBySourceId.Add(sourceNodeModel.NodeId, duplicatedNodeModel);
            }

            for (var i = 0; i < sourceNodeModels.Count; i++) {
                var sourceNodeModel = sourceNodeModels[i];
                var duplicatedNextNodeIds = new List<string>();
                var nextNodeIds = sourceNodeModel.NextNodeIds;
                for (var j = 0; j < nextNodeIds.Count; j++) {
                    if (!duplicatedNodeModelsBySourceId.TryGetValue(nextNodeIds[j], out var duplicatedNextNodeModel)) {
                        continue;
                    }

                    duplicatedNextNodeIds.Add(duplicatedNextNodeModel.NodeId);
                }

                duplicatedNodeModels[i].SetNextNodeIds(duplicatedNextNodeIds);

                if (sourceNodeModel is BranchNodeEditorModel sourceBranchNodeModel && duplicatedNodeModels[i] is BranchNodeEditorModel duplicatedBranchNodeModel) {
                    for (var j = 0; j < sourceBranchNodeModel.ExtensionPortCount; j++) {
                        var duplicatedExtensionNodeIds = new List<string>();
                        var extensionNodeIds = sourceBranchNodeModel.GetExtensionNodeIds(j);
                        for (var k = 0; k < extensionNodeIds.Count; k++) {
                            if (!duplicatedNodeModelsBySourceId.TryGetValue(extensionNodeIds[k], out var duplicatedExtensionNodeModel)) {
                                continue;
                            }

                            duplicatedExtensionNodeIds.Add(duplicatedExtensionNodeModel.NodeId);
                        }

                        duplicatedBranchNodeModel.SetExtensionNodeIds(j, duplicatedExtensionNodeIds);
                    }
                }

                if (sourceNodeModel is ScopedControlNodeEditorModel sourceLoopNodeModel && duplicatedNodeModels[i] is ScopedControlNodeEditorModel duplicatedLoopNodeModel) {
                    var duplicatedLoopNodeIds = new List<string>();
                    var loopNodeIds = sourceLoopNodeModel.BodyNodeIds;
                    for (var j = 0; j < loopNodeIds.Count; j++) {
                        if (!duplicatedNodeModelsBySourceId.TryGetValue(loopNodeIds[j], out var duplicatedLoopTargetNodeModel)) {
                            continue;
                        }

                        duplicatedLoopNodeIds.Add(duplicatedLoopTargetNodeModel.NodeId);
                    }

                    duplicatedLoopNodeModel.SetBodyNodeIds(duplicatedLoopNodeIds);
                }
            }

            return duplicatedNodeModels;
        }

        /// <summary>
        /// 操作対象の AnimationGraphAsset に複数 Signal を複製して追加
        /// </summary>
        /// <param name="signalModels">複製する Signal Model 一覧</param>
        /// <param name="offset">複製先座標に加算する offset</param>
        /// <returns>複製した Signal Model 一覧</returns>
        public IReadOnlyList<SignalEditorModel> DuplicateSignals(IReadOnlyList<SignalEditorModel> signalModels, Vector2 offset) {
            if (signalModels == null) {
                throw new ArgumentNullException(nameof(signalModels));
            }

            var duplicatedSignalModels = new List<SignalEditorModel>();
            var duplicatedSignalIds = new HashSet<string>();
            for (var i = 0; i < signalModels.Count; i++) {
                var sourceSignalModel = signalModels[i];
                if (sourceSignalModel == null || !CanDuplicateSignal(sourceSignalModel) || !duplicatedSignalIds.Add(sourceSignalModel.SignalId)) {
                    continue;
                }

                var duplicatedSignal = AnimationGraphAssetUtility.DuplicateSignal(RequireGraphAsset(), sourceSignalModel.Signal, sourceSignalModel.GraphPosition + offset);
                var duplicatedSignalModel = GetOrAddSignalModel(duplicatedSignal);
                duplicatedSignalModels.Add(duplicatedSignalModel);
            }

            return duplicatedSignalModels;
        }

        /// <summary>
        /// GraphAsset の現在状態からノード Model 一覧を再構築
        /// </summary>
        public void RefreshNodes() {
            var previousNodeModelsById = new Dictionary<string, NodeEditorModel>(_nodeModelsById);
            _nodes.Clear();
            _nodeModelsById.Clear();

            if (_graphAsset != null) {
                var nodes = _graphAsset.Nodes;
                for (var i = 0; i < nodes.Count; i++) {
                    var node = nodes[i];
                    if (node == null || _nodeModelsById.ContainsKey(node.NodeId)) {
                        continue;
                    }

                    if (!previousNodeModelsById.TryGetValue(node.NodeId, out var nodeModel) || nodeModel.Node != node) {
                        nodeModel = NodeEditorModel.Create(node);
                    }

                    _nodes.Add(nodeModel);
                    _nodeModelsById.Add(node.NodeId, nodeModel);
                }
            }

            RefreshSignals();
        }

        /// <summary>
        /// Preview 中の schedule から Node 実行情報を反映
        /// </summary>
        /// <param name="previewSchedule">Preview 中の schedule。null の場合は Preview 情報を消去</param>
        /// <param name="previewTime">Preview の現在時刻</param>
        /// <returns>Node 表示情報が変化した場合は true</returns>
        public bool SetPreviewSchedule(AnimationGraphSchedule previewSchedule, float previewTime) {
            var previewInfoBuildersByNodeId = new Dictionary<string, PreviewExecutionInfoBuilder>();
            if (previewSchedule != null) {
                var scheduledNodes = previewSchedule.Nodes;
                for (var i = 0; i < scheduledNodes.Count; i++) {
                    var scheduledNode = scheduledNodes[i];
                    var node = scheduledNode.Node;
                    if (node == null || string.IsNullOrEmpty(node.NodeId)) {
                        continue;
                    }

                    if (!previewInfoBuildersByNodeId.TryGetValue(node.NodeId, out var builder)) {
                        builder = default;
                    }

                    builder.Add(scheduledNode, previewTime);
                    previewInfoBuildersByNodeId[node.NodeId] = builder;
                }

                ApplyLoopPreviewResets(previewInfoBuildersByNodeId, scheduledNodes, previewTime);
            }

            var changed = false;
            for (var i = 0; i < _nodes.Count; i++) {
                var nodeModel = _nodes[i];
                if (previewInfoBuildersByNodeId.TryGetValue(nodeModel.NodeId, out var builder) && builder.HasState()) {
                    changed |= nodeModel.SetPreviewExecutionInfo(builder.ToPreviewExecutionInfo());
                    continue;
                }

                if (previewSchedule != null) {
                    changed |= nodeModel.SetPreviewExecutionInfo(new NodePreviewExecutionInfo(NodePreviewExecutionState.None, 0.0f));
                    continue;
                }

                changed |= nodeModel.ClearPreviewExecutionInfo();
            }

            return changed;
        }

        private void ApplyLoopPreviewResets(Dictionary<string, PreviewExecutionInfoBuilder> previewInfoBuildersByNodeId, IReadOnlyList<ScheduledNode> scheduledNodes, float previewTime) {
            var resetTimesByNodeId = new Dictionary<string, float>(StringComparer.Ordinal);
            for (var i = 0; i < _nodes.Count; i++) {
                if (_nodes[i] is not ScopedControlNodeEditorModel loopNodeModel) {
                    continue;
                }

                var loopBodyNodeIds = BuildLoopBodyNodeIdSet(loopNodeModel, null);
                if (loopBodyNodeIds.Count == 0 || !TryGetLatestLoopResetTime(loopNodeModel, loopBodyNodeIds, scheduledNodes, previewTime, out var resetTime)) {
                    continue;
                }

                foreach (var loopBodyNodeId in loopBodyNodeIds) {
                    if (!resetTimesByNodeId.TryGetValue(loopBodyNodeId, out var currentResetTime) || currentResetTime < resetTime - PreviewTimeEpsilon) {
                        resetTimesByNodeId[loopBodyNodeId] = resetTime;
                    }
                }
            }

            foreach (var resetTimePair in resetTimesByNodeId) {
                if (!previewInfoBuildersByNodeId.TryGetValue(resetTimePair.Key, out var builder)) {
                    continue;
                }

                builder.ClearCompletedBefore(resetTimePair.Value);
                previewInfoBuildersByNodeId[resetTimePair.Key] = builder;
            }
        }

        private bool TryGetLatestLoopResetTime(
            ScopedControlNodeEditorModel loopNodeModel,
            HashSet<string> loopBodyNodeIds,
            IReadOnlyList<ScheduledNode> scheduledNodes,
            float previewTime,
            out float latestResetTime) {
            latestResetTime = 0.0f;
            var loopStartNodeIds = GetLoopStartNodeIds(loopNodeModel, loopBodyNodeIds);
            if (loopStartNodeIds.Count == 0) {
                return false;
            }

            var loopStartNodeIdSet = new HashSet<string>(loopStartNodeIds, StringComparer.Ordinal);
            var found = false;
            for (var i = 0; i < scheduledNodes.Count; i++) {
                var node = scheduledNodes[i].Node;
                if (node == null || !loopStartNodeIdSet.Contains(node.NodeId)) {
                    continue;
                }

                var resetTime = scheduledNodes[i].StartTime - scheduledNodes[i].Delay;
                if (previewTime + PreviewTimeEpsilon < resetTime) {
                    continue;
                }

                if (!found || latestResetTime < resetTime - PreviewTimeEpsilon) {
                    latestResetTime = resetTime;
                    found = true;
                }
            }

            return found;
        }

        private IReadOnlyList<string> GetLoopStartNodeIds(ScopedControlNodeEditorModel loopNodeModel, HashSet<string> loopBodyNodeIds) {
            var incomingNodeIds = new HashSet<string>(loopBodyNodeIds.Count, StringComparer.Ordinal);
            foreach (var loopBodyNodeId in loopBodyNodeIds) {
                if (!_nodeModelsById.TryGetValue(loopBodyNodeId, out var bodyNodeModel)) {
                    continue;
                }

                var connectedNodeIds = GetPhysicalConnectedNodeIds(bodyNodeModel);
                for (var i = 0; i < connectedNodeIds.Count; i++) {
                    if (loopBodyNodeIds.Contains(connectedNodeIds[i])) {
                        incomingNodeIds.Add(connectedNodeIds[i]);
                    }
                }
            }

            var startNodeIds = new List<string>();
            var addedStartNodeIds = new HashSet<string>(StringComparer.Ordinal);
            var loopNodeIds = loopNodeModel.BodyNodeIds;
            for (var i = 0; i < loopNodeIds.Count; i++) {
                if (!loopBodyNodeIds.Contains(loopNodeIds[i]) || incomingNodeIds.Contains(loopNodeIds[i])) {
                    continue;
                }

                startNodeIds.Add(loopNodeIds[i]);
                addedStartNodeIds.Add(loopNodeIds[i]);
            }

            foreach (var loopBodyNodeId in loopBodyNodeIds) {
                if (incomingNodeIds.Contains(loopBodyNodeId) || addedStartNodeIds.Contains(loopBodyNodeId)) {
                    continue;
                }

                startNodeIds.Add(loopBodyNodeId);
            }

            return startNodeIds;
        }

        /// <summary>
        /// ノードごとの検証エラーメッセージを取得
        /// </summary>
        /// <returns>検証エラーがあるノード ID とメッセージの対応</returns>
        public IReadOnlyDictionary<string, string> GetNodeValidationMessages() {
            var messagesByNodeId = new Dictionary<string, string>(StringComparer.Ordinal);
            if (_graphAsset == null) {
                return messagesByNodeId;
            }

            for (var i = 0; i < _nodes.Count; i++) {
                if (_nodes[i] is not ScopedControlNodeEditorModel loopNodeModel) {
                    continue;
                }

                AddLoopValidationMessages(loopNodeModel, messagesByNodeId);
            }

            AddNodeValidationMessages(messagesByNodeId);

            return messagesByNodeId;
        }

        /// <summary>
        /// 操作対象の AnimationGraphAsset の target 定義を設定
        /// </summary>
        /// <param name="definitions">設定する target 定義一覧</param>
        public void SetTargetDefinitions(params TargetDefinition[] definitions) {
            SetTargetDefinitions((IReadOnlyList<TargetDefinition>)definitions);
        }

        /// <summary>
        /// 操作対象の AnimationGraphAsset の target 定義を設定
        /// </summary>
        /// <param name="definitions">設定する target 定義一覧</param>
        public void SetTargetDefinitions(IReadOnlyList<TargetDefinition> definitions) {
            AnimationGraphAssetUtility.SetTargetDefinitions(RequireGraphAsset(), definitions);
        }

        /// <summary>
        /// 操作対象の AnimationGraphAsset の Blackboard 定義を設定
        /// </summary>
        /// <param name="definitions">設定する Blackboard 定義一覧</param>
        public void SetBlackboardDefinitions(params BlackboardDefinition[] definitions) {
            SetBlackboardDefinitions((IReadOnlyList<BlackboardDefinition>)definitions);
        }

        /// <summary>
        /// 操作対象の AnimationGraphAsset の Blackboard 定義を設定
        /// </summary>
        /// <param name="definitions">設定する Blackboard 定義一覧</param>
        public void SetBlackboardDefinitions(IReadOnlyList<BlackboardDefinition> definitions) {
            AnimationGraphAssetUtility.SetBlackboardDefinitions(RequireGraphAsset(), definitions);
        }

        /// <summary>
        /// 操作対象の AnimationGraphAsset に Signal を追加
        /// </summary>
        /// <param name="signalType">追加する Signal 型</param>
        /// <param name="graphPosition">エディタ上の Signal 位置</param>
        /// <returns>追加した Signal Model</returns>
        internal SignalEditorModel AddSignal(Type signalType, Vector2 graphPosition) {
            var signal = AnimationGraphAssetUtility.AddSignal(RequireGraphAsset(), signalType, graphPosition);
            return GetOrAddSignalModel(signal);
        }

        /// <summary>
        /// 指定した Node の Signal Port に Signal を追加
        /// </summary>
        /// <param name="outputPortKind">追加先 Signal Port 種別</param>
        /// <param name="nodeModel">追加先ノード Model</param>
        /// <param name="signalType">追加する Signal 型</param>
        /// <returns>追加した Signal</returns>
        internal Signal AddSignal(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel nodeModel, Type signalType) {
            return AddSignal(outputPortKind, nodeModel, signalType, Vector2.zero);
        }

        /// <summary>
        /// 指定した Node の Signal Port に Signal を追加
        /// </summary>
        /// <param name="outputPortKind">追加先 Signal Port 種別</param>
        /// <param name="nodeModel">追加先ノード Model</param>
        /// <param name="signalType">追加する Signal 型</param>
        /// <param name="graphPosition">エディタ上の Signal 位置</param>
        /// <returns>追加した Signal</returns>
        internal Signal AddSignal(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel nodeModel, Type signalType, Vector2 graphPosition) {
            if (nodeModel == null) {
                throw new ArgumentNullException(nameof(nodeModel));
            }

            if (!ContainsNode(nodeModel)) {
                throw new InvalidOperationException("Node is not contained in AnimationGraphAsset");
            }

            Signal signal;
            if (outputPortKind == AnimationGraphOutputPortKind.EnterSignal && nodeModel.EnableEnterSignalPort) {
                signal = AnimationGraphAssetUtility.AddEnterSignal(RequireGraphAsset(), nodeModel.Node, signalType, graphPosition);
            }
            else if (outputPortKind == AnimationGraphOutputPortKind.ExitSignal && nodeModel.EnableExitSignalPort) {
                signal = AnimationGraphAssetUtility.AddExitSignal(RequireGraphAsset(), nodeModel.Node, signalType, graphPosition);
            }
            else if (outputPortKind.IsSignal) {
                throw new InvalidOperationException("Signal port is not enabled for this node");
            }
            else {
                throw new InvalidOperationException("Output port is not a Signal port");
            }

            GetOrAddSignalModel(signal);
            return signal;
        }

        /// <summary>
        /// 指定した output port から Signal へ接続
        /// </summary>
        /// <param name="outputPortKind">接続元 output port 種別</param>
        /// <param name="sourceNodeModel">接続元ノード Model</param>
        /// <param name="targetSignalModel">接続先 Signal Model</param>
        /// <param name="errorMessage">接続できない理由</param>
        /// <returns>接続できた場合は true</returns>
        internal bool ConnectSignal(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, SignalEditorModel targetSignalModel, out string errorMessage) {
            if (!CanConnectSignal(outputPortKind, sourceNodeModel, targetSignalModel, out errorMessage)) {
                return false;
            }

            if (outputPortKind == AnimationGraphOutputPortKind.EnterSignal) {
                AnimationGraphAssetUtility.AddEnterSignalReference(sourceNodeModel.Node, targetSignalModel.Signal);
            }
            else if (outputPortKind == AnimationGraphOutputPortKind.ExitSignal) {
                AnimationGraphAssetUtility.AddExitSignalReference(sourceNodeModel.Node, targetSignalModel.Signal);
            }

            RefreshSignals();
            return true;
        }

        /// <summary>
        /// 指定した output port から Signal への接続を解除
        /// </summary>
        /// <param name="outputPortKind">接続元 output port 種別</param>
        /// <param name="sourceNodeModel">接続元ノード Model</param>
        /// <param name="targetSignalModel">接続先 Signal Model</param>
        /// <returns>接続解除できた場合は true</returns>
        internal bool DisconnectSignal(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, SignalEditorModel targetSignalModel) {
            if (sourceNodeModel == null) {
                throw new ArgumentNullException(nameof(sourceNodeModel));
            }

            if (targetSignalModel == null) {
                throw new ArgumentNullException(nameof(targetSignalModel));
            }

            if (!outputPortKind.IsSignal || !ContainsSignalReference(sourceNodeModel, outputPortKind, targetSignalModel.Signal)) {
                return false;
            }

            if (outputPortKind == AnimationGraphOutputPortKind.EnterSignal) {
                AnimationGraphAssetUtility.RemoveEnterSignalReference(sourceNodeModel.Node, targetSignalModel.Signal);
            }
            else if (outputPortKind == AnimationGraphOutputPortKind.ExitSignal) {
                AnimationGraphAssetUtility.RemoveExitSignalReference(sourceNodeModel.Node, targetSignalModel.Signal);
            }

            RefreshSignals();
            return true;
        }

        /// <summary>
        /// 指定した output port から Signal へ接続できるかを判定
        /// </summary>
        /// <param name="outputPortKind">接続元 output port 種別</param>
        /// <param name="sourceNodeModel">接続元ノード Model</param>
        /// <param name="targetSignalModel">接続先 Signal Model</param>
        /// <param name="errorMessage">接続できない理由</param>
        /// <returns>接続できる場合は true</returns>
        internal bool CanConnectSignal(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, SignalEditorModel targetSignalModel, out string errorMessage) {
            if (sourceNodeModel == null) {
                throw new ArgumentNullException(nameof(sourceNodeModel));
            }

            if (targetSignalModel == null) {
                throw new ArgumentNullException(nameof(targetSignalModel));
            }

            if (!ContainsNode(sourceNodeModel) || !ContainsSignal(targetSignalModel)) {
                errorMessage = "Element is not contained in AnimationGraphAsset";
                return false;
            }

            if (!outputPortKind.IsSignal) {
                errorMessage = "Output port is not a Signal port";
                return false;
            }

            if (!CanUseSignalOutputPort(sourceNodeModel, outputPortKind)) {
                errorMessage = "Signal port is not enabled for this node";
                return false;
            }

            if (IsSignalAttached(targetSignalModel.Signal)) {
                errorMessage = "Signal already has an input connection";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// 操作対象の AnimationGraphAsset から Signal を削除
        /// </summary>
        /// <param name="signalModel">削除する Signal Model</param>
        internal void RemoveSignal(SignalEditorModel signalModel) {
            if (signalModel == null) {
                throw new ArgumentNullException(nameof(signalModel));
            }

            var signalId = signalModel.SignalId;
            AnimationGraphAssetUtility.RemoveSignal(RequireGraphAsset(), signalModel.Signal);
            RemoveSignalModel(signalModel, signalId);
        }

        /// <summary>
        /// 操作対象の AnimationGraphAsset から複数 Signal を削除
        /// </summary>
        /// <param name="signalModels">削除する Signal Model 一覧</param>
        internal void RemoveSignals(IReadOnlyList<SignalEditorModel> signalModels) {
            if (signalModels == null) {
                throw new ArgumentNullException(nameof(signalModels));
            }

            for (var i = signalModels.Count - 1; i >= 0; i--) {
                var signalModel = signalModels[i];
                if (signalModel == null || !ContainsSignal(signalModel)) {
                    continue;
                }

                RemoveSignal(signalModel);
            }
        }

        /// <summary>
        /// 指定したノード同士を後続ノードとして接続
        /// </summary>
        /// <param name="sourceNodeModel">接続元ノード Model</param>
        /// <param name="targetNodeModel">接続先ノード Model</param>
        /// <param name="errorMessage">接続できない理由</param>
        /// <returns>接続できた場合は true</returns>
        public bool Connect(NodeEditorModel sourceNodeModel, NodeEditorModel targetNodeModel, out string errorMessage) {
            return Connect(AnimationGraphOutputPortKind.Next, sourceNodeModel, targetNodeModel, out errorMessage);
        }

        /// <summary>
        /// 指定した output port から後続ノードへ接続
        /// </summary>
        /// <param name="outputPortKind">接続元 output port 種別</param>
        /// <param name="sourceNodeModel">接続元ノード Model</param>
        /// <param name="targetNodeModel">接続先ノード Model</param>
        /// <param name="errorMessage">接続できない理由</param>
        /// <returns>接続できた場合は true</returns>
        internal bool Connect(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, NodeEditorModel targetNodeModel, out string errorMessage) {
            if (!CanConnect(outputPortKind, sourceNodeModel, targetNodeModel, out errorMessage)) {
                return false;
            }

            var nodeIds = new List<string>(GetConnectedNodeIds(sourceNodeModel, outputPortKind)) {
                targetNodeModel.NodeId,
            };
            SetConnectedNodeIds(sourceNodeModel, outputPortKind, nodeIds);
            return true;
        }

        /// <summary>
        /// 指定したノード同士の後続ノード接続を解除
        /// </summary>
        /// <param name="sourceNodeModel">接続元ノード Model</param>
        /// <param name="targetNodeModel">接続先ノード Model</param>
        /// <returns>接続解除できた場合は true</returns>
        public bool Disconnect(NodeEditorModel sourceNodeModel, NodeEditorModel targetNodeModel) {
            return Disconnect(AnimationGraphOutputPortKind.Next, sourceNodeModel, targetNodeModel);
        }

        /// <summary>
        /// 指定した output port の接続を解除
        /// </summary>
        /// <param name="outputPortKind">接続元 output port 種別</param>
        /// <param name="sourceNodeModel">接続元ノード Model</param>
        /// <param name="targetNodeModel">接続先ノード Model</param>
        /// <returns>接続解除できた場合は true</returns>
        internal bool Disconnect(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, NodeEditorModel targetNodeModel) {
            if (sourceNodeModel == null) {
                throw new ArgumentNullException(nameof(sourceNodeModel));
            }

            if (targetNodeModel == null) {
                throw new ArgumentNullException(nameof(targetNodeModel));
            }

            var nodeIds = GetConnectedNodeIds(sourceNodeModel, outputPortKind);
            var nodeIdsWithoutTarget = new List<string>();
            var removed = false;
            for (var i = 0; i < nodeIds.Count; i++) {
                if (nodeIds[i] == targetNodeModel.NodeId) {
                    removed = true;
                    continue;
                }

                nodeIdsWithoutTarget.Add(nodeIds[i]);
            }

            if (!removed) {
                return false;
            }

            SetConnectedNodeIds(sourceNodeModel, outputPortKind, nodeIdsWithoutTarget);
            return true;
        }

        /// <summary>
        /// 指定したノード同士を接続できるかを判定
        /// </summary>
        /// <param name="sourceNodeModel">接続元ノード Model</param>
        /// <param name="targetNodeModel">接続先ノード Model</param>
        /// <param name="errorMessage">接続できない理由</param>
        /// <returns>接続できる場合は true</returns>
        public bool CanConnect(NodeEditorModel sourceNodeModel, NodeEditorModel targetNodeModel, out string errorMessage) {
            return CanConnect(AnimationGraphOutputPortKind.Next, sourceNodeModel, targetNodeModel, out errorMessage);
        }

        /// <summary>
        /// 指定した output port から接続できるかを判定
        /// </summary>
        /// <param name="outputPortKind">接続元 output port 種別</param>
        /// <param name="sourceNodeModel">接続元ノード Model</param>
        /// <param name="targetNodeModel">接続先ノード Model</param>
        /// <param name="errorMessage">接続できない理由</param>
        /// <returns>接続できる場合は true</returns>
        internal bool CanConnect(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, NodeEditorModel targetNodeModel, out string errorMessage) {
            if (sourceNodeModel == null) {
                throw new ArgumentNullException(nameof(sourceNodeModel));
            }

            if (targetNodeModel == null) {
                throw new ArgumentNullException(nameof(targetNodeModel));
            }

            if (!ContainsNode(sourceNodeModel) || !ContainsNode(targetNodeModel)) {
                errorMessage = "Node is not contained in AnimationGraphAsset";
                return false;
            }

            if (outputPortKind.IsSignal) {
                errorMessage = "Signal port cannot connect to Node";
                return false;
            }

            if (sourceNodeModel.NodeId == targetNodeModel.NodeId) {
                errorMessage = "Self-loop connection is not allowed";
                return false;
            }

            if (targetNodeModel.NodeType == typeof(StartNode)) {
                if (HasPath(targetNodeModel.NodeId, sourceNodeModel.NodeId)) {
                    errorMessage = "Cycle connection is not allowed";
                    return false;
                }

                errorMessage = "StartNode cannot receive input connections";
                return false;
            }

            if (!CanUseOutputPort(sourceNodeModel, outputPortKind)) {
                errorMessage = "Output port is not available for this node";
                return false;
            }

            var nodeIds = GetConnectedNodeIds(sourceNodeModel, outputPortKind);
            for (var i = 0; i < nodeIds.Count; i++) {
                if (nodeIds[i] != targetNodeModel.NodeId) {
                    continue;
                }

                errorMessage = "Duplicate connection is not allowed";
                return false;
            }

            if (ShouldEnforceSingleInput(outputPortKind, sourceNodeModel, targetNodeModel) && !CanUseMultipleInputs(targetNodeModel) && HasInputConnection(targetNodeModel)) {
                errorMessage = "Only JoinNode can receive multiple input connections";
                return false;
            }

            if (!CanConnectLoopScope(outputPortKind, sourceNodeModel, targetNodeModel, out errorMessage)) {
                return false;
            }

            if (HasPath(targetNodeModel.NodeId, sourceNodeModel.NodeId)) {
                errorMessage = "Cycle connection is not allowed";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// 指定したノードを削除できるかを判定
        /// </summary>
        /// <param name="nodeModel">判定するノード Model</param>
        /// <returns>削除できる場合は true</returns>
        public bool CanRemoveNode(NodeEditorModel nodeModel) {
            return nodeModel != null && nodeModel.NodeId != StartNodeId && ContainsNode(nodeModel);
        }

        /// <summary>
        /// 指定したノードを複製できるかを判定
        /// </summary>
        /// <param name="nodeModel">判定するノード Model</param>
        /// <returns>複製できる場合は true</returns>
        public bool CanDuplicateNode(NodeEditorModel nodeModel) {
            return CanRemoveNode(nodeModel);
        }

        /// <summary>
        /// 指定した Signal を複製できるかを判定
        /// </summary>
        /// <param name="signalModel">判定する Signal Model</param>
        /// <returns>複製できる場合は true</returns>
        public bool CanDuplicateSignal(SignalEditorModel signalModel) {
            return ContainsSignal(signalModel);
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

            nodeModel = NodeEditorModel.Create(node);
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
        /// GraphAsset の現在状態から Signal Model 一覧を再構築
        /// </summary>
        private void RefreshSignals() {
            var previousSignalModelsById = new Dictionary<string, SignalEditorModel>(_signalModelsById);
            _signals.Clear();
            _signalModelsById.Clear();
            if (_graphAsset == null) {
                return;
            }

            AddSignalModels(AnimationGraphAssetUtility.GetSignals(_graphAsset), previousSignalModelsById);
            for (var i = 0; i < _nodes.Count; i++) {
                AddSignalModels(_nodes[i].EnterSignals, previousSignalModelsById);
                AddSignalModels(_nodes[i].ExitSignals, previousSignalModelsById);
            }
        }

        private void AddSignalModels(IReadOnlyList<Signal> signals, Dictionary<string, SignalEditorModel> previousSignalModelsById) {
            for (var i = 0; i < signals.Count; i++) {
                var signal = signals[i];
                if (signal == null || string.IsNullOrEmpty(signal.SignalId) || _signalModelsById.ContainsKey(signal.SignalId)) {
                    continue;
                }

                if (!previousSignalModelsById.TryGetValue(signal.SignalId, out var signalModel) || signalModel.Signal != signal) {
                    signalModel = new SignalEditorModel(signal);
                }

                _signals.Add(signalModel);
                _signalModelsById.Add(signal.SignalId, signalModel);
            }
        }

        private SignalEditorModel GetOrAddSignalModel(Signal signal) {
            if (signal == null) {
                throw new ArgumentNullException(nameof(signal));
            }

            if (_signalModelsById.TryGetValue(signal.SignalId, out var signalModel) && signalModel.Signal == signal) {
                return signalModel;
            }

            if (signalModel != null) {
                RemoveSignalModel(signalModel, signal.SignalId);
            }

            signalModel = new SignalEditorModel(signal);
            _signals.Add(signalModel);
            _signalModelsById.Add(signal.SignalId, signalModel);
            return signalModel;
        }

        private void RemoveSignalModel(SignalEditorModel signalModel, string signalId) {
            _signalModelsById.Remove(signalId);

            for (var i = _signals.Count - 1; i >= 0; i--) {
                if (!ReferenceEquals(_signals[i], signalModel) && _signals[i].SignalId != signalId) {
                    continue;
                }

                _signals.RemoveAt(i);
                return;
            }
        }

        /// <summary>
        /// 指定したノード Model が GraphAsset に含まれるかを判定
        /// </summary>
        /// <param name="nodeModel">判定するノード Model</param>
        /// <returns>GraphAsset に含まれる場合は true</returns>
        private bool ContainsNode(NodeEditorModel nodeModel) {
            return nodeModel != null && _graphAsset != null && _graphAsset.TryGetNode(nodeModel.NodeId, out var node) && node == nodeModel.Node;
        }

        private bool ContainsSignal(SignalEditorModel signalModel) {
            return signalModel != null
                && _graphAsset != null
                && _signalModelsById.TryGetValue(signalModel.SignalId, out var currentSignalModel)
                && currentSignalModel.Signal == signalModel.Signal;
        }

        /// <summary>
        /// 指定した output port を利用できるかを判定
        /// </summary>
        /// <param name="nodeModel">判定するノード Model</param>
        /// <param name="outputPortKind">判定する output port 種別</param>
        /// <returns>利用できる場合は true</returns>
        private static bool CanUseOutputPort(NodeEditorModel nodeModel, AnimationGraphOutputPortKind outputPortKind) {
            if (outputPortKind.IsNext) {
                return true;
            }

            if (outputPortKind.IsBranchExtension && nodeModel is BranchNodeEditorModel branchNodeModel) {
                return outputPortKind.BranchExtensionIndex < branchNodeModel.ExtensionPortCount;
            }

            return outputPortKind == AnimationGraphOutputPortKind.Loop && nodeModel is ScopedControlNodeEditorModel;
        }

        private static bool CanUseSignalOutputPort(NodeEditorModel nodeModel, AnimationGraphOutputPortKind outputPortKind) {
            if (outputPortKind == AnimationGraphOutputPortKind.EnterSignal) {
                return nodeModel.EnableEnterSignalPort;
            }

            return outputPortKind == AnimationGraphOutputPortKind.ExitSignal && nodeModel.EnableExitSignalPort;
        }

        private bool IsSignalAttached(Signal signal) {
            for (var i = 0; i < _nodes.Count; i++) {
                if (ContainsSignalReference(_nodes[i], AnimationGraphOutputPortKind.EnterSignal, signal)
                    || ContainsSignalReference(_nodes[i], AnimationGraphOutputPortKind.ExitSignal, signal)) {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsSignalReference(NodeEditorModel nodeModel, AnimationGraphOutputPortKind outputPortKind, Signal signal) {
            var signals = GetConnectedSignals(nodeModel, outputPortKind);
            for (var i = 0; i < signals.Count; i++) {
                if (signals[i] == signal) {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<Signal> GetConnectedSignals(NodeEditorModel nodeModel, AnimationGraphOutputPortKind outputPortKind) {
            if (outputPortKind == AnimationGraphOutputPortKind.EnterSignal) {
                return nodeModel.EnterSignals;
            }

            return outputPortKind == AnimationGraphOutputPortKind.ExitSignal
                ? nodeModel.ExitSignals
                : Array.Empty<Signal>();
        }

        /// <summary>
        /// LoopNode の内外をまたぐ接続でないかを判定
        /// </summary>
        /// <param name="outputPortKind">接続元 output port 種別</param>
        /// <param name="sourceNodeModel">接続元ノード Model</param>
        /// <param name="targetNodeModel">接続先ノード Model</param>
        /// <param name="errorMessage">接続できない理由</param>
        /// <returns>接続できる場合は true</returns>
        private bool CanConnectLoopScope(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, NodeEditorModel targetNodeModel, out string errorMessage) {
            var sourceLoopOwner = FindLoopOwner(sourceNodeModel.NodeId);
            var targetLoopOwner = FindLoopOwner(targetNodeModel.NodeId);
            if (outputPortKind == AnimationGraphOutputPortKind.Loop) {
                if (targetLoopOwner != null && targetLoopOwner.NodeId != sourceNodeModel.NodeId) {
                    errorMessage = "Node is already contained in another LoopNode";
                    return false;
                }

                if (!CanAddNodeToLoop((ScopedControlNodeEditorModel)sourceNodeModel, targetNodeModel, out errorMessage)) {
                    return false;
                }

                errorMessage = string.Empty;
                return true;
            }

            if (sourceLoopOwner != null && targetLoopOwner == null) {
                if (!CanAddNodeToLoop(sourceLoopOwner, targetNodeModel, out errorMessage)) {
                    return false;
                }

                errorMessage = string.Empty;
                return true;
            }

            if (targetLoopOwner != null && sourceLoopOwner == null) {
                if (!CanAddNodeToLoop(targetLoopOwner, sourceNodeModel, out errorMessage)) {
                    return false;
                }

                errorMessage = string.Empty;
                return true;
            }

            if (sourceLoopOwner != null && targetLoopOwner.NodeId != sourceLoopOwner.NodeId) {
                errorMessage = "Loop body node cannot connect outside its LoopNode";
                return false;
            }

            if (targetLoopOwner != null && sourceLoopOwner.NodeId != targetLoopOwner.NodeId) {
                errorMessage = "Loop body node cannot receive connections from outside its LoopNode";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// 指定したノードを LoopNode に追加できるかを判定
        /// </summary>
        /// <param name="loopNodeModel">追加先の ScopedControlNodeEditorModel</param>
        /// <param name="targetNodeModel">追加するノード Model</param>
        /// <param name="errorMessage">追加できない理由</param>
        /// <returns>追加できる場合は true</returns>
        private bool CanAddNodeToLoop(ScopedControlNodeEditorModel loopNodeModel, NodeEditorModel targetNodeModel, out string errorMessage) {
            if (targetNodeModel.NodeType == typeof(StartNode)) {
                errorMessage = "LoopNode cannot contain StartNode";
                return false;
            }

            if (targetNodeModel.NodeId == loopNodeModel.NodeId) {
                errorMessage = "LoopNode cannot contain itself";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// 指定したノードが所属する LoopNode を取得
        /// </summary>
        /// <param name="nodeId">所属を調べるノード ID</param>
        /// <returns>所属する ScopedControlNodeEditorModel</returns>
        private ScopedControlNodeEditorModel FindLoopOwner(string nodeId) {
            for (var i = 0; i < _nodes.Count; i++) {
                if (_nodes[i] is not ScopedControlNodeEditorModel loopNodeModel) {
                    continue;
                }

                if (ContainsLoopBodyNode(loopNodeModel, nodeId)) {
                    return loopNodeModel;
                }
            }

            return null;
        }

        /// <summary>
        /// ActionNode から参照可能な target 一覧を取得
        /// </summary>
        internal IReadOnlyList<TargetReference> GetTargetReferences(NodeEditorModel nodeModel) {
            var references = new List<TargetReference> { new(TargetReferenceKind.Binding, string.Empty) };
            for (var i = 0; i < TargetDefinitions.Count; i++) {
                var definition = TargetDefinitions[i];
                if (definition.Multiplicity == TargetMultiplicity.Single) {
                    references.Add(new TargetReference(TargetReferenceKind.Binding, definition.Key));
                }
            }

            var owner = FindLoopOwner(nodeModel.NodeId);
            while (owner != null) {
                if (owner.Node is IIterationScopeProvider) {
                    for (var i = 0; i < TargetDefinitions.Count; i++) {
                        var definition = TargetDefinitions[i];
                        if (definition.Multiplicity == TargetMultiplicity.Collection) {
                            references.Add(new TargetReference(TargetReferenceKind.CollectionItem, definition.Key, owner.NodeId));
                        }
                    }
                }

                owner = FindLoopOwner(owner.NodeId);
            }

            return references;
        }

        private void AddLoopValidationMessages(ScopedControlNodeEditorModel loopNodeModel, Dictionary<string, string> messagesByNodeId) {
            var loopBodyNodeIds = BuildLoopBodyNodeIdSet(loopNodeModel, messagesByNodeId);
            AddLoopBodyIsolationValidationMessages(loopNodeModel, loopBodyNodeIds, messagesByNodeId);
        }

        private bool ContainsLoopBodyNode(ScopedControlNodeEditorModel loopNodeModel, string nodeId) {
            return !string.IsNullOrEmpty(nodeId) && BuildLoopBodyNodeIdSet(loopNodeModel, null).Contains(nodeId);
        }

        private void AddNodeValidationMessages(Dictionary<string, string> messagesByNodeId) {
            var context = new NodeValidationContext(TargetDefinitions, BlackboardDefinitions);
            for (var i = 0; i < _nodes.Count; i++) {
                var nodeModel = _nodes[i];
                var message = nodeModel.Node.GetValidationMessage(context);
                AddNodeValidationMessage(messagesByNodeId, nodeModel.NodeId, message);
            }
        }

        private HashSet<string> BuildLoopBodyNodeIdSet(ScopedControlNodeEditorModel loopNodeModel, Dictionary<string, string> messagesByNodeId) {
            var loopBodyNodeIds = new HashSet<string>(StringComparer.Ordinal);
            var nodeIdsToVisit = new Queue<string>();
            var explicitLoopNodeIds = new HashSet<string>(StringComparer.Ordinal);
            var loopExitNodeIds = new HashSet<string>(loopNodeModel.NextNodeIds, StringComparer.Ordinal);
            var loopNodeIds = loopNodeModel.BodyNodeIds;
            for (var i = 0; i < loopNodeIds.Count; i++) {
                var loopNodeId = loopNodeIds[i];
                if (string.IsNullOrEmpty(loopNodeId)) {
                    AddNodeValidationMessageIfNeeded(messagesByNodeId, loopNodeModel.NodeId, $"{loopNodeModel.DisplayName} has empty loop node id");
                    continue;
                }

                if (loopNodeId == loopNodeModel.NodeId) {
                    AddNodeValidationMessageIfNeeded(messagesByNodeId, loopNodeModel.NodeId, $"{loopNodeModel.DisplayName} cannot contain itself");
                    continue;
                }

                if (!explicitLoopNodeIds.Add(loopNodeId)) {
                    AddNodeValidationMessageIfNeeded(messagesByNodeId, loopNodeModel.NodeId, $"{loopNodeModel.DisplayName} contains duplicated loop node");
                    continue;
                }

                if (!_nodeModelsById.TryGetValue(loopNodeId, out var bodyNodeModel)) {
                    AddNodeValidationMessageIfNeeded(messagesByNodeId, loopNodeModel.NodeId, $"{loopNodeModel.DisplayName} contains missing loop node");
                    continue;
                }

                if (bodyNodeModel.NodeType == typeof(StartNode)) {
                    AddNodeValidationMessageIfNeeded(messagesByNodeId, bodyNodeModel.NodeId, $"StartNode cannot be contained in {loopNodeModel.DisplayName}");
                    continue;
                }

                AddLoopBodyNodeId(loopBodyNodeIds, nodeIdsToVisit, loopNodeId);
            }

            while (nodeIdsToVisit.Count > 0) {
                var currentNodeId = nodeIdsToVisit.Dequeue();
                if (!_nodeModelsById.TryGetValue(currentNodeId, out var bodyNodeModel)) {
                    continue;
                }

                var connectedNodeIds = GetPhysicalConnectedNodeIds(bodyNodeModel);
                for (var i = 0; i < connectedNodeIds.Count; i++) {
                    var connectedNodeId = connectedNodeIds[i];
                    if (string.IsNullOrEmpty(connectedNodeId)) {
                        continue;
                    }

                    if (loopExitNodeIds.Contains(connectedNodeId)) {
                        continue;
                    }

                    if (!_nodeModelsById.TryGetValue(connectedNodeId, out var connectedNodeModel)) {
                        continue;
                    }

                    if (connectedNodeModel.NodeType == typeof(StartNode)) {
                        AddNodeValidationMessageIfNeeded(messagesByNodeId, connectedNodeModel.NodeId, $"StartNode cannot be contained in {loopNodeModel.DisplayName}");
                        continue;
                    }

                    AddLoopBodyNodeId(loopBodyNodeIds, nodeIdsToVisit, connectedNodeId);
                }
            }

            return loopBodyNodeIds;
        }

        private void AddLoopBodyIsolationValidationMessages(ScopedControlNodeEditorModel loopNodeModel, HashSet<string> loopBodyNodeIds, Dictionary<string, string> messagesByNodeId) {
            var loopExitNodeIds = new HashSet<string>(loopNodeModel.NextNodeIds, StringComparer.Ordinal);
            foreach (var loopBodyNodeId in loopBodyNodeIds) {
                if (!_nodeModelsById.TryGetValue(loopBodyNodeId, out var bodyNodeModel)) {
                    continue;
                }

                var connectedNodeIds = GetPhysicalConnectedNodeIds(bodyNodeModel);
                for (var i = 0; i < connectedNodeIds.Count; i++) {
                    if (!loopExitNodeIds.Contains(connectedNodeIds[i])) {
                        continue;
                    }

                    AddNodeValidationMessage(messagesByNodeId, bodyNodeModel.NodeId, $"{bodyNodeModel.DisplayName} connects outside {loopNodeModel.DisplayName}");
                    break;
                }
            }

            for (var i = 0; i < _nodes.Count; i++) {
                var sourceNodeModel = _nodes[i];
                if (loopBodyNodeIds.Contains(sourceNodeModel.NodeId)) {
                    continue;
                }

                var connectedNodeIds = GetPhysicalConnectedNodeIds(sourceNodeModel);
                for (var j = 0; j < connectedNodeIds.Count; j++) {
                    if (!loopBodyNodeIds.Contains(connectedNodeIds[j])) {
                        continue;
                    }

                    AddNodeValidationMessage(messagesByNodeId, connectedNodeIds[j], $"{GetNodeDisplayName(connectedNodeIds[j])} receives connection from outside {loopNodeModel.DisplayName}");
                }
            }
        }

        private static void AddLoopBodyNodeId(HashSet<string> loopBodyNodeIds, Queue<string> nodeIdsToVisit, string nodeId) {
            if (!loopBodyNodeIds.Add(nodeId)) {
                return;
            }

            nodeIdsToVisit.Enqueue(nodeId);
        }

        private static void AddNodeValidationMessageIfNeeded(Dictionary<string, string> messagesByNodeId, string nodeId, string message) {
            if (messagesByNodeId == null) {
                return;
            }

            AddNodeValidationMessage(messagesByNodeId, nodeId, message);
        }

        private static void AddNodeValidationMessage(Dictionary<string, string> messagesByNodeId, string nodeId, string message) {
            if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(message)) {
                return;
            }

            if (messagesByNodeId.TryGetValue(nodeId, out var currentMessage)) {
                if (currentMessage.Contains(message, StringComparison.Ordinal)) {
                    return;
                }

                messagesByNodeId[nodeId] = $"{currentMessage} {message}";
                return;
            }

            messagesByNodeId.Add(nodeId, message);
        }

        private string GetNodeDisplayName(string nodeId) {
            return _nodeModelsById.TryGetValue(nodeId, out var nodeModel) ? nodeModel.DisplayName : "Node";
        }

        /// <summary>
        /// input port に複数接続できる Node か判定
        /// </summary>
        /// <param name="nodeModel">判定するノード Model</param>
        /// <returns>複数接続できる場合は true</returns>
        private bool CanUseMultipleInputs(NodeEditorModel nodeModel) {
            return typeof(JoinNode).IsAssignableFrom(nodeModel.NodeType);
        }

        /// <summary>
        /// input port の単一接続制約を適用するか判定
        /// </summary>
        /// <param name="outputPortKind">接続元 output port 種別</param>
        /// <param name="sourceNodeModel">接続元ノード Model</param>
        /// <param name="targetNodeModel">接続先ノード Model</param>
        /// <returns>単一接続制約を適用する場合は true</returns>
        private bool ShouldEnforceSingleInput(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, NodeEditorModel targetNodeModel) {
            if (outputPortKind == AnimationGraphOutputPortKind.Loop) {
                return false;
            }

            return FindLoopOwner(sourceNodeModel.NodeId) == null && FindLoopOwner(targetNodeModel.NodeId) == null;
        }

        /// <summary>
        /// 指定ノードが既に input 接続を持つか判定
        /// </summary>
        /// <param name="targetNodeModel">判定する接続先ノード Model</param>
        /// <returns>既に input 接続を持つ場合は true</returns>
        private bool HasInputConnection(NodeEditorModel targetNodeModel) {
            var nodeModels = _nodes;
            for (var i = 0; i < nodeModels.Count; i++) {
                var sourceNodeModel = nodeModels[i];
                if (ContainsConnectedNodeId(sourceNodeModel, targetNodeModel.NodeId)) {
                    return true;
                }
            }

            return false;
        }

        private bool ContainsConnectedNodeId(NodeEditorModel sourceNodeModel, string targetNodeId) {
            var nodeIds = GetConnectedNodeIds(sourceNodeModel);
            for (var i = 0; i < nodeIds.Count; i++) {
                if (nodeIds[i] == targetNodeId) {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<string> GetConnectedNodeIds(NodeEditorModel nodeModel) {
            var nodeIds = new List<string>(GetPhysicalConnectedNodeIds(nodeModel));
            if (nodeModel is ScopedControlNodeEditorModel loopNodeModel) {
                var loopNodeIds = loopNodeModel.BodyNodeIds;
                for (var i = 0; i < loopNodeIds.Count; i++) {
                    nodeIds.Add(loopNodeIds[i]);
                }
            }

            return nodeIds;
        }

        /// <summary>
        /// 指定した output port の接続先ノード ID 一覧を取得
        /// </summary>
        /// <param name="nodeModel">接続元ノード Model</param>
        /// <param name="outputPortKind">取得する output port 種別</param>
        /// <returns>接続先ノード ID 一覧</returns>
        private static IReadOnlyList<string> GetConnectedNodeIds(NodeEditorModel nodeModel, AnimationGraphOutputPortKind outputPortKind) {
            if (outputPortKind.IsNext) {
                return nodeModel.NextNodeIds;
            }

            if (outputPortKind.IsBranchExtension && nodeModel is BranchNodeEditorModel branchNodeModel) {
                return branchNodeModel.GetExtensionNodeIds(outputPortKind.BranchExtensionIndex);
            }

            return outputPortKind == AnimationGraphOutputPortKind.Loop && nodeModel is ScopedControlNodeEditorModel loopNodeModel
                ? loopNodeModel.BodyNodeIds
                : Array.Empty<string>();
        }

        /// <summary>
        /// LoopNode の所有関係を除いた物理接続先ノード ID 一覧を取得
        /// </summary>
        /// <param name="nodeModel">接続元ノード Model</param>
        /// <returns>物理接続先ノード ID 一覧</returns>
        private static IReadOnlyList<string> GetPhysicalConnectedNodeIds(NodeEditorModel nodeModel) {
            var nodeIds = new List<string>();
            var nextNodeIds = nodeModel.NextNodeIds;
            for (var i = 0; i < nextNodeIds.Count; i++) {
                nodeIds.Add(nextNodeIds[i]);
            }

            if (nodeModel is BranchNodeEditorModel branchNodeModel) {
                for (var i = 0; i < branchNodeModel.ExtensionPortCount; i++) {
                    var extensionNodeIds = branchNodeModel.GetExtensionNodeIds(i);
                    for (var j = 0; j < extensionNodeIds.Count; j++) {
                        nodeIds.Add(extensionNodeIds[j]);
                    }
                }
            }

            return nodeIds;
        }

        /// <summary>
        /// 指定した output port の接続先ノード ID 一覧を設定
        /// </summary>
        /// <param name="nodeModel">接続元ノード Model</param>
        /// <param name="outputPortKind">設定する output port 種別</param>
        /// <param name="nodeIds">設定する接続先ノード ID 一覧</param>
        private static void SetConnectedNodeIds(NodeEditorModel nodeModel, AnimationGraphOutputPortKind outputPortKind, IReadOnlyList<string> nodeIds) {
            if (outputPortKind.IsNext) {
                nodeModel.SetNextNodeIds(nodeIds);
                return;
            }

            if (outputPortKind.IsBranchExtension) {
                ((BranchNodeEditorModel)nodeModel).SetExtensionNodeIds(outputPortKind.BranchExtensionIndex, nodeIds);
                return;
            }

            if (outputPortKind == AnimationGraphOutputPortKind.Loop) {
                ((ScopedControlNodeEditorModel)nodeModel).SetBodyNodeIds(nodeIds);
            }
        }

        /// <summary>
        /// 指定したノード ID から target node ID へ到達できるかを判定
        /// </summary>
        /// <param name="startNodeId">探索開始ノード ID</param>
        /// <param name="targetNodeId">到達判定するノード ID</param>
        /// <returns>到達できる場合は true</returns>
        private bool HasPath(string startNodeId, string targetNodeId) {
            var visitedNodeIds = new HashSet<string>();
            var nodeIdsToVisit = new Stack<string>();
            nodeIdsToVisit.Push(startNodeId);

            while (nodeIdsToVisit.Count > 0) {
                var nodeId = nodeIdsToVisit.Pop();
                if (!visitedNodeIds.Add(nodeId)) {
                    continue;
                }

                if (!_graphAsset.TryGetNode(nodeId, out var node)) {
                    continue;
                }

                var nextNodeIds = GetConnectedNodeIds(node);
                for (var i = 0; i < nextNodeIds.Count; i++) {
                    var nextNodeId = nextNodeIds[i];
                    if (nextNodeId == targetNodeId) {
                        return true;
                    }

                    nodeIdsToVisit.Push(nextNodeId);
                }
            }

            return false;
        }

        /// <summary>
        /// ノードから辿れる接続先ノード ID 一覧を取得
        /// </summary>
        /// <param name="node">接続元ノード</param>
        /// <returns>接続先ノード ID 一覧</returns>
        private static IReadOnlyList<string> GetConnectedNodeIds(Node node) {
            var nodeIds = new List<string>();
            var nextNodeIds = node.NextNodeIds;
            for (var i = 0; i < nextNodeIds.Count; i++) {
                nodeIds.Add(nextNodeIds[i]);
            }

            if (node is BranchNode branchNode) {
                for (var i = 0; i < branchNode.ExtensionPortCount; i++) {
                    var extensionNodeIds = branchNode.GetExtensionNodeIds(i);
                    for (var j = 0; j < extensionNodeIds.Count; j++) {
                        nodeIds.Add(extensionNodeIds[j]);
                    }
                }
            }

            if (node is ScopedControlNode loopNode) {
                var loopNodeIds = loopNode.BodyNodeIds;
                for (var i = 0; i < loopNodeIds.Count; i++) {
                    nodeIds.Add(loopNodeIds[i]);
                }
            }

            return nodeIds;
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
