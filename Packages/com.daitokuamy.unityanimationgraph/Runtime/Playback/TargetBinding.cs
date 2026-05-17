using System;
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

        /// <summary>target key</summary>
        public string Key => _key ?? string.Empty;
        /// <summary>target key が要求する MonoScript GUID または組み込み Component の予約 GUID</summary>
        public string MonoScriptGuid => _monoScriptGuid ?? string.Empty;
        /// <summary>target Component</summary>
        public Component Target => _target;

        /// <summary>
        /// TargetBinding を生成
        /// </summary>
        /// <param name="key">target key</param>
        /// <param name="target">target Component</param>
        public TargetBinding(string key, Component target) {
            _key = key ?? string.Empty;
            _monoScriptGuid = string.Empty;
            _target = target;
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
    }
}
