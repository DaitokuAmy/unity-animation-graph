using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// NodeEditorModel を表示する GraphView Node
    /// </summary>
    internal sealed class AnimationGraphNodeView : UnityEditor.Experimental.GraphView.Node {
        private const float PreviewValueEpsilon = 0.0001f;

        private static readonly Vector2 DefaultSize = new(236.0f, 112.0f);
        private static readonly Color DetailLabelColor = new(0.56f, 0.56f, 0.56f);
        private static readonly Color DetailValueColor = new(0.86f, 0.86f, 0.86f);

        private readonly Dictionary<AnimationGraphOutputPortKind, Port> _outputPortsByKind = new();
        private readonly Func<IReadOnlyList<AnimationGraphTargetDefinition>> _targetDefinitionsProvider;
        private readonly Func<IReadOnlyList<AnimationGraphBlackboardDefinition>> _blackboardDefinitionsProvider;
        private readonly VisualElement _detailsContainer;

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
        /// <summary>DelayNode delay change request</summary>
        public event Action<DelayNodeEditorModel, float> DelayChanged;
        /// <summary>FlagBranchNode flag key change request</summary>
        public event Action<NodeEditorModel, string> FlagBranchKeyChanged;
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
        public AnimationGraphNodeView(NodeEditorModel nodeModel, Func<IReadOnlyList<AnimationGraphTargetDefinition>> targetDefinitionsProvider, Func<IReadOnlyList<AnimationGraphBlackboardDefinition>> blackboardDefinitionsProvider) {
            NodeModel = nodeModel ?? throw new ArgumentNullException(nameof(nodeModel));
            _targetDefinitionsProvider = targetDefinitionsProvider ?? throw new ArgumentNullException(nameof(targetDefinitionsProvider));
            _blackboardDefinitionsProvider = blackboardDefinitionsProvider ?? throw new ArgumentNullException(nameof(blackboardDefinitionsProvider));
            title = nodeModel.DisplayName;
            viewDataKey = nodeModel.NodeId;
            capabilities |= Capabilities.Selectable | Capabilities.Movable | Capabilities.Deletable | Capabilities.Copiable;

            var keyColor = GetKeyColor(nodeModel.NodeType);
            ApplyKeyColor(keyColor);
            ApplyTitleStyle();

            _detailsContainer = CreateDetailsContainer();
            mainContainer.Insert(Mathf.Min(1, mainContainer.childCount), _detailsContainer);
            RefreshDetails();

            if (nodeModel.NodeType != typeof(StartNode)) {
                InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
                InputPort.portName = "In";
                InputPort.portColor = keyColor;
                inputContainer.Add(InputPort);
            }

            AddOutputPorts(keyColor);

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
                if (NodeModel.TryGetPreviewExecutionInfo(out var previewExecutionInfo)) {
                    AddPreviewRow(previewExecutionInfo);
                }
            }

            if (NodeModel is DelayNodeEditorModel delayNodeModel) {
                AddDelayField(delayNodeModel);
            }

            if (NodeModel is LoopNodeEditorModel loopNodeModel) {
                AddLoopCountField(loopNodeModel);
            }

            if (NodeModel.Node is FlagBranchNode flagBranchNode) {
                AddFlagKeyPopup(flagBranchNode.FlagKey, flagBranchNode.ExpectedValue);
            }

            if (NodeModel.Node is JoinNode joinNode) {
                AddJoinTypePopup(joinNode.JoinType);
            }

            _detailsContainer.style.display = _detailsContainer.childCount == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private void AddOutputPorts(Color keyColor) {
            AddOutputPort(AnimationGraphOutputPortKind.Next, GetNextPortName(NodeModel.NodeType), Port.Capacity.Multi, keyColor);
            if (typeof(BranchNode).IsAssignableFrom(NodeModel.NodeType)) {
                AddOutputPort(AnimationGraphOutputPortKind.False, "False", Port.Capacity.Multi, keyColor);
            }

            if (NodeModel.NodeType == typeof(LoopNode)) {
                AddOutputPort(AnimationGraphOutputPortKind.Loop, "Loop", Port.Capacity.Multi, keyColor);
            }
        }

        private void AddOutputPort(AnimationGraphOutputPortKind outputPortKind, string portName, Port.Capacity capacity, Color keyColor) {
            var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, capacity, typeof(bool));
            outputPort.portName = portName;
            outputPort.portColor = keyColor;
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

        private void AddFlagKeyPopup(string currentFlagKey, bool expectedValue) {
            var row = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                },
            };

            if (!expectedValue) {
                row.Add(CreateInvertedConditionLabel());
            }

            var choices = CreateBlackboardKeyChoices(_blackboardDefinitionsProvider(), AnimationGraphValueType.Bool, currentFlagKey);
            var selectedIndex = FindChoiceIndex(choices, currentFlagKey);
            var popup = new PopupField<string>(choices, selectedIndex);
            popup.formatSelectedValueCallback = value => FormatBlackboardKeyChoice(value, _blackboardDefinitionsProvider(), AnimationGraphValueType.Bool);
            popup.formatListItemCallback = value => FormatBlackboardKeyChoice(value, _blackboardDefinitionsProvider(), AnimationGraphValueType.Bool);
            popup.SetValueWithoutNotify(choices[selectedIndex]);
            popup.RegisterValueChangedCallback(evt => {
                var nextFlagKey = evt.newValue ?? string.Empty;
                if (nextFlagKey == currentFlagKey) {
                    return;
                }

                FlagBranchKeyChanged?.Invoke(NodeModel, nextFlagKey);
            });
            ConfigureInputElement(popup);
            row.Add(popup);
            _detailsContainer.Add(row);
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

        private void AddPreviewRow(NodePreviewExecutionInfo previewExecutionInfo) {
            var row = new VisualElement {
                pickingMode = PickingMode.Ignore,
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginTop = 3.0f,
                },
            };
            row.Add(CreateMetricGroup("DELAY", FormatPreviewRange(previewExecutionInfo.MinDelay, previewExecutionInfo.MaxDelay)));
            row.Add(CreateMetricGroup("DUR", FormatPreviewRange(previewExecutionInfo.MinDuration, previewExecutionInfo.MaxDuration)));
            if (previewExecutionInfo.ExecutionCount > 1) {
                row.Add(CreateExecutionCountLabel(previewExecutionInfo.ExecutionCount));
            }

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

        private static VisualElement CreateMetricGroup(string label, string value) {
            var group = new VisualElement {
                pickingMode = PickingMode.Ignore,
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginRight = 10.0f,
                    flexShrink = 0.0f,
                },
            };
            group.Add(CreateDetailLabel(label, 28.0f));
            group.Add(CreateValueLabel(value));
            return group;
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

        private static Label CreateInvertedConditionLabel() {
            return new Label("!") {
                pickingMode = PickingMode.Ignore,
                style = {
                    width = 12.0f,
                    minWidth = 12.0f,
                    flexShrink = 0.0f,
                    fontSize = 12,
                    color = DetailLabelColor,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    unityTextAlign = TextAnchor.MiddleLeft,
                },
            };
        }

        private static Label CreateExecutionCountLabel(int executionCount) {
            return new Label($"x{executionCount}") {
                pickingMode = PickingMode.Ignore,
                style = {
                    fontSize = 10,
                    color = DetailLabelColor,
                    unityTextAlign = TextAnchor.MiddleLeft,
                },
            };
        }

        private static string GetNextPortName(Type nodeType) {
            if (typeof(BranchNode).IsAssignableFrom(nodeType)) {
                return "True";
            }

            return "Next";
        }

        private static string GetDisplayValue(string value) {
            return string.IsNullOrEmpty(value) ? "-" : value;
        }

        private static List<string> CreateTargetKeyChoices(IReadOnlyList<AnimationGraphTargetDefinition> targetDefinitions, string currentTargetKey) {
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

        private static List<string> CreateBlackboardKeyChoices(IReadOnlyList<AnimationGraphBlackboardDefinition> blackboardDefinitions, AnimationGraphValueType valueType, string currentBlackboardKey) {
            var choices = new List<string> {
                string.Empty,
            };

            if (blackboardDefinitions != null) {
                for (var i = 0; i < blackboardDefinitions.Count; i++) {
                    var definition = blackboardDefinitions[i];
                    if (definition.ValueType != valueType) {
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

        private static string FormatTargetKeyChoice(string value, IReadOnlyList<AnimationGraphTargetDefinition> targetDefinitions) {
            if (string.IsNullOrEmpty(value)) {
                return "-";
            }

            return HasTargetDefinition(targetDefinitions, value) ? value : $"{value} (Missing)";
        }

        private static string FormatBlackboardKeyChoice(string value, IReadOnlyList<AnimationGraphBlackboardDefinition> blackboardDefinitions, AnimationGraphValueType valueType) {
            if (string.IsNullOrEmpty(value)) {
                return "-";
            }

            if (!TryGetBlackboardDefinition(blackboardDefinitions, value, out var definition)) {
                return $"{value} (Missing)";
            }

            return definition.ValueType == valueType ? value : $"{value} ({definition.ValueType})";
        }

        private static bool HasTargetDefinition(IReadOnlyList<AnimationGraphTargetDefinition> targetDefinitions, string value) {
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

        private static bool TryGetBlackboardDefinition(IReadOnlyList<AnimationGraphBlackboardDefinition> blackboardDefinitions, string value, out AnimationGraphBlackboardDefinition definition) {
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

        private static string FormatPreviewRange(float minValue, float maxValue) {
            if (Mathf.Abs(minValue - maxValue) <= PreviewValueEpsilon) {
                return FormatPreviewValue(minValue);
            }

            return $"{FormatPreviewValue(minValue)}..{FormatPreviewValue(maxValue)}";
        }

        private static string FormatPreviewValue(float value) {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private void ApplyKeyColor(Color keyColor) {
            style.borderTopColor = keyColor;
            style.borderRightColor = GetSubtleColor(keyColor);
            style.borderBottomColor = GetSubtleColor(keyColor);
            titleContainer.style.backgroundColor = GetTitleBackgroundColor(keyColor);
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
            return new Color(keyColor.r * 0.32f, keyColor.g * 0.32f, keyColor.b * 0.32f, 1.0f);
        }

        private static Color GetSubtleColor(Color keyColor) {
            return new Color(keyColor.r * 0.55f, keyColor.g * 0.55f, keyColor.b * 0.55f, 1.0f);
        }
    }
}
