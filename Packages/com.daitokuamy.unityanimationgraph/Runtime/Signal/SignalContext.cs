namespace UnityAnimationGraph {
    /// <summary>
    /// Signal 発火通知を処理するハンドラー
    /// </summary>
    public interface ISignalHandler {
        /// <summary>
        /// Signal 発火通知を処理
        /// </summary>
        /// <param name="context">Signal 通知コンテキスト</param>
        void Handle(SignalContext context);
    }

    /// <summary>
    /// 指定した Signal 型の発火通知を処理するハンドラー
    /// </summary>
    /// <typeparam name="TSignal">処理対象の Signal 型</typeparam>
    public interface ISignalHandler<TSignal> where TSignal : Signal {
        /// <summary>
        /// Signal 発火通知を処理
        /// </summary>
        /// <param name="context">Signal 通知コンテキスト</param>
        void Handle(SignalContext<TSignal> context);
    }

    /// <summary>
    /// Signal 通知時に購読者へ渡すコンテキスト
    /// </summary>
    public readonly struct SignalContext {
        /// <summary>通知された Signal</summary>
        public Signal Signal { get; }
        /// <summary>通知時の評価コンテキスト</summary>
        public IAnimationGraphContext AnimationGraphContext { get; }

        /// <summary>
        /// SignalContext を作成
        /// </summary>
        /// <param name="signal">通知された Signal</param>
        /// <param name="animationGraphContext">通知時の AnimationGraphContext</param>
        public SignalContext(Signal signal, IAnimationGraphContext animationGraphContext) {
            Signal = signal;
            AnimationGraphContext = animationGraphContext;
        }
    }

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
