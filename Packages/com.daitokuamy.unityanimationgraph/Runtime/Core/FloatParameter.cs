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
        [SerializeField, Tooltip("基準値からの相対値として扱う場合は有効")]
        private bool _relative;

        /// <summary>値の解決元</summary>
        public ParameterSource Source => _source;
        /// <summary>Blackboard の現在値を参照する key</summary>
        public string BlackboardKey => _blackboardKey ?? string.Empty;
        /// <summary>float 型の直値</summary>
        public float Value => _value;
        /// <summary>基準値からの相対値として扱う場合は true</summary>
        public bool Relative => _relative;

        /// <summary>
        /// float 直値を持つ FloatParameter を生成
        /// </summary>
        /// <param name="value">直値</param>
        /// <param name="relative">基準値からの相対値として扱う場合は true</param>
        public FloatParameter(float value, bool relative = false) {
            _source = ParameterSource.Value;
            _blackboardKey = string.Empty;
            _value = value;
            _relative = relative;
        }

        /// <summary>
        /// float Blackboard 値を参照する FloatParameter を生成
        /// </summary>
        /// <param name="blackboardKey">Blackboard key</param>
        /// <param name="relative">基準値からの相対値として扱う場合は true</param>
        /// <returns>生成した FloatParameter</returns>
        public static FloatParameter Blackboard(string blackboardKey, bool relative = false) {
            return new FloatParameter(default) {
                _source = ParameterSource.Blackboard,
                _blackboardKey = blackboardKey ?? string.Empty,
                _relative = relative,
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

        /// <summary>
        /// float 値の取得を試行し、相対値の場合は基準値を加算
        /// </summary>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <param name="baseValue">相対値の基準値</param>
        /// <param name="value">取得した値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetValue(IAnimationGraphBlackboard blackboard, float baseValue, out float value) {
            if (!TryGetValue(blackboard, out value)) {
                return false;
            }

            if (_relative) {
                value += baseValue;
            }

            return true;
        }
    }
}
