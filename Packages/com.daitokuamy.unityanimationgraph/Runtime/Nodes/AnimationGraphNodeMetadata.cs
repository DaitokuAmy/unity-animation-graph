using System;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityAnimationGraph {
    /// <summary>
    /// Animation Graph Node のエディタ表示情報を解決するクラス
    /// </summary>
    internal static class AnimationGraphNodeMetadata {
        /// <summary>
        /// GraphView 上の表示名を取得
        /// </summary>
        /// <param name="nodeType">対象の Node 型</param>
        /// <returns>GraphView 上の表示名</returns>
        public static string GetDisplayName(Type nodeType) {
            if (nodeType == null) {
                throw new ArgumentNullException(nameof(nodeType));
            }

            var attribute = GetAttribute(nodeType);
            if (attribute != null && !string.IsNullOrWhiteSpace(attribute.DisplayName)) {
                return attribute.DisplayName;
            }

            return NicifyTypeName(nodeType.Name);
        }

        /// <summary>
        /// 右クリック作成メニュー内のパスを取得
        /// </summary>
        /// <param name="nodeType">対象の Node 型</param>
        /// <returns>右クリック作成メニュー内のパス</returns>
        public static string GetCreateMenuPath(Type nodeType) {
            if (nodeType == null) {
                throw new ArgumentNullException(nameof(nodeType));
            }

            var categoryPath = GetCreateMenuCategoryPath(nodeType);
            var attribute = GetAttribute(nodeType);
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.MenuPath)) {
                return CombineMenuPath(categoryPath, GetDisplayName(nodeType));
            }

            var createMenuPath = attribute.MenuPath.Trim('/');
            return CombineMenuPath(categoryPath, string.IsNullOrWhiteSpace(createMenuPath) ? GetDisplayName(nodeType) : createMenuPath);
        }

        /// <summary>
        /// Node 型の AnimationGraphNodeAttribute を取得
        /// </summary>
        /// <param name="nodeType">対象の Node 型</param>
        /// <returns>AnimationGraphNodeAttribute。未定義の場合は null</returns>
        private static AnimationGraphNodeAttribute GetAttribute(Type nodeType) {
            return nodeType.GetCustomAttribute<AnimationGraphNodeAttribute>(false);
        }

        /// <summary>
        /// 型名を GraphView 上の表示名へ変換
        /// </summary>
        /// <param name="typeName">変換する型名</param>
        /// <returns>GraphView 上の表示名</returns>
        private static string NicifyTypeName(string typeName) {
            var displayNameWithoutSuffix = RemoveNodeSuffix(typeName);
#if UNITY_EDITOR
            return ObjectNames.NicifyVariableName(displayNameWithoutSuffix);
#else
            return displayNameWithoutSuffix;
#endif
        }

        /// <summary>
        /// 型名末尾の Node サフィックスを除去
        /// </summary>
        /// <param name="typeName">対象の型名</param>
        /// <returns>Node サフィックスを除去した型名</returns>
        private static string RemoveNodeSuffix(string typeName) {
            if (typeName.Length <= "Node".Length || !typeName.EndsWith("Node", StringComparison.Ordinal)) {
                return typeName;
            }

            return typeName.Substring(0, typeName.Length - "Node".Length).TrimEnd();
        }

        /// <summary>
        /// Node 型に対応する作成メニューカテゴリパスを取得
        /// </summary>
        /// <param name="nodeType">対象の Node 型</param>
        /// <returns>作成メニューカテゴリパス</returns>
        private static string GetCreateMenuCategoryPath(Type nodeType) {
            if (typeof(ActionNode).IsAssignableFrom(nodeType)) {
                return "Action";
            }

            if (typeof(ControlNode).IsAssignableFrom(nodeType)) {
                return "Control";
            }

            return string.Empty;
        }

        /// <summary>
        /// 作成メニューカテゴリパスと相対パスを結合
        /// </summary>
        /// <param name="categoryPath">作成メニューカテゴリパス</param>
        /// <param name="relativePath">カテゴリ内の相対パス</param>
        /// <returns>結合した作成メニューパス</returns>
        private static string CombineMenuPath(string categoryPath, string relativePath) {
            if (string.IsNullOrWhiteSpace(categoryPath)) {
                return relativePath;
            }

            if (string.IsNullOrWhiteSpace(relativePath)) {
                return categoryPath;
            }

            return $"{categoryPath}/{relativePath.Trim('/')}";
        }
    }
}
