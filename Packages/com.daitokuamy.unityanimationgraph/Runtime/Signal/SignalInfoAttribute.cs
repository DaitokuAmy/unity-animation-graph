using System;

namespace UnityAnimationGraph {
    /// <summary>
    /// Signal のエディタ表示情報を定義する属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class SignalInfoAttribute : Attribute {
        /// <summary>GraphView 上の表示名</summary>
        public string DisplayName { get; }
        /// <summary>右クリック作成メニュー内の相対パス</summary>
        public string MenuPath { get; }

        /// <summary>
        /// SignalInfoAttribute を作成
        /// </summary>
        /// <param name="displayName">GraphView 上の表示名</param>
        /// <param name="menuPath">右クリック作成メニュー内の相対パス</param>
        public SignalInfoAttribute(string displayName, string menuPath = null) {
            DisplayName = displayName ?? string.Empty;
            MenuPath = menuPath ?? string.Empty;
        }
    }
}
