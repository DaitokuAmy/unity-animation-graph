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
    }
}
