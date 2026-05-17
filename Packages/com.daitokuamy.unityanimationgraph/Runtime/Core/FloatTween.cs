using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// float の Before/After と補間設定
    /// </summary>
    [Serializable]
    public class FloatTween : Tween {
        [SerializeField, Tooltip("Tween 開始時の値")]
        private FloatParameter _before = new(0.0f);
        [SerializeField, Tooltip("Tween 終了時の値")]
        private FloatParameter _after = new(1.0f);

        /// <summary>Tween 開始時の値</summary>
        public FloatParameter Before => _before;
        /// <summary>Tween 終了時の値</summary>
        public FloatParameter After => _after;

        /// <summary>
        /// FloatTween を生成
        /// </summary>
        public FloatTween() {
        }

        /// <summary>
        /// FloatTween を生成
        /// </summary>
        /// <param name="before">Tween 開始時の値</param>
        /// <param name="after">Tween 終了時の値</param>
        public FloatTween(float before, float after) {
            _before = new FloatParameter(before);
            _after = new FloatParameter(after);
        }

        /// <summary>
        /// 指定時刻の補間値の取得を試行
        /// </summary>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <param name="localTime">ノード開始時刻からの経過時間</param>
        /// <param name="calculatedDuration">計算済みの実行時間</param>
        /// <param name="value">補間値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryEvaluate(IAnimationGraphBlackboard blackboard, float localTime, float calculatedDuration, out float value) {
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
        public bool TryEvaluate(IAnimationGraphBlackboard blackboard, float baseValue, float localTime, float calculatedDuration, out float value) {
            if (!_before.TryGetValue(blackboard, baseValue, out var before) || !_after.TryGetValue(blackboard, baseValue, out var after)) {
                value = 0;
                return false;
            }

            value = Mathf.LerpUnclamped(before, after, EvaluateProgress(localTime, calculatedDuration));
            return true;
        }
    }
}
