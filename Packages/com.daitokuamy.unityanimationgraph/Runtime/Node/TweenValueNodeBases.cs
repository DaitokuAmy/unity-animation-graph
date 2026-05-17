using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Component に float Tween を適用する ActionNode の基底クラス
    /// </summary>
    /// <typeparam name="TTarget">Tween の適用対象 Component 型</typeparam>
    public abstract class TweenFloatNode<TTarget> : TweenActionNode<TTarget, FloatTween, float>
        where TTarget : Component {
        /// <inheritdoc/>
        protected override bool TryEvaluateValue(FloatTween tweenSettings, IAnimationGraphBlackboard blackboard, float baseValue, float localTime, float calculatedDuration, out float value) {
            return tweenSettings.TryEvaluate(blackboard, baseValue, localTime, calculatedDuration, out value);
        }
    }

    /// <summary>
    /// Component に Vector2 Tween を適用する ActionNode の基底クラス
    /// </summary>
    /// <typeparam name="TTarget">Tween の適用対象 Component 型</typeparam>
    public abstract class TweenVector2Node<TTarget> : TweenActionNode<TTarget, Vector2Tween, Vector2>
        where TTarget : Component {
        /// <inheritdoc/>
        protected override bool TryEvaluateValue(Vector2Tween tweenSettings, IAnimationGraphBlackboard blackboard, Vector2 baseValue, float localTime, float calculatedDuration, out Vector2 value) {
            return tweenSettings.TryEvaluate(blackboard, baseValue, localTime, calculatedDuration, out value);
        }
    }

    /// <summary>
    /// Component に Vector3 Tween を適用する ActionNode の基底クラス
    /// </summary>
    /// <typeparam name="TTarget">Tween の適用対象 Component 型</typeparam>
    public abstract class TweenVector3Node<TTarget> : TweenActionNode<TTarget, Vector3Tween, Vector3>
        where TTarget : Component {
        /// <inheritdoc/>
        protected override bool TryEvaluateValue(Vector3Tween tweenSettings, IAnimationGraphBlackboard blackboard, Vector3 baseValue, float localTime, float calculatedDuration, out Vector3 value) {
            return tweenSettings.TryEvaluate(blackboard, baseValue, localTime, calculatedDuration, out value);
        }
    }

    /// <summary>
    /// Component に Vector4 Tween を適用する ActionNode の基底クラス
    /// </summary>
    /// <typeparam name="TTarget">Tween の適用対象 Component 型</typeparam>
    public abstract class TweenVector4Node<TTarget> : TweenActionNode<TTarget, Vector4Tween, Vector4>
        where TTarget : Component {
        /// <inheritdoc/>
        protected override bool TryEvaluateValue(Vector4Tween tweenSettings, IAnimationGraphBlackboard blackboard, Vector4 baseValue, float localTime, float calculatedDuration, out Vector4 value) {
            return tweenSettings.TryEvaluate(blackboard, baseValue, localTime, calculatedDuration, out value);
        }
    }

    /// <summary>
    /// Component に Color Tween を適用する ActionNode の基底クラス
    /// </summary>
    /// <typeparam name="TTarget">Tween の適用対象 Component 型</typeparam>
    public abstract class TweenColorNode<TTarget> : TweenActionNode<TTarget, ColorTween, Color>
        where TTarget : Component {
        /// <inheritdoc/>
        protected override bool TryEvaluateValue(ColorTween tweenSettings, IAnimationGraphBlackboard blackboard, Color baseValue, float localTime, float calculatedDuration, out Color value) {
            return tweenSettings.TryEvaluate(blackboard, baseValue, localTime, calculatedDuration, out value);
        }
    }
}
