using System.Collections.Generic;
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
                runner.UpdateMode = AnimationGraphRunner.UpdateType.ManualUpdate;

                var firstHandle = runner.Play();
                runner.ManualUpdate(1.0f);
                runner.GraphAsset = secondGraphAsset;
                var secondHandle = runner.Play();
                runner.ManualUpdate(1.0f);

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
        /// GraphAsset 差し替えによる中断 continuation は Runner 状態更新後に呼ばれる
        /// </summary>
        [Test]
        public void SetGraph_InvokesInterruptedContinuationAfterRunnerStateIsUpdated() {
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
                runner.UpdateMode = AnimationGraphRunner.UpdateType.ManualUpdate;
                var continuationCalled = false;
                var observedGraphAsset = default(AnimationGraphAsset);
                var secondHandle = default(AnimationGraphPlayHandle);

                var firstHandle = runner.Play();
                runner.ManualUpdate(1.0f);
                firstHandle.GetAwaiter().OnCompleted(() => {
                    continuationCalled = true;
                    observedGraphAsset = runner.GraphAsset;
                    secondHandle = runner.Play();
                });
                runner.GraphAsset = secondGraphAsset;
                runner.ManualUpdate(1.0f);

                Assert.IsTrue(continuationCalled);
                Assert.That(observedGraphAsset, Is.EqualTo(secondGraphAsset));
                Assert.IsTrue(firstHandle.IsInterrupted);
                Assert.That(secondActionNode.EvaluateCount, Is.EqualTo(1));
                Assert.IsTrue(secondHandle.IsCompleted);
            }
            finally {
                Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// ManualUpdate は ManualUpdate 設定時のみ再生時間を進める
        /// </summary>
        [Test]
        public void ManualUpdate_TicksOnlyWhenUpdateTypeIsManualUpdate() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 1.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var gameObject = new GameObject("AnimationGraphRunnerTest");

            try {
                var runner = gameObject.AddComponent<AnimationGraphRunner>();
                runner.GraphAsset = graphAsset;

                var handle = runner.Play();
                runner.ManualUpdate(1.0f);

                Assert.That(actionNode.EvaluateCount, Is.EqualTo(0));
                Assert.IsFalse(handle.IsCompleted);

                runner.UpdateMode = AnimationGraphRunner.UpdateType.ManualUpdate;
                runner.ManualUpdate(1.0f);

                Assert.That(actionNode.EvaluateCount, Is.EqualTo(1));
                Assert.IsTrue(handle.IsCompleted);
            }
            finally {
                Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// Graph 指定の Loop と LoopDelay は Runner のデフォルト設定より優先される
        /// </summary>
        [Test]
        public void PlayGraph_UsesExplicitLoopSettingsAndRunnerDefaults() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 1.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var gameObject = new GameObject("AnimationGraphRunnerTest");

            try {
                var runner = gameObject.AddComponent<AnimationGraphRunner>();
                runner.UpdateMode = AnimationGraphRunner.UpdateType.ManualUpdate;
                runner.Loop = false;
                runner.LoopDelay = 0.0f;

                var handle = runner.Play(graphAsset, loop: true, loopDelay: 0.5f);
                runner.ManualUpdate(1.0f);
                runner.ManualUpdate(0.5f);

                Assert.That(actionNode.EvaluateCount, Is.EqualTo(1));
                Assert.IsFalse(handle.IsDone);

                runner.ManualUpdate(0.1f);

                Assert.That(actionNode.EvaluateCount, Is.EqualTo(2));
                Assert.IsFalse(handle.IsDone);
                runner.Stop();
                Assert.IsTrue(handle.IsInterrupted);

                runner.Loop = true;
                runner.LoopDelay = 1.0f;
                var defaultHandle = runner.Play(graphAsset);
                runner.ManualUpdate(1.0f);
                Assert.IsTrue(defaultHandle.IsCompleted);
            }
            finally {
                Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// Runner の LoopDelay は負数を 0 に補正する
        /// </summary>
        [Test]
        public void LoopDelay_ClampsToZero() {
            var gameObject = new GameObject("AnimationGraphRunnerTest");

            try {
                var runner = gameObject.AddComponent<AnimationGraphRunner>();
                runner.LoopDelay = -1.0f;

                Assert.That(runner.LoopDelay, Is.EqualTo(0.0f));
            }
            finally {
                Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// 無効化時に再生中ノードへ中断を通知
        /// </summary>
        [Test]
        public void OnDisable_StopsCurrentPlay() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 10.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var gameObject = new GameObject("AnimationGraphRunnerTest");

            try {
                var runner = gameObject.AddComponent<AnimationGraphRunner>();
                runner.GraphAsset = graphAsset;
                runner.UpdateMode = AnimationGraphRunner.UpdateType.ManualUpdate;

                var handle = runner.Play();
                runner.ManualUpdate(1.0f);
                gameObject.SetActive(false);

                Assert.IsTrue(handle.IsInterrupted);
                Assert.That(actionNode.CancelCount, Is.EqualTo(1));
                Assert.That(runner.State, Is.EqualTo(AnimationGraphPlayerState.Stopped));
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
            builder.SetBlackboardDefinitions(firstGraphAsset, new BlackboardDefinition("speed", 1.5f));
            builder.SetBlackboardDefinitions(secondGraphAsset, new BlackboardDefinition("speed", 2.5f));
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
                new BlackboardDefinition("flag", false),
                new BlackboardDefinition("count", 12),
                new BlackboardDefinition("speed", 1.5f),
                new BlackboardDefinition("label", "idle"),
                new BlackboardDefinition("offset", vector2Value),
                new BlackboardDefinition("position", vector3Value),
                new BlackboardDefinition("tint", colorValue));
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
            builder.SetBlackboardDefinitions(graphAsset, new BlackboardDefinition("speed", 1.5f));
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
        /// 同じ target schema の GraphAsset は Runner の binding を共有する
        /// </summary>
        [Test]
        public void SetGraph_KeepsTargetBindingsAcrossGraphsWithSameSchema() {
            using var builder = new AnimationGraphTestBuilder();
            var firstStartNode = builder.CreateStartNode("firstStart");
            var firstGraphAsset = builder.CreateGraph("firstStart", firstStartNode);
            var secondStartNode = builder.CreateStartNode("secondStart");
            var secondGraphAsset = builder.CreateGraph("secondStart", secondStartNode);
            var targetSchema = builder.CreateTargetSchema(new TargetDefinition("actor"));
            builder.SetTargetSchema(firstGraphAsset, targetSchema);
            builder.SetTargetSchema(secondGraphAsset, targetSchema);
            var runnerObject = new GameObject("AnimationGraphRunnerTest");
            var targetObject = new GameObject("Target");

            try {
                var runner = runnerObject.AddComponent<AnimationGraphRunner>();
                runner.TargetSchema = targetSchema;
                runner.GraphAsset = firstGraphAsset;
                Assert.IsTrue(runner.SetTarget("actor", targetObject.transform));

                runner.GraphAsset = secondGraphAsset;
                Assert.That(runner.GetTarget<Transform>("actor"), Is.EqualTo(targetObject.transform));
            }
            finally {
                Object.DestroyImmediate(runnerObject);
                Object.DestroyImmediate(targetObject);
            }
        }

        /// <summary>
        /// RebuildSchedule は Target Schema と Blackboard 定義を Runner 状態へ同期する
        /// </summary>
        [Test]
        public void RebuildSchedule_RefreshesGraphDerivedRunnerState() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start");
            var graphAsset = builder.CreateGraph("start", startNode);
            builder.SetBlackboardDefinitions(graphAsset, new BlackboardDefinition("speed", 1.5f));
            var targetSchema = builder.CreateTargetSchema(new TargetDefinition("actor"));
            builder.SetTargetSchema(graphAsset, targetSchema);
            var runnerObject = new GameObject("AnimationGraphRunnerTest");
            var actorObject = new GameObject("Actor");

            try {
                var runner = runnerObject.AddComponent<AnimationGraphRunner>();
                runner.TargetSchema = targetSchema;
                runner.GraphAsset = graphAsset;
                Assert.IsTrue(runner.SetBlackboardValue("speed", 9.0f));
                Assert.IsTrue(runner.SetTarget("actor", actorObject.transform));

                builder.SetBlackboardDefinitions(
                    graphAsset,
                    new BlackboardDefinition("speed", 2.5f),
                    new BlackboardDefinition("enabled", true));
                UnityAnimationGraph.Editor.AnimationGraphAssetUtility.SetTargetDefinitions(
                    targetSchema,
                    new[] { new TargetDefinition("actor"), new TargetDefinition("camera") });
                runner.RebuildSchedule();

                Assert.IsTrue(runner.Context.TryGetBlackboardValue("speed", out float speedValue));
                Assert.That(speedValue, Is.EqualTo(9.0f).Within(0.0001f));
                Assert.IsTrue(runner.Context.TryGetBlackboardValue("enabled", out bool enabledValue));
                Assert.IsTrue(enabledValue);
                Assert.That(runner.TargetBindings.Count, Is.EqualTo(2));
                Assert.That(runner.GetTarget<Transform>("actor"), Is.EqualTo(actorObject.transform));
                Assert.That(runner.TargetBindings[1].Key, Is.EqualTo("camera"));
            }
            finally {
                Object.DestroyImmediate(runnerObject);
                Object.DestroyImmediate(actorObject);
            }
        }

        /// <summary>
        /// public GetTarget は target schema に対応する binding から Component を取得する
        /// </summary>
        [Test]
        public void GetTarget_ReturnsCurrentTargetBinding() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start");
            var graphAsset = builder.CreateGraph("start", startNode);
            var targetSchema = builder.CreateTargetSchema(new TargetDefinition("actor"));
            builder.SetTargetSchema(graphAsset, targetSchema);
            var runnerObject = new GameObject("AnimationGraphRunnerTest");
            var targetObject = new GameObject("Target");

            try {
                var runner = runnerObject.AddComponent<AnimationGraphRunner>();
                runner.TargetSchema = targetSchema;
                runner.GraphAsset = graphAsset;
                Assert.IsTrue(runner.SetTarget("actor", targetObject.transform));

                var target = runner.GetTarget<Transform>("actor");
                Assert.That(target, Is.EqualTo(targetObject.transform));
                Assert.IsTrue(runner.TryGetTarget<Transform>("actor", out var tryTarget));
                Assert.That(tryTarget, Is.EqualTo(targetObject.transform));
                Assert.Throws<System.InvalidOperationException>(() => runner.GetTarget<Camera>("actor"));
            }
            finally {
                Object.DestroyImmediate(runnerObject);
                Object.DestroyImmediate(targetObject);
            }
        }

        /// <summary>
        /// 異なる target schema の GraphAsset は設定できない
        /// </summary>
        [Test]
        public void SetGraph_ThrowsWhenTargetSchemaDoesNotMatch() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start");
            var graphAsset = builder.CreateGraph("start", startNode);
            var graphSchema = builder.CreateTargetSchema(new TargetDefinition("actor"));
            var runnerSchema = builder.CreateTargetSchema(new TargetDefinition("actor"));
            builder.SetTargetSchema(graphAsset, graphSchema);
            var runnerObject = new GameObject("AnimationGraphRunnerTest");

            try {
                var runner = runnerObject.AddComponent<AnimationGraphRunner>();
                runner.TargetSchema = runnerSchema;

                Assert.IsFalse(runner.IsTargetSchemaCompatible(graphAsset));
                Assert.Throws<System.InvalidOperationException>(() => runner.GraphAsset = graphAsset);
            }
            finally {
                Object.DestroyImmediate(runnerObject);
            }
        }

        /// <summary>
        /// target binding は target 定義の MonoScript GUID を保持する
        /// </summary>
        [Test]
        public void SetGraph_CopiesTargetDefinitionMonoScriptGuidToBinding() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start");
            var graphAsset = builder.CreateGraph("start", startNode);
            var monoScriptGuid = "11111111111111111111111111111111";
            var targetSchema = builder.CreateTargetSchema(new TargetDefinition("actor", monoScriptGuid));
            builder.SetTargetSchema(graphAsset, targetSchema);
            var gameObject = new GameObject("AnimationGraphRunnerTest");

            try {
                var runner = gameObject.AddComponent<AnimationGraphRunner>();
                runner.TargetSchema = targetSchema;
                runner.GraphAsset = graphAsset;

                Assert.That(runner.TargetBindings.Count, Is.EqualTo(1));
                Assert.That(runner.TargetBindings[0].Key, Is.EqualTo("actor"));
                Assert.That(runner.TargetBindings[0].MonoScriptGuid, Is.EqualTo(monoScriptGuid));
            }
            finally {
                Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// collection binding は script から追加、削除、clear できる
        /// </summary>
        [Test]
        public void TargetCollection_CanMutateFromScript() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start");
            var graphAsset = builder.CreateGraph("start", startNode);
            var targetSchema = builder.CreateTargetSchema(new TargetDefinition("targets", string.Empty, TargetMultiplicity.Collection));
            builder.SetTargetSchema(graphAsset, targetSchema);
            var runnerObject = new GameObject("AnimationGraphRunnerTest");
            var firstTargetObject = new GameObject("FirstTarget");
            var secondTargetObject = new GameObject("SecondTarget");

            try {
                var runner = runnerObject.AddComponent<AnimationGraphRunner>();
                runner.TargetSchema = targetSchema;
                runner.GraphAsset = graphAsset;

                Assert.IsTrue(runner.AddTarget("targets", firstTargetObject.transform));
                Assert.IsTrue(runner.AddTarget("targets", null));
                Assert.IsTrue(runner.AddTarget("targets", secondTargetObject.transform));
                Assert.IsTrue(runner.TryGetTargets("targets", out IReadOnlyList<Transform> targets));
                Assert.That(targets, Is.EqualTo(new Transform[] { firstTargetObject.transform, null, secondTargetObject.transform }));

                Assert.IsTrue(runner.RemoveTarget("targets", firstTargetObject.transform));
                Assert.IsTrue(runner.ClearTargets("targets"));
                Assert.IsTrue(runner.TryGetTargets("targets", out targets));
                Assert.That(targets, Is.Empty);
                Assert.IsFalse(runner.SetTarget("targets", firstTargetObject.transform));
            }
            finally {
                Object.DestroyImmediate(runnerObject);
                Object.DestroyImmediate(firstTargetObject);
                Object.DestroyImmediate(secondTargetObject);
            }
        }
    }
}
