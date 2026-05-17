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
        [SerializeField, BlackboardKey(BlackboardValueType.Color), Tooltip("Blackboard の現在値を参照する key")]
        private string _blackboardKey;
        [SerializeField, Tooltip("Color 型の直値")]
        private Color _value;
        [SerializeField, Tooltip("基準色への乗算値として扱う場合は有効")]
        private bool _relative;

        /// <summary>値の解決元</summary>
        public ParameterSource Source => _source;
        /// <summary>Blackboard の現在値を参照する key</summary>
        public string BlackboardKey => _blackboardKey ?? string.Empty;
        /// <summary>Color 型の直値</summary>
        public Color Value => _value;
        /// <summary>基準色への乗算値として扱う場合は true</summary>
        public bool Relative => _relative;

        /// <summary>
        /// Color 直値を持つ ColorParameter を生成
        /// </summary>
        /// <param name="value">直値</param>
        /// <param name="relative">基準色への乗算値として扱う場合は true</param>
        public ColorParameter(Color value, bool relative = false) {
            _source = ParameterSource.Value;
            _blackboardKey = string.Empty;
            _value = value;
            _relative = relative;
        }

        /// <summary>
        /// Color Blackboard 値を参照する ColorParameter を生成
        /// </summary>
        /// <param name="blackboardKey">Blackboard key</param>
        /// <param name="relative">基準色への乗算値として扱う場合は true</param>
        /// <returns>生成した ColorParameter</returns>
        public static ColorParameter Blackboard(string blackboardKey, bool relative = false) {
            return new ColorParameter(default) {
                _source = ParameterSource.Blackboard,
                _blackboardKey = blackboardKey ?? string.Empty,
                _relative = relative,
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

        /// <summary>
        /// Color 値の取得を試行し、相対値の場合は基準色へ乗算
        /// </summary>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <param name="baseValue">乗算値の基準色</param>
        /// <param name="value">取得した値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetValue(IAnimationGraphBlackboard blackboard, Color baseValue, out Color value) {
            if (!TryGetValue(blackboard, out value)) {
                return false;
            }

            if (_relative) {
                if (value == default(Color)) {
                    value = Color.white;
                }

                value *= baseValue;
            }

            return true;
        }
    }
}
