using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Tween 系 ActionNode の実行時間設定を持つ基底クラス
    /// </summary>
    public abstract class TweenActionNode : ActionNode {
        [SerializeField, Min(0.0f), Tooltip("Tween にかける時間")]
        private float _duration = 1.0f;
        [SerializeField, Min(0.0f), Tooltip("Tween 開始前の待機時間")]
        private float _delay;

        /// <summary>Tween にかける時間</summary>
        public float Duration => Mathf.Max(0.0f, _duration);
        /// <summary>Tween 開始前の待機時間</summary>
        public float Delay => Mathf.Max(0.0f, _delay);

        /// <inheritdoc/>
        protected override float CalculateDuration(int seed, IAnimationGraphContext context) {
            return Duration;
        }

        /// <inheritdoc/>
        protected override float CalculateDelay(int seed, IAnimationGraphContext context) {
            return Delay;
        }
    }

    /// <summary>
    /// Tween を Component に適用する ActionNode の基底クラス
    /// </summary>
    /// <typeparam name="TTarget">Tween の適用対象 Component 型</typeparam>
    /// <typeparam name="TTween">使用する Tween 設定型</typeparam>
    /// <typeparam name="TValue">Tween で補間する値型</typeparam>
    public abstract class TweenActionNode<TTarget, TTween, TValue> : TweenActionNode
        where TTarget : Component
        where TTween : Tween {
        /// <summary>Tween 設定</summary>
        protected abstract TTween TweenSettings { get; }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context) {
            var tweenSettings = TweenSettings;
            if (tweenSettings == null) {
                return;
            }

            if (!TryResolveTarget(context, out var target)) {
                return;
            }

            if (!TryEvaluateValue(tweenSettings, context, localTime, calculatedDuration, out var value)) {
                return;
            }

            ApplyValue(target, value);
        }

        /// <summary>
        /// Tween 設定から指定時刻の補間値を取得
        /// </summary>
        /// <param name="tweenSettings">Tween 設定</param>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <param name="localTime">ノード開始時刻からの経過時間</param>
        /// <param name="calculatedDuration">計算済みの実行時間</param>
        /// <param name="value">補間値</param>
        /// <returns>取得できた場合は true</returns>
        protected abstract bool TryEvaluateValue(TTween tweenSettings, IAnimationGraphBlackboard blackboard, float localTime, float calculatedDuration, out TValue value);

        /// <summary>
        /// 対象 Component に補間値を適用
        /// </summary>
        /// <param name="target">適用対象 Component</param>
        /// <param name="value">補間値</param>
        protected abstract void ApplyValue(TTarget target, TValue value);

        /// <summary>
        /// TargetKey から対象 Component の解決を試行
        /// </summary>
        /// <param name="context">評価コンテキスト</param>
        /// <param name="target">解決した対象 Component</param>
        /// <returns>解決できた場合は true</returns>
        private bool TryResolveTarget(IAnimationGraphContext context, out TTarget target) {
            target = null;
            if (context == null) {
                return false;
            }

            if (string.IsNullOrEmpty(TargetKey)) {
                return false;
            }

            return context.TryGetTarget<TTarget>(TargetKey, out target) && target != null;
        }
    }
}
