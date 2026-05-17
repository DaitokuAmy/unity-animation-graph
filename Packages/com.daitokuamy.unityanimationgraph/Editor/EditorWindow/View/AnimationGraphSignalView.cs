using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// SignalEditorModel を表示する GraphView Node
    /// </summary>
    internal sealed class AnimationGraphSignalView : UnityEditor.Experimental.GraphView.Node {
        private static readonly Vector2 DefaultSize = new(180.0f, 70.0f);
        private static readonly Color SignalColor = new(1.0f, 0.70f, 0.24f);

        /// <summary>表示対象の SignalEditorModel</summary>
        public SignalEditorModel SignalModel { get; }
        /// <summary>入力 Port</summary>
        public Port InputPort { get; }
        /// <summary>選択状態が変化したときに発火</summary>
        public event Action SelectionChanged;

        /// <summary>
        /// AnimationGraphSignalView を作成
        /// </summary>
        /// <param name="signalModel">表示対象の SignalEditorModel</param>
        public AnimationGraphSignalView(SignalEditorModel signalModel) {
            SignalModel = signalModel ?? throw new System.ArgumentNullException(nameof(signalModel));
            title = signalModel.DisplayName;
            viewDataKey = signalModel.SignalId;
            capabilities |= Capabilities.Selectable | Capabilities.Movable | Capabilities.Deletable | Capabilities.Copiable;

            style.borderTopColor = SignalColor;
            style.borderRightColor = SignalColor;
            style.borderBottomColor = SignalColor;
            style.borderLeftColor = SignalColor;
            titleContainer.style.backgroundColor = new Color(SignalColor.r * 0.32f, SignalColor.g * 0.32f, SignalColor.b * 0.32f, 1.0f);

            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(Signal));
            InputPort.portName = "In";
            InputPort.portColor = SignalColor;
            inputContainer.Add(InputPort);

            SetPosition(new Rect(signalModel.GraphPosition, DefaultSize));
            RefreshExpandedState();
            RefreshPorts();
        }

        /// <inheritdoc/>
        public override void OnSelected() {
            base.OnSelected();
            SelectionChanged?.Invoke();
        }

        /// <inheritdoc/>
        public override void OnUnselected() {
            base.OnUnselected();
            SelectionChanged?.Invoke();
        }
    }
}
