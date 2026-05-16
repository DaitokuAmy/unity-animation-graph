namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// GraphView 上の output port 種別
    /// </summary>
    internal enum AnimationGraphOutputPortKind {
        /// <summary>通常の後続ノード</summary>
        Next,
        /// <summary>BranchNode の false 側後続ノード</summary>
        False,
        /// <summary>LoopNode のループ内容ノード</summary>
        Loop,
    }

    /// <summary>
    /// GraphView 上の Edge が表す NodeEditorModel 同士の接続
    /// </summary>
    internal readonly struct AnimationGraphEdgeConnection {
        /// <summary>接続元 output port 種別</summary>
        public AnimationGraphOutputPortKind OutputPortKind { get; }
        /// <summary>接続元ノード Model</summary>
        public NodeEditorModel SourceNodeModel { get; }
        /// <summary>接続先ノード Model</summary>
        public NodeEditorModel TargetNodeModel { get; }

        /// <summary>
        /// AnimationGraphEdgeConnection を作成
        /// </summary>
        /// <param name="sourceNodeModel">接続元ノード Model</param>
        /// <param name="targetNodeModel">接続先ノード Model</param>
        public AnimationGraphEdgeConnection(NodeEditorModel sourceNodeModel, NodeEditorModel targetNodeModel) : this(AnimationGraphOutputPortKind.Next, sourceNodeModel, targetNodeModel) {
        }

        /// <summary>
        /// AnimationGraphEdgeConnection を作成
        /// </summary>
        /// <param name="outputPortKind">接続元 output port 種別</param>
        /// <param name="sourceNodeModel">接続元ノード Model</param>
        /// <param name="targetNodeModel">接続先ノード Model</param>
        public AnimationGraphEdgeConnection(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, NodeEditorModel targetNodeModel) {
            OutputPortKind = outputPortKind;
            SourceNodeModel = sourceNodeModel;
            TargetNodeModel = targetNodeModel;
        }
    }
}
