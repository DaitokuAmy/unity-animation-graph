using UnityEngine;
using UnityEngine.Rendering;

namespace UnityAnimationGraph {
    /// <summary>
    /// Renderer の Material texture transform を Tween する ActionNode の基底クラス
    /// </summary>
    public abstract class TweenRendererMaterialTextureTransformNode : TweenRendererMaterialNode<Vector2Tween, Vector2> {
        [SerializeField, Tooltip("Texture transform Tween 設定")]
        private Vector2Tween _tween = new(Vector2.zero, Vector2.one);

        /// <inheritdoc/>
        protected override Vector2Tween TweenSettings => _tween;

        /// <summary>Texture scale を操作する場合は true</summary>
        protected abstract bool IsScale { get; }

        /// <inheritdoc/>
        protected override bool IsCompatiblePropertyType(ShaderPropertyType propertyType) {
            return propertyType == ShaderPropertyType.Texture;
        }

        /// <inheritdoc/>
        protected override Vector2 GetValue(Material material, int propertyId) {
            return IsScale ? material.GetTextureScale(ShaderPropertyName) : material.GetTextureOffset(ShaderPropertyName);
        }

        /// <inheritdoc/>
        protected override Vector2 GetPreviewValue(Material material, MaterialPropertyBlock propertyBlock, int propertyId) {
            var transformPropertyId = Shader.PropertyToID($"{ShaderPropertyName}_ST");
            if (!propertyBlock.HasVector(transformPropertyId)) {
                return GetValue(material, propertyId);
            }

            var transform = propertyBlock.GetVector(transformPropertyId);
            return IsScale ? new Vector2(transform.x, transform.y) : new Vector2(transform.z, transform.w);
        }

        /// <inheritdoc/>
        protected override bool TryEvaluateValue(Vector2Tween tweenSettings, IAnimationGraphBlackboard blackboard, Vector2 baseValue, float localTime, float calculatedDuration, out Vector2 value) {
            return tweenSettings.TryEvaluate(blackboard, baseValue, localTime, calculatedDuration, out value);
        }

        /// <inheritdoc/>
        protected override void ApplyValue(Material material, MaterialPropertyBlock propertyBlock, int propertyId, Vector2 value, bool isPreview) {
            var transformPropertyId = Shader.PropertyToID($"{ShaderPropertyName}_ST");
            var hasPreviewTransform = isPreview && propertyBlock.HasVector(transformPropertyId);
            var previewTransform = hasPreviewTransform ? propertyBlock.GetVector(transformPropertyId) : default;
            var currentValue = hasPreviewTransform
                ? IsScale ? new Vector2(previewTransform.x, previewTransform.y) : new Vector2(previewTransform.z, previewTransform.w)
                : GetValue(material, propertyId);
            value = _tween.ApplyIgnoreMask(value, currentValue);
            if (!isPreview) {
                if (IsScale) {
                    material.SetTextureScale(ShaderPropertyName, value);
                }
                else {
                    material.SetTextureOffset(ShaderPropertyName, value);
                }

                return;
            }

            var scale = IsScale
                ? value
                : hasPreviewTransform ? new Vector2(previewTransform.x, previewTransform.y) : material.GetTextureScale(ShaderPropertyName);
            var offset = IsScale
                ? hasPreviewTransform ? new Vector2(previewTransform.z, previewTransform.w) : material.GetTextureOffset(ShaderPropertyName)
                : value;
            propertyBlock.SetVector(transformPropertyId, new Vector4(scale.x, scale.y, offset.x, offset.y));
        }
    }
}
