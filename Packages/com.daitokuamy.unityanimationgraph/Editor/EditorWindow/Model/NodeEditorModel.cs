using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Preview 中の Node 実行状態
    /// </summary>
    internal enum NodePreviewExecutionState {
        /// <summary>未実行</summary>
        None,
        /// <summary>実行完了済み</summary>
        Completed,
        /// <summary>実行中</summary>
        Active,
    }

    /// <summary>
    /// Preview 中の Node 実行情報
    /// </summary>
    internal readonly struct NodePreviewExecutionInfo : IEquatable<NodePreviewExecutionInfo> {
        /// <summary>Preview 中の実行状態</summary>
        public NodePreviewExecutionState State { get; }
        /// <summary>Preview 中の進捗率</summary>
        public float Progress { get; }

        /// <summary>
        /// NodePreviewExecutionInfo を作成
        /// </summary>
        /// <param name="state">Preview 中の実行状態</param>
        /// <param name="progress">Preview 中の進捗率</param>
        public NodePreviewExecutionInfo(NodePreviewExecutionState state, float progress) {
            State = state;
            Progress = Mathf.Clamp01(progress);
        }

        /// <inheritdoc/>
        public bool Equals(NodePreviewExecutionInfo other) {
            return State == other.State && Progress.Equals(other.Progress);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            return obj is NodePreviewExecutionInfo other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            unchecked {
                return ((int)State * 397) ^ Progress.GetHashCode();
            }
        }
    }

    /// <summary>
    /// GraphView 詳細に表示する Node field の情報
    /// </summary>
    internal readonly struct NodeDetailField {
        /// <summary>SerializedProperty path</summary>
        public string PropertyPath { get; }
        /// <summary>GraphView 詳細に表示するラベル</summary>
        public string Label { get; }
        /// <summary>SerializedProperty の型</summary>
        public SerializedPropertyType PropertyType { get; }
        /// <summary>Target key field の場合は true</summary>
        public bool IsTargetKey { get; }
        /// <summary>Blackboard key field の場合は true</summary>
        public bool IsBlackboardKey { get; }
        /// <summary>Blackboard value type filter を持つ場合は true</summary>
        public bool HasBlackboardValueTypeFilter { get; }
        /// <summary>Blackboard value type filter</summary>
        public BlackboardValueType BlackboardValueType { get; }

        /// <summary>
        /// NodeDetailField を作成
        /// </summary>
        /// <param name="propertyPath">SerializedProperty path</param>
        /// <param name="label">GraphView 詳細に表示するラベル</param>
        /// <param name="propertyType">SerializedProperty の型</param>
        /// <param name="isTargetKey">Target key field の場合は true</param>
        /// <param name="isBlackboardKey">Blackboard key field の場合は true</param>
        /// <param name="hasBlackboardValueTypeFilter">Blackboard value type filter を持つ場合は true</param>
        /// <param name="blackboardValueType">Blackboard value type filter</param>
        public NodeDetailField(
            string propertyPath,
            string label,
            SerializedPropertyType propertyType,
            bool isTargetKey,
            bool isBlackboardKey,
            bool hasBlackboardValueTypeFilter,
            BlackboardValueType blackboardValueType) {
            PropertyPath = propertyPath ?? string.Empty;
            Label = label ?? string.Empty;
            PropertyType = propertyType;
            IsTargetKey = isTargetKey;
            IsBlackboardKey = isBlackboardKey;
            HasBlackboardValueTypeFilter = hasBlackboardValueTypeFilter;
            BlackboardValueType = blackboardValueType;
        }
    }

    /// <summary>
    /// Editor MVP の Model として Node の参照情報を提供するクラス
    /// </summary>
    public class NodeEditorModel {
        private readonly Node _node;
        private IReadOnlyList<NodeDetailField> _detailFields;
        private NodePreviewExecutionInfo _previewExecutionInfo;
        private bool _hasPreviewExecutionInfo;

        /// <summary>グラフ内で一意なノード ID</summary>
        public string NodeId => _node.NodeId;
        /// <summary>ノード型</summary>
        public Type NodeType => _node.GetType();
        /// <summary>ノード表示名</summary>
        public string DisplayName => _node.DisplayName;
        /// <summary>ノード名</summary>
        public string Name => _node.name ?? string.Empty;
        /// <summary>エディタ上のノード位置</summary>
        public Vector2 GraphPosition => _node.GraphPosition;
        /// <summary>後続ノード ID の一覧</summary>
        public IReadOnlyList<string> NextNodeIds => _node.NextNodeIds;
        /// <summary>Enter シグナル用の出力 Port を表示する場合は true</summary>
        public bool EnableEnterSignalPort => _node.EnableEnterSignalPort;
        /// <summary>Exit シグナル用の出力 Port を表示する場合は true</summary>
        public bool EnableExitSignalPort => _node.EnableExitSignalPort;
        /// <summary>Enter 時に通知する Signal 一覧</summary>
        public IReadOnlyList<Signal> EnterSignals => _node.EnterSignals;
        /// <summary>Exit 時に通知する Signal 一覧</summary>
        public IReadOnlyList<Signal> ExitSignals => _node.ExitSignals;
        /// <summary>GraphView 詳細に表示する field 一覧</summary>
        internal IReadOnlyList<NodeDetailField> DetailFields => _detailFields ??= CreateDetailFields(_node);
        /// <summary>参照元の Node</summary>
        internal Node Node => _node;

        /// <summary>
        /// NodeEditorModel を作成
        /// </summary>
        /// <param name="node">参照する Node</param>
        protected internal NodeEditorModel(Node node) {
            _node = node ?? throw new ArgumentNullException(nameof(node));
        }

        /// <summary>
        /// Node に対応する NodeEditorModel を作成
        /// </summary>
        /// <param name="node">参照する Node</param>
        /// <returns>Node に対応する NodeEditorModel</returns>
        internal static NodeEditorModel Create(Node node) {
            return node switch {
                BranchNode branchNode => new BranchNodeEditorModel(branchNode),
                LoopNode loopNode => new LoopNodeEditorModel(loopNode),
                DelayNode delayNode => new DelayNodeEditorModel(delayNode),
                _ => new NodeEditorModel(node),
            };
        }

        /// <summary>
        /// エディタ上のノード位置を設定
        /// </summary>
        /// <param name="graphPosition">エディタ上のノード位置</param>
        public void SetGraphPosition(Vector2 graphPosition) {
            AnimationGraphAssetUtility.SetNodeGraphPosition(_node, graphPosition);
        }

        /// <summary>
        /// Preview 中の実行情報取得を試行
        /// </summary>
        /// <param name="previewExecutionInfo">取得した実行情報</param>
        /// <returns>実行情報を取得できた場合は true</returns>
        internal bool TryGetPreviewExecutionInfo(out NodePreviewExecutionInfo previewExecutionInfo) {
            previewExecutionInfo = _previewExecutionInfo;
            return _hasPreviewExecutionInfo;
        }

        /// <summary>
        /// 後続ノード ID 一覧を設定
        /// </summary>
        /// <param name="nextNodeIds">設定する後続ノード ID 一覧</param>
        internal void SetNextNodeIds(IReadOnlyList<string> nextNodeIds) {
            AnimationGraphAssetUtility.SetNodeNextNodeIds(_node, nextNodeIds);
        }

        /// <summary>
        /// Enter シグナル用の出力 Port 表示フラグを設定
        /// </summary>
        /// <param name="enabled">表示する場合は true</param>
        internal void SetEnterSignalPortEnabled(bool enabled) {
            AnimationGraphAssetUtility.SetNodeEnterSignalPortEnabled(_node, enabled);
        }

        /// <summary>
        /// Exit シグナル用の出力 Port 表示フラグを設定
        /// </summary>
        /// <param name="enabled">表示する場合は true</param>
        internal void SetExitSignalPortEnabled(bool enabled) {
            AnimationGraphAssetUtility.SetNodeExitSignalPortEnabled(_node, enabled);
        }

        /// <summary>
        /// Sets the target key when this model wraps an ActionNode.
        /// </summary>
        /// <param name="targetKey">Target key to set</param>
        internal void SetActionTargetKey(string targetKey) {
            if (_node is not ActionNode actionNode) {
                return;
            }

            AnimationGraphAssetUtility.SetActionNodeTargetKey(actionNode, targetKey);
        }

        /// <summary>
        /// GraphView 詳細 field の string 値を取得
        /// </summary>
        /// <param name="field">取得対象 field</param>
        /// <returns>取得した string 値</returns>
        internal string GetDetailFieldStringValue(NodeDetailField field) {
            var property = FindDetailFieldProperty(field);
            return property != null && property.propertyType == SerializedPropertyType.String
                ? property.stringValue
                : string.Empty;
        }

        /// <summary>
        /// GraphView 詳細 field の bool 値を取得
        /// </summary>
        /// <param name="field">取得対象 field</param>
        /// <returns>取得した bool 値</returns>
        internal bool GetDetailFieldBoolValue(NodeDetailField field) {
            var property = FindDetailFieldProperty(field);
            return property != null && property.propertyType == SerializedPropertyType.Boolean && property.boolValue;
        }

        /// <summary>
        /// GraphView 詳細 field の int 値を取得
        /// </summary>
        /// <param name="field">取得対象 field</param>
        /// <returns>取得した int 値</returns>
        internal int GetDetailFieldIntValue(NodeDetailField field) {
            var property = FindDetailFieldProperty(field);
            return property != null && property.propertyType == SerializedPropertyType.Integer
                ? property.intValue
                : 0;
        }

        /// <summary>
        /// GraphView 詳細 field の float 値を取得
        /// </summary>
        /// <param name="field">取得対象 field</param>
        /// <returns>取得した float 値</returns>
        internal float GetDetailFieldFloatValue(NodeDetailField field) {
            var property = FindDetailFieldProperty(field);
            return property != null && property.propertyType == SerializedPropertyType.Float
                ? property.floatValue
                : 0.0f;
        }

        /// <summary>
        /// GraphView 詳細 field の string 値を設定
        /// </summary>
        /// <param name="field">設定対象 field</param>
        /// <param name="value">設定する値</param>
        internal void SetDetailFieldValue(NodeDetailField field, string value) {
            AnimationGraphAssetUtility.SetNodeDetailFieldValue(_node, field, value);
        }

        /// <summary>
        /// GraphView 詳細 field の bool 値を設定
        /// </summary>
        /// <param name="field">設定対象 field</param>
        /// <param name="value">設定する値</param>
        internal void SetDetailFieldValue(NodeDetailField field, bool value) {
            AnimationGraphAssetUtility.SetNodeDetailFieldValue(_node, field, value);
        }

        /// <summary>
        /// GraphView 詳細 field の int 値を設定
        /// </summary>
        /// <param name="field">設定対象 field</param>
        /// <param name="value">設定する値</param>
        internal void SetDetailFieldValue(NodeDetailField field, int value) {
            AnimationGraphAssetUtility.SetNodeDetailFieldValue(_node, field, value);
        }

        /// <summary>
        /// GraphView 詳細 field の float 値を設定
        /// </summary>
        /// <param name="field">設定対象 field</param>
        /// <param name="value">設定する値</param>
        internal void SetDetailFieldValue(NodeDetailField field, float value) {
            AnimationGraphAssetUtility.SetNodeDetailFieldValue(_node, field, value);
        }

        /// <summary>
        /// Sets the join type when this model wraps a JoinNode.
        /// </summary>
        /// <param name="joinType">Join type to set</param>
        internal void SetJoinType(JoinType joinType) {
            if (_node is not JoinNode joinNode) {
                return;
            }

            AnimationGraphAssetUtility.SetJoinNodeJoinType(joinNode, joinType);
        }

        /// <summary>
        /// Preview 中の実行情報を設定
        /// </summary>
        /// <param name="previewExecutionInfo">設定する実行情報</param>
        /// <returns>表示情報が変化した場合は true</returns>
        internal bool SetPreviewExecutionInfo(NodePreviewExecutionInfo previewExecutionInfo) {
            if (_hasPreviewExecutionInfo && _previewExecutionInfo.Equals(previewExecutionInfo)) {
                return false;
            }

            _previewExecutionInfo = previewExecutionInfo;
            _hasPreviewExecutionInfo = true;
            return true;
        }

        /// <summary>
        /// Preview 中の実行情報を消去
        /// </summary>
        /// <returns>表示情報が変化した場合は true</returns>
        internal bool ClearPreviewExecutionInfo() {
            if (!_hasPreviewExecutionInfo) {
                return false;
            }

            _previewExecutionInfo = default;
            _hasPreviewExecutionInfo = false;
            return true;
        }

        private SerializedProperty FindDetailFieldProperty(NodeDetailField field) {
            if (string.IsNullOrEmpty(field.PropertyPath)) {
                return null;
            }

            var serializedNode = new SerializedObject(_node);
            return serializedNode.FindProperty(field.PropertyPath);
        }

        private static IReadOnlyList<NodeDetailField> CreateDetailFields(Node node) {
            if (node == null) {
                return Array.Empty<NodeDetailField>();
            }

            var fields = new List<NodeDetailField>();
            var serializedNode = new SerializedObject(node);
            foreach (var fieldInfo in EnumerateNodeFields(node.GetType())) {
                var detailFieldAttribute = fieldInfo.GetCustomAttribute<NodeDetailFieldAttribute>();
                if (detailFieldAttribute == null || !IsSerializedField(fieldInfo)) {
                    continue;
                }

                var property = serializedNode.FindProperty(fieldInfo.Name);
                if (property == null || !CanEditDetailProperty(property.propertyType)) {
                    continue;
                }

                var blackboardKeyAttribute = fieldInfo.GetCustomAttribute<BlackboardKeyAttribute>();
                fields.Add(new NodeDetailField(
                    fieldInfo.Name,
                    GetDetailFieldLabel(fieldInfo, detailFieldAttribute),
                    property.propertyType,
                    fieldInfo.GetCustomAttribute<TargetKeyAttribute>() != null,
                    blackboardKeyAttribute != null,
                    blackboardKeyAttribute?.HasValueTypeFilter ?? false,
                    blackboardKeyAttribute?.ValueType ?? default));
            }

            return fields;
        }

        private static IEnumerable<FieldInfo> EnumerateNodeFields(Type nodeType) {
            var types = new Stack<Type>();
            for (var currentType = nodeType; currentType != null && typeof(Node).IsAssignableFrom(currentType); currentType = currentType.BaseType) {
                types.Push(currentType);
            }

            while (types.Count > 0) {
                var currentType = types.Pop();
                var fields = currentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                for (var i = 0; i < fields.Length; i++) {
                    yield return fields[i];
                }
            }
        }

        private static bool IsSerializedField(FieldInfo fieldInfo) {
            if (fieldInfo.IsStatic || fieldInfo.IsNotSerialized) {
                return false;
            }

            return fieldInfo.IsPublic || fieldInfo.GetCustomAttribute<SerializeField>() != null;
        }

        private static bool CanEditDetailProperty(SerializedPropertyType propertyType) {
            return propertyType is SerializedPropertyType.String
                or SerializedPropertyType.Boolean
                or SerializedPropertyType.Integer
                or SerializedPropertyType.Float;
        }

        private static string GetDetailFieldLabel(FieldInfo fieldInfo, NodeDetailFieldAttribute attribute) {
            if (!string.IsNullOrEmpty(attribute.Label)) {
                return attribute.Label;
            }

            var fieldName = fieldInfo.Name.StartsWith("_", StringComparison.Ordinal)
                ? fieldInfo.Name.Substring(1)
                : fieldInfo.Name;
            return ObjectNames.NicifyVariableName(fieldName);
        }
    }
}
