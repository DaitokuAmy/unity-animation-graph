using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// Tween 共通値の EditMode テスト
    /// </summary>
    public sealed class TweenValueTests {
        /// <summary>
        /// Easing は EaseType に応じた補間率を返す
        /// </summary>
        [Test]
        public void Easing_EvaluatesEaseType() {
            var value = Easing.Evaluate(EaseType.EaseInQuad, 0.0f, 10.0f, 0.5f);

            Assert.That(value, Is.EqualTo(2.5f).Within(0.0001f));
        }

        /// <summary>
        /// TweenEase は AnimationCurve を使用できる
        /// </summary>
        [Test]
        public void TweenEase_EvaluatesAnimationCurve() {
            var tweenEase = new TweenEase(AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 0.5f));

            var value = tweenEase.Evaluate(0.5f);

            Assert.That(value, Is.EqualTo(0.25f).Within(0.0001f));
        }

        /// <summary>
        /// Vector3Parameter は Vector3 直値を解決できる
        /// </summary>
        [Test]
        public void Vector3Parameter_ResolvesDirectVector3Value() {
            var parameter = new Vector3Parameter(new Vector3(1.0f, 2.0f, 3.0f));

            var resolved = parameter.TryGetValue(new TestAnimationGraphContext(), out Vector3 value);

            Assert.IsTrue(resolved);
            Assert.That(value, Is.EqualTo(new Vector3(1.0f, 2.0f, 3.0f)));
        }

        /// <summary>
        /// Vector3Parameter は Blackboard の Vector3 値を解決できる
        /// </summary>
        [Test]
        public void Vector3Parameter_ResolvesBlackboardVector3Value() {
            var context = new TestAnimationGraphContext();
            context.SetBlackboardValue("target", new Vector3(3.0f, 2.0f, 1.0f));
            var parameter = Vector3Parameter.Blackboard("target");

            var resolved = parameter.TryGetValue(context, out Vector3 value);

            Assert.IsTrue(resolved);
            Assert.That(value, Is.EqualTo(new Vector3(3.0f, 2.0f, 1.0f)));
        }

        /// <summary>
        /// Vector3Parameter は相対値として基準値を加算できる
        /// </summary>
        [Test]
        public void Vector3Parameter_ResolvesRelativeVector3Value() {
            var parameter = new Vector3Parameter(new Vector3(1.0f, 2.0f, 3.0f), true);

            var resolved = parameter.TryGetValue(new TestAnimationGraphContext(), new Vector3(10.0f, 20.0f, 30.0f), out Vector3 value);

            Assert.IsTrue(resolved);
            Assert.That(value, Is.EqualTo(new Vector3(11.0f, 22.0f, 33.0f)));
        }

        /// <summary>
        /// ColorParameter は相対値として基準色に乗算できる
        /// </summary>
        [Test]
        public void ColorParameter_ResolvesRelativeColorValue() {
            var parameter = new ColorParameter(new Color(0.5f, 0.25f, 0.75f, 0.8f), true);

            var resolved = parameter.TryGetValue(new TestAnimationGraphContext(), new Color(0.2f, 0.4f, 0.6f, 0.5f), out Color value);

            Assert.IsTrue(resolved);
            Assert.That(value.r, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(value.g, Is.EqualTo(0.1f).Within(0.0001f));
            Assert.That(value.b, Is.EqualTo(0.45f).Within(0.0001f));
            Assert.That(value.a, Is.EqualTo(0.4f).Within(0.0001f));
        }

        /// <summary>
        /// ColorTween は基準色なし評価で相対色を黒透明にしない
        /// </summary>
        [Test]
        public void ColorTween_EvaluatesRelativeColorWithNeutralBaseValue() {
            var tween = new ColorTween();
            var beforeField = typeof(ColorTween).GetField("_before", BindingFlags.Instance | BindingFlags.NonPublic);
            var afterField = typeof(ColorTween).GetField("_after", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(beforeField);
            Assert.NotNull(afterField);
            beforeField.SetValue(tween, new ColorParameter(new Color(0.5f, 0.25f, 0.75f, 0.8f), true));
            afterField.SetValue(tween, new ColorParameter(Color.white));

            var resolved = tween.TryEvaluate(new TestAnimationGraphContext(), 0.0f, 1.0f, out Color value);

            Assert.IsTrue(resolved);
            Assert.That(value.r, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(value.g, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(value.b, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(value.a, Is.EqualTo(0.8f).Within(0.0001f));
        }

        /// <summary>
        /// Vector4Parameter は Vector4 直値を解決できる
        /// </summary>
        [Test]
        public void Vector4Parameter_ResolvesDirectVector4Value() {
            var parameter = new Vector4Parameter(new Vector4(1.0f, 2.0f, 3.0f, 4.0f));

            var resolved = parameter.TryGetValue(new TestAnimationGraphContext(), out Vector4 value);

            Assert.IsTrue(resolved);
            Assert.That(value, Is.EqualTo(new Vector4(1.0f, 2.0f, 3.0f, 4.0f)));
        }

        /// <summary>
        /// Vector4Parameter は Blackboard の Vector4 値を解決できる
        /// </summary>
        [Test]
        public void Vector4Parameter_ResolvesBlackboardVector4Value() {
            var context = new TestAnimationGraphContext();
            context.SetBlackboardValue("target", new Vector4(4.0f, 3.0f, 2.0f, 1.0f));
            var parameter = Vector4Parameter.Blackboard("target");

            var resolved = parameter.TryGetValue(context, out Vector4 value);

            Assert.IsTrue(resolved);
            Assert.That(value, Is.EqualTo(new Vector4(4.0f, 3.0f, 2.0f, 1.0f)));
        }
    }
}
