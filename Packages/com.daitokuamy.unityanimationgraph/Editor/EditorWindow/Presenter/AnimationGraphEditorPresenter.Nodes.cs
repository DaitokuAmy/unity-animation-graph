using System;
using System.Globalization;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// AnimationGraphEditorPresenter の Node 操作
    /// </summary>
    internal sealed partial class AnimationGraphEditorPresenter {
        /// <summary>
        /// 指定 type の Node を Graph に追加
        /// </summary>
        /// <param name="nodeType">追加する Node type</param>
        /// <param name="graphPosition">Graph 上の追加位置</param>
        private void AddNode(Type nodeType, Vector2 graphPosition) {
            if (!_assetModel.HasGraphAsset) {
                SetFooterMessage("GraphAsset is not selected", true);
                return;
            }

            var nodeModel = _assetModel.AddNode(nodeType, graphPosition);
            _graphView.Rebuild(_assetModel);
            _graphView.SelectNodeModels(new[] { nodeModel });
            UpdateInspectorSelection();
            if (RefreshPreviewAfterGraphChanged()) {
                SetFooterMessage($"{nodeModel.DisplayName} added");
            }
        }

        /// <summary>
        /// Node の Graph 上の表示位置を更新
        /// </summary>
        /// <param name="nodeModel">移動対象 Node model</param>
        /// <param name="nodePosition">移動後の表示範囲</param>
        private void MoveNode(NodeEditorModel nodeModel, Rect nodePosition) {
            nodeModel.SetGraphPosition(nodePosition.position);
        }

        /// <summary>
        /// ActionNode の Target key を更新
        /// </summary>
        /// <param name="nodeModel">更新対象 Node model</param>
        /// <param name="targetKey">Target key</param>
        private void SetActionTargetKey(NodeEditorModel nodeModel, string targetKey) {
            if (nodeModel == null) {
                return;
            }

            nodeModel.SetActionTargetKey(targetKey);
            var previewUpdated = RefreshPreviewAfterGraphChanged();
            RefreshNodeDetails();
            RefreshInspectorSelection();
            if (previewUpdated) {
                SetFooterMessage($"{nodeModel.DisplayName} target: {GetDisplayValue(targetKey)}");
            }
        }

        /// <summary>
        /// DelayNode の待機時間を更新
        /// </summary>
        /// <param name="nodeModel">更新対象 DelayNode model</param>
        /// <param name="delay">待機時間</param>
        private void SetDelay(DelayNodeEditorModel nodeModel, float delay) {
            if (nodeModel == null) {
                return;
            }

            nodeModel.SetDelay(delay);
            var previewUpdated = RefreshPreviewAfterGraphChanged();
            RefreshNodeDetails();
            RefreshInspectorSelection();
            if (previewUpdated) {
                SetFooterMessage($"{nodeModel.DisplayName} delay: {nodeModel.Delay:0.###}");
            }
        }

        /// <summary>
        /// Node 詳細 field の string 値を更新
        /// </summary>
        /// <param name="nodeModel">更新対象 Node model</param>
        /// <param name="field">更新対象 field</param>
        /// <param name="value">設定する値</param>
        private void SetNodeDetailString(NodeEditorModel nodeModel, NodeDetailField field, string value) {
            if (nodeModel == null) {
                return;
            }

            nodeModel.SetDetailFieldValue(field, value);
            RefreshAfterNodeDetailFieldChanged(nodeModel, field, GetDisplayValue(value));
        }

        /// <summary>
        /// Node 詳細 field の bool 値を更新
        /// </summary>
        /// <param name="nodeModel">更新対象 Node model</param>
        /// <param name="field">更新対象 field</param>
        /// <param name="value">設定する値</param>
        private void SetNodeDetailBool(NodeEditorModel nodeModel, NodeDetailField field, bool value) {
            if (nodeModel == null) {
                return;
            }

            nodeModel.SetDetailFieldValue(field, value);
            RefreshAfterNodeDetailFieldChanged(nodeModel, field, value ? "True" : "False");
        }

        /// <summary>
        /// Node 詳細 field の int 値を更新
        /// </summary>
        /// <param name="nodeModel">更新対象 Node model</param>
        /// <param name="field">更新対象 field</param>
        /// <param name="value">設定する値</param>
        private void SetNodeDetailInt(NodeEditorModel nodeModel, NodeDetailField field, int value) {
            if (nodeModel == null) {
                return;
            }

            nodeModel.SetDetailFieldValue(field, value);
            RefreshAfterNodeDetailFieldChanged(nodeModel, field, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Node 詳細 field の float 値を更新
        /// </summary>
        /// <param name="nodeModel">更新対象 Node model</param>
        /// <param name="field">更新対象 field</param>
        /// <param name="value">設定する値</param>
        private void SetNodeDetailFloat(NodeEditorModel nodeModel, NodeDetailField field, float value) {
            if (nodeModel == null) {
                return;
            }

            nodeModel.SetDetailFieldValue(field, value);
            RefreshAfterNodeDetailFieldChanged(nodeModel, field, value.ToString("0.###", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// JoinNode の join type を更新
        /// </summary>
        /// <param name="nodeModel">更新対象 Node model</param>
        /// <param name="joinType">Join type</param>
        private void SetJoinType(NodeEditorModel nodeModel, JoinType joinType) {
            if (nodeModel == null) {
                return;
            }

            nodeModel.SetJoinType(joinType);
            var previewUpdated = RefreshPreviewAfterGraphChanged();
            RefreshNodeDetails();
            RefreshInspectorSelection();
            if (previewUpdated) {
                SetFooterMessage($"{nodeModel.DisplayName} join: {joinType}");
            }
        }

        /// <summary>
        /// LoopNode の繰り返し回数を更新
        /// </summary>
        /// <param name="nodeModel">更新対象 LoopNode model</param>
        /// <param name="loopCount">繰り返し回数</param>
        private void SetLoopCount(LoopNodeEditorModel nodeModel, int loopCount) {
            if (nodeModel == null) {
                return;
            }

            nodeModel.SetLoopCount(loopCount);
            var previewUpdated = RefreshPreviewAfterGraphChanged();
            RefreshNodeDetails();
            RefreshInspectorSelection();
            if (previewUpdated) {
                SetFooterMessage($"{nodeModel.DisplayName} count: {nodeModel.LoopCount}");
            }
        }

        private void RefreshAfterNodeDetailFieldChanged(NodeEditorModel nodeModel, NodeDetailField field, string displayValue) {
            var previewUpdated = RefreshPreviewAfterGraphChanged();
            RefreshNodeDetails();
            RefreshInspectorSelection();
            if (previewUpdated) {
                SetFooterMessage($"{nodeModel.DisplayName} {field.Label}: {displayValue}");
            }
        }

        /// <summary>
        /// Node 同士を接続
        /// </summary>
        /// <param name="outputPortKind">接続元 output port</param>
        /// <param name="sourceNodeModel">接続元 Node model</param>
        /// <param name="targetNodeModel">接続先 Node model</param>
        /// <returns>接続できた場合は true</returns>
        private bool ConnectNodes(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, NodeEditorModel targetNodeModel) {
            if (!_assetModel.Connect(outputPortKind, sourceNodeModel, targetNodeModel, out var errorMessage)) {
                SetFooterMessage(errorMessage, true);
                return false;
            }

            if (RefreshPreviewAfterGraphChanged()) {
                SetFooterMessage($"{sourceNodeModel.DisplayName}.{GetOutputPortName(outputPortKind, sourceNodeModel)} -> {targetNodeModel.DisplayName}");
            }

            return true;
        }

        /// <summary>
        /// Node 同士の接続を解除
        /// </summary>
        /// <param name="outputPortKind">接続元 output port</param>
        /// <param name="sourceNodeModel">接続元 Node model</param>
        /// <param name="targetNodeModel">接続先 Node model</param>
        private void DisconnectNodes(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, NodeEditorModel targetNodeModel) {
            if (_assetModel.Disconnect(outputPortKind, sourceNodeModel, targetNodeModel)) {
                if (RefreshPreviewAfterGraphChanged()) {
                    SetFooterMessage($"{sourceNodeModel.DisplayName} disconnected");
                }
            }
        }

        /// <summary>
        /// Node の詳細表示を更新
        /// </summary>
        private void RefreshNodeDetails() {
            _graphView.RefreshNodeDetails();
        }

        /// <summary>
        /// 空文字を表示用の代替文字列に変換
        /// </summary>
        /// <param name="value">表示する値</param>
        /// <returns>表示用文字列</returns>
        private string GetDisplayValue(string value) {
            return string.IsNullOrEmpty(value) ? "-" : value;
        }

        /// <summary>
        /// Output port の表示名を取得
        /// </summary>
        /// <param name="outputPortKind">Output port kind</param>
        /// <param name="sourceNodeModel">接続元 Node model</param>
        /// <returns>Output port の表示名</returns>
        private string GetOutputPortName(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel) {
            if (outputPortKind.IsNext) {
                return sourceNodeModel is BranchNodeEditorModel primaryBranchNodeModel ? primaryBranchNodeModel.PrimaryPortName : "Next";
            }

            if (outputPortKind.IsBranchExtension && sourceNodeModel is BranchNodeEditorModel extensionBranchNodeModel) {
                return extensionBranchNodeModel.GetExtensionPortName(outputPortKind.BranchExtensionIndex);
            }

            if (outputPortKind == AnimationGraphOutputPortKind.Loop) {
                return "Loop";
            }

            if (outputPortKind == AnimationGraphOutputPortKind.EnterSignal) {
                return "Enter";
            }

            return outputPortKind == AnimationGraphOutputPortKind.ExitSignal ? "Exit" : outputPortKind.ToString();
        }
    }
}
