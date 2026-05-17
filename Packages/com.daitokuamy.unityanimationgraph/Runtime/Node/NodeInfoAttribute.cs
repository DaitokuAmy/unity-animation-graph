using System;

namespace UnityAnimationGraph {
    /// <summary>
    /// Node のエディタ表示情報を定義する属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class NodeInfoAttribute : Attribute {
        /// <summary>GraphView 上の表示名</summary>
        public string DisplayName { get; }
        /// <summary>右クリック作成メニュー内の相対パス</summary>
        public string MenuPath { get; }

        /// <summary>
        /// NodeInfoAttribute を作成
        /// </summary>
        /// <param name="displayName">GraphView 上の表示名</param>
        /// <param name="menuPath">右クリック作成メニュー内の相対パス</param>
        public NodeInfoAttribute(string displayName, string menuPath = null) {
            DisplayName = displayName ?? string.Empty;
            MenuPath = menuPath ?? string.Empty;
        }
    }

    /// <summary>
    /// Node の GraphView 詳細に表示する serialized field を定義する属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class NodeDetailFieldAttribute : Attribute {
        /// <summary>GraphView 詳細に表示するラベル</summary>
        public string Label { get; }

        /// <summary>
        /// NodeDetailFieldAttribute を作成
        /// </summary>
        /// <param name="label">GraphView 詳細に表示するラベル</param>
        public NodeDetailFieldAttribute(string label = null) {
            Label = label ?? string.Empty;
        }
    }
}
