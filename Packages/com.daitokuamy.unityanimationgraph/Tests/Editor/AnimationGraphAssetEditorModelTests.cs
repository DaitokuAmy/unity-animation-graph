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

            var node = model.AddNode<RouteNode>(nodePosition);

            Assert.That(node.NodeType, Is.EqualTo(typeof(RouteNode)));
            Assert.That(node.GraphPosition, Is.EqualTo(nodePosition));
            Assert.That(model.Nodes.Count, Is.EqualTo(2));
            Assert.IsTrue(model.TryGetNode(node.NodeId, out var foundNode));
            Assert.That(foundNode.NodeId, Is.EqualTo(node.NodeId));
            Assert.That(foundNode.NodeType, Is.EqualTo(typeof(RouteNode)));
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

            var routeNode = model.AddNode<RouteNode>(Vector2.zero);
            var routeNodeFromNodes = model.Nodes[1];

            Assert.That(routeNodeFromNodes, Is.SameAs(routeNode));
            Assert.IsTrue(model.TryGetNode(routeNode.NodeId, out var foundRouteNode));
            Assert.That(foundRouteNode, Is.SameAs(routeNode));
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
            var node = model.AddNode<RouteNode>(Vector2.zero);

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
            var node = model.AddNode<RouteNode>(Vector2.zero);
            var nodeId = node.NodeId;

            model.RemoveNode(node);

            Assert.That(model.Nodes.Count, Is.EqualTo(1));
            Assert.IsFalse(model.TryGetNode(nodeId, out _));
        }

        /// <summary>
        /// GraphAsset の target 定義を serialized property 経由で更新できる
        /// </summary>
        [Test]
        public void SetTargetDefinitions_UpdatesGraphAssetDefinitions() {
            var graphAsset = CreateSavedGraphAsset();
            var model = new AnimationGraphAssetEditorModel();
            model.SetGraphAsset(graphAsset);

            model.SetTargetDefinitions(new AnimationGraphTargetDefinition("actor"), new AnimationGraphTargetDefinition("camera"));

            Assert.That(model.TargetDefinitions.Count, Is.EqualTo(2));
            Assert.IsTrue(graphAsset.TryGetTargetDefinition("actor", out var actorDefinition));
            Assert.That(actorDefinition.Key, Is.EqualTo("actor"));
            Assert.IsTrue(graphAsset.TryGetTargetDefinition("camera", out var cameraDefinition));
            Assert.That(cameraDefinition.Key, Is.EqualTo("camera"));

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

            Assert.Throws<InvalidOperationException>(() => model.AddNode<RouteNode>(Vector2.zero));
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
