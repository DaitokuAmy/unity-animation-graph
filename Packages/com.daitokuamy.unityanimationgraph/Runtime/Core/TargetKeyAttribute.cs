using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// target key を AnimationGraphAsset の target schema から選択するための属性
    /// </summary>
    public sealed class TargetKeyAttribute : PropertyAttribute {
        /// <summary>選択可能な target multiplicity</summary>
        public TargetMultiplicity Multiplicity { get; }

        /// <summary>
        /// TargetKeyAttribute を生成
        /// </summary>
        public TargetKeyAttribute() : this(TargetMultiplicity.Single) {
        }

        /// <summary>
        /// TargetKeyAttribute を生成
        /// </summary>
        /// <param name="multiplicity">選択可能な target multiplicity</param>
        public TargetKeyAttribute(TargetMultiplicity multiplicity) {
            Multiplicity = multiplicity;
        }
    }
}
