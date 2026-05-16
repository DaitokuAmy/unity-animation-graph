using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Animation Graph の評価に必要なターゲットと Blackboard 値を提供するコンテキスト
    /// </summary>
    public interface IAnimationGraphContext : IAnimationGraphBlackboard {
        /// <summary>
        /// 指定したキーに対応するターゲットを取得
        /// </summary>
        /// <param name="key">ターゲットキー</param>
        /// <typeparam name="T">取得する Component の型</typeparam>
        /// <returns>指定したキーに対応するターゲット</returns>
        T GetTarget<T>(string key) where T : Component;

        /// <summary>
        /// 指定したキーに対応するターゲットの取得を試行
        /// </summary>
        /// <param name="key">ターゲットキー</param>
        /// <param name="target">取得したターゲット</param>
        /// <typeparam name="T">取得する Component の型</typeparam>
        /// <returns>取得できた場合は true</returns>
        bool TryGetTarget<T>(string key, out T target) where T : Component;
    }
}