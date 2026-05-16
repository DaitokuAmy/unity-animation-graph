using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// AnimationGraphScheduler の EditMode テスト
    /// </summary>
    public sealed class AnimationGraphSchedulerTests {
        /// <summary>
        /// 直列接続では前ノードの終了時刻から後続ノードを開始する
        /// </summary>
        [Test]
        public void BuildSchedule_SerialNodesStartAfterPreviousDuration() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "first");
            var firstNode = builder.CreateActionNode("first", 1.0f, "second");
            var secondNode = builder.CreateActionNode("second", 2.0f);
            var graphAsset = builder.CreateGraph("start", startNode, firstNode, secondNode);

            var schedule = BuildSchedule(graphAsset);

            AssertScheduledNode(schedule, firstNode, 0.0f, 1.0f);
            AssertScheduledNode(schedule, secondNode, 1.0f, 2.0f);
        }

        /// <summary>
        /// DelayNode は後続ノードの開始時刻を待機時間分だけ遅らせる
        /// </summary>
        [Test]
        public void BuildSchedule_DelayNodeDelaysNextNodes() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "delay");
            var delayNode = builder.CreateNode<DelayNode>("delay", "action");
            var actionNode = builder.CreateActionNode("action", 2.0f);
            builder.SetDelay(delayNode, 1.5f);
            var graphAsset = builder.CreateGraph("start", startNode, delayNode, actionNode);

            var schedule = BuildSchedule(graphAsset);

            AssertScheduledNode(schedule, delayNode, 1.5f, 0.0f);
            AssertScheduledNode(schedule, actionNode, 1.5f, 2.0f);
        }

        /// <summary>
        /// JoinNode は JoinType に応じた合流時刻で後続を開始する
        /// </summary>
        /// <param name="joinType">合流方法</param>
        /// <param name="expectedAfterStartTime">後続ノードの期待開始時刻</param>
        [TestCase(JoinType.All, 3.0f)]
        [TestCase(JoinType.Any, 1.0f)]
        public void BuildSchedule_JoinNodeStartsNextNodeByJoinType(JoinType joinType, float expectedAfterStartTime) {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "delay");
            var delayNode = builder.CreateNode<DelayNode>("delay", "short", "long");
            var shortNode = builder.CreateActionNode("short", 1.0f, "join");
            var longNode = builder.CreateActionNode("long", 3.0f, "join");
            var joinNode = builder.CreateNode<JoinNode>("join", "after");
            var afterNode = builder.CreateActionNode("after", 2.0f);
            builder.SetJoinType(joinNode, joinType);
            var graphAsset = builder.CreateGraph("start", startNode, delayNode, shortNode, longNode, joinNode, afterNode);

            var schedule = BuildSchedule(graphAsset);

            AssertScheduledNode(schedule, afterNode, expectedAfterStartTime, 2.0f);
        }

        /// <summary>
        /// BranchNode は build 時に選択されなかった側を schedule に含めない
        /// </summary>
        [Test]
        public void BuildSchedule_BranchNodeSchedulesOnlySelectedSide() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "branch");
            var branchNode = builder.CreateNode<TestBranchNode>("branch", "trueAction");
            branchNode.Condition = false;
            var trueActionNode = builder.CreateActionNode("trueAction", 1.0f);
            var falseActionNode = builder.CreateActionNode("falseAction", 2.0f);
            builder.SetFalseNodeIds(branchNode, "falseAction");
            var graphAsset = builder.CreateGraph("start", startNode, branchNode, trueActionNode, falseActionNode);

            var schedule = BuildSchedule(graphAsset);

            Assert.IsFalse(ContainsNode(schedule, trueActionNode));
            AssertScheduledNode(schedule, falseActionNode, 0.0f, 2.0f);
        }

        /// <summary>
        /// LoopNode はループ内容を指定回数分 schedule に展開してから後続へ進む
        /// </summary>
        [Test]
        public void BuildSchedule_LoopNodeExpandsBodyByLoopCount() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "loop");
            var loopNode = builder.CreateNode<LoopNode>("loop", "after");
            var bodyNode = builder.CreateActionNode("body", 1.0f);
            var afterNode = builder.CreateActionNode("after", 2.0f);
            builder.SetLoop(loopNode, 3, "body");
            var graphAsset = builder.CreateGraph("start", startNode, loopNode, bodyNode, afterNode);

            var schedule = BuildSchedule(graphAsset);
            var bodyScheduledNodes = FindScheduledNodes(schedule, bodyNode);

            Assert.That(bodyScheduledNodes.Count, Is.EqualTo(3));
            Assert.That(bodyScheduledNodes[0].StartTime, Is.EqualTo(0.0f).Within(0.0001f));
            Assert.That(bodyScheduledNodes[1].StartTime, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(bodyScheduledNodes[2].StartTime, Is.EqualTo(2.0f).Within(0.0001f));
            AssertScheduledNode(schedule, afterNode, 3.0f, 2.0f);
        }

        /// <summary>
        /// LoopNode は LoopNodeIds の開始ノードから到達できる後続ノードも body として展開する
        /// </summary>
        [Test]
        public void BuildSchedule_LoopNodeExpandsReachableBodyByLoopCount() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "loop");
            var loopNode = builder.CreateNode<LoopNode>("loop", "after");
            var bodyNode = builder.CreateActionNode("body", 1.0f, "bodyNext");
            var bodyNextNode = builder.CreateActionNode("bodyNext", 2.0f);
            var afterNode = builder.CreateActionNode("after", 3.0f);
            builder.SetLoop(loopNode, 3, "body");
            var graphAsset = builder.CreateGraph("start", startNode, loopNode, bodyNode, bodyNextNode, afterNode);

            var schedule = BuildSchedule(graphAsset);
            var bodyScheduledNodes = FindScheduledNodes(schedule, bodyNode);
            var bodyNextScheduledNodes = FindScheduledNodes(schedule, bodyNextNode);

            Assert.That(bodyScheduledNodes.Count, Is.EqualTo(3));
            Assert.That(bodyNextScheduledNodes.Count, Is.EqualTo(3));
            Assert.That(bodyScheduledNodes[0].StartTime, Is.EqualTo(0.0f).Within(0.0001f));
            Assert.That(bodyNextScheduledNodes[0].StartTime, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(bodyScheduledNodes[1].StartTime, Is.EqualTo(3.0f).Within(0.0001f));
            Assert.That(bodyNextScheduledNodes[1].StartTime, Is.EqualTo(4.0f).Within(0.0001f));
            Assert.That(bodyScheduledNodes[2].StartTime, Is.EqualTo(6.0f).Within(0.0001f));
            Assert.That(bodyNextScheduledNodes[2].StartTime, Is.EqualTo(7.0f).Within(0.0001f));
            AssertScheduledNode(schedule, afterNode, 9.0f, 3.0f);
        }

        /// <summary>
        /// RandomSeed が有効な場合は schedule build ごとに default seed を生成する
        /// </summary>
        [Test]
        public void BuildSchedule_GeneratesDefaultSeedWhenRandomSeedIsEnabled() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 1.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            builder.SetRandomSeed(graphAsset, true);
            var seeds = new HashSet<int>();

            for (var i = 0; i < 8; i++) {
                var schedule = BuildSchedule(graphAsset);
                seeds.Add(FindSingleScheduledNode(schedule, actionNode).Seed);
            }

            Assert.That(seeds.Count, Is.GreaterThan(1));
        }

        /// <summary>
        /// overrideSeed 指定時は RandomSeed より overrideSeed を優先する
        /// </summary>
        [Test]
        public void BuildSchedule_UsesOverrideSeedWhenRandomSeedIsEnabled() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 1.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            builder.SetRandomSeed(graphAsset, true);
            var scheduler = new AnimationGraphScheduler();
            scheduler.SetGraph(graphAsset);
            var context = new TestAnimationGraphContext();

            var firstSchedule = scheduler.BuildSchedule(context, 6789);
            var secondSchedule = scheduler.BuildSchedule(context, 6789);

            Assert.That(FindSingleScheduledNode(firstSchedule, actionNode).Seed, Is.EqualTo(FindSingleScheduledNode(secondSchedule, actionNode).Seed));
        }

        /// <summary>
        /// LoopNode の内容ノードはループ外へ接続できない
        /// </summary>
        [Test]
        public void SetGraph_ThrowsWhenLoopBodyConnectsOutsideLoop() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "loop");
            var loopNode = builder.CreateNode<LoopNode>("loop", "after");
            var bodyNode = builder.CreateActionNode("body", 1.0f, "after");
            var afterNode = builder.CreateActionNode("after", 2.0f);
            builder.SetLoop(loopNode, 1, "body");
            var graphAsset = builder.CreateGraph("start", startNode, loopNode, bodyNode, afterNode);

            var scheduler = new AnimationGraphScheduler();
            Assert.Throws<InvalidOperationException>(() => scheduler.SetGraph(graphAsset));
        }

        /// <summary>
        /// AnimationGraphSchedule を構築
        /// </summary>
        /// <param name="graphAsset">構築対象 graph asset</param>
        /// <returns>構築した schedule</returns>
        private AnimationGraphSchedule BuildSchedule(AnimationGraphAsset graphAsset) {
            var scheduler = new AnimationGraphScheduler();
            scheduler.SetGraph(graphAsset);
            return scheduler.BuildSchedule(new TestAnimationGraphContext());
        }

        /// <summary>
        /// 指定ノードの schedule 結果を検証
        /// </summary>
        /// <param name="schedule">検証対象 schedule</param>
        /// <param name="node">検証対象ノード</param>
        /// <param name="expectedStartTime">期待開始時刻</param>
        /// <param name="expectedDuration">期待実行時間</param>
        private void AssertScheduledNode(AnimationGraphSchedule schedule, Node node, float expectedStartTime, float expectedDuration) {
            var scheduledNode = FindSingleScheduledNode(schedule, node);
            Assert.That(scheduledNode.StartTime, Is.EqualTo(expectedStartTime).Within(0.0001f));
            Assert.That(scheduledNode.Duration, Is.EqualTo(expectedDuration).Within(0.0001f));
        }

        /// <summary>
        /// 指定ノードが schedule に含まれるか判定
        /// </summary>
        /// <param name="schedule">検索対象 schedule</param>
        /// <param name="node">検索対象ノード</param>
        /// <returns>含まれる場合は true</returns>
        private bool ContainsNode(AnimationGraphSchedule schedule, Node node) {
            for (var i = 0; i < schedule.Nodes.Count; i++) {
                if (schedule.Nodes[i].Node == node) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 指定ノードの schedule 結果を 1 件取得
        /// </summary>
        /// <param name="schedule">検索対象 schedule</param>
        /// <param name="node">検索対象ノード</param>
        /// <returns>見つかった scheduled node</returns>
        private ScheduledNode FindSingleScheduledNode(AnimationGraphSchedule schedule, Node node) {
            var scheduledNodes = FindScheduledNodes(schedule, node);
            Assert.That(scheduledNodes.Count, Is.EqualTo(1));
            return scheduledNodes[0];
        }

        /// <summary>
        /// 指定ノードの schedule 結果をすべて取得
        /// </summary>
        /// <param name="schedule">検索対象 schedule</param>
        /// <param name="node">検索対象ノード</param>
        /// <returns>見つかった scheduled node 一覧</returns>
        private List<ScheduledNode> FindScheduledNodes(AnimationGraphSchedule schedule, Node node) {
            var scheduledNodes = new List<ScheduledNode>();
            for (var i = 0; i < schedule.Nodes.Count; i++) {
                var scheduledNode = schedule.Nodes[i];
                if (scheduledNode.Node == node) {
                    scheduledNodes.Add(scheduledNode);
                }
            }

            return scheduledNodes;
        }
    }
}
