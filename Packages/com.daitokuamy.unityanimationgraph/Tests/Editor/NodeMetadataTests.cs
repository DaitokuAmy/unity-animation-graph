using NUnit.Framework;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// NodeMetadata の EditMode テスト
    /// </summary>
    public sealed class NodeMetadataTests {
        /// <summary>
        /// 表示名と作成メニューパスを指定したテスト用 action node
        /// </summary>
        [NodeInfo("表示名", "Samples/表示名")]
        private sealed class CustomMenuNode : ActionNode {
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
        /// 表示名のみ指定したテスト用 action node
        /// </summary>
        [NodeInfo("表示名のみ")]
        private sealed class DisplayNameOnlyNode : ActionNode {
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
        /// NodeInfo を指定しないテスト用 action node
        /// </summary>
        private sealed class DefaultMenuNode : ActionNode {
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
        /// Control category の作成メニュー確認用 control node
        /// </summary>
        [NodeInfo("制御表示名", "Flow/制御表示名")]
        private sealed class ControlMenuNode : ControlNode {
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
        /// Attribute の表示名を取得できる
        /// </summary>
        [Test]
        public void GetDisplayName_ReturnsAttributeDisplayName() {
            var displayName = NodeMetadata.GetDisplayName(typeof(CustomMenuNode));

            Assert.That(displayName, Is.EqualTo("表示名"));
        }

        /// <summary>
        /// Attribute がない場合は型名から表示名を取得する
        /// </summary>
        [Test]
        public void GetDisplayName_FallsBackToNicifiedTypeNameWithoutNodeSuffix() {
            var displayName = NodeMetadata.GetDisplayName(typeof(DefaultMenuNode));

            Assert.That(displayName, Is.EqualTo("Default Menu"));
        }

        /// <summary>
        /// Attribute の作成メニューパスを取得できる
        /// </summary>
        [Test]
        public void GetCreateMenuPath_ReturnsAttributeMenuPath() {
            var createMenuPath = NodeMetadata.GetCreateMenuPath(typeof(CustomMenuNode));

            Assert.That(createMenuPath, Is.EqualTo("Action/Samples/表示名"));
        }

        /// <summary>
        /// 作成メニューパス未指定の場合は node category と表示名を取得する
        /// </summary>
        [Test]
        public void GetCreateMenuPath_FallsBackToDisplayName() {
            var createMenuPath = NodeMetadata.GetCreateMenuPath(typeof(DisplayNameOnlyNode));

            Assert.That(createMenuPath, Is.EqualTo("Action/表示名のみ"));
        }

        /// <summary>
        /// 作成メニューパス未指定の場合は型名由来の表示名を取得する
        /// </summary>
        [Test]
        public void GetCreateMenuPath_FallsBackToNicifiedTypeNameWithoutNodeSuffix() {
            var createMenuPath = NodeMetadata.GetCreateMenuPath(typeof(DefaultMenuNode));

            Assert.That(createMenuPath, Is.EqualTo("Action/Default Menu"));
        }

        /// <summary>
        /// ControlNode は Control category 以下の作成メニューパスを取得する
        /// </summary>
        [Test]
        public void GetCreateMenuPath_ReturnsControlCategoryPath() {
            var createMenuPath = NodeMetadata.GetCreateMenuPath(typeof(ControlMenuNode));

            Assert.That(createMenuPath, Is.EqualTo("Control/Flow/制御表示名"));
        }
    }
}
