using NUnit.Framework;
using UnityEngine;
using UnityAnimationGraph.Editor;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// Node validation hook の EditMode テスト
    /// </summary>
    public sealed class NodeValidationTests {
        /// <summary>
        /// AnimationGraphAssetEditorModel は Node の validation hook の結果を返す
        /// </summary>
        [Test]
        public void GetNodeValidationMessages_IncludesNodeValidationHookMessage() {
            using var builder = new AnimationGraphTestBuilder();
            var node = builder.CreateNode<ValidatingNode>("node");
            node.Message = "Custom validation";
            var graphAsset = builder.CreateGraph(node.NodeId, node);
            var model = new AnimationGraphAssetEditorModel();
            model.SetGraphAsset(graphAsset);

            var validationMessages = model.GetNodeValidationMessages();

            Assert.IsTrue(validationMessages.ContainsKey(node.NodeId));
            Assert.That(validationMessages[node.NodeId], Is.EqualTo("Custom validation"));
        }

        private sealed class ValidatingNode : Node {
            /// <summary>Validation hook が返すメッセージ</summary>
            public string Message { get; set; }

            protected override float CalculateDuration(int seed, IAnimationGraphContext context) {
                return 0.0f;
            }

            protected override float CalculateDelay(int seed, IAnimationGraphContext context) {
                return 0.0f;
            }

            protected override void Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context) {
            }

            protected override string Validate(NodeValidationContext context) {
                return Message;
            }
        }
    }
}
