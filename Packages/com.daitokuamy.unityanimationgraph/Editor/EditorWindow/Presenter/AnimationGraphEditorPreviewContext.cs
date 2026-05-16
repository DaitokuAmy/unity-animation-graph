using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// EditorWindow preview 用の一時的な評価コンテキスト
    /// </summary>
    internal sealed class AnimationGraphEditorPreviewContext : IAnimationGraphContext {
        private readonly Dictionary<string, AnimationGraphBlackboardValue> _blackboardValuesByKey = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Component> _targetsByKey = new(StringComparer.Ordinal);
        private readonly HashSet<string> _typedTargetKeys = new(StringComparer.Ordinal);
        private readonly List<string> _missingTargetMessages = new();
        private readonly GameObject _rootGameObject;

        /// <summary>Preview root として使う GameObject</summary>
        public GameObject RootGameObject => _rootGameObject;
        /// <summary>自動解決できなかった target 一覧</summary>
        public IReadOnlyList<string> MissingTargetMessages => _missingTargetMessages;

        /// <summary>
        /// AnimationGraphEditorPreviewContext を生成
        /// </summary>
        /// <param name="graphAsset">Preview 対象の AnimationGraphAsset</param>
        /// <param name="rootGameObject">Selection.activeGameObject から取得した preview root</param>
        /// <param name="targetBindings">Runner に設定されている target binding 一覧</param>
        public AnimationGraphEditorPreviewContext(AnimationGraphAsset graphAsset, GameObject rootGameObject, IReadOnlyList<AnimationGraphTargetBinding> targetBindings = null) {
            if (graphAsset == null) {
                throw new ArgumentNullException(nameof(graphAsset));
            }

            _rootGameObject = rootGameObject != null ? rootGameObject : throw new ArgumentNullException(nameof(rootGameObject));
            Refresh(graphAsset, targetBindings);
        }

        /// <summary>
        /// GraphAsset の定義と Runner binding から Preview 用の解決情報を再構築
        /// </summary>
        /// <param name="graphAsset">Preview 対象の AnimationGraphAsset</param>
        /// <param name="targetBindings">Runner に設定されている target binding 一覧</param>
        public void Refresh(AnimationGraphAsset graphAsset, IReadOnlyList<AnimationGraphTargetBinding> targetBindings = null) {
            if (graphAsset == null) {
                throw new ArgumentNullException(nameof(graphAsset));
            }

            _blackboardValuesByKey.Clear();
            _targetsByKey.Clear();
            _typedTargetKeys.Clear();
            _missingTargetMessages.Clear();
            BuildBlackboardValues(graphAsset.BlackboardDefinitions);
            BindRunnerTargets(targetBindings);
            BindTargets(graphAsset.TargetDefinitions);
        }

        /// <inheritdoc/>
        public T GetTarget<T>(string key) where T : Component {
            if (TryGetTarget<T>(key, out var target)) {
                return target;
            }

            throw new InvalidOperationException($"Preview target '{key}' is not found on '{_rootGameObject.name}'");
        }

        /// <inheritdoc/>
        public bool TryGetTarget<T>(string key, out T target) where T : Component {
            if (!string.IsNullOrEmpty(key) && _targetsByKey.TryGetValue(key, out var boundTarget)) {
                if (boundTarget is T typedBoundTarget) {
                    target = typedBoundTarget;
                    return true;
                }

                target = null;
                return false;
            }

            if (!string.IsNullOrEmpty(key) && _typedTargetKeys.Contains(key)) {
                target = null;
                return false;
            }

            return TryFindTarget(out target);
        }

        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out bool value) {
            if (TryGetBlackboardEntry(key, out var blackboardValue) && blackboardValue.TryGetValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out int value) {
            if (TryGetBlackboardEntry(key, out var blackboardValue) && blackboardValue.TryGetValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out float value) {
            if (TryGetBlackboardEntry(key, out var blackboardValue) && blackboardValue.TryGetValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out string value) {
            if (TryGetBlackboardEntry(key, out var blackboardValue) && blackboardValue.TryGetValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out Vector2 value) {
            if (TryGetBlackboardEntry(key, out var blackboardValue) && blackboardValue.TryGetValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out Vector3 value) {
            if (TryGetBlackboardEntry(key, out var blackboardValue) && blackboardValue.TryGetValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out Color value) {
            if (TryGetBlackboardEntry(key, out var blackboardValue) && blackboardValue.TryGetValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Blackboard 定義から Preview 用の初期値を構築
        /// </summary>
        /// <param name="definitions">Blackboard 定義一覧</param>
        private void BuildBlackboardValues(IReadOnlyList<AnimationGraphBlackboardDefinition> definitions) {
            for (var i = 0; i < definitions.Count; i++) {
                var value = new AnimationGraphBlackboardValue(definitions[i]);
                if (string.IsNullOrEmpty(value.Key)) {
                    continue;
                }

                _blackboardValuesByKey[value.Key] = value;
            }
        }

        /// <summary>
        /// GraphAsset の target 定義を Preview root 上の Component に解決
        /// </summary>
        /// <param name="definitions">Target 定義一覧</param>
        private void BindTargets(IReadOnlyList<AnimationGraphTargetDefinition> definitions) {
            for (var i = 0; i < definitions.Count; i++) {
                var definition = definitions[i];
                var key = definition.Key;
                if (string.IsNullOrEmpty(key)) {
                    continue;
                }

                var componentType = AnimationGraphTargetScriptUtility.GetTargetComponentType(definition.MonoScriptGuid);
                if (componentType == null) {
                    continue;
                }

                _typedTargetKeys.Add(key);
                if (_targetsByKey.TryGetValue(key, out var boundTarget)) {
                    if (boundTarget != null && componentType.IsInstanceOfType(boundTarget)) {
                        continue;
                    }

                    _targetsByKey.Remove(key);
                }

                var target = FindComponent(componentType);
                if (target != null) {
                    SetTarget(key, target);
                    continue;
                }

                _missingTargetMessages.Add($"{key} ({ObjectNames.NicifyVariableName(componentType.Name)})");
            }
        }

        /// <summary>
        /// Runner に設定された target binding を Preview target として登録
        /// </summary>
        /// <param name="targetBindings">Runner の target binding 一覧</param>
        private void BindRunnerTargets(IReadOnlyList<AnimationGraphTargetBinding> targetBindings) {
            if (targetBindings == null) {
                return;
            }

            for (var i = 0; i < targetBindings.Count; i++) {
                BindRunnerTarget(targetBindings[i]);
            }
        }

        /// <summary>
        /// Runner の target binding を 1 件登録
        /// </summary>
        /// <param name="binding">登録する target binding</param>
        private void BindRunnerTarget(AnimationGraphTargetBinding binding) {
            var key = binding.Key;
            if (string.IsNullOrEmpty(key)) {
                return;
            }

            var target = binding.Target;
            if (target == null) {
                return;
            }

            if (!AnimationGraphTargetScriptUtility.IsTargetAssignable(target, binding.MonoScriptGuid)) {
                return;
            }

            SetTarget(key, target);
        }

        /// <summary>
        /// 指定 key の Blackboard 値を取得
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="value">取得した Blackboard 値</param>
        /// <returns>値が見つかった場合は true</returns>
        private bool TryGetBlackboardEntry(string key, out AnimationGraphBlackboardValue value) {
            if (!string.IsNullOrEmpty(key) && _blackboardValuesByKey.TryGetValue(key, out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Preview root から指定型の Component を探索
        /// </summary>
        /// <param name="componentType">探索する Component 型</param>
        /// <returns>見つかった Component。見つからない場合は null</returns>
        private Component FindComponent(Type componentType) {
            if (componentType == typeof(Transform)) {
                return _rootGameObject.transform;
            }

            var rootComponent = _rootGameObject.GetComponent(componentType);
            if (rootComponent != null) {
                return rootComponent;
            }

            return _rootGameObject.GetComponentInChildren(componentType, true);
        }

        /// <summary>
        /// Preview root から指定型の target を探索
        /// </summary>
        /// <param name="target">見つかった target</param>
        /// <typeparam name="T">探索する Component 型</typeparam>
        /// <returns>target が見つかった場合は true</returns>
        private bool TryFindTarget<T>(out T target) where T : Component {
            if (typeof(T) == typeof(Transform) && _rootGameObject.transform is T transformTarget) {
                target = transformTarget;
                return true;
            }

            var rootTarget = _rootGameObject.GetComponent<T>();
            if (rootTarget != null) {
                target = rootTarget;
                return true;
            }

            target = _rootGameObject.GetComponentInChildren<T>(true);
            return target != null;
        }

        /// <summary>
        /// key と Component の対応を Preview target として登録
        /// </summary>
        /// <param name="key">Target key</param>
        /// <param name="target">登録する Component</param>
        private void SetTarget(string key, Component target) {
            _targetsByKey[key] = target;
        }
    }
}
