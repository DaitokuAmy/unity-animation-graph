using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Animation Graph EditorWindow の View と Model を仲介するクラス
    /// </summary>
    internal sealed partial class AnimationGraphEditorPresenter : IDisposable {
        /// <summary>
        /// 複製した要素をずらす距離
        /// </summary>
        private static readonly Vector2 DuplicateOffset = new(30.0f, 30.0f);

        /// <summary>
        /// StartNode の初期表示位置
        /// </summary>
        private static readonly Vector2 DefaultStartNodePosition = new(80.0f, 80.0f);

        /// <summary>
        /// Footer に表示するエラーメッセージ色
        /// </summary>
        private static readonly Color FooterErrorColor = new(1.0f, 0.32f, 0.28f);

        /// <summary>
        /// Editor Preview の AnimationMode 所有状態を保存する SessionState key
        /// </summary>
        private const string PreviewAnimationModeSessionStateKey = "UnityAnimationGraph.Editor.AnimationGraphEditorPresenter.OwnsAnimationMode";

        /// <summary>
        /// Preview 再生ボタンに表示する icon 名
        /// </summary>
        private const string PreviewPlayIconName = "PlayButton";

        /// <summary>
        /// Preview 一時停止ボタンに表示する icon 名
        /// </summary>
        private const string PreviewPauseIconName = "PauseButton";

        /// <summary>
        /// Preview 停止ボタンに表示する icon 名
        /// </summary>
        private const string PreviewStopIconName = "PreMatQuad";

        /// <summary>
        /// Preview 先頭 frame ボタンに表示する icon 名
        /// </summary>
        private const string PreviewFirstFrameIconName = "Animation.FirstKey";

        /// <summary>
        /// Preview 前 frame ボタンに表示する icon 名
        /// </summary>
        private const string PreviewPreviousFrameIconName = "Animation.PrevKey";

        /// <summary>
        /// Preview 次 frame ボタンに表示する icon 名
        /// </summary>
        private const string PreviewNextFrameIconName = "Animation.NextKey";

        /// <summary>
        /// Preview 終端 frame ボタンに表示する icon 名
        /// </summary>
        private const string PreviewLastFrameIconName = "Animation.LastKey";

        /// <summary>
        /// Preview 操作ボタンの icon size
        /// </summary>
        private const float PreviewButtonIconSize = 16.0f;

        /// <summary>
        /// Preview frame rate の初期値
        /// </summary>
        private const int DefaultPreviewFrameRate = 30;

        /// <summary>
        /// Preview frame rate の最小値
        /// </summary>
        private const int MinPreviewFrameRate = 1;

        /// <summary>
        /// Preview frame rate の最大値
        /// </summary>
        private const int MaxPreviewFrameRate = 240;

        /// <summary>
        /// Preview 時刻比較に使用する許容誤差
        /// </summary>
        private const float PreviewTimeEpsilon = 0.0001f;

        private readonly AnimationGraphAssetEditorModel _assetModel = new();
        private readonly List<NodeEditorModel> _copiedNodeModels = new();
        private readonly List<SignalEditorModel> _copiedSignalModels = new();
        private readonly List<string> _inspectedNodeIds = new();
        private readonly List<string> _inspectedSignalIds = new();

        private ObjectField _graphAssetField;
        private ObjectField _previewSourceField;
        private ToolbarButton _previewFirstFrameButton;
        private ToolbarButton _previewPreviousFrameButton;
        private ToolbarButton _previewPlayButton;
        private ToolbarButton _previewStopButton;
        private ToolbarButton _previewNextFrameButton;
        private ToolbarButton _previewLastFrameButton;
        private Image _previewFirstFrameIcon;
        private Image _previewPreviousFrameIcon;
        private Image _previewPlayIcon;
        private Image _previewStopIcon;
        private Image _previewNextFrameIcon;
        private Image _previewLastFrameIcon;
        private IntegerField _previewFrameRateField;
        private AnimationGraphPreviewTimelineView _previewTimelineView;
        private Label _previewTimeLabel;
        private AnimationGraphSchemaView _schemaView;
        private AnimationGraphView _graphView;
        private AnimationGraphInspectorView _inspectorView;
        private Label _footerLabel;
        private StyleColor _footerDefaultColor;
        private AnimationGraphRunner _previewRunner;
        private AnimationGraphSchedule _previewSchedule;
        private AnimationGraphPlayerState _previewState;
        private float _previewTime;
        private AnimationGraphPlayer _editorPreviewPlayer;
        private AnimationGraphEditorPreviewContext _editorPreviewContext;
        private GameObject _editorPreviewRoot;
        private AnimationGraphRunner _editorPreviewSourceRunner;
        private AnimationModeDriver _previewAnimationModeDriver;
        private bool _isEditorPreviewPlaying;
        private bool _ownsAnimationMode;
        private bool _isUpdatingPreviewControls;
        private bool _hasEditorPreviewInteraction;
        private int _previewFrameRate = DefaultPreviewFrameRate;
        private double _lastEditorPreviewUpdateTime;

        /// <summary>Inspector 表示対象 node ID 一覧が変更されたときに発火</summary>
        public event Action<IReadOnlyList<string>> InspectedNodeIdsChanged;

        /// <summary>
        /// Presenter を初期化
        /// </summary>
        /// <param name="graphAssetField">GraphAsset を表示する ObjectField</param>
        /// <param name="previewSourceField">Preview source を表示する ObjectField</param>
        /// <param name="previewFirstFrameButton">Preview 先頭 frame button</param>
        /// <param name="previewPreviousFrameButton">Preview 前 frame button</param>
        /// <param name="previewPlayButton">Preview 再生ボタン</param>
        /// <param name="previewStopButton">Preview 停止ボタン</param>
        /// <param name="previewNextFrameButton">Preview 次 frame button</param>
        /// <param name="previewLastFrameButton">Preview 終端 frame button</param>
        /// <param name="previewFrameRateField">Preview frame rate field</param>
        /// <param name="previewTimelineView">Preview timeline view</param>
        /// <param name="previewTimeLabel">Preview time label</param>
        /// <param name="schemaView">Target と Blackboard を表示する View</param>
        /// <param name="graphView">GraphView 領域</param>
        /// <param name="inspectorView">Inspector 領域</param>
        /// <param name="footerLabel">Footer 領域</param>
        /// <param name="initialInspectedNodeIds">初期表示する Inspector 対象 node ID 一覧</param>
        public void Initialize(
            ObjectField graphAssetField,
            ObjectField previewSourceField,
            ToolbarButton previewFirstFrameButton,
            ToolbarButton previewPreviousFrameButton,
            ToolbarButton previewPlayButton,
            ToolbarButton previewStopButton,
            ToolbarButton previewNextFrameButton,
            ToolbarButton previewLastFrameButton,
            IntegerField previewFrameRateField,
            AnimationGraphPreviewTimelineView previewTimelineView,
            Label previewTimeLabel,
            AnimationGraphSchemaView schemaView,
            AnimationGraphView graphView,
            AnimationGraphInspectorView inspectorView,
            Label footerLabel,
            IReadOnlyList<string> initialInspectedNodeIds) {
            _graphAssetField = graphAssetField ?? throw new ArgumentNullException(nameof(graphAssetField));
            _previewSourceField = previewSourceField ?? throw new ArgumentNullException(nameof(previewSourceField));
            _previewFirstFrameButton = previewFirstFrameButton ?? throw new ArgumentNullException(nameof(previewFirstFrameButton));
            _previewPreviousFrameButton = previewPreviousFrameButton ?? throw new ArgumentNullException(nameof(previewPreviousFrameButton));
            _previewPlayButton = previewPlayButton ?? throw new ArgumentNullException(nameof(previewPlayButton));
            _previewStopButton = previewStopButton ?? throw new ArgumentNullException(nameof(previewStopButton));
            _previewNextFrameButton = previewNextFrameButton ?? throw new ArgumentNullException(nameof(previewNextFrameButton));
            _previewLastFrameButton = previewLastFrameButton ?? throw new ArgumentNullException(nameof(previewLastFrameButton));
            _previewFirstFrameIcon = CreatePreviewButtonIcon(_previewFirstFrameButton);
            _previewPreviousFrameIcon = CreatePreviewButtonIcon(_previewPreviousFrameButton);
            _previewPlayIcon = CreatePreviewButtonIcon(_previewPlayButton);
            _previewStopIcon = CreatePreviewButtonIcon(_previewStopButton);
            _previewNextFrameIcon = CreatePreviewButtonIcon(_previewNextFrameButton);
            _previewLastFrameIcon = CreatePreviewButtonIcon(_previewLastFrameButton);
            _previewFrameRateField = previewFrameRateField ?? throw new ArgumentNullException(nameof(previewFrameRateField));
            _previewFrameRate = ClampPreviewFrameRate(_previewFrameRateField.value);
            _previewFrameRateField.SetValueWithoutNotify(_previewFrameRate);
            _previewTimelineView = previewTimelineView ?? throw new ArgumentNullException(nameof(previewTimelineView));
            _previewTimeLabel = previewTimeLabel ?? throw new ArgumentNullException(nameof(previewTimeLabel));
            _schemaView = schemaView ?? throw new ArgumentNullException(nameof(schemaView));
            _graphView = graphView ?? throw new ArgumentNullException(nameof(graphView));
            _inspectorView = inspectorView ?? throw new ArgumentNullException(nameof(inspectorView));
            _footerLabel = footerLabel ?? throw new ArgumentNullException(nameof(footerLabel));
            _footerDefaultColor = _footerLabel.style.color;

            _graphAssetField.RegisterValueChangedCallback(OnGraphAssetChanged);
            _previewFirstFrameButton.clicked += SeekPreviewToFirstFrame;
            _previewPreviousFrameButton.clicked += StepPreviewToPreviousFrame;
            _previewPlayButton.clicked += ToggleEditorPreviewPlayback;
            _previewStopButton.clicked += StopEditorPreview;
            _previewNextFrameButton.clicked += StepPreviewToNextFrame;
            _previewLastFrameButton.clicked += SeekPreviewToLastFrame;
            _previewFrameRateField.RegisterValueChangedCallback(OnPreviewFrameRateChanged);
            _previewTimelineView.SeekRequested += OnPreviewTimelineSeekRequested;
            _graphView.NodeCreateRequested += AddNode;
            _graphView.NodeMoved += MoveNode;
            _graphView.SignalMoved += MoveSignal;
            _graphView.EdgeCreateRequested += ConnectNodes;
            _graphView.EdgeRemoveRequested += DisconnectNodes;
            _graphView.SignalEdgeCreateRequested += ConnectSignal;
            _graphView.SignalEdgeRemoveRequested += DisconnectSignal;
            _graphView.SignalCreateRequested += AddSignal;
            _graphView.SelectionChanged += UpdateInspectorSelection;
            _graphView.CopyRequested += CopySelection;
            _graphView.PasteRequested += PasteCopiedElements;
            _graphView.DuplicateRequested += DuplicateSelection;
            _graphView.DeleteRequested += DeleteSelection;
            _graphView.ActionTargetKeyChanged += SetActionTargetKey;
            _graphView.DetailStringChanged += SetNodeDetailString;
            _graphView.DetailBoolChanged += SetNodeDetailBool;
            _graphView.DetailIntChanged += SetNodeDetailInt;
            _graphView.DetailFloatChanged += SetNodeDetailFloat;
            _graphView.DelayChanged += SetDelay;
            _graphView.JoinTypeChanged += SetJoinType;
            _graphView.LoopCountChanged += SetLoopCount;
            _schemaView.SchemaChanged += OnSchemaChanged;
            _inspectorView.NodePropertiesChanged += RefreshGraph;
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            Selection.selectionChanged += OnSelectionChanged;
            Undo.undoRedoPerformed += RefreshGraph;

            RecoverPreviewAnimationModeAfterReload();
            SetFooterMessage("Select an AnimationGraphAsset");
            UpdatePreviewControls();
            SetGraphAsset((AnimationGraphAsset)_graphAssetField.value);
            RefreshPreviewSourceFromSelection(false);
            UpdatePreviewControls();
            EditorApplication.delayCall += RestoreIdleFooterMessage;
            SetInspectorSelectionByIds(initialInspectedNodeIds);
        }

        /// <inheritdoc/>
        public void Dispose() {
            StopEditorPreview(null);

            if (_graphAssetField != null) {
                _graphAssetField.UnregisterValueChangedCallback(OnGraphAssetChanged);
            }

            if (_previewFirstFrameButton != null) {
                _previewFirstFrameButton.clicked -= SeekPreviewToFirstFrame;
            }

            if (_previewPreviousFrameButton != null) {
                _previewPreviousFrameButton.clicked -= StepPreviewToPreviousFrame;
            }

            if (_previewPlayButton != null) {
                _previewPlayButton.clicked -= ToggleEditorPreviewPlayback;
            }

            if (_previewStopButton != null) {
                _previewStopButton.clicked -= StopEditorPreview;
            }

            if (_previewNextFrameButton != null) {
                _previewNextFrameButton.clicked -= StepPreviewToNextFrame;
            }

            if (_previewLastFrameButton != null) {
                _previewLastFrameButton.clicked -= SeekPreviewToLastFrame;
            }

            if (_previewFrameRateField != null) {
                _previewFrameRateField.UnregisterValueChangedCallback(OnPreviewFrameRateChanged);
            }

            if (_previewTimelineView != null) {
                _previewTimelineView.SeekRequested -= OnPreviewTimelineSeekRequested;
            }

            if (_graphView != null) {
                _graphView.NodeCreateRequested -= AddNode;
                _graphView.NodeMoved -= MoveNode;
                _graphView.SignalMoved -= MoveSignal;
                _graphView.EdgeCreateRequested -= ConnectNodes;
                _graphView.EdgeRemoveRequested -= DisconnectNodes;
                _graphView.SignalEdgeCreateRequested -= ConnectSignal;
                _graphView.SignalEdgeRemoveRequested -= DisconnectSignal;
                _graphView.SignalCreateRequested -= AddSignal;
                _graphView.SelectionChanged -= UpdateInspectorSelection;
                _graphView.CopyRequested -= CopySelection;
                _graphView.PasteRequested -= PasteCopiedElements;
                _graphView.DuplicateRequested -= DuplicateSelection;
                _graphView.DeleteRequested -= DeleteSelection;
                _graphView.ActionTargetKeyChanged -= SetActionTargetKey;
                _graphView.DetailStringChanged -= SetNodeDetailString;
                _graphView.DetailBoolChanged -= SetNodeDetailBool;
                _graphView.DetailIntChanged -= SetNodeDetailInt;
                _graphView.DetailFloatChanged -= SetNodeDetailFloat;
                _graphView.DelayChanged -= SetDelay;
                _graphView.JoinTypeChanged -= SetJoinType;
                _graphView.LoopCountChanged -= SetLoopCount;
            }

            if (_schemaView != null) {
                _schemaView.SchemaChanged -= OnSchemaChanged;
            }

            if (_inspectorView != null) {
                _inspectorView.NodePropertiesChanged -= RefreshGraph;
            }

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.delayCall -= RestoreIdleFooterMessage;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            Selection.selectionChanged -= OnSelectionChanged;
            Undo.undoRedoPerformed -= RefreshGraph;
            _schemaView?.Dispose();
            _inspectorView?.Dispose();
            DestroyPreviewAnimationModeDriver();
        }

        /// <summary>
        /// GraphAsset field の変更を Model と View に反映
        /// </summary>
        /// <param name="evt">GraphAsset field の変更イベント</param>
        private void OnGraphAssetChanged(ChangeEvent<UnityEngine.Object> evt) {
            StopEditorPreview();
            SetGraphAsset((AnimationGraphAsset)evt.newValue, false);
        }

        /// <summary>
        /// 編集対象 GraphAsset を設定し、表示状態を更新
        /// </summary>
        /// <param name="graphAsset">編集対象 GraphAsset</param>
        /// <param name="stopPreview">差し替え前に Preview を停止する場合は true</param>
        private void SetGraphAsset(AnimationGraphAsset graphAsset, bool stopPreview = true) {
            if (stopPreview) {
                StopEditorPreview(null);
            }

            _hasEditorPreviewInteraction = false;
            _assetModel.SetGraphAsset(graphAsset);
            if (graphAsset != null && !_assetModel.HasStartNode && _assetModel.Nodes.Count == 0) {
                try {
                    _assetModel.InitializeGraph(DefaultStartNodePosition);
                }
                catch (Exception exception) {
                    SetFooterMessage(exception.Message, true);
                }
            }

            _copiedNodeModels.Clear();
            ClearInspectorSelection();
            _schemaView.SetGraphAsset(graphAsset);
            _graphView.SetGraphAsset(_assetModel);
            RefreshPreviewSchedule();
            RefreshPreviewSourceFromSelection(false);
            UpdatePreviewControls();
            if (graphAsset == null) {
                SetFooterMessage("Select an AnimationGraphAsset");
                return;
            }

            SetFooterMessage(_assetModel.HasStartNode ? graphAsset.name : "Graph is not initialized");
        }

        /// <summary>
        /// GraphAsset の最新状態を View と Preview に反映
        /// </summary>
        private void RefreshGraph() {
            RefreshGraphState();
        }

        /// <summary>
        /// GraphAsset の最新状態を再読み込みし、Preview 中なら現在時刻で再評価
        /// </summary>
        /// <returns>Preview の更新に成功した場合は true</returns>
        private bool RefreshGraphState() {
            _assetModel.RefreshNodes();
            _schemaView.Refresh();
            _graphView.Rebuild(_assetModel);
            var previewUpdated = RefreshPreviewAfterGraphChanged();
            RefreshInspectorSelection();
            return previewUpdated;
        }

        /// <summary>
        /// Schema 変更を Node 表示と Preview 評価に反映
        /// </summary>
        private void OnSchemaChanged() {
            RefreshPreviewAfterGraphChanged();
            RefreshNodeDetails();
        }

        /// <summary>
        /// Editor 更新ごとに Preview 再生または実行中 Runner の schedule 表示を更新
        /// </summary>
        private void OnEditorUpdate() {
            if (_editorPreviewPlayer != null) {
                UpdateEditorPreview();
                return;
            }

            if (RefreshPreviewSourceFromSelection(false)) {
                UpdatePreviewControls();
            }

            RefreshPreviewSchedule();
        }

        /// <summary>
        /// Play Mode 遷移時に Editor Preview の AnimationMode を終了
        /// </summary>
        /// <param name="stateChange">Play Mode の状態遷移</param>
        private void OnPlayModeStateChanged(PlayModeStateChange stateChange) {
            if ((_editorPreviewPlayer != null || _ownsAnimationMode)
                && (stateChange == PlayModeStateChange.ExitingEditMode || stateChange == PlayModeStateChange.EnteredPlayMode)) {
                StopEditorPreview("Preview stopped");
            }
        }

        /// <summary>
        /// Assembly reload 前に Editor Preview の AnimationMode を終了
        /// </summary>
        private void OnBeforeAssemblyReload() {
            StopEditorPreview(null);
            RecoverPreviewAnimationModeAfterReload();
        }

        /// <summary>
        /// Selection 変更時に Preview Source を同期し、Preview 中なら source を切り替え
        /// </summary>
        private void OnSelectionChanged() {
            if (_editorPreviewPlayer == null) {
                RefreshPreviewSourceFromSelection(false);
                UpdatePreviewControls();
                return;
            }

            if (!TryGetSelectionPreviewSource(out var nextRoot, out var nextRunner)) {
                UpdatePreviewControls();
                return;
            }

            if (_editorPreviewRoot == nextRoot && _editorPreviewSourceRunner == nextRunner) {
                UpdatePreviewControls();
                return;
            }

            SwitchEditorPreviewSource(nextRoot, nextRunner);
        }

        /// <summary>
        /// Selection.activeGameObject から Preview Source を更新
        /// </summary>
        /// <param name="clearWhenMissing">Runner が見つからない場合に source を消す場合は true</param>
        /// <returns>Preview Source が変化した場合は true</returns>
        private bool RefreshPreviewSourceFromSelection(bool clearWhenMissing) {
            var nextRoot = default(GameObject);
            var nextRunner = default(AnimationGraphRunner);
            if (!TryGetSelectionPreviewSource(out nextRoot, out nextRunner) && !clearWhenMissing) {
                return false;
            }

            if (_editorPreviewRoot == nextRoot && _editorPreviewSourceRunner == nextRunner) {
                return false;
            }

            _editorPreviewRoot = nextRoot;
            _editorPreviewSourceRunner = nextRunner;
            UpdatePreviewSourceField();
            return true;
        }

        /// <summary>
        /// Selection.activeGameObject に付いている Runner を Preview Source として取得
        /// </summary>
        /// <param name="rootGameObject">切り替え先 Preview root</param>
        /// <param name="previewRunner">切り替え先 Runner</param>
        /// <returns>Runner が見つかった場合は true</returns>
        private bool TryGetSelectionPreviewSource(out GameObject rootGameObject, out AnimationGraphRunner previewRunner) {
            rootGameObject = null;
            previewRunner = null;
            if (!TryGetSelectedPreviewGameObject(out var selectedGameObject)) {
                return false;
            }

            previewRunner = selectedGameObject.GetComponent<AnimationGraphRunner>();
            if (previewRunner == null) {
                return false;
            }

            rootGameObject = selectedGameObject;
            return true;
        }

        /// <summary>
        /// Selection.activeGameObject から Preview 対象 GameObject を取得
        /// </summary>
        /// <param name="selectedGameObject">取得した GameObject</param>
        /// <returns>Scene 上の GameObject が取得できた場合は true</returns>
        private bool TryGetSelectedPreviewGameObject(out GameObject selectedGameObject) {
            selectedGameObject = Selection.activeGameObject;
            if (selectedGameObject == null || EditorUtility.IsPersistent(selectedGameObject)) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Editor Preview の source Runner を切り替えて現在時刻を再評価
        /// </summary>
        /// <param name="rootGameObject">切り替え先 Preview root</param>
        /// <param name="previewRunner">切り替え先 Runner</param>
        private void SwitchEditorPreviewSource(GameObject rootGameObject, AnimationGraphRunner previewRunner) {
            _editorPreviewRoot = rootGameObject;
            _editorPreviewSourceRunner = previewRunner;
            UpdatePreviewSourceField();
            if (ReevaluateEditorPreviewAtCurrentTime()) {
                SetFooterMessage(CreatePreviewStatusMessage("Preview source changed"));
            }
        }

        /// <summary>
        /// Editor Preview の再生と一時停止を切り替え
        /// </summary>
        private void ToggleEditorPreviewPlayback() {
            if (_isEditorPreviewPlaying) {
                PauseEditorPreview();
                return;
            }

            PlayEditorPreview();
        }

        /// <summary>
        /// Selection.activeGameObject を対象に Editor Preview を開始または再開
        /// </summary>
        private void PlayEditorPreview() {
            _hasEditorPreviewInteraction = true;
            var graphAsset = GetSelectedGraphAsset();
            if (graphAsset == null) {
                SetFooterMessage("GraphAsset is not selected", true);
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                SetFooterMessage("Editor preview is available outside Play Mode", true);
                return;
            }

            if (_assetModel.GraphAsset != graphAsset) {
                SetGraphAsset(graphAsset);
            }

            if (_editorPreviewPlayer == null && !TryStartEditorPreview(graphAsset, out var message)) {
                SetFooterMessage(message, true);
                UpdatePreviewControls();
                return;
            }

            _editorPreviewPlayer.Play();
            _isEditorPreviewPlaying = true;
            _lastEditorPreviewUpdateTime = EditorApplication.timeSinceStartup;
            SetPreviewSchedule(null, _editorPreviewPlayer.Schedule, AnimationGraphPlayerState.Playing, _editorPreviewPlayer.CurrentTime);
            UpdatePreviewControls();
            SetFooterMessage(CreatePreviewStatusMessage("Preview playing"));
        }

        /// <summary>
        /// Editor Preview の現在時刻を保ったまま一時停止
        /// </summary>
        private void PauseEditorPreview() {
            if (_editorPreviewPlayer == null) {
                return;
            }

            _editorPreviewPlayer.Pause();
            _isEditorPreviewPlaying = false;
            SetPreviewSchedule(null, _editorPreviewPlayer.Schedule, AnimationGraphPlayerState.Paused, _editorPreviewPlayer.CurrentTime);
            UpdatePreviewControls();
            SetFooterMessage(CreatePreviewStatusMessage("Preview paused"));
        }

        /// <summary>
        /// Editor Preview を停止
        /// </summary>
        private void StopEditorPreview() {
            StopEditorPreview("Preview stopped");
        }

        /// <summary>
        /// Editor Preview を停止し、必要なら Footer 表示を更新
        /// </summary>
        /// <param name="footerMessage">停止後に表示する Footer メッセージ</param>
        /// <param name="isError">Footer メッセージをエラー表示にする場合は true</param>
        private void StopEditorPreview(string footerMessage, bool isError = false) {
            if (_editorPreviewPlayer == null && !_ownsAnimationMode && !IsPreviewAnimationModeActive()) {
                RefreshPreviewSourceFromSelection(false);
                UpdatePreviewControls();
                if (!string.IsNullOrEmpty(footerMessage)) {
                    SetFooterMessage(footerMessage, isError);
                }

                return;
            }

            var player = _editorPreviewPlayer;
            var ownsAnimationMode = _ownsAnimationMode;
            _editorPreviewPlayer = null;
            _editorPreviewContext = null;
            _isEditorPreviewPlaying = false;
            _ownsAnimationMode = false;

            try {
                if (player != null && !IsPreviewAnimationModeActive() && !AnimationMode.InAnimationMode()) {
                    AnimationMode.StartAnimationMode(GetPreviewAnimationModeDriver());
                    SetOwnsAnimationMode(true);
                }

                if (player != null && IsPreviewAnimationModeActive()) {
                    AnimationMode.BeginSampling();
                    try {
                        player.Stop();
                        RegisterEditorPreviewPropertyModifications(player);
                    }
                    finally {
                        AnimationMode.EndSampling();
                    }
                }
                else {
                    player?.Stop();
                }
            }
            finally {
                if (ownsAnimationMode || IsPreviewAnimationModeActive()) {
                    StopOwnedAnimationMode();
                }

                SceneView.RepaintAll();
                SetPreviewSchedule(null, null, AnimationGraphPlayerState.Stopped, 0.0f);
                RefreshPreviewSourceFromSelection(false);
                UpdatePreviewControls();
                if (!string.IsNullOrEmpty(footerMessage)) {
                    SetFooterMessage(footerMessage, isError);
                }
            }
        }

        /// <summary>
        /// Timeline drag による seek 要求を Preview 評価に反映
        /// </summary>
        /// <param name="time">Seek 先の時刻</param>
        private void OnPreviewTimelineSeekRequested(float time) {
            _hasEditorPreviewInteraction = true;
            if (_isUpdatingPreviewControls || !EnsureEditorPreviewForTimelineSeek()) {
                return;
            }

            SeekEditorPreview(time, "Preview scrubbed");
        }

        /// <summary>
        /// Timeline seek 用に Editor Preview を開始して一時停止状態にする
        /// </summary>
        /// <returns>Timeline seek 可能な Preview がある場合は true</returns>
        private bool EnsureEditorPreviewForTimelineSeek() {
            if (_editorPreviewPlayer != null) {
                return true;
            }

            var graphAsset = GetSelectedGraphAsset();
            if (graphAsset == null) {
                SetFooterMessage("GraphAsset is not selected", true);
                UpdatePreviewControls();
                return false;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                SetFooterMessage("Editor preview is available outside Play Mode", true);
                UpdatePreviewControls();
                return false;
            }

            if (!TryStartEditorPreview(graphAsset, out var message)) {
                SetFooterMessage(message, true);
                UpdatePreviewControls();
                return false;
            }

            _editorPreviewPlayer.Play();
            _editorPreviewPlayer.Pause();
            _isEditorPreviewPlaying = false;
            SetPreviewSchedule(null, _editorPreviewPlayer.Schedule, AnimationGraphPlayerState.Paused, _editorPreviewPlayer.CurrentTime);
            UpdatePreviewControls();
            return true;
        }

        /// <summary>
        /// Preview frame rate の変更を反映
        /// </summary>
        /// <param name="evt">Frame rate field の値変更イベント</param>
        private void OnPreviewFrameRateChanged(ChangeEvent<int> evt) {
            if (_isUpdatingPreviewControls) {
                return;
            }

            var nextFrameRate = ClampPreviewFrameRate(evt.newValue);
            _previewFrameRate = nextFrameRate;
            if (evt.newValue != nextFrameRate) {
                _previewFrameRateField.SetValueWithoutNotify(nextFrameRate);
            }

            UpdatePreviewControls();
        }

        /// <summary>
        /// Preview を先頭 frame へ seek
        /// </summary>
        private void SeekPreviewToFirstFrame() {
            SeekEditorPreviewFrame(0, "Preview first frame");
        }

        /// <summary>
        /// Preview を 1 frame 戻す
        /// </summary>
        private void StepPreviewToPreviousFrame() {
            StepPreviewFrame(-1);
        }

        /// <summary>
        /// Preview を 1 frame 進める
        /// </summary>
        private void StepPreviewToNextFrame() {
            StepPreviewFrame(1);
        }

        /// <summary>
        /// Preview を終端 frame へ seek
        /// </summary>
        private void SeekPreviewToLastFrame() {
            SeekEditorPreviewFrame(GetPreviewLastFrame(), "Preview last frame");
        }

        /// <summary>
        /// Preview を指定 frame 数だけ移動
        /// </summary>
        /// <param name="direction">移動方向</param>
        private void StepPreviewFrame(int direction) {
            if (_editorPreviewPlayer == null || direction == 0) {
                return;
            }

            var framePosition = _editorPreviewPlayer.CurrentTime * _previewFrameRate;
            var nextFrame = direction > 0
                ? Mathf.FloorToInt(framePosition + PreviewTimeEpsilon) + 1
                : Mathf.CeilToInt(framePosition - PreviewTimeEpsilon) - 1;
            nextFrame = Mathf.Clamp(nextFrame, 0, GetPreviewLastFrame());
            SeekEditorPreviewFrame(nextFrame, $"Preview frame {nextFrame}");
        }

        /// <summary>
        /// Preview を指定 frame へ seek
        /// </summary>
        /// <param name="frame">Seek 先 frame</param>
        /// <param name="messagePrefix">Footer message prefix</param>
        private void SeekEditorPreviewFrame(int frame, string messagePrefix) {
            if (_editorPreviewPlayer == null) {
                return;
            }

            var clampedFrame = Mathf.Clamp(frame, 0, GetPreviewLastFrame());
            SeekEditorPreview(GetPreviewTimeForFrame(clampedFrame), messagePrefix);
        }

        /// <summary>
        /// Preview を指定時刻へ seek
        /// </summary>
        /// <param name="time">Seek 先の時刻</param>
        /// <param name="messagePrefix">Footer message prefix</param>
        private void SeekEditorPreview(float time, string messagePrefix) {
            if (_editorPreviewPlayer == null) {
                return;
            }

            _editorPreviewPlayer.Pause();
            _isEditorPreviewPlaying = false;
            EvaluateEditorPreview(Mathf.Clamp(time, 0.0f, _editorPreviewPlayer.Duration));
            SetFooterMessage(CreatePreviewStatusMessage(messagePrefix));
        }

        /// <summary>
        /// Preview の最終 frame index を取得
        /// </summary>
        /// <returns>Preview の最終 frame index</returns>
        private int GetPreviewLastFrame() {
            if (_editorPreviewPlayer == null) {
                return 0;
            }

            return Mathf.Max(0, Mathf.CeilToInt(_editorPreviewPlayer.Duration * _previewFrameRate));
        }

        /// <summary>
        /// Preview の現在 frame index を取得
        /// </summary>
        /// <returns>Preview の現在 frame index</returns>
        private int GetCurrentPreviewFrame() {
            if (_editorPreviewPlayer == null) {
                return 0;
            }

            return Mathf.Clamp(Mathf.RoundToInt(_editorPreviewPlayer.CurrentTime * _previewFrameRate), 0, GetPreviewLastFrame());
        }

        /// <summary>
        /// Preview frame index を時刻に変換
        /// </summary>
        /// <param name="frame">変換する frame index</param>
        /// <returns>Frame に対応する時刻</returns>
        private float GetPreviewTimeForFrame(int frame) {
            if (_editorPreviewPlayer == null) {
                return 0.0f;
            }

            var lastFrame = GetPreviewLastFrame();
            var clampedFrame = Mathf.Clamp(frame, 0, lastFrame);
            return clampedFrame / (float)_previewFrameRate;
        }

        /// <summary>
        /// Preview frame rate を有効範囲に丸める
        /// </summary>
        /// <param name="frameRate">丸める frame rate</param>
        /// <returns>有効範囲内の frame rate</returns>
        private int ClampPreviewFrameRate(int frameRate) {
            return Mathf.Clamp(frameRate, MinPreviewFrameRate, MaxPreviewFrameRate);
        }

        /// <summary>
        /// Editor Preview 開始時に使用する source を Selection.activeGameObject から解決
        /// </summary>
        /// <param name="rootGameObject">Preview root の GameObject</param>
        /// <param name="previewRunner">Target binding 取得元の Runner</param>
        /// <returns>Preview source が解決できた場合は true</returns>
        private bool TryResolveEditorPreviewStartSource(out GameObject rootGameObject, out AnimationGraphRunner previewRunner) {
            rootGameObject = _editorPreviewRoot;
            previewRunner = _editorPreviewSourceRunner;
            if ((rootGameObject == null || previewRunner == null)
                && _previewSourceField != null
                && _previewSourceField.value is AnimationGraphRunner fieldRunner) {
                rootGameObject = fieldRunner.gameObject;
                previewRunner = fieldRunner;
            }

            if ((rootGameObject == null || previewRunner == null)
                && TryGetSelectionPreviewSource(out var selectionRootGameObject, out var selectionPreviewRunner)) {
                rootGameObject = selectionRootGameObject;
                previewRunner = selectionPreviewRunner;
            }

            if (rootGameObject == null || previewRunner == null) {
                return false;
            }

            _editorPreviewRoot = rootGameObject;
            _editorPreviewSourceRunner = previewRunner;
            UpdatePreviewSourceField();
            return true;
        }

        /// <summary>
        /// Editor Preview 用の context と player を初期化
        /// </summary>
        /// <param name="graphAsset">Preview 対象の GraphAsset</param>
        /// <param name="message">開始結果のメッセージ</param>
        /// <returns>開始できた場合は true</returns>
        private bool TryStartEditorPreview(AnimationGraphAsset graphAsset, out string message) {
            if (graphAsset == null) {
                message = "GraphAsset is not selected";
                return false;
            }

            if (!TryResolveEditorPreviewStartSource(out var rootGameObject, out var previewRunner)) {
                message = "Select a GameObject to preview";
                return false;
            }

            try {
                var targetBindings = GetEditorPreviewTargetBindings(graphAsset, previewRunner);
                var context = new AnimationGraphEditorPreviewContext(graphAsset, rootGameObject, targetBindings);
                var player = new AnimationGraphPlayer();
                player.SetContext(context);
                player.SetGraph(graphAsset);
                player.RebuildSchedule();

                if (!TryStartPreviewAnimationMode(out message)) {
                    return false;
                }

                _editorPreviewContext = context;
                _editorPreviewPlayer = player;
                _editorPreviewRoot = rootGameObject;
                _editorPreviewSourceRunner = previewRunner;
                _ownsAnimationMode = true;
                _lastEditorPreviewUpdateTime = EditorApplication.timeSinceStartup;

                EvaluateEditorPreview(0.0f);
                message = CreatePreviewStatusMessage("Preview ready");
                return true;
            }
            catch (Exception exception) {
                if (_ownsAnimationMode) {
                    StopOwnedAnimationMode();
                }

                _editorPreviewContext = null;
                _editorPreviewPlayer = null;
                _editorPreviewRoot = null;
                _isEditorPreviewPlaying = false;
                _ownsAnimationMode = false;
                message = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// Editor Preview 再生中の時間更新と状態反映を実行
        /// </summary>
        private void UpdateEditorPreview() {
            if (_editorPreviewPlayer == null) {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                StopEditorPreview("Preview stopped");
                return;
            }

            if (_editorPreviewRoot == null) {
                StopEditorPreview("Preview stopped: target was removed", true);
                return;
            }

            if (!_isEditorPreviewPlaying) {
                SetPreviewSchedule(null, _editorPreviewPlayer.Schedule, AnimationGraphPlayerState.Paused, _editorPreviewPlayer.CurrentTime);
                return;
            }

            var currentUpdateTime = EditorApplication.timeSinceStartup;
            var deltaTime = Mathf.Max(0.0f, (float)(currentUpdateTime - _lastEditorPreviewUpdateTime));
            _lastEditorPreviewUpdateTime = currentUpdateTime;
            if (deltaTime <= 0.0f) {
                return;
            }

            EvaluateEditorPreviewDelta(deltaTime);
            if (_editorPreviewPlayer.State == AnimationGraphPlayerState.Stopped || IsPreviewAtEnd()) {
                _isEditorPreviewPlaying = false;
            }

            SetPreviewSchedule(null, _editorPreviewPlayer.Schedule, GetEditorPreviewState(), _editorPreviewPlayer.CurrentTime);
            UpdatePreviewControls();
        }

        /// <summary>
        /// Graph 変更後に Preview schedule を更新し、Editor Preview 中なら現在時刻で再評価
        /// </summary>
        /// <returns>Preview の更新に成功した場合は true</returns>
        private bool RefreshPreviewAfterGraphChanged() {
            if (_editorPreviewPlayer != null) {
                return ReevaluateEditorPreviewAtCurrentTime();
            }

            RebuildPreviewSchedule();
            UpdatePreviewControls();
            return true;
        }

        /// <summary>
        /// Editor Preview の現在時刻と再生状態を保って context と schedule を再構築
        /// </summary>
        /// <returns>再評価に成功した場合は true</returns>
        private bool ReevaluateEditorPreviewAtCurrentTime() {
            if (_editorPreviewPlayer == null) {
                return true;
            }

            var graphAsset = GetSelectedGraphAsset();
            if (graphAsset == null) {
                StopEditorPreview("Preview stopped: GraphAsset is not selected", true);
                return false;
            }

            if (_editorPreviewRoot == null) {
                StopEditorPreview("Preview stopped: target was removed", true);
                return false;
            }

            var player = _editorPreviewPlayer;
            var wasPlaying = _isEditorPreviewPlaying;
            var currentTime = player.CurrentTime;
            try {
                player.Pause();
                _isEditorPreviewPlaying = false;
                ResetEditorPreviewSamplingState();
                RefreshEditorPreviewContext(graphAsset);
                player.RebuildSchedule();
                EvaluateEditorPreview(currentTime);
                if (wasPlaying && !IsPreviewAtEnd()) {
                    player.Play();
                    _isEditorPreviewPlaying = true;
                    _lastEditorPreviewUpdateTime = EditorApplication.timeSinceStartup;
                }

                SetPreviewSchedule(null, player.Schedule, GetEditorPreviewState(), player.CurrentTime);
                UpdatePreviewControls();
                return true;
            }
            catch (Exception exception) {
                _isEditorPreviewPlaying = false;
                SetPreviewSchedule(null, player.Schedule, GetEditorPreviewState(), player.CurrentTime);
                UpdatePreviewControls();
                SetFooterMessage($"Preview update failed: {exception.Message}", true);
                return false;
            }
        }

        /// <summary>
        /// Editor Preview context の Blackboard と Target binding を再解決
        /// </summary>
        /// <param name="graphAsset">Preview 対象の GraphAsset</param>
        private void RefreshEditorPreviewContext(AnimationGraphAsset graphAsset) {
            var targetBindings = GetEditorPreviewTargetBindings(graphAsset, _editorPreviewSourceRunner);
            if (_editorPreviewContext == null || _editorPreviewContext.RootGameObject != _editorPreviewRoot) {
                _editorPreviewContext = new AnimationGraphEditorPreviewContext(graphAsset, _editorPreviewRoot, targetBindings);
                _editorPreviewPlayer.SetContext(_editorPreviewContext);
                return;
            }

            _editorPreviewContext.Refresh(graphAsset, targetBindings);
        }

        /// <summary>
        /// AnimationMode 上で指定時刻の Editor Preview を評価
        /// </summary>
        /// <param name="time">評価する時刻</param>
        private void EvaluateEditorPreview(float time) {
            if (_editorPreviewPlayer == null) {
                return;
            }

            ResetEditorPreviewSamplingState();
            EnsureAnimationMode();
            AnimationMode.BeginSampling();
            try {
                _editorPreviewPlayer.SeekFromInitialState(time);
                RegisterEditorPreviewPropertyModifications();
            }
            finally {
                AnimationMode.EndSampling();
            }

            SceneView.RepaintAll();
            SetPreviewSchedule(null, _editorPreviewPlayer.Schedule, GetEditorPreviewState(), _editorPreviewPlayer.CurrentTime);
            UpdatePreviewControls();
        }

        /// <summary>
        /// AnimationMode を一度終了し、前回サンプリングされた値を Unity に戻させる
        /// </summary>
        private void ResetEditorPreviewSamplingState() {
            if (_ownsAnimationMode && IsPreviewAnimationModeActive()) {
                StopOwnedAnimationMode();
            }
        }

        /// <summary>
        /// AnimationMode 上で Editor Preview の差分時間を評価
        /// </summary>
        /// <param name="deltaTime">進める時間</param>
        private void EvaluateEditorPreviewDelta(float deltaTime) {
            EnsureAnimationMode();
            AnimationMode.BeginSampling();
            try {
                _editorPreviewPlayer.Tick(deltaTime);
                RegisterEditorPreviewPropertyModifications();
            }
            finally {
                AnimationMode.EndSampling();
            }

            SceneView.RepaintAll();
        }

        /// <summary>
        /// Editor Preview で変更される Property を AnimationMode の復元対象として登録
        /// </summary>
        private void RegisterEditorPreviewPropertyModifications() {
            RegisterEditorPreviewPropertyModifications(_editorPreviewPlayer);
        }

        /// <summary>
        /// 指定 player で変更される Property を AnimationMode の復元対象として登録
        /// </summary>
        /// <param name="player">登録対象 Property を取得する player</param>
        private void RegisterEditorPreviewPropertyModifications(AnimationGraphPlayer player) {
            if (player == null || !IsPreviewAnimationModeActive()) {
                return;
            }

            foreach (var previewProperty in player.GetPreviewProperties()) {
                var target = previewProperty.Target;
                var propertyPath = previewProperty.PropertyPath;
                if (target == null || string.IsNullOrEmpty(propertyPath)) {
                    continue;
                }

                if (!TryGetPreviewRootGameObject(target, out var rootGameObject)) {
                    continue;
                }

                var modification = new PropertyModification {
                    target = target,
                    propertyPath = propertyPath,
                };
                if (!TryFillPropertyModificationValue(modification)) {
                    continue;
                }

                if (AnimationUtility.PropertyModificationToEditorCurveBinding(modification, rootGameObject, out var binding) == null) {
                    var targetGameObject = GetGameObject(target);
                    if (targetGameObject == null
                        || rootGameObject == targetGameObject
                        || AnimationUtility.PropertyModificationToEditorCurveBinding(modification, targetGameObject, out binding) == null) {
                        continue;
                    }
                }

                AnimationMode.AddPropertyModification(binding, modification, true);
            }
        }

        private bool TryGetPreviewRootGameObject(UnityEngine.Object target, out GameObject rootGameObject) {
            rootGameObject = _editorPreviewRoot != null ? _editorPreviewRoot : GetGameObject(target);
            return rootGameObject != null;
        }

        private static GameObject GetGameObject(UnityEngine.Object target) {
            return target switch {
                Component component => component.gameObject,
                GameObject gameObject => gameObject,
                _ => null,
            };
        }

        /// <summary>
        /// PropertyModification に現在の SerializedProperty 値を設定
        /// </summary>
        /// <param name="modification">値を設定する PropertyModification</param>
        /// <returns>値を設定できた場合は true</returns>
        private bool TryFillPropertyModificationValue(PropertyModification modification) {
            if (modification == null || modification.target == null || string.IsNullOrEmpty(modification.propertyPath)) {
                return false;
            }

            var serializedObject = new SerializedObject(modification.target);
            var property = serializedObject.FindProperty(modification.propertyPath);
            if (property == null) {
                return false;
            }

            switch (property.propertyType) {
                case SerializedPropertyType.Integer:
                    modification.value = property.intValue.ToString(CultureInfo.InvariantCulture);
                    return true;
                case SerializedPropertyType.Boolean:
                    modification.value = property.boolValue ? "1" : "0";
                    return true;
                case SerializedPropertyType.Enum:
                    modification.value = property.enumValueIndex.ToString(CultureInfo.InvariantCulture);
                    return true;
                case SerializedPropertyType.Float:
                    modification.value = property.floatValue.ToString("R", CultureInfo.InvariantCulture);
                    return true;
                case SerializedPropertyType.String:
                    modification.value = property.stringValue;
                    return true;
                case SerializedPropertyType.ObjectReference:
                    modification.objectReference = property.objectReferenceValue;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Editor Preview 用の AnimationMode を開始済みにする
        /// </summary>
        private void EnsureAnimationMode() {
            if (IsPreviewAnimationModeActive()) {
                return;
            }

            if (AnimationMode.InAnimationMode()) {
                throw new InvalidOperationException("AnimationMode is active in another editor");
            }

            AnimationMode.StartAnimationMode(GetPreviewAnimationModeDriver());
            SetOwnsAnimationMode(true);
        }

        /// <summary>
        /// Editor Preview 用の AnimationMode を開始
        /// </summary>
        /// <param name="message">開始できなかった場合の理由</param>
        /// <returns>開始できた場合は true</returns>
        private bool TryStartPreviewAnimationMode(out string message) {
            RecoverPreviewAnimationModeAfterReload();
            if (IsPreviewAnimationModeActive()) {
                SetOwnsAnimationMode(true);
                message = null;
                return true;
            }

            if (AnimationMode.InAnimationMode()) {
                message = "AnimationMode is active in another editor";
                return false;
            }

            AnimationMode.StartAnimationMode(GetPreviewAnimationModeDriver());
            SetOwnsAnimationMode(true);
            message = null;
            return true;
        }

        /// <summary>
        /// Presenter が所有する AnimationMode を停止
        /// </summary>
        private void StopOwnedAnimationMode() {
            if (IsPreviewAnimationModeActive()) {
                AnimationMode.StopAnimationMode(_previewAnimationModeDriver);
            }

            SetOwnsAnimationMode(false);
        }

        /// <summary>
        /// Presenter が AnimationMode を所有している状態を保存
        /// </summary>
        /// <param name="ownsAnimationMode">所有中の場合は true</param>
        private void SetOwnsAnimationMode(bool ownsAnimationMode) {
            _ownsAnimationMode = ownsAnimationMode;
            SessionState.SetBool(PreviewAnimationModeSessionStateKey, ownsAnimationMode);
        }

        /// <summary>
        /// Assembly reload 後に残った Editor Preview の AnimationMode を復旧
        /// </summary>
        private void RecoverPreviewAnimationModeAfterReload() {
            if (!SessionState.GetBool(PreviewAnimationModeSessionStateKey, false)) {
                return;
            }

            if (IsPreviewAnimationModeActive()) {
                AnimationMode.StopAnimationMode(_previewAnimationModeDriver);
            }
            else if (AnimationMode.InAnimationMode()) {
                AnimationMode.StopAnimationMode();
            }

            SetOwnsAnimationMode(false);
        }

        /// <summary>
        /// Editor Preview 用の AnimationModeDriver を取得
        /// </summary>
        /// <returns>Editor Preview 用の AnimationModeDriver</returns>
        private AnimationModeDriver GetPreviewAnimationModeDriver() {
            if (_previewAnimationModeDriver != null) {
                return _previewAnimationModeDriver;
            }

            _previewAnimationModeDriver = ScriptableObject.CreateInstance<AnimationModeDriver>();
            _previewAnimationModeDriver.hideFlags = HideFlags.HideAndDontSave;
            return _previewAnimationModeDriver;
        }

        /// <summary>
        /// Editor Preview が所有する AnimationMode が有効か判定
        /// </summary>
        /// <returns>Editor Preview の AnimationMode が有効な場合は true</returns>
        private bool IsPreviewAnimationModeActive() {
            return _previewAnimationModeDriver != null && AnimationMode.InAnimationMode(_previewAnimationModeDriver);
        }

        /// <summary>
        /// Editor Preview 用の AnimationModeDriver を破棄
        /// </summary>
        private void DestroyPreviewAnimationModeDriver() {
            if (_previewAnimationModeDriver == null) {
                return;
            }

            if (IsPreviewAnimationModeActive()) {
                AnimationMode.StopAnimationMode(_previewAnimationModeDriver);
            }

            UnityEngine.Object.DestroyImmediate(_previewAnimationModeDriver);
            _previewAnimationModeDriver = null;
        }

        /// <summary>
        /// Editor Preview が終端時刻に到達しているか判定
        /// </summary>
        /// <returns>終端時刻の場合は true</returns>
        private bool IsPreviewAtEnd() {
            if (_editorPreviewPlayer == null) {
                return false;
            }

            return Mathf.Abs(_editorPreviewPlayer.CurrentTime - _editorPreviewPlayer.Duration) <= PreviewTimeEpsilon;
        }

        /// <summary>
        /// Editor Preview の表示用再生状態を取得
        /// </summary>
        /// <returns>表示用の再生状態</returns>
        private AnimationGraphPlayerState GetEditorPreviewState() {
            return _isEditorPreviewPlaying ? AnimationGraphPlayerState.Playing : AnimationGraphPlayerState.Paused;
        }

        /// <summary>
        /// Preview toolbar の表示と操作可否を更新
        /// </summary>
        private void UpdatePreviewControls() {
            UpdatePreviewSourceField();
            if (_previewPlayButton == null || _previewStopButton == null || _previewTimelineView == null || _previewTimeLabel == null) {
                return;
            }

            var hasPreview = _editorPreviewPlayer != null;
            TryGetPreviewTimelineDisplayState(out var currentTime, out var duration);
            var canShowTimeline = duration > PreviewTimeEpsilon;
            var canSeekPreview = hasPreview && duration > PreviewTimeEpsilon;
            var canStartPreviewFromTimeline = !hasPreview && canShowTimeline && CanStartEditorPreviewFromTimeline();
            UpdatePreviewControlIcons();
            _previewPlayButton.SetEnabled(hasPreview || GetSelectedGraphAsset() != null && _editorPreviewSourceRunner != null);
            _previewFirstFrameButton.SetEnabled(canSeekPreview);
            _previewPreviousFrameButton.SetEnabled(canSeekPreview);
            _previewStopButton.SetEnabled(hasPreview);
            _previewNextFrameButton.SetEnabled(canSeekPreview);
            _previewLastFrameButton.SetEnabled(canSeekPreview);
            _previewTimelineView.SetEnabled(canShowTimeline);
            _previewTimelineView.SetSeekable(canSeekPreview || canStartPreviewFromTimeline);
            _isUpdatingPreviewControls = true;
            try {
                _previewFrameRateField.SetValueWithoutNotify(_previewFrameRate);
                _previewTimelineView.SetPreviewState(currentTime, duration, _previewFrameRate);
            }
            finally {
                _isUpdatingPreviewControls = false;
            }

            _previewTimeLabel.text = $"{currentTime:0.00} / {duration:0.00}s  F{GetPreviewFrameForTime(currentTime, duration)}";
        }

        /// <summary>
        /// Preview Timeline の表示に使用する現在時刻と総時間を取得
        /// </summary>
        /// <param name="currentTime">表示する現在時刻</param>
        /// <param name="duration">表示する総時間</param>
        /// <returns>表示用の情報を取得できた場合は true</returns>
        private bool TryGetPreviewTimelineDisplayState(out float currentTime, out float duration) {
            if (_editorPreviewPlayer != null) {
                currentTime = _editorPreviewPlayer.CurrentTime;
                duration = _editorPreviewPlayer.Duration;
                return true;
            }

            if (_previewSchedule != null) {
                duration = _previewSchedule.Duration;
                currentTime = Mathf.Clamp(_previewTime, 0.0f, duration);
                return true;
            }

            if (TryBuildIdlePreviewSchedule(out var previewSchedule)) {
                currentTime = 0.0f;
                duration = previewSchedule.Duration;
                return true;
            }

            currentTime = 0.0f;
            duration = 0.0f;
            return false;
        }

        /// <summary>
        /// Timeline 操作から Editor Preview を開始できるか判定
        /// </summary>
        /// <returns>開始できる場合は true</returns>
        private bool CanStartEditorPreviewFromTimeline() {
            return !EditorApplication.isPlayingOrWillChangePlaymode
                && GetSelectedGraphAsset() != null
                && TryResolveEditorPreviewStartSource(out _, out _);
        }

        /// <summary>
        /// 停止中の Preview Timeline 表示用に schedule を構築
        /// </summary>
        /// <param name="previewSchedule">構築した schedule</param>
        /// <returns>Schedule を構築できた場合は true</returns>
        private bool TryBuildIdlePreviewSchedule(out AnimationGraphSchedule previewSchedule) {
            previewSchedule = null;
            var graphAsset = GetSelectedGraphAsset();
            if (graphAsset == null || !TryResolveEditorPreviewStartSource(out var rootGameObject, out var previewRunner)) {
                return false;
            }

            try {
                var targetBindings = GetEditorPreviewTargetBindings(graphAsset, previewRunner);
                var context = new AnimationGraphEditorPreviewContext(graphAsset, rootGameObject, targetBindings);
                var scheduler = new AnimationGraphScheduler();
                previewSchedule = scheduler.Build(graphAsset, context);
                return previewSchedule != null;
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// 表示時刻に対応する preview frame index を取得
        /// </summary>
        /// <param name="time">表示時刻</param>
        /// <param name="duration">総時間</param>
        /// <returns>表示時刻に対応する frame index</returns>
        private int GetPreviewFrameForTime(float time, float duration) {
            var lastFrame = Mathf.Max(0, Mathf.CeilToInt(duration * _previewFrameRate));
            return Mathf.Clamp(Mathf.RoundToInt(time * _previewFrameRate), 0, lastFrame);
        }

        /// <summary>
        /// Preview 操作用 button に表示する icon element を作成
        /// </summary>
        /// <param name="button">Icon を表示する button</param>
        /// <returns>作成した icon element</returns>
        private Image CreatePreviewButtonIcon(ToolbarButton button) {
            button.text = string.Empty;
            button.Clear();
            var icon = new Image {
                pickingMode = PickingMode.Ignore,
                scaleMode = ScaleMode.ScaleToFit,
            };
            icon.style.width = PreviewButtonIconSize;
            icon.style.height = PreviewButtonIconSize;
            button.Add(icon);
            return icon;
        }

        /// <summary>
        /// Preview 操作用 button の icon と tooltip を更新
        /// </summary>
        private void UpdatePreviewControlIcons() {
            var playIconName = _isEditorPreviewPlaying ? PreviewPauseIconName : PreviewPlayIconName;
            var playTooltip = _isEditorPreviewPlaying ? "Pause preview" : "Play preview";
            SetPreviewButtonIcon(_previewFirstFrameButton, _previewFirstFrameIcon, PreviewFirstFrameIconName, "Go to first frame");
            SetPreviewButtonIcon(_previewPreviousFrameButton, _previewPreviousFrameIcon, PreviewPreviousFrameIconName, "Previous frame");
            SetPreviewButtonIcon(_previewPlayButton, _previewPlayIcon, playIconName, playTooltip);
            SetPreviewButtonIcon(_previewStopButton, _previewStopIcon, PreviewStopIconName, "Stop preview and restore sampled values");
            SetPreviewButtonIcon(_previewNextFrameButton, _previewNextFrameIcon, PreviewNextFrameIconName, "Next frame");
            SetPreviewButtonIcon(_previewLastFrameButton, _previewLastFrameIcon, PreviewLastFrameIconName, "Go to last frame");
        }

        /// <summary>
        /// Preview button に Unity 組み込み icon を設定
        /// </summary>
        /// <param name="button">Tooltip を設定する button</param>
        /// <param name="icon">Icon 表示 element</param>
        /// <param name="iconName">EditorGUIUtility.IconContent で読み込む icon 名</param>
        /// <param name="tooltip">Button tooltip</param>
        private void SetPreviewButtonIcon(ToolbarButton button, Image icon, string iconName, string tooltip) {
            if (button == null || icon == null) {
                return;
            }

            var content = EditorGUIUtility.IconContent(iconName);
            button.text = string.Empty;
            button.tooltip = tooltip;
            icon.image = content.image;
        }

        /// <summary>
        /// Preview Source 表示 field を現在の source Runner に更新
        /// </summary>
        private void UpdatePreviewSourceField() {
            if (_previewSourceField == null) {
                return;
            }

            _previewSourceField.SetValueWithoutNotify(_editorPreviewSourceRunner);
            _previewSourceField.MarkDirtyRepaint();
        }

        /// <summary>
        /// EditorWindow で選択中の GraphAsset を取得
        /// </summary>
        /// <returns>選択中の GraphAsset</returns>
        private AnimationGraphAsset GetSelectedGraphAsset() {
            if (_assetModel != null && _assetModel.HasGraphAsset) {
                return _assetModel.GraphAsset;
            }

            return _graphAssetField == null ? null : _graphAssetField.value as AnimationGraphAsset;
        }

        /// <summary>
        /// Editor Preview の対象名と不足 target 情報を含む status メッセージを生成
        /// </summary>
        /// <param name="prefix">メッセージ先頭の状態文字列</param>
        /// <returns>Preview status メッセージ</returns>
        private string CreatePreviewStatusMessage(string prefix) {
            var rootName = _editorPreviewRoot == null ? "-" : _editorPreviewRoot.name;
            if (_editorPreviewContext == null || _editorPreviewContext.MissingTargetMessages.Count == 0) {
                return $"{prefix}: {rootName}";
            }

            return _editorPreviewContext.MissingTargetMessages.Count == 1
                ? $"{prefix}: {rootName} (missing {_editorPreviewContext.MissingTargetMessages[0]})"
                : $"{prefix}: {rootName} ({_editorPreviewContext.MissingTargetMessages.Count} missing targets)";
        }

        /// <summary>
        /// 実行中 Runner の schedule と現在時刻を Node 表示へ反映
        /// </summary>
        private void RefreshPreviewSchedule() {
            if (_assetModel == null || !_assetModel.HasGraphAsset) {
                SetPreviewSchedule(null, null, AnimationGraphPlayerState.Stopped, 0.0f);
                return;
            }

            var previewRunner = FindPreviewRunner(_assetModel.GraphAsset);
            var previewSchedule = previewRunner == null ? null : previewRunner.Schedule;
            var previewState = previewRunner == null ? AnimationGraphPlayerState.Stopped : previewRunner.State;
            var previewTime = previewRunner == null ? 0.0f : previewRunner.CurrentTime;
            SetPreviewSchedule(previewRunner, previewSchedule, previewState, previewTime);
        }

        /// <summary>
        /// 実行中 Runner の schedule を再構築して Node 表示へ反映
        /// </summary>
        private void RebuildPreviewSchedule() {
            if (_assetModel == null || !_assetModel.HasGraphAsset) {
                SetPreviewSchedule(null, null, AnimationGraphPlayerState.Stopped, 0.0f);
                return;
            }

            var previewRunner = FindPreviewRunner(_assetModel.GraphAsset);
            if (previewRunner == null) {
                SetPreviewSchedule(null, null, AnimationGraphPlayerState.Stopped, 0.0f);
                return;
            }

            previewRunner.RebuildSchedule();
            SetPreviewSchedule(previewRunner, previewRunner.Schedule, previewRunner.State, previewRunner.CurrentTime);
        }

        /// <summary>
        /// Preview schedule と現在時刻を保存し、Node の実行状態表示を更新
        /// </summary>
        /// <param name="previewRunner">Schedule 取得元 Runner</param>
        /// <param name="previewSchedule">Preview schedule</param>
        /// <param name="previewState">Preview 再生状態</param>
        /// <param name="previewTime">Preview の現在時刻</param>
        private void SetPreviewSchedule(AnimationGraphRunner previewRunner, AnimationGraphSchedule previewSchedule, AnimationGraphPlayerState previewState, float previewTime) {
            if (_previewRunner == previewRunner
                && _previewSchedule == previewSchedule
                && _previewState == previewState
                && Mathf.Abs(_previewTime - previewTime) <= PreviewTimeEpsilon) {
                return;
            }

            _previewRunner = previewRunner;
            _previewSchedule = previewSchedule;
            _previewState = previewState;
            _previewTime = previewTime;
            if (_assetModel.SetPreviewSchedule(previewSchedule, previewTime)) {
                _graphView.RefreshPreviewExecutionState();
            }
        }

        /// <summary>
        /// Footer message を表示
        /// </summary>
        /// <param name="message">表示する message</param>
        /// <param name="isError">エラー表示にする場合は true</param>
        private void SetFooterMessage(string message, bool isError = false) {
            if (_footerLabel == null) {
                return;
            }

            _footerLabel.text = string.IsNullOrEmpty(message) ? string.Empty : message;
            _footerLabel.style.color = isError ? FooterErrorColor : _footerDefaultColor;
        }

        /// <summary>
        /// 初期表示中に残った Preview 開始失敗メッセージを通常の Footer 表示へ戻す
        /// </summary>
        private void RestoreIdleFooterMessage() {
            if (_footerLabel == null || _hasEditorPreviewInteraction || _editorPreviewPlayer != null) {
                return;
            }

            var graphAsset = GetSelectedGraphAsset();
            if (graphAsset == null) {
                SetFooterMessage("Select an AnimationGraphAsset");
                return;
            }

            SetFooterMessage(_assetModel.HasStartNode ? graphAsset.name : "Graph is not initialized");
        }

        /// <summary>
        /// GraphAsset を再生中または一時停止中の Runner を Scene から検索
        /// </summary>
        /// <param name="graphAsset">検索対象 GraphAsset</param>
        /// <returns>見つかった Runner</returns>
        private AnimationGraphRunner FindPreviewRunner(AnimationGraphAsset graphAsset) {
            if (graphAsset == null) {
                return null;
            }

            var pausedRunner = default(AnimationGraphRunner);
            var runners = UnityEngine.Object.FindObjectsByType<AnimationGraphRunner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < runners.Length; i++) {
                var runner = runners[i];
                if (runner == null || runner.GraphAsset != graphAsset || runner.Schedule == null || runner.State == AnimationGraphPlayerState.Stopped) {
                    continue;
                }

                if (runner.State == AnimationGraphPlayerState.Playing) {
                    return runner;
                }

                pausedRunner ??= runner;
            }

            return pausedRunner;
        }

        /// <summary>
        /// Runner に保存されている GraphAsset GUID 対応の Target binding を取得
        /// </summary>
        /// <param name="graphAsset">Preview 対象の GraphAsset</param>
        /// <param name="runner">Binding 取得元の Runner</param>
        /// <returns>Target binding 一覧</returns>
        private IReadOnlyList<TargetBinding> GetEditorPreviewTargetBindings(AnimationGraphAsset graphAsset, AnimationGraphRunner runner) {
            if (graphAsset == null || runner == null) {
                return Array.Empty<TargetBinding>();
            }

            var graphAssetGuid = GetGraphAssetGuid(graphAsset);
            return string.IsNullOrEmpty(graphAssetGuid)
                ? Array.Empty<TargetBinding>()
                : runner.GetTargetBindingsByGraphAssetGuid(graphAssetGuid);
        }

        /// <summary>
        /// AssetDatabase または GraphAsset 内部値から GraphAsset GUID を取得
        /// </summary>
        /// <param name="graphAsset">GUID を取得する GraphAsset</param>
        /// <returns>GraphAsset GUID</returns>
        private string GetGraphAssetGuid(AnimationGraphAsset graphAsset) {
            var assetPath = AssetDatabase.GetAssetPath(graphAsset);
            if (!string.IsNullOrEmpty(assetPath)) {
                var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                if (!string.IsNullOrEmpty(assetGuid)) {
                    return assetGuid;
                }
            }

            return graphAsset.AssetGuid;
        }
    }
}
