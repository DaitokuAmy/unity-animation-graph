using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Color の Before/After と補間設定
    /// </summary>
    [Serializable]
    public class ColorTween : Tween {
        [SerializeField, Tooltip("Tween 開始時の色")]
        private ColorParameter _before = new(Color.white);
        [SerializeField, Tooltip("Tween 終了時の色")]
        private ColorParameter _after = new(Color.red);
        [SerializeField, Tooltip("更新しない要素")]
        private ColorIgnoreMask _ignoreMask;

        /// <summary>Tween 開始時の色</summary>
        public ColorParameter Before => _before;
        /// <summary>Tween 終了時の色</summary>
        public ColorParameter After => _after;
        /// <summary>更新しない要素</summary>
        public ColorIgnoreMask IgnoreMask => _ignoreMask;

        /// <summary>
        /// ColorTween を生成
        /// </summary>
        public ColorTween() {
        }

        /// <summary>
        /// ColorTween を生成
        /// </summary>
        /// <param name="before">Tween 開始時の色</param>
        /// <param name="after">Tween 終了時の色</param>
        public ColorTween(Color before, Color after) {
            _before = new ColorParameter(before);
            _after = new ColorParameter(after);
        }

        /// <summary>
        /// 指定時刻の補間色の取得を試行
        /// </summary>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <param name="localTime">ノード開始時刻からの経過時間</param>
        /// <param name="calculatedDuration">計算済みの実行時間</param>
        /// <param name="value">補間色</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryEvaluate(IAnimationGraphBlackboard blackboard, float localTime, float calculatedDuration, out Color value) {
            return TryEvaluate(blackboard, Color.white, localTime, calculatedDuration, out value);
        }

        /// <summary>
        /// 指定時刻の補間色の取得を試行
        /// </summary>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <param name="baseValue">乗算値の基準色</param>
        /// <param name="localTime">ノード開始時刻からの経過時間</param>
        /// <param name="calculatedDuration">計算済みの実行時間</param>
        /// <param name="value">補間色</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryEvaluate(IAnimationGraphBlackboard blackboard, Color baseValue, float localTime, float calculatedDuration, out Color value) {
            if (!_before.TryGetValue(blackboard, baseValue, out Color before) || !_after.TryGetValue(blackboard, baseValue, out Color after)) {
                value = default;
                return false;
            }

            value = Color.LerpUnclamped(before, after, EvaluateProgress(localTime, calculatedDuration));
            return true;
        }

        /// <summary>
        /// IgnoreMask に含まれる要素を現在値で維持
        /// </summary>
        /// <param name="value">Tween 評価後の色</param>
        /// <param name="currentValue">現在色</param>
        /// <returns>IgnoreMask を反映した色</returns>
        public Color ApplyIgnoreMask(Color value, Color currentValue) {
            if ((_ignoreMask & ColorIgnoreMask.R) != 0) {
                value.r = currentValue.r;
            }

            if ((_ignoreMask & ColorIgnoreMask.G) != 0) {
                value.g = currentValue.g;
            }

            if ((_ignoreMask & ColorIgnoreMask.B) != 0) {
                value.b = currentValue.b;
            }

            if ((_ignoreMask & ColorIgnoreMask.A) != 0) {
                value.a = currentValue.a;
            }

            return value;
        }
    }
}
