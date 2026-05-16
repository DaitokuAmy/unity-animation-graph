using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// AudioSource の pitch を Tween するノード
    /// </summary>
    [AnimationGraphNode("Tween Audio Source Pitch", "Built-in/Tween/Audio Source/Pitch")]
    public sealed class TweenAudioSourcePitchNode : TweenFloatNode<AudioSource> {
        [SerializeField, Tooltip("Pitch Tween 設定")]
        private FloatTween _tween = new(1.0f, 1.2f);

        /// <summary>Pitch Tween 設定</summary>
        public FloatTween Tween => _tween;
        /// <inheritdoc/>
        protected override FloatTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override void ApplyValue(AudioSource target, float value) {
            target.pitch = value;
        }
    }
}
