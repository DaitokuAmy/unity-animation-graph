using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// GraphAsset GUID ごとの target binding group
    /// </summary>
    [Serializable]
    public sealed class TargetBindingGroup {
        [SerializeField, Tooltip("対応する GraphAsset の Unity アセット GUID")]
        private string _graphAssetGuid;
        [SerializeField, Tooltip("この GraphAsset 用の target binding 一覧")]
        private TargetBinding[] _bindings = Array.Empty<TargetBinding>();

        /// <summary>GraphAsset の asset GUID</summary>
        public string GraphAssetGuid => _graphAssetGuid ?? string.Empty;
        /// <summary>target binding 一覧</summary>
        public IReadOnlyList<TargetBinding> Bindings => _bindings ?? Array.Empty<TargetBinding>();

        /// <summary>
        /// TargetBindingGroup を生成
        /// </summary>
        /// <param name="graphAssetGuid">GraphAsset の asset GUID</param>
        /// <param name="targetDefinitions">target 定義一覧</param>
        public TargetBindingGroup(string graphAssetGuid, IReadOnlyList<TargetDefinition> targetDefinitions) {
            _graphAssetGuid = graphAssetGuid ?? string.Empty;
            SetTargetDefinitions(targetDefinitions);
        }

        /// <summary>
        /// target 定義に合わせて binding 一覧を更新
        /// </summary>
        /// <param name="targetDefinitions">target 定義一覧</param>
        public void SetTargetDefinitions(IReadOnlyList<TargetDefinition> targetDefinitions) {
            if (targetDefinitions == null || targetDefinitions.Count == 0) {
                _bindings = Array.Empty<TargetBinding>();
                return;
            }

            var nextBindings = new TargetBinding[targetDefinitions.Count];
            for (var i = 0; i < targetDefinitions.Count; i++) {
                var definition = targetDefinitions[i];
                var key = definition.Key;
                TryGetBinding(key, out var currentBinding);
                nextBindings[i] = new TargetBinding(key, currentBinding.Target, currentBinding.Targets, definition.MonoScriptGuid, definition.Multiplicity);
            }

            _bindings = nextBindings;
        }

        /// <summary>
        /// target Component を設定
        /// </summary>
        /// <param name="key">target key</param>
        /// <param name="target">設定する Component</param>
        /// <returns>設定できた場合は true</returns>
        public bool SetTarget(string key, Component target) {
            var bindingIndex = FindBindingIndex(key);
            if (bindingIndex < 0) {
                return false;
            }

            var binding = _bindings[bindingIndex];
            if (binding.Multiplicity != TargetMultiplicity.Single) {
                return false;
            }

            _bindings[bindingIndex] = new TargetBinding(key, target, binding.Targets, binding.MonoScriptGuid, binding.Multiplicity);
            return true;
        }

        /// <summary>
        /// target collection を設定
        /// </summary>
        /// <param name="key">target key</param>
        /// <param name="targets">設定する Component 一覧</param>
        /// <returns>設定できた場合は true</returns>
        public bool SetTargets(string key, IReadOnlyList<Component> targets) {
            var bindingIndex = FindBindingIndex(key);
            if (bindingIndex < 0 || _bindings[bindingIndex].Multiplicity != TargetMultiplicity.Collection) {
                return false;
            }

            var binding = _bindings[bindingIndex];
            _bindings[bindingIndex] = new TargetBinding(key, binding.Target, targets, binding.MonoScriptGuid, binding.Multiplicity);
            return true;
        }

        /// <summary>
        /// target collection に Component を追加
        /// </summary>
        /// <param name="key">target key</param>
        /// <param name="target">追加する Component</param>
        /// <returns>追加できた場合は true</returns>
        public bool AddTarget(string key, Component target) {
            if (!TryGetCollectionBinding(key, out var bindingIndex, out var binding)) {
                return false;
            }

            var targets = new List<Component>(binding.Targets) { target };
            _bindings[bindingIndex] = new TargetBinding(key, binding.Target, targets, binding.MonoScriptGuid, binding.Multiplicity);
            return true;
        }

        /// <summary>
        /// target collection から最初に一致する Component を削除
        /// </summary>
        /// <param name="key">target key</param>
        /// <param name="target">削除する Component</param>
        /// <returns>削除できた場合は true</returns>
        public bool RemoveTarget(string key, Component target) {
            if (!TryGetCollectionBinding(key, out var bindingIndex, out var binding)) {
                return false;
            }

            var targets = new List<Component>(binding.Targets);
            if (!targets.Remove(target)) {
                return false;
            }

            _bindings[bindingIndex] = new TargetBinding(key, binding.Target, targets, binding.MonoScriptGuid, binding.Multiplicity);
            return true;
        }

        /// <summary>
        /// target collection を空にする
        /// </summary>
        /// <param name="key">target key</param>
        /// <returns>空にできた場合は true</returns>
        public bool ClearTargets(string key) {
            return SetTargets(key, Array.Empty<Component>());
        }

        /// <summary>
        /// 指定型の target 取得を試行
        /// </summary>
        /// <param name="key">target key</param>
        /// <param name="target">取得した target</param>
        /// <typeparam name="T">取得する Component 型</typeparam>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetTarget<T>(string key, out T target) where T : Component {
            if (TryGetBinding(key, out var binding) && binding.TryGetTarget(out target)) {
                return true;
            }

            target = null;
            return false;
        }

        /// <summary>
        /// 指定型の target collection 取得を試行
        /// </summary>
        /// <param name="key">target key</param>
        /// <param name="targets">取得した target collection</param>
        /// <typeparam name="T">取得する Component 型</typeparam>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetTargets<T>(string key, out IReadOnlyList<T> targets) where T : Component {
            if (TryGetBinding(key, out var binding) && binding.TryGetTargets(out targets)) {
                return true;
            }

            targets = Array.Empty<T>();
            return false;
        }

        /// <summary>
        /// target binding の取得を試行
        /// </summary>
        /// <param name="key">target key</param>
        /// <param name="binding">取得した binding</param>
        /// <returns>取得できた場合は true</returns>
        private bool TryGetBinding(string key, out TargetBinding binding) {
            var bindingIndex = FindBindingIndex(key);
            if (bindingIndex >= 0) {
                binding = _bindings[bindingIndex];
                return true;
            }

            binding = default;
            return false;
        }

        private bool TryGetCollectionBinding(string key, out int bindingIndex, out TargetBinding binding) {
            bindingIndex = FindBindingIndex(key);
            if (bindingIndex >= 0 && _bindings[bindingIndex].Multiplicity == TargetMultiplicity.Collection) {
                binding = _bindings[bindingIndex];
                return true;
            }

            binding = default;
            return false;
        }

        /// <summary>
        /// target binding の index を検索
        /// </summary>
        /// <param name="key">target key</param>
        /// <returns>見つかった index。見つからない場合は -1</returns>
        private int FindBindingIndex(string key) {
            if (string.IsNullOrEmpty(key)) {
                return -1;
            }

            var bindings = _bindings ?? Array.Empty<TargetBinding>();
            for (var i = 0; i < bindings.Length; i++) {
                if (bindings[i].Key == key) {
                    return i;
                }
            }

            return -1;
        }
    }
}
