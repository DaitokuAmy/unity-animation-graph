using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Vector3 の直値または Blackboard key 参照
    /// </summary>
    [Serializable]
    public struct Vector3Parameter {
        [SerializeField, Tooltip("値の解決元")]
        private ParameterSource _source;
        [SerializeField, BlackboardKey(BlackboardValueType.Vector3), Tooltip("Blackboard の現在値を参照する key")]
        private string _blackboardKey;
        [SerializeField, Tooltip("Vector3 型の直値")]
        private Vector3 _value;
        [SerializeField, Tooltip("基準値からの相対値として扱う場合は有効")]
        private bool _relative;

        /// <summary>値の解決元</summary>
        public ParameterSource Source => _source;
        /// <summary>Blackboard の現在値を参照する key</summary>
        public string BlackboardKey => _blackboardKey ?? string.Empty;
        /// <summary>Vector3 型の直値</summary>
        public Vector3 Value => _value;
        /// <summary>基準値からの相対値として扱う場合は true</summary>
        public bool Relative => _relative;

        /// <summary>
        /// Vector3 直値を持つ Vector3Parameter を生成
        /// </summary>
        /// <param name="value">直値</param>
        /// <param name="relative">基準値からの相対値として扱う場合は true</param>
        public Vector3Parameter(Vector3 value, bool relative = false) {
            _source = ParameterSource.Value;
            _blackboardKey = string.Empty;
            _value = value;
            _relative = relative;
        }

        /// <summary>
        /// Vector3 Blackboard 値を参照する Vector3Parameter を生成
        /// </summary>
        /// <param name="blackboardKey">Blackboard key</param>
        /// <param name="relative">基準値からの相対値として扱う場合は true</param>
        /// <returns>生成した Vector3Parameter</returns>
        public static Vector3Parameter Blackboard(string blackboardKey, bool relative = false) {
            return new Vector3Parameter(default) {
                _source = ParameterSource.Blackboard,
                _blackboardKey = blackboardKey ?? string.Empty,
                _relative = relative,
            };
        }

        /// <summary>
        /// Vector3 値の取得を試行
        /// </summary>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <param name="value">取得した値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetValue(IAnimationGraphBlackboard blackboard, out Vector3 value) {
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
        /// Vector3 値の取得を試行し、相対値の場合は基準値を加算
        /// </summary>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <param name="baseValue">相対値の基準値</param>
        /// <param name="value">取得した値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetValue(IAnimationGraphBlackboard blackboard, Vector3 baseValue, out Vector3 value) {
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
