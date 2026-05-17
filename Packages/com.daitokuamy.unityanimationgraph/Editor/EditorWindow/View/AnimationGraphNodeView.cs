using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// NodeEditorModel を表示する GraphView Node
    /// </summary>
    internal sealed class AnimationGraphNodeView : UnityEditor.Experimental.GraphView.Node {
        private const float TitleBackgroundBrightness = 0.50f;
        private const float TitleProgressRemainingBrightness = 0.32f;

        private static readonly Vector2 DefaultSize = new(236.0f, 112.0f);
        private static readonly Color DetailLabelColor = new(0.56f, 0.56f, 0.56f);
        private static readonly Color DetailValueColor = new(0.86f, 0.86f, 0.86f);
        private static readonly Color SignalPortColor = new(1.0f, 0.70f, 0.24f);
        private static readonly Color ValidationErrorColor = new(1.0f, 0.24f, 0.20f);
        private static readonly Color PreviewCompletedColor = new(0.24f, 0.82f, 0.36f);
        private static readonly Color PreviewActiveColor = new(1.0f, 0.82f, 0.22f);

        private readonly Dictionary<AnimationGraphOutputPortKind, Port> _outputPortsByKind = new();
        private readonly Func<IReadOnlyList<TargetDefinition>> _targetDefinitionsProvider;
        private readonly Func<IReadOnlyList<BlackboardDefinition>> _blackboardDefinitionsProvider;
        private readonly VisualElement _titleProgressFill;
        private readonly VisualElement _detailsContainer;
        private readonly Color _keyColor;
        private string _validationMessage;

        /// <summary>表示対象の NodeEditorModel</summary>
        public NodeEditorModel NodeModel { get; }
        /// <summary>入力 Port</summary>
        public Port InputPort { get; }
        /// <summary>出力 Port</summary>
        public Port OutputPort => GetOutputPort(AnimationGraphOutputPortKind.Next);
        /// <summary>選択状態が変化したときに発火</summary>
        public event Action SelectionChanged;
        /// <summary>ActionNode target key change request</summary>
        public event Action<NodeEditorModel, string> ActionTargetKeyChanged;
        /// <summary>Node detail string field change request</summary>
        public event Action<NodeEditorModel, NodeDetailField, string> DetailStringChanged;
        /// <summary>Node detail bool field change request</summary>
        public event Action<NodeEditorModel, NodeDetailField, bool> DetailBoolChanged;
        /// <summary>Node detail int field change request</summary>
        public event Action<NodeEditorModel, NodeDetailField, int> DetailIntChanged;
        /// <summary>Node detail float field change request</summary>
        public event Action<NodeEditorModel, NodeDetailField, float> DetailFloatChanged;
        /// <summary>DelayNode delay change request</summary>
        public event Action<DelayNodeEditorModel, float> DelayChanged;
        /// <summary>JoinNode join type change request</summary>
        public event Action<NodeEditorModel, JoinType> JoinTypeChanged;
        /// <summary>LoopNode loop count change request</summary>
        public event Action<LoopNodeEditorModel, int> LoopCountChanged;

        /// <summary>
        /// AnimationGraphNodeView を作成
        /// </summary>
        /// <param name="nodeModel">表示対象の NodeEditorModel</param>
        /// <param name="targetDefinitionsProvider">Provider for target key candidates</param>
        /// <param name="blackboardDefinitionsProvider">Provider for blackboard key candidates</param>
        public AnimationGraphNodeView(NodeEditorModel nodeModel, Func<IReadOnlyList<TargetDefinition>> targetDefinitionsProvider, Func<IReadOnlyList<BlackboardDefinition>> blackboardDefinitionsProvider) {
            NodeModel = nodeModel ?? throw new ArgumentNullException(nameof(nodeModel));
            _targetDefinitionsProvider = targetDefinitionsProvider ?? throw new ArgumentNullException(nameof(targetDefinitionsProvider));
            _blackboardDefinitionsProvider = blackboardDefinitionsProvider ?? throw new ArgumentNullException(nameof(blackboardDefinitionsProvider));
            title = nodeModel.DisplayName;
            viewDataKey = nodeModel.NodeId;
            capabilities |= Capabilities.Selectable | Capabilities.Movable | Capabilities.Deletable | Capabilities.Copiable;

            _keyColor = GetKeyColor(nodeModel.NodeType);
            _titleProgressFill = CreateTitleProgressFill();
            titleContainer.style.position = Position.Relative;
            titleContainer.style.overflow = Overflow.Hidden;
            titleContainer.Insert(0, _titleProgressFill);
            ApplyKeyColor(_keyColor);
            ApplyTitleStyle();

            _detailsContainer = CreateDetailsContainer();
            mainContainer.Insert(Mathf.Min(1, mainContainer.childCount), _detailsContainer);
            RefreshDetails();

            if (nodeModel.NodeType != typeof(StartNode)) {
                InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, GetInputPortCapacity(), typeof(bool));
                InputPort.portName = "In";
                InputPort.portColor = _keyColor;
                inputContainer.Add(InputPort);
            }

            AddOutputPorts(_keyColor);

            SetPosition(new Rect(nodeModel.GraphPosition, DefaultSize));
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

        /// <summary>
        /// 指定した output port を取得
        /// </summary>
        /// <param name="outputPortKind">取得する output port 種別</param>
        /// <returns>output port</returns>
        public Port GetOutputPort(AnimationGraphOutputPortKind outputPortKind) {
            return _outputPortsByKind.TryGetValue(outputPortKind, out var outputPort) ? outputPort : null;
        }

        /// <summary>
        /// output port の種別取得を試行
        /// </summary>
        /// <param name="port">判定する port</param>
        /// <param name="outputPortKind">取得した output port 種別</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetOutputPortKind(Port port, out AnimationGraphOutputPortKind outputPortKind) {
            foreach (var outputPortPair in _outputPortsByKind) {
                if (outputPortPair.Value != port) {
                    continue;
                }

                outputPortKind = outputPortPair.Key;
                return true;
            }

            outputPortKind = default;
            return false;
        }

        /// <summary>
        /// 表示中の Node 詳細を更新
        /// </summary>
        public void RefreshDetails() {
            _detailsContainer.Clear();

            if (NodeModel.Node is ActionNode actionNode) {
                AddTargetKeyPopup(actionNode.TargetKey);
            }

            if (NodeModel is DelayNodeEditorModel delayNodeModel) {
                AddDelayField(delayNodeModel);
            }

            if (NodeModel is LoopNodeEditorModel loopNodeModel) {
                AddLoopCountField(loopNodeModel);
            }

            for (var i = 0; i < NodeModel.DetailFields.Count; i++) {
                AddDetailField(NodeModel.DetailFields[i]);
            }

            if (NodeModel.Node is JoinNode joinNode) {
                AddJoinTypePopup(joinNode.JoinType);
            }

            ApplyNodeFrameColor();
            _detailsContainer.style.display = _detailsContainer.childCount == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>
        /// 検証エラー表示を設定
        /// </summary>
        /// <param name="message">検証エラーメッセージ</param>
        public void SetValidationMessage(string message) {
            _validationMessage = message;
            ApplyNodeFrameColor();
        }

        /// <summary>
        /// Preview 中の実行状態表示を更新
        /// </summary>
        public void RefreshPreviewExecutionState() {
            ApplyNodeFrameColor();
        }

        private void AddOutputPorts(Color keyColor) {
            if (NodeModel is BranchNodeEditorModel branchNodeModel) {
                AddOutputPort(AnimationGraphOutputPortKind.Next, branchNodeModel.PrimaryPortName, Port.Capacity.Multi, keyColor);
                for (var i = 0; i < branchNodeModel.ExtensionPortCount; i++) {
                    AddOutputPort(AnimationGraphOutputPortKind.BranchExtension(i), branchNodeModel.GetExtensionPortName(i), Port.Capacity.Multi, keyColor);
                }
            }
            else {
                AddOutputPort(AnimationGraphOutputPortKind.Next, "Next", Port.Capacity.Multi, keyColor);
            }

            if (NodeModel.NodeType == typeof(LoopNode)) {
                AddOutputPort(AnimationGraphOutputPortKind.Loop, "Loop", Port.Capacity.Multi, keyColor);
            }

            if (NodeModel.EnableEnterSignalPort) {
                AddOutputPort(AnimationGraphOutputPortKind.EnterSignal, "Enter", Port.Capacity.Multi, SignalPortColor, typeof(Signal));
            }

            if (NodeModel.EnableExitSignalPort) {
                AddOutputPort(AnimationGraphOutputPortKind.ExitSignal, "Exit", Port.Capacity.Multi, SignalPortColor, typeof(Signal));
            }
        }

        /// <summary>
        /// input port の接続数を Node 種別から取得
        /// </summary>
        /// <returns>input port の接続数</returns>
        private Port.Capacity GetInputPortCapacity() {
            return typeof(JoinNode).IsAssignableFrom(NodeModel.NodeType)
                ? Port.Capacity.Multi
                : Port.Capacity.Single;
        }

        private void AddOutputPort(AnimationGraphOutputPortKind outputPortKind, string portName, Port.Capacity capacity, Color keyColor, Type portType = null) {
            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, capacity, portType ?? typeof(bool));
            outputPort.portName = portName;
            outputPort.portColor = keyColor;
            if (outputPortKind.IsSignal) {
                outputPort.tooltip = "Connect to Signal";
            }

            _outputPortsByKind.Add(outputPortKind, outputPort);
            outputContainer.Add(outputPort);
        }

        private void AddDetailRow(string label, string value) {
            _detailsContainer.Add(CreateDetailRow(label, value));
        }

        private void AddTargetKeyPopup(string currentTargetKey) {
            var choices = CreateTargetKeyChoices(_targetDefinitionsProvider(), currentTargetKey);
            var selectedIndex = FindChoiceIndex(choices, currentTargetKey);
            var popup = new PopupField<string>(choices, selectedIndex);
            popup.formatSelectedValueCallback = value => FormatTargetKeyChoice(value, _targetDefinitionsProvider());
            popup.formatListItemCallback = value => FormatTargetKeyChoice(value, _targetDefinitionsProvider());
            popup.SetValueWithoutNotify(choices[selectedIndex]);
            popup.RegisterValueChangedCallback(evt => {
                var nextTargetKey = evt.newValue ?? string.Empty;
                if (nextTargetKey == currentTargetKey) {
                    return;
                }

                ActionTargetKeyChanged?.Invoke(NodeModel, nextTargetKey);
            });
            ConfigureInputElement(popup);
            popup.style.marginBottom = 3.0f;
            _detailsContainer.Add(popup);
        }

        private void AddJoinTypePopup(JoinType currentJoinType) {
            var choices = CreateJoinTypeChoices();
            var selectedIndex = choices.IndexOf(currentJoinType);
            if (selectedIndex < 0) {
                selectedIndex = 0;
            }

            var popup = new PopupField<JoinType>(choices, selectedIndex);
            popup.SetValueWithoutNotify(choices[selectedIndex]);
            popup.RegisterValueChangedCallback(evt => {
                if (evt.newValue == currentJoinType) {
                    return;
                }

                JoinTypeChanged?.Invoke(NodeModel, evt.newValue);
            });
            ConfigureInputElement(popup);
            _detailsContainer.Add(popup);
        }

        private void AddDelayField(DelayNodeEditorModel delayNodeModel) {
            var row = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                },
            };
            row.Add(CreateDetailLabel("Delay", 48.0f));

            var delayField = new FloatField {
                isDelayed = true,
                style = {
                    width = 72.0f,
                    minWidth = 72.0f,
                    height = 20.0f,
                    minHeight = 20.0f,
                    fontSize = 11,
                },
            };
            delayField.SetValueWithoutNotify(delayNodeModel.Delay);
            delayField.RegisterValueChangedCallback(evt => {
                var nextDelay = Mathf.Max(0.0f, evt.newValue);
                if (Mathf.Approximately(nextDelay, evt.previousValue)) {
                    return;
                }

                delayField.SetValueWithoutNotify(nextDelay);
                DelayChanged?.Invoke(delayNodeModel, nextDelay);
            });
            ConfigureInputElement(delayField);
            delayField.style.flexGrow = 0.0f;
            row.Add(delayField);
            _detailsContainer.Add(row);
        }

        private void AddLoopCountField(LoopNodeEditorModel loopNodeModel) {
            var row = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                },
            };
            row.Add(CreateDetailLabel("Count", 48.0f));

            var loopCountField = new IntegerField {
                isDelayed = true,
                style = {
                    width = 56.0f,
                    minWidth = 56.0f,
                    height = 20.0f,
                    minHeight = 20.0f,
                    fontSize = 11,
                },
            };
            loopCountField.SetValueWithoutNotify(loopNodeModel.LoopCount);
            loopCountField.RegisterValueChangedCallback(evt => {
                var nextLoopCount = Mathf.Max(1, evt.newValue);
                if (nextLoopCount == evt.previousValue) {
                    return;
                }

                loopCountField.SetValueWithoutNotify(nextLoopCount);
                LoopCountChanged?.Invoke(loopNodeModel, nextLoopCount);
            });
            ConfigureInputElement(loopCountField);
            loopCountField.style.flexGrow = 0.0f;
            row.Add(loopCountField);
            _detailsContainer.Add(row);
        }

        private void AddDetailField(NodeDetailField field) {
            if (field.PropertyType == SerializedPropertyType.String) {
                if (field.IsBlackboardKey) {
                    AddBlackboardKeyDetailField(field);
                    return;
                }

                if (field.IsTargetKey) {
                    AddTargetKeyDetailField(field);
                    return;
                }

                AddStringDetailField(field);
                return;
            }

            if (field.PropertyType == SerializedPropertyType.Boolean) {
                AddBoolDetailField(field);
                return;
            }

            if (field.PropertyType == SerializedPropertyType.Integer) {
                AddIntDetailField(field);
                return;
            }

            if (field.PropertyType == SerializedPropertyType.Float) {
                AddFloatDetailField(field);
            }
        }

        private void AddBlackboardKeyDetailField(NodeDetailField field) {
            var currentValue = NodeModel.GetDetailFieldStringValue(field);
            var choices = CreateBlackboardKeyChoices(_blackboardDefinitionsProvider(), field, currentValue);
            var selectedIndex = FindChoiceIndex(choices, currentValue);
            var popup = new PopupField<string>(choices, selectedIndex);
            popup.formatSelectedValueCallback = value => FormatBlackboardKeyChoice(value, _blackboardDefinitionsProvider(), field);
            popup.formatListItemCallback = value => FormatBlackboardKeyChoice(value, _blackboardDefinitionsProvider(), field);
            popup.SetValueWithoutNotify(choices[selectedIndex]);
            popup.RegisterValueChangedCallback(evt => {
                var nextValue = evt.newValue ?? string.Empty;
                if (nextValue == currentValue) {
                    return;
                }

                DetailStringChanged?.Invoke(NodeModel, field, nextValue);
            });
            AddDetailInputElement(field.Label, popup);
        }

        private void AddTargetKeyDetailField(NodeDetailField field) {
            var currentValue = NodeModel.GetDetailFieldStringValue(field);
            var choices = CreateTargetKeyChoices(_targetDefinitionsProvider(), currentValue);
            var selectedIndex = FindChoiceIndex(choices, currentValue);
            var popup = new PopupField<string>(choices, selectedIndex);
            popup.formatSelectedValueCallback = value => FormatTargetKeyChoice(value, _targetDefinitionsProvider());
            popup.formatListItemCallback = value => FormatTargetKeyChoice(value, _targetDefinitionsProvider());
            popup.SetValueWithoutNotify(choices[selectedIndex]);
            popup.RegisterValueChangedCallback(evt => {
                var nextValue = evt.newValue ?? string.Empty;
                if (nextValue == currentValue) {
                    return;
                }

                DetailStringChanged?.Invoke(NodeModel, field, nextValue);
            });
            AddDetailInputElement(field.Label, popup);
        }

        private void AddStringDetailField(NodeDetailField field) {
            var currentValue = NodeModel.GetDetailFieldStringValue(field);
            var textField = new TextField {
                isDelayed = true,
            };
            textField.SetValueWithoutNotify(currentValue);
            textField.RegisterValueChangedCallback(evt => {
                var nextValue = evt.newValue ?? string.Empty;
                if (nextValue == currentValue) {
                    return;
                }

                DetailStringChanged?.Invoke(NodeModel, field, nextValue);
            });
            AddDetailInputElement(field.Label, textField);
        }

        private void AddBoolDetailField(NodeDetailField field) {
            var currentValue = NodeModel.GetDetailFieldBoolValue(field);
            var toggle = new Toggle();
            toggle.SetValueWithoutNotify(currentValue);
            toggle.RegisterValueChangedCallback(evt => {
                if (evt.newValue == currentValue) {
                    return;
                }

                DetailBoolChanged?.Invoke(NodeModel, field, evt.newValue);
            });
            AddDetailInputElement(field.Label, toggle);
            toggle.style.flexGrow = 0.0f;
        }

        private void AddIntDetailField(NodeDetailField field) {
            var currentValue = NodeModel.GetDetailFieldIntValue(field);
            var intField = new IntegerField {
                isDelayed = true,
            };
            intField.SetValueWithoutNotify(currentValue);
            intField.RegisterValueChangedCallback(evt => {
                if (evt.newValue == currentValue) {
                    return;
                }

                DetailIntChanged?.Invoke(NodeModel, field, evt.newValue);
            });
            AddDetailInputElement(field.Label, intField);
        }

        private void AddFloatDetailField(NodeDetailField field) {
            var currentValue = NodeModel.GetDetailFieldFloatValue(field);
            var floatField = new FloatField {
                isDelayed = true,
            };
            floatField.SetValueWithoutNotify(currentValue);
            floatField.RegisterValueChangedCallback(evt => {
                if (Mathf.Approximately(evt.newValue, currentValue)) {
                    return;
                }

                DetailFloatChanged?.Invoke(NodeModel, field, evt.newValue);
            });
            AddDetailInputElement(field.Label, floatField);
        }

        private void AddDetailInputElement(string label, VisualElement inputElement) {
            var row = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                },
            };
            row.Add(CreateDetailLabel(label, 58.0f));
            ConfigureInputElement(inputElement);
            row.Add(inputElement);
            _detailsContainer.Add(row);
        }

        private static VisualElement CreateDetailsContainer() {
            return new VisualElement {
                style = {
                    marginLeft = 8.0f,
                    marginRight = 8.0f,
                    marginTop = 4.0f,
                    marginBottom = 2.0f,
                    paddingLeft = 2.0f,
                    paddingRight = 2.0f,
                    paddingTop = 2.0f,
                    paddingBottom = 2.0f,
                },
            };
        }

        private static VisualElement CreateTitleProgressFill() {
            return new VisualElement {
                pickingMode = PickingMode.Ignore,
                style = {
                    position = Position.Absolute,
                    left = 0.0f,
                    top = 0.0f,
                    bottom = 0.0f,
                    width = Length.Percent(0.0f),
                    display = DisplayStyle.None,
                },
            };
        }

        private static void ConfigureInputElement(VisualElement element) {
            element.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
            element.RegisterCallback<KeyDownEvent>(evt => evt.StopPropagation());
            element.style.height = 20.0f;
            element.style.minHeight = 20.0f;
            element.style.flexGrow = 1.0f;
            element.style.fontSize = 11;
        }

        private static VisualElement CreateDetailRow(string label, string value) {
            var row = new VisualElement {
                pickingMode = PickingMode.Ignore,
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                },
            };
            row.Add(CreateDetailLabel(label, 48.0f));
            row.Add(CreateValueLabel(value));
            return row;
        }

        private static Label CreateDetailLabel(string text, float width) {
            return new Label(text) {
                pickingMode = PickingMode.Ignore,
                style = {
                    width = width,
                    minWidth = width,
                    flexShrink = 0.0f,
                    fontSize = 10,
                    color = DetailLabelColor,
                    unityTextAlign = TextAnchor.MiddleLeft,
                },
            };
        }

        private static Label CreateValueLabel(string text) {
            return new Label(text) {
                pickingMode = PickingMode.Ignore,
                style = {
                    flexGrow = 1.0f,
                    flexShrink = 1.0f,
                    fontSize = 11,
                    color = DetailValueColor,
                    whiteSpace = WhiteSpace.Normal,
                    unityTextAlign = TextAnchor.MiddleLeft,
                },
            };
        }

        private static List<string> CreateTargetKeyChoices(IReadOnlyList<TargetDefinition> targetDefinitions, string currentTargetKey) {
            var choices = new List<string> {
                string.Empty,
            };

            if (targetDefinitions != null) {
                for (var i = 0; i < targetDefinitions.Count; i++) {
                    AddUniqueChoice(choices, targetDefinitions[i].Key);
                }
            }

            AddUniqueChoice(choices, currentTargetKey);
            return choices;
        }

        private static List<string> CreateBlackboardKeyChoices(IReadOnlyList<BlackboardDefinition> blackboardDefinitions, NodeDetailField field, string currentBlackboardKey) {
            var choices = new List<string> {
                string.Empty,
            };

            if (blackboardDefinitions != null) {
                for (var i = 0; i < blackboardDefinitions.Count; i++) {
                    var definition = blackboardDefinitions[i];
                    if (field.HasBlackboardValueTypeFilter && definition.ValueType != field.BlackboardValueType) {
                        continue;
                    }

                    AddUniqueChoice(choices, definition.Key);
                }
            }

            AddUniqueChoice(choices, currentBlackboardKey);
            return choices;
        }

        private static List<JoinType> CreateJoinTypeChoices() {
            return new List<JoinType> {
                JoinType.All,
                JoinType.Any,
            };
        }

        private static void AddUniqueChoice(List<string> choices, string value) {
            value ??= string.Empty;
            for (var i = 0; i < choices.Count; i++) {
                if (choices[i] == value) {
                    return;
                }
            }

            choices.Add(value);
        }

        private static int FindChoiceIndex(IReadOnlyList<string> choices, string value) {
            value ??= string.Empty;
            for (var i = 0; i < choices.Count; i++) {
                if (choices[i] == value) {
                    return i;
                }
            }

            return 0;
        }

        private static string FormatTargetKeyChoice(string value, IReadOnlyList<TargetDefinition> targetDefinitions) {
            if (string.IsNullOrEmpty(value)) {
                return "-";
            }

            return HasTargetDefinition(targetDefinitions, value) ? value : $"{value} (Missing)";
        }

        private static string FormatBlackboardKeyChoice(string value, IReadOnlyList<BlackboardDefinition> blackboardDefinitions, NodeDetailField field) {
            if (string.IsNullOrEmpty(value)) {
                return "-";
            }

            if (!TryGetBlackboardDefinition(blackboardDefinitions, value, out var definition)) {
                return $"{value} (Missing)";
            }

            return !field.HasBlackboardValueTypeFilter || definition.ValueType == field.BlackboardValueType
                ? value
                : $"{value} ({definition.ValueType})";
        }

        private static bool HasTargetDefinition(IReadOnlyList<TargetDefinition> targetDefinitions, string value) {
            if (targetDefinitions == null) {
                return false;
            }

            for (var i = 0; i < targetDefinitions.Count; i++) {
                if (targetDefinitions[i].Key == value) {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetBlackboardDefinition(IReadOnlyList<BlackboardDefinition> blackboardDefinitions, string value, out BlackboardDefinition definition) {
            if (blackboardDefinitions != null) {
                for (var i = 0; i < blackboardDefinitions.Count; i++) {
                    var currentDefinition = blackboardDefinitions[i];
                    if (currentDefinition.Key != value) {
                        continue;
                    }

                    definition = currentDefinition;
                    return true;
                }
            }

            definition = default;
            return false;
        }

        /// <summary>
        /// Node の状態に応じた枠色を適用
        /// </summary>
        private void ApplyNodeFrameColor() {
            tooltip = string.IsNullOrEmpty(_validationMessage) ? string.Empty : _validationMessage;
            ApplyKeyColor(_keyColor);
            ApplyPreviewTitleProgress();
            if (!string.IsNullOrEmpty(_validationMessage)) {
                ApplyBorderColor(ValidationErrorColor);
                return;
            }

            if (!NodeModel.TryGetPreviewExecutionInfo(out var previewExecutionInfo)) {
                return;
            }

            if (previewExecutionInfo.State == NodePreviewExecutionState.Active) {
                ApplyBorderColor(PreviewActiveColor);
                return;
            }

            if (previewExecutionInfo.State == NodePreviewExecutionState.Completed) {
                ApplyBorderColor(PreviewCompletedColor);
            }
        }

        /// <summary>
        /// Node 種別に応じた基本色を適用
        /// </summary>
        /// <param name="keyColor">Node 種別の色</param>
        private void ApplyKeyColor(Color keyColor) {
            style.borderTopColor = keyColor;
            style.borderRightColor = GetSubtleColor(keyColor);
            style.borderBottomColor = GetSubtleColor(keyColor);
            style.borderLeftColor = GetSubtleColor(keyColor);
            titleContainer.style.backgroundColor = GetTitleBackgroundColor(keyColor);
            if (_titleProgressFill != null) {
                _titleProgressFill.style.backgroundColor = GetTitleBackgroundColor(keyColor);
            }
        }

        /// <summary>
        /// Node の枠色を全辺に適用
        /// </summary>
        /// <param name="borderColor">適用する枠色</param>
        private void ApplyBorderColor(Color borderColor) {
            style.borderTopColor = borderColor;
            style.borderRightColor = borderColor;
            style.borderBottomColor = borderColor;
            style.borderLeftColor = borderColor;
        }

        private void ApplyPreviewTitleProgress() {
            if (!NodeModel.TryGetPreviewExecutionInfo(out var previewExecutionInfo)) {
                titleContainer.style.backgroundColor = GetTitleBackgroundColor(_keyColor);
                _titleProgressFill.style.display = DisplayStyle.None;
                _titleProgressFill.style.width = Length.Percent(0.0f);
                return;
            }

            var progress = Mathf.Clamp01(previewExecutionInfo.Progress);
            titleContainer.style.backgroundColor = GetTitleProgressRemainingColor(_keyColor);
            _titleProgressFill.style.display = DisplayStyle.Flex;
            _titleProgressFill.style.width = Length.Percent(progress * 100.0f);
            _titleProgressFill.style.backgroundColor = GetTitleBackgroundColor(_keyColor);
        }

        private void ApplyTitleStyle() {
            titleContainer.style.unityFontStyleAndWeight = FontStyle.Bold;
            var titleLabel = titleContainer.Q<Label>();
            if (titleLabel != null) {
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
        }

        private static Color GetKeyColor(Type nodeType) {
            if (nodeType == typeof(StartNode)) {
                return new Color(0.30f, 0.78f, 0.42f);
            }

            if (typeof(ControlNode).IsAssignableFrom(nodeType)) {
                return new Color(0.24f, 0.68f, 0.92f);
            }

            if (typeof(ActionNode).IsAssignableFrom(nodeType)) {
                return new Color(0.92f, 0.38f, 0.34f);
            }

            return new Color(0.72f, 0.72f, 0.72f);
        }

        private static Color GetTitleBackgroundColor(Color keyColor) {
            return new Color(keyColor.r * TitleBackgroundBrightness, keyColor.g * TitleBackgroundBrightness, keyColor.b * TitleBackgroundBrightness, 1.0f);
        }

        private static Color GetTitleProgressRemainingColor(Color keyColor) {
            return new Color(keyColor.r * TitleProgressRemainingBrightness, keyColor.g * TitleProgressRemainingBrightness, keyColor.b * TitleProgressRemainingBrightness, 1.0f);
        }

        private static Color GetSubtleColor(Color keyColor) {
            return new Color(keyColor.r * 0.55f, keyColor.g * 0.55f, keyColor.b * 0.55f, 1.0f);
        }
    }
}
