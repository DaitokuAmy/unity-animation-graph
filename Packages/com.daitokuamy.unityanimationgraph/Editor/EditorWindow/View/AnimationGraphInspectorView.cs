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
            _editor.OnInspectorGUI();
            var changed = EditorGUI.EndChangeCheck();
            _editor.serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();
            if (changed) {
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
