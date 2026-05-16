using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Animation Graph 全体の永続データを保持するアセット
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationGraph", menuName = "Unity Animation Graph/Animation Graph")]
    public sealed class AnimationGraphAsset : ScriptableObject {
        [SerializeField, HideInInspector]
        private int _graphSeed;
        [SerializeField, HideInInspector]
        private string _startNodeId = string.Empty;
        [SerializeField, HideInInspector]
        private Node[] _nodes = Array.Empty<Node>();

        /// <summary>グラフ全体のシード</summary>
        public int GraphSeed => _graphSeed;
        /// <summary>開始ノード ID</summary>
        public string StartNodeId => _startNodeId;
        /// <summary>グラフに含まれるノード一覧</summary>
        public IReadOnlyList<Node> Nodes => _nodes ?? Array.Empty<Node>();

        /// <summary>
        /// 指定した ID に対応するノードの取得を試行
        /// </summary>
        /// <param name="nodeId">取得するノード ID</param>
        /// <param name="node">取得したノード</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetNode(string nodeId, out Node node) {
            if (string.IsNullOrEmpty(nodeId)) {
                node = null;
                return false;
            }

            var nodes = _nodes ?? Array.Empty<Node>();
            for (var i = 0; i < nodes.Length; i++) {
                var current = nodes[i];
                if (current == null) {
                    continue;
                }

                if (current.NodeId != nodeId) {
                    continue;
                }

                node = current;
                return true;
            }

            node = null;
            return false;
        }
    }
}
