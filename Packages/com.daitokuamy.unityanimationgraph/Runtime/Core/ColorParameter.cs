using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Color の直値または Blackboard key 参照
    /// </summary>
    [Serializable]
    public struct ColorParameter {
        [SerializeField, Tooltip("値の解決元")]
        private ParameterSource _source;
        [SerializeField, BlackboardKey(AnimationGraphValueType.Color), Tooltip("Blackboard の現在値を参照する key")]
        private string _blackboardKey;
        [SerializeField, Tooltip("Color 型の直値")]
        private Color _value;

        /// <summary>値の解決元</summary>
        public ParameterSource Source => _source;
        /// <summary>Blackboard の現在値を参照する key</summary>
        public string BlackboardKey => _blackboardKey ?? string.Empty;
        /// <summary>Color 型の直値</summary>
        public Color Value => _value;

        /// <summary>
        /// Color 直値を持つ ColorParameter を生成
        /// </summary>
        /// <param name="value">直値</param>
        public ColorParameter(Color value) {
            _source = ParameterSource.Value;
            _blackboardKey = string.Empty;
            _value = value;
        }

        /// <summary>
        /// Color Blackboard 値を参照する ColorParameter を生成
        /// </summary>
        /// <param name="blackboardKey">Blackboard key</param>
        /// <returns>生成した ColorParameter</returns>
        public static ColorParameter Blackboard(string blackboardKey) {
            return new ColorParameter(default) {
                _source = ParameterSource.Blackboard,
                _blackboardKey = blackboardKey ?? string.Empty,
            };
        }

        /// <summary>
        /// Color 値の取得を試行
        /// </summary>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <param name="value">取得した値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetValue(IAnimationGraphBlackboard blackboard, out Color value) {
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
