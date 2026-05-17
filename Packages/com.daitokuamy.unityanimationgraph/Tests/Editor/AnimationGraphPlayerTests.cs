using System.Collections;
using System.Collections.Generic;
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
        /// Seek は現在の評価状態を戻してから 0 秒から指定時刻までを順方向に評価する
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
            Assert.That(firstNode.EvaluateCount, Is.EqualTo(3));
            Assert.That(firstNode.LastLocalTime, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(secondNode.EvaluateCount, Is.EqualTo(2));
            Assert.That(secondNode.LastLocalTime, Is.EqualTo(0.0f).Within(0.0001f));
        }

        /// <summary>
        /// Seek は直前時刻までに評価済みの node を逆順に初期 localTime へ戻す
        /// </summary>
        [Test]
        public void Seek_ResetsEvaluatedNodesToStartInReverseOrder() {
            using var builder = new AnimationGraphTestBuilder();
            var events = new List<string>();
            var startNode = builder.CreateStartNode("start", "first");
            var firstNode = builder.CreateActionNode("first", 1.0f, "second");
            var secondNode = builder.CreateActionNode("second", 2.0f);
            firstNode.ConfigureEvents(events, "first");
            secondNode.ConfigureEvents(events, "second");
            var graphAsset = builder.CreateGraph("start", startNode, firstNode, secondNode);
            var player = CreatePlayer(graphAsset);

            player.Seek(2.0f);
            events.Clear();

            player.Seek(0.5f);

            Assert.That(events[0], Is.EqualTo("second.Evaluate"));
            Assert.That(events[1], Is.EqualTo("first.Evaluate"));
            Assert.That(secondNode.LastLocalTime, Is.EqualTo(0.0f).Within(0.0001f));
            Assert.That(firstNode.LastLocalTime, Is.EqualTo(0.5f).Within(0.0001f));
        }

        /// <summary>
        /// SeekFromInitialState は直前時刻からの巻き戻し評価を行わない
        /// </summary>
        [Test]
        public void SeekFromInitialState_DoesNotResetEvaluatedNodesFromCurrentTime() {
            using var builder = new AnimationGraphTestBuilder();
            var events = new List<string>();
            var startNode = builder.CreateStartNode("start", "first");
            var firstNode = builder.CreateActionNode("first", 1.0f, "second");
            var secondNode = builder.CreateActionNode("second", 2.0f);
            firstNode.ConfigureEvents(events, "first");
            secondNode.ConfigureEvents(events, "second");
            var graphAsset = builder.CreateGraph("start", startNode, firstNode, secondNode);
            var player = CreatePlayer(graphAsset);

            player.Seek(2.0f);
            events.Clear();

            player.SeekFromInitialState(0.5f);

            Assert.That(events, Is.EqualTo(new[] { "first.Enter", "first.Evaluate" }));
            Assert.That(player.CurrentTime, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(firstNode.LastLocalTime, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(secondNode.LastLocalTime, Is.EqualTo(1.0f).Within(0.0001f));
        }

        /// <summary>
        /// RebuildSchedule は GraphAsset の構造変更を反映する
        /// </summary>
        [Test]
        public void RebuildSchedule_RefreshesGraphStructure() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "first");
            var firstNode = builder.CreateActionNode("first", 1.0f);
            var graphAsset = builder.CreateGraph("start", startNode, firstNode);
            var player = CreatePlayer(graphAsset);

            player.RebuildSchedule();
            Assert.That(player.Duration, Is.EqualTo(1.0f).Within(0.0001f));

            var secondNode = builder.CreateActionNode("second", 2.0f);
            builder.SetNextNodeIds(firstNode, "second");
            builder.SetNodes(graphAsset, startNode, firstNode, secondNode);

            player.RebuildSchedule();
            player.Seek(2.0f);

            Assert.That(player.Duration, Is.EqualTo(3.0f).Within(0.0001f));
            Assert.That(secondNode.EvaluateCount, Is.EqualTo(1));
            Assert.That(secondNode.LastLocalTime, Is.EqualTo(1.0f).Within(0.0001f));
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
        /// Tick は node が active になったときに Enter を一度だけ呼ぶ
        /// </summary>
        [Test]
        public void Tick_CallsEnterOnceWhenNodeBecomesActive() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 1.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);

            player.Play();
            Assert.That(actionNode.EnterCount, Is.EqualTo(0));

            player.Tick(0.5f);
            player.Pause();
            player.Play();
            player.Tick(0.25f);

            Assert.That(actionNode.EnterCount, Is.EqualTo(1));
        }

        /// <summary>
        /// Tick は node が自然終了したときに Exit を呼ぶ
        /// </summary>
        [Test]
        public void Tick_CallsExitWhenNodeNaturallyEnds() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 1.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);

            player.Play();
            player.Tick(1.0f);

            Assert.That(actionNode.EnterCount, Is.EqualTo(1));
            Assert.That(actionNode.ExitCount, Is.EqualTo(1));
            Assert.That(actionNode.LastExitSeed, Is.EqualTo(actionNode.LastSeed));
        }

        /// <summary>
        /// Enter シグナルは node の Enter と Evaluate より前に通知される
        /// </summary>
        [Test]
        public void Tick_DispatchesEnterSignalsBeforeNodeProcessing() {
            using var builder = new AnimationGraphTestBuilder();
            var events = new List<string>();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 1.0f);
            actionNode.ConfigureEvents(events, "node");
            var enterSignal = builder.CreateSignal("enter", "signal.Enter", events);
            builder.SetEnterSignals(actionNode, enterSignal);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);

            player.Play();
            player.Tick(0.5f);

            CollectionAssert.AreEqual(new[] { "signal.Enter", "node.Enter", "node.Evaluate" }, events);
            Assert.That(enterSignal.DispatchCount, Is.EqualTo(1));
            Assert.That(enterSignal.LastSeed, Is.EqualTo(actionNode.LastSeed));
        }

        /// <summary>
        /// Exit シグナルは node の Evaluate と Exit より後に通知される
        /// </summary>
        [Test]
        public void Tick_DispatchesExitSignalsAfterNodeProcessing() {
            using var builder = new AnimationGraphTestBuilder();
            var events = new List<string>();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 1.0f);
            actionNode.ConfigureEvents(events, "node");
            var exitSignal = builder.CreateSignal("exit", "signal.Exit", events);
            builder.SetExitSignals(actionNode, exitSignal);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);

            player.Play();
            player.Tick(1.0f);

            CollectionAssert.AreEqual(new[] { "node.Enter", "node.Evaluate", "node.Exit", "signal.Exit" }, events);
            Assert.That(exitSignal.DispatchCount, Is.EqualTo(1));
            Assert.That(exitSignal.LastSeed, Is.EqualTo(actionNode.LastSeed));
        }

        /// <summary>
        /// Signal 発火時に購読 callback を呼び出す
        /// </summary>
        [Test]
        public void Tick_NotifiesSignalSubscribers() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 1.0f);
            var enterSignal = builder.CreateSignal("enter");
            builder.SetEnterSignals(actionNode, enterSignal);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);
            var receivedSignals = new List<TestSignal>();
            player.SubscribeSignal<TestSignal>(receivedSignals.Add);

            player.Play();
            player.Tick(0.5f);

            Assert.That(receivedSignals.Count, Is.EqualTo(1));
            Assert.That(receivedSignals[0], Is.SameAs(enterSignal));
        }

        /// <summary>
        /// ClearSignalSubscriptions は Signal 発火通知の購読を解除する
        /// </summary>
        [Test]
        public void ClearSignalSubscriptions_RemovesSignalSubscribers() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 1.0f);
            var enterSignal = builder.CreateSignal("enter");
            builder.SetEnterSignals(actionNode, enterSignal);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);
            var receivedSignals = new List<TestSignal>();
            player.SubscribeSignal<TestSignal>(receivedSignals.Add);
            player.ClearSignalSubscriptions();

            player.Play();
            player.Tick(0.5f);

            Assert.That(receivedSignals, Is.Empty);
        }

        /// <summary>
        /// Seek は Signal を通知しない
        /// </summary>
        [Test]
        public void Seek_DoesNotDispatchSignalsOrNotifySubscribers() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 1.0f);
            var enterSignal = builder.CreateSignal("enter");
            var exitSignal = builder.CreateSignal("exit");
            builder.SetEnterSignals(actionNode, enterSignal);
            builder.SetExitSignals(actionNode, exitSignal);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);
            var receivedSignals = new List<TestSignal>();
            player.SubscribeSignal<TestSignal>(receivedSignals.Add);

            player.Seek(1.0f);

            Assert.That(enterSignal.DispatchCount, Is.EqualTo(0));
            Assert.That(exitSignal.DispatchCount, Is.EqualTo(0));
            Assert.That(receivedSignals, Is.Empty);
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
            Assert.That(runningNode.ExitCount, Is.EqualTo(0));
            Assert.That(futureNode.ExitCount, Is.EqualTo(0));
            Assert.IsTrue(handle.IsDone);
            Assert.IsTrue(handle.IsInterrupted);
            Assert.IsFalse(handle.IsCompleted);
        }

        /// <summary>
        /// Stop は Seek で記録された Preview 用 active node をクリアする
        /// </summary>
        [Test]
        public void Stop_ClearsPreviewActiveNodesAfterSeek() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 2.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);

            player.Seek(0.5f);
            player.Stop();
            player.Play();
            player.Tick(0.5f);

            Assert.That(actionNode.EnterCount, Is.EqualTo(2));
            Assert.That(actionNode.CancelCount, Is.EqualTo(0));
        }

        /// <summary>
        /// RebuildSchedule は再生中に active だった node をキャンセルしてから記録をクリアする
        /// </summary>
        [Test]
        public void RebuildSchedule_CancelsActiveNodesDuringPlayback() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 10.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);
            var handle = player.Play();
            player.Tick(1.0f);

            player.RebuildSchedule();

            Assert.That(actionNode.CancelCount, Is.EqualTo(1));
            Assert.That(player.State, Is.EqualTo(AnimationGraphPlayerState.Playing));
            Assert.IsFalse(handle.IsDone);
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
            Assert.That(firstNode.ExitCount, Is.EqualTo(1));
            Assert.That(secondNode.ExitCount, Is.EqualTo(1));
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
            Assert.That(actionNode.ExitCount, Is.EqualTo(1));
        }

        /// <summary>
        /// PlayHandle の Pause と Resume は対象の再生を一時停止、再開する
        /// </summary>
        [Test]
        public void PlayHandle_PauseAndResumeControlsPlayback() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 1.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);

            var handle = player.Play();
            var paused = handle.Pause();
            player.Tick(0.5f);

            Assert.IsTrue(paused);
            Assert.That(player.State, Is.EqualTo(AnimationGraphPlayerState.Paused));
            Assert.That(player.CurrentTime, Is.EqualTo(0.0f).Within(0.0001f));
            Assert.That(actionNode.EvaluateCount, Is.EqualTo(0));

            var resumed = handle.Resume();
            player.Tick(0.5f);

            Assert.IsTrue(resumed);
            Assert.That(player.State, Is.EqualTo(AnimationGraphPlayerState.Playing));
            Assert.That(player.CurrentTime, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(actionNode.LastLocalTime, Is.EqualTo(0.5f).Within(0.0001f));
        }

        /// <summary>
        /// PlayHandle の Stop は対象の再生を中断する
        /// </summary>
        [Test]
        public void PlayHandle_StopInterruptsPlayback() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 10.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);

            var handle = player.Play();
            player.Tick(1.0f);
            var stopped = handle.Stop();

            Assert.IsTrue(stopped);
            Assert.That(player.State, Is.EqualTo(AnimationGraphPlayerState.Stopped));
            Assert.That(player.CurrentTime, Is.EqualTo(0.0f).Within(0.0001f));
            Assert.That(actionNode.CancelCount, Is.EqualTo(1));
            Assert.That(actionNode.LastCancelSeed, Is.EqualTo(actionNode.LastSeed));
            Assert.IsTrue(handle.IsDone);
            Assert.IsTrue(handle.IsInterrupted);
            Assert.IsFalse(handle.IsCompleted);
        }

        /// <summary>
        /// 古い PlayHandle は現在の再生を操作しない
        /// </summary>
        [Test]
        public void PlayHandle_OperationsReturnFalseWhenHandleIsOld() {
            using var builder = new AnimationGraphTestBuilder();
            var startNode = builder.CreateStartNode("start", "action");
            var actionNode = builder.CreateActionNode("action", 10.0f);
            var graphAsset = builder.CreateGraph("start", startNode, actionNode);
            var player = CreatePlayer(graphAsset);

            var firstHandle = player.Play();
            Assert.IsTrue(firstHandle.Stop());

            var secondHandle = player.Play();
            Assert.IsTrue(secondHandle.Pause());

            Assert.IsFalse(firstHandle.Pause());
            Assert.IsFalse(firstHandle.Resume());
            Assert.IsFalse(firstHandle.Stop());
            Assert.That(player.State, Is.EqualTo(AnimationGraphPlayerState.Paused));
            Assert.IsTrue(secondHandle.Resume());
            Assert.That(player.State, Is.EqualTo(AnimationGraphPlayerState.Playing));
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
