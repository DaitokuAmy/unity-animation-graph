using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// AudioSource の pitch を Tween するノード
    /// </summary>
    [NodeInfo("Tween Audio Source Pitch", "Built-in/Tween/Audio Source/Pitch")]
    public sealed class TweenAudioSourcePitchNode : TweenFloatNode<AudioSource> {
        [SerializeField, Tooltip("Pitch Tween 設定")]
        private FloatTween _tween = new(1.0f, 1.2f);

        /// <inheritdoc/>
        protected override FloatTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override IEnumerable<string> GetPreviewProperties(AudioSource target) {
            yield return PreviewPropertyPaths.AudioSource.Pitch;
        }

        /// <inheritdoc/>
        protected override float GetBaseValue(AudioSource target) {
            return target.pitch;
        }

        /// <inheritdoc/>
        protected override void ApplyValue(AudioSource target, float value) {
            target.pitch = value;
        }
    }
}
