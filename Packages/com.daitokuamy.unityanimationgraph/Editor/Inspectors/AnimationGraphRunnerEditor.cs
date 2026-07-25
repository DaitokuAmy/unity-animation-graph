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
        private const string GraphAssetPropertyName = "_graphAsset";
        private const string PlayOnEnabledPropertyName = "_playOnEnabled";
        private const string UpdateTypePropertyName = "_updateType";
        private const string TargetSchemaPropertyName = "_targetSchema";
        private const string TargetBindingsPropertyName = "_targetBindings";
        private const string KeyPropertyName = "_key";
        private const string MonoScriptGuidPropertyName = "_monoScriptGuid";
        private const string TargetPropertyName = "_target";
        private const string TargetsPropertyName = "_targets";
        private const string MultiplicityPropertyName = "_multiplicity";
        private const float KeyMinWidth = 90.0f;
        private const float KeyWidthRatio = 0.42f;
        private const float ColumnSpacing = 6.0f;
        private const float ComponentMenuButtonWidth = 22.0f;

        private static readonly GUIContent TargetBindingsLabel = new("Target Bindings");
        private static readonly GUIContent KeyLabel = new("Key");
        private static readonly GUIContent TargetLabel = new("Target");
        private static readonly GUIContent ComponentMenuLabel = new(string.Empty, "Select Component");

        private SerializedProperty _graphAssetProperty;
        private SerializedProperty _playOnEnabledProperty;
        private SerializedProperty _updateTypeProperty;
        private SerializedProperty _targetSchemaProperty;
        private SerializedProperty _targetBindingsProperty;

        private void OnEnable() {
            _graphAssetProperty = serializedObject.FindProperty(GraphAssetPropertyName);
            _playOnEnabledProperty = serializedObject.FindProperty(PlayOnEnabledPropertyName);
            _updateTypeProperty = serializedObject.FindProperty(UpdateTypePropertyName);
            _targetSchemaProperty = serializedObject.FindProperty(TargetSchemaPropertyName);
            _targetBindingsProperty = serializedObject.FindProperty(TargetBindingsPropertyName);
        }

        /// <inheritdoc/>
        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_graphAssetProperty);
            EditorGUILayout.PropertyField(_targetSchemaProperty);
            EditorGUILayout.PropertyField(_playOnEnabledProperty);
            EditorGUILayout.PropertyField(_updateTypeProperty);

            EnsureTargetBindings();
            DrawTargetSchemaCompatibilityMessage();
            EditorGUILayout.Space();
            DrawTargetBindings();

            serializedObject.ApplyModifiedProperties();
        }

        private static void SetBindingProperties(SerializedProperty bindingsProperty, AnimationGraphTargetSchema targetSchema) {
            var previousTargetsByKey = CreatePreviousTargetsByKey(bindingsProperty);
            var previousTargetCollectionsByKey = CreatePreviousTargetCollectionsByKey(bindingsProperty);
            var definitions = targetSchema?.Definitions;
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
                var targetComponent = previousTargetsByKey.TryGetValue(key, out var previousTarget)
                    && AnimationGraphTargetScriptUtility.IsTargetAssignable(previousTarget, monoScriptGuid)
                    ? previousTarget
                    : null;
                bindingProperty.FindPropertyRelative(TargetPropertyName).objectReferenceValue = targetComponent;
                var targetsProperty = bindingProperty.FindPropertyRelative(TargetsPropertyName);
                var previousTargets = previousTargetCollectionsByKey.TryGetValue(key, out var collection) ? collection : System.Array.Empty<Object>();
                targetsProperty.arraySize = previousTargets.Count;
                for (var targetIndex = 0; targetIndex < previousTargets.Count; targetIndex++) {
                    var previousCollectionTarget = previousTargets[targetIndex];
                    targetsProperty.GetArrayElementAtIndex(targetIndex).objectReferenceValue =
                        AnimationGraphTargetScriptUtility.IsTargetAssignable(previousCollectionTarget, monoScriptGuid)
                            ? previousCollectionTarget
                            : null;
                }
            }
        }

        private static Dictionary<string, Object> CreatePreviousTargetsByKey(SerializedProperty bindingsProperty) {
            var targetsByKey = new Dictionary<string, Object>();
            for (var i = 0; i < bindingsProperty.arraySize; i++) {
                var bindingProperty = bindingsProperty.GetArrayElementAtIndex(i);
                var key = bindingProperty.FindPropertyRelative(KeyPropertyName).stringValue;
                if (!string.IsNullOrEmpty(key)) {
                    targetsByKey[key] = bindingProperty.FindPropertyRelative(TargetPropertyName).objectReferenceValue;
                }
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

        private static bool IsBindingStructureValid(SerializedProperty bindingsProperty, AnimationGraphTargetSchema targetSchema) {
            var definitions = targetSchema?.Definitions;
            var definitionCount = definitions?.Count ?? 0;
            if (bindingsProperty.arraySize != definitionCount) {
                return false;
            }

            for (var i = 0; i < definitionCount; i++) {
                var definition = definitions[i];
                var bindingProperty = bindingsProperty.GetArrayElementAtIndex(i);
                if (bindingProperty.FindPropertyRelative(KeyPropertyName).stringValue != definition.Key
                    || bindingProperty.FindPropertyRelative(MonoScriptGuidPropertyName).stringValue != definition.MonoScriptGuid
                    || bindingProperty.FindPropertyRelative(MultiplicityPropertyName).enumValueIndex != (int)definition.Multiplicity) {
                    return false;
                }
            }

            return true;
        }

        private void EnsureTargetBindings() {
            var targetSchema = _targetSchemaProperty.objectReferenceValue as AnimationGraphTargetSchema;
            if (!IsBindingStructureValid(_targetBindingsProperty, targetSchema)) {
                SetBindingProperties(_targetBindingsProperty, targetSchema);
            }
        }

        private void DrawTargetSchemaCompatibilityMessage() {
            var graphAsset = _graphAssetProperty.objectReferenceValue as AnimationGraphAsset;
            var targetSchema = _targetSchemaProperty.objectReferenceValue as AnimationGraphTargetSchema;
            if (graphAsset != null && graphAsset.TargetSchema != targetSchema) {
                EditorGUILayout.HelpBox("Graph Asset and Runner must use the same Target Schema.", MessageType.Error);
            }
        }

        private void DrawTargetBindings() {
            EditorGUILayout.LabelField(TargetBindingsLabel, EditorStyles.boldLabel);
            if (_targetBindingsProperty.arraySize == 0) {
                EditorGUILayout.HelpBox("Target Schema has no target definitions.", MessageType.Info);
                return;
            }

            DrawBindingHeader();
            for (var i = 0; i < _targetBindingsProperty.arraySize; i++) {
                DrawBinding(_targetBindingsProperty.GetArrayElementAtIndex(i));
            }
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
                drawElementCallback = (rect, index, _, _) => DrawCollectionElement(rect, targetsProperty, index, monoScriptGuid, targetType),
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
                if (component == null || !CanUseComponent(component, targetType)) {
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
