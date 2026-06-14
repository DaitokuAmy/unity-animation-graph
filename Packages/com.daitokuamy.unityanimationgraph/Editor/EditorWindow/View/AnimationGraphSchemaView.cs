using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Target と Blackboard の定義を表示する View
    /// </summary>
    internal sealed class AnimationGraphSchemaView : VisualElement, IDisposable {
        private const string GraphSeedPropertyName = "_graphSeed";
        private const string RandomSeedPropertyName = "_randomSeed";
        private const string TargetDefinitionsPropertyName = "_targetDefinitions";
        private const string BlackboardDefinitionsPropertyName = "_blackboardDefinitions";
        private const string KeyPropertyName = "_key";
        private const string MonoScriptGuidPropertyName = "_monoScriptGuid";
        private const string ValueTypePropertyName = "_valueType";
        private const string DefaultBoolValuePropertyName = "_defaultBoolValue";
        private const string DefaultIntValuePropertyName = "_defaultIntValue";
        private const string DefaultFloatValuePropertyName = "_defaultFloatValue";
        private const string DefaultStringValuePropertyName = "_defaultStringValue";
        private const string DefaultVector2ValuePropertyName = "_defaultVector2Value";
        private const string DefaultVector3ValuePropertyName = "_defaultVector3Value";
        private const string DefaultColorValuePropertyName = "_defaultColorValue";
        private const string DefaultVector4ValuePropertyName = "_defaultVector4Value";
        private const float ElementTopPadding = 2.0f;
        private const float ElementVerticalSpacing = 2.0f;
        private const float ElementBottomPadding = 6.0f;
        private const float TargetElementSpacing = 4.0f;
        private const float TargetComponentWidth = 170.0f;
        private const float BlackboardValueLabelWidth = 48.0f;
        private const float RandomSeedButtonWidth = 96.0f;

        /// <summary>
        /// Target component 選択用 SearchWindow provider
        /// </summary>
        private sealed class TargetComponentSearchProvider : ScriptableObject, ISearchWindowProvider {
            private Action<string> _selected;
            private IReadOnlyList<AnimationGraphTargetComponentOption> _options;

            /// <summary>
            /// 選択コールバックと表示候補を初期化
            /// </summary>
            /// <param name="selected">選択時に呼び出すコールバック</param>
            public void Initialize(Action<string> selected) {
                _selected = selected;
                _options = AnimationGraphTargetScriptUtility.GetComponentOptions();
            }

            /// <inheritdoc/>
            public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context) {
                var entries = new List<SearchTreeEntry> {
                    new SearchTreeGroupEntry(new GUIContent("Target Component"), 0),
                    CreateEntry("Any Component", string.Empty, 1),
                };

                var groupPaths = new HashSet<string>(StringComparer.Ordinal);
                for (var i = 0; i < _options.Count; i++) {
                    AddOption(entries, groupPaths, _options[i]);
                }

                return entries;
            }

            /// <inheritdoc/>
            public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context) {
                _selected?.Invoke(searchTreeEntry.userData as string ?? string.Empty);
                return true;
            }

            private SearchTreeEntry CreateEntry(string name, string monoScriptGuid, int level) {
                return new SearchTreeEntry(new GUIContent(name)) {
                    level = level,
                    userData = monoScriptGuid,
                };
            }

            private void AddOption(List<SearchTreeEntry> entries, HashSet<string> groupPaths, AnimationGraphTargetComponentOption option) {
                var menuPath = option.MenuPath;
                var segments = menuPath.Split('/');
                var groupPath = string.Empty;
                for (var i = 0; i < segments.Length - 1; i++) {
                    groupPath = string.IsNullOrEmpty(groupPath) ? segments[i] : groupPath + "/" + segments[i];
                    if (!groupPaths.Add(groupPath)) {
                        continue;
                    }

                    entries.Add(new SearchTreeGroupEntry(new GUIContent(segments[i]), i + 1));
                }

                var label = segments.Length == 0 ? menuPath : segments[^1];
                var entry = CreateEntry(label, option.MonoScriptGuid, segments.Length);
                entry.content.tooltip = menuPath;
                entries.Add(entry);
            }
        }

        private readonly IMGUIContainer _container;

        private AnimationGraphAsset _graphAsset;
        private SerializedObject _serializedGraph;
        private ReorderableList _targetDefinitionsList;
        private ReorderableList _blackboardDefinitionsList;
        private TargetComponentSearchProvider _targetComponentSearchProvider;
        private Vector2 _scrollPosition;
        private bool _isReadOnly;

        /// <summary>Schema edit notification</summary>
        public event Action SchemaChanged;

        /// <summary>
        /// AnimationGraphSchemaView を作成
        /// </summary>
        public AnimationGraphSchemaView() {
            focusable = true;
            style.borderTopWidth = 1.0f;
            style.borderTopColor = new Color(0.18f, 0.18f, 0.18f);
            style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);

            _container = new IMGUIContainer(DrawContent) {
                focusable = true,
                style = {
                    flexGrow = 1.0f,
                    paddingLeft = 6.0f,
                    paddingRight = 6.0f,
                    paddingTop = 6.0f,
                    paddingBottom = 6.0f,
                },
            };
            Add(_container);

            _container.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        /// <summary>
        /// 表示対象の AnimationGraphAsset を設定
        /// </summary>
        /// <param name="graphAsset">表示対象の AnimationGraphAsset</param>
        public void SetGraphAsset(AnimationGraphAsset graphAsset) {
            _graphAsset = graphAsset;
            RebuildLists();
            _container.MarkDirtyRepaint();
        }

        /// <summary>
        /// Schema の編集可否を設定
        /// </summary>
        /// <param name="isReadOnly">編集を禁止する場合は true</param>
        public void SetReadOnly(bool isReadOnly) {
            if (_isReadOnly == isReadOnly) {
                return;
            }

            _isReadOnly = isReadOnly;
            _container.MarkDirtyRepaint();
        }

        /// <summary>
        /// 表示内容を更新
        /// </summary>
        public void Refresh() {
            if (_graphAsset == null) {
                ClearSerializedState();
            }
            else if (_serializedGraph == null || _serializedGraph.targetObject != _graphAsset) {
                RebuildLists();
            }

            _container.MarkDirtyRepaint();
        }

        /// <inheritdoc/>
        public void Dispose() {
            _container.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            UnregisterCallback<KeyDownEvent>(OnKeyDown);
            ClearTargetComponentSearchProvider();
            ClearSerializedState();
        }

        private void DrawTargetDefinitionElement(Rect rect, SerializedProperty elementProperty) {
            rect.y += ElementTopPadding;
            rect.height = EditorGUIUtility.singleLineHeight;
            var componentWidth = Mathf.Min(TargetComponentWidth, Mathf.Max(0.0f, (rect.width - TargetElementSpacing) * 0.5f));
            var keyWidth = Mathf.Max(0.0f, rect.width - componentWidth - TargetElementSpacing);
            var keyRect = new Rect(rect.x, rect.y, keyWidth, rect.height);
            var componentRect = new Rect(keyRect.xMax + TargetElementSpacing, rect.y, componentWidth, rect.height);
            EditorGUI.PropertyField(keyRect, elementProperty.FindPropertyRelative(KeyPropertyName), GUIContent.none);
            DrawComponentTypeSelector(componentRect, elementProperty.FindPropertyRelative(MonoScriptGuidPropertyName));
        }

        private static void DrawBlackboardDefinitionElement(Rect rect, SerializedProperty elementProperty) {
            rect.y += ElementTopPadding;
            rect.height = EditorGUIUtility.singleLineHeight;

            const float Spacing = 4.0f;
            var keyWidth = Mathf.Max(0.0f, rect.width - 92.0f - Spacing);
            var keyRect = new Rect(rect.x, rect.y, keyWidth, rect.height);
            var typeRect = new Rect(keyRect.xMax + Spacing, rect.y, 92.0f, rect.height);
            var valueRect = new Rect(rect.x, rect.yMax + ElementVerticalSpacing, rect.width, GetBlackboardDefaultValueHeight(elementProperty));

            EditorGUI.PropertyField(keyRect, elementProperty.FindPropertyRelative(KeyPropertyName), GUIContent.none);
            EditorGUI.PropertyField(typeRect, elementProperty.FindPropertyRelative(ValueTypePropertyName), GUIContent.none);
            DrawBlackboardDefaultValue(valueRect, elementProperty);
        }

        private static float GetBlackboardDefinitionElementHeight(SerializedProperty elementProperty) {
            return ElementTopPadding + EditorGUIUtility.singleLineHeight + ElementVerticalSpacing + GetBlackboardDefaultValueHeight(elementProperty) + ElementBottomPadding;
        }

        private static float GetBlackboardDefaultValueHeight(SerializedProperty elementProperty) {
            var valueTypeProperty = elementProperty.FindPropertyRelative(ValueTypePropertyName);
            var valueType = (BlackboardValueType)valueTypeProperty.enumValueIndex;
            var valuePropertyName = GetBlackboardDefaultValuePropertyName(valueType);
            if (string.IsNullOrEmpty(valuePropertyName)) {
                return EditorGUIUtility.singleLineHeight;
            }

            var propertyHeight = EditorGUI.GetPropertyHeight(elementProperty.FindPropertyRelative(valuePropertyName), new GUIContent("Default"), true);
            var fallbackHeight = valueType is BlackboardValueType.Vector2 or BlackboardValueType.Vector3 or BlackboardValueType.Vector4
                ? EditorGUIUtility.singleLineHeight * 2.0f + ElementVerticalSpacing
                : EditorGUIUtility.singleLineHeight;
            return Mathf.Max(propertyHeight, fallbackHeight);
        }

        private static string GetBlackboardDefaultValuePropertyName(BlackboardValueType valueType) {
            return valueType switch {
                BlackboardValueType.Bool => DefaultBoolValuePropertyName,
                BlackboardValueType.Int => DefaultIntValuePropertyName,
                BlackboardValueType.Float => DefaultFloatValuePropertyName,
                BlackboardValueType.String => DefaultStringValuePropertyName,
                BlackboardValueType.Vector2 => DefaultVector2ValuePropertyName,
                BlackboardValueType.Vector3 => DefaultVector3ValuePropertyName,
                BlackboardValueType.Color => DefaultColorValuePropertyName,
                BlackboardValueType.Vector4 => DefaultVector4ValuePropertyName,
                _ => string.Empty,
            };
        }

        private static void DrawBlackboardDefaultValue(Rect rect, SerializedProperty elementProperty) {
            var valueTypeProperty = elementProperty.FindPropertyRelative(ValueTypePropertyName);
            var valueType = (BlackboardValueType)valueTypeProperty.enumValueIndex;
            var valuePropertyName = GetBlackboardDefaultValuePropertyName(valueType);

            if (string.IsNullOrEmpty(valuePropertyName)) {
                return;
            }

            var previousLabelWidth = EditorGUIUtility.labelWidth;
            try {
                EditorGUIUtility.labelWidth = BlackboardValueLabelWidth;
                EditorGUI.PropertyField(rect, elementProperty.FindPropertyRelative(valuePropertyName), new GUIContent("Default"), true);
            }
            finally {
                EditorGUIUtility.labelWidth = previousLabelWidth;
            }
        }

        private static void ResetTargetDefinitionProperty(SerializedProperty elementProperty) {
            elementProperty.FindPropertyRelative(KeyPropertyName).stringValue = string.Empty;
            elementProperty.FindPropertyRelative(MonoScriptGuidPropertyName).stringValue = string.Empty;
        }

        private void DrawComponentTypeSelector(Rect rect, SerializedProperty monoScriptGuidProperty) {
            var label = new GUIContent(AnimationGraphTargetScriptUtility.GetComponentDisplayName(monoScriptGuidProperty.stringValue));
            if (!EditorGUI.DropdownButton(rect, label, FocusType.Keyboard, EditorStyles.popup)) {
                return;
            }

            ShowComponentTypeMenu(rect, monoScriptGuidProperty.propertyPath);
        }

        private void ShowComponentTypeMenu(Rect rect, string propertyPath) {
            ClearTargetComponentSearchProvider();
            var provider = ScriptableObject.CreateInstance<TargetComponentSearchProvider>();
            provider.hideFlags = HideFlags.HideAndDontSave;
            provider.Initialize(monoScriptGuid => SetTargetDefinitionMonoScriptGuid(propertyPath, monoScriptGuid));
            _targetComponentSearchProvider = provider;
            SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(rect.center)), provider);
        }

        private void SetTargetDefinitionMonoScriptGuid(string propertyPath, string monoScriptGuid) {
            if (_isReadOnly || _serializedGraph == null) {
                return;
            }

            _serializedGraph.Update();
            _serializedGraph.FindProperty(propertyPath).stringValue = monoScriptGuid ?? string.Empty;
            if (_serializedGraph.ApplyModifiedProperties()) {
                EditorUtility.SetDirty(_graphAsset);
                SchemaChanged?.Invoke();
            }

            _container.MarkDirtyRepaint();
        }

        private static void ResetBlackboardDefinitionProperty(SerializedProperty elementProperty) {
            elementProperty.FindPropertyRelative(KeyPropertyName).stringValue = string.Empty;
            elementProperty.FindPropertyRelative(ValueTypePropertyName).enumValueIndex = (int)BlackboardValueType.Bool;
            elementProperty.FindPropertyRelative(DefaultBoolValuePropertyName).boolValue = false;
            elementProperty.FindPropertyRelative(DefaultIntValuePropertyName).intValue = 0;
            elementProperty.FindPropertyRelative(DefaultFloatValuePropertyName).floatValue = 0.0f;
            elementProperty.FindPropertyRelative(DefaultStringValuePropertyName).stringValue = string.Empty;
            elementProperty.FindPropertyRelative(DefaultVector2ValuePropertyName).vector2Value = Vector2.zero;
            elementProperty.FindPropertyRelative(DefaultVector3ValuePropertyName).vector3Value = Vector3.zero;
            elementProperty.FindPropertyRelative(DefaultColorValuePropertyName).colorValue = default;
            elementProperty.FindPropertyRelative(DefaultVector4ValuePropertyName).vector4Value = Vector4.zero;
        }

        private static void DrawSeedField(SerializedProperty randomSeedProperty, SerializedProperty seedProperty) {
            EditorGUILayout.PropertyField(randomSeedProperty, new GUIContent("Random Seed"));

            var randomSeed = randomSeedProperty.boolValue;
            using (new EditorGUI.DisabledScope(randomSeed)) {
                if (randomSeed) {
                    EditorGUILayout.IntField(new GUIContent("Seed"), 0);
                }
                else {
                    EditorGUILayout.PropertyField(seedProperty, new GUIContent("Seed"));
                }
            }

            using (new EditorGUILayout.HorizontalScope()) {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(randomSeed)) {
                    if (GUILayout.Button("Generate", GUILayout.Width(RandomSeedButtonWidth))) {
                        seedProperty.intValue = CreateRandomSeed();
                    }
                }
            }
        }

        private static int CreateRandomSeed() {
            return BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0);
        }

        private void DrawContent() {
            if (_graphAsset == null) {
                EditorGUILayout.HelpBox("Select an AnimationGraphAsset", MessageType.Info);
                return;
            }

            if (_serializedGraph == null || _targetDefinitionsList == null || _blackboardDefinitionsList == null) {
                RebuildLists();
            }

            _serializedGraph.Update();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            using (new EditorGUI.DisabledScope(_isReadOnly)) {
                DrawSeedField(_serializedGraph.FindProperty(RandomSeedPropertyName), _serializedGraph.FindProperty(GraphSeedPropertyName));
                EditorGUILayout.Space(8.0f);
                _targetDefinitionsList.DoLayoutList();
                EditorGUILayout.Space(8.0f);
                _blackboardDefinitionsList.DoLayoutList();
            }

            EditorGUILayout.EndScrollView();

            if (!_isReadOnly && _serializedGraph.ApplyModifiedProperties()) {
                EditorUtility.SetDirty(_graphAsset);
                SchemaChanged?.Invoke();
            }
        }

        private void RebuildLists() {
            ClearSerializedState();
            if (_graphAsset == null) {
                return;
            }

            _serializedGraph = new SerializedObject(_graphAsset);
            _targetDefinitionsList = CreateTargetDefinitionsList(_serializedGraph.FindProperty(TargetDefinitionsPropertyName));
            _blackboardDefinitionsList = CreateBlackboardDefinitionsList(_serializedGraph.FindProperty(BlackboardDefinitionsPropertyName));
        }

        private ReorderableList CreateTargetDefinitionsList(SerializedProperty definitionsProperty) {
            var list = new ReorderableList(_serializedGraph, definitionsProperty, true, true, true, true) {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Target (Key / Component)"),
                elementHeight = EditorGUIUtility.singleLineHeight + 6.0f,
                drawElementCallback = (rect, index, _, _) => DrawTargetDefinitionElement(rect, definitionsProperty.GetArrayElementAtIndex(index)),
                onAddCallback = reorderableList => AddElement(reorderableList, ResetTargetDefinitionProperty),
            };
            return list;
        }

        private ReorderableList CreateBlackboardDefinitionsList(SerializedProperty definitionsProperty) {
            var list = new ReorderableList(_serializedGraph, definitionsProperty, true, true, true, true) {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Blackboard"),
                drawElementCallback = (rect, index, _, _) => DrawBlackboardDefinitionElement(rect, definitionsProperty.GetArrayElementAtIndex(index)),
                elementHeightCallback = index => GetBlackboardDefinitionElementHeight(definitionsProperty.GetArrayElementAtIndex(index)),
                onAddCallback = reorderableList => AddElement(reorderableList, ResetBlackboardDefinitionProperty),
            };
            return list;
        }

        private void AddElement(ReorderableList reorderableList, Action<SerializedProperty> resetElement) {
            if (_isReadOnly) {
                return;
            }

            var property = reorderableList.serializedProperty;
            var index = property.arraySize;
            property.arraySize++;
            resetElement(property.GetArrayElementAtIndex(index));
            property.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(_graphAsset);
            SchemaChanged?.Invoke();
        }

        private void ClearSerializedState() {
            _serializedGraph = null;
            _targetDefinitionsList = null;
            _blackboardDefinitionsList = null;
        }

        private void ClearTargetComponentSearchProvider() {
            if (_targetComponentSearchProvider == null) {
                return;
            }

            UnityEngine.Object.DestroyImmediate(_targetComponentSearchProvider);
            _targetComponentSearchProvider = null;
        }

        private void OnMouseDown(MouseDownEvent evt) {
            _container.Focus();
        }

        private void OnKeyDown(KeyDownEvent evt) {
            evt.StopPropagation();
        }
    }
}
