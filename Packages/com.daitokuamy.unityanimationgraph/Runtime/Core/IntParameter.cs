using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// int の直値または Blackboard key 参照
    /// </summary>
    [Serializable]
    public struct IntParameter {
        [SerializeField, Tooltip("値の解決元")]
        private ParameterSource _source;
        [SerializeField, BlackboardKey(AnimationGraphValueType.Int), Tooltip("Blackboard の現在値を参照する key")]
        private string _blackboardKey;
        [SerializeField, Tooltip("int 型の直値")]
        private int _value;

        /// <summary>値の解決元</summary>
        public ParameterSource Source => _source;
        /// <summary>Blackboard の現在値を参照する key</summary>
        public string BlackboardKey => _blackboardKey ?? string.Empty;
        /// <summary>int 型の直値</summary>
        public int Value => _value;

        /// <summary>
        /// int 直値を持つ IntParameter を生成
        /// </summary>
        /// <param name="value">直値</param>
        public IntParameter(int value) {
            _source = ParameterSource.Value;
            _blackboardKey = string.Empty;
            _value = value;
        }

        /// <summary>
        /// int Blackboard 値を参照する IntParameter を生成
        /// </summary>
        /// <param name="blackboardKey">Blackboard key</param>
        /// <returns>生成した IntParameter</returns>
        public static IntParameter Blackboard(string blackboardKey) {
            return new IntParameter(default) {
                _source = ParameterSource.Blackboard,
                _blackboardKey = blackboardKey ?? string.Empty,
            };
        }

        /// <summary>
        /// int 値の取得を試行
        /// </summary>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <param name="value">取得した値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetValue(IAnimationGraphBlackboard blackboard, out int value) {
            if (_source == ParameterSource.Blackboard) {
                if (blackboard != null && blackboard.TryGetBlackboardValue(BlackboardKey, out value)) {
                    return true;
                }

                value = default;
                return false;
            }

            value = _value;
            return true;
        }
    }
}
