using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Animation Graph の評価に必要な Blackboard 値を提供するインターフェース
    /// </summary>
    public interface IAnimationGraphBlackboard {
        /// <summary>
        /// 指定したキーに対応する bool Blackboard 値の取得を試行
        /// </summary>
        /// <param name="key">Blackboard キー</param>
        /// <param name="value">取得した bool 値</param>
        /// <returns>取得できた場合は true</returns>
        bool TryGetBlackboardValue(string key, out bool value);

        /// <summary>
        /// 指定したキーに対応する int Blackboard 値の取得を試行
        /// </summary>
        /// <param name="key">Blackboard キー</param>
        /// <param name="value">取得した int 値</param>
        /// <returns>取得できた場合は true</returns>
        bool TryGetBlackboardValue(string key, out int value);

        /// <summary>
        /// 指定したキーに対応する float Blackboard 値の取得を試行
        /// </summary>
        /// <param name="key">Blackboard キー</param>
        /// <param name="value">取得した float 値</param>
        /// <returns>取得できた場合は true</returns>
        bool TryGetBlackboardValue(string key, out float value);

        /// <summary>
        /// 指定したキーに対応する string Blackboard 値の取得を試行
        /// </summary>
        /// <param name="key">Blackboard キー</param>
        /// <param name="value">取得した string 値</param>
        /// <returns>取得できた場合は true</returns>
        bool TryGetBlackboardValue(string key, out string value);

        /// <summary>
        /// 指定したキーに対応する Vector2 Blackboard 値の取得を試行
        /// </summary>
        /// <param name="key">Blackboard キー</param>
        /// <param name="value">取得した Vector2 値</param>
        /// <returns>取得できた場合は true</returns>
        bool TryGetBlackboardValue(string key, out Vector2 value);

        /// <summary>
        /// 指定したキーに対応する Vector3 Blackboard 値の取得を試行
        /// </summary>
        /// <param name="key">Blackboard キー</param>
        /// <param name="value">取得した Vector3 値</param>
        /// <returns>取得できた場合は true</returns>
        bool TryGetBlackboardValue(string key, out Vector3 value);

        /// <summary>
        /// 指定したキーに対応する Color Blackboard 値の取得を試行
        /// </summary>
        /// <param name="key">Blackboard キー</param>
        /// <param name="value">取得した Color 値</param>
        /// <returns>取得できた場合は true</returns>
        bool TryGetBlackboardValue(string key, out Color value);

        /// <summary>
        /// 指定したキーに対応する Vector4 Blackboard 値の取得を試行
        /// </summary>
        /// <param name="key">Blackboard キー</param>
        /// <param name="value">取得した Vector4 値</param>
        /// <returns>取得できた場合は true</returns>
        bool TryGetBlackboardValue(string key, out Vector4 value);
    }
}
