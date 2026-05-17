using System;
using System.Collections.Generic;

namespace UnityAnimationGraph {
    /// <summary>
    /// Node の設定検証に使用するコンテキスト
    /// </summary>
    public readonly struct NodeValidationContext {
        private readonly IReadOnlyList<TargetDefinition> _targetDefinitions;
        private readonly IReadOnlyList<BlackboardDefinition> _blackboardDefinitions;

        /// <summary>GraphAsset に定義された Target 一覧</summary>
        public IReadOnlyList<TargetDefinition> TargetDefinitions => _targetDefinitions ?? Array.Empty<TargetDefinition>();
        /// <summary>GraphAsset に定義された Blackboard 一覧</summary>
        public IReadOnlyList<BlackboardDefinition> BlackboardDefinitions => _blackboardDefinitions ?? Array.Empty<BlackboardDefinition>();

        /// <summary>
        /// NodeValidationContext を作成
        /// </summary>
        /// <param name="targetDefinitions">GraphAsset に定義された Target 一覧</param>
        /// <param name="blackboardDefinitions">GraphAsset に定義された Blackboard 一覧</param>
        public NodeValidationContext(IReadOnlyList<TargetDefinition> targetDefinitions, IReadOnlyList<BlackboardDefinition> blackboardDefinitions) {
            _targetDefinitions = targetDefinitions;
            _blackboardDefinitions = blackboardDefinitions;
        }

        /// <summary>
        /// Target 定義が存在するか判定
        /// </summary>
        /// <param name="key">Target key</param>
        /// <returns>Target 定義が存在する場合は true</returns>
        public bool HasTargetDefinition(string key) {
            return TryGetTargetDefinition(key, out _);
        }

        /// <summary>
        /// Target 定義の取得を試行
        /// </summary>
        /// <param name="key">Target key</param>
        /// <param name="definition">取得した Target 定義</param>
        /// <returns>Target 定義を取得できた場合は true</returns>
        public bool TryGetTargetDefinition(string key, out TargetDefinition definition) {
            var definitions = TargetDefinitions;
            for (var i = 0; i < definitions.Count; i++) {
                var currentDefinition = definitions[i];
                if (currentDefinition.Key != key) {
                    continue;
                }

                definition = currentDefinition;
                return true;
            }

            definition = default;
            return false;
        }

        /// <summary>
        /// 指定型の Blackboard 定義が存在するか判定
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="valueType">期待する Blackboard value type</param>
        /// <returns>指定型の Blackboard 定義が存在する場合は true</returns>
        public bool HasBlackboardDefinition(string key, BlackboardValueType valueType) {
            return TryGetBlackboardDefinition(key, out var definition) && definition.ValueType == valueType;
        }

        /// <summary>
        /// Blackboard 定義の取得を試行
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="definition">取得した Blackboard 定義</param>
        /// <returns>Blackboard 定義を取得できた場合は true</returns>
        public bool TryGetBlackboardDefinition(string key, out BlackboardDefinition definition) {
            var definitions = BlackboardDefinitions;
            for (var i = 0; i < definitions.Count; i++) {
                var currentDefinition = definitions[i];
                if (currentDefinition.Key != key) {
                    continue;
                }

                definition = currentDefinition;
                return true;
            }

            definition = default;
            return false;
        }
    }
}
