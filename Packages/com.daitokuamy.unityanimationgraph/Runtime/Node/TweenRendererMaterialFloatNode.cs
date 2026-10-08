using UnityEngine;
using UnityEngine.Rendering;

namespace UnityAnimationGraph {
    /// <summary>
    /// Renderer の Material float property を Tween するノード
    /// </summary>
    [NodeInfo("Tween Renderer Material Float", "Built-in/Tween/Renderer Material/Float")]
    public sealed class TweenRendererMaterialFloatNode : TweenRendererMaterialNode<FloatTween, float> {
        [SerializeField, Tooltip("Float Tween 設定")]
        private FloatTween _tween = new(0.0f, 1.0f);

        /// <inheritdoc/>
        protected override FloatTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override bool IsCompatiblePropertyType(ShaderPropertyType propertyType) {
            return propertyType == ShaderPropertyType.Float || propertyType == ShaderPropertyType.Range;
        }

        /// <inheritdoc/>
        protected override float GetValue(Material material, int propertyId) {
            return material.GetFloat(propertyId);
        }

        /// <inheritdoc/>
        protected override float GetPreviewValue(Material material, MaterialPropertyBlock propertyBlock, int propertyId) {
            return propertyBlock.HasFloat(propertyId) ? propertyBlock.GetFloat(propertyId) : GetValue(material, propertyId);
        }

        /// <inheritdoc/>
        protected override bool TryEvaluateValue(FloatTween tweenSettings, IAnimationGraphBlackboard blackboard, float baseValue, float localTime, float calculatedDuration, out float value) {
            return tweenSettings.TryEvaluate(blackboard, baseValue, localTime, calculatedDuration, out value);
        }

        /// <inheritdoc/>
        protected override void ApplyValue(Material material, MaterialPropertyBlock propertyBlock, int propertyId, float value, bool isPreview) {
            if (isPreview) {
                propertyBlock.SetFloat(propertyId, value);
                return;
            }

            material.SetFloat(propertyId, value);
        }
    }
}
