using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// Node / Signal の表示名上書きに関する EditMode テスト
    /// </summary>
    public sealed class DisplayNameOverrideTests {
        private const string DisplayNamePropertyName = "_displayName";

        /// <summary>
        /// 表示名のみ指定したテスト用 action node
        /// </summary>
        [NodeInfo("既定ノード名")]
        private sealed class DefaultDisplayNameNode : ActionNode {
            /// <inheritdoc/>
            protected override float CalculateDuration(int seed, IAnimationGraphContext context) {
                return 0.0f;
            }

            /// <inheritdoc/>
            protected override float CalculateDelay(int seed, IAnimationGraphContext context) {
                return 0.0f;
            }

            /// <inheritdoc/>
            protected override void Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context) {
            }
        }

        /// <summary>
        /// 表示名を指定したテスト用 signal
        /// </summary>
        [SignalInfo("既定シグナル名")]
        private sealed class DefaultDisplayNameSignal : Signal {
        }

        /// <summary>
        /// Node の DisplayName が空の場合は metadata の表示名を返す
        /// </summary>
        [Test]
        public void NodeDisplayName_FallsBackToMetadataWhenEmpty() {
            var node = ScriptableObject.CreateInstance<DefaultDisplayNameNode>();
            try {
                Assert.That(node.DisplayName, Is.EqualTo("既定ノード名"));
            }
            finally {
                Object.DestroyImmediate(node);
            }
        }

        /// <summary>
        /// Node の DisplayName が設定済みの場合は入力値を返す
        /// </summary>
        [Test]
        public void NodeDisplayName_ReturnsSerializedDisplayNameWhenSet() {
            var node = ScriptableObject.CreateInstance<DefaultDisplayNameNode>();
            try {
                SetDisplayName(node, "Custom Node");

                Assert.That(node.DisplayName, Is.EqualTo("Custom Node"));
            }
            finally {
                Object.DestroyImmediate(node);
            }
        }

        /// <summary>
        /// Signal の DisplayName が空の場合は metadata の表示名を返す
        /// </summary>
        [Test]
        public void SignalDisplayName_FallsBackToMetadataWhenEmpty() {
            var signal = ScriptableObject.CreateInstance<DefaultDisplayNameSignal>();
            try {
                Assert.That(signal.DisplayName, Is.EqualTo("既定シグナル名"));
            }
            finally {
                Object.DestroyImmediate(signal);
            }
        }

        /// <summary>
        /// Signal の DisplayName が設定済みの場合は入力値を返す
        /// </summary>
        [Test]
        public void SignalDisplayName_ReturnsSerializedDisplayNameWhenSet() {
            var signal = ScriptableObject.CreateInstance<DefaultDisplayNameSignal>();
            try {
                SetDisplayName(signal, "Custom Signal");

                Assert.That(signal.DisplayName, Is.EqualTo("Custom Signal"));
            }
            finally {
                Object.DestroyImmediate(signal);
            }
        }

        private static void SetDisplayName(Object target, string displayName) {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(DisplayNamePropertyName).stringValue = displayName;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
