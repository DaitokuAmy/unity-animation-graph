using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Vector3 の Before/After と補間設定
    /// </summary>
    [Serializable]
    public class Vector3Tween : Tween {
        [SerializeField, Tooltip("Tween 開始時の値")]
        private Vector3Parameter _before = new(Vector3.zero);
        [SerializeField, Tooltip("Tween 終了時の値")]
        private Vector3Parameter _after = new(Vector3.one);
        [SerializeField, Tooltip("更新しない要素")]
        private Vector3IgnoreMask _ignoreMask;

        /// <summary>Tween 開始時の値</summary>
        public Vector3Parameter Before => _before;
        /// <summary>Tween 終了時の値</summary>
        public Vector3Parameter After => _after;
        /// <summary>更新しない要素</summary>
        public Vector3IgnoreMask IgnoreMask => _ignoreMask;

        /// <summary>
        /// Vector3Tween を生成
        /// </summary>
        public Vector3Tween() {
        }

        /// <summary>
        /// Vector3Tween を生成
        /// </summary>
        /// <param name="before">Tween 開始時の値</param>
        /// <param name="after">Tween 終了時の値</param>
        public Vector3Tween(Vector3 before, Vector3 after) {
            _before = new Vector3Parameter(before);
            _after = new Vector3Parameter(after);
        }

        /// <summary>
        /// 指定時刻の補間値の取得を試行
        /// </summary>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <param name="localTime">ノード開始時刻からの経過時間</param>
        /// <param name="calculatedDuration">計算済みの実行時間</param>
        /// <param name="value">補間値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryEvaluate(IAnimationGraphBlackboard blackboard, float localTime, float calculatedDuration, out Vector3 value) {
            if (!_before.TryGetValue(blackboard, out Vector3 before) || !_after.TryGetValue(blackboard, out Vector3 after)) {
                value = default;
                return false;
            }

            value = Vector3.LerpUnclamped(before, after, EvaluateProgress(localTime, calculatedDuration));
            return true;
        }

        /// <summary>
        /// IgnoreMask に含まれる要素を現在値で維持
        /// </summary>
        /// <param name="value">Tween 評価後の値</param>
        /// <param name="currentValue">現在値</param>
        /// <returns>IgnoreMask を反映した値</returns>
        public Vector3 ApplyIgnoreMask(Vector3 value, Vector3 currentValue) {
            if ((_ignoreMask & Vector3IgnoreMask.X) != 0) {
                value.x = currentValue.x;
            }

            if ((_ignoreMask & Vector3IgnoreMask.Y) != 0) {
                value.y = currentValue.y;
            }

            if ((_ignoreMask & Vector3IgnoreMask.Z) != 0) {
                value.z = currentValue.z;
            }

            return value;
        }
    }
}
