using System;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityAnimationGraph {
    /// <summary>
    /// Signal のエディタ表示情報を解決するクラス
    /// </summary>
    internal static class SignalMetadata {
        /// <summary>
        /// GraphView 上の表示名を取得
        /// </summary>
        /// <param name="signalType">対象の Signal 型</param>
        /// <returns>GraphView 上の表示名</returns>
        public static string GetDisplayName(Type signalType) {
            if (signalType == null) {
                throw new ArgumentNullException(nameof(signalType));
            }

            var attribute = GetAttribute(signalType);
            if (attribute != null && !string.IsNullOrWhiteSpace(attribute.DisplayName)) {
                return attribute.DisplayName;
            }

            return NicifyTypeName(signalType.Name);
        }

        /// <summary>
        /// 右クリック作成メニュー内のパスを取得
        /// </summary>
        /// <param name="signalType">対象の Signal 型</param>
        /// <returns>右クリック作成メニュー内のパス</returns>
        public static string GetCreateMenuPath(Type signalType) {
            if (signalType == null) {
                throw new ArgumentNullException(nameof(signalType));
            }

            var attribute = GetAttribute(signalType);
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.MenuPath)) {
                return GetDisplayName(signalType);
            }

            var createMenuPath = attribute.MenuPath.Trim('/');
            return string.IsNullOrWhiteSpace(createMenuPath) ? GetDisplayName(signalType) : createMenuPath;
        }

        /// <summary>
        /// Signal 型の SignalInfoAttribute を取得
        /// </summary>
        /// <param name="signalType">対象の Signal 型</param>
        /// <returns>SignalInfoAttribute。未定義の場合は null</returns>
        private static SignalInfoAttribute GetAttribute(Type signalType) {
            return signalType.GetCustomAttribute<SignalInfoAttribute>(false);
        }

        /// <summary>
        /// 型名を GraphView 上の表示名へ変換
        /// </summary>
        /// <param name="typeName">変換する型名</param>
        /// <returns>GraphView 上の表示名</returns>
        private static string NicifyTypeName(string typeName) {
            var displayNameWithoutSuffix = RemoveSignalSuffix(typeName);
#if UNITY_EDITOR
            return ObjectNames.NicifyVariableName(displayNameWithoutSuffix);
#else
            return displayNameWithoutSuffix;
#endif
        }

        /// <summary>
        /// 型名末尾の Signal サフィックスを除去
        /// </summary>
        /// <param name="typeName">対象の型名</param>
        /// <returns>Signal サフィックスを除去した型名</returns>
        private static string RemoveSignalSuffix(string typeName) {
            if (typeName.Length <= "Signal".Length || !typeName.EndsWith("Signal", StringComparison.Ordinal)) {
                return typeName;
            }

            return typeName.Substring(0, typeName.Length - "Signal".Length).TrimEnd();
        }
    }
}
