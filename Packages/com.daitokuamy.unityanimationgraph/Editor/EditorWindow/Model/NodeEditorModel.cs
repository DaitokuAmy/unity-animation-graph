using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Preview 中の Node 実行状態
    /// </summary>
    internal enum NodePreviewExecutionState {
        /// <summary>未実行</summary>
        None,
        /// <summary>実行完了済み</summary>
        Completed,
        /// <summary>実行中</summary>
        Active,
    }

    /// <summary>
    /// Preview 中の Node 実行情報
    /// </summary>
    internal readonly struct NodePreviewExecutionInfo : IEquatable<NodePreviewExecutionInfo> {
        /// <summary>Preview 中の実行状態</summary>
        public NodePreviewExecutionState State { get; }
        /// <summary>Preview 中の進捗率</summary>
        public float Progress { get; }

        /// <summary>
        /// NodePreviewExecutionInfo を作成
        /// </summary>
        /// <param name="state">Preview 中の実行状態</param>
        /// <param name="progress">Preview 中の進捗率</param>
        public NodePreviewExecutionInfo(NodePreviewExecutionState state, float progress) {
            State = state;
            Progress = Mathf.Clamp01(progress);
        }

        /// <inheritdoc/>
        public bool Equals(NodePreviewExecutionInfo other) {
            return State == other.State && Progress.Equals(other.Progress);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            return obj is NodePreviewExecutionInfo other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            unchecked {
                return ((int)State * 397) ^ Progress.GetHashCode();
            }
        }
    }

    /// <summary>
    /// Editor MVP の Model として Node の参照情報を提供するクラス
    /// </summary>
    public class NodeEditorModel {
        private readonly Node _node;
        private NodePreviewExecutionInfo _previewExecutionInfo;
        private bool _hasPreviewExecutionInfo;

        /// <summary>グラフ内で一意なノード ID</summary>
        public string NodeId => _node.NodeId;
        /// <summary>ノード型</summary>
        public Type NodeType => _node.GetType();
        /// <summary>ノード表示名</summary>
        public string DisplayName => _node.DisplayName;
        /// <summary>ノード名</summary>
        public string Name => _node.name ?? string.Empty;
        /// <summary>エディタ上のノード位置</summary>
        public Vector2 GraphPosition => _node.GraphPosition;
        /// <summary>後続ノード ID の一覧</summary>
        public IReadOnlyList<string> NextNodeIds => _node.NextNodeIds;
        /// <summary>Enter シグナル用の出力 Port を表示する場合は true</summary>
        public bool EnableEnterSignalPort => _node.EnableEnterSignalPort;
        /// <summary>Exit シグナル用の出力 Port を表示する場合は true</summary>
        public bool EnableExitSignalPort => _node.EnableExitSignalPort;
        /// <summary>Enter 時に通知する Signal 一覧</summary>
        public IReadOnlyList<Signal> EnterSignals => _node.EnterSignals;
        /// <summary>Exit 時に通知する Signal 一覧</summary>
        public IReadOnlyList<Signal> ExitSignals => _node.ExitSignals;
        /// <summary>参照元の Node</summary>
        internal Node Node => _node;

        /// <summary>
        /// NodeEditorModel を作成
        /// </summary>
        /// <param name="node">参照する Node</param>
        protected internal NodeEditorModel(Node node) {
            _node = node ?? throw new ArgumentNullException(nameof(node));
        }

        /// <summary>
        /// Node に対応する NodeEditorModel を作成
        /// </summary>
        /// <param name="node">参照する Node</param>
        /// <returns>Node に対応する NodeEditorModel</returns>
        internal static NodeEditorModel Create(Node node) {
            return node switch {
                BranchNode branchNode => new BranchNodeEditorModel(branchNode),
                LoopNode loopNode => new LoopNodeEditorModel(loopNode),
                DelayNode delayNode => new DelayNodeEditorModel(delayNode),
                _ => new NodeEditorModel(node),
            };
        }

        /// <summary>
        /// エディタ上のノード位置を設定
        /// </summary>
        /// <param name="graphPosition">エディタ上のノード位置</param>
        public void SetGraphPosition(Vector2 graphPosition) {
            AnimationGraphAssetUtility.SetNodeGraphPosition(_node, graphPosition);
        }

        /// <summary>
        /// Preview 中の実行情報取得を試行
        /// </summary>
        /// <param name="previewExecutionInfo">取得した実行情報</param>
        /// <returns>実行情報を取得できた場合は true</returns>
        internal bool TryGetPreviewExecutionInfo(out NodePreviewExecutionInfo previewExecutionInfo) {
            previewExecutionInfo = _previewExecutionInfo;
            return _hasPreviewExecutionInfo;
        }

        /// <summary>
        /// 後続ノード ID 一覧を設定
        /// </summary>
        /// <param name="nextNodeIds">設定する後続ノード ID 一覧</param>
        internal void SetNextNodeIds(IReadOnlyList<string> nextNodeIds) {
            AnimationGraphAssetUtility.SetNodeNextNodeIds(_node, nextNodeIds);
        }

        /// <summary>
        /// Enter シグナル用の出力 Port 表示フラグを設定
        /// </summary>
        /// <param name="enabled">表示する場合は true</param>
        internal void SetEnterSignalPortEnabled(bool enabled) {
            AnimationGraphAssetUtility.SetNodeEnterSignalPortEnabled(_node, enabled);
        }

        /// <summary>
        /// Exit シグナル用の出力 Port 表示フラグを設定
        /// </summary>
        /// <param name="enabled">表示する場合は true</param>
        internal void SetExitSignalPortEnabled(bool enabled) {
            AnimationGraphAssetUtility.SetNodeExitSignalPortEnabled(_node, enabled);
        }

        /// <summary>
        /// Sets the target key when this model wraps an ActionNode.
        /// </summary>
        /// <param name="targetKey">Target key to set</param>
        internal void SetActionTargetKey(string targetKey) {
            if (_node is not ActionNode actionNode) {
                return;
            }

            AnimationGraphAssetUtility.SetActionNodeTargetKey(actionNode, targetKey);
        }

        /// <summary>
        /// Sets the flag key when this model wraps a FlagBranchNode.
        /// </summary>
        /// <param name="flagKey">Flag key to set</param>
        internal void SetFlagBranchKey(string flagKey) {
            if (_node is not FlagBranchNode flagBranchNode) {
                return;
            }

            AnimationGraphAssetUtility.SetFlagBranchNodeFlagKey(flagBranchNode, flagKey);
        }

        /// <summary>
        /// Sets the join type when this model wraps a JoinNode.
        /// </summary>
        /// <param name="joinType">Join type to set</param>
        internal void SetJoinType(JoinType joinType) {
            if (_node is not JoinNode joinNode) {
                return;
            }

            AnimationGraphAssetUtility.SetJoinNodeJoinType(joinNode, joinType);
        }

        /// <summary>
        /// Preview 中の実行情報を設定
        /// </summary>
        /// <param name="previewExecutionInfo">設定する実行情報</param>
        /// <returns>表示情報が変化した場合は true</returns>
        internal bool SetPreviewExecutionInfo(NodePreviewExecutionInfo previewExecutionInfo) {
            if (_hasPreviewExecutionInfo && _previewExecutionInfo.Equals(previewExecutionInfo)) {
                return false;
            }

            _previewExecutionInfo = previewExecutionInfo;
            _hasPreviewExecutionInfo = true;
            return true;
        }

        /// <summary>
        /// Preview 中の実行情報を消去
        /// </summary>
        /// <returns>表示情報が変化した場合は true</returns>
        internal bool ClearPreviewExecutionInfo() {
            if (!_hasPreviewExecutionInfo) {
                return false;
            }

            _previewExecutionInfo = default;
            _hasPreviewExecutionInfo = false;
            return true;
        }
    }
}
