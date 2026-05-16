using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Attribute for selecting a blackboard key from an AnimationGraphAsset definition list.
    /// </summary>
    public sealed class BlackboardKeyAttribute : PropertyAttribute {
        /// <summary>Whether this selector filters keys by value type.</summary>
        public bool HasValueTypeFilter { get; }
        /// <summary>Blackboard value type used for filtering.</summary>
        public AnimationGraphValueType ValueType { get; }

        /// <summary>
        /// Creates an attribute that allows selecting any blackboard key.
        /// </summary>
        public BlackboardKeyAttribute() {
            HasValueTypeFilter = false;
            ValueType = default;
        }

        /// <summary>
        /// Creates an attribute that allows selecting a blackboard key with the specified value type.
        /// </summary>
        /// <param name="valueType">Blackboard value type to select</param>
        public BlackboardKeyAttribute(AnimationGraphValueType valueType) {
            HasValueTypeFilter = true;
            ValueType = valueType;
        }
    }
}
