using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// AnimationGraphAsset を表示編集する GraphView
    /// </summary>
    internal sealed class AnimationGraphView : GraphView {
        private readonly Dictionary<string, AnimationGraphNodeView> _nodeViewsById = new();
        private readonly Label _emptyStateLabel;
        private readonly Label _validationLabel;

        private AnimationGraphAssetEditorModel _assetModel;
        private bool _isRebuilding;

        /// <summary>ノード作成要求</summary>
        public event Action<Type, Vector2> NodeCreateRequested;
        /// <summary>ノード移動通知</summary>
        public event Action<NodeEditorModel, Rect> NodeMoved;
        /// <summary>Edge 作成要求</summary>
        public event Func<AnimationGraphOutputPortKind, NodeEditorModel, NodeEditorModel, bool> EdgeCreateRequested;
        /// <summary>Edge 削除要求</summary>
        public event Action<AnimationGraphOutputPortKind, NodeEditorModel, NodeEditorModel> EdgeRemoveRequested;
        /// <summary>選択状態変更通知</summary>
        public event Action SelectionChanged;
        /// <summary>コピー要求</summary>
        public event Action CopyRequested;
        /// <summary>ペースト要求</summary>
        public event Action PasteRequested;
        /// <summary>複製要求</summary>
        public event Action DuplicateRequested;
        /// <summary>削除要求</summary>
        public event Action DeleteRequested;
        /// <summary>ActionNode target key change request</summary>
        public event Action<NodeEditorModel, string> ActionTargetKeyChanged;
        /// <summary>DelayNode delay change request</summary>
        public event Action<DelayNodeEditorModel, float> DelayChanged;
        /// <summary>FlagBranchNode flag key change request</summary>
        public event Action<NodeEditorModel, string> FlagBranchKeyChanged;
        /// <summary>JoinNode join type change request</summary>
        public event Action<NodeEditorModel, JoinType> JoinTypeChanged;
        /// <summary>LoopNode loop count change request</summary>
        public event Action<LoopNodeEditorModel, int> LoopCountChanged;

        /// <summary>
        /// AnimationGraphView を作成
        /// </summary>
        public AnimationGraphView() {
            style.flexGrow = 1.0f;
            focusable = true;

            var gridBackground = new GridBackground();
            Insert(0, gridBackground);
            gridBackground.StretchToParentSize();

            _emptyStateLabel = new Label {
                pickingMode = PickingMode.Ignore,
                style = {
                    position = Position.Absolute,
                    left = 0.0f,
                    right = 0.0f,
                    top = 0.0f,
                    bottom = 0.0f,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    fontSize = 13,
                    color = new Color(0.72f, 0.72f, 0.72f),
                },
            };
            Add(_emptyStateLabel);

            _validationLabel = CreateValidationLabel();
            Add(_validationLabel);

            SetupZoom(0.05f, 2.5f);
            new ContentDragger { target = this };
            new SelectionDragger { target = this };
            new RectangleSelector { target = this };

            graphViewChanged = OnGraphViewChanged;
            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        /// <summary>
        /// 表示対象の AnimationGraphAssetEditorModel を設定
        /// </summary>
        /// <param name="assetModel">表示対象の Model</param>
        public void SetGraphAsset(AnimationGraphAssetEditorModel assetModel) {
            _assetModel = assetModel;
            Rebuild(assetModel);
        }

        /// <summary>
        /// GraphView の表示を再構築
        /// </summary>
        /// <param name="assetModel">表示対象の Model</param>
        public void Rebuild(AnimationGraphAssetEditorModel assetModel) {
            _isRebuilding = true;
            try {
                RemoveGraphElements();
                ClearValidationState();
                _assetModel = assetModel;
                if (assetModel == null || !assetModel.HasGraphAsset) {
                    SetEmptyState("Select an AnimationGraphAsset");
                    return;
                }

                var nodeModels = assetModel.Nodes;
                if (!assetModel.HasStartNode) {
                    SetEmptyState("Graph is not initialized");
                    return;
                }

                SetEmptyState(string.Empty);
                for (var i = 0; i < nodeModels.Count; i++) {
                    AddNodeView(nodeModels[i]);
                }

                for (var i = 0; i < nodeModels.Count; i++) {
                    AddEdgeViews(nodeModels[i]);
                }

                RefreshValidationState();
            }
            finally {
                _isRebuilding = false;
            }
        }

        /// <summary>
        /// 選択中の NodeEditorModel 一覧を取得
        /// </summary>
        /// <returns>選択中の NodeEditorModel 一覧</returns>
        public IReadOnlyList<NodeEditorModel> GetSelectedNodeModels() {
            var nodeModels = new List<NodeEditorModel>();
            foreach (var selectable in selection) {
                if (selectable is not AnimationGraphNodeView nodeView) {
                    continue;
                }

                nodeModels.Add(nodeView.NodeModel);
            }

            return nodeModels;
        }

        /// <summary>
        /// 選択中の Edge 接続一覧を取得
        /// </summary>
        /// <returns>選択中の Edge 接続一覧</returns>
        public IReadOnlyList<AnimationGraphEdgeConnection> GetSelectedEdgeConnections() {
            var edgeConnections = new List<AnimationGraphEdgeConnection>();
            foreach (var selectable in selection) {
                if (selectable is not Edge edge || !TryGetEdgeConnection(edge, out var edgeConnection)) {
                    continue;
                }

                edgeConnections.Add(edgeConnection);
            }

            return edgeConnections;
        }

        /// <summary>
        /// 指定した NodeEditorModel を選択
        /// </summary>
        /// <param name="nodeModels">選択する NodeEditorModel 一覧</param>
        public void SelectNodeModels(IReadOnlyList<NodeEditorModel> nodeModels) {
            ClearSelection();
            for (var i = 0; i < nodeModels.Count; i++) {
                if (!_nodeViewsById.TryGetValue(nodeModels[i].NodeId, out var nodeView)) {
                    continue;
                }

                AddToSelection(nodeView);
            }

            SelectionChanged?.Invoke();
        }

        /// <summary>
        /// Node 詳細表示を更新
        /// </summary>
        public void RefreshNodeDetails() {
            foreach (var nodeView in _nodeViewsById.Values) {
                nodeView.RefreshDetails();
            }

            RefreshValidationState();
        }

        /// <summary>
        /// 検証エラー表示を更新
        /// </summary>
        public void RefreshValidationState() {
            var validationMessages = _assetModel?.GetNodeValidationMessages();
            foreach (var nodeViewPair in _nodeViewsById) {
                var message = validationMessages != null && validationMessages.TryGetValue(nodeViewPair.Key, out var nodeMessage) ? nodeMessage : string.Empty;
                nodeViewPair.Value.SetValidationMessage(message);
            }

            SetValidationMessage(validationMessages);
        }

        /// <inheritdoc/>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
            base.BuildContextualMenu(evt);
            if (_assetModel == null || !_assetModel.HasGraphAsset) {
                return;
            }

            var graphPosition = GetGraphPosition(evt.localMousePosition);
            var nodeTypes = TypeCache.GetTypesDerivedFrom<UnityAnimationGraph.Node>();
            for (var i = 0; i < nodeTypes.Count; i++) {
                var nodeType = nodeTypes[i];
                if (!IsCreatableNodeType(nodeType)) {
                    continue;
                }

                var createMenuPath = AnimationGraphNodeMetadata.GetCreateMenuPath(nodeType);
                evt.menu.AppendAction($"Create/{createMenuPath}", _ => NodeCreateRequested?.Invoke(nodeType, graphPosition));
            }
        }

        /// <inheritdoc/>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
            var compatiblePorts = new List<Port>();
            if (_assetModel == null || startPort?.node is not AnimationGraphNodeView startNodeView) {
                return compatiblePorts;
            }

            ports.ForEach(port => {
                if (port == null || port == startPort || port.direction == startPort.direction || port.node == startPort.node) {
                    return;
                }

                if (port.node is not AnimationGraphNodeView portNodeView) {
                    return;
                }

                if (!TryGetOutputPortKind(startPort, startNodeView, port, portNodeView, out var outputPortKind)) {
                    return;
                }

                var sourceNodeModel = startPort.direction == Direction.Output ? startNodeView.NodeModel : portNodeView.NodeModel;
                var targetNodeModel = startPort.direction == Direction.Output ? portNodeView.NodeModel : startNodeView.NodeModel;
                if (!_assetModel.CanConnect(outputPortKind, sourceNodeModel, targetNodeModel, out _)) {
                    return;
                }

                compatiblePorts.Add(port);
            });
            return compatiblePorts;
        }

        private static bool IsCreatableNodeType(Type nodeType) {
            if (nodeType == null || nodeType.IsAbstract || nodeType.IsGenericType || nodeType == typeof(StartNode)) {
                return false;
            }

            if (!nodeType.IsPublic && !nodeType.IsNestedPublic) {
                return false;
            }

            var assemblyName = nodeType.Assembly.GetName().Name;
            return !assemblyName.EndsWith(".Tests", StringComparison.Ordinal);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange) {
            if (_isRebuilding) {
                return graphViewChange;
            }

            if (graphViewChange.movedElements != null) {
                for (var i = 0; i < graphViewChange.movedElements.Count; i++) {
                    if (graphViewChange.movedElements[i] is not AnimationGraphNodeView nodeView) {
                        continue;
                    }

                    NodeMoved?.Invoke(nodeView.NodeModel, nodeView.GetPosition());
                }
            }

            if (graphViewChange.edgesToCreate != null) {
                var edgesToCreate = new List<Edge>();
                for (var i = 0; i < graphViewChange.edgesToCreate.Count; i++) {
                    var edge = graphViewChange.edgesToCreate[i];
                    if (!TryGetEdgeConnection(edge, out var edgeConnection)) {
                        continue;
                    }

                    if (EdgeCreateRequested?.Invoke(edgeConnection.OutputPortKind, edgeConnection.SourceNodeModel, edgeConnection.TargetNodeModel) != true) {
                        continue;
                    }

                    edgesToCreate.Add(edge);
                }

                graphViewChange.edgesToCreate = edgesToCreate;
            }

            if (graphViewChange.elementsToRemove != null) {
                var nodeRemoved = false;
                for (var i = 0; i < graphViewChange.elementsToRemove.Count; i++) {
                    if (graphViewChange.elementsToRemove[i] is Edge edge && TryGetEdgeConnection(edge, out var edgeConnection)) {
                        EdgeRemoveRequested?.Invoke(edgeConnection.OutputPortKind, edgeConnection.SourceNodeModel, edgeConnection.TargetNodeModel);
                    }

                    if (graphViewChange.elementsToRemove[i] is AnimationGraphNodeView) {
                        nodeRemoved = true;
                    }
                }

                if (nodeRemoved) {
                    DeleteRequested?.Invoke();
                    graphViewChange.elementsToRemove = new List<GraphElement>();
                }
            }

            RefreshValidationState();
            return graphViewChange;
        }

        private void AddNodeView(NodeEditorModel nodeModel) {
            var nodeView = new AnimationGraphNodeView(nodeModel, GetTargetDefinitions, GetBlackboardDefinitions);
            nodeView.SelectionChanged += OnNodeViewSelectionChanged;
            nodeView.ActionTargetKeyChanged += OnNodeViewActionTargetKeyChanged;
            nodeView.DelayChanged += OnNodeViewDelayChanged;
            nodeView.FlagBranchKeyChanged += OnNodeViewFlagBranchKeyChanged;
            nodeView.JoinTypeChanged += OnNodeViewJoinTypeChanged;
            nodeView.LoopCountChanged += OnNodeViewLoopCountChanged;
            _nodeViewsById.Add(nodeModel.NodeId, nodeView);
            AddElement(nodeView);
        }

        private void AddEdgeViews(NodeEditorModel sourceNodeModel) {
            if (!_nodeViewsById.TryGetValue(sourceNodeModel.NodeId, out var sourceNodeView)) {
                return;
            }

            AddEdgeViews(sourceNodeView, AnimationGraphOutputPortKind.Next, sourceNodeModel.NextNodeIds);
            if (sourceNodeModel is BranchNodeEditorModel branchNodeModel) {
                AddEdgeViews(sourceNodeView, AnimationGraphOutputPortKind.False, branchNodeModel.FalseNodeIds);
            }

            if (sourceNodeModel is LoopNodeEditorModel loopNodeModel) {
                AddEdgeViews(sourceNodeView, AnimationGraphOutputPortKind.Loop, loopNodeModel.LoopNodeIds);
            }
        }

        private void AddEdgeViews(AnimationGraphNodeView sourceNodeView, AnimationGraphOutputPortKind outputPortKind, IReadOnlyList<string> targetNodeIds) {
            var outputPort = sourceNodeView.GetOutputPort(outputPortKind);
            if (outputPort == null) {
                return;
            }

            var nextNodeIds = targetNodeIds;
            for (var i = 0; i < nextNodeIds.Count; i++) {
                if (!_nodeViewsById.TryGetValue(nextNodeIds[i], out var targetNodeView)) {
                    continue;
                }

                if (targetNodeView.InputPort == null) {
                    continue;
                }

                var edge = outputPort.ConnectTo(targetNodeView.InputPort);
                AddElement(edge);
            }
        }

        private void RemoveGraphElements() {
            var elementsToRemove = new List<GraphElement>();
            foreach (var graphElement in graphElements) {
                if (graphElement is AnimationGraphNodeView nodeView) {
                    nodeView.SelectionChanged -= OnNodeViewSelectionChanged;
                    nodeView.ActionTargetKeyChanged -= OnNodeViewActionTargetKeyChanged;
                    nodeView.DelayChanged -= OnNodeViewDelayChanged;
                    nodeView.FlagBranchKeyChanged -= OnNodeViewFlagBranchKeyChanged;
                    nodeView.JoinTypeChanged -= OnNodeViewJoinTypeChanged;
                    nodeView.LoopCountChanged -= OnNodeViewLoopCountChanged;
                    elementsToRemove.Add(nodeView);
                    continue;
                }

                if (graphElement is Edge edge) {
                    elementsToRemove.Add(edge);
                }
            }

            for (var i = 0; i < elementsToRemove.Count; i++) {
                RemoveElement(elementsToRemove[i]);
            }

            _nodeViewsById.Clear();
        }

        private void OnNodeViewSelectionChanged() {
            SelectionChanged?.Invoke();
        }

        private void OnNodeViewActionTargetKeyChanged(NodeEditorModel nodeModel, string targetKey) {
            ActionTargetKeyChanged?.Invoke(nodeModel, targetKey);
        }

        private void OnNodeViewDelayChanged(DelayNodeEditorModel nodeModel, float delay) {
            DelayChanged?.Invoke(nodeModel, delay);
        }

        private void OnNodeViewFlagBranchKeyChanged(NodeEditorModel nodeModel, string flagKey) {
            FlagBranchKeyChanged?.Invoke(nodeModel, flagKey);
        }

        private void OnNodeViewJoinTypeChanged(NodeEditorModel nodeModel, JoinType joinType) {
            JoinTypeChanged?.Invoke(nodeModel, joinType);
        }

        private void OnNodeViewLoopCountChanged(LoopNodeEditorModel nodeModel, int loopCount) {
            LoopCountChanged?.Invoke(nodeModel, loopCount);
        }

        private void OnKeyDown(KeyDownEvent evt) {
            if (EditorGUIUtility.editingTextField) {
                return;
            }

            var actionKey = evt.ctrlKey || evt.commandKey;
            if (actionKey && evt.keyCode == KeyCode.C) {
                CopyRequested?.Invoke();
                evt.StopPropagation();
                return;
            }

            if (actionKey && evt.keyCode == KeyCode.V) {
                PasteRequested?.Invoke();
                evt.StopPropagation();
                return;
            }

            if (actionKey && evt.keyCode == KeyCode.D) {
                DuplicateRequested?.Invoke();
                evt.StopPropagation();
                return;
            }

            if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace) {
                DeleteRequested?.Invoke();
                evt.StopPropagation();
            }
        }

        private bool TryGetEdgeConnection(Edge edge, out AnimationGraphEdgeConnection edgeConnection) {
            if (edge.output?.node is AnimationGraphNodeView sourceNodeView && edge.input?.node is AnimationGraphNodeView targetNodeView && sourceNodeView.TryGetOutputPortKind(edge.output, out var outputPortKind)) {
                edgeConnection = new AnimationGraphEdgeConnection(outputPortKind, sourceNodeView.NodeModel, targetNodeView.NodeModel);
                return true;
            }

            edgeConnection = default;
            return false;
        }

        private static bool TryGetOutputPortKind(Port startPort, AnimationGraphNodeView startNodeView, Port candidatePort, AnimationGraphNodeView candidateNodeView, out AnimationGraphOutputPortKind outputPortKind) {
            if (startPort.direction == Direction.Output) {
                return startNodeView.TryGetOutputPortKind(startPort, out outputPortKind);
            }

            return candidateNodeView.TryGetOutputPortKind(candidatePort, out outputPortKind);
        }

        private void SetEmptyState(string message) {
            _emptyStateLabel.text = message ?? string.Empty;
            _emptyStateLabel.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void ClearValidationState() {
            foreach (var nodeView in _nodeViewsById.Values) {
                nodeView.SetValidationMessage(string.Empty);
            }

            SetValidationMessage(null);
        }

        private void SetValidationMessage(IReadOnlyDictionary<string, string> validationMessages) {
            if (validationMessages == null || validationMessages.Count == 0) {
                _validationLabel.text = string.Empty;
                _validationLabel.style.display = DisplayStyle.None;
                return;
            }

            var firstMessage = string.Empty;
            foreach (var validationMessage in validationMessages.Values) {
                firstMessage = validationMessage;
                break;
            }

            _validationLabel.text = validationMessages.Count == 1 ? firstMessage : $"{validationMessages.Count} nodes have graph errors. {firstMessage}";
            _validationLabel.style.display = DisplayStyle.Flex;
        }

        private Vector2 GetGraphPosition(Vector2 localPosition) {
            var worldPosition = worldTransform.MultiplyPoint3x4(new Vector3(localPosition.x, localPosition.y, 0.0f));
            return contentViewContainer.WorldToLocal(worldPosition);
        }

        private IReadOnlyList<AnimationGraphTargetDefinition> GetTargetDefinitions() {
            return _assetModel?.TargetDefinitions ?? Array.Empty<AnimationGraphTargetDefinition>();
        }

        private IReadOnlyList<AnimationGraphBlackboardDefinition> GetBlackboardDefinitions() {
            return _assetModel?.BlackboardDefinitions ?? Array.Empty<AnimationGraphBlackboardDefinition>();
        }

        private static Label CreateValidationLabel() {
            return new Label {
                pickingMode = PickingMode.Ignore,
                style = {
                    position = Position.Absolute,
                    left = 8.0f,
                    right = 8.0f,
                    top = 8.0f,
                    paddingLeft = 10.0f,
                    paddingRight = 10.0f,
                    paddingTop = 6.0f,
                    paddingBottom = 6.0f,
                    backgroundColor = new Color(0.45f, 0.06f, 0.05f, 0.94f),
                    borderTopColor = new Color(0.95f, 0.25f, 0.20f),
                    borderRightColor = new Color(0.95f, 0.25f, 0.20f),
                    borderBottomColor = new Color(0.95f, 0.25f, 0.20f),
                    borderLeftColor = new Color(0.95f, 0.25f, 0.20f),
                    borderTopWidth = 1.0f,
                    borderRightWidth = 1.0f,
                    borderBottomWidth = 1.0f,
                    borderLeftWidth = 1.0f,
                    color = Color.white,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    whiteSpace = WhiteSpace.Normal,
                    display = DisplayStyle.None,
                },
            };
        }
    }
}
