using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Vector4 の Before/After と補間設定
    /// </summary>
    [Serializable]
    public class Vector4Tween : Tween {
        [SerializeField, Tooltip("Tween 開始時の値")]
        private Vector4Parameter _before = new(Vector4.zero);
        [SerializeField, Tooltip("Tween 終了時の値")]
        private Vector4Parameter _after = new(Vector4.one);
        [SerializeField, Tooltip("更新しない要素")]
        private Vector4IgnoreMask _ignoreMask;

        /// <summary>Tween 開始時の値</summary>
        public Vector4Parameter Before => _before;
        /// <summary>Tween 終了時の値</summary>
        public Vector4Parameter After => _after;
        /// <summary>更新しない要素</summary>
        public Vector4IgnoreMask IgnoreMask => _ignoreMask;

        /// <summary>
        /// Vector4Tween を生成
        /// </summary>
        public Vector4Tween() {
        }

        /// <summary>
        /// Vector4Tween を生成
        /// </summary>
        /// <param name="before">Tween 開始時の値</param>
        /// <param name="after">Tween 終了時の値</param>
        public Vector4Tween(Vector4 before, Vector4 after) {
            _before = new Vector4Parameter(before);
            _after = new Vector4Parameter(after);
        }

        /// <summary>
        /// 指定時刻の補間値の取得を試行
        /// </summary>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <param name="localTime">ノード開始時刻からの経過時間</param>
        /// <param name="calculatedDuration">計算済みの実行時間</param>
        /// <param name="value">補間値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryEvaluate(IAnimationGraphBlackboard blackboard, float localTime, float calculatedDuration, out Vector4 value) {
            return TryEvaluate(blackboard, default, localTime, calculatedDuration, out value);
        }

        /// <summary>
        /// 指定時刻の補間値の取得を試行
        /// </summary>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <param name="baseValue">相対値の基準値</param>
        /// <param name="localTime">ノード開始時刻からの経過時間</param>
        /// <param name="calculatedDuration">計算済みの実行時間</param>
        /// <param name="value">補間値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryEvaluate(IAnimationGraphBlackboard blackboard, Vector4 baseValue, float localTime, float calculatedDuration, out Vector4 value) {
            if (!_before.TryGetValue(blackboard, baseValue, out var before) || !_after.TryGetValue(blackboard, baseValue, out var after)) {
                value = default;
                return false;
            }

            value = Vector4.LerpUnclamped(before, after, EvaluateProgress(localTime, calculatedDuration));
            return true;
        }

        /// <summary>
        /// IgnoreMask に含まれる要素を現在値で維持
        /// </summary>
        /// <param name="value">Tween 評価後の値</param>
        /// <param name="currentValue">現在値</param>
        /// <returns>IgnoreMask を反映した値</returns>
        public Vector4 ApplyIgnoreMask(Vector4 value, Vector4 currentValue) {
            if ((_ignoreMask & Vector4IgnoreMask.X) != 0) {
                value.x = currentValue.x;
            }

            if ((_ignoreMask & Vector4IgnoreMask.Y) != 0) {
                value.y = currentValue.y;
            }

            if ((_ignoreMask & Vector4IgnoreMask.Z) != 0) {
                value.z = currentValue.z;
            }

            if ((_ignoreMask & Vector4IgnoreMask.W) != 0) {
                value.w = currentValue.w;
            }

            return value;
        }
    }
}
