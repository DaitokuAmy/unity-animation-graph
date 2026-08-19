using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Tween node の実行状態
    /// </summary>
    /// <typeparam name="TValue">Tween で補間する値型</typeparam>
    internal sealed class TweenActionNodeState<TValue> : IActionNodeState {
        /// <summary>基準値を取得済みの場合は true</summary>
        public bool HasBaseValue { get; set; }
        /// <summary>Tween 相対値の基準値</summary>
        public TValue BaseValue { get; set; }
    }

    /// <summary>
    /// Tween を Component に適用する ActionNode の基底クラス
    /// </summary>
    /// <typeparam name="TTarget">Tween の適用対象 Component 型</typeparam>
    /// <typeparam name="TTween">使用する Tween 設定型</typeparam>
    /// <typeparam name="TValue">Tween で補間する値型</typeparam>
    public abstract class TweenActionNode<TTarget, TTween, TValue> : ActionNode<TTarget>
        where TTarget : Component
        where TTween : Tween {
        [SerializeField, Min(0.0f), Tooltip("Tween にかける時間")]
        private float _duration = 1.0f;
        [SerializeField, Min(0.0f), Tooltip("Tween 開始前の待機時間")]
        private float _delay;

        /// <summary>Tween 設定</summary>
        protected abstract TTween TweenSettings { get; }

        /// <inheritdoc/>
        protected override IActionNodeState CreateState() {
            return new TweenActionNodeState<TValue>();
        }

        /// <inheritdoc/>
        protected override float CalculateDuration(int seed, TTarget target, IAnimationGraphBlackboard blackboard) {
            return Mathf.Max(0.0f, _duration);
        }

        /// <inheritdoc/>
        protected override float CalculateDelay(int seed, TTarget target, IAnimationGraphBlackboard blackboard) {
            return Mathf.Max(0.0f, _delay);
        }

        /// <inheritdoc/>
        protected override void Enter(int seed, TTarget target, IAnimationGraphBlackboard blackboard, IActionNodeState state) {
            var tweenState = (TweenActionNodeState<TValue>)state;
            tweenState.BaseValue = GetBaseValue(target);
            tweenState.HasBaseValue = true;
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, TTarget target, float localTime, float calculatedDuration, IAnimationGraphBlackboard blackboard, IActionNodeState state) {
            var tweenState = (TweenActionNodeState<TValue>)state;
            if (!tweenState.HasBaseValue) {
                tweenState.BaseValue = GetBaseValue(target);
                tweenState.HasBaseValue = true;
            }

            Evaluate(seed, target, tweenState.BaseValue, localTime, calculatedDuration, blackboard);
        }

        /// <summary>
        /// Tween 相対値の基準値を取得
        /// </summary>
        /// <param name="target">適用対象 Component</param>
        /// <returns>Tween 相対値の基準値</returns>
        protected abstract TValue GetBaseValue(TTarget target);

        /// <summary>
        /// Tween 設定から指定時刻の補間値を取得
        /// </summary>
        /// <param name="tweenSettings">Tween 設定</param>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <param name="baseValue">相対値の基準値</param>
        /// <param name="localTime">ノード開始時刻からの経過時間</param>
        /// <param name="calculatedDuration">計算済みの実行時間</param>
        /// <param name="value">補間値</param>
        /// <returns>取得できた場合は true</returns>
        protected abstract bool TryEvaluateValue(TTween tweenSettings, IAnimationGraphBlackboard blackboard, TValue baseValue, float localTime, float calculatedDuration, out TValue value);

        /// <summary>
        /// 対象 Component に補間値を適用
        /// </summary>
        /// <param name="target">適用対象 Component</param>
        /// <param name="value">補間値</param>
        protected abstract void ApplyValue(TTarget target, TValue value);

        /// <summary>
        /// 基準値を使って Tween node を評価
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="target">解決済みの操作対象 Component</param>
        /// <param name="baseValue">Tween 相対値の基準値</param>
        /// <param name="localTime">ノード開始時刻からの経過時間</param>
        /// <param name="calculatedDuration">計算済みの実行時間</param>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        private void Evaluate(int seed, TTarget target, TValue baseValue, float localTime, float calculatedDuration, IAnimationGraphBlackboard blackboard) {
            var tweenSettings = TweenSettings;
            if (tweenSettings == null) {
                return;
            }

            if (!TryEvaluateValue(tweenSettings, blackboard, baseValue, localTime, calculatedDuration, out var value)) {
                return;
            }

            ApplyValue(target, value);
        }

    }
}
