namespace UnityAnimationGraph {
    /// <summary>
    /// Animation Graph の評価に必要な Blackboard 値を提供するインターフェース
    /// </summary>
    public interface IAnimationGraphBlackboard {
        /// <summary>
        /// 指定したキーに対応する Blackboard 値の取得を試行
        /// </summary>
        /// <param name="key">Blackboard キー</param>
        /// <param name="value">取得した値</param>
        /// <typeparam name="T">取得する値の型</typeparam>
        /// <returns>取得できた場合は true</returns>
        bool TryGetBlackboardValue<T>(string key, out T value);
    }
}