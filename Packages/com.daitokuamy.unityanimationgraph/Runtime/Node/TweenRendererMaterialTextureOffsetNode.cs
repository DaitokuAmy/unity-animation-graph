namespace UnityAnimationGraph {
    /// <summary>
    /// Renderer の Material texture offset を Tween するノード
    /// </summary>
    [NodeInfo("Tween Renderer Material Texture Offset", "Built-in/Tween/Renderer Material/Texture Offset")]
    public sealed class TweenRendererMaterialTextureOffsetNode : TweenRendererMaterialTextureTransformNode {
        /// <inheritdoc/>
        protected override bool IsScale => false;
    }
}
