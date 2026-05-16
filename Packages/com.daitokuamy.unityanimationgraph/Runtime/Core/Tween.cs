using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Tween 共通設定
    /// </summary>
    [Serializable]
    public abstract class Tween {
        [SerializeField, Tooltip("Tween の補間カーブ")]
        private TweenEase _ease = new(EaseType.Linear);

        /// <summary>Tween の補間カーブ</summary>
        public TweenEase Ease => _ease;

        /// <summary>
        /// Tween 補間設定を生成
        /// </summary>
        protected Tween() {
        }

        /// <summary>
        /// Tween 補間設定を生成
        /// </summary>
        /// <param name="ease">Tween の補間カーブ</param>
        protected Tween(TweenEase ease) {
            _ease = ease;
        }

        /// <summary>
        /// 指定時刻の補間率を取得
        /// </summary>
        /// <param name="localTime">ノード開始時刻からの経過時間</param>
        /// <param name="calculatedDuration">計算済みの実行時間</param>
        /// <returns>評価済みの補間率</returns>
        protected float EvaluateProgress(float localTime, float calculatedDuration) {
            var progress = calculatedDuration <= 0.0f ? 1.0f : Mathf.Clamp01(localTime / calculatedDuration);
            return _ease.Evaluate(progress);
        }
    }
}
