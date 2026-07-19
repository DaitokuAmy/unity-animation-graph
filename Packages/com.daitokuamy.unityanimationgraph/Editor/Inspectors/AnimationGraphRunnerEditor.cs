using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// AnimationGraphRunner の Inspector
    /// </summary>
    [CustomEditor(typeof(AnimationGraphRunner))]
    internal sealed class AnimationGraphRunnerEditor : UnityEditor.Editor {
        /// <summary>AnimationGraphRunner の graph asset フィールド名</summary>
        private const string GraphAssetPropertyName = "_graphAsset";
        /// <summary>AnimationGraphRunner の play on enabled フィールド名</summary>
        private const string PlayOnEnabledPropertyName = "_playOnEnabled";
        /// <summary>AnimationGraphRunner の update type フィールド名</summary>
        private const string UpdateTypePropertyName = "_updateType";
        /// <summary>AnimationGraphRunner の target binding group 配列フィールド名</summary>
        private const string TargetBindingGroupsPropertyName = "_targetBindingGroups";
        /// <summary>TargetBindingGroup の graph asset GUID フィールド名</summary>
        private const string GraphAssetGuidPropertyName = "_graphAssetGuid";
        /// <summary>TargetBindingGroup の binding 配列フィールド名</summary>
        private const string BindingsPropertyName = "_bindings";
        /// <summary>TargetBinding の key フィールド名</summary>
        private const string KeyPropertyName = "_key";
        /// <summary>TargetBinding の MonoScript GUID フィールド名</summary>
        private const string MonoScriptGuidPropertyName = "_monoScriptGuid";
        /// <summary>TargetBinding の target フィールド名</summary>
        private const string TargetPropertyName = "_target";
        /// <summary>TargetBinding の target collection フィールド名</summary>
        private const string TargetsPropertyName = "_targets";
        /// <summary>TargetBinding の multiplicity フィールド名</summary>
        private const string MultiplicityPropertyName = "_multiplicity";
        /// <summary>AnimationGraphAsset の asset GUID フィールド名</summary>
        private const string AssetGuidPropertyName = "_assetGuid";
        /// <summary>binding 行の key 領域の最小幅</summary>
        private const float KeyMinWidth = 90.0f;
        /// <summary>binding 行の key 領域の幅比率</summary>
        private const float KeyWidthRatio = 0.42f;
        /// <summary>binding 行の列間隔</summary>
        private const float ColumnSpacing = 6.0f;
        /// <summary>target component 選択ボタンの幅</summary>
        private const float ComponentMenuButtonWidth = 22.0f;
        /// <summary>target binding group 削除ボタンの幅</summary>
        private const float DeleteButtonWidth = 22.0f;

        private static readonly GUIContent TargetBindingGroupsLabel = new("Target Binding Groups");
        private static readonly GUIContent GraphAssetLabel = new("Graph Asset");
        private static readonly GUIContent KeyLabel = new("Key");
        private static readonly GUIContent TargetLabel = new("Target");
        private static readonly GUIContent ComponentMenuLabel = new(string.Empty, "Select Component");
        private static readonly GUIContent DeleteLabel = new("x", "Remove Target Binding Group");
        private static readonly GUIContent ShowOtherTargetBindingGroupsLabel = new("Show Other Target Binding Groups");

        private SerializedProperty _graphAssetProperty;
        private SerializedProperty _playOnEnabledProperty;
        private SerializedProperty _updateTypeProperty;
        private SerializedProperty _targetBindingGroupsProperty;
        private bool _showOtherTargetBindingGroups = true;

        private void OnEnable() {
            _graphAssetProperty = serializedObject.FindProperty(GraphAssetPropertyName);
            _playOnEnabledProperty = serializedObject.FindProperty(PlayOnEnabledPropertyName);
            _updateTypeProperty = serializedObject.FindProperty(UpdateTypePropertyName);
            _targetBindingGroupsProperty = serializedObject.FindProperty(TargetBindingGroupsPropertyName);
        }

        /// <inheritdoc/>
        public override void OnInspectorGUI() {
            serializedObject.Update();

            DrawGraphAssetProperty();
            EditorGUILayout.PropertyField(_playOnEnabledProperty);
            EditorGUILayout.PropertyField(_updateTypeProperty);

            EnsureCurrentGraphAssetBindingGroup();
            EditorGUILayout.Space();
            DrawTargetBindingGroups();

            serializedObject.ApplyModifiedProperties();
        }

        private static AnimationGraphAsset GetGraphAsset(SerializedProperty groupProperty) {
            var graphAssetGuid = groupProperty.FindPropertyRelative(GraphAssetGuidPropertyName).stringValue;
            if (string.IsNullOrEmpty(graphAssetGuid)) {
                return null;
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(graphAssetGuid);
            return string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<AnimationGraphAsset>(assetPath);
        }

        private static string EnsureGraphAssetGuid(AnimationGraphAsset graphAsset) {
            if (graphAsset == null) {
                return string.Empty;
            }

            var assetPath = AssetDatabase.GetAssetPath(graphAsset);
            if (string.IsNullOrEmpty(assetPath)) {
                return graphAsset.AssetGuid;
            }

            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            if (graphAsset.AssetGuid == assetGuid) {
                return assetGuid;
            }

            var serializedGraph = new SerializedObject(graphAsset);
            serializedGraph.FindProperty(AssetGuidPropertyName).stringValue = assetGuid;
            serializedGraph.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(graphAsset);
            return assetGuid;
        }

        private static void SetGroupGraphAsset(SerializedProperty groupProperty, AnimationGraphAsset graphAsset) {
            var graphAssetGuid = EnsureGraphAssetGuid(graphAsset);
            groupProperty.FindPropertyRelative(GraphAssetGuidPropertyName).stringValue = graphAssetGuid;
            SetBindingProperties(groupProperty.FindPropertyRelative(BindingsPropertyName), graphAsset);
        }

        private static void SetBindingProperties(SerializedProperty bindingsProperty, AnimationGraphAsset graphAsset) {
            var previousTargetsByKey = CreatePreviousTargetsByKey(bindingsProperty);
            var previousTargetCollectionsByKey = CreatePreviousTargetCollectionsByKey(bindingsProperty);
            var definitions = graphAsset?.TargetDefinitions;
            var definitionCount = definitions?.Count ?? 0;
            bindingsProperty.arraySize = definitionCount;

            for (var i = 0; i < definitionCount; i++) {
                var definition = definitions[i];
                var key = definition.Key;
                var monoScriptGuid = definition.MonoScriptGuid;
                var bindingProperty = bindingsProperty.GetArrayElementAtIndex(i);
                bindingProperty.FindPropertyRelative(KeyPropertyName).stringValue = key;
                bindingProperty.FindPropertyRelative(MonoScriptGuidPropertyName).stringValue = monoScriptGuid;
                bindingProperty.FindPropertyRelative(MultiplicityPropertyName).enumValueIndex = (int)definition.Multiplicity;
                var targetComponent = previousTargetsByKey.TryGetValue(key, out var previousTarget) && AnimationGraphTargetScriptUtility.IsTargetAssignable(previousTarget, monoScriptGuid)
                    ? previousTarget
                    : null;
                bindingProperty.FindPropertyRelative(TargetPropertyName).objectReferenceValue = targetComponent;
                var targetsProperty = bindingProperty.FindPropertyRelative(TargetsPropertyName);
                var previousTargets = previousTargetCollectionsByKey.TryGetValue(key, out var collection) ? collection : System.Array.Empty<Object>();
                targetsProperty.arraySize = previousTargets.Count;
                for (var targetIndex = 0; targetIndex < previousTargets.Count; targetIndex++) {
                    targetsProperty.GetArrayElementAtIndex(targetIndex).objectReferenceValue = previousTargets[targetIndex];
                }
            }
        }

        private static Dictionary<string, Object> CreatePreviousTargetsByKey(SerializedProperty bindingsProperty) {
            var targetsByKey = new Dictionary<string, Object>();
            for (var i = 0; i < bindingsProperty.arraySize; i++) {
                var bindingProperty = bindingsProperty.GetArrayElementAtIndex(i);
                var key = bindingProperty.FindPropertyRelative(KeyPropertyName).stringValue;
                if (string.IsNullOrEmpty(key)) {
                    continue;
                }

                targetsByKey[key] = bindingProperty.FindPropertyRelative(TargetPropertyName).objectReferenceValue;
            }

            return targetsByKey;
        }

        private static Dictionary<string, IReadOnlyList<Object>> CreatePreviousTargetCollectionsByKey(SerializedProperty bindingsProperty) {
            var targetsByKey = new Dictionary<string, IReadOnlyList<Object>>();
            for (var i = 0; i < bindingsProperty.arraySize; i++) {
                var bindingProperty = bindingsProperty.GetArrayElementAtIndex(i);
                var key = bindingProperty.FindPropertyRelative(KeyPropertyName).stringValue;
                if (string.IsNullOrEmpty(key)) {
                    continue;
                }

                var targetsProperty = bindingProperty.FindPropertyRelative(TargetsPropertyName);
                var targets = new Object[targetsProperty.arraySize];
                for (var targetIndex = 0; targetIndex < targets.Length; targetIndex++) {
                    targets[targetIndex] = targetsProperty.GetArrayElementAtIndex(targetIndex).objectReferenceValue;
                }

                targetsByKey[key] = targets;
            }

            return targetsByKey;
        }

        private static bool IsBindingStructureValid(SerializedProperty bindingsProperty, AnimationGraphAsset graphAsset) {
            var definitions = graphAsset?.TargetDefinitions;
            var definitionCount = definitions?.Count ?? 0;
            if (bindingsProperty.arraySize != definitionCount) {
                return false;
            }

            for (var i = 0; i < definitionCount; i++) {
                var definition = definitions[i];
                var bindingProperty = bindingsProperty.GetArrayElementAtIndex(i);
                if (bindingProperty.FindPropertyRelative(KeyPropertyName).stringValue != definition.Key) {
                    return false;
                }

                if (bindingProperty.FindPropertyRelative(MonoScriptGuidPropertyName).stringValue != definition.MonoScriptGuid) {
                    return false;
                }

                if (bindingProperty.FindPropertyRelative(MultiplicityPropertyName).enumValueIndex != (int)definition.Multiplicity) {
                    return false;
                }
            }

            return true;
        }

        private void DrawGraphAssetProperty() {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_graphAssetProperty);
            if (!EditorGUI.EndChangeCheck()) {
                return;
            }

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }

        private void EnsureCurrentGraphAssetBindingGroup() {
            var graphAsset = _graphAssetProperty.objectReferenceValue as AnimationGraphAsset;
            if (graphAsset == null) {
                return;
            }

            var graphAssetGuid = EnsureGraphAssetGuid(graphAsset);
            if (string.IsNullOrEmpty(graphAssetGuid)) {
                return;
            }

            var groupIndex = FindTargetBindingGroupIndex(graphAssetGuid);
            var groupCreated = false;
            if (groupIndex < 0) {
                groupIndex = _targetBindingGroupsProperty.arraySize;
                _targetBindingGroupsProperty.arraySize++;
                groupCreated = true;
            }

            var groupProperty = _targetBindingGroupsProperty.GetArrayElementAtIndex(groupIndex);
            if (groupCreated) {
                groupProperty.FindPropertyRelative(BindingsPropertyName).arraySize = 0;
                SetGroupGraphAsset(groupProperty, graphAsset);
                return;
            }

            if (groupProperty.FindPropertyRelative(GraphAssetGuidPropertyName).stringValue != graphAssetGuid) {
                groupProperty.FindPropertyRelative(GraphAssetGuidPropertyName).stringValue = graphAssetGuid;
            }

            var bindingsProperty = groupProperty.FindPropertyRelative(BindingsPropertyName);
            if (!IsBindingStructureValid(bindingsProperty, graphAsset)) {
                SetBindingProperties(bindingsProperty, graphAsset);
            }
        }

        private int FindTargetBindingGroupIndex(string graphAssetGuid) {
            for (var i = 0; i < _targetBindingGroupsProperty.arraySize; i++) {
                var groupProperty = _targetBindingGroupsProperty.GetArrayElementAtIndex(i);
                if (groupProperty.FindPropertyRelative(GraphAssetGuidPropertyName).stringValue == graphAssetGuid) {
                    return i;
                }
            }

            return -1;
        }

        private void DrawTargetBindingGroups() {
            EditorGUILayout.LabelField(TargetBindingGroupsLabel, EditorStyles.boldLabel);
            var currentGroupIndex = FindCurrentTargetBindingGroupIndex();
            if (currentGroupIndex >= 0) {
                var currentGroupProperty = _targetBindingGroupsProperty.GetArrayElementAtIndex(currentGroupIndex);
                DrawTargetBindingGroup(currentGroupProperty, false);
                if (HasOtherTargetBindingGroups(currentGroupIndex)) {
                    _showOtherTargetBindingGroups = EditorGUILayout.ToggleLeft(ShowOtherTargetBindingGroupsLabel, _showOtherTargetBindingGroups);
                    if (!_showOtherTargetBindingGroups) {
                        return;
                    }
                }
            }

            for (var i = 0; i < _targetBindingGroupsProperty.arraySize; i++) {
                if (i == currentGroupIndex) {
                    continue;
                }

                var groupProperty = _targetBindingGroupsProperty.GetArrayElementAtIndex(i);
                if (!DrawTargetBindingGroup(groupProperty, true)) {
                    continue;
                }

                _targetBindingGroupsProperty.DeleteArrayElementAtIndex(i);
                if (i < currentGroupIndex) {
                    currentGroupIndex--;
                }

                i--;
            }
        }

        private int FindCurrentTargetBindingGroupIndex() {
            var graphAsset = _graphAssetProperty.objectReferenceValue as AnimationGraphAsset;
            if (graphAsset == null) {
                return -1;
            }

            var graphAssetGuid = EnsureGraphAssetGuid(graphAsset);
            return string.IsNullOrEmpty(graphAssetGuid) ? -1 : FindTargetBindingGroupIndex(graphAssetGuid);
        }

        private bool HasOtherTargetBindingGroups(int currentGroupIndex) {
            for (var i = 0; i < _targetBindingGroupsProperty.arraySize; i++) {
                if (i != currentGroupIndex) {
                    return true;
                }
            }

            return false;
        }

        private bool DrawTargetBindingGroup(SerializedProperty groupProperty, bool canDelete) {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                var graphAsset = GetGraphAsset(groupProperty);
                if (DrawTargetBindingGroupHeader(graphAsset, canDelete)) {
                    return true;
                }

                var bindingsProperty = groupProperty.FindPropertyRelative(BindingsPropertyName);
                if (graphAsset != null && !IsBindingStructureValid(bindingsProperty, graphAsset)) {
                    SetBindingProperties(bindingsProperty, graphAsset);
                }

                DrawBindingHeader();
                for (var i = 0; i < bindingsProperty.arraySize; i++) {
                    DrawBinding(bindingsProperty.GetArrayElementAtIndex(i));
                }
            }

            return false;
        }

        private bool DrawTargetBindingGroupHeader(AnimationGraphAsset graphAsset, bool canDelete) {
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            var graphAssetRect = rect;
            var deleteRect = Rect.zero;
            if (canDelete) {
                deleteRect = new Rect(rect.xMax - DeleteButtonWidth, rect.y, DeleteButtonWidth, rect.height);
                var graphAssetWidth = Mathf.Max(0.0f, deleteRect.xMin - rect.x - ColumnSpacing);
                graphAssetRect = new Rect(rect.x, rect.y, graphAssetWidth, rect.height);
            }

            using (new EditorGUI.DisabledScope(true)) {
                EditorGUI.ObjectField(graphAssetRect, GraphAssetLabel, graphAsset, typeof(AnimationGraphAsset), false);
            }

            return canDelete && GUI.Button(deleteRect, DeleteLabel, EditorStyles.miniButton);
        }

        private void DrawBindingHeader() {
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            var keyRect = GetKeyRect(rect);
            var targetRect = GetTargetRect(rect, keyRect);
            EditorGUI.LabelField(keyRect, KeyLabel, EditorStyles.miniBoldLabel);
            EditorGUI.LabelField(targetRect, TargetLabel, EditorStyles.miniBoldLabel);
        }

        private void DrawBinding(SerializedProperty bindingProperty) {
            var multiplicity = (TargetMultiplicity)bindingProperty.FindPropertyRelative(MultiplicityPropertyName).enumValueIndex;
            if (multiplicity == TargetMultiplicity.Collection) {
                DrawCollectionBinding(bindingProperty);
                return;
            }

            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            var keyRect = GetKeyRect(rect);
            var targetRect = GetTargetRect(rect, keyRect);
            var componentMenuRect = GetComponentMenuRect(targetRect);
            var targetFieldRect = GetTargetFieldRect(targetRect, componentMenuRect);
            var keyProperty = bindingProperty.FindPropertyRelative(KeyPropertyName);
            var monoScriptGuidProperty = bindingProperty.FindPropertyRelative(MonoScriptGuidPropertyName);
            var targetComponentProperty = bindingProperty.FindPropertyRelative(TargetPropertyName);
            var targetType = AnimationGraphTargetScriptUtility.GetObjectFieldType(monoScriptGuidProperty.stringValue);

            using (new EditorGUI.DisabledScope(true)) {
                EditorGUI.TextField(keyRect, keyProperty.stringValue);
            }

            if (!AnimationGraphTargetScriptUtility.IsTargetAssignable(targetComponentProperty.objectReferenceValue, monoScriptGuidProperty.stringValue)) {
                targetComponentProperty.objectReferenceValue = null;
            }

            var targetObject = EditorGUI.ObjectField(targetFieldRect, targetComponentProperty.objectReferenceValue, targetType, true);
            targetComponentProperty.objectReferenceValue = AnimationGraphTargetScriptUtility.IsTargetAssignable(targetObject, monoScriptGuidProperty.stringValue) ? targetObject : null;
            DrawComponentMenuButton(componentMenuRect, targetComponentProperty, targetComponentProperty.objectReferenceValue as Component, targetType);
        }

        private void DrawCollectionBinding(SerializedProperty bindingProperty) {
            var key = bindingProperty.FindPropertyRelative(KeyPropertyName).stringValue;
            var monoScriptGuid = bindingProperty.FindPropertyRelative(MonoScriptGuidPropertyName).stringValue;
            var targetType = AnimationGraphTargetScriptUtility.GetObjectFieldType(monoScriptGuid);
            var targetsProperty = bindingProperty.FindPropertyRelative(TargetsPropertyName);
            var label = new GUIContent($"{key} ({ObjectNames.NicifyVariableName(targetType.Name)}[])");
            var targetsList = new ReorderableList(serializedObject, targetsProperty, true, true, true, true) {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, label),
                drawElementCallback = (rect, index, isActive, isFocused) => DrawCollectionElement(rect, targetsProperty, index, monoScriptGuid, targetType),
                elementHeight = EditorGUIUtility.singleLineHeight,
            };
            targetsList.DoLayoutList();
        }

        private static void DrawCollectionElement(Rect rect, SerializedProperty targetsProperty, int index, string monoScriptGuid, System.Type targetType) {
            var elementProperty = targetsProperty.GetArrayElementAtIndex(index);
            var currentTarget = AnimationGraphTargetScriptUtility.IsTargetAssignable(elementProperty.objectReferenceValue, monoScriptGuid)
                ? elementProperty.objectReferenceValue
                : null;
            var target = EditorGUI.ObjectField(rect, $"Element {index}", currentTarget, targetType, true);
            elementProperty.objectReferenceValue = AnimationGraphTargetScriptUtility.IsTargetAssignable(target, monoScriptGuid)
                ? target
                : null;
        }

        private void DrawComponentMenuButton(Rect rect, SerializedProperty targetComponentProperty, Component targetComponent, System.Type targetType) {
            var canSelectComponent = targetComponent != null && targetType == typeof(Component);
            using (new EditorGUI.DisabledScope(!canSelectComponent)) {
                if (!EditorGUI.DropdownButton(rect, ComponentMenuLabel, FocusType.Keyboard, EditorStyles.popup)) {
                    return;
                }
            }

            ShowComponentMenu(rect, targetComponentProperty, targetComponent, targetType);
        }

        private void ShowComponentMenu(Rect rect, SerializedProperty targetComponentProperty, Component targetComponent, System.Type targetType) {
            var menu = new GenericMenu();
            if (targetComponent == null) {
                menu.AddDisabledItem(new GUIContent("No Component"));
                menu.DropDown(rect);
                return;
            }

            var propertyPath = targetComponentProperty.propertyPath;
            var components = targetComponent.GetComponents<Component>();
            var hasItem = false;
            for (var i = 0; i < components.Length; i++) {
                var component = components[i];
                if (component == null) {
                    continue;
                }

                if (!CanUseComponent(component, targetType)) {
                    continue;
                }

                var label = CreateComponentLabel(components, i);
                menu.AddItem(new GUIContent(label), component == targetComponent, () => SetTargetComponent(propertyPath, component));
                hasItem = true;
            }

            if (!hasItem) {
                menu.AddDisabledItem(new GUIContent("No Matching Component"));
            }

            menu.DropDown(rect);
        }

        private static string CreateComponentLabel(IReadOnlyList<Component> components, int componentIndex) {
            var component = components[componentIndex];
            var componentTypeName = component.GetType().Name;
            var sameTypeCount = 0;
            var sameTypeIndex = 0;
            for (var i = 0; i < components.Count; i++) {
                var current = components[i];
                if (current == null || current.GetType() != component.GetType()) {
                    continue;
                }

                sameTypeCount++;
                if (i <= componentIndex) {
                    sameTypeIndex++;
                }
            }

            return sameTypeCount <= 1 ? componentTypeName : $"{componentTypeName} #{sameTypeIndex}";
        }

        private static bool CanUseComponent(Component component, System.Type targetType) {
            return component != null && (targetType == typeof(Component) || targetType.IsInstanceOfType(component));
        }

        private void SetTargetComponent(string propertyPath, Component component) {
            serializedObject.Update();
            serializedObject.FindProperty(propertyPath).objectReferenceValue = component;
            serializedObject.ApplyModifiedProperties();
        }

        private static Rect GetKeyRect(Rect rect) {
            var keyWidth = Mathf.Max(KeyMinWidth, rect.width * KeyWidthRatio);
            return new Rect(rect.x, rect.y, keyWidth, rect.height);
        }

        private static Rect GetTargetRect(Rect rect, Rect keyRect) {
            var x = keyRect.xMax + ColumnSpacing;
            return new Rect(x, rect.y, rect.xMax - x, rect.height);
        }

        private static Rect GetComponentMenuRect(Rect targetRect) {
            return new Rect(targetRect.xMax - ComponentMenuButtonWidth, targetRect.y, ComponentMenuButtonWidth, targetRect.height);
        }

        private static Rect GetTargetFieldRect(Rect targetRect, Rect componentMenuRect) {
            var targetFieldWidth = Mathf.Max(0.0f, componentMenuRect.xMin - targetRect.x - ColumnSpacing);
            return new Rect(targetRect.x, targetRect.y, targetFieldWidth, targetRect.height);
        }
    }
}
