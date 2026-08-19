namespace UnityAnimationGraph {
    /// <summary>
    /// ActionNode state を使用してライフサイクルを実行する内部インターフェース
    /// </summary>
    internal interface IActionNodeStateExecutor {
        /// <summary>
        /// 実行状態を生成
        /// </summary>
        /// <returns>生成した実行状態。状態を使用しない場合は null</returns>
        IActionNodeState CreateState();

        /// <summary>
        /// ノード開始時の処理を行う
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        /// <param name="state">実行状態</param>
        void Enter(int seed, IAnimationGraphContext context, IActionNodeState state);

        /// <summary>
        /// ノードを評価
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="localTime">ノード開始時刻からの経過時間</param>
        /// <param name="calculatedDuration">計算済みの実行時間</param>
        /// <param name="context">評価コンテキスト</param>
        /// <param name="state">実行状態</param>
        void Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context, IActionNodeState state);

        /// <summary>
        /// ノード終了時の処理を行う
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        /// <param name="state">実行状態</param>
        void Exit(int seed, IAnimationGraphContext context, IActionNodeState state);

        /// <summary>
        /// 実行中のノードをキャンセル
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        /// <param name="state">実行状態</param>
        void Cancel(int seed, IAnimationGraphContext context, IActionNodeState state);
    }
}
