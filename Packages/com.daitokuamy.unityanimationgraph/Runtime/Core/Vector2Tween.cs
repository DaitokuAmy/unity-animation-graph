using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Vector2 の Before/After と補間設定
    /// </summary>
    [Serializable]
    public class Vector2Tween : Tween {
        [SerializeField, Tooltip("Tween 開始時の値")]
        private Vector2Parameter _before = new(Vector2.zero);
        [SerializeField, Tooltip("Tween 終了時の値")]
        private Vector2Parameter _after = new(Vector2.one);
        [SerializeField, Tooltip("更新しない要素")]
        private Vector2IgnoreMask _ignoreMask;

        /// <summary>Tween 開始時の値</summary>
        public Vector2Parameter Before => _before;
        /// <summary>Tween 終了時の値</summary>
        public Vector2Parameter After => _after;
        /// <summary>更新しない要素</summary>
        public Vector2IgnoreMask IgnoreMask => _ignoreMask;

        /// <summary>
        /// Vector2Tween を生成
        /// </summary>
        public Vector2Tween() {
        }

        /// <summary>
        /// Vector2Tween を生成
        /// </summary>
        /// <param name="before">Tween 開始時の値</param>
        /// <param name="after">Tween 終了時の値</param>
        public Vector2Tween(Vector2 before, Vector2 after) {
            _before = new Vector2Parameter(before);
            _after = new Vector2Parameter(after);
        }

        /// <summary>
        /// 指定時刻の補間値の取得を試行
        /// </summary>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <param name="localTime">ノード開始時刻からの経過時間</param>
        /// <param name="calculatedDuration">計算済みの実行時間</param>
        /// <param name="value">補間値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryEvaluate(IAnimationGraphBlackboard blackboard, float localTime, float calculatedDuration, out Vector2 value) {
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
        public bool TryEvaluate(IAnimationGraphBlackboard blackboard, Vector2 baseValue, float localTime, float calculatedDuration, out Vector2 value) {
            if (!_before.TryGetValue(blackboard, baseValue, out Vector2 before) || !_after.TryGetValue(blackboard, baseValue, out Vector2 after)) {
                value = default;
                return false;
            }

            value = Vector2.LerpUnclamped(before, after, EvaluateProgress(localTime, calculatedDuration));
            return true;
        }

        /// <summary>
        /// IgnoreMask に含まれる要素を現在値で維持
        /// </summary>
        /// <param name="value">Tween 評価後の値</param>
        /// <param name="currentValue">現在値</param>
        /// <returns>IgnoreMask を反映した値</returns>
        public Vector2 ApplyIgnoreMask(Vector2 value, Vector2 currentValue) {
            if ((_ignoreMask & Vector2IgnoreMask.X) != 0) {
                value.x = currentValue.x;
            }

            if ((_ignoreMask & Vector2IgnoreMask.Y) != 0) {
                value.y = currentValue.y;
            }

            return value;
        }
    }
}
