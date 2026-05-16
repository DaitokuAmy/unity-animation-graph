using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// AnimationGraphPlayer の EditMode テスト
    /// </summary>
    public sealed class AnimationGraphPlayerTests {
        /// <summary>
        /// Tick は現在時刻で active なノードと終了時刻を通過したノードを評価する
        /// </summary>
        [Test]
        public void Tick_EvaluatesActiveNodesAndCrossedEndTime() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "first");
            var firstNode = builder.CreateActionNode("first", 2.0f, "second");
            var secondNode = builder.CreateActionNode("second", 2.0f);
            var graphAsset = builder.CreateGraph("start", startNode, firstNode, secondNode);
            var player = CreatePlayer(graphAsset);

            player.Play();
            player.Tick(0.5f);

            Assert.That(firstNode.EvaluateCount, Is.EqualTo(1));
            Assert.That(firstNode.LastLocalTime, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(secondNode.EvaluateCount, Is.EqualTo(0));

            player.Tick(2.0f);

            Assert.That(firstNode.EvaluateCount, Is.EqualTo(2));
            Assert.That(firstNode.LastLocalTime, Is.EqualTo(2.0f).Within(0.0001f));
            Assert.That(secondNode.EvaluateCount, Is.EqualTo(1));
            Assert.That(secondNode.LastLocalTime, Is.EqualTo(0.5f).Within(0.0001f));
        }

        /// <summary>
        /// Tick は前回時刻と今回時刻の間で終了したノードを終了時刻で評価する
        /// </summary>
        [Test]
        public void Tick_EvaluatesEndTimeWhenNodeEndsBetweenFrames() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "first");
            var firstNode = builder.CreateActionNode("first", 1.0f, "second");
            var secondNode = builder.CreateActionNode("second", 2.0f);
            var graphAsset = builder.CreateGraph("start", startNode, firstNode, secondNode);
            var player = CreatePlayer(graphAsset);

            player.Play();
            player.Tick(0.98f);
            player.Tick(0.04f);

            Assert.That(firstNode.EvaluateCount, Is.EqualTo(2));
            Assert.That(firstNode.LastLocalTime, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(secondNode.EvaluateCount, Is.EqualTo(1));
            Assert.That(secondNode.LastLocalTime, Is.EqualTo(0.02f).Within(0.0001f));
        }

        /// <summary>
        /// DelayNode の待機時間を過ぎるまで後続ノードを評価しない
        /// </summary>
        [Test]
        public void Tick_DelayNodeWaitsBeforeNextNodeStarts() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "delay");
            var delayNode = builder.CreateNode<DelayNode>("delay", "action");
            var actionNode = builder.CreateActionNode("action", 1.0f);
            builder.SetDelay(delayNode, 1.0f);
            var graphAsset = builder.CreateGraph("start", startNode, delayNode, actionNode);
            var player = CreatePlayer(graphAsset);

            player.Play();
            player.Tick(0.5f);

            Assert.That(actionNode.EvaluateCount, Is.EqualTo(0));

            player.Tick(0.5f);

            Assert.That(actionNode.EvaluateCount, Is.EqualTo(1));
            Assert.That(actionNode.LastLocalTime, Is.EqualTo(0.0f).Within(0.0001f));
        }

        /// <summary>
        /// Seek は 0 秒から指定時刻までを順方向に評価する
        /// </summary>
        [Test]
        public void Seek_EvaluatesFromStartToTargetTime() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "first");
            var firstNode = builder.CreateActionNode("first", 1.0f, "second");
            var secondNode = builder.CreateActionNode("second", 2.0f);
            var graphAsset = builder.CreateGraph("start", startNode, firstNode, secondNode);
            var player = CreatePlayer(graphAsset);

            player.Seek(2.0f);

            Assert.That(player.CurrentTime, Is.EqualTo(2.0f).Within(0.0001f));
            Assert.That(firstNode.EvaluateCount, Is.EqualTo(1));
            Assert.That(firstNode.LastLocalTime, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(secondNode.EvaluateCount, Is.EqualTo(1));
            Assert.That(secondNode.LastLocalTime, Is.EqualTo(1.0f).Within(0.0001f));

            player.Seek(0.5f);

            Assert.That(player.CurrentTime, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(firstNode.EvaluateCount, Is.EqualTo(2));
            Assert.That(firstNode.LastLocalTime, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(secondNode.EvaluateCount, Is.EqualTo(1));
        }

        /// <summary>
        /// Tick は deltaTime に TimeScale を乗算して再生時間を進める
        /// </summary>
        [Test]
        public void Tick_AdvancesTimeWithTimeScale() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 2.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);

            Assert.That(player.TimeScale, Is.EqualTo(1.0f));
            player.TimeScale = 0.5f;
            player.Play();
            player.Tick(1.0f);

            Assert.That(player.CurrentTime, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(actionNode.LastLocalTime, Is.EqualTo(0.5f).Within(0.0001f));
        }

        /// <summary>
        /// Play は schedule 内の全ノードに再生開始を通知する
        /// </summary>
        [Test]
        public void Play_CallsBeginPlaybackForScheduledNodes() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 1.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);

            player.Play();
            player.Pause();
            player.Play();

            Assert.That(actionNode.BeginPlaybackCount, Is.EqualTo(1));
        }

        /// <summary>
        /// 自然完了時は schedule 内の全ノードに再生終了を通知する
        /// </summary>
        [Test]
        public void Tick_CallsEndPlaybackForScheduledNodesOnNaturalCompletion() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 1.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);

            player.Play();
            player.Tick(1.0f);

            Assert.That(actionNode.EndPlaybackCount, Is.EqualTo(1));
            Assert.That(actionNode.LastEndPlaybackSeed, Is.EqualTo(actionNode.LastSeed));
        }

        /// <summary>
        /// Stop は直前に active だった non-zero duration node だけをキャンセルする
        /// </summary>
        [Test]
        public void Stop_CancelsOnlyLastActiveNonZeroDurationNodes() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "running");
            var runningNode = builder.CreateActionNode("running", 10.0f, "future");
            var futureNode = builder.CreateActionNode("future", 1.0f);
            var graphAsset = builder.CreateGraph("start", startNode, runningNode, futureNode);
            var player = CreatePlayer(graphAsset);

            var handle = player.Play();
            player.Tick(1.0f);
            player.Stop();

            Assert.That(runningNode.CancelCount, Is.EqualTo(1));
            Assert.That(runningNode.LastCancelSeed, Is.EqualTo(runningNode.LastSeed));
            Assert.That(futureNode.EvaluateCount, Is.EqualTo(0));
            Assert.That(futureNode.CancelCount, Is.EqualTo(0));
            Assert.That(runningNode.EndPlaybackCount, Is.EqualTo(1));
            Assert.That(futureNode.EndPlaybackCount, Is.EqualTo(1));
            Assert.IsTrue(handle.IsDone);
            Assert.IsTrue(handle.IsInterrupted);
            Assert.IsFalse(handle.IsCompleted);
        }

        /// <summary>
        /// 自然完了した PlayHandle は完了状態になる
        /// </summary>
        [Test]
        public void Tick_CompletesPlayHandleOnNaturalCompletion() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 1.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);

            var handle = player.Play();
            player.Tick(1.0f);

            Assert.That(actionNode.CancelCount, Is.EqualTo(0));
            Assert.IsTrue(handle.IsDone);
            Assert.IsTrue(handle.IsCompleted);
            Assert.IsFalse(handle.IsInterrupted);
        }

        /// <summary>
        /// PlayHandle の Complete は最終フレームを評価して自然完了状態にする
        /// </summary>
        [Test]
        public void PlayHandle_CompleteEvaluatesFinalFrameAndCompletesPlayback() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "first");
            var firstNode = builder.CreateActionNode("first", 1.0f, "second");
            var secondNode = builder.CreateActionNode("second", 2.0f);
            var graphAsset = builder.CreateGraph("start", startNode, firstNode, secondNode);
            var player = CreatePlayer(graphAsset);

            var handle = player.Play();
            player.Tick(0.5f);
            var completed = handle.Complete();

            Assert.IsTrue(completed);
            Assert.That(player.CurrentTime, Is.EqualTo(player.Duration).Within(0.0001f));
            Assert.That(player.State, Is.EqualTo(AnimationGraphPlayerState.Stopped));
            Assert.That(firstNode.LastLocalTime, Is.EqualTo(1.0f).Within(0.0001f));
            Assert.That(secondNode.LastLocalTime, Is.EqualTo(2.0f).Within(0.0001f));
            Assert.That(firstNode.CancelCount, Is.EqualTo(0));
            Assert.That(secondNode.CancelCount, Is.EqualTo(0));
            Assert.That(firstNode.EndPlaybackCount, Is.EqualTo(1));
            Assert.That(secondNode.EndPlaybackCount, Is.EqualTo(1));
            Assert.IsTrue(handle.IsDone);
            Assert.IsTrue(handle.IsCompleted);
            Assert.IsFalse(handle.IsInterrupted);
        }

        /// <summary>
        /// 完了済み PlayHandle の Complete は何もしない
        /// </summary>
        [Test]
        public void PlayHandle_CompleteReturnsFalseWhenHandleIsDone() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 1.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);

            var handle = player.Play();
            player.Tick(1.0f);
            var completed = handle.Complete();

            Assert.IsFalse(completed);
            Assert.That(actionNode.EndPlaybackCount, Is.EqualTo(1));
        }

        /// <summary>
        /// PlayHandle は await で自然完了結果を返す
        /// </summary>
        /// <returns>テスト task</returns>
        [Test]
        public async Task PlayHandle_AwaitReturnsTrueWhenPlaybackCompletes() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 1.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);

            var handle = player.Play();
            var completionTask = AwaitHandle(handle);
            Assert.IsFalse(completionTask.IsCompleted);
            player.Tick(1.0f);
            var completed = await completionTask;

            Assert.IsTrue(completed);
        }

        /// <summary>
        /// PlayHandle は await で中断結果を返す
        /// </summary>
        /// <returns>テスト task</returns>
        [Test]
        public async Task PlayHandle_AwaitReturnsFalseWhenPlaybackInterrupted() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 10.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);

            var handle = player.Play();
            var completionTask = AwaitHandle(handle);
            Assert.IsFalse(completionTask.IsCompleted);
            player.Tick(1.0f);
            player.Stop();
            var completed = await completionTask;

            Assert.IsFalse(completed);
        }

        /// <summary>
        /// PlayHandle は IEnumerator として再生完了まで待機できる
        /// </summary>
        [Test]
        public void PlayHandle_IEnumeratorWaitsUntilPlaybackCompletes() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 1.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);

            var enumerator = (IEnumerator)player.Play();

            Assert.IsTrue(enumerator.MoveNext());
            player.Tick(1.0f);
            Assert.IsFalse(enumerator.MoveNext());
        }

        /// <summary>
        /// AnimationGraphPlayer を生成
        /// </summary>
        /// <param name="graphAsset">設定する graph asset</param>
        /// <returns>生成した player</returns>
        private AnimationGraphPlayer CreatePlayer(AnimationGraphAsset graphAsset) {
            var player = new AnimationGraphPlayer();
            player.SetGraph(graphAsset);
            player.SetContext(new TestAnimationGraphContext());
            return player;
        }

        /// <summary>
        /// PlayHandle の await 結果を Task として取得
        /// </summary>
        /// <param name="handle">待機対象 handle</param>
        /// <returns>await 結果 task</returns>
        private async Task<bool> AwaitHandle(AnimationGraphPlayHandle handle) {
            return await handle;
        }
    }
}
