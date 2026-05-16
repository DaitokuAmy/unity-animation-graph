using System;
using System.Reflection;

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
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.DisplayName)) {
                return nodeType.Name;
            }

            return attribute.DisplayName;
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

        private static AnimationGraphNodeAttribute GetAttribute(Type nodeType) {
            return nodeType.GetCustomAttribute<AnimationGraphNodeAttribute>(false);
        }

        private static string GetCreateMenuCategoryPath(Type nodeType) {
            if (typeof(ActionNode).IsAssignableFrom(nodeType)) {
                return "Action";
            }

            if (typeof(ControlNode).IsAssignableFrom(nodeType)) {
                return "Control";
            }

            return string.Empty;
        }

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
