namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// GraphView 上の output port 種別
    /// </summary>
    internal readonly struct AnimationGraphOutputPortKind : System.IEquatable<AnimationGraphOutputPortKind> {
        private enum PortCategory {
            Next,
            BranchExtension,
            Loop,
            EnterSignal,
            ExitSignal,
        }

        private readonly PortCategory _category;
        private readonly int _branchExtensionIndex;

        /// <summary>通常の後続ノード</summary>
        public static AnimationGraphOutputPortKind Next => new(PortCategory.Next, 0);
        /// <summary>LoopNode のループ内容ノード</summary>
        public static AnimationGraphOutputPortKind Loop => new(PortCategory.Loop, 0);
        /// <summary>Node Enter 時に通知する Signal</summary>
        public static AnimationGraphOutputPortKind EnterSignal => new(PortCategory.EnterSignal, 0);
        /// <summary>Node Exit 時に通知する Signal</summary>
        public static AnimationGraphOutputPortKind ExitSignal => new(PortCategory.ExitSignal, 0);
        /// <summary>通常の後続ノードの場合は true</summary>
        public bool IsNext => _category == PortCategory.Next;
        /// <summary>BranchNode の拡張 Port の場合は true</summary>
        public bool IsBranchExtension => _category == PortCategory.BranchExtension;
        /// <summary>LoopNode のループ内容 Port の場合は true</summary>
        public bool IsLoop => _category == PortCategory.Loop;
        /// <summary>Signal Port の場合は true</summary>
        public bool IsSignal => _category is PortCategory.EnterSignal or PortCategory.ExitSignal;
        /// <summary>BranchNode の拡張 Port index。拡張 Port でない場合は -1</summary>
        public int BranchExtensionIndex => IsBranchExtension ? _branchExtensionIndex : -1;

        private AnimationGraphOutputPortKind(PortCategory category, int branchExtensionIndex) {
            _category = category;
            _branchExtensionIndex = branchExtensionIndex;
        }

        /// <summary>
        /// BranchNode の拡張 Port を作成
        /// </summary>
        /// <param name="extensionIndex">拡張 Branch Port index</param>
        /// <returns>BranchNode の拡張 Port</returns>
        public static AnimationGraphOutputPortKind BranchExtension(int extensionIndex) {
            if (extensionIndex < 0) {
                throw new System.ArgumentOutOfRangeException(nameof(extensionIndex));
            }

            return new AnimationGraphOutputPortKind(PortCategory.BranchExtension, extensionIndex);
        }

        /// <inheritdoc/>
        public bool Equals(AnimationGraphOutputPortKind other) {
            return _category == other._category && _branchExtensionIndex == other._branchExtensionIndex;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            return obj is AnimationGraphOutputPortKind other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            unchecked {
                return ((int)_category * 397) ^ _branchExtensionIndex;
            }
        }

        /// <inheritdoc/>
        public override string ToString() {
            return IsBranchExtension ? $"BranchExtension({_branchExtensionIndex})" : _category.ToString();
        }

        /// <summary>
        /// 2 つの output port 種別が同じか判定
        /// </summary>
        /// <param name="left">比較する output port 種別</param>
        /// <param name="right">比較する output port 種別</param>
        /// <returns>同じ場合は true</returns>
        public static bool operator ==(AnimationGraphOutputPortKind left, AnimationGraphOutputPortKind right) {
            return left.Equals(right);
        }

        /// <summary>
        /// 2 つの output port 種別が異なるか判定
        /// </summary>
        /// <param name="left">比較する output port 種別</param>
        /// <param name="right">比較する output port 種別</param>
        /// <returns>異なる場合は true</returns>
        public static bool operator !=(AnimationGraphOutputPortKind left, AnimationGraphOutputPortKind right) {
            return !left.Equals(right);
        }
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

    /// <summary>
    /// GraphView 上の Edge が表す NodeEditorModel から SignalEditorModel への接続
    /// </summary>
    internal readonly struct AnimationGraphSignalEdgeConnection {
        /// <summary>接続元 output port 種別</summary>
        public AnimationGraphOutputPortKind OutputPortKind { get; }
        /// <summary>接続元ノード Model</summary>
        public NodeEditorModel SourceNodeModel { get; }
        /// <summary>接続先 Signal Model</summary>
        public SignalEditorModel TargetSignalModel { get; }

        /// <summary>
        /// AnimationGraphSignalEdgeConnection を作成
        /// </summary>
        /// <param name="outputPortKind">接続元 output port 種別</param>
        /// <param name="sourceNodeModel">接続元ノード Model</param>
        /// <param name="targetSignalModel">接続先 Signal Model</param>
        public AnimationGraphSignalEdgeConnection(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, SignalEditorModel targetSignalModel) {
            OutputPortKind = outputPortKind;
            SourceNodeModel = sourceNodeModel;
            TargetSignalModel = targetSignalModel;
        }
    }
}
