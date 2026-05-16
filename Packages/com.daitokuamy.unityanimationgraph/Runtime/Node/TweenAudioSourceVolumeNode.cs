using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// AudioSource の volume を Tween するノード
    /// </summary>
    [NodeInfo("Tween Audio Source Volume", "Built-in/Tween/Audio Source/Volume")]
    public sealed class TweenAudioSourceVolumeNode : TweenFloatNode<AudioSource> {
        [SerializeField, Tooltip("Volume Tween 設定")]
        private FloatTween _tween = new(0.0f, 1.0f);

        /// <summary>Volume Tween 設定</summary>
        public FloatTween Tween => _tween;
        /// <inheritdoc/>
        protected override FloatTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override void ApplyValue(AudioSource target, float value) {
            target.volume = value;
        }
    }
}
