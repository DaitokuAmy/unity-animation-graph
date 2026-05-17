using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityAnimationGraph.Editor;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// AnimationGraphAssetEditorModel の EditMode テスト
    /// </summary>
    public sealed class AnimationGraphAssetEditorModelTests {
        private const string SignalPortsPropertyName = "_signalPorts";
        private const string SignalPortsEnterEnabledPropertyName = "_enterEnabled";
        private const string SignalPortsExitEnabledPropertyName = "_exitEnabled";
        private const string SignalPortSettingsVersionPropertyName = "_signalPortSettingsVersion";
        private const string LegacyEnableEnterSignalPortPropertyName = "_enableEnterSignalPort";
        private const string LegacyEnableExitSignalPortPropertyName = "_enableExitSignalPort";

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
        /// Preview 進捗は Delay を含めず Duration の進行率だけを反映する
        /// </summary>
        [Test]
        public void SetPreviewSchedule_ExcludesDelayFromProgress() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            model.SetGraphAsset(graphAsset);
            model.InitializeGraph(Vector2.zero);
            var nodeModel = model.AddNode<DelayNode>(Vector2.right);
            Assert.IsTrue(graphAsset.TryGetNode(nodeModel.NodeId, out var node));
            var schedule = new AnimationGraphSchedule(
                new[] { new ScheduledNode(node, 2.0f, 2.0f, 4.0f, 0, 0) },
                6.0f);

            model.SetPreviewSchedule(schedule, 1.0f);

            AssertPreviewExecutionInfo(nodeModel, "Active", 0.0f);

            model.SetPreviewSchedule(schedule, 3.0f);

            AssertPreviewExecutionInfo(nodeModel, "Active", 0.25f);
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
        /// 通常 Node から複数の Next 接続を追加できる
        /// </summary>
        [Test]
        public void Connect_AllowsMultipleNextOutputs() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            model.SetGraphAsset(graphAsset);
            var startNode = model.InitializeGraph(Vector2.zero);
            var sourceNode = model.AddNode<TestActionNode>(Vector2.right);
            var firstNode = model.AddNode<TestActionNode>(Vector2.right * 2.0f);
            var secondNode = model.AddNode<TestActionNode>(Vector2.right * 3.0f);

            Assert.IsTrue(model.Connect(startNode, sourceNode, out _));
            Assert.IsTrue(model.Connect(sourceNode, firstNode, out _));
            Assert.IsTrue(model.Connect(sourceNode, secondNode, out var errorMessage));

            Assert.That(errorMessage, Is.EqualTo(string.Empty));
            Assert.That(sourceNode.NextNodeIds.Count, Is.EqualTo(2));
            Assert.That(sourceNode.NextNodeIds[0], Is.EqualTo(firstNode.NodeId));
            Assert.That(sourceNode.NextNodeIds[1], Is.EqualTo(secondNode.NodeId));
        }

        /// <summary>
        /// JoinNode 以外の input には複数接続できない
        /// </summary>
        [Test]
        public void Connect_PreventsMultipleInputsExceptJoinNode() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            model.SetGraphAsset(graphAsset);
            var startNode = model.InitializeGraph(Vector2.zero);
            var firstSourceNode = model.AddNode<TestActionNode>(Vector2.right);
            var secondSourceNode = model.AddNode<TestActionNode>(Vector2.right * 2.0f);
            var targetNode = model.AddNode<TestActionNode>(Vector2.right * 3.0f);
            var joinNode = model.AddNode<JoinNode>(Vector2.right * 4.0f);

            Assert.IsTrue(model.Connect(startNode, firstSourceNode, out _));
            Assert.IsTrue(model.Connect(startNode, secondSourceNode, out _));
            Assert.IsTrue(model.Connect(firstSourceNode, targetNode, out _));

            Assert.IsFalse(model.Connect(secondSourceNode, targetNode, out var targetErrorMessage));
            Assert.That(targetErrorMessage, Is.EqualTo("Only JoinNode can receive multiple input connections"));
            Assert.IsTrue(model.Connect(firstSourceNode, joinNode, out _));
            Assert.IsTrue(model.Connect(secondSourceNode, joinNode, out var joinErrorMessage));
            Assert.That(joinErrorMessage, Is.EqualTo(string.Empty));
        }

        /// <summary>
        /// Node の Signal Port 表示フラグを serialized property 経由で更新できる
        /// </summary>
        [Test]
        public void SetNodeSignalPortEnabled_UpdatesNodeFlags() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            model.SetGraphAsset(graphAsset);
            model.InitializeGraph(Vector2.zero);
            var nodeModel = model.AddNode<DelayNode>(Vector2.right);
            Assert.IsTrue(graphAsset.TryGetNode(nodeModel.NodeId, out var node));

            AnimationGraphAssetUtility.SetNodeEnterSignalPortEnabled(node, true);
            AnimationGraphAssetUtility.SetNodeExitSignalPortEnabled(node, true);

            Assert.IsTrue(node.EnableEnterSignalPort);
            Assert.IsTrue(node.EnableExitSignalPort);

            AnimationGraphAssetUtility.SetNodeEnterSignalPortEnabled(node, false);
            AnimationGraphAssetUtility.SetNodeExitSignalPortEnabled(node, false);

            Assert.IsFalse(node.EnableEnterSignalPort);
            Assert.IsFalse(node.EnableExitSignalPort);
        }

        /// <summary>
        /// Node の Signal Port を無効にすると対応する Signal 参照を解除する
        /// </summary>
        [Test]
        public void SetNodeSignalPortEnabled_RemovesSignalReferencesWhenDisabled() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            model.SetGraphAsset(graphAsset);
            model.InitializeGraph(Vector2.zero);
            var nodeModel = model.AddNode<DelayNode>(Vector2.right);
            Assert.IsTrue(graphAsset.TryGetNode(nodeModel.NodeId, out var node));
            AnimationGraphAssetUtility.SetNodeEnterSignalPortEnabled(node, true);
            AnimationGraphAssetUtility.SetNodeExitSignalPortEnabled(node, true);
            var enterSignal = AnimationGraphAssetUtility.AddEnterSignal(graphAsset, node, typeof(TestSignal));
            var exitSignal = AnimationGraphAssetUtility.AddExitSignal(graphAsset, node, typeof(TestSignal));

            AnimationGraphAssetUtility.SetNodeEnterSignalPortEnabled(node, false);

            Assert.That(node.EnterSignals, Is.Empty);
            Assert.That(node.ExitSignals.Count, Is.EqualTo(1));
            Assert.That(node.ExitSignals[0], Is.SameAs(exitSignal));

            AnimationGraphAssetUtility.SetNodeExitSignalPortEnabled(node, false);

            Assert.That(node.ExitSignals, Is.Empty);
            CollectionAssert.Contains(AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(graphAsset)), enterSignal);
            CollectionAssert.Contains(AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(graphAsset)), exitSignal);
        }

        /// <summary>
        /// 旧 Signal Port フィールドから構造体設定へ移行して更新できる
        /// </summary>
        [Test]
        public void SetNodeSignalPortEnabled_MigratesLegacyFlagsToSettings() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            model.SetGraphAsset(graphAsset);
            model.InitializeGraph(Vector2.zero);
            var nodeModel = model.AddNode<DelayNode>(Vector2.right);
            Assert.IsTrue(graphAsset.TryGetNode(nodeModel.NodeId, out var node));
            SetLegacySignalPortFlags(node, true, true);

            AnimationGraphAssetUtility.SetNodeEnterSignalPortEnabled(node, false);
            var serializedNode = new SerializedObject(node);
            var signalPortsProperty = serializedNode.FindProperty(SignalPortsPropertyName);

            Assert.IsFalse(node.EnableEnterSignalPort);
            Assert.IsTrue(node.EnableExitSignalPort);
            Assert.IsFalse(signalPortsProperty.FindPropertyRelative(SignalPortsEnterEnabledPropertyName).boolValue);
            Assert.IsTrue(signalPortsProperty.FindPropertyRelative(SignalPortsExitEnabledPropertyName).boolValue);
            Assert.That(serializedNode.FindProperty(SignalPortSettingsVersionPropertyName).intValue, Is.EqualTo(1));
            Assert.IsFalse(serializedNode.FindProperty(LegacyEnableEnterSignalPortPropertyName).boolValue);
            Assert.IsFalse(serializedNode.FindProperty(LegacyEnableExitSignalPortPropertyName).boolValue);
        }

        /// <summary>
        /// Node の Enter Signal 一覧に Signal sub asset を追加できる
        /// </summary>
        [Test]
        public void AddEnterSignal_AddsSignalSubAssetToNode() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            model.SetGraphAsset(graphAsset);
            model.InitializeGraph(Vector2.zero);
            var nodeModel = model.AddNode<DelayNode>(Vector2.right);
            Assert.IsTrue(graphAsset.TryGetNode(nodeModel.NodeId, out var node));

            var signal = AnimationGraphAssetUtility.AddEnterSignal(graphAsset, node, typeof(TestSignal));

            Assert.That(signal, Is.TypeOf<TestSignal>());
            Assert.That(signal.SignalId, Is.Not.Empty);
            Assert.That(node.EnterSignals.Count, Is.EqualTo(1));
            Assert.That(node.EnterSignals[0], Is.SameAs(signal));
            Assert.That(node.ExitSignals, Is.Empty);
            CollectionAssert.Contains(AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(graphAsset)), signal);
        }

        /// <summary>
        /// Node の Exit Signal 一覧に Signal sub asset を追加できる
        /// </summary>
        [Test]
        public void AddExitSignal_AddsSignalSubAssetToNode() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            model.SetGraphAsset(graphAsset);
            model.InitializeGraph(Vector2.zero);
            var nodeModel = model.AddNode<DelayNode>(Vector2.right);
            Assert.IsTrue(graphAsset.TryGetNode(nodeModel.NodeId, out var node));

            var signal = AnimationGraphAssetUtility.AddExitSignal(graphAsset, node, typeof(TestSignal));

            Assert.That(signal, Is.TypeOf<TestSignal>());
            Assert.That(signal.SignalId, Is.Not.Empty);
            Assert.That(node.EnterSignals, Is.Empty);
            Assert.That(node.ExitSignals.Count, Is.EqualTo(1));
            Assert.That(node.ExitSignals[0], Is.SameAs(signal));
            CollectionAssert.Contains(AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(graphAsset)), signal);
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
        /// Signal を複製すると新しい Signal ID と位置で sub asset が追加される
        /// </summary>
        [Test]
        public void DuplicateSignals_DuplicatesSignalSubAssets() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            var signalPosition = new Vector2(10.0f, 20.0f);
            var offset = new Vector2(20.0f, 30.0f);
            model.SetGraphAsset(graphAsset);
            AnimationGraphAssetUtility.AddSignal(graphAsset, typeof(TestSignal), signalPosition);
            model.RefreshNodes();
            var signalModel = model.Signals[0];

            var duplicatedSignals = model.DuplicateSignals(new[] { signalModel }, offset);

            Assert.That(duplicatedSignals.Count, Is.EqualTo(1));
            Assert.That(duplicatedSignals[0].SignalId, Is.Not.EqualTo(signalModel.SignalId));
            Assert.That(duplicatedSignals[0].SignalType, Is.EqualTo(signalModel.SignalType));
            Assert.That(duplicatedSignals[0].GraphPosition, Is.EqualTo(signalPosition + offset));
            Assert.That(model.Signals.Count, Is.EqualTo(2));
            Assert.That(AnimationGraphAssetUtility.GetSignals(graphAsset).Count, Is.EqualTo(2));
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

            model.SetTargetDefinitions(new TargetDefinition("actor", actorScriptGuid), new TargetDefinition("camera", cameraScriptGuid));

            Assert.That(model.TargetDefinitions.Count, Is.EqualTo(2));
            Assert.IsTrue(graphAsset.TryGetTargetDefinition("actor", out var actorDefinition));
            Assert.That(actorDefinition.Key, Is.EqualTo("actor"));
            Assert.That(actorDefinition.MonoScriptGuid, Is.EqualTo(actorScriptGuid));
            Assert.IsTrue(graphAsset.TryGetTargetDefinition("camera", out var cameraDefinition));
            Assert.That(cameraDefinition.Key, Is.EqualTo("camera"));
            Assert.That(cameraDefinition.MonoScriptGuid, Is.EqualTo(cameraScriptGuid));

            model.SetTargetDefinitions(new TargetDefinition("camera"));

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

            model.SetBlackboardDefinitions(new BlackboardDefinition("flag", true), new BlackboardDefinition("speed", 1.5f));

            Assert.That(model.BlackboardDefinitions.Count, Is.EqualTo(2));
            Assert.IsTrue(graphAsset.TryGetBlackboardDefaultValue("flag", out bool flagValue));
            Assert.IsTrue(flagValue);
            Assert.IsTrue(graphAsset.TryGetBlackboardDefaultValue("speed", out float speedValue));
            Assert.That(speedValue, Is.EqualTo(1.5f));

            model.SetBlackboardDefinitions(new BlackboardDefinition("count", 12));

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

        private static void SetLegacySignalPortFlags(Node node, bool enableEnterSignalPort, bool enableExitSignalPort) {
            var serializedNode = new SerializedObject(node);
            serializedNode.FindProperty(LegacyEnableEnterSignalPortPropertyName).boolValue = enableEnterSignalPort;
            serializedNode.FindProperty(LegacyEnableExitSignalPortPropertyName).boolValue = enableExitSignalPort;
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssertPreviewExecutionInfo(NodeEditorModel nodeModel, string expectedState, float expectedProgress) {
            var method = typeof(NodeEditorModel).GetMethod("TryGetPreviewExecutionInfo", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);

            var parameters = new object[] { null };
            Assert.IsTrue((bool)method.Invoke(nodeModel, parameters));

            var previewExecutionInfo = parameters[0];
            var previewExecutionInfoType = previewExecutionInfo.GetType();
            var state = previewExecutionInfoType.GetProperty("State")?.GetValue(previewExecutionInfo)?.ToString();
            var progress = (float)previewExecutionInfoType.GetProperty("Progress").GetValue(previewExecutionInfo);

            Assert.That(state, Is.EqualTo(expectedState));
            Assert.That(progress, Is.EqualTo(expectedProgress).Within(0.0001f));
        }
    }
}
