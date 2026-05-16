using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Tween の補間率の解決方法
    /// </summary>
    public enum TweenEaseMode {
        /// <summary>EaseType を使用</summary>
        EaseType,
        /// <summary>AnimationCurve を使用</summary>
        AnimationCurve,
    }

    /// <summary>
    /// EaseType と AnimationCurve を切り替えて Tween の補間率を評価する値
    /// </summary>
    [Serializable]
    public struct TweenEase {
        [SerializeField, Tooltip("Tween 補間率の解決方法")]
        private TweenEaseMode _mode;
        [SerializeField, Tooltip("EaseType を使用する場合の補間カーブ")]
        private EaseType _easeType;
        [SerializeField, Tooltip("AnimationCurve を使用する場合の補間カーブ")]
        private AnimationCurve _animationCurve;

        /// <summary>Tween 補間率の解決方法</summary>
        public TweenEaseMode Mode => _mode;
        /// <summary>EaseType を使用する場合の補間カーブ</summary>
        public EaseType EaseType => _easeType;
        /// <summary>AnimationCurve を使用する場合の補間カーブ</summary>
        public AnimationCurve AnimationCurve => _animationCurve;

        /// <summary>
        /// EaseType を使う TweenEase を生成
        /// </summary>
        /// <param name="easeType">補間カーブ種別</param>
        public TweenEase(EaseType easeType) {
            _mode = TweenEaseMode.EaseType;
            _easeType = easeType;
            _animationCurve = null;
        }

        /// <summary>
        /// AnimationCurve を使う TweenEase を生成
        /// </summary>
        /// <param name="animationCurve">補間カーブ</param>
        public TweenEase(AnimationCurve animationCurve) {
            _mode = TweenEaseMode.AnimationCurve;
            _easeType = EaseType.Linear;
            _animationCurve = animationCurve;
        }

        /// <summary>
        /// 補間率を評価
        /// </summary>
        /// <param name="ratio">0 から 1 の補間率</param>
        /// <returns>評価後の補間率</returns>
        public float Evaluate(float ratio) {
            ratio = Mathf.Clamp01(ratio);
            if (_mode == TweenEaseMode.AnimationCurve) {
                return _animationCurve == null ? ratio : _animationCurve.Evaluate(ratio);
            }

            return Easing.Evaluate(_easeType, ratio);
        }
    }
}
