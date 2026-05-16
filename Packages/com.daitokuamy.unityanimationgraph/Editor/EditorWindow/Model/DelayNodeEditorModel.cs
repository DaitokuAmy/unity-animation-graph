namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// NodeEditorModel that provides editing information for DelayNode.
    /// </summary>
    public sealed class DelayNodeEditorModel : NodeEditorModel {
        private readonly DelayNode _delayNode;

        /// <summary>Delay before continuing to next nodes.</summary>
        public float Delay => _delayNode.Delay;

        /// <summary>
        /// Creates a DelayNodeEditorModel.
        /// </summary>
        /// <param name="node">Referenced DelayNode</param>
        internal DelayNodeEditorModel(DelayNode node) : base(node) {
            _delayNode = node;
        }

        /// <summary>
        /// Sets the delay before continuing to next nodes.
        /// </summary>
        /// <param name="delay">Delay to set</param>
        internal void SetDelay(float delay) {
            AnimationGraphAssetUtility.SetDelayNodeDelay(_delayNode, delay);
        }
    }
}
