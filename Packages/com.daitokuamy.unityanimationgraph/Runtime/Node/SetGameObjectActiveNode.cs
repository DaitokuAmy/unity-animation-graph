using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Target の GameObject activeSelf を設定するノード
    /// </summary>
    [NodeInfo("Set GameObject Active", "Built-in/State/GameObject Active")]
    public sealed class SetGameObjectActiveNode : ActionNode<Component> {
        [SerializeField, Tooltip("GameObject activeSelf に設定する値")]
        private bool _active = true;

        /// <inheritdoc/>
        protected override IEnumerable<(Object Target, string PropertyPath)> GetPreviewObjectProperties(Component target) {
            yield return (target.gameObject, PreviewPropertyPaths.GameObject.ActiveSelf);
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, Component target, float localTime, float calculatedDuration, IAnimationGraphBlackboard blackboard) {
            target.gameObject.SetActive(_active);
        }
    }
}
