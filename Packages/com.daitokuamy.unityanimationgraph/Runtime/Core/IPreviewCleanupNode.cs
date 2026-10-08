namespace UnityAnimationGraph {
    /// <summary>
    /// Editor Preview 終了時に一時状態を破棄する Node
    /// </summary>
    public interface IPreviewCleanupNode {
        /// <summary>
        /// Editor Preview で変更した一時状態を破棄
        /// </summary>
        /// <param name="context">評価コンテキスト</param>
        void CleanupPreview(IAnimationGraphContext context);
    }
}
