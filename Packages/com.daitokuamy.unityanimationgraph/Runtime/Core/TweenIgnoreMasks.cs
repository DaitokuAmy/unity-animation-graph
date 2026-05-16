using System;

namespace UnityAnimationGraph {
    /// <summary>
    /// Vector2 Tween で更新しない要素
    /// </summary>
    [Flags]
    public enum Vector2IgnoreMask {
        /// <summary>すべて更新</summary>
        None = 0,
        /// <summary>X を更新しない</summary>
        X = 1 << 0,
        /// <summary>Y を更新しない</summary>
        Y = 1 << 1,
    }

    /// <summary>
    /// Vector3 Tween で更新しない要素
    /// </summary>
    [Flags]
    public enum Vector3IgnoreMask {
        /// <summary>すべて更新</summary>
        None = 0,
        /// <summary>X を更新しない</summary>
        X = 1 << 0,
        /// <summary>Y を更新しない</summary>
        Y = 1 << 1,
        /// <summary>Z を更新しない</summary>
        Z = 1 << 2,
    }

    /// <summary>
    /// Color Tween で更新しない要素
    /// </summary>
    [Flags]
    public enum ColorIgnoreMask {
        /// <summary>すべて更新</summary>
        None = 0,
        /// <summary>R を更新しない</summary>
        R = 1 << 0,
        /// <summary>G を更新しない</summary>
        G = 1 << 1,
        /// <summary>B を更新しない</summary>
        B = 1 << 2,
        /// <summary>A を更新しない</summary>
        A = 1 << 3,
    }
}
