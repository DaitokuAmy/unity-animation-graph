using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Editor Preview の seek 位置を表示、操作する Timeline view
    /// </summary>
    internal sealed class AnimationGraphPreviewTimelineView : VisualElement {
        private const int DefaultFrameRate = 30;
        private const int MaxTickCount = 160;
        private const int TimeLabelApproximateWidth = 72;
        private const float TimeEpsilon = 0.0001f;
        private const float TimelineHeight = 48.0f;
        private const float TimeLabelHeight = 16.0f;
        private const float TimeLabelWidth = 48.0f;
        private const float TrackTop = 25.0f;
        private const float TrackBottom = 5.0f;
        private const float MajorTickTop = 20.0f;
        private const float MinorTickTop = 31.0f;
        private const float MaxFrameCellWidth = 18.0f;
        private const float PlayheadWidth = 3.0f;
        private const float TimelineLeftPadding = 28.0f;
        private const float TimelineRightPadding = 4.0f;
        private const double DragSeekDispatchInterval = 1.0 / 30.0;

        private static readonly Color TrackColor = new(0.12f, 0.12f, 0.12f);
        private static readonly Color TrackBorderColor = new(0.30f, 0.30f, 0.30f);
        private static readonly Color ElapsedColor = new(0.65f, 0.10f, 0.08f, 0.35f);
        private static readonly Color PlayheadColor = new(1.0f, 0.12f, 0.08f);
        private static readonly Color MajorTickColor = new(0.62f, 0.62f, 0.62f);
        private static readonly Color MinorTickColor = new(0.38f, 0.38f, 0.38f);
        private static readonly Color InactiveMajorTickColor = new(0.30f, 0.30f, 0.30f);
        private static readonly Color InactiveMinorTickColor = new(0.22f, 0.22f, 0.22f);
        private static readonly Color TimeLabelColor = new(0.70f, 0.70f, 0.70f);

        private readonly VisualElement _track;
        private readonly VisualElement _elapsedFill;
        private readonly VisualElement _timeLabelContainer;
        private readonly VisualElement _tickContainer;
        private readonly VisualElement _playhead;

        private float _currentTime;
        private float _duration;
        private float _timelineDuration;
        private float _contentWidth;
        private float _lastDispatchedSeekTime = float.NaN;
        private int _frameRate = DefaultFrameRate;
        private VisualElement _dragRoot;
        private int _dragPointerId = PointerId.invalidPointerId;
        private bool _isDragging;
        private bool _isSeekable;
        private double _lastSeekDispatchTime;

        /// <summary>Seek 位置の変更を要求したときに発火</summary>
        public event Action<float> SeekRequested;

        /// <summary>
        /// AnimationGraphPreviewTimelineView を作成
        /// </summary>
        public AnimationGraphPreviewTimelineView() {
            focusable = true;
            tooltip = "Preview timeline";
            style.flexGrow = 1.0f;
            style.flexShrink = 1.0f;
            style.minWidth = 160.0f;
            style.height = TimelineHeight;
            style.position = Position.Relative;
            style.marginLeft = 4.0f;
            style.marginRight = 4.0f;

            _track = CreateTrack();
            _elapsedFill = CreateElapsedFill();
            _timeLabelContainer = CreateOverlay();
            _tickContainer = CreateOverlay();
            _playhead = CreatePlayhead();

            _track.Add(_elapsedFill);
            Add(_track);
            Add(_tickContainer);
            Add(_timeLabelContainer);
            Add(_playhead);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        /// <summary>
        /// Timeline 表示状態を通知なしで更新
        /// </summary>
        /// <param name="currentTime">現在時刻</param>
        /// <param name="duration">総時間</param>
        /// <param name="frameRate">表示と frame seek に使用する frame rate</param>
        public void SetPreviewState(float currentTime, float duration, int frameRate) {
            var nextCurrentTime = IsFinite(currentTime) ? currentTime : 0.0f;
            var nextDuration = IsFinite(duration) ? Mathf.Max(0.0f, duration) : 0.0f;
            var nextFrameRate = Mathf.Max(1, frameRate);
            var shouldRebuildTicks = Mathf.Abs(_duration - nextDuration) > TimeEpsilon || _frameRate != nextFrameRate;

            _duration = nextDuration;
            _frameRate = nextFrameRate;
            _timelineDuration = GetFrameAlignedDuration(_duration, _frameRate);
            if (shouldRebuildTicks) {
                RebuildTicks();
            }

            SetTimeWithoutNotify(nextCurrentTime);
        }

        /// <summary>
        /// Timeline の seek 操作可否を設定
        /// </summary>
        /// <param name="isSeekable">Seek 操作を許可する場合は true</param>
        public void SetSeekable(bool isSeekable) {
            if (_isSeekable == isSeekable) {
                return;
            }

            _isSeekable = isSeekable;
            if (!_isSeekable && _isDragging) {
                EndDrag(false);
            }
        }

        /// <summary>
        /// 実行中の Timeline drag を通知なしで終了
        /// </summary>
        public void CancelDrag() {
            if (!_isDragging) {
                return;
            }

            EndDrag(false);
        }

        private static VisualElement CreateTrack() {
            var track = new VisualElement {
                pickingMode = PickingMode.Ignore,
            };
            track.style.position = Position.Absolute;
            track.style.left = TimelineLeftPadding;
            track.style.right = TimelineRightPadding;
            track.style.top = TrackTop;
            track.style.bottom = TrackBottom;
            track.style.backgroundColor = TrackColor;
            track.style.borderTopWidth = 1.0f;
            track.style.borderRightWidth = 1.0f;
            track.style.borderBottomWidth = 1.0f;
            track.style.borderLeftWidth = 1.0f;
            track.style.borderTopColor = TrackBorderColor;
            track.style.borderRightColor = TrackBorderColor;
            track.style.borderBottomColor = TrackBorderColor;
            track.style.borderLeftColor = TrackBorderColor;
            return track;
        }

        private static VisualElement CreateElapsedFill() {
            var fill = new VisualElement {
                pickingMode = PickingMode.Ignore,
            };
            fill.style.position = Position.Absolute;
            fill.style.left = 0.0f;
            fill.style.top = 0.0f;
            fill.style.bottom = 0.0f;
            fill.style.width = Length.Percent(0.0f);
            fill.style.backgroundColor = ElapsedColor;
            return fill;
        }

        private static VisualElement CreateOverlay() {
            var overlay = new VisualElement {
                pickingMode = PickingMode.Ignore,
            };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0.0f;
            overlay.style.right = 0.0f;
            overlay.style.top = 0.0f;
            overlay.style.bottom = 0.0f;
            return overlay;
        }

        private static VisualElement CreatePlayhead() {
            var playhead = new VisualElement {
                pickingMode = PickingMode.Ignore,
            };
            playhead.style.position = Position.Absolute;
            playhead.style.top = 18.0f;
            playhead.style.bottom = 3.0f;
            playhead.style.width = PlayheadWidth;
            playhead.style.backgroundColor = PlayheadColor;
            return playhead;
        }

        private void SetTimeWithoutNotify(float currentTime) {
            var nextCurrentTime = IsFinite(currentTime) ? currentTime : 0.0f;
            var timelineDuration = IsFinite(_timelineDuration) ? Mathf.Max(0.0f, _timelineDuration) : 0.0f;
            _currentTime = Mathf.Clamp(nextCurrentTime, 0.0f, timelineDuration);
            UpdatePlayhead();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt) {
            RebuildTicks();
            UpdatePlayhead();
        }

        private void OnPointerDown(PointerDownEvent evt) {
            if (evt.button != 0 || !_isSeekable || !enabledInHierarchy) {
                return;
            }

            _isDragging = true;
            _dragPointerId = evt.pointerId;
            RegisterDragCallbacks();
            SeekFromLocalX(evt.localPosition.x, true);
            evt.StopPropagation();
        }

        private void OnDragPointerMove(PointerMoveEvent evt) {
            if (!_isDragging || evt.pointerId != _dragPointerId) {
                return;
            }

            SeekFromPanelPosition(evt.position, false);
            evt.StopPropagation();
        }

        private void OnDragPointerUp(PointerUpEvent evt) {
            if (evt.button != 0 || !_isDragging || evt.pointerId != _dragPointerId) {
                return;
            }

            SeekFromPanelPosition(evt.position, true);
            EndDrag(false);
            evt.StopPropagation();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt) {
            EndDrag(true);
        }

        private void RegisterDragCallbacks() {
            var root = panel?.visualTree;
            if (_dragRoot == root) {
                return;
            }

            UnregisterDragCallbacks();
            _dragRoot = root;
            if (_dragRoot == null) {
                return;
            }

            _dragRoot.RegisterCallback<PointerMoveEvent>(OnDragPointerMove, TrickleDown.TrickleDown);
            _dragRoot.RegisterCallback<PointerUpEvent>(OnDragPointerUp, TrickleDown.TrickleDown);
            _dragRoot.RegisterCallback<MouseLeaveWindowEvent>(OnDragMouseLeaveWindow);
        }

        private void UnregisterDragCallbacks() {
            if (_dragRoot == null) {
                return;
            }

            _dragRoot.UnregisterCallback<PointerMoveEvent>(OnDragPointerMove, TrickleDown.TrickleDown);
            _dragRoot.UnregisterCallback<PointerUpEvent>(OnDragPointerUp, TrickleDown.TrickleDown);
            _dragRoot.UnregisterCallback<MouseLeaveWindowEvent>(OnDragMouseLeaveWindow);
            _dragRoot = null;
        }

        private void OnDragMouseLeaveWindow(MouseLeaveWindowEvent evt) {
            EndDrag(true);
        }

        private void EndDrag(bool dispatchCurrentTime) {
            if (dispatchCurrentTime) {
                DispatchSeekRequested(_currentTime, true);
            }

            _isDragging = false;
            _dragPointerId = PointerId.invalidPointerId;
            UnregisterDragCallbacks();
        }

        private void SeekFromPanelPosition(Vector3 panelPosition, bool forceDispatch) {
            SeekFromLocalX(panelPosition.x - worldBound.x, forceDispatch);
        }

        private void SeekFromLocalX(float localX, bool forceDispatch) {
            if (!IsFinite(localX) || !IsFinite(_timelineDuration) || _timelineDuration <= TimeEpsilon) {
                return;
            }

            var width = IsFinite(_contentWidth) ? Mathf.Max(1.0f, _contentWidth) : 1.0f;
            var ratio = Mathf.Clamp01((localX - TimelineLeftPadding) / width);
            var nextTime = SnapTimeToFrame(ratio * _timelineDuration);
            if (Mathf.Abs(_currentTime - nextTime) > TimeEpsilon) {
                SetTimeWithoutNotify(nextTime);
            }

            DispatchSeekRequested(nextTime, forceDispatch);
        }

        private void DispatchSeekRequested(float time, bool forceDispatch) {
            if (!IsFinite(time)) {
                return;
            }

            if (!forceDispatch && !CanDispatchDragSeek()) {
                return;
            }

            if (!float.IsNaN(_lastDispatchedSeekTime) && Mathf.Abs(_lastDispatchedSeekTime - time) <= TimeEpsilon) {
                return;
            }

            _lastDispatchedSeekTime = time;
            _lastSeekDispatchTime = EditorApplication.timeSinceStartup;
            SeekRequested?.Invoke(time);
        }

        private bool CanDispatchDragSeek() {
            return EditorApplication.timeSinceStartup - _lastSeekDispatchTime >= DragSeekDispatchInterval;
        }

        private void UpdatePlayhead() {
            var width = IsFinite(_contentWidth) ? Mathf.Max(0.0f, _contentWidth) : 0.0f;
            var currentTime = IsFinite(_currentTime) ? _currentTime : 0.0f;
            var timelineDuration = IsFinite(_timelineDuration) ? _timelineDuration : 0.0f;
            var ratio = timelineDuration <= TimeEpsilon ? 0.0f : Mathf.Clamp01(currentTime / timelineDuration);
            _elapsedFill.style.width = ratio * width;
            if (width <= 0.0f) {
                _playhead.style.left = TimelineLeftPadding;
                return;
            }

            _playhead.style.left = Mathf.Clamp(
                TimelineLeftPadding + (ratio * width) - (PlayheadWidth * 0.5f),
                TimelineLeftPadding,
                TimelineLeftPadding + Mathf.Max(0.0f, width - PlayheadWidth));
        }

        private void RebuildTicks() {
            _tickContainer.Clear();
            _timeLabelContainer.Clear();
            _contentWidth = 0.0f;

            var width = resolvedStyle.width;
            if (!IsFinite(width) || width <= 0.0f || !IsFinite(_duration) || _duration <= TimeEpsilon) {
                return;
            }

            _contentWidth = GetContentWidth(width);
            if (!IsFinite(_contentWidth) || _contentWidth <= TimeEpsilon) {
                return;
            }

            var totalFrames = GetTotalFrames();
            var stepFrames = Mathf.Max(1, Mathf.CeilToInt(totalFrames / (float)MaxTickCount));
            var framesPerSecond = Mathf.Max(1, Mathf.RoundToInt(_frameRate));
            for (var tickIndex = 0; tickIndex <= MaxTickCount; tickIndex++) {
                var frame = (int)Math.Min(totalFrames, (long)tickIndex * stepFrames);
                AddTick(frame, totalFrames, frame % framesPerSecond == 0, true);
                if (frame >= totalFrames) {
                    break;
                }
            }

            AddInactiveTicks(totalFrames);
            AddTimeLabels();
        }

        private void AddTick(int frame, int totalFrames, bool isMajor, bool isActive) {
            var ratio = totalFrames == 0 ? 0.0f : Mathf.Clamp01(frame / (float)totalFrames);
            AddTickAtPosition(ratio * _contentWidth, isMajor, isActive);
        }

        private void AddInactiveTicks(int totalFrames) {
            var width = resolvedStyle.width;
            if (!IsFinite(width) || !IsFinite(_contentWidth) || _contentWidth <= TimeEpsilon) {
                return;
            }

            var timelineRight = GetTimelineRight(width);
            var remainingWidth = timelineRight - TimelineLeftPadding - _contentWidth;
            if (!IsFinite(remainingWidth) || remainingWidth <= 1.0f) {
                return;
            }

            var cellWidth = totalFrames <= 0 ? MaxFrameCellWidth : _contentWidth / totalFrames;
            if (!IsFinite(cellWidth) || cellWidth <= TimeEpsilon) {
                cellWidth = MaxFrameCellWidth;
            }

            var rawInactiveTickCount = remainingWidth / cellWidth;
            if (!IsFinite(rawInactiveTickCount) || rawInactiveTickCount <= 0.0f) {
                return;
            }

            var inactiveTickCount = Mathf.Min(MaxTickCount, Mathf.CeilToInt(Mathf.Min(rawInactiveTickCount, MaxTickCount)));
            if (inactiveTickCount <= 0) {
                return;
            }

            var framesPerSecond = Mathf.Max(1, Mathf.RoundToInt(_frameRate));
            for (var frameOffset = 1; frameOffset <= inactiveTickCount; frameOffset++) {
                var x = _contentWidth + (frameOffset * cellWidth);
                if (TimelineLeftPadding + x > timelineRight) {
                    return;
                }

                var frame = totalFrames >= int.MaxValue - frameOffset ? int.MaxValue : totalFrames + frameOffset;
                AddTickAtPosition(x, frame % framesPerSecond == 0, false);
            }
        }

        private void AddTickAtPosition(float x, bool isMajor, bool isActive) {
            if (!IsFinite(x)) {
                return;
            }

            var tick = new VisualElement {
                pickingMode = PickingMode.Ignore,
            };
            tick.style.position = Position.Absolute;
            tick.style.left = Mathf.Clamp(
                TimelineLeftPadding + x,
                TimelineLeftPadding,
                Mathf.Max(TimelineLeftPadding, GetTimelineRight(resolvedStyle.width) - 1.0f));
            tick.style.top = isMajor ? MajorTickTop : MinorTickTop;
            tick.style.bottom = TrackBottom;
            tick.style.width = 1.0f;
            tick.style.backgroundColor = GetTickColor(isMajor, isActive);
            _tickContainer.Add(tick);
        }

        private static Color GetTickColor(bool isMajor, bool isActive) {
            if (isActive) {
                return isMajor ? MajorTickColor : MinorTickColor;
            }

            return isMajor ? InactiveMajorTickColor : InactiveMinorTickColor;
        }

        private void AddTimeLabels() {
            if (!IsFinite(_timelineDuration) || !IsFinite(_contentWidth) || _timelineDuration <= TimeEpsilon || _contentWidth <= TimeEpsilon) {
                return;
            }

            var maxLabelCount = Mathf.Max(1, Mathf.FloorToInt(_contentWidth / TimeLabelApproximateWidth));
            var totalSeconds = _timelineDuration >= int.MaxValue ? int.MaxValue : Mathf.Max(1, Mathf.CeilToInt(_timelineDuration));
            var stepSeconds = Mathf.Max(1, Mathf.CeilToInt(totalSeconds / (float)maxLabelCount));
            var labelTimes = new List<float> {
                0.0f,
            };
            for (var second = stepSeconds; second < totalSeconds; second += stepSeconds) {
                labelTimes.Add(second);
            }

            for (var i = 0; i < labelTimes.Count; i++) {
                AddTimeLabel(labelTimes[i]);
            }
        }

        private void AddTimeLabel(float time) {
            if (!IsFinite(time) || !IsFinite(_timelineDuration) || _timelineDuration <= TimeEpsilon) {
                return;
            }

            var width = resolvedStyle.width;
            if (!IsFinite(width)) {
                return;
            }

            var label = new Label(FormatTimeLabel(time)) {
                pickingMode = PickingMode.Ignore,
            };
            var ratio = Mathf.Clamp01(time / _timelineDuration);
            label.style.position = Position.Absolute;
            label.style.left = Mathf.Clamp(
                TimelineLeftPadding + (ratio * _contentWidth) - (TimeLabelWidth * 0.5f),
                0.0f,
                Mathf.Max(0.0f, width - TimeLabelWidth));
            label.style.top = 0.0f;
            label.style.width = TimeLabelWidth;
            label.style.height = TimeLabelHeight;
            label.style.fontSize = 10.0f;
            label.style.color = TimeLabelColor;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            _timeLabelContainer.Add(label);
        }

        private static string FormatTimeLabel(float time) {
            return $"{Mathf.RoundToInt(time)}s";
        }

        private float SnapTimeToFrame(float time) {
            var nextTime = IsFinite(time) ? time : 0.0f;
            var timelineDuration = IsFinite(_timelineDuration) ? Mathf.Max(0.0f, _timelineDuration) : 0.0f;
            if (_frameRate <= 0 || timelineDuration <= TimeEpsilon) {
                return Mathf.Clamp(nextTime, 0.0f, timelineDuration);
            }

            var framePosition = nextTime * _frameRate;
            var totalFrames = GetTotalFrames();
            if (!IsFinite(framePosition)) {
                return 0.0f;
            }

            var frame = framePosition >= int.MaxValue ? totalFrames : Mathf.Clamp(Mathf.RoundToInt(framePosition), 0, totalFrames);
            return frame / (float)_frameRate;
        }

        private int GetTotalFrames() {
            var totalFrames = _duration * _frameRate;
            if (!IsFinite(totalFrames) || totalFrames <= 1.0f) {
                return 1;
            }

            if (totalFrames >= int.MaxValue) {
                return int.MaxValue;
            }

            return Mathf.Max(1, Mathf.CeilToInt(totalFrames));
        }

        private float GetContentWidth(float availableWidth) {
            if (!IsFinite(availableWidth) || availableWidth <= 0.0f || !IsFinite(_duration) || _duration <= TimeEpsilon) {
                return 0.0f;
            }

            var contentWidth = Mathf.Min(GetTimelineAvailableWidth(availableWidth), GetTotalFrames() * MaxFrameCellWidth);
            return IsFinite(contentWidth) ? Mathf.Max(0.0f, contentWidth) : 0.0f;
        }

        private static float GetTimelineAvailableWidth(float width) {
            if (!IsFinite(width)) {
                return 0.0f;
            }

            return Mathf.Max(0.0f, GetTimelineRight(width) - TimelineLeftPadding);
        }

        private static float GetTimelineRight(float width) {
            if (!IsFinite(width)) {
                return TimelineLeftPadding;
            }

            return Mathf.Max(TimelineLeftPadding, width - TimelineRightPadding);
        }

        private static float GetFrameAlignedDuration(float duration, int frameRate) {
            if (!IsFinite(duration) || duration <= TimeEpsilon || frameRate <= 0) {
                return 0.0f;
            }

            var totalFrames = duration * frameRate;
            if (!IsFinite(totalFrames) || totalFrames >= int.MaxValue) {
                return duration;
            }

            return Mathf.CeilToInt(totalFrames) / (float)frameRate;
        }

        private static bool IsFinite(float value) {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
