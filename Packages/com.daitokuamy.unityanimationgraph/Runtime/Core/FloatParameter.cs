using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// float の直値または Blackboard key 参照
    /// </summary>
    [Serializable]
    public struct FloatParameter {
        [SerializeField, Tooltip("値の解決元")]
        private ParameterSource _source;
        [SerializeField, BlackboardKey(AnimationGraphValueType.Float), Tooltip("Blackboard の現在値を参照する key")]
        private string _blackboardKey;
        [SerializeField, Tooltip("float 型の直値")]
        private float _value;

        /// <summary>値の解決元</summary>
        public ParameterSource Source => _source;
        /// <summary>Blackboard の現在値を参照する key</summary>
        public string BlackboardKey => _blackboardKey ?? string.Empty;
        /// <summary>float 型の直値</summary>
        public float Value => _value;

        /// <summary>
        /// float 直値を持つ FloatParameter を生成
        /// </summary>
        /// <param name="value">直値</param>
        public FloatParameter(float value) {
            _source = ParameterSource.Value;
            _blackboardKey = string.Empty;
            _value = value;
        }

        /// <summary>
        /// float Blackboard 値を参照する FloatParameter を生成
        /// </summary>
        /// <param name="blackboardKey">Blackboard key</param>
        /// <returns>生成した FloatParameter</returns>
        public static FloatParameter Blackboard(string blackboardKey) {
            return new FloatParameter(default) {
                _source = ParameterSource.Blackboard,
                _blackboardKey = blackboardKey ?? string.Empty,
            };
        }

        /// <summary>
        /// float 値の取得を試行
        /// </summary>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <param name="value">取得した値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetValue(IAnimationGraphBlackboard blackboard, out float value) {
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
