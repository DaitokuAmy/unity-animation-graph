namespace UnityAnimationGraph {
    /// <summary>
    /// Animation Graph シグナルの通知処理を行うインターフェース
    /// </summary>
    public interface ISignalExecutor {
        /// <summary>
        /// シグナルを通知
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        void Dispatch(int seed, IAnimationGraphContext context);
    }
}
