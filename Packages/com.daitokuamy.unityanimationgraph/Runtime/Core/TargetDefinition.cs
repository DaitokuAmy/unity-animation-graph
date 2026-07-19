using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// target key が保持する要素数
    /// </summary>
    public enum TargetMultiplicity {
        /// <summary>単一 target</summary>
        Single,
        /// <summary>target collection</summary>
        Collection,
    }

    /// <summary>
    /// AnimationGraph が要求する target key の定義
    /// </summary>
    [Serializable]
    public struct TargetDefinition {
        [SerializeField, Tooltip("Runner が Component 参照を解決するための target key")]
        private string _key;
        [SerializeField, Tooltip("target key が要求する MonoScript GUID または組み込み Component の予約 GUID")]
        private string _monoScriptGuid;
        [SerializeField, Tooltip("target key が単一 target または collection のどちらを保持するか")]
        private TargetMultiplicity _multiplicity;

        /// <summary>target key</summary>
        public string Key => _key ?? string.Empty;
        /// <summary>target key が要求する MonoScript GUID または組み込み Component の予約 GUID</summary>
        public string MonoScriptGuid => _monoScriptGuid ?? string.Empty;
        /// <summary>target key が保持する要素数</summary>
        public TargetMultiplicity Multiplicity => _multiplicity;

        /// <summary>
        /// TargetDefinition を生成
        /// </summary>
        /// <param name="key">target key</param>
        public TargetDefinition(string key) {
            _key = key ?? string.Empty;
            _monoScriptGuid = string.Empty;
            _multiplicity = TargetMultiplicity.Single;
        }

        /// <summary>
        /// TargetDefinition を生成
        /// </summary>
        /// <param name="key">target key</param>
        /// <param name="monoScriptGuid">target key が要求する MonoScript GUID または組み込み Component の予約 GUID</param>
        public TargetDefinition(string key, string monoScriptGuid) {
            _key = key ?? string.Empty;
            _monoScriptGuid = monoScriptGuid ?? string.Empty;
            _multiplicity = TargetMultiplicity.Single;
        }

        /// <summary>
        /// TargetDefinition を生成
        /// </summary>
        /// <param name="key">target key</param>
        /// <param name="monoScriptGuid">target key が要求する MonoScript GUID または組み込み Component の予約 GUID</param>
        /// <param name="multiplicity">target key が保持する要素数</param>
        public TargetDefinition(string key, string monoScriptGuid, TargetMultiplicity multiplicity) {
            _key = key ?? string.Empty;
            _monoScriptGuid = monoScriptGuid ?? string.Empty;
            _multiplicity = multiplicity;
        }
    }
}
