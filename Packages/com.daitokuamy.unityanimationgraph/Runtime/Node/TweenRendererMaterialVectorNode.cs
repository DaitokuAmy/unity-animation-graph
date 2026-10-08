using UnityEngine;
using UnityEngine.Rendering;

namespace UnityAnimationGraph {
    /// <summary>
    /// Renderer の Material vector property を Tween するノード
    /// </summary>
    [NodeInfo("Tween Renderer Material Vector", "Built-in/Tween/Renderer Material/Vector")]
    public sealed class TweenRendererMaterialVectorNode : TweenRendererMaterialNode<Vector4Tween, Vector4> {
        [SerializeField, Tooltip("Vector Tween 設定")]
        private Vector4Tween _tween = new(Vector4.zero, Vector4.one);

        /// <inheritdoc/>
        protected override Vector4Tween TweenSettings => _tween;

        /// <inheritdoc/>
        protected override bool IsCompatiblePropertyType(ShaderPropertyType propertyType) {
            return propertyType == ShaderPropertyType.Vector;
        }

        /// <inheritdoc/>
        protected override Vector4 GetValue(Material material, int propertyId) {
            return material.GetVector(propertyId);
        }

        /// <inheritdoc/>
        protected override Vector4 GetPreviewValue(Material material, MaterialPropertyBlock propertyBlock, int propertyId) {
            return propertyBlock.HasVector(propertyId) ? propertyBlock.GetVector(propertyId) : GetValue(material, propertyId);
        }

        /// <inheritdoc/>
        protected override bool TryEvaluateValue(Vector4Tween tweenSettings, IAnimationGraphBlackboard blackboard, Vector4 baseValue, float localTime, float calculatedDuration, out Vector4 value) {
            return tweenSettings.TryEvaluate(blackboard, baseValue, localTime, calculatedDuration, out value);
        }

        /// <inheritdoc/>
        protected override void ApplyValue(Material material, MaterialPropertyBlock propertyBlock, int propertyId, Vector4 value, bool isPreview) {
            var currentValue = isPreview && propertyBlock.HasVector(propertyId)
                ? propertyBlock.GetVector(propertyId)
                : material.GetVector(propertyId);
            value = _tween.ApplyIgnoreMask(value, currentValue);
            if (isPreview) {
                propertyBlock.SetVector(propertyId, value);
                return;
            }

            material.SetVector(propertyId, value);
        }
    }
}
