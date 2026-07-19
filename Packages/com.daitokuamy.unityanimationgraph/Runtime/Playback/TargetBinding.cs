using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// target key と Component 参照の binding
    /// </summary>
    [Serializable]
    public struct TargetBinding {
        [SerializeField, Tooltip("Component 参照を解決するための target key")]
        private string _key;
        [SerializeField, Tooltip("target key が要求する MonoScript GUID または組み込み Component の予約 GUID")]
        private string _monoScriptGuid;
        [SerializeField, Tooltip("target key に対応する Component 参照")]
        private Component _target;
        [SerializeField, Tooltip("target key に対応する Component 参照一覧")]
        private List<Component> _targets;
        [SerializeField, Tooltip("target key が単一 target または collection のどちらを保持するか")]
        private TargetMultiplicity _multiplicity;

        /// <summary>target key</summary>
        public string Key => _key ?? string.Empty;
        /// <summary>target key が要求する MonoScript GUID または組み込み Component の予約 GUID</summary>
        public string MonoScriptGuid => _monoScriptGuid ?? string.Empty;
        /// <summary>target Component</summary>
        public Component Target => _target;
        /// <summary>target Component 一覧</summary>
        public IReadOnlyList<Component> Targets => _targets != null ? _targets : Array.Empty<Component>();
        /// <summary>target key が保持する要素数</summary>
        public TargetMultiplicity Multiplicity => _multiplicity;

        /// <summary>
        /// TargetBinding を生成
        /// </summary>
        /// <param name="key">target key</param>
        /// <param name="target">target Component</param>
        public TargetBinding(string key, Component target) {
            _key = key ?? string.Empty;
            _monoScriptGuid = string.Empty;
            _target = target;
            _targets = new List<Component>();
            _multiplicity = TargetMultiplicity.Single;
        }

        /// <summary>
        /// TargetBinding を生成
        /// </summary>
        /// <param name="key">target key</param>
        /// <param name="target">target Component</param>
        /// <param name="monoScriptGuid">target key が要求する MonoScript GUID または組み込み Component の予約 GUID</param>
        public TargetBinding(string key, Component target, string monoScriptGuid) {
            _key = key ?? string.Empty;
            _monoScriptGuid = monoScriptGuid ?? string.Empty;
            _target = target;
            _targets = new List<Component>();
            _multiplicity = TargetMultiplicity.Single;
        }

        /// <summary>
        /// TargetBinding を生成
        /// </summary>
        /// <param name="key">target key</param>
        /// <param name="target">単一 target Component</param>
        /// <param name="targets">target Component 一覧</param>
        /// <param name="monoScriptGuid">target key が要求する MonoScript GUID または組み込み Component の予約 GUID</param>
        /// <param name="multiplicity">target key が保持する要素数</param>
        public TargetBinding(string key, Component target, IReadOnlyList<Component> targets, string monoScriptGuid, TargetMultiplicity multiplicity) {
            _key = key ?? string.Empty;
            _monoScriptGuid = monoScriptGuid ?? string.Empty;
            _target = target;
            _targets = targets == null ? new List<Component>() : new List<Component>(targets);
            _multiplicity = multiplicity;
        }

        /// <summary>
        /// 指定型の target 取得を試行
        /// </summary>
        /// <param name="target">取得した target</param>
        /// <typeparam name="T">取得する Component 型</typeparam>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetTarget<T>(out T target) where T : Component {
            if (_target is T typedTarget) {
                target = typedTarget;
                return true;
            }

            target = null;
            return false;
        }

        /// <summary>
        /// 指定型の target collection 取得を試行
        /// </summary>
        /// <param name="targets">取得した target collection</param>
        /// <typeparam name="T">取得する Component 型</typeparam>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetTargets<T>(out IReadOnlyList<T> targets) where T : Component {
            if (_multiplicity != TargetMultiplicity.Collection) {
                targets = Array.Empty<T>();
                return false;
            }

            var source = _targets ?? new List<Component>();
            var typedTargets = new T[source.Count];
            for (var i = 0; i < source.Count; i++) {
                if (source[i] != null && source[i] is not T) {
                    targets = Array.Empty<T>();
                    return false;
                }

                typedTargets[i] = source[i] as T;
            }

            targets = typedTargets;
            return true;
        }
    }
}
