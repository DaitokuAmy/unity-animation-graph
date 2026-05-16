using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// target key と Component 参照の binding
    /// </summary>
    [Serializable]
    public struct AnimationGraphTargetBinding {
        [SerializeField, Tooltip("Component 参照を解決するための target key")]
        private string _key;
        [SerializeField, Tooltip("target key に対応する Component 参照")]
        private Component _target;

        /// <summary>target key</summary>
        public string Key => _key ?? string.Empty;
        /// <summary>target Component</summary>
        public Component Target => _target;

        /// <summary>
        /// AnimationGraphTargetBinding を生成
        /// </summary>
        /// <param name="key">target key</param>
        /// <param name="target">target Component</param>
        public AnimationGraphTargetBinding(string key, Component target) {
            _key = key ?? string.Empty;
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
