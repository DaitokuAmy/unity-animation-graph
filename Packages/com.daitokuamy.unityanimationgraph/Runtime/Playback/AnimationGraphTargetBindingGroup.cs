using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// GraphAsset GUID ごとの target binding group
    /// </summary>
    [Serializable]
    public sealed class AnimationGraphTargetBindingGroup {
        [SerializeField, Tooltip("対応する GraphAsset の Unity アセット GUID")]
        private string _graphAssetGuid;
        [SerializeField, Tooltip("この GraphAsset 用の target binding 一覧")]
        private AnimationGraphTargetBinding[] _bindings = Array.Empty<AnimationGraphTargetBinding>();

        /// <summary>GraphAsset の asset GUID</summary>
        public string GraphAssetGuid => _graphAssetGuid ?? string.Empty;
        /// <summary>target binding 一覧</summary>
        public IReadOnlyList<AnimationGraphTargetBinding> Bindings => _bindings ?? Array.Empty<AnimationGraphTargetBinding>();

        /// <summary>
        /// AnimationGraphTargetBindingGroup を生成
        /// </summary>
        /// <param name="graphAssetGuid">GraphAsset の asset GUID</param>
        /// <param name="targetDefinitions">target 定義一覧</param>
        public AnimationGraphTargetBindingGroup(string graphAssetGuid, IReadOnlyList<AnimationGraphTargetDefinition> targetDefinitions) {
            _graphAssetGuid = graphAssetGuid ?? string.Empty;
            SetTargetDefinitions(targetDefinitions);
        }

        /// <summary>
        /// target 定義に合わせて binding 一覧を更新
        /// </summary>
        /// <param name="targetDefinitions">target 定義一覧</param>
        public void SetTargetDefinitions(IReadOnlyList<AnimationGraphTargetDefinition> targetDefinitions) {
            if (targetDefinitions == null || targetDefinitions.Count == 0) {
                _bindings = Array.Empty<AnimationGraphTargetBinding>();
                return;
            }

            var nextBindings = new AnimationGraphTargetBinding[targetDefinitions.Count];
            for (var i = 0; i < targetDefinitions.Count; i++) {
                var key = targetDefinitions[i].Key;
                TryGetBinding(key, out var currentBinding);
                nextBindings[i] = new AnimationGraphTargetBinding(key, currentBinding.Target);
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

            _bindings[bindingIndex] = new AnimationGraphTargetBinding(key, target);
            return true;
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
        /// target binding の取得を試行
        /// </summary>
        /// <param name="key">target key</param>
        /// <param name="binding">取得した binding</param>
        /// <returns>取得できた場合は true</returns>
        private bool TryGetBinding(string key, out AnimationGraphTargetBinding binding) {
            var bindingIndex = FindBindingIndex(key);
            if (bindingIndex >= 0) {
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

            var bindings = _bindings ?? Array.Empty<AnimationGraphTargetBinding>();
            for (var i = 0; i < bindings.Length; i++) {
                if (bindings[i].Key == key) {
                    return i;
                }
            }

            return -1;
        }
    }
}
