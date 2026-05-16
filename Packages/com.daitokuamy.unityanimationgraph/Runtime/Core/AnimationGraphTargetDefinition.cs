using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// AnimationGraph が要求する target key の定義
    /// </summary>
    [Serializable]
    public struct AnimationGraphTargetDefinition {
        [SerializeField, Tooltip("Runner が Component 参照を解決するための target key")]
        private string _key;

        /// <summary>target key</summary>
        public string Key => _key ?? string.Empty;

        /// <summary>
        /// AnimationGraphTargetDefinition を生成
        /// </summary>
        /// <param name="key">target key</param>
        public AnimationGraphTargetDefinition(string key) {
            _key = key ?? string.Empty;
        }
    }
}
