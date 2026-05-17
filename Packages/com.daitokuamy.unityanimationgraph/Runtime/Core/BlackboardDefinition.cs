using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// AnimationGraph が要求する Blackboard key と値型の定義
    /// </summary>
    [Serializable]
    public struct BlackboardDefinition {
        [SerializeField, Tooltip("Blackboard の参照キー")]
        private string _key;
        [SerializeField, Tooltip("Blackboard 値の型")]
        private BlackboardValueType _valueType;
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
        [SerializeField, Tooltip("Vector4 型の初期値")]
        private Vector4 _defaultVector4Value;

        /// <summary>Blackboard key</summary>
        public string Key => _key ?? string.Empty;
        /// <summary>Blackboard value type</summary>
        public BlackboardValueType ValueType => _valueType;
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
        /// <summary>Vector4 default value</summary>
        public Vector4 DefaultVector4Value => _defaultVector4Value;

        /// <summary>
        /// BlackboardDefinition を生成
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="valueType">Blackboard value type</param>
        public BlackboardDefinition(string key, BlackboardValueType valueType) {
            _key = key ?? string.Empty;
            _valueType = valueType;
            _defaultBoolValue = false;
            _defaultIntValue = 0;
            _defaultFloatValue = 0.0f;
            _defaultStringValue = string.Empty;
            _defaultVector2Value = Vector2.zero;
            _defaultVector3Value = Vector3.zero;
            _defaultColorValue = default;
            _defaultVector4Value = Vector4.zero;
        }

        /// <summary>
        /// bool default value を持つ BlackboardDefinition を生成
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="defaultValue">bool default value</param>
        public BlackboardDefinition(string key, bool defaultValue) : this(key, BlackboardValueType.Bool) {
            _defaultBoolValue = defaultValue;
        }

        /// <summary>
        /// int default value を持つ BlackboardDefinition を生成
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="defaultValue">int default value</param>
        public BlackboardDefinition(string key, int defaultValue) : this(key, BlackboardValueType.Int) {
            _defaultIntValue = defaultValue;
        }

        /// <summary>
        /// float default value を持つ BlackboardDefinition を生成
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="defaultValue">float default value</param>
        public BlackboardDefinition(string key, float defaultValue) : this(key, BlackboardValueType.Float) {
            _defaultFloatValue = defaultValue;
        }

        /// <summary>
        /// string default value を持つ BlackboardDefinition を生成
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="defaultValue">string default value</param>
        public BlackboardDefinition(string key, string defaultValue) : this(key, BlackboardValueType.String) {
            _defaultStringValue = defaultValue ?? string.Empty;
        }

        /// <summary>
        /// Vector2 default value を持つ BlackboardDefinition を生成
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="defaultValue">Vector2 default value</param>
        public BlackboardDefinition(string key, Vector2 defaultValue) : this(key, BlackboardValueType.Vector2) {
            _defaultVector2Value = defaultValue;
        }

        /// <summary>
        /// Vector3 default value を持つ BlackboardDefinition を生成
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="defaultValue">Vector3 default value</param>
        public BlackboardDefinition(string key, Vector3 defaultValue) : this(key, BlackboardValueType.Vector3) {
            _defaultVector3Value = defaultValue;
        }

        /// <summary>
        /// Color default value を持つ BlackboardDefinition を生成
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="defaultValue">Color default value</param>
        public BlackboardDefinition(string key, Color defaultValue) : this(key, BlackboardValueType.Color) {
            _defaultColorValue = defaultValue;
        }

        /// <summary>
        /// Vector4 default value を持つ BlackboardDefinition を生成
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="defaultValue">Vector4 default value</param>
        public BlackboardDefinition(string key, Vector4 defaultValue) : this(key, BlackboardValueType.Vector4) {
            _defaultVector4Value = defaultValue;
        }

        /// <summary>
        /// bool default value の取得を試行
        /// </summary>
        /// <param name="value">取得した bool default value</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetDefaultValue(out bool value) {
            if (_valueType == BlackboardValueType.Bool) {
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
            if (_valueType == BlackboardValueType.Int) {
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
            if (_valueType == BlackboardValueType.Float) {
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
            if (_valueType == BlackboardValueType.String) {
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
            if (_valueType == BlackboardValueType.Vector2) {
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
            if (_valueType == BlackboardValueType.Vector3) {
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
            if (_valueType == BlackboardValueType.Color) {
                value = _defaultColorValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Vector4 default value の取得を試行
        /// </summary>
        /// <param name="value">取得した Vector4 default value</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetDefaultValue(out Vector4 value) {
            if (_valueType == BlackboardValueType.Vector4) {
                value = _defaultVector4Value;
                return true;
            }

            value = default;
            return false;
        }
    }
}
