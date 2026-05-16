using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// string の直値または Blackboard key 参照
    /// </summary>
    [Serializable]
    public struct StringParameter {
        [SerializeField, Tooltip("値の解決元")]
        private ParameterSource _source;
        [SerializeField, BlackboardKey(AnimationGraphValueType.String), Tooltip("Blackboard の現在値を参照する key")]
        private string _blackboardKey;
        [SerializeField, Tooltip("string 型の直値")]
        private string _value;

        /// <summary>値の解決元</summary>
        public ParameterSource Source => _source;
        /// <summary>Blackboard の現在値を参照する key</summary>
        public string BlackboardKey => _blackboardKey ?? string.Empty;
        /// <summary>string 型の直値</summary>
        public string Value => _value ?? string.Empty;

        /// <summary>
        /// string 直値を持つ StringParameter を生成
        /// </summary>
        /// <param name="value">直値</param>
        public StringParameter(string value) {
            _source = ParameterSource.Value;
            _blackboardKey = string.Empty;
            _value = value ?? string.Empty;
        }

        /// <summary>
        /// string Blackboard 値を参照する StringParameter を生成
        /// </summary>
        /// <param name="blackboardKey">Blackboard key</param>
        /// <returns>生成した StringParameter</returns>
        public static StringParameter Blackboard(string blackboardKey) {
            return new StringParameter(default) {
                _source = ParameterSource.Blackboard,
                _blackboardKey = blackboardKey ?? string.Empty,
            };
        }

        /// <summary>
        /// string 値の取得を試行
        /// </summary>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <param name="value">取得した値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetValue(IAnimationGraphBlackboard blackboard, out string value) {
            if (_source == ParameterSource.Blackboard) {
                if (blackboard != null && blackboard.TryGetBlackboardValue(BlackboardKey, out value)) {
                    value ??= string.Empty;
                    return true;
                }

                value = default;
                return false;
            }

            value = Value;
            return true;
        }
    }
}
