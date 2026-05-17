using System;
using System.Collections.Generic;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// AnimationGraphEditorPresenter の選択操作
    /// </summary>
    internal sealed partial class AnimationGraphEditorPresenter {
        /// <summary>
        /// GraphView の選択状態を Inspector に反映
        /// </summary>
        private void UpdateInspectorSelection() {
            var selectedNodeModels = _graphView.GetSelectedNodeModels();
            if (selectedNodeModels.Count > 0) {
                SetInspectorSelection(selectedNodeModels);
                return;
            }

            var selectedSignalModels = _graphView.GetSelectedSignalModels();
            if (selectedSignalModels.Count > 0) {
                SetSignalInspectorSelection(selectedSignalModels);
                return;
            }

            RefreshInspectorSelection();
        }

        /// <summary>
        /// 選択中の Node と Signal を複製用に保持
        /// </summary>
        private void CopySelection() {
            _copiedNodeModels.Clear();
            _copiedSignalModels.Clear();
            var selectedNodeModels = _graphView.GetSelectedNodeModels();
            for (var i = 0; i < selectedNodeModels.Count; i++) {
                var nodeModel = selectedNodeModels[i];
                if (!_assetModel.CanDuplicateNode(nodeModel)) {
                    continue;
                }

                _copiedNodeModels.Add(nodeModel);
            }

            var selectedSignalModels = _graphView.GetSelectedSignalModels();
            for (var i = 0; i < selectedSignalModels.Count; i++) {
                var signalModel = selectedSignalModels[i];
                if (!_assetModel.CanDuplicateSignal(signalModel)) {
                    continue;
                }

                _copiedSignalModels.Add(signalModel);
            }

            SetFooterMessage($"{_copiedNodeModels.Count + _copiedSignalModels.Count} item copied");
        }

        /// <summary>
        /// 保持している Node と Signal を Graph に貼り付け
        /// </summary>
        private void PasteCopiedElements() {
            if (_copiedNodeModels.Count == 0 && _copiedSignalModels.Count == 0) {
                SetFooterMessage("No copied item", true);
                return;
            }

            var duplicatedNodeModels = _assetModel.DuplicateNodes(_copiedNodeModels, DuplicateOffset);
            var duplicatedSignalModels = _assetModel.DuplicateSignals(_copiedSignalModels, DuplicateOffset);
            _graphView.Rebuild(_assetModel);
            _graphView.SelectGraphElementModels(duplicatedNodeModels, duplicatedSignalModels);
            UpdateInspectorSelection();
            if (RefreshPreviewAfterGraphChanged()) {
                SetFooterMessage($"{duplicatedNodeModels.Count + duplicatedSignalModels.Count} item pasted");
            }
        }

        /// <summary>
        /// 選択中の Node と Signal を複製
        /// </summary>
        private void DuplicateSelection() {
            var selectedNodeModels = _graphView.GetSelectedNodeModels();
            var selectedSignalModels = _graphView.GetSelectedSignalModels();
            var duplicatedNodeModels = _assetModel.DuplicateNodes(selectedNodeModels, DuplicateOffset);
            var duplicatedSignalModels = _assetModel.DuplicateSignals(selectedSignalModels, DuplicateOffset);
            _graphView.Rebuild(_assetModel);
            _graphView.SelectGraphElementModels(duplicatedNodeModels, duplicatedSignalModels);
            UpdateInspectorSelection();
            if (RefreshPreviewAfterGraphChanged()) {
                SetFooterMessage($"{duplicatedNodeModels.Count + duplicatedSignalModels.Count} item duplicated");
            }
        }

        /// <summary>
        /// 選択中の edge、Node、Signal を削除
        /// </summary>
        private void DeleteSelection() {
            var selectedEdges = _graphView.GetSelectedEdgeConnections();
            for (var i = 0; i < selectedEdges.Count; i++) {
                _assetModel.Disconnect(selectedEdges[i].OutputPortKind, selectedEdges[i].SourceNodeModel, selectedEdges[i].TargetNodeModel);
            }

            var selectedSignalEdges = _graphView.GetSelectedSignalEdgeConnections();
            for (var i = 0; i < selectedSignalEdges.Count; i++) {
                _assetModel.DisconnectSignal(selectedSignalEdges[i].OutputPortKind, selectedSignalEdges[i].SourceNodeModel, selectedSignalEdges[i].TargetSignalModel);
            }

            var selectedNodeModels = _graphView.GetSelectedNodeModels();
            _assetModel.RemoveNodes(selectedNodeModels);
            var selectedSignalModels = _graphView.GetSelectedSignalModels();
            _assetModel.RemoveSignals(selectedSignalModels);
            if (RefreshGraphState()) {
                SetFooterMessage("Selection removed");
            }
        }

        /// <summary>
        /// Node の選択状態を Inspector と永続化用 ID に反映
        /// </summary>
        /// <param name="nodeModels">選択中の Node model 一覧</param>
        private void SetInspectorSelection(IReadOnlyList<NodeEditorModel> nodeModels) {
            _inspectedNodeIds.Clear();
            _inspectedSignalIds.Clear();
            for (var i = 0; i < nodeModels.Count; i++) {
                _inspectedNodeIds.Add(nodeModels[i].NodeId);
            }

            _inspectorView.SetSelection(nodeModels);
            NotifyInspectedNodeIdsChanged();
        }

        /// <summary>
        /// Signal の選択状態を Inspector と保持 ID に反映
        /// </summary>
        /// <param name="signalModels">選択中の Signal model 一覧</param>
        private void SetSignalInspectorSelection(IReadOnlyList<SignalEditorModel> signalModels) {
            _inspectedNodeIds.Clear();
            _inspectedSignalIds.Clear();
            for (var i = 0; i < signalModels.Count; i++) {
                _inspectedSignalIds.Add(signalModels[i].SignalId);
            }

            _inspectorView.SetSignalSelection(signalModels);
            NotifyInspectedNodeIdsChanged();
        }

        /// <summary>
        /// Node ID 一覧から Inspector 選択状態を復元
        /// </summary>
        /// <param name="nodeIds">復元する Node ID 一覧</param>
        private void SetInspectorSelectionByIds(IReadOnlyList<string> nodeIds) {
            _inspectedNodeIds.Clear();
            _inspectedSignalIds.Clear();
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

        /// <summary>
        /// 保持している ID から Inspector 選択状態を更新
        /// </summary>
        private void RefreshInspectorSelection() {
            if (_inspectedSignalIds.Count > 0) {
                RefreshSignalInspectorSelection();
                return;
            }

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

        /// <summary>
        /// 保持している Signal ID から Inspector 選択状態を更新
        /// </summary>
        private void RefreshSignalInspectorSelection() {
            var signalModels = new List<SignalEditorModel>();
            for (var i = _inspectedSignalIds.Count - 1; i >= 0; i--) {
                if (!TryGetSignalModel(_inspectedSignalIds[i], out var signalModel)) {
                    _inspectedSignalIds.RemoveAt(i);
                    continue;
                }

                signalModels.Insert(0, signalModel);
            }

            _inspectorView.SetSignalSelection(signalModels);
            NotifyInspectedNodeIdsChanged();
        }

        /// <summary>
        /// Signal ID に対応する Signal model の取得を試行
        /// </summary>
        /// <param name="signalId">取得する Signal ID</param>
        /// <param name="signalModel">取得した Signal model</param>
        /// <returns>取得できた場合は true</returns>
        private bool TryGetSignalModel(string signalId, out SignalEditorModel signalModel) {
            var signalModels = _assetModel.Signals;
            for (var i = 0; i < signalModels.Count; i++) {
                if (signalModels[i].SignalId != signalId) {
                    continue;
                }

                signalModel = signalModels[i];
                return true;
            }

            signalModel = null;
            return false;
        }

        /// <summary>
        /// Inspector 選択状態を解除
        /// </summary>
        private void ClearInspectorSelection() {
            _inspectedNodeIds.Clear();
            _inspectedSignalIds.Clear();
            _inspectorView.SetSelection(Array.Empty<NodeEditorModel>());
            NotifyInspectedNodeIdsChanged();
        }

        /// <summary>
        /// Inspector 表示対象 Node ID 一覧の変更を通知
        /// </summary>
        private void NotifyInspectedNodeIdsChanged() {
            InspectedNodeIdsChanged?.Invoke(_inspectedNodeIds);
        }
    }
}
