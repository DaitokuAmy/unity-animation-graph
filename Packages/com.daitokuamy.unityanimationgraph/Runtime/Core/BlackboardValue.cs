using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Runner が保持する Blackboard の現在値
    /// </summary>
    public struct BlackboardValue {
        private readonly string _key;
        private readonly BlackboardValueType _valueType;

        private bool _boolValue;
        private int _intValue;
        private float _floatValue;
        private string _stringValue;
        private Vector2 _vector2Value;
        private Vector3 _vector3Value;
        private Color _colorValue;
        private Vector4 _vector4Value;

        /// <summary>Blackboard key</summary>
        public string Key => _key ?? string.Empty;
        /// <summary>Blackboard value type</summary>
        public BlackboardValueType ValueType => _valueType;
        /// <summary>bool value</summary>
        public bool BoolValue => _boolValue;
        /// <summary>int value</summary>
        public int IntValue => _intValue;
        /// <summary>float value</summary>
        public float FloatValue => _floatValue;
        /// <summary>string value</summary>
        public string StringValue => _stringValue ?? string.Empty;
        /// <summary>Vector2 value</summary>
        public Vector2 Vector2Value => _vector2Value;
        /// <summary>Vector3 value</summary>
        public Vector3 Vector3Value => _vector3Value;
        /// <summary>Color value</summary>
        public Color ColorValue => _colorValue;
        /// <summary>Vector4 value</summary>
        public Vector4 Vector4Value => _vector4Value;

        /// <summary>
        /// BlackboardValue を生成
        /// </summary>
        /// <param name="definition">初期値として使う Blackboard 定義</param>
        public BlackboardValue(BlackboardDefinition definition) {
            _key = definition.Key;
            _valueType = definition.ValueType;
            _boolValue = definition.DefaultBoolValue;
            _intValue = definition.DefaultIntValue;
            _floatValue = definition.DefaultFloatValue;
            _stringValue = definition.DefaultStringValue;
            _vector2Value = definition.DefaultVector2Value;
            _vector3Value = definition.DefaultVector3Value;
            _colorValue = definition.DefaultColorValue;
            _vector4Value = definition.DefaultVector4Value;
        }

        /// <summary>
        /// bool 現在値の取得を試行
        /// </summary>
        /// <param name="value">取得した bool 値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetValue(out bool value) {
            if (_valueType == BlackboardValueType.Bool) {
                value = _boolValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// int 現在値の取得を試行
        /// </summary>
        /// <param name="value">取得した int 値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetValue(out int value) {
            if (_valueType == BlackboardValueType.Int) {
                value = _intValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// float 現在値の取得を試行
        /// </summary>
        /// <param name="value">取得した float 値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetValue(out float value) {
            if (_valueType == BlackboardValueType.Float) {
                value = _floatValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// string 現在値の取得を試行
        /// </summary>
        /// <param name="value">取得した string 値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetValue(out string value) {
            if (_valueType == BlackboardValueType.String) {
                value = _stringValue ?? string.Empty;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Vector2 現在値の取得を試行
        /// </summary>
        /// <param name="value">取得した Vector2 値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetValue(out Vector2 value) {
            if (_valueType == BlackboardValueType.Vector2) {
                value = _vector2Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Vector3 現在値の取得を試行
        /// </summary>
        /// <param name="value">取得した Vector3 値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetValue(out Vector3 value) {
            if (_valueType == BlackboardValueType.Vector3) {
                value = _vector3Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Color 現在値の取得を試行
        /// </summary>
        /// <param name="value">取得した Color 値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetValue(out Color value) {
            if (_valueType == BlackboardValueType.Color) {
                value = _colorValue;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Vector4 現在値の取得を試行
        /// </summary>
        /// <param name="value">取得した Vector4 値</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetValue(out Vector4 value) {
            if (_valueType == BlackboardValueType.Vector4) {
                value = _vector4Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// bool 現在値の設定を試行
        /// </summary>
        /// <param name="value">設定する bool 値</param>
        /// <returns>設定できた場合は true</returns>
        public bool TrySetValue(bool value) {
            if (_valueType != BlackboardValueType.Bool) {
                return false;
            }

            _boolValue = value;
            return true;
        }

        /// <summary>
        /// int 現在値の設定を試行
        /// </summary>
        /// <param name="value">設定する int 値</param>
        /// <returns>設定できた場合は true</returns>
        public bool TrySetValue(int value) {
            if (_valueType != BlackboardValueType.Int) {
                return false;
            }

            _intValue = value;
            return true;
        }

        /// <summary>
        /// float 現在値の設定を試行
        /// </summary>
        /// <param name="value">設定する float 値</param>
        /// <returns>設定できた場合は true</returns>
        public bool TrySetValue(float value) {
            if (_valueType != BlackboardValueType.Float) {
                return false;
            }

            _floatValue = value;
            return true;
        }

        /// <summary>
        /// string 現在値の設定を試行
        /// </summary>
        /// <param name="value">設定する string 値</param>
        /// <returns>設定できた場合は true</returns>
        public bool TrySetValue(string value) {
            if (_valueType != BlackboardValueType.String) {
                return false;
            }

            _stringValue = value ?? string.Empty;
            return true;
        }

        /// <summary>
        /// Vector2 現在値の設定を試行
        /// </summary>
        /// <param name="value">設定する Vector2 値</param>
        /// <returns>設定できた場合は true</returns>
        public bool TrySetValue(Vector2 value) {
            if (_valueType != BlackboardValueType.Vector2) {
                return false;
            }

            _vector2Value = value;
            return true;
        }

        /// <summary>
        /// Vector3 現在値の設定を試行
        /// </summary>
        /// <param name="value">設定する Vector3 値</param>
        /// <returns>設定できた場合は true</returns>
        public bool TrySetValue(Vector3 value) {
            if (_valueType != BlackboardValueType.Vector3) {
                return false;
            }

            _vector3Value = value;
            return true;
        }

        /// <summary>
        /// Color 現在値の設定を試行
        /// </summary>
        /// <param name="value">設定する Color 値</param>
        /// <returns>設定できた場合は true</returns>
        public bool TrySetValue(Color value) {
            if (_valueType != BlackboardValueType.Color) {
                return false;
            }

            _colorValue = value;
            return true;
        }

        /// <summary>
        /// Vector4 現在値の設定を試行
        /// </summary>
        /// <param name="value">設定する Vector4 値</param>
        /// <returns>設定できた場合は true</returns>
        public bool TrySetValue(Vector4 value) {
            if (_valueType != BlackboardValueType.Vector4) {
                return false;
            }

            _vector4Value = value;
            return true;
        }
    }
}
