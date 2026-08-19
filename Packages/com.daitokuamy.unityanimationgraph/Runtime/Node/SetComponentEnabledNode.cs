using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Target Component の enabled を設定するノード
    /// </summary>
    [NodeInfo("Set Component Enabled", "Built-in/State/Component Enabled")]
    public sealed class SetComponentEnabledNode : ActionNode<Component> {
        private const string EnabledPropertyName = "enabled";

        [SerializeField, Tooltip("Component enabled に設定する値")]
        private bool _enabled = true;

        /// <inheritdoc/>
        protected override IEnumerable<string> GetPreviewProperties(Component target) {
            if (HasEnabledProperty(target)) {
                yield return PreviewPropertyPaths.Component.Enabled;
            }
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, Component target, float localTime, float calculatedDuration, IAnimationGraphBlackboard blackboard, IActionNodeState state) {
            if (!TryGetEnabledProperty(target, out var enabledProperty)) {
                return;
            }

            enabledProperty.SetValue(target, _enabled);
        }

        private static bool HasEnabledProperty(Component target) {
            return TryGetEnabledProperty(target, out _);
        }

        private static bool TryGetEnabledProperty(Component target, out PropertyInfo enabledProperty) {
            enabledProperty = null;
            if (target == null) {
                return false;
            }

            var property = target.GetType().GetProperty(EnabledPropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || property.PropertyType != typeof(bool) || !property.CanRead || !property.CanWrite) {
                return false;
            }

            enabledProperty = property;
            return true;
        }
    }
}
