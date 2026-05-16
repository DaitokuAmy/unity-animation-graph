using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Attribute for selecting a blackboard key from an AnimationGraphAsset definition list.
    /// </summary>
    public sealed class AnimationGraphBlackboardKeyAttribute : PropertyAttribute {
        /// <summary>Whether this selector filters keys by value type.</summary>
        public bool HasValueTypeFilter { get; }
        /// <summary>Blackboard value type used for filtering.</summary>
        public AnimationGraphValueType ValueType { get; }

        /// <summary>
        /// Creates an attribute that allows selecting any blackboard key.
        /// </summary>
        public AnimationGraphBlackboardKeyAttribute() {
            HasValueTypeFilter = false;
            ValueType = default;
        }

        /// <summary>
        /// Creates an attribute that allows selecting a blackboard key with the specified value type.
        /// </summary>
        /// <param name="valueType">Blackboard value type to select</param>
        public AnimationGraphBlackboardKeyAttribute(AnimationGraphValueType valueType) {
            HasValueTypeFilter = true;
            ValueType = valueType;
        }
    }
}
