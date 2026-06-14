using System;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// AnimationGraphEditorPresenter の Signal 操作
    /// </summary>
    internal sealed partial class AnimationGraphEditorPresenter {
        /// <summary>
        /// Signal の Graph 上の表示位置を更新
        /// </summary>
        /// <param name="signalModel">移動対象 Signal model</param>
        /// <param name="signalPosition">移動後の表示範囲</param>
        private void MoveSignal(SignalEditorModel signalModel, Rect signalPosition) {
            if (!EnsureGraphEditingAllowed()) {
                return;
            }

            signalModel.SetGraphPosition(signalPosition.position);
        }

        /// <summary>
        /// Node の Signal output を Signal に接続
        /// </summary>
        /// <param name="outputPortKind">接続元 output port</param>
        /// <param name="sourceNodeModel">接続元 Node model</param>
        /// <param name="targetSignalModel">接続先 Signal model</param>
        /// <returns>接続できた場合は true</returns>
        private bool ConnectSignal(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, SignalEditorModel targetSignalModel) {
            if (!EnsureGraphEditingAllowed()) {
                return false;
            }

            if (!_assetModel.ConnectSignal(outputPortKind, sourceNodeModel, targetSignalModel, out var errorMessage)) {
                SetFooterMessage(errorMessage, true);
                return false;
            }

            if (RefreshPreviewAfterGraphChanged()) {
                SetFooterMessage($"{sourceNodeModel.DisplayName}.{GetOutputPortName(outputPortKind, sourceNodeModel)} -> {targetSignalModel.DisplayName}");
            }

            return true;
        }

        /// <summary>
        /// Node の Signal output と Signal の接続を解除
        /// </summary>
        /// <param name="outputPortKind">接続元 output port</param>
        /// <param name="sourceNodeModel">接続元 Node model</param>
        /// <param name="targetSignalModel">接続先 Signal model</param>
        private void DisconnectSignal(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, SignalEditorModel targetSignalModel) {
            if (!EnsureGraphEditingAllowed()) {
                return;
            }

            if (!_assetModel.DisconnectSignal(outputPortKind, sourceNodeModel, targetSignalModel)) {
                return;
            }

            if (RefreshPreviewAfterGraphChanged()) {
                SetFooterMessage($"{targetSignalModel.DisplayName} removed");
            }

            EditorApplication.delayCall += RefreshGraph;
        }

        /// <summary>
        /// 指定 type の Signal を Graph に追加
        /// </summary>
        /// <param name="signalType">追加する Signal type</param>
        /// <param name="graphPosition">Graph 上の追加位置</param>
        private void AddSignal(Type signalType, Vector2 graphPosition) {
            if (!EnsureGraphEditingAllowed()) {
                return;
            }

            if (!_assetModel.HasGraphAsset) {
                SetFooterMessage("GraphAsset is not selected", true);
                return;
            }

            try {
                var signalModel = _assetModel.AddSignal(signalType, graphPosition);
                if (RefreshGraphState()) {
                    SetFooterMessage($"Signal added: {signalModel.DisplayName}");
                }
            }
            catch (Exception exception) {
                SetFooterMessage(exception.Message, true);
            }
        }
    }
}
