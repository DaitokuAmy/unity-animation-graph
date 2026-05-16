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
        [SerializeField, BlackboardKey(AnimationGraphValueType.Vector3), Tooltip("Blackboard の現在値を参照する key")]
        private string _blackboardKey;
        [SerializeField, Tooltip("Vector3 型の直値")]
        private Vector3 _value;

        /// <summary>値の解決元</summary>
        public ParameterSource Source => _source;
        /// <summary>Blackboard の現在値を参照する key</summary>
        public string BlackboardKey => _blackboardKey ?? string.Empty;
        /// <summary>Vector3 型の直値</summary>
        public Vector3 Value => _value;

        /// <summary>
        /// Vector3 直値を持つ Vector3Parameter を生成
        /// </summary>
        /// <param name="value">直値</param>
        public Vector3Parameter(Vector3 value) {
            _source = ParameterSource.Value;
            _blackboardKey = string.Empty;
            _value = value;
        }

        /// <summary>
        /// Vector3 Blackboard 値を参照する Vector3Parameter を生成
        /// </summary>
        /// <param name="blackboardKey">Blackboard key</param>
        /// <returns>生成した Vector3Parameter</returns>
        public static Vector3Parameter Blackboard(string blackboardKey) {
            return new Vector3Parameter(default) {
                _source = ParameterSource.Blackboard,
                _blackboardKey = blackboardKey ?? string.Empty,
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
    }
}
