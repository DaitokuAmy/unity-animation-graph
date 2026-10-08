using UnityEngine;
using UnityEngine.Rendering;

namespace UnityAnimationGraph {
    /// <summary>
    /// Renderer の Material color property を Tween するノード
    /// </summary>
    [NodeInfo("Tween Renderer Material Color", "Built-in/Tween/Renderer Material/Color")]
    public sealed class TweenRendererMaterialColorNode : TweenRendererMaterialNode<ColorTween, Color> {
        [SerializeField, Tooltip("Color Tween 設定")]
        private ColorTween _tween = new(Color.white, Color.red);

        /// <inheritdoc/>
        protected override ColorTween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override bool IsCompatiblePropertyType(ShaderPropertyType propertyType) {
            return propertyType == ShaderPropertyType.Color;
        }

        /// <inheritdoc/>
        protected override Color GetValue(Material material, int propertyId) {
            return material.GetColor(propertyId);
        }

        /// <inheritdoc/>
        protected override Color GetPreviewValue(Material material, MaterialPropertyBlock propertyBlock, int propertyId) {
            return propertyBlock.HasColor(propertyId) ? propertyBlock.GetColor(propertyId) : GetValue(material, propertyId);
        }

        /// <inheritdoc/>
        protected override bool TryEvaluateValue(ColorTween tweenSettings, IAnimationGraphBlackboard blackboard, Color baseValue, float localTime, float calculatedDuration, out Color value) {
            return tweenSettings.TryEvaluate(blackboard, baseValue, localTime, calculatedDuration, out value);
        }

        /// <inheritdoc/>
        protected override void ApplyValue(Material material, MaterialPropertyBlock propertyBlock, int propertyId, Color value, bool isPreview) {
            var currentValue = isPreview && propertyBlock.HasColor(propertyId)
                ? propertyBlock.GetColor(propertyId)
                : material.GetColor(propertyId);
            value = _tween.ApplyIgnoreMask(value, currentValue);
            if (isPreview) {
                propertyBlock.SetColor(propertyId, value);
                return;
            }

            material.SetColor(propertyId, value);
        }
    }
}
