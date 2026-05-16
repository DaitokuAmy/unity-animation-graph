using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityAnimationGraph.Editor;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// AnimationGraphAssetEditorModel の EditMode テスト
    /// </summary>
    public sealed class AnimationGraphAssetEditorModelTests {
        private readonly List<string> _assetPaths = new();

        /// <summary>
        /// テスト後に作成した asset を削除
        /// </summary>
        [TearDown]
        public void TearDown() {
            for (var i = 0; i < _assetPaths.Count; i++) {
                AssetDatabase.DeleteAsset(_assetPaths[i]);
            }

            _assetPaths.Clear();
        }

        /// <summary>
        /// GraphAsset を設定すると Model から現在状態を参照できる
        /// </summary>
        [Test]
        public void SetGraphAsset_ExposesGraphAssetState() {
            var graphAsset = ScriptableObject.CreateInstance<AnimationGraphAsset>();

            try {
                var model = new AnimationGraphAssetEditorModel();
                Assert.IsFalse(model.HasGraphAsset);
                Assert.That(model.Nodes, Is.Empty);

                model.SetGraphAsset(graphAsset);

                Assert.IsTrue(model.HasGraphAsset);
                Assert.That(model.GraphAsset, Is.EqualTo(graphAsset));
                Assert.That(model.StartNodeId, Is.EqualTo(string.Empty));
                Assert.That(model.Nodes, Is.Empty);
            }
            finally {
                UnityEngine.Object.DestroyImmediate(graphAsset);
            }
        }

        /// <summary>
        /// GraphAsset を初期化すると StartNode を作成して参照できる
        /// </summary>
        [Test]
        public void InitializeGraph_CreatesStartNode() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            var startNodePosition = new Vector2(12.0f, 34.0f);
            model.SetGraphAsset(graphAsset);

            var startNode = model.InitializeGraph(startNodePosition);

            Assert.That(startNode.NodeType, Is.EqualTo(typeof(StartNode)));
            Assert.That(startNode.GraphPosition, Is.EqualTo(startNodePosition));
            Assert.That(model.StartNodeId, Is.EqualTo(startNode.NodeId));
            Assert.That(model.Nodes.Count, Is.EqualTo(1));
            Assert.IsTrue(model.TryGetNode(startNode.NodeId, out var foundNode));
            Assert.That(foundNode.NodeId, Is.EqualTo(startNode.NodeId));
            Assert.That(foundNode.NodeType, Is.EqualTo(typeof(StartNode)));
        }

        /// <summary>
        /// GraphAsset にノードを追加して取得できる
        /// </summary>
        [Test]
        public void AddNode_AddsNodeToGraphAsset() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            var nodePosition = new Vector2(56.0f, 78.0f);
            model.SetGraphAsset(graphAsset);
            model.InitializeGraph(Vector2.zero);

            var node = model.AddNode<DelayNode>(nodePosition);

            Assert.That(node.NodeType, Is.EqualTo(typeof(DelayNode)));
            Assert.That(node.GraphPosition, Is.EqualTo(nodePosition));
            Assert.That(model.Nodes.Count, Is.EqualTo(2));
            Assert.IsTrue(model.TryGetNode(node.NodeId, out var foundNode));
            Assert.That(foundNode.NodeId, Is.EqualTo(node.NodeId));
            Assert.That(foundNode.NodeType, Is.EqualTo(typeof(DelayNode)));
        }

        /// <summary>
        /// NodeEditorModel は同じ Node に対して同じインスタンスを返す
        /// </summary>
        [Test]
        public void Nodes_ReturnsCachedNodeModels() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            model.SetGraphAsset(graphAsset);

            var startNode = model.InitializeGraph(Vector2.zero);
            var startNodeFromNodes = model.Nodes[0];

            Assert.That(startNodeFromNodes, Is.SameAs(startNode));
            Assert.IsTrue(model.TryGetNode(startNode.NodeId, out var foundStartNode));
            Assert.That(foundStartNode, Is.SameAs(startNode));

            var delayNode = model.AddNode<DelayNode>(Vector2.zero);
            var delayNodeFromNodes = model.Nodes[1];

            Assert.That(delayNodeFromNodes, Is.SameAs(delayNode));
            Assert.IsTrue(model.TryGetNode(delayNode.NodeId, out var foundDelayNode));
            Assert.That(foundDelayNode, Is.SameAs(delayNode));
        }

        /// <summary>
        /// NodeEditorModel からノード座標を serialized property 経由で更新できる
        /// </summary>
        [Test]
        public void SetGraphPosition_UpdatesNodePosition() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            var nextPosition = new Vector2(90.0f, 12.0f);
            model.SetGraphAsset(graphAsset);
            model.InitializeGraph(Vector2.zero);
            var node = model.AddNode<DelayNode>(Vector2.zero);

            node.SetGraphPosition(nextPosition);

            Assert.That(node.GraphPosition, Is.EqualTo(nextPosition));
            Assert.IsTrue(model.TryGetNode(node.NodeId, out var foundNode));
            Assert.That(foundNode.GraphPosition, Is.EqualTo(nextPosition));
        }

        /// <summary>
        /// GraphAsset からノードを削除できる
        /// </summary>
        [Test]
        public void RemoveNode_RemovesNodeFromGraphAsset() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            model.SetGraphAsset(graphAsset);
            model.InitializeGraph(Vector2.zero);
            var node = model.AddNode<DelayNode>(Vector2.zero);
            var nodeId = node.NodeId;

            model.RemoveNode(node);

            Assert.That(model.Nodes.Count, Is.EqualTo(1));
            Assert.IsFalse(model.TryGetNode(nodeId, out _));
        }

        /// <summary>
        /// NextNodeIds ベースの接続を追加削除できる
        /// </summary>
        [Test]
        public void Connect_UpdatesNextNodeIds() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            model.SetGraphAsset(graphAsset);
            var startNode = model.InitializeGraph(Vector2.zero);
            var delayNode = model.AddNode<DelayNode>(Vector2.right);

            Assert.IsTrue(model.Connect(startNode, delayNode, out var errorMessage));
            Assert.That(errorMessage, Is.EqualTo(string.Empty));
            Assert.That(startNode.NextNodeIds.Count, Is.EqualTo(1));
            Assert.That(startNode.NextNodeIds[0], Is.EqualTo(delayNode.NodeId));

            Assert.IsTrue(model.Disconnect(startNode, delayNode));
            Assert.That(startNode.NextNodeIds, Is.Empty);
        }

        /// <summary>
        /// 重複接続、self-loop、cycle は接続できない
        /// </summary>
        [Test]
        public void Connect_PreventsDuplicateSelfLoopAndCycle() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            model.SetGraphAsset(graphAsset);
            var startNode = model.InitializeGraph(Vector2.zero);
            var firstNode = model.AddNode<DelayNode>(Vector2.right);
            var secondNode = model.AddNode<DelayNode>(Vector2.right * 2.0f);

            Assert.IsTrue(model.Connect(startNode, firstNode, out _));
            Assert.IsFalse(model.Connect(startNode, firstNode, out var duplicateErrorMessage));
            Assert.That(duplicateErrorMessage, Is.EqualTo("Duplicate connection is not allowed"));

            Assert.IsFalse(model.Connect(firstNode, firstNode, out var selfLoopErrorMessage));
            Assert.That(selfLoopErrorMessage, Is.EqualTo("Self-loop connection is not allowed"));

            Assert.IsFalse(model.Connect(firstNode, startNode, out var cycleErrorMessage));
            Assert.That(cycleErrorMessage, Is.EqualTo("Cycle connection is not allowed"));

            Assert.IsTrue(model.Connect(firstNode, secondNode, out _));
        }

        /// <summary>
        /// LoopNode に所属する ActionNode から未所属 ActionNode へ通常接続しても Loop port 接続は追加しない
        /// </summary>
        [Test]
        public void Connect_DoesNotAddLoopPortConnectionWhenSourceIsLoopBody() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            model.SetGraphAsset(graphAsset);
            model.InitializeGraph(Vector2.zero);
            var loopNodeModel = model.AddNode<LoopNode>(Vector2.right);
            var bodyNodeModel = model.AddNode<TestActionNode>(Vector2.right * 2.0f);
            var nextNodeModel = model.AddNode<TestActionNode>(Vector2.right * 3.0f);
            Assert.IsTrue(graphAsset.TryGetNode(loopNodeModel.NodeId, out var loopNode));
            AnimationGraphAssetUtility.SetLoopNodeIds((LoopNode)loopNode, new[] { bodyNodeModel.NodeId });
            model.RefreshNodes();

            Assert.IsTrue(model.Connect(bodyNodeModel, nextNodeModel, out var errorMessage));
            var validationMessages = model.GetNodeValidationMessages();

            Assert.That(errorMessage, Is.EqualTo(string.Empty));
            Assert.That(bodyNodeModel.NextNodeIds.Count, Is.EqualTo(1));
            Assert.That(bodyNodeModel.NextNodeIds[0], Is.EqualTo(nextNodeModel.NodeId));
            Assert.That(((LoopNode)loopNode).LoopNodeIds.Count, Is.EqualTo(1));
            Assert.That(((LoopNode)loopNode).LoopNodeIds[0], Is.EqualTo(bodyNodeModel.NodeId));
            Assert.IsFalse(validationMessages.ContainsKey(nextNodeModel.NodeId));
        }

        /// <summary>
        /// LoopNode に取り込んだノードの後続は Loop body として扱う
        /// </summary>
        [Test]
        public void Connect_TreatsReachableNextNodesAsLoopBody() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            model.SetGraphAsset(graphAsset);
            model.InitializeGraph(Vector2.zero);
            var loopNodeModel = model.AddNode<LoopNode>(Vector2.right);
            var bodyNodeModel = model.AddNode<DelayNode>(Vector2.right * 2.0f);
            var candidateNodeModel = model.AddNode<DelayNode>(Vector2.right * 3.0f);
            var outsideNodeModel = model.AddNode<DelayNode>(Vector2.right * 4.0f);
            Assert.IsTrue(graphAsset.TryGetNode(loopNodeModel.NodeId, out var loopNode));
            Assert.IsTrue(model.Connect(candidateNodeModel, outsideNodeModel, out _));
            AnimationGraphAssetUtility.SetLoopNodeIds((LoopNode)loopNode, new[] { bodyNodeModel.NodeId });
            model.RefreshNodes();

            Assert.IsTrue(model.Connect(bodyNodeModel, candidateNodeModel, out var errorMessage));
            var validationMessages = model.GetNodeValidationMessages();

            Assert.That(errorMessage, Is.EqualTo(string.Empty));
            Assert.That(((LoopNode)loopNode).LoopNodeIds.Count, Is.EqualTo(1));
            Assert.That(((LoopNode)loopNode).LoopNodeIds[0], Is.EqualTo(bodyNodeModel.NodeId));
            Assert.IsFalse(validationMessages.ContainsKey(candidateNodeModel.NodeId));
            Assert.IsFalse(validationMessages.ContainsKey(outsideNodeModel.NodeId));
        }

        /// <summary>
        /// Loop body として到達できるノードに外側から通常接続が入る場合は検証エラーとして扱う
        /// </summary>
        [Test]
        public void Connect_ReportsValidationWhenReachableLoopBodyReceivesOutsideConnection() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            model.SetGraphAsset(graphAsset);
            model.InitializeGraph(Vector2.zero);
            var loopNodeModel = model.AddNode<LoopNode>(Vector2.right);
            var bodyNodeModel = model.AddNode<DelayNode>(Vector2.right * 2.0f);
            var candidateNodeModel = model.AddNode<DelayNode>(Vector2.right * 3.0f);
            var outsideNodeModel = model.AddNode<DelayNode>(Vector2.right * 4.0f);
            Assert.IsTrue(graphAsset.TryGetNode(loopNodeModel.NodeId, out var loopNode));
            Assert.IsTrue(model.Connect(outsideNodeModel, candidateNodeModel, out _));
            AnimationGraphAssetUtility.SetLoopNodeIds((LoopNode)loopNode, new[] { bodyNodeModel.NodeId });
            model.RefreshNodes();

            Assert.IsTrue(model.Connect(bodyNodeModel, candidateNodeModel, out var errorMessage));
            var validationMessages = model.GetNodeValidationMessages();

            Assert.That(errorMessage, Is.EqualTo(string.Empty));
            Assert.IsTrue(validationMessages.ContainsKey(candidateNodeModel.NodeId));
            Assert.That(validationMessages[candidateNodeModel.NodeId], Does.Contain("receives connection from outside"));
        }

        /// <summary>
        /// Loop body から LoopNode の after 側へ直接接続する場合は検証エラーとして扱う
        /// </summary>
        [Test]
        public void Connect_ReportsValidationWhenLoopBodyConnectsToLoopExit() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            model.SetGraphAsset(graphAsset);
            model.InitializeGraph(Vector2.zero);
            var loopNodeModel = model.AddNode<LoopNode>(Vector2.right);
            var bodyNodeModel = model.AddNode<DelayNode>(Vector2.right * 2.0f);
            var afterNodeModel = model.AddNode<DelayNode>(Vector2.right * 3.0f);
            Assert.IsTrue(graphAsset.TryGetNode(loopNodeModel.NodeId, out var loopNode));
            Assert.IsTrue(model.Connect(loopNodeModel, afterNodeModel, out _));
            AnimationGraphAssetUtility.SetLoopNodeIds((LoopNode)loopNode, new[] { bodyNodeModel.NodeId });
            model.RefreshNodes();

            Assert.IsTrue(model.Connect(bodyNodeModel, afterNodeModel, out var errorMessage));
            var validationMessages = model.GetNodeValidationMessages();

            Assert.That(errorMessage, Is.EqualTo(string.Empty));
            Assert.IsTrue(validationMessages.ContainsKey(bodyNodeModel.NodeId));
            Assert.That(validationMessages[bodyNodeModel.NodeId], Does.Contain("connects outside"));
        }

        /// <summary>
        /// 複数ノードを複製すると選択内の接続も複製される
        /// </summary>
        [Test]
        public void DuplicateNodes_DuplicatesNodesAndInternalConnections() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            var offset = new Vector2(20.0f, 30.0f);
            model.SetGraphAsset(graphAsset);
            model.InitializeGraph(Vector2.zero);
            var firstNode = model.AddNode<DelayNode>(new Vector2(10.0f, 20.0f));
            var secondNode = model.AddNode<DelayNode>(new Vector2(30.0f, 40.0f));
            model.Connect(firstNode, secondNode, out _);

            var duplicatedNodes = model.DuplicateNodes(new[] { firstNode, secondNode }, offset);

            Assert.That(duplicatedNodes.Count, Is.EqualTo(2));
            Assert.That(duplicatedNodes[0].NodeId, Is.Not.EqualTo(firstNode.NodeId));
            Assert.That(duplicatedNodes[1].NodeId, Is.Not.EqualTo(secondNode.NodeId));
            Assert.That(duplicatedNodes[0].GraphPosition, Is.EqualTo(firstNode.GraphPosition + offset));
            Assert.That(duplicatedNodes[1].GraphPosition, Is.EqualTo(secondNode.GraphPosition + offset));
            Assert.That(duplicatedNodes[0].NextNodeIds.Count, Is.EqualTo(1));
            Assert.That(duplicatedNodes[0].NextNodeIds[0], Is.EqualTo(duplicatedNodes[1].NodeId));
        }

        /// <summary>
        /// GraphAsset の target 定義を serialized property 経由で更新できる
        /// </summary>
        [Test]
        public void SetTargetDefinitions_UpdatesGraphAssetDefinitions() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            var actorScriptGuid = "11111111111111111111111111111111";
            var cameraScriptGuid = "22222222222222222222222222222222";
            model.SetGraphAsset(graphAsset);

            model.SetTargetDefinitions(new AnimationGraphTargetDefinition("actor", actorScriptGuid), new AnimationGraphTargetDefinition("camera", cameraScriptGuid));

            Assert.That(model.TargetDefinitions.Count, Is.EqualTo(2));
            Assert.IsTrue(graphAsset.TryGetTargetDefinition("actor", out var actorDefinition));
            Assert.That(actorDefinition.Key, Is.EqualTo("actor"));
            Assert.That(actorDefinition.MonoScriptGuid, Is.EqualTo(actorScriptGuid));
            Assert.IsTrue(graphAsset.TryGetTargetDefinition("camera", out var cameraDefinition));
            Assert.That(cameraDefinition.Key, Is.EqualTo("camera"));
            Assert.That(cameraDefinition.MonoScriptGuid, Is.EqualTo(cameraScriptGuid));

            model.SetTargetDefinitions(new AnimationGraphTargetDefinition("camera"));

            Assert.That(model.TargetDefinitions.Count, Is.EqualTo(1));
            Assert.IsFalse(graphAsset.TryGetTargetDefinition("actor", out _));
            Assert.IsTrue(graphAsset.TryGetTargetDefinition("camera", out _));
        }

        /// <summary>
        /// GraphAsset の Blackboard 定義を serialized property 経由で更新できる
        /// </summary>
        [Test]
        public void SetBlackboardDefinitions_UpdatesGraphAssetDefinitions() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            model.SetGraphAsset(graphAsset);

            model.SetBlackboardDefinitions(new AnimationGraphBlackboardDefinition("flag", true), new AnimationGraphBlackboardDefinition("speed", 1.5f));

            Assert.That(model.BlackboardDefinitions.Count, Is.EqualTo(2));
            Assert.IsTrue(graphAsset.TryGetBlackboardDefaultValue("flag", out bool flagValue));
            Assert.IsTrue(flagValue);
            Assert.IsTrue(graphAsset.TryGetBlackboardDefaultValue("speed", out float speedValue));
            Assert.That(speedValue, Is.EqualTo(1.5f));

            model.SetBlackboardDefinitions(new AnimationGraphBlackboardDefinition("count", 12));

            Assert.That(model.BlackboardDefinitions.Count, Is.EqualTo(1));
            Assert.IsFalse(graphAsset.TryGetBlackboardDefinition("flag", out _));
            Assert.IsTrue(graphAsset.TryGetBlackboardDefaultValue("count", out int countValue));
            Assert.That(countValue, Is.EqualTo(12));
        }

        /// <summary>
        /// GraphAsset 未設定時の編集操作は例外になる
        /// </summary>
        [Test]
        public void AddNode_ThrowsWhenGraphAssetIsNotSet() {
            var model = new AnimationGraphAssetEditorModel();

            Assert.Throws<InvalidOperationException>(() => model.AddNode<DelayNode>(Vector2.zero));
        }

        /// <summary>
        /// 保存済み AnimationGraphAsset を作成
        /// </summary>
        /// <returns>作成した AnimationGraphAsset</returns>
        private AnimationGraphAsset CreateSavedGraphAsset() {
            var graphAsset = ScriptableObject.CreateInstance<AnimationGraphAsset>();
            var assetPath = $"Assets/AnimationGraphAssetEditorModelTests_{Guid.NewGuid():N}.asset";
            AssetDatabase.CreateAsset(graphAsset, assetPath);
            AssetDatabase.SaveAssets();
            _assetPaths.Add(assetPath);
            return graphAsset;
        }
    }
}
