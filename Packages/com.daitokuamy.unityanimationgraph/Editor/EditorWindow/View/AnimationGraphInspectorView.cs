using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// 選択中 Node の Inspector を表示する View
    /// </summary>
    internal sealed class AnimationGraphInspectorView : VisualElement, IDisposable {
        private readonly Label _messageLabel;
        private readonly IMGUIContainer _inspectorContainer;

        private UnityEditor.Editor _editor;
        private Vector2 _scrollPosition;
        private bool _isReadOnly;

        /// <summary>表示中 Node の serialized property が変更されたときに発火</summary>
        public event Action NodePropertiesChanged;

        /// <summary>
        /// AnimationGraphInspectorView を作成
        /// </summary>
        public AnimationGraphInspectorView() {
            style.borderLeftWidth = 1.0f;
            style.borderLeftColor = new Color(0.18f, 0.18f, 0.18f);
            style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);

            _messageLabel = new Label("No node selected");
            _messageLabel.style.paddingLeft = 8.0f;
            _messageLabel.style.paddingRight = 8.0f;
            _messageLabel.style.paddingTop = 8.0f;
            _messageLabel.style.whiteSpace = WhiteSpace.Normal;
            Add(_messageLabel);

            _inspectorContainer = new IMGUIContainer(DrawInspector) {
                style = {
                    flexGrow = 1.0f,
                    paddingLeft = 6.0f,
                    paddingRight = 6.0f,
                    paddingTop = 6.0f,
                },
            };
            Add(_inspectorContainer);
        }

        /// <summary>
        /// Inspector の表示対象を設定
        /// </summary>
        /// <param name="nodeModels">選択中のノード Model 一覧</param>
        public void SetSelection(IReadOnlyList<NodeEditorModel> nodeModels) {
            if (nodeModels == null) {
                throw new ArgumentNullException(nameof(nodeModels));
            }

            DestroyEditor();
            _scrollPosition = Vector2.zero;
            if (nodeModels.Count == 0) {
                _messageLabel.text = "No node selected";
                _messageLabel.style.display = DisplayStyle.Flex;
                return;
            }

            var nodeType = nodeModels[0].NodeType;
            for (var i = 1; i < nodeModels.Count; i++) {
                if (nodeModels[i].NodeType == nodeType) {
                    continue;
                }

                _messageLabel.text = "Multiple node types selected";
                _messageLabel.style.display = DisplayStyle.Flex;
                return;
            }

            var targets = new UnityEngine.Object[nodeModels.Count];
            for (var i = 0; i < nodeModels.Count; i++) {
                targets[i] = nodeModels[i].Node;
            }

            _editor = UnityEditor.Editor.CreateEditor(targets);
            _messageLabel.style.display = DisplayStyle.None;
            _inspectorContainer.MarkDirtyRepaint();
        }

        /// <summary>
        /// Inspector の表示対象 Signal を設定
        /// </summary>
        /// <param name="signalModels">選択中の Signal Model 一覧</param>
        public void SetSignalSelection(IReadOnlyList<SignalEditorModel> signalModels) {
            if (signalModels == null) {
                throw new ArgumentNullException(nameof(signalModels));
            }

            DestroyEditor();
            _scrollPosition = Vector2.zero;
            if (signalModels.Count == 0) {
                _messageLabel.text = "No node selected";
                _messageLabel.style.display = DisplayStyle.Flex;
                return;
            }

            var signalType = signalModels[0].SignalType;
            for (var i = 1; i < signalModels.Count; i++) {
                if (signalModels[i].SignalType == signalType) {
                    continue;
                }

                _messageLabel.text = "Multiple signal types selected";
                _messageLabel.style.display = DisplayStyle.Flex;
                return;
            }

            var targets = new UnityEngine.Object[signalModels.Count];
            for (var i = 0; i < signalModels.Count; i++) {
                targets[i] = signalModels[i].Signal;
            }

            _editor = UnityEditor.Editor.CreateEditor(targets);
            _messageLabel.style.display = DisplayStyle.None;
            _inspectorContainer.MarkDirtyRepaint();
        }

        /// <summary>
        /// Inspector の編集可否を設定
        /// </summary>
        /// <param name="isReadOnly">編集を禁止する場合は true</param>
        public void SetReadOnly(bool isReadOnly) {
            if (_isReadOnly == isReadOnly) {
                return;
            }

            _isReadOnly = isReadOnly;
            _inspectorContainer.MarkDirtyRepaint();
        }

        /// <inheritdoc/>
        public void Dispose() {
            DestroyEditor();
        }

        private void DrawInspector() {
            if (_editor == null) {
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            _editor.serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            using (new EditorGUI.DisabledScope(_isReadOnly)) {
                _editor.OnInspectorGUI();
            }

            var changed = EditorGUI.EndChangeCheck();
            if (!_isReadOnly) {
                _editor.serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.EndScrollView();
            if (!_isReadOnly && changed) {
                NodePropertiesChanged?.Invoke();
            }
        }

        private void DestroyEditor() {
            if (_editor == null) {
                return;
            }

            UnityEngine.Object.DestroyImmediate(_editor);
            _editor = null;
        }
    }
}
