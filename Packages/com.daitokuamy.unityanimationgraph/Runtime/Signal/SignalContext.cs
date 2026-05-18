namespace UnityAnimationGraph {
    /// <summary>
    /// Signal 通知時に購読者へ渡すコンテキスト
    /// </summary>
    /// <typeparam name="TSignal">通知された Signal 型</typeparam>
    public readonly struct SignalContext<TSignal> where TSignal : Signal {
        /// <summary>通知された Signal</summary>
        public TSignal Signal { get; }
        /// <summary>通知時の評価コンテキスト</summary>
        public IAnimationGraphContext AnimationGraphContext { get; }

        /// <summary>
        /// SignalContext を作成
        /// </summary>
        /// <param name="signal">通知された Signal</param>
        /// <param name="animationGraphContext">通知時の AnimationGraphContext</param>
        public SignalContext(TSignal signal, IAnimationGraphContext animationGraphContext) {
            Signal = signal;
            AnimationGraphContext = animationGraphContext;
        }
    }
}
