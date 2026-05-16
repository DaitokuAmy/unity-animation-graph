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
        }

        /// <summary>
        /// スケジュールを再構築
        /// </summary>
        /// <param name="overrideSeed">グラフのシードを一時的に上書きする値</param>
        public void RebuildSchedule(int? overrideSeed = null) {
            EnsureReady();
            _schedule = _scheduler.BuildSchedule(_context, overrideSeed);
            _currentTime = Mathf.Clamp(_currentTime, 0.0f, Duration);
        }

        /// <summary>
        /// 再生を開始
        /// </summary>
        /// <returns>再生完了を待機する handle</returns>
        public AnimationGraphPlayHandle Play() {
            EnsureSchedule();
            if (IsEndTime(_currentTime)) {
                _currentTime = 0.0f;
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
        /// 指定時刻へシークして評価
        /// </summary>
        /// <param name="time">シーク先時刻</param>
        public void Seek(float time) {
            EnsureSchedule();
            _currentTime = Mathf.Clamp(time, 0.0f, Duration);
            EvaluateCurrentTime(_currentTime, false, _currentTime);
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
            EvaluateCurrentTime(_currentTime, true, previousTime);
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
        }

        /// <summary>
        /// 現在時刻の active node を評価
        /// </summary>
        /// <param name="currentTime">評価時刻</param>
        /// <param name="includeCrossedInstantNodes">通過した duration 0 node も評価する場合は true</param>
        /// <param name="previousTime">直前時刻</param>
        private void EvaluateCurrentTime(float currentTime, bool includeCrossedInstantNodes, float previousTime) {
            _nextActiveScheduledNodes.Clear();
            var nodes = _schedule.Nodes;
            for (var i = 0; i < nodes.Count; i++) {
                var scheduledNode = nodes[i];
                if (!IsActive(scheduledNode, currentTime, includeCrossedInstantNodes, previousTime)) {
                    continue;
                }

                var executor = (INodeExecutor)scheduledNode.Node;
                var localTime = CalculateLocalTime(scheduledNode, currentTime);
                executor.Evaluate(scheduledNode.Seed, localTime, scheduledNode.Duration, _context);
                if (scheduledNode.Duration > TimeComparisonEpsilon) {
                    _nextActiveScheduledNodes.Add(scheduledNode);
                }
            }

            var activeScheduledNodes = _activeScheduledNodes;
            _activeScheduledNodes = _nextActiveScheduledNodes;
            _nextActiveScheduledNodes = activeScheduledNodes;
        }

        /// <summary>
        /// 指定ノードが現在時刻で active か判定
        /// </summary>
        /// <param name="scheduledNode">判定対象の scheduled node</param>
        /// <param name="currentTime">判定時刻</param>
        /// <param name="includeCrossedInstantNodes">通過した duration 0 node も評価する場合は true</param>
        /// <param name="previousTime">直前時刻</param>
        /// <returns>active の場合は true</returns>
        private bool IsActive(ScheduledNode scheduledNode, float currentTime, bool includeCrossedInstantNodes, float previousTime) {
            if (scheduledNode.Duration <= TimeComparisonEpsilon) {
                if (Mathf.Abs(currentTime - scheduledNode.StartTime) <= TimeComparisonEpsilon) {
                    return true;
                }

                return includeCrossedInstantNodes
                    && previousTime - TimeComparisonEpsilon <= scheduledNode.StartTime
                    && scheduledNode.StartTime <= currentTime + TimeComparisonEpsilon;
            }

            return scheduledNode.StartTime - TimeComparisonEpsilon <= currentTime
                && currentTime <= scheduledNode.EndTime + TimeComparisonEpsilon;
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
        /// 指定時刻が終了時刻か判定
        /// </summary>
        /// <param name="time">判定対象時刻</param>
        /// <returns>終了時刻の場合は true</returns>
        private bool IsEndTime(float time) {
            return Mathf.Abs(time - Duration) <= TimeComparisonEpsilon;
        }
    }
}
