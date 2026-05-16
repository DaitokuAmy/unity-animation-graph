using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// AnimationGraph が要求する Blackboard key と値型の定義
    /// </summary>
    [Serializable]
    public struct AnimationGraphBlackboardDefinition {
        [SerializeField, Tooltip("Blackboard の参照キー")]
        private string _key;
        [SerializeField, Tooltip("Blackboard 値の型")]
        private AnimationGraphValueType _valueType;
        [SerializeField, Tooltip("bool 型の初期値")]
        private bool _defaultBoolValue;
        [SerializeField, Tooltip("int 型の初期値")]
        private int _defaultIntValue;
        [SerializeField, Tooltip("float 型の初期値")]
        private float _defaultFloatValue;
        [SerializeField, Tooltip("string 型の初期値")]
        private string _defaultStringValue;
        [SerializeField, Tooltip("Vector2 型の初期値")]
        private Vector2 _defaultVector2Value;
        [SerializeField, Tooltip("Vector3 型の初期値")]
        private Vector3 _defaultVector3Value;
        [SerializeField, Tooltip("Color 型の初期値")]
        private Color _defaultColorValue;

        /// <summary>Blackboard key</summary>
        public string Key => _key ?? string.Empty;
        /// <summary>Blackboard value type</summary>
        public AnimationGraphValueType ValueType => _valueType;
        /// <summary>bool default value</summary>
        public bool DefaultBoolValue => _defaultBoolValue;
        /// <summary>int default value</summary>
        public int DefaultIntValue => _defaultIntValue;
        /// <summary>float default value</summary>
        public float DefaultFloatValue => _defaultFloatValue;
        /// <summary>string default value</summary>
        public string DefaultStringValue => _defaultStringValue ?? string.Empty;
        /// <summary>Vector2 default value</summary>
        public Vector2 DefaultVector2Value => _defaultVector2Value;
        /// <summary>Vector3 default value</summary>
        public Vector3 DefaultVector3Value => _defaultVector3Value;
        /// <summary>Color default value</summary>
        public Color DefaultColorValue => _defaultColorValue;

        /// <summary>
        /// AnimationGraphBlackboardDefinition を生成
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="valueType">Blackboard value type</param>
        public AnimationGraphBlackboardDefinition(string key, AnimationGraphValueType valueType) {
            _key = key ?? string.Empty;
            _valueType = valueType;
            _defaultBoolValue = false;
            _defaultIntValue = 0;
            _defaultFloatValue = 0.0f;
            _defaultStringValue = string.Empty;
            _defaultVector2Value = Vector2.zero;
            _defaultVector3Value = Vector3.zero;
            _defaultColorValue = default;
        }

        /// <summary>
        /// bool default value を持つ AnimationGraphBlackboardDefinition を生成
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="defaultValue">bool default value</param>
        public AnimationGraphBlackboardDefinition(string key, bool defaultValue) : this(key, AnimationGraphValueType.Bool) {
            _defaultBoolValue = defaultValue;
        }

        /// <summary>
        /// int default value を持つ AnimationGraphBlackboardDefinition を生成
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="defaultValue">int default value</param>
        public AnimationGraphBlackboardDefinition(string key, int defaultValue) : this(key, AnimationGraphValueType.Int) {
            _defaultIntValue = defaultValue;
        }

        /// <summary>
        /// float default value を持つ AnimationGraphBlackboardDefinition を生成
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="defaultValue">float default value</param>
        public AnimationGraphBlackboardDefinition(string key, float defaultValue) : this(key, AnimationGraphValueType.Float) {
            _defaultFloatValue = defaultValue;
        }

        /// <summary>
        /// string default value を持つ AnimationGraphBlackboardDefinition を生成
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="defaultValue">string default value</param>
        public AnimationGraphBlackboardDefinition(string key, string defaultValue) : this(key, AnimationGraphValueType.String) {
            _defaultStringValue = defaultValue ?? string.Empty;
        }

        /// <summary>
        /// Vector2 default value を持つ AnimationGraphBlackboardDefinition を生成
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="defaultValue">Vector2 default value</param>
        public AnimationGraphBlackboardDefinition(string key, Vector2 defaultValue) : this(key, AnimationGraphValueType.Vector2) {
            _defaultVector2Value = defaultValue;
        }

        /// <summary>
        /// Vector3 default value を持つ AnimationGraphBlackboardDefinition を生成
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="defaultValue">Vector3 default value</param>
        public AnimationGraphBlackboardDefinition(string key, Vector3 defaultValue) : this(key, AnimationGraphValueType.Vector3) {
            _defaultVector3Value = defaultValue;
        }

        /// <summary>
        /// Color default value を持つ AnimationGraphBlackboardDefinition を生成
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="defaultValue">Color default value</param>
        public AnimationGraphBlackboardDefinition(string key, Color defaultValue) : this(key, AnimationGraphValueType.Color) {
            _defaultColorValue = defaultValue;
        }

        /// <summary>
        /// bool default value の取得を試行
        /// </summary>
        /// <param name="value">取得した bool default value</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetDefaultValue(out bool value) {
            if (_valueType == AnimationGraphValueType.Bool) {
                value = _defaultBoolValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// int default value の取得を試行
        /// </summary>
        /// <param name="value">取得した int default value</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetDefaultValue(out int value) {
            if (_valueType == AnimationGraphValueType.Int) {
                value = _defaultIntValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// float default value の取得を試行
        /// </summary>
        /// <param name="value">取得した float default value</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetDefaultValue(out float value) {
            if (_valueType == AnimationGraphValueType.Float) {
                value = _defaultFloatValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// string default value の取得を試行
        /// </summary>
        /// <param name="value">取得した string default value</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetDefaultValue(out string value) {
            if (_valueType == AnimationGraphValueType.String) {
                value = _defaultStringValue ?? string.Empty;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Vector2 default value の取得を試行
        /// </summary>
        /// <param name="value">取得した Vector2 default value</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetDefaultValue(out Vector2 value) {
            if (_valueType == AnimationGraphValueType.Vector2) {
                value = _defaultVector2Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Vector3 default value の取得を試行
        /// </summary>
        /// <param name="value">取得した Vector3 default value</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetDefaultValue(out Vector3 value) {
            if (_valueType == AnimationGraphValueType.Vector3) {
                value = _defaultVector3Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Color default value の取得を試行
        /// </summary>
        /// <param name="value">取得した Color default value</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetDefaultValue(out Color value) {
            if (_valueType == AnimationGraphValueType.Color) {
                value = _defaultColorValue;
                return true;
            }

            value = default;
            return false;
        }
    }
}
