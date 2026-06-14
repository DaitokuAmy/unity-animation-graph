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
        private readonly Dictionary<string, AnimationGraphSignalView> _signalViewsById = new();
        private readonly Label _emptyStateLabel;
        private readonly Label _validationLabel;

        private AnimationGraphAssetEditorModel _assetModel;
        private bool _isRebuilding;
        private bool _isReadOnly;

        /// <summary>ノード作成要求</summary>
        public event Action<Type, Vector2> NodeCreateRequested;
        /// <summary>ノード移動通知</summary>
        public event Action<NodeEditorModel, Rect> NodeMoved;
        /// <summary>Signal 移動通知</summary>
        public event Action<SignalEditorModel, Rect> SignalMoved;
        /// <summary>Edge 作成要求</summary>
        public event Func<AnimationGraphOutputPortKind, NodeEditorModel, NodeEditorModel, bool> EdgeCreateRequested;
        /// <summary>Edge 削除要求</summary>
        public event Action<AnimationGraphOutputPortKind, NodeEditorModel, NodeEditorModel> EdgeRemoveRequested;
        /// <summary>Signal Edge 作成要求</summary>
        public event Func<AnimationGraphOutputPortKind, NodeEditorModel, SignalEditorModel, bool> SignalEdgeCreateRequested;
        /// <summary>Signal Edge 削除要求</summary>
        public event Action<AnimationGraphOutputPortKind, NodeEditorModel, SignalEditorModel> SignalEdgeRemoveRequested;
        /// <summary>Signal 作成要求</summary>
        public event Action<Type, Vector2> SignalCreateRequested;
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
        /// <summary>Node detail string field change request</summary>
        public event Action<NodeEditorModel, NodeDetailField, string> DetailStringChanged;
        /// <summary>Node detail bool field change request</summary>
        public event Action<NodeEditorModel, NodeDetailField, bool> DetailBoolChanged;
        /// <summary>Node detail int field change request</summary>
        public event Action<NodeEditorModel, NodeDetailField, int> DetailIntChanged;
        /// <summary>Node detail float field change request</summary>
        public event Action<NodeEditorModel, NodeDetailField, float> DetailFloatChanged;
        /// <summary>DelayNode delay change request</summary>
        public event Action<DelayNodeEditorModel, float> DelayChanged;
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
        /// GraphView の編集可否を設定
        /// </summary>
        /// <param name="isReadOnly">編集を禁止する場合は true</param>
        public void SetReadOnly(bool isReadOnly) {
            _isReadOnly = isReadOnly;
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

                var signalModels = assetModel.Signals;
                for (var i = 0; i < signalModels.Count; i++) {
                    AddSignalView(signalModels[i]);
                }

                for (var i = 0; i < nodeModels.Count; i++) {
                    AddEdgeViews(nodeModels[i]);
                    AddSignalEdgeViews(nodeModels[i]);
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
        /// 選択中の SignalEditorModel 一覧を取得
        /// </summary>
        /// <returns>選択中の SignalEditorModel 一覧</returns>
        public IReadOnlyList<SignalEditorModel> GetSelectedSignalModels() {
            var signalModels = new List<SignalEditorModel>();
            foreach (var selectable in selection) {
                if (selectable is not AnimationGraphSignalView signalView) {
                    continue;
                }

                signalModels.Add(signalView.SignalModel);
            }

            return signalModels;
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
        /// 選択中の Signal Edge 接続一覧を取得
        /// </summary>
        /// <returns>選択中の Signal Edge 接続一覧</returns>
        public IReadOnlyList<AnimationGraphSignalEdgeConnection> GetSelectedSignalEdgeConnections() {
            var edgeConnections = new List<AnimationGraphSignalEdgeConnection>();
            foreach (var selectable in selection) {
                if (selectable is not Edge edge || !TryGetSignalEdgeConnection(edge, out var edgeConnection)) {
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
            SelectGraphElementModels(nodeModels, Array.Empty<SignalEditorModel>());
        }

        /// <summary>
        /// 指定した NodeEditorModel と SignalEditorModel を選択
        /// </summary>
        /// <param name="nodeModels">選択する NodeEditorModel 一覧</param>
        /// <param name="signalModels">選択する SignalEditorModel 一覧</param>
        public void SelectGraphElementModels(IReadOnlyList<NodeEditorModel> nodeModels, IReadOnlyList<SignalEditorModel> signalModels) {
            ClearSelection();
            for (var i = 0; i < nodeModels.Count; i++) {
                if (!_nodeViewsById.TryGetValue(nodeModels[i].NodeId, out var nodeView)) {
                    continue;
                }

                AddToSelection(nodeView);
            }

            for (var i = 0; i < signalModels.Count; i++) {
                if (!_signalViewsById.TryGetValue(signalModels[i].SignalId, out var signalView)) {
                    continue;
                }

                AddToSelection(signalView);
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
        /// Preview 中の Node 実行状態表示を更新
        /// </summary>
        public void RefreshPreviewExecutionState() {
            foreach (var nodeView in _nodeViewsById.Values) {
                nodeView.RefreshPreviewExecutionState();
            }
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
            if (_isReadOnly || _assetModel == null || !_assetModel.HasGraphAsset) {
                return;
            }

            var graphPosition = GetGraphPosition(evt.localMousePosition);
            var nodeTypes = TypeCache.GetTypesDerivedFrom<UnityAnimationGraph.Node>();
            for (var i = 0; i < nodeTypes.Count; i++) {
                var nodeType = nodeTypes[i];
                if (!IsCreatableNodeType(nodeType)) {
                    continue;
                }

                var createMenuPath = NodeMetadata.GetCreateMenuPath(nodeType);
                evt.menu.AppendAction($"Create/{createMenuPath}", _ => NodeCreateRequested?.Invoke(nodeType, graphPosition));
            }

            AppendSignalCreateActions(evt.menu, graphPosition);
        }

        /// <inheritdoc/>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
            var compatiblePorts = new List<Port>();
            if (_isReadOnly || _assetModel == null || startPort == null) {
                return compatiblePorts;
            }

            ports.ForEach(port => {
                if (port == null || port == startPort || port.direction == startPort.direction || port.node == startPort.node) {
                    return;
                }

                if (TryGetNodeEdgeConnection(startPort, port, out var outputPortKind, out var sourceNodeModel, out var targetNodeModel)) {
                    if (!_assetModel.CanConnect(outputPortKind, sourceNodeModel, targetNodeModel, out _)) {
                        return;
                    }

                    compatiblePorts.Add(port);
                    return;
                }

                if (!TryGetSignalEdgeConnection(startPort, port, out var signalOutputPortKind, out var signalSourceNodeModel, out var targetSignalModel)) {
                    return;
                }

                if (!_assetModel.CanConnectSignal(signalOutputPortKind, signalSourceNodeModel, targetSignalModel, out _)) {
                    return;
                }

                compatiblePorts.Add(port);
            });
            return compatiblePorts;
        }

        private static bool IsCreatableSignalType(Type signalType) {
            if (signalType == null || signalType.IsAbstract || signalType.IsGenericType) {
                return false;
            }

            if (!signalType.IsPublic && !signalType.IsNestedPublic) {
                return false;
            }

            var assemblyName = signalType.Assembly.GetName().Name;
            return !assemblyName.EndsWith(".Tests", StringComparison.Ordinal);
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

        private void AppendSignalCreateActions(DropdownMenu menu, Vector2 graphPosition) {
            var signalTypes = TypeCache.GetTypesDerivedFrom<UnityAnimationGraph.Signal>();
            var hasSignalType = false;
            for (var i = 0; i < signalTypes.Count; i++) {
                var signalType = signalTypes[i];
                if (!IsCreatableSignalType(signalType)) {
                    continue;
                }

                hasSignalType = true;
                var createMenuPath = GetSignalCreateMenuPath(signalType);
                var capturedSignalType = signalType;
                menu.AppendAction($"Create/Signal/{createMenuPath}", _ => SignalCreateRequested?.Invoke(capturedSignalType, graphPosition));
            }

            if (!hasSignalType) {
                menu.AppendAction("Create/Signal/No creatable Signal", _ => { }, DropdownMenuAction.Status.Disabled);
            }
        }

        private static bool TryGetSignalOutputPort(IEventHandler target, out AnimationGraphNodeView nodeView, out AnimationGraphOutputPortKind outputPortKind) {
            var element = target as VisualElement;
            while (element != null) {
                if (element is Port port && port.node is AnimationGraphNodeView currentNodeView && currentNodeView.TryGetOutputPortKind(port, out var currentOutputPortKind) && currentOutputPortKind.IsSignal) {
                    nodeView = currentNodeView;
                    outputPortKind = currentOutputPortKind;
                    return true;
                }

                element = element.parent;
            }

            nodeView = null;
            outputPortKind = default;
            return false;
        }

        private static string GetSignalCreateMenuPath(Type signalType) {
            return SignalMetadata.GetCreateMenuPath(signalType);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange) {
            if (_isRebuilding) {
                return graphViewChange;
            }

            if (_isReadOnly) {
                graphViewChange.edgesToCreate = null;
                graphViewChange.elementsToRemove = null;
                graphViewChange.movedElements = null;
                graphViewChange.moveDelta = Vector2.zero;
                return graphViewChange;
            }

            if (graphViewChange.movedElements != null) {
                for (var i = 0; i < graphViewChange.movedElements.Count; i++) {
                    if (graphViewChange.movedElements[i] is AnimationGraphNodeView nodeView) {
                        NodeMoved?.Invoke(nodeView.NodeModel, nodeView.GetPosition());
                        continue;
                    }

                    if (graphViewChange.movedElements[i] is AnimationGraphSignalView signalView) {
                        SignalMoved?.Invoke(signalView.SignalModel, signalView.GetPosition());
                    }
                }
            }

            if (graphViewChange.edgesToCreate != null) {
                var edgesToCreate = new List<Edge>();
                for (var i = 0; i < graphViewChange.edgesToCreate.Count; i++) {
                    var edge = graphViewChange.edgesToCreate[i];
                    if (!TryGetEdgeConnection(edge, out var edgeConnection)) {
                        if (!TryGetSignalEdgeConnection(edge, out var signalEdgeConnection)) {
                            continue;
                        }

                        if (SignalEdgeCreateRequested?.Invoke(signalEdgeConnection.OutputPortKind, signalEdgeConnection.SourceNodeModel, signalEdgeConnection.TargetSignalModel) != true) {
                            continue;
                        }

                        edgesToCreate.Add(edge);
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
                var graphNodeRemoved = false;
                for (var i = 0; i < graphViewChange.elementsToRemove.Count; i++) {
                    if (graphViewChange.elementsToRemove[i] is Edge edge && TryGetEdgeConnection(edge, out var edgeConnection)) {
                        EdgeRemoveRequested?.Invoke(edgeConnection.OutputPortKind, edgeConnection.SourceNodeModel, edgeConnection.TargetNodeModel);
                    }

                    if (graphViewChange.elementsToRemove[i] is Edge signalEdge && TryGetSignalEdgeConnection(signalEdge, out var signalEdgeConnection)) {
                        SignalEdgeRemoveRequested?.Invoke(signalEdgeConnection.OutputPortKind, signalEdgeConnection.SourceNodeModel, signalEdgeConnection.TargetSignalModel);
                    }

                    if (graphViewChange.elementsToRemove[i] is AnimationGraphNodeView or AnimationGraphSignalView) {
                        graphNodeRemoved = true;
                    }
                }

                if (graphNodeRemoved) {
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
            nodeView.DetailStringChanged += OnNodeViewDetailStringChanged;
            nodeView.DetailBoolChanged += OnNodeViewDetailBoolChanged;
            nodeView.DetailIntChanged += OnNodeViewDetailIntChanged;
            nodeView.DetailFloatChanged += OnNodeViewDetailFloatChanged;
            nodeView.DelayChanged += OnNodeViewDelayChanged;
            nodeView.JoinTypeChanged += OnNodeViewJoinTypeChanged;
            nodeView.LoopCountChanged += OnNodeViewLoopCountChanged;
            _nodeViewsById.Add(nodeModel.NodeId, nodeView);
            AddElement(nodeView);
        }

        private void AddSignalView(SignalEditorModel signalModel) {
            var signalView = new AnimationGraphSignalView(signalModel);
            signalView.SelectionChanged += OnSignalViewSelectionChanged;
            _signalViewsById.Add(signalModel.SignalId, signalView);
            AddElement(signalView);
        }

        private void AddEdgeViews(NodeEditorModel sourceNodeModel) {
            if (!_nodeViewsById.TryGetValue(sourceNodeModel.NodeId, out var sourceNodeView)) {
                return;
            }

            AddEdgeViews(sourceNodeView, AnimationGraphOutputPortKind.Next, sourceNodeModel.NextNodeIds);
            if (sourceNodeModel is BranchNodeEditorModel branchNodeModel) {
                for (var i = 0; i < branchNodeModel.ExtensionPortCount; i++) {
                    AddEdgeViews(sourceNodeView, AnimationGraphOutputPortKind.BranchExtension(i), branchNodeModel.GetExtensionNodeIds(i));
                }
            }

            if (sourceNodeModel is LoopNodeEditorModel loopNodeModel) {
                AddEdgeViews(sourceNodeView, AnimationGraphOutputPortKind.Loop, loopNodeModel.LoopNodeIds);
            }
        }

        private void AddSignalEdgeViews(NodeEditorModel sourceNodeModel) {
            if (!_nodeViewsById.TryGetValue(sourceNodeModel.NodeId, out var sourceNodeView)) {
                return;
            }

            AddSignalEdgeViews(sourceNodeView, AnimationGraphOutputPortKind.EnterSignal, sourceNodeModel.EnterSignals);
            AddSignalEdgeViews(sourceNodeView, AnimationGraphOutputPortKind.ExitSignal, sourceNodeModel.ExitSignals);
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

        private void AddSignalEdgeViews(AnimationGraphNodeView sourceNodeView, AnimationGraphOutputPortKind outputPortKind, IReadOnlyList<Signal> targetSignals) {
            var outputPort = sourceNodeView.GetOutputPort(outputPortKind);
            if (outputPort == null) {
                return;
            }

            for (var i = 0; i < targetSignals.Count; i++) {
                var targetSignal = targetSignals[i];
                if (targetSignal == null || !_signalViewsById.TryGetValue(targetSignal.SignalId, out var targetSignalView)) {
                    continue;
                }

                var edge = outputPort.ConnectTo(targetSignalView.InputPort);
                AddElement(edge);
            }
        }

        private void RemoveGraphElements() {
            var elementsToRemove = new List<GraphElement>();
            foreach (var graphElement in graphElements) {
                if (graphElement is AnimationGraphNodeView nodeView) {
                    nodeView.SelectionChanged -= OnNodeViewSelectionChanged;
                    nodeView.ActionTargetKeyChanged -= OnNodeViewActionTargetKeyChanged;
                    nodeView.DetailStringChanged -= OnNodeViewDetailStringChanged;
                    nodeView.DetailBoolChanged -= OnNodeViewDetailBoolChanged;
                    nodeView.DetailIntChanged -= OnNodeViewDetailIntChanged;
                    nodeView.DetailFloatChanged -= OnNodeViewDetailFloatChanged;
                    nodeView.DelayChanged -= OnNodeViewDelayChanged;
                    nodeView.JoinTypeChanged -= OnNodeViewJoinTypeChanged;
                    nodeView.LoopCountChanged -= OnNodeViewLoopCountChanged;
                    elementsToRemove.Add(nodeView);
                    continue;
                }

                if (graphElement is AnimationGraphSignalView signalView) {
                    signalView.SelectionChanged -= OnSignalViewSelectionChanged;
                    elementsToRemove.Add(signalView);
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
            _signalViewsById.Clear();
        }

        private void OnNodeViewSelectionChanged() {
            SelectionChanged?.Invoke();
        }

        private void OnSignalViewSelectionChanged() {
            SelectionChanged?.Invoke();
        }

        private void OnNodeViewActionTargetKeyChanged(NodeEditorModel nodeModel, string targetKey) {
            if (_isReadOnly) {
                return;
            }

            ActionTargetKeyChanged?.Invoke(nodeModel, targetKey);
        }

        private void OnNodeViewDetailStringChanged(NodeEditorModel nodeModel, NodeDetailField field, string value) {
            if (_isReadOnly) {
                return;
            }

            DetailStringChanged?.Invoke(nodeModel, field, value);
        }

        private void OnNodeViewDetailBoolChanged(NodeEditorModel nodeModel, NodeDetailField field, bool value) {
            if (_isReadOnly) {
                return;
            }

            DetailBoolChanged?.Invoke(nodeModel, field, value);
        }

        private void OnNodeViewDetailIntChanged(NodeEditorModel nodeModel, NodeDetailField field, int value) {
            if (_isReadOnly) {
                return;
            }

            DetailIntChanged?.Invoke(nodeModel, field, value);
        }

        private void OnNodeViewDetailFloatChanged(NodeEditorModel nodeModel, NodeDetailField field, float value) {
            if (_isReadOnly) {
                return;
            }

            DetailFloatChanged?.Invoke(nodeModel, field, value);
        }

        private void OnNodeViewDelayChanged(DelayNodeEditorModel nodeModel, float delay) {
            if (_isReadOnly) {
                return;
            }

            DelayChanged?.Invoke(nodeModel, delay);
        }

        private void OnNodeViewJoinTypeChanged(NodeEditorModel nodeModel, JoinType joinType) {
            if (_isReadOnly) {
                return;
            }

            JoinTypeChanged?.Invoke(nodeModel, joinType);
        }

        private void OnNodeViewLoopCountChanged(LoopNodeEditorModel nodeModel, int loopCount) {
            if (_isReadOnly) {
                return;
            }

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
                if (_isReadOnly) {
                    evt.StopPropagation();
                    return;
                }

                PasteRequested?.Invoke();
                evt.StopPropagation();
                return;
            }

            if (actionKey && evt.keyCode == KeyCode.D) {
                if (_isReadOnly) {
                    evt.StopPropagation();
                    return;
                }

                DuplicateRequested?.Invoke();
                evt.StopPropagation();
                return;
            }

            if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace) {
                if (_isReadOnly) {
                    evt.StopPropagation();
                    return;
                }

                DeleteRequested?.Invoke();
                evt.StopPropagation();
            }
        }

        private bool TryGetEdgeConnection(Edge edge, out AnimationGraphEdgeConnection edgeConnection) {
            if (TryGetNodeEdgeConnection(edge.output, edge.input, out var outputPortKind, out var sourceNodeModel, out var targetNodeModel)) {
                edgeConnection = new AnimationGraphEdgeConnection(outputPortKind, sourceNodeModel, targetNodeModel);
                return true;
            }

            edgeConnection = default;
            return false;
        }

        private bool TryGetSignalEdgeConnection(Edge edge, out AnimationGraphSignalEdgeConnection edgeConnection) {
            if (TryGetSignalEdgeConnection(edge.output, edge.input, out var outputPortKind, out var sourceNodeModel, out var targetSignalModel)) {
                edgeConnection = new AnimationGraphSignalEdgeConnection(outputPortKind, sourceNodeModel, targetSignalModel);
                return true;
            }

            edgeConnection = default;
            return false;
        }

        private static bool TryGetNodeEdgeConnection(Port firstPort, Port secondPort, out AnimationGraphOutputPortKind outputPortKind, out NodeEditorModel sourceNodeModel, out NodeEditorModel targetNodeModel) {
            var outputPort = firstPort?.direction == Direction.Output ? firstPort : secondPort;
            var inputPort = firstPort?.direction == Direction.Input ? firstPort : secondPort;
            if (outputPort?.node is AnimationGraphNodeView sourceNodeView
                && inputPort?.node is AnimationGraphNodeView targetNodeView
                && targetNodeView.InputPort == inputPort
                && sourceNodeView.TryGetOutputPortKind(outputPort, out outputPortKind)
                && !outputPortKind.IsSignal) {
                sourceNodeModel = sourceNodeView.NodeModel;
                targetNodeModel = targetNodeView.NodeModel;
                return true;
            }

            outputPortKind = default;
            sourceNodeModel = null;
            targetNodeModel = null;
            return false;
        }

        private static bool TryGetSignalEdgeConnection(Port firstPort, Port secondPort, out AnimationGraphOutputPortKind outputPortKind, out NodeEditorModel sourceNodeModel, out SignalEditorModel targetSignalModel) {
            var outputPort = firstPort?.direction == Direction.Output ? firstPort : secondPort;
            var inputPort = firstPort?.direction == Direction.Input ? firstPort : secondPort;
            if (outputPort?.node is AnimationGraphNodeView sourceNodeView
                && inputPort?.node is AnimationGraphSignalView targetSignalView
                && targetSignalView.InputPort == inputPort
                && sourceNodeView.TryGetOutputPortKind(outputPort, out outputPortKind)
                && outputPortKind.IsSignal) {
                sourceNodeModel = sourceNodeView.NodeModel;
                targetSignalModel = targetSignalView.SignalModel;
                return true;
            }

            outputPortKind = default;
            sourceNodeModel = null;
            targetSignalModel = null;
            return false;
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

        private IReadOnlyList<TargetDefinition> GetTargetDefinitions() {
            return _assetModel?.TargetDefinitions ?? Array.Empty<TargetDefinition>();
        }

        private IReadOnlyList<BlackboardDefinition> GetBlackboardDefinitions() {
            return _assetModel?.BlackboardDefinitions ?? Array.Empty<BlackboardDefinition>();
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
