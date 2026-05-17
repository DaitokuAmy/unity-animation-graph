using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// AnimationGraphAsset を編集する EditorWindow
    /// </summary>
    public sealed class AnimationGraphEditorWindow : EditorWindow {
        private const string WindowTitle = "Animation Graph";
        private const float DefaultSidePanelWidth = 320.0f;
        private const float DefaultSchemaPanelHeight = 280.0f;
        private const float HeaderGraphAssetFieldWidth = 320.0f;
        private const float PreviewSourceFieldWidth = 260.0f;
        private const float PreviewSliderWidth = 240.0f;
        private const float PreviewTimeLabelWidth = 92.0f;
        private const float PreviewIconButtonWidth = 28.0f;
        private const float MinGraphViewWidth = 240.0f;
        private const float MinInspectorPanelHeight = 160.0f;
        private const float MinSchemaPanelHeight = 180.0f;
        private const float MinSidePanelWidth = 280.0f;

        [SerializeField]
        private AnimationGraphAsset _graphAsset;
        [SerializeField]
        private string[] _inspectedNodeIds = Array.Empty<string>();
        [SerializeField]
        private float _sidePanelWidth = DefaultSidePanelWidth;
        [SerializeField]
        private float _schemaPanelHeight = DefaultSchemaPanelHeight;

        private AnimationGraphEditorPresenter _presenter;
        private ObjectField _graphAssetField;
        private VisualElement _schemaPanel;
        private VisualElement _sidePanel;

        /// <summary>
        /// Animation Graph EditorWindow を表示
        /// </summary>
        [MenuItem("Window/Unity Animation Graph/Animation Graph")]
        public static void Open() {
            Open(null);
        }

        /// <summary>
        /// Animation Graph EditorWindow を指定 Asset で表示
        /// </summary>
        /// <param name="graphAsset">表示する AnimationGraphAsset</param>
        public static void Open(AnimationGraphAsset graphAsset) {
            var window = GetWindow<AnimationGraphEditorWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            if (graphAsset != null) {
                window.SetGraphAsset(graphAsset);
            }

            window.Show();
            window.Focus();
        }

        /// <summary>
        /// Window 有効化時にタイトルを初期化
        /// </summary>
        private void OnEnable() {
            titleContent = new GUIContent(WindowTitle);
        }

        /// <summary>
        /// Window 無効化時に Presenter を解放
        /// </summary>
        private void OnDisable() {
            ClearPresenter();
        }

        /// <summary>
        /// Window 破棄時に Presenter を解放
        /// </summary>
        private void OnDestroy() {
            ClearPresenter();
        }

        /// <summary>
        /// Window の UI を構築
        /// </summary>
        private void CreateGUI() {
            ClearPresenter();
            rootVisualElement.Clear();

            var header = CreateHeader(out var graphAssetField);
            _graphAssetField = graphAssetField;
            _graphAssetField.SetValueWithoutNotify(_graphAsset);
            _graphAssetField.RegisterValueChangedCallback(OnGraphAssetChanged);

            var schemaView = new AnimationGraphSchemaView();
            var graphView = new AnimationGraphView();
            var inspectorView = new AnimationGraphInspectorView();
            var previewControls = CreatePreviewControls(out var previewSourceField, out var previewPlayButton, out var previewStopButton, out var previewTimeSlider, out var previewTimeLabel);
            var footerLabel = CreateFooter();
            var body = CreateBody(schemaView, graphView, previewControls, inspectorView);

            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.Add(header);
            rootVisualElement.Add(body);
            rootVisualElement.Add(footerLabel);

            _presenter = new AnimationGraphEditorPresenter();
            _presenter.InspectedNodeIdsChanged += OnInspectedNodeIdsChanged;
            _presenter.Initialize(
                graphAssetField,
                previewSourceField,
                previewPlayButton,
                previewStopButton,
                previewTimeSlider,
                previewTimeLabel,
                schemaView,
                graphView,
                inspectorView,
                footerLabel,
                _inspectedNodeIds);
        }

        /// <summary>
        /// Header toolbar を作成
        /// </summary>
        /// <param name="graphAssetField">GraphAsset 選択 field</param>
        /// <returns>Header toolbar</returns>
        private static VisualElement CreateHeader(out ObjectField graphAssetField) {
            var header = new Toolbar();
            header.style.flexShrink = 0.0f;

            graphAssetField = new ObjectField {
                objectType = typeof(AnimationGraphAsset),
                allowSceneObjects = false,
            };
            graphAssetField.style.width = HeaderGraphAssetFieldWidth;
            graphAssetField.style.minWidth = HeaderGraphAssetFieldWidth;
            graphAssetField.style.maxWidth = HeaderGraphAssetFieldWidth;
            header.Add(graphAssetField);
            header.Add(new VisualElement {
                style = {
                    flexGrow = 1.0f,
                },
            });

            return header;
        }

        /// <summary>
        /// GraphView 下部に表示する Preview 操作領域を作成
        /// </summary>
        /// <param name="previewSourceField">Preview source 表示 field</param>
        /// <param name="previewPlayButton">Preview 再生ボタン</param>
        /// <param name="previewStopButton">Preview 停止ボタン</param>
        /// <param name="previewTimeSlider">Preview seek slider</param>
        /// <param name="previewTimeLabel">Preview time label</param>
        /// <returns>Preview 操作領域</returns>
        private VisualElement CreatePreviewControls(
            out ObjectField previewSourceField,
            out ToolbarButton previewPlayButton,
            out ToolbarButton previewStopButton,
            out Slider previewTimeSlider,
            out Label previewTimeLabel) {
            var controls = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Column,
                    flexShrink = 0.0f,
                },
            };
            var sourceRow = new Toolbar();
            sourceRow.style.flexShrink = 0.0f;
            var playbackRow = new Toolbar();
            playbackRow.style.flexShrink = 0.0f;

            previewSourceField = new ObjectField("Preview Source") {
                objectType = typeof(AnimationGraphRunner),
                allowSceneObjects = true,
                tooltip = "Current preview source Runner",
            };
            previewSourceField.SetEnabled(false);
            previewSourceField.style.minWidth = PreviewSourceFieldWidth;
            previewSourceField.style.flexGrow = 1.0f;
            previewSourceField.style.marginRight = 4.0f;
            sourceRow.Add(previewSourceField);
            previewPlayButton = new ToolbarButton {
                tooltip = "Play preview",
            };
            previewStopButton = new ToolbarButton {
                tooltip = "Stop preview and restore sampled values",
            };
            ConfigurePreviewIconButton(previewPlayButton);
            ConfigurePreviewIconButton(previewStopButton);
            previewTimeSlider = new Slider {
                lowValue = 0.0f,
                highValue = 0.0f,
                value = 0.0f,
                tooltip = "Preview time",
            };
            previewTimeSlider.style.minWidth = PreviewSliderWidth;
            previewTimeSlider.style.flexGrow = 1.0f;
            previewTimeLabel = new Label("0.00 / 0.00s") {
                tooltip = "Preview time",
            };
            previewTimeLabel.style.width = PreviewTimeLabelWidth;
            previewTimeLabel.style.minWidth = PreviewTimeLabelWidth;
            previewTimeLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            previewTimeLabel.style.marginLeft = 4.0f;
            previewTimeLabel.style.marginRight = 4.0f;
            playbackRow.Add(previewPlayButton);
            playbackRow.Add(previewStopButton);
            playbackRow.Add(previewTimeSlider);
            playbackRow.Add(previewTimeLabel);
            controls.Add(sourceRow);
            controls.Add(playbackRow);

            return controls;
        }

        /// <summary>
        /// Preview 用 icon button の寸法と配置を設定
        /// </summary>
        /// <param name="button">設定する button</param>
        private void ConfigurePreviewIconButton(ToolbarButton button) {
            button.style.width = PreviewIconButtonWidth;
            button.style.minWidth = PreviewIconButtonWidth;
            button.style.maxWidth = PreviewIconButtonWidth;
            button.style.flexShrink = 0.0f;
            button.style.alignItems = Align.Center;
            button.style.justifyContent = Justify.Center;
        }

        /// <summary>
        /// GraphView、Preview 操作領域、Inspector、Schema を含む Body を作成
        /// </summary>
        /// <param name="schemaView">Schema view</param>
        /// <param name="graphView">Graph view</param>
        /// <param name="previewControls">Preview 操作領域</param>
        /// <param name="inspectorView">Inspector view</param>
        /// <returns>Body 領域</returns>
        private VisualElement CreateBody(AnimationGraphSchemaView schemaView, AnimationGraphView graphView, VisualElement previewControls, AnimationGraphInspectorView inspectorView) {
            var body = new TwoPaneSplitView(1, Mathf.Max(MinSidePanelWidth, _sidePanelWidth), TwoPaneSplitViewOrientation.Horizontal) {
                style = {
                    flexGrow = 1.0f,
                    minHeight = 0.0f,
                },
            };

            _sidePanel = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Column,
                    minWidth = MinSidePanelWidth,
                    minHeight = 0.0f,
                    borderLeftWidth = 1.0f,
                    borderLeftColor = new Color(0.18f, 0.18f, 0.18f),
                },
            };
            var sideSplitView = new TwoPaneSplitView(1, Mathf.Max(MinSchemaPanelHeight, _schemaPanelHeight), TwoPaneSplitViewOrientation.Vertical) {
                style = {
                    flexGrow = 1.0f,
                    minHeight = 0.0f,
                },
            };

            graphView.style.flexGrow = 1.0f;
            graphView.style.minWidth = MinGraphViewWidth;
            var graphPanel = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Column,
                    flexGrow = 1.0f,
                    minWidth = MinGraphViewWidth,
                    minHeight = 0.0f,
                },
            };
            inspectorView.style.flexGrow = 1.0f;
            inspectorView.style.minHeight = MinInspectorPanelHeight;
            inspectorView.style.borderLeftWidth = 0.0f;
            schemaView.style.minHeight = MinSchemaPanelHeight;
            schemaView.style.flexShrink = 0.0f;
            _schemaPanel = schemaView;

            sideSplitView.Add(inspectorView);
            sideSplitView.Add(schemaView);
            _sidePanel.Add(sideSplitView);
            _schemaPanel.RegisterCallback<GeometryChangedEvent>(OnSchemaPanelGeometryChanged);
            _sidePanel.RegisterCallback<GeometryChangedEvent>(OnSidePanelGeometryChanged);
            graphPanel.Add(graphView);
            graphPanel.Add(previewControls);
            body.Add(graphPanel);
            body.Add(_sidePanel);
            return body;
        }

        private static Label CreateFooter() {
            var footerLabel = new Label();
            footerLabel.style.flexShrink = 0.0f;
            footerLabel.style.height = 22.0f;
            footerLabel.style.paddingLeft = 8.0f;
            footerLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            return footerLabel;
        }

        private void OnGraphAssetChanged(ChangeEvent<UnityEngine.Object> evt) {
            _graphAsset = (AnimationGraphAsset)evt.newValue;
            EditorUtility.SetDirty(this);
        }

        private void SetGraphAsset(AnimationGraphAsset graphAsset) {
            _graphAsset = graphAsset;
            if (_graphAssetField != null) {
                _graphAssetField.value = graphAsset;
                return;
            }

            EditorUtility.SetDirty(this);
        }

        private void OnInspectedNodeIdsChanged(IReadOnlyList<string> inspectedNodeIds) {
            _inspectedNodeIds = new string[inspectedNodeIds.Count];
            for (var i = 0; i < inspectedNodeIds.Count; i++) {
                _inspectedNodeIds[i] = inspectedNodeIds[i];
            }

            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Presenter と View callback を解放
        /// </summary>
        private void ClearPresenter() {
            if (_schemaPanel != null) {
                _schemaPanel.UnregisterCallback<GeometryChangedEvent>(OnSchemaPanelGeometryChanged);
                _schemaPanel = null;
            }

            if (_sidePanel != null) {
                _sidePanel.UnregisterCallback<GeometryChangedEvent>(OnSidePanelGeometryChanged);
                _sidePanel = null;
            }

            if (_graphAssetField != null) {
                _graphAssetField.UnregisterValueChangedCallback(OnGraphAssetChanged);
                _graphAssetField = null;
            }

            if (_presenter == null) {
                return;
            }

            _presenter.InspectedNodeIdsChanged -= OnInspectedNodeIdsChanged;
            _presenter.Dispose();
            _presenter = null;
        }

        private void OnSidePanelGeometryChanged(GeometryChangedEvent evt) {
            var nextWidth = Mathf.Max(MinSidePanelWidth, evt.newRect.width);
            if (Mathf.Abs(_sidePanelWidth - nextWidth) < 0.5f) {
                return;
            }

            _sidePanelWidth = nextWidth;
            EditorUtility.SetDirty(this);
        }

        private void OnSchemaPanelGeometryChanged(GeometryChangedEvent evt) {
            var nextHeight = Mathf.Max(MinSchemaPanelHeight, evt.newRect.height);
            if (Mathf.Abs(_schemaPanelHeight - nextHeight) < 0.5f) {
                return;
            }

            _schemaPanelHeight = nextHeight;
            EditorUtility.SetDirty(this);
        }
    }
}
