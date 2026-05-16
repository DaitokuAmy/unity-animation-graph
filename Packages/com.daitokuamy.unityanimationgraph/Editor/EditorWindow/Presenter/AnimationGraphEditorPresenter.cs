using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Animation Graph EditorWindow の View と Model を仲介するクラス
    /// </summary>
    internal sealed class AnimationGraphEditorPresenter : IDisposable {
        private static readonly Vector2 DuplicateOffset = new(30.0f, 30.0f);
        private static readonly Vector2 DefaultStartNodePosition = new(80.0f, 80.0f);

        private readonly AnimationGraphAssetEditorModel _assetModel = new();
        private readonly List<NodeEditorModel> _copiedNodeModels = new();
        private readonly List<string> _inspectedNodeIds = new();

        private ObjectField _graphAssetField;
        private AnimationGraphSchemaView _schemaView;
        private AnimationGraphView _graphView;
        private AnimationGraphInspectorView _inspectorView;
        private Label _footerLabel;
        private AnimationGraphRunner _previewRunner;
        private AnimationGraphSchedule _previewSchedule;
        private AnimationGraphPlayerState _previewState;

        /// <summary>Inspector 表示対象 node ID 一覧が変更されたときに発火</summary>
        public event Action<IReadOnlyList<string>> InspectedNodeIdsChanged;

        /// <summary>
        /// Presenter を初期化
        /// </summary>
        /// <param name="graphAssetField">GraphAsset を表示する ObjectField</param>
        /// <param name="schemaView">Target と Blackboard を表示する View</param>
        /// <param name="graphView">GraphView 領域</param>
        /// <param name="inspectorView">Inspector 領域</param>
        /// <param name="footerLabel">Footer 領域</param>
        /// <param name="initialInspectedNodeIds">初期表示する Inspector 対象 node ID 一覧</param>
        public void Initialize(ObjectField graphAssetField, AnimationGraphSchemaView schemaView, AnimationGraphView graphView, AnimationGraphInspectorView inspectorView, Label footerLabel, IReadOnlyList<string> initialInspectedNodeIds) {
            _graphAssetField = graphAssetField ?? throw new ArgumentNullException(nameof(graphAssetField));
            _schemaView = schemaView ?? throw new ArgumentNullException(nameof(schemaView));
            _graphView = graphView ?? throw new ArgumentNullException(nameof(graphView));
            _inspectorView = inspectorView ?? throw new ArgumentNullException(nameof(inspectorView));
            _footerLabel = footerLabel ?? throw new ArgumentNullException(nameof(footerLabel));

            _graphAssetField.RegisterValueChangedCallback(OnGraphAssetChanged);
            _graphView.NodeCreateRequested += AddNode;
            _graphView.NodeMoved += MoveNode;
            _graphView.EdgeCreateRequested += ConnectNodes;
            _graphView.EdgeRemoveRequested += DisconnectNodes;
            _graphView.SelectionChanged += UpdateInspectorSelection;
            _graphView.CopyRequested += CopySelection;
            _graphView.PasteRequested += PasteCopiedNodes;
            _graphView.DuplicateRequested += DuplicateSelection;
            _graphView.DeleteRequested += DeleteSelection;
            _graphView.ActionTargetKeyChanged += SetActionTargetKey;
            _graphView.DelayChanged += SetDelay;
            _graphView.FlagBranchKeyChanged += SetFlagBranchKey;
            _graphView.JoinTypeChanged += SetJoinType;
            _graphView.LoopCountChanged += SetLoopCount;
            _schemaView.SchemaChanged += RefreshNodeDetails;
            _inspectorView.NodePropertiesChanged += RefreshNodeDetails;
            EditorApplication.update += RefreshPreviewSchedule;
            Undo.undoRedoPerformed += RefreshGraph;

            SetFooterMessage("Select an AnimationGraphAsset");
            SetGraphAsset((AnimationGraphAsset)_graphAssetField.value);
            SetInspectorSelectionByIds(initialInspectedNodeIds);
        }

        /// <inheritdoc/>
        public void Dispose() {
            if (_graphAssetField != null) {
                _graphAssetField.UnregisterValueChangedCallback(OnGraphAssetChanged);
            }

            if (_graphView != null) {
                _graphView.NodeCreateRequested -= AddNode;
                _graphView.NodeMoved -= MoveNode;
                _graphView.EdgeCreateRequested -= ConnectNodes;
                _graphView.EdgeRemoveRequested -= DisconnectNodes;
                _graphView.SelectionChanged -= UpdateInspectorSelection;
                _graphView.CopyRequested -= CopySelection;
                _graphView.PasteRequested -= PasteCopiedNodes;
                _graphView.DuplicateRequested -= DuplicateSelection;
                _graphView.DeleteRequested -= DeleteSelection;
                _graphView.ActionTargetKeyChanged -= SetActionTargetKey;
                _graphView.DelayChanged -= SetDelay;
                _graphView.FlagBranchKeyChanged -= SetFlagBranchKey;
                _graphView.JoinTypeChanged -= SetJoinType;
                _graphView.LoopCountChanged -= SetLoopCount;
            }

            if (_schemaView != null) {
                _schemaView.SchemaChanged -= RefreshNodeDetails;
            }

            if (_inspectorView != null) {
                _inspectorView.NodePropertiesChanged -= RefreshNodeDetails;
            }

            EditorApplication.update -= RefreshPreviewSchedule;
            Undo.undoRedoPerformed -= RefreshGraph;
            _schemaView?.Dispose();
            _inspectorView?.Dispose();
        }

        private void OnGraphAssetChanged(ChangeEvent<UnityEngine.Object> evt) {
            SetGraphAsset((AnimationGraphAsset)evt.newValue);
        }

        private void SetGraphAsset(AnimationGraphAsset graphAsset) {
            _assetModel.SetGraphAsset(graphAsset);
            if (graphAsset != null && !_assetModel.HasStartNode && _assetModel.Nodes.Count == 0) {
                try {
                    _assetModel.InitializeGraph(DefaultStartNodePosition);
                }
                catch (Exception exception) {
                    SetFooterMessage(exception.Message);
                }
            }

            _copiedNodeModels.Clear();
            ClearInspectorSelection();
            _schemaView.SetGraphAsset(graphAsset);
            _graphView.SetGraphAsset(_assetModel);
            RefreshPreviewSchedule();
            if (graphAsset == null) {
                SetFooterMessage("Select an AnimationGraphAsset");
                return;
            }

            SetFooterMessage(_assetModel.HasStartNode ? graphAsset.name : "Graph is not initialized");
        }

        private void AddNode(Type nodeType, Vector2 graphPosition) {
            if (!_assetModel.HasGraphAsset) {
                SetFooterMessage("GraphAsset is not selected");
                return;
            }

            var nodeModel = _assetModel.AddNode(nodeType, graphPosition);
            _graphView.Rebuild(_assetModel);
            _graphView.SelectNodeModels(new[] { nodeModel });
            UpdateInspectorSelection();
            SetFooterMessage($"{nodeModel.DisplayName} added");
        }

        private void MoveNode(NodeEditorModel nodeModel, Rect nodePosition) {
            nodeModel.SetGraphPosition(nodePosition.position);
        }

        private void SetActionTargetKey(NodeEditorModel nodeModel, string targetKey) {
            if (nodeModel == null) {
                return;
            }

            nodeModel.SetActionTargetKey(targetKey);
            RebuildPreviewSchedule();
            RefreshNodeDetails();
            RefreshInspectorSelection();
            SetFooterMessage($"{nodeModel.DisplayName} target: {GetDisplayValue(targetKey)}");
        }

        private void SetDelay(DelayNodeEditorModel nodeModel, float delay) {
            if (nodeModel == null) {
                return;
            }

            nodeModel.SetDelay(delay);
            RebuildPreviewSchedule();
            RefreshNodeDetails();
            RefreshInspectorSelection();
            SetFooterMessage($"{nodeModel.DisplayName} delay: {nodeModel.Delay:0.###}");
        }

        private void SetFlagBranchKey(NodeEditorModel nodeModel, string flagKey) {
            if (nodeModel == null) {
                return;
            }

            nodeModel.SetFlagBranchKey(flagKey);
            RebuildPreviewSchedule();
            RefreshNodeDetails();
            RefreshInspectorSelection();
            SetFooterMessage($"{nodeModel.DisplayName} flag: {GetDisplayValue(flagKey)}");
        }

        private void SetJoinType(NodeEditorModel nodeModel, JoinType joinType) {
            if (nodeModel == null) {
                return;
            }

            nodeModel.SetJoinType(joinType);
            RebuildPreviewSchedule();
            RefreshNodeDetails();
            RefreshInspectorSelection();
            SetFooterMessage($"{nodeModel.DisplayName} join: {joinType}");
        }

        private void SetLoopCount(LoopNodeEditorModel nodeModel, int loopCount) {
            if (nodeModel == null) {
                return;
            }

            nodeModel.SetLoopCount(loopCount);
            RebuildPreviewSchedule();
            RefreshNodeDetails();
            RefreshInspectorSelection();
            SetFooterMessage($"{nodeModel.DisplayName} count: {nodeModel.LoopCount}");
        }

        private bool ConnectNodes(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, NodeEditorModel targetNodeModel) {
            if (!_assetModel.Connect(outputPortKind, sourceNodeModel, targetNodeModel, out var errorMessage)) {
                SetFooterMessage(errorMessage);
                return false;
            }

            SetFooterMessage($"{sourceNodeModel.DisplayName}.{GetOutputPortName(outputPortKind, sourceNodeModel)} -> {targetNodeModel.DisplayName}");
            return true;
        }

        private void DisconnectNodes(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, NodeEditorModel targetNodeModel) {
            if (_assetModel.Disconnect(outputPortKind, sourceNodeModel, targetNodeModel)) {
                SetFooterMessage($"{sourceNodeModel.DisplayName} disconnected");
            }
        }

        private void UpdateInspectorSelection() {
            var selectedNodeModels = _graphView.GetSelectedNodeModels();
            if (selectedNodeModels.Count > 0) {
                SetInspectorSelection(selectedNodeModels);
                return;
            }

            RefreshInspectorSelection();
        }

        private void CopySelection() {
            _copiedNodeModels.Clear();
            var selectedNodeModels = _graphView.GetSelectedNodeModels();
            for (var i = 0; i < selectedNodeModels.Count; i++) {
                var nodeModel = selectedNodeModels[i];
                if (!_assetModel.CanDuplicateNode(nodeModel)) {
                    continue;
                }

                _copiedNodeModels.Add(nodeModel);
            }

            SetFooterMessage($"{_copiedNodeModels.Count} node copied");
        }

        private void PasteCopiedNodes() {
            if (_copiedNodeModels.Count == 0) {
                SetFooterMessage("No copied node");
                return;
            }

            var duplicatedNodeModels = _assetModel.DuplicateNodes(_copiedNodeModels, DuplicateOffset);
            _graphView.Rebuild(_assetModel);
            _graphView.SelectNodeModels(duplicatedNodeModels);
            UpdateInspectorSelection();
            SetFooterMessage($"{duplicatedNodeModels.Count} node pasted");
        }

        private void DuplicateSelection() {
            var selectedNodeModels = _graphView.GetSelectedNodeModels();
            var duplicatedNodeModels = _assetModel.DuplicateNodes(selectedNodeModels, DuplicateOffset);
            _graphView.Rebuild(_assetModel);
            _graphView.SelectNodeModels(duplicatedNodeModels);
            UpdateInspectorSelection();
            SetFooterMessage($"{duplicatedNodeModels.Count} node duplicated");
        }

        private void DeleteSelection() {
            var selectedEdges = _graphView.GetSelectedEdgeConnections();
            for (var i = 0; i < selectedEdges.Count; i++) {
                _assetModel.Disconnect(selectedEdges[i].OutputPortKind, selectedEdges[i].SourceNodeModel, selectedEdges[i].TargetNodeModel);
            }

            var selectedNodeModels = _graphView.GetSelectedNodeModels();
            _assetModel.RemoveNodes(selectedNodeModels);
            RefreshGraph();
            SetFooterMessage("Selection removed");
        }

        private void RefreshGraph() {
            _assetModel.RefreshNodes();
            _schemaView.Refresh();
            _graphView.Rebuild(_assetModel);
            RefreshPreviewSchedule();
            RefreshInspectorSelection();
        }

        private void RefreshNodeDetails() {
            _graphView.RefreshNodeDetails();
        }

        private void RefreshPreviewSchedule() {
            if (_assetModel == null || !_assetModel.HasGraphAsset) {
                SetPreviewSchedule(null, null, AnimationGraphPlayerState.Stopped);
                return;
            }

            var previewRunner = FindPreviewRunner(_assetModel.GraphAsset);
            var previewSchedule = previewRunner == null ? null : previewRunner.Schedule;
            var previewState = previewRunner == null ? AnimationGraphPlayerState.Stopped : previewRunner.State;
            SetPreviewSchedule(previewRunner, previewSchedule, previewState);
        }

        private void RebuildPreviewSchedule() {
            if (_assetModel == null || !_assetModel.HasGraphAsset) {
                SetPreviewSchedule(null, null, AnimationGraphPlayerState.Stopped);
                return;
            }

            var previewRunner = FindPreviewRunner(_assetModel.GraphAsset);
            if (previewRunner == null) {
                SetPreviewSchedule(null, null, AnimationGraphPlayerState.Stopped);
                return;
            }

            previewRunner.RebuildSchedule();
            SetPreviewSchedule(previewRunner, previewRunner.Schedule, previewRunner.State);
        }

        private void SetPreviewSchedule(AnimationGraphRunner previewRunner, AnimationGraphSchedule previewSchedule, AnimationGraphPlayerState previewState) {
            if (_previewRunner == previewRunner && _previewSchedule == previewSchedule && _previewState == previewState) {
                return;
            }

            _previewRunner = previewRunner;
            _previewSchedule = previewSchedule;
            _previewState = previewState;
            if (_assetModel.SetPreviewSchedule(previewSchedule)) {
                _graphView.RefreshNodeDetails();
            }
        }

        private void SetInspectorSelection(IReadOnlyList<NodeEditorModel> nodeModels) {
            _inspectedNodeIds.Clear();
            for (var i = 0; i < nodeModels.Count; i++) {
                _inspectedNodeIds.Add(nodeModels[i].NodeId);
            }

            _inspectorView.SetSelection(nodeModels);
            NotifyInspectedNodeIdsChanged();
        }

        private void SetInspectorSelectionByIds(IReadOnlyList<string> nodeIds) {
            _inspectedNodeIds.Clear();
            if (nodeIds != null) {
                for (var i = 0; i < nodeIds.Count; i++) {
                    if (string.IsNullOrEmpty(nodeIds[i])) {
                        continue;
                    }

                    _inspectedNodeIds.Add(nodeIds[i]);
                }
            }

            RefreshInspectorSelection();
        }

        private void RefreshInspectorSelection() {
            var nodeModels = new List<NodeEditorModel>();
            for (var i = _inspectedNodeIds.Count - 1; i >= 0; i--) {
                if (!_assetModel.TryGetNode(_inspectedNodeIds[i], out var nodeModel)) {
                    _inspectedNodeIds.RemoveAt(i);
                    continue;
                }

                nodeModels.Insert(0, nodeModel);
            }

            _inspectorView.SetSelection(nodeModels);
            NotifyInspectedNodeIdsChanged();
        }

        private void ClearInspectorSelection() {
            _inspectedNodeIds.Clear();
            _inspectorView.SetSelection(Array.Empty<NodeEditorModel>());
            NotifyInspectedNodeIdsChanged();
        }

        private void NotifyInspectedNodeIdsChanged() {
            InspectedNodeIdsChanged?.Invoke(_inspectedNodeIds);
        }

        private void SetFooterMessage(string message) {
            _footerLabel.text = string.IsNullOrEmpty(message) ? string.Empty : message;
        }

        private static string GetDisplayValue(string value) {
            return string.IsNullOrEmpty(value) ? "-" : value;
        }

        private static string GetOutputPortName(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel) {
            return outputPortKind switch {
                AnimationGraphOutputPortKind.Next when typeof(BranchNode).IsAssignableFrom(sourceNodeModel.NodeType) => "True",
                AnimationGraphOutputPortKind.Next => "Next",
                AnimationGraphOutputPortKind.False => "False",
                AnimationGraphOutputPortKind.Loop => "Loop",
                _ => outputPortKind.ToString(),
            };
        }

        private static AnimationGraphRunner FindPreviewRunner(AnimationGraphAsset graphAsset) {
            if (graphAsset == null) {
                return null;
            }

            var pausedRunner = default(AnimationGraphRunner);
            var runners = UnityEngine.Object.FindObjectsByType<AnimationGraphRunner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < runners.Length; i++) {
                var runner = runners[i];
                if (runner == null || runner.GraphAsset != graphAsset || runner.Schedule == null || runner.State == AnimationGraphPlayerState.Stopped) {
                    continue;
                }

                if (runner.State == AnimationGraphPlayerState.Playing) {
                    return runner;
                }

                pausedRunner ??= runner;
            }

            return pausedRunner;
        }
    }
}
