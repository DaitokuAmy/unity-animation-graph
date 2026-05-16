using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// EaseType に対応する補間値を計算するユーティリティ
    /// </summary>
    public static class Easing {
        /// <summary>
        /// start から end までの補間値を取得
        /// </summary>
        /// <param name="easeType">補間カーブ種別</param>
        /// <param name="start">開始値</param>
        /// <param name="end">終了値</param>
        /// <param name="ratio">補間率</param>
        /// <returns>補間後の値</returns>
        public static float Evaluate(EaseType easeType, float start, float end, float ratio) {
            return Mathf.LerpUnclamped(start, end, Evaluate(easeType, ratio));
        }

        /// <summary>
        /// 0 から 1 までの補間率を取得
        /// </summary>
        /// <param name="easeType">補間カーブ種別</param>
        /// <param name="ratio">補間率</param>
        /// <returns>補間後の補間率</returns>
        public static float Evaluate(EaseType easeType, float ratio) {
            switch (easeType) {
                case EaseType.Linear:
                    return ratio;
                case EaseType.EaseInQuad:
                    return ratio * ratio;
                case EaseType.EaseOutQuad:
                    return 1.0f - (1.0f - ratio) * (1.0f - ratio);
                case EaseType.EaseInOutQuad:
                    return ratio < 0.5f ? 2.0f * ratio * ratio : 1.0f - Mathf.Pow(-2.0f * ratio + 2.0f, 2.0f) * 0.5f;
                case EaseType.EaseInCubic:
                    return ratio * ratio * ratio;
                case EaseType.EaseOutCubic:
                    return 1.0f - Mathf.Pow(1.0f - ratio, 3.0f);
                case EaseType.EaseInOutCubic:
                    return ratio < 0.5f ? 4.0f * ratio * ratio * ratio : 1.0f - Mathf.Pow(-2.0f * ratio + 2.0f, 3.0f) * 0.5f;
                case EaseType.EaseInQuart:
                    return ratio * ratio * ratio * ratio;
                case EaseType.EaseOutQuart:
                    return 1.0f - Mathf.Pow(1.0f - ratio, 4.0f);
                case EaseType.EaseInOutQuart:
                    return ratio < 0.5f ? 8.0f * ratio * ratio * ratio * ratio : 1.0f - Mathf.Pow(-2.0f * ratio + 2.0f, 4.0f) * 0.5f;
                case EaseType.EaseInQuint:
                    return ratio * ratio * ratio * ratio * ratio;
                case EaseType.EaseOutQuint:
                    return 1.0f - Mathf.Pow(1.0f - ratio, 5.0f);
                case EaseType.EaseInOutQuint:
                    return ratio < 0.5f ? 16.0f * ratio * ratio * ratio * ratio * ratio : 1.0f - Mathf.Pow(-2.0f * ratio + 2.0f, 5.0f) * 0.5f;
                case EaseType.EaseInSine:
                    return 1.0f - Mathf.Cos(ratio * Mathf.PI * 0.5f);
                case EaseType.EaseOutSine:
                    return Mathf.Sin(ratio * Mathf.PI * 0.5f);
                case EaseType.EaseInOutSine:
                    return -(Mathf.Cos(Mathf.PI * ratio) - 1.0f) * 0.5f;
                case EaseType.EaseInExpo:
                    return ratio <= 0.0f ? 0.0f : Mathf.Pow(2.0f, 10.0f * ratio - 10.0f);
                case EaseType.EaseOutExpo:
                    return ratio >= 1.0f ? 1.0f : 1.0f - Mathf.Pow(2.0f, -10.0f * ratio);
                case EaseType.EaseInOutExpo:
                    return EaseInOutExpo(ratio);
                case EaseType.EaseInCirc:
                    return 1.0f - Mathf.Sqrt(1.0f - ratio * ratio);
                case EaseType.EaseOutCirc:
                    return Mathf.Sqrt(1.0f - Mathf.Pow(ratio - 1.0f, 2.0f));
                case EaseType.EaseInOutCirc:
                    return EaseInOutCirc(ratio);
                case EaseType.EaseInBounce:
                    return 1.0f - EaseOutBounce(1.0f - ratio);
                case EaseType.EaseOutBounce:
                    return EaseOutBounce(ratio);
                case EaseType.EaseInOutBounce:
                    return ratio < 0.5f ? (1.0f - EaseOutBounce(1.0f - 2.0f * ratio)) * 0.5f : (1.0f + EaseOutBounce(2.0f * ratio - 1.0f)) * 0.5f;
                case EaseType.EaseInBack:
                    return EaseInBack(ratio);
                case EaseType.EaseOutBack:
                    return EaseOutBack(ratio);
                case EaseType.EaseInOutBack:
                    return EaseInOutBack(ratio);
                case EaseType.EaseInElastic:
                    return EaseInElastic(ratio);
                case EaseType.EaseOutElastic:
                    return EaseOutElastic(ratio);
                case EaseType.EaseInOutElastic:
                    return EaseInOutElastic(ratio);
                case EaseType.Spring:
                    return Spring(ratio);
                case EaseType.Punch:
                    return Punch(ratio);
                default:
                    return ratio;
            }
        }

        /// <summary>
        /// EaseInOutExpo の補間率を評価
        /// </summary>
        /// <param name="ratio">0 から 1 の補間率</param>
        /// <returns>評価後の補間率</returns>
        private static float EaseInOutExpo(float ratio) {
            if (ratio <= 0.0f) {
                return 0.0f;
            }

            if (ratio >= 1.0f) {
                return 1.0f;
            }

            return ratio < 0.5f
                ? Mathf.Pow(2.0f, 20.0f * ratio - 10.0f) * 0.5f
                : (2.0f - Mathf.Pow(2.0f, -20.0f * ratio + 10.0f)) * 0.5f;
        }

        /// <summary>
        /// EaseInOutCirc の補間率を評価
        /// </summary>
        /// <param name="ratio">0 から 1 の補間率</param>
        /// <returns>評価後の補間率</returns>
        private static float EaseInOutCirc(float ratio) {
            return ratio < 0.5f
                ? (1.0f - Mathf.Sqrt(1.0f - Mathf.Pow(2.0f * ratio, 2.0f))) * 0.5f
                : (Mathf.Sqrt(1.0f - Mathf.Pow(-2.0f * ratio + 2.0f, 2.0f)) + 1.0f) * 0.5f;
        }

        /// <summary>
        /// EaseOutBounce の補間率を評価
        /// </summary>
        /// <param name="ratio">0 から 1 の補間率</param>
        /// <returns>評価後の補間率</returns>
        private static float EaseOutBounce(float ratio) {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (ratio < 1.0f / d1) {
                return n1 * ratio * ratio;
            }

            if (ratio < 2.0f / d1) {
                ratio -= 1.5f / d1;
                return n1 * ratio * ratio + 0.75f;
            }

            if (ratio < 2.5f / d1) {
                ratio -= 2.25f / d1;
                return n1 * ratio * ratio + 0.9375f;
            }

            ratio -= 2.625f / d1;
            return n1 * ratio * ratio + 0.984375f;
        }

        /// <summary>
        /// EaseInBack の補間率を評価
        /// </summary>
        /// <param name="ratio">0 から 1 の補間率</param>
        /// <returns>評価後の補間率</returns>
        private static float EaseInBack(float ratio) {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1.0f;

            return c3 * ratio * ratio * ratio - c1 * ratio * ratio;
        }

        /// <summary>
        /// EaseOutBack の補間率を評価
        /// </summary>
        /// <param name="ratio">0 から 1 の補間率</param>
        /// <returns>評価後の補間率</returns>
        private static float EaseOutBack(float ratio) {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1.0f;

            return 1.0f + c3 * Mathf.Pow(ratio - 1.0f, 3.0f) + c1 * Mathf.Pow(ratio - 1.0f, 2.0f);
        }

        /// <summary>
        /// EaseInOutBack の補間率を評価
        /// </summary>
        /// <param name="ratio">0 から 1 の補間率</param>
        /// <returns>評価後の補間率</returns>
        private static float EaseInOutBack(float ratio) {
            const float c1 = 1.70158f;
            const float c2 = c1 * 1.525f;

            return ratio < 0.5f
                ? Mathf.Pow(2.0f * ratio, 2.0f) * ((c2 + 1.0f) * 2.0f * ratio - c2) * 0.5f
                : (Mathf.Pow(2.0f * ratio - 2.0f, 2.0f) * ((c2 + 1.0f) * (ratio * 2.0f - 2.0f) + c2) + 2.0f) * 0.5f;
        }

        /// <summary>
        /// EaseInElastic の補間率を評価
        /// </summary>
        /// <param name="ratio">0 から 1 の補間率</param>
        /// <returns>評価後の補間率</returns>
        private static float EaseInElastic(float ratio) {
            if (ratio <= 0.0f) {
                return 0.0f;
            }

            if (ratio >= 1.0f) {
                return 1.0f;
            }

            const float c4 = 2.0f * Mathf.PI / 3.0f;
            return -Mathf.Pow(2.0f, 10.0f * ratio - 10.0f) * Mathf.Sin((ratio * 10.0f - 10.75f) * c4);
        }

        /// <summary>
        /// EaseOutElastic の補間率を評価
        /// </summary>
        /// <param name="ratio">0 から 1 の補間率</param>
        /// <returns>評価後の補間率</returns>
        private static float EaseOutElastic(float ratio) {
            if (ratio <= 0.0f) {
                return 0.0f;
            }

            if (ratio >= 1.0f) {
                return 1.0f;
            }

            const float c4 = 2.0f * Mathf.PI / 3.0f;
            return Mathf.Pow(2.0f, -10.0f * ratio) * Mathf.Sin((ratio * 10.0f - 0.75f) * c4) + 1.0f;
        }

        /// <summary>
        /// EaseInOutElastic の補間率を評価
        /// </summary>
        /// <param name="ratio">0 から 1 の補間率</param>
        /// <returns>評価後の補間率</returns>
        private static float EaseInOutElastic(float ratio) {
            if (ratio <= 0.0f) {
                return 0.0f;
            }

            if (ratio >= 1.0f) {
                return 1.0f;
            }

            const float c5 = 2.0f * Mathf.PI / 4.5f;
            return ratio < 0.5f
                ? -(Mathf.Pow(2.0f, 20.0f * ratio - 10.0f) * Mathf.Sin((20.0f * ratio - 11.125f) * c5)) * 0.5f
                : Mathf.Pow(2.0f, -20.0f * ratio + 10.0f) * Mathf.Sin((20.0f * ratio - 11.125f) * c5) * 0.5f + 1.0f;
        }

        /// <summary>
        /// Spring の補間率を評価
        /// </summary>
        /// <param name="ratio">0 から 1 の補間率</param>
        /// <returns>評価後の補間率</returns>
        private static float Spring(float ratio) {
            if (ratio <= 0.0f) {
                return 0.0f;
            }

            if (ratio >= 1.0f) {
                return 1.0f;
            }

            return 1.0f - Mathf.Exp(-6.0f * ratio) * Mathf.Cos(12.0f * ratio);
        }

        /// <summary>
        /// Punch の補間率を評価
        /// </summary>
        /// <param name="ratio">0 から 1 の補間率</param>
        /// <returns>評価後の補間率</returns>
        private static float Punch(float ratio) {
            if (ratio <= 0.0f || ratio >= 1.0f) {
                return Mathf.Clamp01(ratio);
            }

            return ratio + Mathf.Sin(ratio * Mathf.PI * 6.0f) * Mathf.Pow(1.0f - ratio, 2.0f) * 0.3f;
        }
    }
}
