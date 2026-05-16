using NUnit.Framework;
using UnityEngine;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// AnimationGraphRunner の EditMode テスト
    /// </summary>
    public sealed class AnimationGraphRunnerTests {
        /// <summary>
        /// 再生中に GraphAsset を差し替えると現在の再生を中断して次の graph を再生できる
        /// </summary>
        [Test]
        public void SetGraph_CanSwapGraphAssetAtRuntime() {
            using var builder = new AnimationGraphTestBuilder();
            var firstStartNode = builder.CreateStartNode("firstStart", "firstAction");
            var firstActionNode = builder.CreateActionNode("firstAction", 10.0f);
            var firstGraphAsset = builder.CreateGraph("firstStart", firstStartNode, firstActionNode);
            var secondStartNode = builder.CreateStartNode("secondStart", "secondAction");
            var secondActionNode = builder.CreateActionNode("secondAction", 1.0f);
            var secondGraphAsset = builder.CreateGraph("secondStart", secondStartNode, secondActionNode);
            var gameObject = new GameObject("AnimationGraphRunnerTest");

            try {
                var runner = gameObject.AddComponent<AnimationGraphRunner>();
                runner.GraphAsset = firstGraphAsset;

                var firstHandle = runner.Play();
                runner.Tick(1.0f);
                runner.GraphAsset = secondGraphAsset;
                var secondHandle = runner.Play();
                runner.Tick(1.0f);

                Assert.That(firstActionNode.CancelCount, Is.EqualTo(1));
                Assert.IsTrue(firstHandle.IsInterrupted);
                Assert.That(secondActionNode.EvaluateCount, Is.EqualTo(1));
                Assert.IsTrue(secondHandle.IsCompleted);
            }
            finally {
                Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// GraphAsset を設定すると Blackboard 現在値は GraphAsset の default value で作り直される
        /// </summary>
        [Test]
        public void SetGraph_ResetsBlackboardValuesFromGraphAssetDefaults() {
            using var builder = new AnimationGraphTestBuilder();
            var firstStartNode = builder.CreateStartNode("firstStart");
            var firstGraphAsset = builder.CreateGraph("firstStart", firstStartNode);
            var secondStartNode = builder.CreateStartNode("secondStart");
            var secondGraphAsset = builder.CreateGraph("secondStart", secondStartNode);
            builder.SetBlackboardDefinitions(firstGraphAsset, new AnimationGraphBlackboardDefinition("speed", 1.5f));
            builder.SetBlackboardDefinitions(secondGraphAsset, new AnimationGraphBlackboardDefinition("speed", 2.5f));
            var gameObject = new GameObject("AnimationGraphRunnerTest");

            try {
                var runner = gameObject.AddComponent<AnimationGraphRunner>();
                runner.GraphAsset = firstGraphAsset;
                Assert.IsTrue(runner.Context.TryGetBlackboardValue("speed", out float firstDefaultValue));
                Assert.That(firstDefaultValue, Is.EqualTo(1.5f).Within(0.0001f));

                Assert.IsTrue(runner.SetBlackboardValue("speed", 9.0f));
                runner.GraphAsset = secondGraphAsset;

                Assert.IsTrue(runner.Context.TryGetBlackboardValue("speed", out float secondDefaultValue));
                Assert.That(secondDefaultValue, Is.EqualTo(2.5f).Within(0.0001f));
            }
            finally {
                Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// Blackboard 現在値は対応する全 value type で設定・取得できる
        /// </summary>
        [Test]
        public void SetBlackboardValue_SupportsAllValueTypes() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start");
            var graphAsset = builder.CreateGraph("start", startNode);
            var vector2Value = new Vector2(1.0f, 2.0f);
            var vector3Value = new Vector3(3.0f, 4.0f, 5.0f);
            var colorValue = new Color(0.2f, 0.4f, 0.6f, 0.8f);
            var nextVector2Value = new Vector2(6.0f, 7.0f);
            var nextVector3Value = new Vector3(8.0f, 9.0f, 10.0f);
            var nextColorValue = new Color(0.1f, 0.3f, 0.5f, 0.7f);
            builder.SetBlackboardDefinitions(
                graphAsset,
                new AnimationGraphBlackboardDefinition("flag", false),
                new AnimationGraphBlackboardDefinition("count", 12),
                new AnimationGraphBlackboardDefinition("speed", 1.5f),
                new AnimationGraphBlackboardDefinition("label", "idle"),
                new AnimationGraphBlackboardDefinition("offset", vector2Value),
                new AnimationGraphBlackboardDefinition("position", vector3Value),
                new AnimationGraphBlackboardDefinition("tint", colorValue));
            var gameObject = new GameObject("AnimationGraphRunnerTest");

            try {
                var runner = gameObject.AddComponent<AnimationGraphRunner>();
                runner.GraphAsset = graphAsset;

                Assert.IsTrue(runner.SetBlackboardValue("flag", true));
                Assert.IsTrue(runner.SetBlackboardValue("count", 24));
                Assert.IsTrue(runner.SetBlackboardValue("speed", 2.5f));
                Assert.IsTrue(runner.SetBlackboardValue("label", "run"));
                Assert.IsTrue(runner.SetBlackboardValue("offset", nextVector2Value));
                Assert.IsTrue(runner.SetBlackboardValue("position", nextVector3Value));
                Assert.IsTrue(runner.SetBlackboardValue("tint", nextColorValue));

                Assert.IsTrue(runner.Context.TryGetBlackboardValue("flag", out bool flagValue));
                Assert.IsTrue(flagValue);
                Assert.IsTrue(runner.Context.TryGetBlackboardValue("count", out int countValue));
                Assert.That(countValue, Is.EqualTo(24));
                Assert.IsTrue(runner.Context.TryGetBlackboardValue("speed", out float speedValue));
                Assert.That(speedValue, Is.EqualTo(2.5f).Within(0.0001f));
                Assert.IsTrue(runner.Context.TryGetBlackboardValue("label", out string labelValue));
                Assert.That(labelValue, Is.EqualTo("run"));
                Assert.IsTrue(runner.Context.TryGetBlackboardValue("offset", out Vector2 offsetValue));
                Assert.That(offsetValue, Is.EqualTo(nextVector2Value));
                Assert.IsTrue(runner.Context.TryGetBlackboardValue("position", out Vector3 positionValue));
                Assert.That(positionValue, Is.EqualTo(nextVector3Value));
                Assert.IsTrue(runner.Context.TryGetBlackboardValue("tint", out Color tintValue));
                Assert.That(tintValue, Is.EqualTo(nextColorValue));
            }
            finally {
                Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// Blackboard 現在値は value type が一致しない場合に取得・設定されない
        /// </summary>
        [Test]
        public void BlackboardValue_ReturnsFalseWhenValueTypeDoesNotMatch() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start");
            var graphAsset = builder.CreateGraph("start", startNode);
            builder.SetBlackboardDefinitions(graphAsset, new AnimationGraphBlackboardDefinition("speed", 1.5f));
            var gameObject = new GameObject("AnimationGraphRunnerTest");

            try {
                var runner = gameObject.AddComponent<AnimationGraphRunner>();
                runner.GraphAsset = graphAsset;

                Assert.IsFalse(runner.SetBlackboardValue("speed", 24));
                Assert.IsFalse(runner.Context.TryGetBlackboardValue("speed", out int _));
                Assert.IsFalse(runner.SetBlackboardValue("missing", 2.5f));
                Assert.IsTrue(runner.Context.TryGetBlackboardValue("speed", out float speedValue));
                Assert.That(speedValue, Is.EqualTo(1.5f).Within(0.0001f));
            }
            finally {
                Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// target binding は GraphAsset GUID ごとに保持される
        /// </summary>
        [Test]
        public void SetGraph_KeepsTargetBindingsPerGraphAssetGuid() {
            using var builder = new AnimationGraphTestBuilder();
            var firstStartNode = builder.CreateStartNode("firstStart");
            var firstGraphAsset = builder.CreateGraph("firstStart", firstStartNode);
            var secondStartNode = builder.CreateStartNode("secondStart");
            var secondGraphAsset = builder.CreateGraph("secondStart", secondStartNode);
            builder.SetTargetDefinitions(firstGraphAsset, "actor");
            builder.SetTargetDefinitions(secondGraphAsset, "actor");
            var runnerObject = new GameObject("AnimationGraphRunnerTest");
            var firstTargetObject = new GameObject("FirstTarget");
            var secondTargetObject = new GameObject("SecondTarget");

            try {
                var runner = runnerObject.AddComponent<AnimationGraphRunner>();
                runner.GraphAsset = firstGraphAsset;
                Assert.IsTrue(runner.SetTarget("actor", firstTargetObject.transform));

                runner.GraphAsset = secondGraphAsset;
                Assert.IsTrue(runner.SetTarget("actor", secondTargetObject.transform));

                runner.GraphAsset = firstGraphAsset;
                Assert.IsTrue(runner.Context.TryGetTarget<Transform>("actor", out var firstTarget));
                Assert.That(firstTarget, Is.EqualTo(firstTargetObject.transform));

                runner.GraphAsset = secondGraphAsset;
                Assert.IsTrue(runner.Context.TryGetTarget<Transform>("actor", out var secondTarget));
                Assert.That(secondTarget, Is.EqualTo(secondTargetObject.transform));
            }
            finally {
                Object.DestroyImmediate(runnerObject);
                Object.DestroyImmediate(firstTargetObject);
                Object.DestroyImmediate(secondTargetObject);
            }
        }
    }
}
