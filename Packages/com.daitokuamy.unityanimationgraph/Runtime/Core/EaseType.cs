namespace UnityAnimationGraph {
    /// <summary>
    /// Tween の補間カーブ種別
    /// </summary>
    public enum EaseType {
        /// <summary>線形補間</summary>
        Linear,
        /// <summary>加速する 2 次補間</summary>
        EaseInQuad,
        /// <summary>減速する 2 次補間</summary>
        EaseOutQuad,
        /// <summary>加減速する 2 次補間</summary>
        EaseInOutQuad,
        /// <summary>加速する 3 次補間</summary>
        EaseInCubic,
        /// <summary>減速する 3 次補間</summary>
        EaseOutCubic,
        /// <summary>加減速する 3 次補間</summary>
        EaseInOutCubic,
        /// <summary>加速する 4 次補間</summary>
        EaseInQuart,
        /// <summary>減速する 4 次補間</summary>
        EaseOutQuart,
        /// <summary>加減速する 4 次補間</summary>
        EaseInOutQuart,
        /// <summary>加速する 5 次補間</summary>
        EaseInQuint,
        /// <summary>減速する 5 次補間</summary>
        EaseOutQuint,
        /// <summary>加減速する 5 次補間</summary>
        EaseInOutQuint,
        /// <summary>加速する Sine 補間</summary>
        EaseInSine,
        /// <summary>減速する Sine 補間</summary>
        EaseOutSine,
        /// <summary>加減速する Sine 補間</summary>
        EaseInOutSine,
        /// <summary>加速する Expo 補間</summary>
        EaseInExpo,
        /// <summary>減速する Expo 補間</summary>
        EaseOutExpo,
        /// <summary>加減速する Expo 補間</summary>
        EaseInOutExpo,
        /// <summary>加速する Circ 補間</summary>
        EaseInCirc,
        /// <summary>減速する Circ 補間</summary>
        EaseOutCirc,
        /// <summary>加減速する Circ 補間</summary>
        EaseInOutCirc,
        /// <summary>加速する Bounce 補間</summary>
        EaseInBounce,
        /// <summary>減速する Bounce 補間</summary>
        EaseOutBounce,
        /// <summary>加減速する Bounce 補間</summary>
        EaseInOutBounce,
        /// <summary>加速する Back 補間</summary>
        EaseInBack,
        /// <summary>減速する Back 補間</summary>
        EaseOutBack,
        /// <summary>加減速する Back 補間</summary>
        EaseInOutBack,
        /// <summary>加速する Elastic 補間</summary>
        EaseInElastic,
        /// <summary>減速する Elastic 補間</summary>
        EaseOutElastic,
        /// <summary>加減速する Elastic 補間</summary>
        EaseInOutElastic,
        /// <summary>減衰ばね補間</summary>
        Spring,
        /// <summary>揺れを加える補間</summary>
        Punch,
    }
}
