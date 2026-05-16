using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Drawer for selecting a blackboard key from an AnimationGraphAsset definition list.
    /// </summary>
    [CustomPropertyDrawer(typeof(BlackboardKeyAttribute))]
    internal sealed class BlackboardKeyAttributeDrawer : PropertyDrawer {
        /// <summary>Display label for an empty blackboard key.</summary>
        private const string EmptyLabel = "<None>";
        /// <summary>Suffix for a key that does not exist in blackboard definitions.</summary>
        private const string MissingSuffix = " (Missing)";

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType != SerializedPropertyType.String) {
                EditorGUI.PropertyField(position, property, label);
                EditorGUI.EndProperty();
                return;
            }

            var graphAsset = AnimationGraphPropertyDrawerUtility.FindGraphAsset(property.serializedObject.targetObjects);
            if (graphAsset == null) {
                EditorGUI.PropertyField(position, property, label);
                EditorGUI.EndProperty();
                return;
            }

            var blackboardKeyAttribute = (BlackboardKeyAttribute)attribute;
            var currentValue = property.stringValue ?? string.Empty;
            var options = CreateOptions(graphAsset, blackboardKeyAttribute, currentValue);
            var displayOptions = CreateDisplayOptions(graphAsset, blackboardKeyAttribute, options);
            var selectedIndex = FindOptionIndex(options, currentValue);
            var previousShowMixedValue = EditorGUI.showMixedValue;

            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var popupPosition = string.IsNullOrEmpty(label.text) ? position : EditorGUI.PrefixLabel(position, label);
            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            var nextIndex = EditorGUI.Popup(popupPosition, selectedIndex, displayOptions);
            EditorGUI.indentLevel = indentLevel;
            if (EditorGUI.EndChangeCheck()) {
                property.stringValue = options[nextIndex];
            }

            EditorGUI.showMixedValue = previousShowMixedValue;
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Blackboard key popup の選択肢を作成
        /// </summary>
        /// <param name="graphAsset">参照する GraphAsset</param>
        /// <param name="blackboardKeyAttribute">Blackboard key のフィルタ設定</param>
        /// <param name="currentValue">現在値</param>
        /// <returns>選択肢一覧</returns>
        private static List<string> CreateOptions(AnimationGraphAsset graphAsset, BlackboardKeyAttribute blackboardKeyAttribute, string currentValue) {
            var options = new List<string>();
            AddOption(options, string.Empty);

            var definitions = graphAsset.BlackboardDefinitions;
            for (var i = 0; i < definitions.Count; i++) {
                var definition = definitions[i];
                if (!MatchesValueTypeFilter(blackboardKeyAttribute, definition)) {
                    continue;
                }

                AddOption(options, definition.Key);
            }

            if (!string.IsNullOrEmpty(currentValue)) {
                AddOption(options, currentValue);
            }

            return options;
        }

        /// <summary>
        /// Blackboard key popup の表示名一覧を作成
        /// </summary>
        /// <param name="graphAsset">参照する GraphAsset</param>
        /// <param name="blackboardKeyAttribute">Blackboard key のフィルタ設定</param>
        /// <param name="options">選択肢一覧</param>
        /// <returns>表示名一覧</returns>
        private static string[] CreateDisplayOptions(AnimationGraphAsset graphAsset, BlackboardKeyAttribute blackboardKeyAttribute, IReadOnlyList<string> options) {
            var displayOptions = new string[options.Count];
            for (var i = 0; i < options.Count; i++) {
                var option = options[i];
                if (string.IsNullOrEmpty(option)) {
                    displayOptions[i] = EmptyLabel;
                    continue;
                }

                if (!graphAsset.TryGetBlackboardDefinition(option, out var definition)) {
                    displayOptions[i] = option + MissingSuffix;
                    continue;
                }

                displayOptions[i] = MatchesValueTypeFilter(blackboardKeyAttribute, definition)
                    ? option
                    : $"{option} ({definition.ValueType})";
            }

            return displayOptions;
        }

        /// <summary>
        /// Blackboard 定義が値型フィルタに一致するか判定
        /// </summary>
        /// <param name="blackboardKeyAttribute">Blackboard key のフィルタ設定</param>
        /// <param name="definition">判定対象 Blackboard 定義</param>
        /// <returns>一致する場合は true</returns>
        private static bool MatchesValueTypeFilter(BlackboardKeyAttribute blackboardKeyAttribute, AnimationGraphBlackboardDefinition definition) {
            return !blackboardKeyAttribute.HasValueTypeFilter || definition.ValueType == blackboardKeyAttribute.ValueType;
        }

        /// <summary>
        /// 選択肢一覧から指定値の index を検索
        /// </summary>
        /// <param name="options">選択肢一覧</param>
        /// <param name="value">検索する値</param>
        /// <returns>見つかった index。見つからない場合は 0</returns>
        private static int FindOptionIndex(IReadOnlyList<string> options, string value) {
            for (var i = 0; i < options.Count; i++) {
                if (options[i] == value) {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>
        /// 重複を避けて選択肢を追加
        /// </summary>
        /// <param name="options">追加先の選択肢一覧</param>
        /// <param name="option">追加する選択肢</param>
        private static void AddOption(List<string> options, string option) {
            option = option ?? string.Empty;
            for (var i = 0; i < options.Count; i++) {
                if (options[i] == option) {
                    return;
                }
            }

            options.Add(option);
        }
    }
}
