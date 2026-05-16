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

        private void OnEnable() {
            titleContent = new GUIContent(WindowTitle);
        }

        private void OnDisable() {
            ClearPresenter();
            _presenter = null;
        }

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
            var footerLabel = CreateFooter();
            var body = CreateBody(schemaView, graphView, inspectorView);

            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.Add(header);
            rootVisualElement.Add(body);
            rootVisualElement.Add(footerLabel);

            _presenter = new AnimationGraphEditorPresenter();
            _presenter.InspectedNodeIdsChanged += OnInspectedNodeIdsChanged;
            _presenter.Initialize(graphAssetField, schemaView, graphView, inspectorView, footerLabel, _inspectedNodeIds);
        }

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

        private VisualElement CreateBody(AnimationGraphSchemaView schemaView, AnimationGraphView graphView, AnimationGraphInspectorView inspectorView) {
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
            body.Add(graphView);
            body.Add(_sidePanel);
            return body;
        }

        private static Label CreateFooter() {
            var footerLabel = new Label("Ctrl+C Copy  Ctrl+V Paste  Ctrl+D Duplicate  Delete Remove  Drag Move");
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
