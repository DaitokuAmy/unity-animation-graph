namespace UnityAnimationGraph {
    /// <summary>
    /// Renderer の Material texture scale を Tween するノード
    /// </summary>
    [NodeInfo("Tween Renderer Material Texture Scale", "Built-in/Tween/Renderer Material/Texture Scale")]
    public sealed class TweenRendererMaterialTextureScaleNode : TweenRendererMaterialTextureTransformNode {
        /// <inheritdoc/>
        protected override bool IsScale => true;
    }
}
