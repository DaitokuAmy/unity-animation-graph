using NUnit.Framework;
using UnityAnimationGraph.Editor;
using UnityEngine;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// AnimationGraphEditorPreviewPlayerFactory tests.
    /// </summary>
    public sealed class AnimationGraphEditorPreviewPlayerFactoryTests {
        /// <summary>
        /// TryCreate builds a schedule without evaluating preview nodes.
        /// </summary>
        [Test]
        public void TryCreate_BuildsScheduleWithoutEvaluatingNodes() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 1.5f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var rootGameObject = new GameObject("PreviewRoot");

            try {
                var created = AnimationGraphEditorPreviewPlayerFactory.TryCreate(
                    graphAsset,
                    rootGameObject,
                    null,
                    out var player,
                    out var context,
                    out var message);

                Assert.IsTrue(created, message);
                Assert.IsNotNull(player);
                Assert.IsNotNull(context);
                Assert.IsNotNull(player.Schedule);
                Assert.That(player.Duration, Is.EqualTo(1.5f).Within(0.0001f));
                Assert.That(actionNode.EvaluateCount, Is.EqualTo(0));
                Assert.That(player.State, Is.EqualTo(AnimationGraphPlayerState.Stopped));
            }
            finally {
                Object.DestroyImmediate(rootGameObject);
            }
        }

        /// <summary>
        /// TryBuildSchedule fails instead of expanding more nodes than the idle preview limit.
        /// </summary>
        [Test]
        public void TryBuildSchedule_StopsWhenScheduleNodeLimitIsExceeded() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "loop");
            var loopNode = builder.CreateNode<LoopNode>("loop", "after");
            var bodyNode = builder.CreateActionNode("body", 1.0f);
            var afterNode = builder.CreateActionNode("after", 1.0f);
            builder.SetLoop(loopNode, 10, "body");
            var graphAsset = builder.CreateGraph("start", startNode, loopNode, bodyNode, afterNode);
            var rootGameObject = new GameObject("PreviewRoot");

            try {
                var created = AnimationGraphEditorPreviewPlayerFactory.TryBuildSchedule(
                    graphAsset,
                    rootGameObject,
                    null,
                    4,
                    out var schedule,
                    out var message);

                Assert.IsFalse(created);
                Assert.IsNull(schedule);
                Assert.That(message, Does.Contain("Schedule node count exceeded limit"));
            }
            finally {
                Object.DestroyImmediate(rootGameObject);
            }
        }
    }
}
