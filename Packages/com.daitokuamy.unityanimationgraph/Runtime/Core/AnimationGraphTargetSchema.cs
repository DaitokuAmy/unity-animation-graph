using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// 複数の AnimationGraphAsset で共有する target 定義
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationGraphTargetSchema", menuName = "Unity Animation Graph/Target Schema")]
    public sealed class AnimationGraphTargetSchema : ScriptableObject {
        [SerializeField, Tooltip("Runner がバインドする target key 定義一覧")]
        private TargetDefinition[] _definitions = Array.Empty<TargetDefinition>();

        /// <summary>target key 定義一覧</summary>
        public IReadOnlyList<TargetDefinition> Definitions => _definitions ?? Array.Empty<TargetDefinition>();

        /// <summary>
        /// 指定した key に対応する target 定義の取得を試行
        /// </summary>
        /// <param name="key">取得する target key</param>
        /// <param name="definition">取得した target 定義</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetDefinition(string key, out TargetDefinition definition) {
            if (string.IsNullOrEmpty(key)) {
                definition = default;
                return false;
            }

            var definitions = _definitions ?? Array.Empty<TargetDefinition>();
            for (var i = 0; i < definitions.Length; i++) {
                var current = definitions[i];
                if (current.Key != key) {
                    continue;
                }

                definition = current;
                return true;
            }

            definition = default;
            return false;
        }
    }
}
