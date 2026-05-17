using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// AnimationGraphSchedule を再生するランタイムプレイヤー
    /// </summary>
    public sealed class AnimationGraphPlayer {
        /// <summary>同時刻として扱う時刻差</summary>
        private const float TimeComparisonEpsilon = 0.00001f;

        /// <summary>
        /// 再生 handle の内部状態
        /// </summary>
        private enum PlayStatus {
            /// <summary>再生 handle がない状態</summary>
            None,
            /// <summary>再生中または一時停止中</summary>
            Playing,
            /// <summary>最後まで再生完了</summary>
            Completed,
            /// <summary>再生中断</summary>
            Interrupted,
        }

        private readonly AnimationGraphScheduler _scheduler = new();
        private readonly Dictionary<Type, List<Action<Signal>>> _signalSubscriptionsByType = new();
        private readonly Dictionary<int, object> _tweenBaseValuesByStableOrder = new();

        private List<ScheduledNode> _activeScheduledNodes = new();
        private List<ScheduledNode> _nextActiveScheduledNodes = new();
        private AnimationGraphAsset _graphAsset;
        private IAnimationGraphContext _context;
        private AnimationGraphSchedule _schedule;
        private Action _playContinuation;
        private PlayStatus _playStatus;
        private AnimationGraphPlayerState _state;
        private float _currentTime;
        private int _playVersion;

        /// <summary>設定中の AnimationGraphAsset</summary>
        public AnimationGraphAsset GraphAsset => _graphAsset;
        /// <summary>設定中の評価コンテキスト</summary>
        public IAnimationGraphContext Context => _context;
        /// <summary>構築済みスケジュール</summary>
        public AnimationGraphSchedule Schedule => _schedule;
        /// <summary>現在の再生状態</summary>
        public AnimationGraphPlayerState State => _state;
        /// <summary>再生中の場合は true</summary>
        public bool IsPlaying => _state == AnimationGraphPlayerState.Playing;
        /// <summary>現在時刻</summary>
        public float CurrentTime => _currentTime;
        /// <summary>スケジュール全体の長さ</summary>
        public float Duration => _schedule?.Duration ?? 0.0f;
        /// <summary>Tick の deltaTime に乗算する再生速度</summary>
        public float TimeScale { get; set; } = 1.0f;

        /// <summary>
        /// 再生する AnimationGraphAsset を設定
        /// </summary>
        /// <param name="graphAsset">再生する AnimationGraphAsset。null の場合は設定を解除</param>
        public void SetGraph(AnimationGraphAsset graphAsset) {
            if (graphAsset != null) {
                _scheduler.SetGraph(graphAsset);
            }

            CompleteCurrentPlay(PlayStatus.Interrupted);
            _graphAsset = graphAsset;
            _schedule = null;
            _currentTime = 0.0f;
            _state = AnimationGraphPlayerState.Stopped;
            ClearActiveNodes();
        }

        /// <summary>
        /// 評価コンテキストを設定
        /// </summary>
        /// <param name="context">評価コンテキスト</param>
        public void SetContext(IAnimationGraphContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            CompleteCurrentPlay(PlayStatus.Interrupted);
            _context = context;
            _schedule = null;
            _currentTime = 0.0f;
            _state = AnimationGraphPlayerState.Stopped;
            ClearActiveNodes();
        }

        /// <summary>
        /// スケジュールを再構築
        /// </summary>
        /// <param name="overrideSeed">グラフのシードを一時的に上書きする値</param>
        public void RebuildSchedule(int? overrideSeed = null) {
            EnsureReady();
            _scheduler.SetGraph(_graphAsset);
            _schedule = _scheduler.BuildSchedule(_context, overrideSeed);
            _currentTime = Mathf.Clamp(_currentTime, 0.0f, Duration);
            ClearActiveNodes();
        }

        /// <summary>
        /// Preview 再生時に復元対象として登録すべき Property を取得
        /// </summary>
        /// <returns>登録対象の Component と SerializedProperty path の一覧</returns>
        public IEnumerable<(Component Component, string PropertyPath)> GetPreviewProperties() {
            EnsureSchedule();
            foreach (var scheduledNode in _schedule.Nodes) {
                var executor = (INodeExecutor)scheduledNode.Node;
                foreach (var previewProperty in executor.GetPreviewProperties(_context)) {
                    yield return previewProperty;
                }
            }
        }

        /// <summary>
        /// 指定した Signal 型の発火通知を購読
        /// </summary>
        /// <param name="callback">Signal 発火時に呼び出す callback</param>
        /// <typeparam name="TSignal">購読対象の Signal 型</typeparam>
        public void SubscribeSignal<TSignal>(Action<TSignal> callback) where TSignal : Signal {
            if (callback == null) {
                throw new ArgumentNullException(nameof(callback));
            }

            var signalType = typeof(TSignal);
            if (!_signalSubscriptionsByType.TryGetValue(signalType, out var callbacks)) {
                callbacks = new List<Action<Signal>>();
                _signalSubscriptionsByType.Add(signalType, callbacks);
            }

            callbacks.Add(signal => callback((TSignal)signal));
        }

        /// <summary>
        /// Signal 発火通知の購読をすべて解除
        /// </summary>
        public void ClearSignalSubscriptions() {
            _signalSubscriptionsByType.Clear();
        }

        /// <summary>
        /// 再生を開始
        /// </summary>
        /// <returns>再生完了を待機する handle</returns>
        public AnimationGraphPlayHandle Play() {
            EnsureSchedule();
            if (IsEndTime(_currentTime)) {
                _currentTime = 0.0f;
                ClearActiveNodes();
            }

            if (_playStatus != PlayStatus.Playing) {
                _playVersion++;
                _playContinuation = null;
                _playStatus = PlayStatus.Playing;
            }

            _state = AnimationGraphPlayerState.Playing;
            return new AnimationGraphPlayHandle(this, _playVersion);
        }

        /// <summary>
        /// 再生を一時停止
        /// </summary>
        public void Pause() {
            if (_state != AnimationGraphPlayerState.Playing) {
                return;
            }

            _state = AnimationGraphPlayerState.Paused;
        }

        /// <summary>
        /// 再生を停止
        /// </summary>
        public void Stop() {
            _state = AnimationGraphPlayerState.Stopped;
            _currentTime = 0.0f;
            CompleteCurrentPlay(PlayStatus.Interrupted);
        }

        /// <summary>
        /// 現在の評価状態を 0 秒へ戻してから、0 秒から指定時刻までを順方向に評価
        /// </summary>
        /// <param name="time">評価する時刻</param>
        public void Seek(float time) {
            EnsureSchedule();
            ResetEvaluatedNodesToStart(_currentTime);
            ClearActiveNodes();
            _currentTime = Mathf.Clamp(time, 0.0f, Duration);
            EvaluateCurrentTime(_currentTime, true, 0.0f, false);

            if (_state == AnimationGraphPlayerState.Playing && IsEndTime(_currentTime)) {
                _state = AnimationGraphPlayerState.Stopped;
                CompleteCurrentPlay(PlayStatus.Completed);
            }
        }

        /// <summary>
        /// 再生時間を進めて評価
        /// </summary>
        /// <param name="deltaTime">進める時間</param>
        public void Tick(float deltaTime) {
            if (_state != AnimationGraphPlayerState.Playing) {
                return;
            }

            EnsureSchedule();
            var previousTime = _currentTime;
            var scaledDeltaTime = Mathf.Max(0.0f, deltaTime * TimeScale);
            _currentTime = Mathf.Clamp(_currentTime + scaledDeltaTime, 0.0f, Duration);
            EvaluateCurrentTime(_currentTime, true, previousTime, true);
            if (IsEndTime(_currentTime)) {
                _state = AnimationGraphPlayerState.Stopped;
                CompleteCurrentPlay(PlayStatus.Completed);
            }
        }

        /// <summary>
        /// 指定 version の再生 handle が完了済みか判定
        /// </summary>
        /// <param name="version">再生 version</param>
        /// <returns>完了済みの場合は true</returns>
        internal bool IsPlayDone(int version) {
            return version != _playVersion || _playStatus != PlayStatus.Playing;
        }

        /// <summary>
        /// 指定 version の再生 handle が自然完了したか判定
        /// </summary>
        /// <param name="version">再生 version</param>
        /// <returns>自然完了した場合は true</returns>
        internal bool IsPlayCompleted(int version) {
            return version == _playVersion && _playStatus == PlayStatus.Completed;
        }

        /// <summary>
        /// 指定 version の再生 handle が中断されたか判定
        /// </summary>
        /// <param name="version">再生 version</param>
        /// <returns>中断された場合は true</returns>
        internal bool IsPlayInterrupted(int version) {
            return version != _playVersion || _playStatus == PlayStatus.Interrupted;
        }

        /// <summary>
        /// 指定 version の再生を最終フレームまで評価して完了
        /// </summary>
        /// <param name="version">再生 version</param>
        /// <returns>完了できた場合は true</returns>
        internal bool CompletePlay(int version) {
            if (version != _playVersion || _playStatus != PlayStatus.Playing) {
                return false;
            }

            EnsureSchedule();
            var previousTime = _currentTime;
            _currentTime = Duration;
            EvaluateCurrentTime(_currentTime, true, previousTime, true);
            _state = AnimationGraphPlayerState.Stopped;
            CompleteCurrentPlay(PlayStatus.Completed);
            return true;
        }

        /// <summary>
        /// 指定 version の再生完了 continuation を登録
        /// </summary>
        /// <param name="version">再生 version</param>
        /// <param name="continuation">再開処理</param>
        internal void RegisterPlayContinuation(int version, Action continuation) {
            if (IsPlayDone(version)) {
                continuation();
                return;
            }

            _playContinuation += continuation;
        }

        /// <summary>
        /// 再生に必要な参照が設定済みであることを検証
        /// </summary>
        private void EnsureReady() {
            if (_graphAsset == null) {
                throw new InvalidOperationException("AnimationGraphAsset is not set");
            }

            if (_context == null) {
                throw new InvalidOperationException("IAnimationGraphContext is not set");
            }
        }

        /// <summary>
        /// スケジュールが構築済みであることを保証
        /// </summary>
        private void EnsureSchedule() {
            if (_schedule != null) {
                return;
            }

            RebuildSchedule();
        }

        /// <summary>
        /// 現在の再生 handle を完了状態にする
        /// </summary>
        /// <param name="playStatus">設定する完了状態</param>
        private void CompleteCurrentPlay(PlayStatus playStatus) {
            if (_playStatus != PlayStatus.Playing) {
                return;
            }

            if (playStatus == PlayStatus.Interrupted) {
                CancelActiveNodes();
            }
            else {
                ClearActiveNodes();
            }

            _playStatus = playStatus;
            var continuation = _playContinuation;
            _playContinuation = null;
            continuation?.Invoke();
        }

        /// <summary>
        /// 直前に active だったノードへキャンセルを通知
        /// </summary>
        private void CancelActiveNodes() {
            for (var i = 0; i < _activeScheduledNodes.Count; i++) {
                var scheduledNode = _activeScheduledNodes[i];
                ((INodeExecutor)scheduledNode.Node).Cancel(scheduledNode.Seed, _context);
            }

            ClearActiveNodes();
        }

        /// <summary>
        /// active node の記録をクリア
        /// </summary>
        private void ClearActiveNodes() {
            _activeScheduledNodes.Clear();
            _nextActiveScheduledNodes.Clear();
            _tweenBaseValuesByStableOrder.Clear();
        }

        /// <summary>
        /// 指定時刻までに評価済みの node を逆順に初期 localTime で評価
        /// </summary>
        /// <param name="fromTime">巻き戻しを開始する時刻</param>
        private void ResetEvaluatedNodesToStart(float fromTime) {
            if (_schedule == null || fromTime <= TimeComparisonEpsilon) {
                return;
            }

            var nodes = _schedule.Nodes;
            for (var i = nodes.Count - 1; i >= 0; i--) {
                var scheduledNode = nodes[i];
                if (scheduledNode.StartTime > fromTime + TimeComparisonEpsilon) {
                    continue;
                }

                EvaluateScheduledNode(scheduledNode, 0.0f);
            }
        }

        /// <summary>
        /// 現在時刻の active node を評価
        /// </summary>
        /// <param name="currentTime">評価時刻</param>
        /// <param name="includeCrossedNodes">通過した node の端時刻も評価する場合は true</param>
        /// <param name="previousTime">直前時刻</param>
        /// <param name="dispatchSignals">Signal を通知する場合は true</param>
        private void EvaluateCurrentTime(float currentTime, bool includeCrossedNodes, float previousTime, bool dispatchSignals) {
            _nextActiveScheduledNodes.Clear();
            var nodes = _schedule.Nodes;
            for (var i = 0; i < nodes.Count; i++) {
                EvaluateScheduledNodeAtCurrentTime(nodes[i], currentTime, includeCrossedNodes, previousTime, dispatchSignals);
            }

            (_activeScheduledNodes, _nextActiveScheduledNodes) = (_nextActiveScheduledNodes, _activeScheduledNodes);
        }

        /// <summary>
        /// scheduled node を現在時刻または通過した端時刻で評価
        /// </summary>
        /// <param name="scheduledNode">評価対象の scheduled node</param>
        /// <param name="currentTime">評価時刻</param>
        /// <param name="includeCrossedNodes">通過した node の端時刻も評価する場合は true</param>
        /// <param name="previousTime">直前時刻</param>
        /// <param name="dispatchSignals">Signal を通知する場合は true</param>
        private void EvaluateScheduledNodeAtCurrentTime(ScheduledNode scheduledNode, float currentTime, bool includeCrossedNodes, float previousTime, bool dispatchSignals) {
            var wasActive = IsRecordedActive(scheduledNode);
            var isActive = IsActive(scheduledNode, currentTime, includeCrossedNodes, previousTime);
            var isCrossedEndTime = IsCrossedEndTime(scheduledNode, currentTime, includeCrossedNodes, previousTime);
            if (!isActive && !isCrossedEndTime) {
                return;
            }

            if (!wasActive && IsCrossedStartTime(scheduledNode, currentTime, includeCrossedNodes, previousTime)) {
                EnterScheduledNode(scheduledNode, dispatchSignals);
            }

            var localTime = isActive ? CalculateLocalTime(scheduledNode, currentTime) : scheduledNode.Duration;
            EvaluateScheduledNode(scheduledNode, localTime);

            if (ShouldExitScheduledNode(scheduledNode, currentTime, includeCrossedNodes, previousTime)) {
                ExitScheduledNode(scheduledNode, dispatchSignals);
                return;
            }

            if (ShouldRemainActive(scheduledNode, currentTime)) {
                _nextActiveScheduledNodes.Add(scheduledNode);
            }
        }

        /// <summary>
        /// scheduled node が前回評価後も active として記録されていたか判定
        /// </summary>
        /// <param name="scheduledNode">判定対象の scheduled node</param>
        /// <returns>active として記録されていた場合は true</returns>
        private bool IsRecordedActive(ScheduledNode scheduledNode) {
            return _activeScheduledNodes.Contains(scheduledNode);
        }

        /// <summary>
        /// 指定ノードが現在時刻で active か判定
        /// </summary>
        /// <param name="scheduledNode">判定対象の scheduled node</param>
        /// <param name="currentTime">判定時刻</param>
        /// <param name="includeCrossedNodes">通過した node の端時刻も評価する場合は true</param>
        /// <param name="previousTime">直前時刻</param>
        /// <returns>active の場合は true</returns>
        private bool IsActive(ScheduledNode scheduledNode, float currentTime, bool includeCrossedNodes, float previousTime) {
            if (scheduledNode.Duration <= TimeComparisonEpsilon) {
                if (Mathf.Abs(currentTime - scheduledNode.StartTime) <= TimeComparisonEpsilon) {
                    return true;
                }

                return includeCrossedNodes && ContainsTimeBetween(previousTime, currentTime, scheduledNode.StartTime);
            }

            return scheduledNode.StartTime - TimeComparisonEpsilon <= currentTime
                && currentTime <= scheduledNode.EndTime + TimeComparisonEpsilon;
        }

        /// <summary>
        /// 前回時刻と今回時刻の間で non-zero duration node の終了時刻を通過したか判定
        /// </summary>
        /// <param name="scheduledNode">判定対象の scheduled node</param>
        /// <param name="currentTime">判定時刻</param>
        /// <param name="includeCrossedNodes">通過した node の端時刻も評価する場合は true</param>
        /// <param name="previousTime">直前時刻</param>
        /// <returns>終了時刻を通過した場合は true</returns>
        private bool IsCrossedEndTime(ScheduledNode scheduledNode, float currentTime, bool includeCrossedNodes, float previousTime) {
            return includeCrossedNodes
                && scheduledNode.Duration > TimeComparisonEpsilon
                && previousTime < scheduledNode.EndTime - TimeComparisonEpsilon
                && scheduledNode.EndTime + TimeComparisonEpsilon < currentTime;
        }

        /// <summary>
        /// 前回時刻と今回時刻の間で node の開始時刻に到達したか判定
        /// </summary>
        /// <param name="scheduledNode">判定対象の scheduled node</param>
        /// <param name="currentTime">判定時刻</param>
        /// <param name="includeCrossedNodes">通過した node の端時刻も評価する場合は true</param>
        /// <param name="previousTime">直前時刻</param>
        /// <returns>開始時刻に到達した場合は true</returns>
        private bool IsCrossedStartTime(ScheduledNode scheduledNode, float currentTime, bool includeCrossedNodes, float previousTime) {
            if (Mathf.Abs(currentTime - scheduledNode.StartTime) <= TimeComparisonEpsilon) {
                return true;
            }

            return includeCrossedNodes && ContainsTimeBetween(previousTime, currentTime, scheduledNode.StartTime);
        }

        /// <summary>
        /// scheduled node を今回評価後も active として保持するか判定
        /// </summary>
        /// <param name="scheduledNode">判定対象の scheduled node</param>
        /// <param name="currentTime">評価時刻</param>
        /// <returns>active として保持する場合は true</returns>
        private bool ShouldRemainActive(ScheduledNode scheduledNode, float currentTime) {
            return scheduledNode.Duration > TimeComparisonEpsilon
                && scheduledNode.StartTime - TimeComparisonEpsilon <= currentTime
                && currentTime < scheduledNode.EndTime - TimeComparisonEpsilon;
        }

        /// <summary>
        /// scheduled node を今回評価後に終了させるか判定
        /// </summary>
        /// <param name="scheduledNode">判定対象の scheduled node</param>
        /// <param name="currentTime">評価時刻</param>
        /// <param name="includeCrossedNodes">通過した node の端時刻も評価する場合は true</param>
        /// <param name="previousTime">直前時刻</param>
        /// <returns>終了させる場合は true</returns>
        private bool ShouldExitScheduledNode(ScheduledNode scheduledNode, float currentTime, bool includeCrossedNodes, float previousTime) {
            if (scheduledNode.Duration <= TimeComparisonEpsilon) {
                return true;
            }

            return currentTime >= scheduledNode.EndTime - TimeComparisonEpsilon
                || includeCrossedNodes
                && previousTime < scheduledNode.EndTime - TimeComparisonEpsilon
                && scheduledNode.EndTime <= currentTime + TimeComparisonEpsilon;
        }

        /// <summary>
        /// 指定時刻が 2 つの時刻の間に含まれるか判定
        /// </summary>
        /// <param name="from">片方の時刻</param>
        /// <param name="to">もう片方の時刻</param>
        /// <param name="time">判定対象時刻</param>
        /// <returns>含まれる場合は true</returns>
        private bool ContainsTimeBetween(float from, float to, float time) {
            return Mathf.Min(from, to) - TimeComparisonEpsilon <= time
                && time <= Mathf.Max(from, to) + TimeComparisonEpsilon;
        }

        /// <summary>
        /// scheduled node の local time を計算
        /// </summary>
        /// <param name="scheduledNode">対象の scheduled node</param>
        /// <param name="currentTime">評価時刻</param>
        /// <returns>local time</returns>
        private float CalculateLocalTime(ScheduledNode scheduledNode, float currentTime) {
            if (scheduledNode.Duration <= TimeComparisonEpsilon) {
                return 0.0f;
            }

            return Mathf.Clamp(currentTime - scheduledNode.StartTime, 0.0f, scheduledNode.Duration);
        }

        /// <summary>
        /// scheduled node を指定 local time で評価
        /// </summary>
        /// <param name="scheduledNode">評価対象の scheduled node</param>
        /// <param name="localTime">評価に使用する local time</param>
        private void EvaluateScheduledNode(ScheduledNode scheduledNode, float localTime) {
            var executor = (INodeExecutor)scheduledNode.Node;
            if (scheduledNode.Node is ITweenNodeExecutor tweenExecutor) {
                if (!_tweenBaseValuesByStableOrder.TryGetValue(scheduledNode.StableOrder, out var baseValue)) {
                    baseValue = tweenExecutor.CaptureBaseValue(scheduledNode.Seed, _context);
                    _tweenBaseValuesByStableOrder[scheduledNode.StableOrder] = baseValue;
                }

                tweenExecutor.Evaluate(scheduledNode.Seed, localTime, scheduledNode.Duration, _context, baseValue);
                return;
            }

            executor.Evaluate(scheduledNode.Seed, localTime, scheduledNode.Duration, _context);
        }

        /// <summary>
        /// scheduled node の開始処理を実行
        /// </summary>
        /// <param name="scheduledNode">開始する scheduled node</param>
        /// <param name="dispatchSignals">Signal を通知する場合は true</param>
        private void EnterScheduledNode(ScheduledNode scheduledNode, bool dispatchSignals) {
            if (dispatchSignals) {
                DispatchSignals(scheduledNode.Node.EnterSignals, scheduledNode.Seed);
            }

            if (scheduledNode.Node is ITweenNodeExecutor tweenExecutor) {
                _tweenBaseValuesByStableOrder[scheduledNode.StableOrder] = tweenExecutor.CaptureBaseValue(scheduledNode.Seed, _context);
            }

            ((INodeExecutor)scheduledNode.Node).Enter(scheduledNode.Seed, _context);
        }

        /// <summary>
        /// scheduled node の終了処理を実行
        /// </summary>
        /// <param name="scheduledNode">終了する scheduled node</param>
        /// <param name="dispatchSignals">Signal を通知する場合は true</param>
        private void ExitScheduledNode(ScheduledNode scheduledNode, bool dispatchSignals) {
            ((INodeExecutor)scheduledNode.Node).Exit(scheduledNode.Seed, _context);
            if (dispatchSignals) {
                DispatchSignals(scheduledNode.Node.ExitSignals, scheduledNode.Seed);
            }
        }

        /// <summary>
        /// シグナル一覧を通知
        /// </summary>
        /// <param name="signals">通知するシグナル一覧</param>
        /// <param name="seed">評価に使用するシード</param>
        private void DispatchSignals(IReadOnlyList<Signal> signals, int seed) {
            for (var i = 0; i < signals.Count; i++) {
                var signal = signals[i];
                if (signal == null) {
                    continue;
                }

                ((ISignalExecutor)signal).Dispatch(seed, _context);
                NotifySignalSubscribers(signal);
            }
        }

        /// <summary>
        /// Signal 購読者へ通知
        /// </summary>
        /// <param name="signal">通知する Signal</param>
        private void NotifySignalSubscribers(Signal signal) {
            if (!_signalSubscriptionsByType.TryGetValue(signal.GetType(), out var callbacks)) {
                return;
            }

            for (var i = 0; i < callbacks.Count; i++) {
                callbacks[i].Invoke(signal);
            }
        }

        /// <summary>
        /// 指定時刻が終了時刻か判定
        /// </summary>
        /// <param name="time">判定対象時刻</param>
        /// <returns>終了時刻の場合は true</returns>
        private bool IsEndTime(float time) {
            return Mathf.Abs(time - Duration) <= TimeComparisonEpsilon;
        }
    }
}
