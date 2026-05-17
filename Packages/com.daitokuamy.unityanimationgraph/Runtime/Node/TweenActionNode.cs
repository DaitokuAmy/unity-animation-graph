using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Tween node の基準値を型ごとに保持するストア
    /// </summary>
    internal sealed class TweenBaseValueStore {
        private readonly Dictionary<Type, IStorage> _storagesByValueType = new();

        /// <summary>
        /// 保存済みの基準値をすべてクリア
        /// </summary>
        public void Clear() {
            foreach (var storage in _storagesByValueType.Values) {
                storage.Clear();
            }
        }

        /// <summary>
        /// 指定した安定順序の基準値を取得
        /// </summary>
        /// <param name="stableOrder">安定順序</param>
        /// <param name="value">取得した基準値</param>
        /// <typeparam name="TValue">基準値の型</typeparam>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetValue<TValue>(int stableOrder, out TValue value) {
            if (_storagesByValueType.TryGetValue(typeof(TValue), out var storage)) {
                return ((Storage<TValue>)storage).TryGetValue(stableOrder, out value);
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 指定した安定順序の基準値を保存
        /// </summary>
        /// <param name="stableOrder">安定順序</param>
        /// <param name="value">保存する基準値</param>
        /// <typeparam name="TValue">基準値の型</typeparam>
        public void SetValue<TValue>(int stableOrder, TValue value) {
            GetStorage<TValue>().SetValue(stableOrder, value);
        }

        /// <summary>
        /// 指定した安定順序の基準値を削除
        /// </summary>
        /// <param name="stableOrder">安定順序</param>
        /// <typeparam name="TValue">基準値の型</typeparam>
        public void RemoveValue<TValue>(int stableOrder) {
            if (_storagesByValueType.TryGetValue(typeof(TValue), out var storage)) {
                ((Storage<TValue>)storage).RemoveValue(stableOrder);
            }
        }

        /// <summary>
        /// 指定型のストレージを取得
        /// </summary>
        /// <typeparam name="TValue">基準値の型</typeparam>
        /// <returns>指定型のストレージ</returns>
        private Storage<TValue> GetStorage<TValue>() {
            var valueType = typeof(TValue);
            if (_storagesByValueType.TryGetValue(valueType, out var storage)) {
                return (Storage<TValue>)storage;
            }

            var typedStorage = new Storage<TValue>();
            _storagesByValueType.Add(valueType, typedStorage);
            return typedStorage;
        }

        /// <summary>
        /// 型別ストレージの共通インターフェース
        /// </summary>
        private interface IStorage {
            /// <summary>
            /// 保存済みの基準値をクリア
            /// </summary>
            void Clear();
        }

        /// <summary>
        /// 型付きの基準値ストレージ
        /// </summary>
        /// <typeparam name="TValue">基準値の型</typeparam>
        private sealed class Storage<TValue> : IStorage {
            private readonly Dictionary<int, TValue> _valuesByStableOrder = new();

            /// <summary>
            /// 保存済みの基準値をクリア
            /// </summary>
            public void Clear() {
                _valuesByStableOrder.Clear();
            }

            /// <summary>
            /// 指定した安定順序の基準値を取得
            /// </summary>
            /// <param name="stableOrder">安定順序</param>
            /// <param name="value">取得した基準値</param>
            /// <returns>取得できた場合は true</returns>
            public bool TryGetValue(int stableOrder, out TValue value) {
                return _valuesByStableOrder.TryGetValue(stableOrder, out value);
            }

            /// <summary>
            /// 指定した安定順序の基準値を保存
            /// </summary>
            /// <param name="stableOrder">安定順序</param>
            /// <param name="value">保存する基準値</param>
            public void SetValue(int stableOrder, TValue value) {
                _valuesByStableOrder[stableOrder] = value;
            }

            /// <summary>
            /// 指定した安定順序の基準値を削除
            /// </summary>
            /// <param name="stableOrder">安定順序</param>
            public void RemoveValue(int stableOrder) {
                _valuesByStableOrder.Remove(stableOrder);
            }
        }
    }

    /// <summary>
    /// Tween node の基準値キャプチャと基準値付き評価を行う内部インターフェース
    /// </summary>
    internal interface ITweenNodeExecutor {
        /// <summary>
        /// Tween 相対値の基準値を取得
        /// </summary>
        /// <param name="stableOrder">安定順序</param>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        /// <param name="baseValues">Tween 相対値の基準値ストア</param>
        void CaptureBaseValue(int stableOrder, int seed, IAnimationGraphContext context, TweenBaseValueStore baseValues);

        /// <summary>
        /// 基準値を使って Tween node を評価
        /// </summary>
        /// <param name="stableOrder">安定順序</param>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="localTime">ノード開始時刻からの経過時間</param>
        /// <param name="calculatedDuration">計算済みの実行時間</param>
        /// <param name="context">評価コンテキスト</param>
        /// <param name="baseValues">Tween 相対値の基準値ストア</param>
        void Evaluate(int stableOrder, int seed, float localTime, float calculatedDuration, IAnimationGraphContext context, TweenBaseValueStore baseValues);
    }

    /// <summary>
    /// Tween を Component に適用する ActionNode の基底クラス
    /// </summary>
    /// <typeparam name="TTarget">Tween の適用対象 Component 型</typeparam>
    /// <typeparam name="TTween">使用する Tween 設定型</typeparam>
    /// <typeparam name="TValue">Tween で補間する値型</typeparam>
    public abstract class TweenActionNode<TTarget, TTween, TValue> : ActionNode<TTarget>, ITweenNodeExecutor
        where TTarget : Component
        where TTween : Tween {
        [SerializeField, Min(0.0f), Tooltip("Tween にかける時間")]
        private float _duration = 1.0f;
        [SerializeField, Min(0.0f), Tooltip("Tween 開始前の待機時間")]
        private float _delay;

        /// <summary>Tween 設定</summary>
        protected abstract TTween TweenSettings { get; }

        /// <inheritdoc/>
        protected override float CalculateDuration(int seed, TTarget target, IAnimationGraphBlackboard blackboard) {
            return Mathf.Max(0.0f, _duration);
        }

        /// <inheritdoc/>
        protected override float CalculateDelay(int seed, TTarget target, IAnimationGraphBlackboard blackboard) {
            return Mathf.Max(0.0f, _delay);
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, TTarget target, float localTime, float calculatedDuration, IAnimationGraphBlackboard blackboard) {
            Evaluate(seed, target, GetBaseValue(target), localTime, calculatedDuration, blackboard);
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

        /// <inheritdoc/>
        void ITweenNodeExecutor.CaptureBaseValue(int stableOrder, int seed, IAnimationGraphContext context, TweenBaseValueStore baseValues) {
            if (!TryResolveTarget(context, out var target)) {
                baseValues.RemoveValue<TValue>(stableOrder);
                return;
            }

            baseValues.SetValue(stableOrder, GetBaseValue(target));
        }

        /// <inheritdoc/>
        void ITweenNodeExecutor.Evaluate(int stableOrder, int seed, float localTime, float calculatedDuration, IAnimationGraphContext context, TweenBaseValueStore baseValues) {
            if (!TryResolveTarget(context, out var target)) {
                return;
            }

            if (!baseValues.TryGetValue<TValue>(stableOrder, out var baseValue)) {
                baseValue = GetBaseValue(target);
                baseValues.SetValue(stableOrder, baseValue);
            }

            Evaluate(seed, target, baseValue, localTime, calculatedDuration, context);
        }
    }
}
