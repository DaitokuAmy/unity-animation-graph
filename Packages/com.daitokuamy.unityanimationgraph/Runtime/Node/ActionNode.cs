using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Animation Graph でターゲットを操作するノードの基底クラス
    /// </summary>
    public abstract class ActionNode : Node {
        [SerializeField, Tooltip("操作対象を解決するための target 参照")]
        private TargetReference _targetReference;
        [SerializeField, HideInInspector]
        private string _targetKey = string.Empty;

        /// <summary>操作対象を解決するための target 参照</summary>
        internal TargetReference TargetReference => _targetReference.Kind == TargetReferenceKind.CollectionItem || !string.IsNullOrEmpty(_targetReference.TargetKey)
            ? _targetReference
            : new TargetReference(TargetReferenceKind.Binding, _targetKey);
        /// <summary>操作対象を解決するためのターゲットキー</summary>
        internal string TargetKey => TargetReference.TargetKey;
    }

    /// <summary>
    /// 型付きターゲットを操作する ActionNode の基底クラス
    /// </summary>
    /// <typeparam name="TTarget">操作対象 Component 型</typeparam>
    public abstract class ActionNode<TTarget> : ActionNode
        where TTarget : Component {
        /// <summary>null target を有効な解決結果として扱う場合は true</summary>
        protected virtual bool AllowNullTarget => false;

        /// <inheritdoc/>
        protected sealed override float CalculateDuration(int seed, IAnimationGraphContext context) {
            return TryResolveTarget(context, out var target)
                ? CalculateDuration(seed, target, context)
                : 0.0f;
        }

        /// <inheritdoc/>
        protected sealed override float CalculateDelay(int seed, IAnimationGraphContext context) {
            return TryResolveTarget(context, out var target)
                ? CalculateDelay(seed, target, context)
                : 0.0f;
        }

        /// <inheritdoc/>
        protected sealed override IEnumerable<(Object Target, string PropertyPath)> GetPreviewProperties(IAnimationGraphContext context) {
            if (!TryResolveTarget(context, out var target)) {
                yield break;
            }

            foreach (var propertyPath in GetPreviewProperties(target)) {
                yield return (target, propertyPath);
            }

            foreach (var previewProperty in GetPreviewObjectProperties(target)) {
                yield return previewProperty;
            }
        }

        /// <inheritdoc/>
        protected sealed override void Enter(int seed, IAnimationGraphContext context) {
            if (!TryResolveTarget(context, out var target)) {
                return;
            }

            Enter(seed, target, context);
        }

        /// <inheritdoc/>
        protected sealed override void Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context) {
            if (!TryResolveTarget(context, out var target)) {
                return;
            }

            Evaluate(seed, target, localTime, calculatedDuration, context);
        }

        /// <inheritdoc/>
        protected sealed override void Exit(int seed, IAnimationGraphContext context) {
            if (!TryResolveTarget(context, out var target)) {
                return;
            }

            Exit(seed, target, context);
        }

        /// <inheritdoc/>
        protected sealed override void Cancel(int seed, IAnimationGraphContext context) {
            if (!TryResolveTarget(context, out var target)) {
                return;
            }

            Cancel(seed, target, context);
        }

        /// <summary>
        /// ノードの実行時間を計算
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="target">解決済みの操作対象 Component</param>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <returns>ノードの実行時間</returns>
        protected virtual float CalculateDuration(int seed, TTarget target, IAnimationGraphBlackboard blackboard) {
            return 0.0f;
        }

        /// <summary>
        /// ノードの開始遅延を計算
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="target">解決済みの操作対象 Component</param>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <returns>ノードの開始遅延</returns>
        protected virtual float CalculateDelay(int seed, TTarget target, IAnimationGraphBlackboard blackboard) {
            return 0.0f;
        }

        /// <summary>
        /// Preview 再生時に AnimationMode へ登録するプロパティを取得
        /// </summary>
        /// <param name="target">解決済みの操作対象 Component</param>
        /// <returns>登録対象の SerializedProperty path の一覧</returns>
        protected virtual IEnumerable<string> GetPreviewProperties(TTarget target) {
            yield break;
        }

        /// <summary>
        /// Preview 再生時に AnimationMode へ登録する target object とプロパティを取得
        /// </summary>
        /// <param name="target">解決済みの操作対象 Component</param>
        /// <returns>登録対象の Object と SerializedProperty path の一覧</returns>
        protected virtual IEnumerable<(Object Target, string PropertyPath)> GetPreviewObjectProperties(TTarget target) {
            yield break;
        }

        /// <summary>
        /// ノード開始時の処理を行う
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="target">解決済みの操作対象 Component</param>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        protected virtual void Enter(int seed, TTarget target, IAnimationGraphBlackboard blackboard) {
        }

        /// <summary>
        /// ノードを評価
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="target">解決済みの操作対象 Component</param>
        /// <param name="localTime">ノード開始時刻からの経過時間</param>
        /// <param name="calculatedDuration">計算済みの実行時間</param>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        protected abstract void Evaluate(int seed, TTarget target, float localTime, float calculatedDuration, IAnimationGraphBlackboard blackboard);

        /// <summary>
        /// ノード終了時の処理を行う
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="target">解決済みの操作対象 Component</param>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        protected virtual void Exit(int seed, TTarget target, IAnimationGraphBlackboard blackboard) {
        }

        /// <summary>
        /// 実行中のノードをキャンセル
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="target">解決済みの操作対象 Component</param>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        protected virtual void Cancel(int seed, TTarget target, IAnimationGraphBlackboard blackboard) {
        }

        /// <summary>
        /// TargetKey から対象 Component の解決を試行
        /// </summary>
        /// <param name="context">評価コンテキスト</param>
        /// <param name="target">解決した対象 Component</param>
        /// <returns>解決できた場合は true</returns>
        protected bool TryResolveTarget(IAnimationGraphContext context, out TTarget target) {
            target = null;
            if (context == null) {
                return false;
            }

            if (string.IsNullOrEmpty(TargetKey)) {
                return false;
            }

            if (TargetReference.Kind == TargetReferenceKind.CollectionItem) {
                var resolved = context is ScopedAnimationGraphContext scopedContext
                    && scopedContext.TryGetScopedTarget(TargetReference.ScopeNodeId, TargetReference.TargetKey, out target);
                return resolved && (target != null || AllowNullTarget);
            }

            return context.TryGetTarget(TargetKey, out target)
                && (target != null || AllowNullTarget);
        }

        /// <inheritdoc/>
        protected override string Validate(NodeValidationContext context) {
            if (string.IsNullOrEmpty(TargetKey)) {
                return $"{DisplayName} has empty target key";
            }

            if (TargetReference.Kind == TargetReferenceKind.CollectionItem) {
                return string.IsNullOrEmpty(TargetReference.ScopeNodeId)
                    ? $"{DisplayName} has empty iteration scope node ID"
                    : string.Empty;
            }

            return context.HasTargetDefinition(TargetKey)
                ? string.Empty
                : $"{DisplayName} references missing target '{TargetKey}'";
        }
    }
}
