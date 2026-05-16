using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Animation Graph EditorWindow の View と Model を仲介するクラス
    /// </summary>
    internal sealed class AnimationGraphEditorPresenter : IDisposable {
        private static readonly Vector2 DuplicateOffset = new(30.0f, 30.0f);
        private static readonly Vector2 DefaultStartNodePosition = new(80.0f, 80.0f);
        private const string PreviewAnimationModeSessionStateKey = "UnityAnimationGraph.Editor.AnimationGraphEditorPresenter.OwnsAnimationMode";
        private const string PreviewPlayIconName = "PlayButton";
        private const string PreviewPauseIconName = "PauseButton";
        private const string PreviewStopIconName = "PreMatQuad";
        private const float PreviewButtonIconSize = 16.0f;
        private const float PreviewTimeEpsilon = 0.0001f;

        private readonly AnimationGraphAssetEditorModel _assetModel = new();
        private readonly List<NodeEditorModel> _copiedNodeModels = new();
        private readonly List<string> _inspectedNodeIds = new();

        private ObjectField _graphAssetField;
        private ObjectField _previewSourceField;
        private ToolbarButton _previewPlayButton;
        private ToolbarButton _previewStopButton;
        private Image _previewPlayIcon;
        private Image _previewStopIcon;
        private Slider _previewTimeSlider;
        private Label _previewTimeLabel;
        private AnimationGraphSchemaView _schemaView;
        private AnimationGraphView _graphView;
        private AnimationGraphInspectorView _inspectorView;
        private Label _footerLabel;
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
        private double _lastEditorPreviewUpdateTime;

        /// <summary>Inspector 表示対象 node ID 一覧が変更されたときに発火</summary>
        public event Action<IReadOnlyList<string>> InspectedNodeIdsChanged;

        /// <summary>
        /// Presenter を初期化
        /// </summary>
        /// <param name="graphAssetField">GraphAsset を表示する ObjectField</param>
        /// <param name="previewSourceField">Preview source を表示する ObjectField</param>
        /// <param name="previewPlayButton">Preview 再生ボタン</param>
        /// <param name="previewStopButton">Preview 停止ボタン</param>
        /// <param name="previewTimeSlider">Preview time slider</param>
        /// <param name="previewTimeLabel">Preview time label</param>
        /// <param name="schemaView">Target と Blackboard を表示する View</param>
        /// <param name="graphView">GraphView 領域</param>
        /// <param name="inspectorView">Inspector 領域</param>
        /// <param name="footerLabel">Footer 領域</param>
        /// <param name="initialInspectedNodeIds">初期表示する Inspector 対象 node ID 一覧</param>
        public void Initialize(
            ObjectField graphAssetField,
            ObjectField previewSourceField,
            ToolbarButton previewPlayButton,
            ToolbarButton previewStopButton,
            Slider previewTimeSlider,
            Label previewTimeLabel,
            AnimationGraphSchemaView schemaView,
            AnimationGraphView graphView,
            AnimationGraphInspectorView inspectorView,
            Label footerLabel,
            IReadOnlyList<string> initialInspectedNodeIds) {
            _graphAssetField = graphAssetField ?? throw new ArgumentNullException(nameof(graphAssetField));
            _previewSourceField = previewSourceField ?? throw new ArgumentNullException(nameof(previewSourceField));
            _previewPlayButton = previewPlayButton ?? throw new ArgumentNullException(nameof(previewPlayButton));
            _previewStopButton = previewStopButton ?? throw new ArgumentNullException(nameof(previewStopButton));
            _previewPlayIcon = CreatePreviewButtonIcon(_previewPlayButton);
            _previewStopIcon = CreatePreviewButtonIcon(_previewStopButton);
            _previewTimeSlider = previewTimeSlider ?? throw new ArgumentNullException(nameof(previewTimeSlider));
            _previewTimeLabel = previewTimeLabel ?? throw new ArgumentNullException(nameof(previewTimeLabel));
            _schemaView = schemaView ?? throw new ArgumentNullException(nameof(schemaView));
            _graphView = graphView ?? throw new ArgumentNullException(nameof(graphView));
            _inspectorView = inspectorView ?? throw new ArgumentNullException(nameof(inspectorView));
            _footerLabel = footerLabel ?? throw new ArgumentNullException(nameof(footerLabel));

            _graphAssetField.RegisterValueChangedCallback(OnGraphAssetChanged);
            _previewPlayButton.clicked += ToggleEditorPreviewPlayback;
            _previewStopButton.clicked += StopEditorPreview;
            _previewTimeSlider.RegisterValueChangedCallback(OnPreviewTimeSliderChanged);
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
            _graphView.PasteRequested += PasteCopiedNodes;
            _graphView.DuplicateRequested += DuplicateSelection;
            _graphView.DeleteRequested += DeleteSelection;
            _graphView.ActionTargetKeyChanged += SetActionTargetKey;
            _graphView.DelayChanged += SetDelay;
            _graphView.FlagBranchKeyChanged += SetFlagBranchKey;
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
            SetInspectorSelectionByIds(initialInspectedNodeIds);
        }

        /// <inheritdoc/>
        public void Dispose() {
            StopEditorPreview(null);

            if (_graphAssetField != null) {
                _graphAssetField.UnregisterValueChangedCallback(OnGraphAssetChanged);
            }

            if (_previewPlayButton != null) {
                _previewPlayButton.clicked -= ToggleEditorPreviewPlayback;
            }

            if (_previewStopButton != null) {
                _previewStopButton.clicked -= StopEditorPreview;
            }

            if (_previewTimeSlider != null) {
                _previewTimeSlider.UnregisterValueChangedCallback(OnPreviewTimeSliderChanged);
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
                _graphView.PasteRequested -= PasteCopiedNodes;
                _graphView.DuplicateRequested -= DuplicateSelection;
                _graphView.DeleteRequested -= DeleteSelection;
                _graphView.ActionTargetKeyChanged -= SetActionTargetKey;
                _graphView.DelayChanged -= SetDelay;
                _graphView.FlagBranchKeyChanged -= SetFlagBranchKey;
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
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            Selection.selectionChanged -= OnSelectionChanged;
            Undo.undoRedoPerformed -= RefreshGraph;
            _schemaView?.Dispose();
            _inspectorView?.Dispose();
            DestroyPreviewAnimationModeDriver();
        }

        private void OnGraphAssetChanged(ChangeEvent<UnityEngine.Object> evt) {
            SetGraphAsset((AnimationGraphAsset)evt.newValue);
        }

        private void SetGraphAsset(AnimationGraphAsset graphAsset) {
            StopEditorPreview(null);
            _assetModel.SetGraphAsset(graphAsset);
            if (graphAsset != null && !_assetModel.HasStartNode && _assetModel.Nodes.Count == 0) {
                try {
                    _assetModel.InitializeGraph(DefaultStartNodePosition);
                }
                catch (Exception exception) {
                    SetFooterMessage(exception.Message);
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

        private void AddNode(Type nodeType, Vector2 graphPosition) {
            if (!_assetModel.HasGraphAsset) {
                SetFooterMessage("GraphAsset is not selected");
                return;
            }

            var nodeModel = _assetModel.AddNode(nodeType, graphPosition);
            _graphView.Rebuild(_assetModel);
            _graphView.SelectNodeModels(new[] { nodeModel });
            UpdateInspectorSelection();
            if (RefreshPreviewAfterGraphChanged()) {
                SetFooterMessage($"{nodeModel.DisplayName} added");
            }
        }

        private void MoveNode(NodeEditorModel nodeModel, Rect nodePosition) {
            nodeModel.SetGraphPosition(nodePosition.position);
        }

        private void MoveSignal(SignalEditorModel signalModel, Rect signalPosition) {
            signalModel.SetGraphPosition(signalPosition.position);
        }

        private void SetActionTargetKey(NodeEditorModel nodeModel, string targetKey) {
            if (nodeModel == null) {
                return;
            }

            nodeModel.SetActionTargetKey(targetKey);
            var previewUpdated = RefreshPreviewAfterGraphChanged();
            RefreshNodeDetails();
            RefreshInspectorSelection();
            if (previewUpdated) {
                SetFooterMessage($"{nodeModel.DisplayName} target: {GetDisplayValue(targetKey)}");
            }
        }

        private void SetDelay(DelayNodeEditorModel nodeModel, float delay) {
            if (nodeModel == null) {
                return;
            }

            nodeModel.SetDelay(delay);
            var previewUpdated = RefreshPreviewAfterGraphChanged();
            RefreshNodeDetails();
            RefreshInspectorSelection();
            if (previewUpdated) {
                SetFooterMessage($"{nodeModel.DisplayName} delay: {nodeModel.Delay:0.###}");
            }
        }

        private void SetFlagBranchKey(NodeEditorModel nodeModel, string flagKey) {
            if (nodeModel == null) {
                return;
            }

            nodeModel.SetFlagBranchKey(flagKey);
            var previewUpdated = RefreshPreviewAfterGraphChanged();
            RefreshNodeDetails();
            RefreshInspectorSelection();
            if (previewUpdated) {
                SetFooterMessage($"{nodeModel.DisplayName} flag: {GetDisplayValue(flagKey)}");
            }
        }

        private void SetJoinType(NodeEditorModel nodeModel, JoinType joinType) {
            if (nodeModel == null) {
                return;
            }

            nodeModel.SetJoinType(joinType);
            var previewUpdated = RefreshPreviewAfterGraphChanged();
            RefreshNodeDetails();
            RefreshInspectorSelection();
            if (previewUpdated) {
                SetFooterMessage($"{nodeModel.DisplayName} join: {joinType}");
            }
        }

        private void SetLoopCount(LoopNodeEditorModel nodeModel, int loopCount) {
            if (nodeModel == null) {
                return;
            }

            nodeModel.SetLoopCount(loopCount);
            var previewUpdated = RefreshPreviewAfterGraphChanged();
            RefreshNodeDetails();
            RefreshInspectorSelection();
            if (previewUpdated) {
                SetFooterMessage($"{nodeModel.DisplayName} count: {nodeModel.LoopCount}");
            }
        }

        private bool ConnectNodes(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, NodeEditorModel targetNodeModel) {
            if (!_assetModel.Connect(outputPortKind, sourceNodeModel, targetNodeModel, out var errorMessage)) {
                SetFooterMessage(errorMessage);
                return false;
            }

            if (RefreshPreviewAfterGraphChanged()) {
                SetFooterMessage($"{sourceNodeModel.DisplayName}.{GetOutputPortName(outputPortKind, sourceNodeModel)} -> {targetNodeModel.DisplayName}");
            }

            return true;
        }

        private void DisconnectNodes(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, NodeEditorModel targetNodeModel) {
            if (_assetModel.Disconnect(outputPortKind, sourceNodeModel, targetNodeModel)) {
                if (RefreshPreviewAfterGraphChanged()) {
                    SetFooterMessage($"{sourceNodeModel.DisplayName} disconnected");
                }
            }
        }

        private bool ConnectSignal(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, SignalEditorModel targetSignalModel) {
            if (!_assetModel.ConnectSignal(outputPortKind, sourceNodeModel, targetSignalModel, out var errorMessage)) {
                SetFooterMessage(errorMessage);
                return false;
            }

            if (RefreshPreviewAfterGraphChanged()) {
                SetFooterMessage($"{sourceNodeModel.DisplayName}.{GetOutputPortName(outputPortKind, sourceNodeModel)} -> {targetSignalModel.DisplayName}");
            }

            return true;
        }

        private void DisconnectSignal(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel, SignalEditorModel targetSignalModel) {
            if (!_assetModel.DisconnectSignal(outputPortKind, sourceNodeModel, targetSignalModel)) {
                return;
            }

            if (RefreshPreviewAfterGraphChanged()) {
                SetFooterMessage($"{targetSignalModel.DisplayName} removed");
            }

            EditorApplication.delayCall += RefreshGraph;
        }

        private void AddSignal(Type signalType, Vector2 graphPosition) {
            if (!_assetModel.HasGraphAsset) {
                SetFooterMessage("GraphAsset is not selected");
                return;
            }

            try {
                var signalModel = _assetModel.AddSignal(signalType, graphPosition);
                if (RefreshGraphState()) {
                    SetFooterMessage($"Signal added: {signalModel.DisplayName}");
                }
            }
            catch (Exception exception) {
                SetFooterMessage(exception.Message);
            }
        }

        private void UpdateInspectorSelection() {
            var selectedNodeModels = _graphView.GetSelectedNodeModels();
            if (selectedNodeModels.Count > 0) {
                SetInspectorSelection(selectedNodeModels);
                return;
            }

            var selectedSignalModels = _graphView.GetSelectedSignalModels();
            if (selectedSignalModels.Count > 0) {
                _inspectorView.SetSignalSelection(selectedSignalModels);
                return;
            }

            RefreshInspectorSelection();
        }

        private void CopySelection() {
            _copiedNodeModels.Clear();
            var selectedNodeModels = _graphView.GetSelectedNodeModels();
            for (var i = 0; i < selectedNodeModels.Count; i++) {
                var nodeModel = selectedNodeModels[i];
                if (!_assetModel.CanDuplicateNode(nodeModel)) {
                    continue;
                }

                _copiedNodeModels.Add(nodeModel);
            }

            SetFooterMessage($"{_copiedNodeModels.Count} node copied");
        }

        private void PasteCopiedNodes() {
            if (_copiedNodeModels.Count == 0) {
                SetFooterMessage("No copied node");
                return;
            }

            var duplicatedNodeModels = _assetModel.DuplicateNodes(_copiedNodeModels, DuplicateOffset);
            _graphView.Rebuild(_assetModel);
            _graphView.SelectNodeModels(duplicatedNodeModels);
            UpdateInspectorSelection();
            if (RefreshPreviewAfterGraphChanged()) {
                SetFooterMessage($"{duplicatedNodeModels.Count} node pasted");
            }
        }

        private void DuplicateSelection() {
            var selectedNodeModels = _graphView.GetSelectedNodeModels();
            var duplicatedNodeModels = _assetModel.DuplicateNodes(selectedNodeModels, DuplicateOffset);
            _graphView.Rebuild(_assetModel);
            _graphView.SelectNodeModels(duplicatedNodeModels);
            UpdateInspectorSelection();
            if (RefreshPreviewAfterGraphChanged()) {
                SetFooterMessage($"{duplicatedNodeModels.Count} node duplicated");
            }
        }

        private void DeleteSelection() {
            var selectedEdges = _graphView.GetSelectedEdgeConnections();
            for (var i = 0; i < selectedEdges.Count; i++) {
                _assetModel.Disconnect(selectedEdges[i].OutputPortKind, selectedEdges[i].SourceNodeModel, selectedEdges[i].TargetNodeModel);
            }

            var selectedSignalEdges = _graphView.GetSelectedSignalEdgeConnections();
            for (var i = 0; i < selectedSignalEdges.Count; i++) {
                _assetModel.DisconnectSignal(selectedSignalEdges[i].OutputPortKind, selectedSignalEdges[i].SourceNodeModel, selectedSignalEdges[i].TargetSignalModel);
            }

            var selectedNodeModels = _graphView.GetSelectedNodeModels();
            _assetModel.RemoveNodes(selectedNodeModels);
            var selectedSignalModels = _graphView.GetSelectedSignalModels();
            _assetModel.RemoveSignals(selectedSignalModels);
            if (RefreshGraphState()) {
                SetFooterMessage("Selection removed");
            }
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

        private void RefreshNodeDetails() {
            _graphView.RefreshNodeDetails();
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
            var graphAsset = GetSelectedGraphAsset();
            if (graphAsset == null) {
                SetFooterMessage("GraphAsset is not selected");
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                SetFooterMessage("Editor preview is available outside Play Mode");
                return;
            }

            if (_assetModel.GraphAsset != graphAsset) {
                SetGraphAsset(graphAsset);
            }

            if (_editorPreviewPlayer == null && !TryStartEditorPreview(graphAsset, out var message)) {
                SetFooterMessage(message);
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
        private void StopEditorPreview(string footerMessage) {
            if (_editorPreviewPlayer == null && !_ownsAnimationMode && !IsPreviewAnimationModeActive()) {
                RefreshPreviewSourceFromSelection(false);
                UpdatePreviewControls();
                if (!string.IsNullOrEmpty(footerMessage)) {
                    SetFooterMessage(footerMessage);
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
                if (player != null && IsPreviewAnimationModeActive()) {
                    AnimationMode.BeginSampling();
                    try {
                        player.Stop();
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
                    SetFooterMessage(footerMessage);
                }
            }
        }

        /// <summary>
        /// Seek スライダーの変更を Preview 評価に反映
        /// </summary>
        /// <param name="evt">Slider の値変更イベント</param>
        private void OnPreviewTimeSliderChanged(ChangeEvent<float> evt) {
            if (_isUpdatingPreviewControls || _editorPreviewPlayer == null) {
                return;
            }

            _editorPreviewPlayer.Pause();
            _isEditorPreviewPlaying = false;
            EvaluateEditorPreview(evt.newValue);
            SetFooterMessage(CreatePreviewStatusMessage("Preview scrubbed"));
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
            return rootGameObject != null && previewRunner != null;
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
                StopEditorPreview("Preview stopped: target was removed");
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
                StopEditorPreview("Preview stopped: GraphAsset is not selected");
                return false;
            }

            if (_editorPreviewRoot == null) {
                StopEditorPreview("Preview stopped: target was removed");
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
                SetFooterMessage($"Preview update failed: {exception.Message}");
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
                _editorPreviewPlayer.Seek(time);
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
            }
            finally {
                AnimationMode.EndSampling();
            }

            SceneView.RepaintAll();
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
            if (_previewPlayButton == null || _previewStopButton == null || _previewTimeSlider == null || _previewTimeLabel == null) {
                return;
            }

            var hasPreview = _editorPreviewPlayer != null;
            var currentTime = hasPreview ? _editorPreviewPlayer.CurrentTime : 0.0f;
            var duration = hasPreview ? _editorPreviewPlayer.Duration : 0.0f;
            UpdatePreviewControlIcons();
            _previewPlayButton.SetEnabled(hasPreview || GetSelectedGraphAsset() != null && _editorPreviewSourceRunner != null);
            _previewStopButton.SetEnabled(hasPreview);
            _previewTimeSlider.SetEnabled(hasPreview && duration > PreviewTimeEpsilon);
            _previewTimeSlider.lowValue = 0.0f;
            _previewTimeSlider.highValue = Mathf.Max(0.0f, duration);
            _isUpdatingPreviewControls = true;
            try {
                _previewTimeSlider.SetValueWithoutNotify(currentTime);
            }
            finally {
                _isUpdatingPreviewControls = false;
            }

            _previewTimeLabel.text = $"{currentTime:0.00} / {duration:0.00}s";
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
            SetPreviewButtonIcon(_previewPlayButton, _previewPlayIcon, playIconName, playTooltip);
            SetPreviewButtonIcon(_previewStopButton, _previewStopIcon, PreviewStopIconName, "Stop preview and restore sampled values");
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
                _graphView.RefreshNodeDetails();
            }
        }

        private void SetInspectorSelection(IReadOnlyList<NodeEditorModel> nodeModels) {
            _inspectedNodeIds.Clear();
            for (var i = 0; i < nodeModels.Count; i++) {
                _inspectedNodeIds.Add(nodeModels[i].NodeId);
            }

            _inspectorView.SetSelection(nodeModels);
            NotifyInspectedNodeIdsChanged();
        }

        private void SetInspectorSelectionByIds(IReadOnlyList<string> nodeIds) {
            _inspectedNodeIds.Clear();
            if (nodeIds != null) {
                for (var i = 0; i < nodeIds.Count; i++) {
                    if (string.IsNullOrEmpty(nodeIds[i])) {
                        continue;
                    }

                    _inspectedNodeIds.Add(nodeIds[i]);
                }
            }

            RefreshInspectorSelection();
        }

        private void RefreshInspectorSelection() {
            var nodeModels = new List<NodeEditorModel>();
            for (var i = _inspectedNodeIds.Count - 1; i >= 0; i--) {
                if (!_assetModel.TryGetNode(_inspectedNodeIds[i], out var nodeModel)) {
                    _inspectedNodeIds.RemoveAt(i);
                    continue;
                }

                nodeModels.Insert(0, nodeModel);
            }

            _inspectorView.SetSelection(nodeModels);
            NotifyInspectedNodeIdsChanged();
        }

        private void ClearInspectorSelection() {
            _inspectedNodeIds.Clear();
            _inspectorView.SetSelection(Array.Empty<NodeEditorModel>());
            NotifyInspectedNodeIdsChanged();
        }

        private void NotifyInspectedNodeIdsChanged() {
            InspectedNodeIdsChanged?.Invoke(_inspectedNodeIds);
        }

        private void SetFooterMessage(string message) {
            if (_footerLabel == null) {
                return;
            }

            _footerLabel.text = string.IsNullOrEmpty(message) ? string.Empty : message;
        }

        private static string GetDisplayValue(string value) {
            return string.IsNullOrEmpty(value) ? "-" : value;
        }

        private static string GetOutputPortName(AnimationGraphOutputPortKind outputPortKind, NodeEditorModel sourceNodeModel) {
            return outputPortKind switch {
                AnimationGraphOutputPortKind.Next when typeof(BranchNode).IsAssignableFrom(sourceNodeModel.NodeType) => "True",
                AnimationGraphOutputPortKind.Next => "Next",
                AnimationGraphOutputPortKind.False => "False",
                AnimationGraphOutputPortKind.Loop => "Loop",
                AnimationGraphOutputPortKind.EnterSignal => "Enter",
                AnimationGraphOutputPortKind.ExitSignal => "Exit",
                _ => outputPortKind.ToString(),
            };
        }

        private static AnimationGraphRunner FindPreviewRunner(AnimationGraphAsset graphAsset) {
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
        private IReadOnlyList<AnimationGraphTargetBinding> GetEditorPreviewTargetBindings(AnimationGraphAsset graphAsset, AnimationGraphRunner runner) {
            if (graphAsset == null || runner == null) {
                return Array.Empty<AnimationGraphTargetBinding>();
            }

            var graphAssetGuid = GetGraphAssetGuid(graphAsset);
            return string.IsNullOrEmpty(graphAssetGuid)
                ? Array.Empty<AnimationGraphTargetBinding>()
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
