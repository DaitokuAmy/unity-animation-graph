using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// AnimationGraphAsset から評価スケジュールを構築するクラス
    /// </summary>
    public sealed class AnimationGraphScheduler {
        /// <summary>FNV-1a 32bit のオフセット基準値</summary>
        private const uint FnvOffsetBasis = 2166136261u;
        /// <summary>FNV-1a 32bit の素数</summary>
        private const uint FnvPrime = 16777619u;
        /// <summary>同時刻として扱う時刻差</summary>
        private const float TimeComparisonEpsilon = 0.00001f;
        /// <summary>Schedule node 数の上限なしを表す値</summary>
        private const int UnlimitedScheduledNodeCount = 0;

        private static readonly Comparison<ScheduledNode> ScheduledNodeComparison = CompareScheduledNodes;

        /// <summary>
        /// JoinNode の build 中状態
        /// </summary>
        private struct JoinBuildState {
            /// <summary>必要な到達数</summary>
            public int RequiredArrivalCount { get; }
            /// <summary>到達済み数</summary>
            public int ArrivedCount { get; set; }
            /// <summary>合流時刻</summary>
            public float JoinTime { get; set; }
            /// <summary>スケジュール済みの場合は true</summary>
            public bool IsScheduled { get; set; }

            /// <summary>
            /// JoinBuildState を生成
            /// </summary>
            /// <param name="requiredArrivalCount">必要な到達数</param>
            public JoinBuildState(int requiredArrivalCount) {
                RequiredArrivalCount = requiredArrivalCount;
                ArrivedCount = 0;
                JoinTime = 0.0f;
                IsScheduled = false;
            }
        }

        /// <summary>
        /// スケジュール対象の到着情報
        /// </summary>
        private readonly struct ScheduleRequest {
            /// <summary>到着先ノード</summary>
            public Node Node { get; }
            /// <summary>先行ノードの終了時刻</summary>
            public float IncomingTime { get; }
            /// <summary>安定順序</summary>
            public int StableOrder { get; }

            /// <summary>
            /// ScheduleRequest を生成
            /// </summary>
            /// <param name="node">到着先ノード</param>
            /// <param name="incomingTime">先行ノードの終了時刻</param>
            /// <param name="stableOrder">安定順序</param>
            public ScheduleRequest(Node node, float incomingTime, int stableOrder) {
                Node = node;
                IncomingTime = incomingTime;
                StableOrder = stableOrder;
            }
        }

        /// <summary>
        /// スケジュール build 全体の状態
        /// </summary>
        private struct ScheduleBuildContext {
            /// <summary>グラフ全体のシード</summary>
            public int GraphSeed { get; }
            /// <summary>評価コンテキスト</summary>
            public IAnimationGraphContext Context { get; }
            /// <summary>スケジュール済みノード一覧</summary>
            public List<ScheduledNode> ScheduledNodes { get; }
            /// <summary>Schedule node 数の上限</summary>
            public int MaxScheduledNodeCount { get; }
            /// <summary>安定順序</summary>
            public int StableOrder { get; set; }
            /// <summary>リクエスト順序</summary>
            public int RequestOrder { get; set; }

            /// <summary>
            /// ScheduleBuildContext を生成
            /// </summary>
            /// <param name="graphSeed">グラフ全体のシード</param>
            /// <param name="context">評価コンテキスト</param>
            /// <param name="scheduledNodes">スケジュール済みノード一覧</param>
            /// <param name="maxScheduledNodeCount">Schedule node 数の上限。0 以下の場合は上限なし</param>
            public ScheduleBuildContext(int graphSeed, IAnimationGraphContext context, List<ScheduledNode> scheduledNodes, int maxScheduledNodeCount) {
                GraphSeed = graphSeed;
                Context = context;
                ScheduledNodes = scheduledNodes;
                MaxScheduledNodeCount = maxScheduledNodeCount;
                StableOrder = 0;
                RequestOrder = 0;
            }
        }

        private AnimationGraphAsset _graphAsset;
        private Node _startNode;
        private string[] _startNodeIds;
        private Dictionary<string, Node> _nodeMap;

        /// <summary>
        /// ScheduledNode の安定ソート順を比較
        /// </summary>
        /// <param name="left">左辺</param>
        /// <param name="right">右辺</param>
        /// <returns>比較結果</returns>
        private static int CompareScheduledNodes(ScheduledNode left, ScheduledNode right) {
            var startTimeComparison = CompareTime(left.StartTime, right.StartTime);
            return startTimeComparison != 0 ? startTimeComparison : left.StableOrder.CompareTo(right.StableOrder);
        }

        /// <summary>
        /// 時刻を許容誤差つきで比較
        /// </summary>
        /// <param name="left">左辺</param>
        /// <param name="right">右辺</param>
        /// <returns>比較結果</returns>
        private static int CompareTime(float left, float right) {
            var delta = left - right;
            if (Mathf.Abs(delta) <= TimeComparisonEpsilon) {
                return 0;
            }

            return delta < 0.0f ? -1 : 1;
        }

        /// <summary>
        /// AnimationGraphAsset を設定
        /// </summary>
        /// <param name="graphAsset">設定する AnimationGraphAsset</param>
        public void SetGraph(AnimationGraphAsset graphAsset) {
            if (graphAsset == null) {
                throw new ArgumentNullException(nameof(graphAsset));
            }

            var nodeMap = BuildNodeMap(graphAsset);
            var startNode = FindStartNode(graphAsset, nodeMap);
            ValidateGraphConnections(nodeMap);
            _graphAsset = graphAsset;
            _startNode = startNode;
            _startNodeIds = new[] { startNode.NodeId };
            _nodeMap = nodeMap;
        }

        /// <summary>
        /// 設定済みの AnimationGraphAsset から評価スケジュールを構築
        /// </summary>
        /// <param name="context">評価コンテキスト</param>
        /// <param name="overrideSeed">グラフのシードを一時的に上書きする値</param>
        /// <param name="maxScheduledNodeCount">Schedule node 数の上限。0 以下の場合は上限なし</param>
        /// <returns>構築した評価スケジュール</returns>
        public AnimationGraphSchedule BuildSchedule(IAnimationGraphContext context, int? overrideSeed = null, int maxScheduledNodeCount = UnlimitedScheduledNodeCount) {
            if (_graphAsset == null || _startNode == null || _startNodeIds == null || _nodeMap == null) {
                throw new InvalidOperationException("AnimationGraphAsset is not set");
            }

            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            var graphSeed = overrideSeed ?? _graphAsset.GraphSeed;
            var scheduledNodes = new List<ScheduledNode>(_nodeMap.Count);
            var buildContext = new ScheduleBuildContext(graphSeed, context, scheduledNodes, Mathf.Max(0, maxScheduledNodeCount));
            BuildScope(_startNodeIds, 0.0f, 0, null, null, ref buildContext);
            scheduledNodes.Sort(ScheduledNodeComparison);

            var duration = 0.0f;
            for (var i = 0; i < scheduledNodes.Count; i++) {
                duration = Mathf.Max(duration, scheduledNodes[i].EndTime);
            }

            return new AnimationGraphSchedule(scheduledNodes, duration);
        }

        /// <summary>
        /// AnimationGraphAsset を設定して評価スケジュールを構築
        /// </summary>
        /// <param name="graphAsset">構築元の AnimationGraphAsset</param>
        /// <param name="context">評価コンテキスト</param>
        /// <param name="overrideSeed">グラフのシードを一時的に上書きする値</param>
        /// <param name="maxScheduledNodeCount">Schedule node 数の上限。0 以下の場合は上限なし</param>
        /// <returns>構築した評価スケジュール</returns>
        public AnimationGraphSchedule Build(AnimationGraphAsset graphAsset, IAnimationGraphContext context, int? overrideSeed = null, int maxScheduledNodeCount = UnlimitedScheduledNodeCount) {
            SetGraph(graphAsset);
            return BuildSchedule(context, overrideSeed, maxScheduledNodeCount);
        }

        /// <summary>
        /// ノード ID からノードを引くための辞書を構築
        /// </summary>
        /// <param name="graphAsset">構築元の AnimationGraphAsset</param>
        /// <returns>ノード ID 辞書</returns>
        private Dictionary<string, Node> BuildNodeMap(AnimationGraphAsset graphAsset) {
            var nodes = graphAsset.Nodes;
            var nodeMap = new Dictionary<string, Node>(nodes.Count, StringComparer.Ordinal);
            var startNodeCount = 0;
            for (var i = 0; i < nodes.Count; i++) {
                var node = nodes[i];
                if (node == null) {
                    throw new InvalidOperationException($"Node at index {i} is null");
                }

                if (node is StartNode) {
                    startNodeCount++;
                }

                if (string.IsNullOrEmpty(node.NodeId)) {
                    throw new InvalidOperationException($"Node at index {i} has empty NodeId");
                }

                if (nodeMap.ContainsKey(node.NodeId)) {
                    throw new InvalidOperationException($"NodeId '{node.NodeId}' is duplicated");
                }

                nodeMap.Add(node.NodeId, node);
            }

            if (startNodeCount == 0) {
                throw new InvalidOperationException("StartNode is missing");
            }

            if (startNodeCount > 1) {
                throw new InvalidOperationException("AnimationGraphAsset contains multiple StartNodes");
            }

            return nodeMap;
        }

        /// <summary>
        /// 開始ノードを取得
        /// </summary>
        /// <param name="graphAsset">構築元の AnimationGraphAsset</param>
        /// <param name="nodeMap">ノード ID 辞書</param>
        /// <returns>開始ノード</returns>
        private Node FindStartNode(AnimationGraphAsset graphAsset, Dictionary<string, Node> nodeMap) {
            if (string.IsNullOrEmpty(graphAsset.StartNodeId)) {
                throw new InvalidOperationException("StartNodeId is empty");
            }

            if (!nodeMap.TryGetValue(graphAsset.StartNodeId, out var startNode)) {
                throw new InvalidOperationException($"StartNodeId '{graphAsset.StartNodeId}' does not exist");
            }

            if (!(startNode is StartNode)) {
                throw new InvalidOperationException($"StartNodeId '{graphAsset.StartNodeId}' does not point to StartNode");
            }

            return startNode;
        }

        /// <summary>
        /// GraphAsset の物理接続を検証
        /// </summary>
        /// <param name="nodeMap">ノード ID 辞書</param>
        private void ValidateGraphConnections(Dictionary<string, Node> nodeMap) {
            var visited = new HashSet<string>(nodeMap.Count, StringComparer.Ordinal);
            var currentPath = new HashSet<string>(nodeMap.Count, StringComparer.Ordinal);
            var physicalNextNodeIds = new List<string>();

            void Visit(Node node) {
                if (visited.Contains(node.NodeId)) {
                    return;
                }

                if (!currentPath.Add(node.NodeId)) {
                    throw new InvalidOperationException($"Cycle detected at node '{node.NodeId}'");
                }

                VisitNodeIds(node, node.NextNodeIds);
                if (node is BranchNode branchNode) {
                    for (var i = 0; i < branchNode.ExtensionPortCount; i++) {
                        VisitNodeIds(node, branchNode.GetExtensionNodeIds(i));
                    }
                }
                else if (node is LoopNode loopNode) {
                    var loopBodyNodeIds = ValidateLoopNode(loopNode);
                    foreach (var loopBodyNodeId in loopBodyNodeIds) {
                        VisitNodeId(node, loopBodyNodeId);
                    }
                }

                currentPath.Remove(node.NodeId);
                visited.Add(node.NodeId);
            }

            void VisitNodeIds(Node sourceNode, IReadOnlyList<string> nodeIds) {
                for (var i = 0; i < nodeIds.Count; i++) {
                    VisitNodeId(sourceNode, nodeIds[i]);
                }
            }

            void VisitNodeId(Node sourceNode, string nodeId) {
                if (string.IsNullOrEmpty(nodeId)) {
                    throw new InvalidOperationException($"Node '{sourceNode.NodeId}' has empty next node id");
                }

                if (!nodeMap.TryGetValue(nodeId, out var node)) {
                    throw new InvalidOperationException($"Next node '{nodeId}' does not exist");
                }

                Visit(node);
            }

            HashSet<string> ValidateLoopNode(LoopNode loopNode) {
                var loopBodyNodeIds = BuildLoopBodyNodeIdSet(loopNode, nodeMap);
                ValidateLoopBodyIsolation(loopNode, loopBodyNodeIds);
                return loopBodyNodeIds;
            }

            void ValidateLoopBodyIsolation(LoopNode loopNode, HashSet<string> loopBodyNodeIds) {
                var loopExitNodeIds = new HashSet<string>(loopNode.NextNodeIds, StringComparer.Ordinal);
                foreach (var loopBodyNodeId in loopBodyNodeIds) {
                    var bodyNode = nodeMap[loopBodyNodeId];
                    CollectPhysicalNextNodeIds(bodyNode, physicalNextNodeIds);
                    for (var i = 0; i < physicalNextNodeIds.Count; i++) {
                        if (!loopExitNodeIds.Contains(physicalNextNodeIds[i]) && loopBodyNodeIds.Contains(physicalNextNodeIds[i])) {
                            continue;
                        }

                        throw new InvalidOperationException($"Loop body node '{bodyNode.NodeId}' cannot connect outside LoopNode '{loopNode.NodeId}'");
                    }
                }

                foreach (var nodePair in nodeMap) {
                    if (loopBodyNodeIds.Contains(nodePair.Key)) {
                        continue;
                    }

                    CollectPhysicalNextNodeIds(nodePair.Value, physicalNextNodeIds);
                    for (var i = 0; i < physicalNextNodeIds.Count; i++) {
                        if (!loopBodyNodeIds.Contains(physicalNextNodeIds[i])) {
                            continue;
                        }

                        throw new InvalidOperationException($"Loop body node '{physicalNextNodeIds[i]}' cannot receive connections from outside LoopNode '{loopNode.NodeId}'");
                    }
                }
            }

            foreach (var nodePair in nodeMap) {
                Visit(nodePair.Value);
            }
        }

        /// <summary>
        /// LoopNode の開始 ID から通常接続で到達できる body node ID 一覧を構築
        /// </summary>
        /// <param name="loopNode">対象の LoopNode</param>
        /// <param name="nodeMap">ノード ID 辞書</param>
        /// <returns>LoopNode の body node ID 一覧</returns>
        private HashSet<string> BuildLoopBodyNodeIdSet(LoopNode loopNode, Dictionary<string, Node> nodeMap) {
            var loopBodyNodeIds = new HashSet<string>(StringComparer.Ordinal);
            var explicitLoopNodeIds = new HashSet<string>(StringComparer.Ordinal);
            var nodeIdsToVisit = new Queue<string>();
            var loopExitNodeIds = new HashSet<string>(loopNode.NextNodeIds, StringComparer.Ordinal);
            var physicalNextNodeIds = new List<string>();
            var loopNodeIds = loopNode.LoopNodeIds;
            for (var i = 0; i < loopNodeIds.Count; i++) {
                var loopNodeId = loopNodeIds[i];
                if (string.IsNullOrEmpty(loopNodeId)) {
                    throw new InvalidOperationException($"LoopNode '{loopNode.NodeId}' has empty loop node id");
                }

                if (loopNodeId == loopNode.NodeId) {
                    throw new InvalidOperationException($"LoopNode '{loopNode.NodeId}' cannot contain itself");
                }

                if (!explicitLoopNodeIds.Add(loopNodeId)) {
                    throw new InvalidOperationException($"LoopNode '{loopNode.NodeId}' has duplicated loop node '{loopNodeId}'");
                }

                if (!nodeMap.TryGetValue(loopNodeId, out var bodyNode)) {
                    throw new InvalidOperationException($"Loop node '{loopNodeId}' does not exist");
                }

                if (bodyNode is StartNode) {
                    throw new InvalidOperationException($"LoopNode '{loopNode.NodeId}' cannot contain StartNode");
                }

                AddLoopBodyNodeId(loopBodyNodeIds, nodeIdsToVisit, loopNodeId);
            }

            while (nodeIdsToVisit.Count > 0) {
                var currentNodeId = nodeIdsToVisit.Dequeue();
                var bodyNode = nodeMap[currentNodeId];
                CollectPhysicalNextNodeIds(bodyNode, physicalNextNodeIds);
                for (var i = 0; i < physicalNextNodeIds.Count; i++) {
                    var nextNodeId = physicalNextNodeIds[i];
                    if (string.IsNullOrEmpty(nextNodeId)) {
                        throw new InvalidOperationException($"Node '{bodyNode.NodeId}' has empty next node id");
                    }

                    if (loopExitNodeIds.Contains(nextNodeId)) {
                        continue;
                    }

                    if (nextNodeId == loopNode.NodeId) {
                        throw new InvalidOperationException($"LoopNode '{loopNode.NodeId}' cannot contain itself");
                    }

                    if (!nodeMap.TryGetValue(nextNodeId, out var nextNode)) {
                        throw new InvalidOperationException($"Next node '{nextNodeId}' does not exist");
                    }

                    if (nextNode is StartNode) {
                        throw new InvalidOperationException($"LoopNode '{loopNode.NodeId}' cannot contain StartNode");
                    }

                    AddLoopBodyNodeId(loopBodyNodeIds, nodeIdsToVisit, nextNodeId);
                }
            }

            return loopBodyNodeIds;
        }

        /// <summary>
        /// 未登録の Loop body node ID を追加して後続探索対象に積む
        /// </summary>
        /// <param name="loopBodyNodeIds">Loop body node ID 一覧</param>
        /// <param name="nodeIdsToVisit">探索対象 queue</param>
        /// <param name="nodeId">追加するノード ID</param>
        private static void AddLoopBodyNodeId(HashSet<string> loopBodyNodeIds, Queue<string> nodeIdsToVisit, string nodeId) {
            if (!loopBodyNodeIds.Add(nodeId)) {
                return;
            }

            nodeIdsToVisit.Enqueue(nodeId);
        }

        /// <summary>
        /// 指定 scope のスケジュールを構築
        /// </summary>
        /// <param name="startNodeIds">scope の開始ノード ID 一覧</param>
        /// <param name="incomingTime">scope の開始時刻</param>
        /// <param name="seedSalt">評価単位を分けるシード salt</param>
        /// <param name="terminalNodeId">scope の終端ノード ID</param>
        /// <param name="allowedNodeIds">scope 内で到達できるノード ID 一覧</param>
        /// <param name="buildContext">スケジュール build 全体の状態</param>
        /// <returns>scope の終了時刻</returns>
        private float BuildScope(IReadOnlyList<string> startNodeIds, float incomingTime, int seedSalt, string terminalNodeId,
            HashSet<string> allowedNodeIds, ref ScheduleBuildContext buildContext) {
            if (startNodeIds.Count == 0) {
                return incomingTime;
            }

            var nextNodeIdsByNodeId = BuildNextNodeIdsByNodeId(startNodeIds, seedSalt, terminalNodeId, allowedNodeIds, ref buildContext, out var incomingCounts);
            ValidateScopeTerminalReachability(terminalNodeId, nextNodeIdsByNodeId);
            ValidateIncomingCounts(_nodeMap, incomingCounts);

            var joinStates = BuildJoinStates(_nodeMap, incomingCounts);
            var scopeEndTime = BuildScheduledNodes(startNodeIds, incomingTime, seedSalt, terminalNodeId, nextNodeIdsByNodeId, joinStates, ref buildContext);
            foreach (var joinStatePair in joinStates) {
                if (!joinStatePair.Value.IsScheduled) {
                    throw new InvalidOperationException($"JoinNode '{joinStatePair.Key}' was not scheduled");
                }
            }

            return scopeEndTime;
        }

        /// <summary>
        /// build 時に使用する後続ノード ID 一覧を構築
        /// </summary>
        /// <param name="startNodeIds">scope の開始ノード ID 一覧</param>
        /// <param name="seedSalt">評価単位を分けるシード salt</param>
        /// <param name="terminalNodeId">scope の終端ノード ID</param>
        /// <param name="allowedNodeIds">scope 内で到達できるノード ID 一覧</param>
        /// <param name="buildContext">スケジュール build 全体の状態</param>
        /// <param name="incomingCounts">到達可能なノードの入次数</param>
        /// <returns>ノード ID ごとの後続ノード ID 一覧</returns>
        private Dictionary<string, IReadOnlyList<string>> BuildNextNodeIdsByNodeId(IReadOnlyList<string> startNodeIds, int seedSalt, string terminalNodeId, HashSet<string> allowedNodeIds,
            ref ScheduleBuildContext buildContext, out Dictionary<string, int> incomingCounts) {
            var scopeCapacity = allowedNodeIds == null ? _nodeMap.Count : allowedNodeIds.Count;
            var nextNodeIdsByNodeId = new Dictionary<string, IReadOnlyList<string>>(scopeCapacity, StringComparer.Ordinal);
            var incomingCountMap = new Dictionary<string, int>(scopeCapacity, StringComparer.Ordinal);
            var visited = new HashSet<string>(scopeCapacity, StringComparer.Ordinal);
            var currentPath = new HashSet<string>(scopeCapacity, StringComparer.Ordinal);
            var startNodeIdSet = new HashSet<string>(startNodeIds.Count, StringComparer.Ordinal);
            var graphSeed = buildContext.GraphSeed;
            var context = buildContext.Context;

            void Visit(Node node) {
                if (visited.Contains(node.NodeId)) {
                    return;
                }

                if (!currentPath.Add(node.NodeId)) {
                    throw new InvalidOperationException($"Cycle detected at node '{node.NodeId}'");
                }

                if (node.NodeId == terminalNodeId) {
                    nextNodeIdsByNodeId[node.NodeId] = Array.Empty<string>();
                    currentPath.Remove(node.NodeId);
                    visited.Add(node.NodeId);
                    return;
                }

                var nextNodeIds = GetNextNodeIds(node, graphSeed, seedSalt, context);
                nextNodeIdsByNodeId[node.NodeId] = nextNodeIds;

                for (var i = 0; i < nextNodeIds.Count; i++) {
                    var nextNodeId = nextNodeIds[i];
                    if (string.IsNullOrEmpty(nextNodeId)) {
                        throw new InvalidOperationException($"Node '{node.NodeId}' has empty next node id");
                    }

                    if (!ContainsAllowedNodeId(nextNodeId)) {
                        throw new InvalidOperationException($"Loop body cannot connect outside loop to node '{nextNodeId}'");
                    }

                    if (!_nodeMap.TryGetValue(nextNodeId, out var nextNode)) {
                        throw new InvalidOperationException($"Next node '{nextNodeId}' does not exist");
                    }

                    incomingCountMap.TryGetValue(nextNodeId, out var incomingCount);
                    incomingCountMap[nextNodeId] = incomingCount + 1;
                    Visit(nextNode);
                }

                currentPath.Remove(node.NodeId);
                visited.Add(node.NodeId);
            }

            bool ContainsAllowedNodeId(string nodeId) {
                return allowedNodeIds == null || allowedNodeIds.Contains(nodeId);
            }

            for (var i = 0; i < startNodeIds.Count; i++) {
                var startNodeId = startNodeIds[i];
                if (string.IsNullOrEmpty(startNodeId)) {
                    throw new InvalidOperationException("Scope has empty start node id");
                }

                if (!ContainsAllowedNodeId(startNodeId)) {
                    throw new InvalidOperationException($"Loop body cannot start outside loop from node '{startNodeId}'");
                }

                if (!startNodeIdSet.Add(startNodeId)) {
                    throw new InvalidOperationException($"Scope start node '{startNodeId}' is duplicated");
                }

                if (!_nodeMap.TryGetValue(startNodeId, out var startNode)) {
                    throw new InvalidOperationException($"Start node '{startNodeId}' does not exist");
                }

                incomingCountMap.TryGetValue(startNodeId, out var incomingCount);
                incomingCountMap[startNodeId] = incomingCount + 1;
                Visit(startNode);
            }

            incomingCounts = incomingCountMap;
            return nextNodeIdsByNodeId;
        }

        /// <summary>
        /// scope 内の全経路が終端ノードへ到達することを検証
        /// </summary>
        /// <param name="terminalNodeId">scope の終端ノード ID</param>
        /// <param name="nextNodeIdsByNodeId">ノード ID ごとの後続ノード ID 一覧</param>
        private void ValidateScopeTerminalReachability(string terminalNodeId, Dictionary<string, IReadOnlyList<string>> nextNodeIdsByNodeId) {
            if (string.IsNullOrEmpty(terminalNodeId)) {
                return;
            }

            if (!nextNodeIdsByNodeId.ContainsKey(terminalNodeId)) {
                throw new InvalidOperationException($"Scope does not reach terminal node '{terminalNodeId}'");
            }

            var reachableMap = new Dictionary<string, bool>(nextNodeIdsByNodeId.Count, StringComparer.Ordinal);
            var currentPath = new HashSet<string>(nextNodeIdsByNodeId.Count, StringComparer.Ordinal);

            bool CanReachTerminal(string nodeId) {
                if (nodeId == terminalNodeId) {
                    return true;
                }

                if (reachableMap.TryGetValue(nodeId, out var reachable)) {
                    return reachable;
                }

                if (!currentPath.Add(nodeId)) {
                    throw new InvalidOperationException($"Cycle detected at node '{nodeId}'");
                }

                if (!nextNodeIdsByNodeId.TryGetValue(nodeId, out var nextNodeIds) || nextNodeIds.Count == 0) {
                    currentPath.Remove(nodeId);
                    reachableMap[nodeId] = false;
                    return false;
                }

                for (var i = 0; i < nextNodeIds.Count; i++) {
                    if (CanReachTerminal(nextNodeIds[i])) {
                        continue;
                    }

                    currentPath.Remove(nodeId);
                    reachableMap[nodeId] = false;
                    return false;
                }

                currentPath.Remove(nodeId);
                reachableMap[nodeId] = true;
                return true;
            }

            foreach (var nextNodeIdsPair in nextNodeIdsByNodeId) {
                if (CanReachTerminal(nextNodeIdsPair.Key)) {
                    continue;
                }

                throw new InvalidOperationException($"Scope node '{nextNodeIdsPair.Key}' does not reach terminal node '{terminalNodeId}'");
            }
        }

        /// <summary>
        /// 入次数の制約を検証
        /// </summary>
        /// <param name="nodeMap">ノード ID 辞書</param>
        /// <param name="incomingCounts">到達可能なノードの入次数</param>
        private void ValidateIncomingCounts(Dictionary<string, Node> nodeMap, Dictionary<string, int> incomingCounts) {
            foreach (var incomingCountPair in incomingCounts) {
                if (incomingCountPair.Value <= 1) {
                    continue;
                }

                if (nodeMap[incomingCountPair.Key] is JoinNode) {
                    continue;
                }

                throw new InvalidOperationException($"Node '{incomingCountPair.Key}' has multiple inputs but is not JoinNode");
            }
        }

        /// <summary>
        /// JoinNode の build 中状態を構築
        /// </summary>
        /// <param name="nodeMap">ノード ID 辞書</param>
        /// <param name="incomingCounts">到達可能なノードの入次数</param>
        /// <returns>JoinNode の build 中状態辞書</returns>
        private Dictionary<string, JoinBuildState> BuildJoinStates(Dictionary<string, Node> nodeMap, Dictionary<string, int> incomingCounts) {
            var joinStates = new Dictionary<string, JoinBuildState>(incomingCounts.Count, StringComparer.Ordinal);
            foreach (var incomingCountPair in incomingCounts) {
                if (!(nodeMap[incomingCountPair.Key] is JoinNode)) {
                    continue;
                }

                if (incomingCountPair.Value == 0) {
                    throw new InvalidOperationException($"JoinNode '{incomingCountPair.Key}' has no input");
                }

                joinStates.Add(incomingCountPair.Key, new JoinBuildState(incomingCountPair.Value));
            }

            return joinStates;
        }

        /// <summary>
        /// スケジュール済みノード一覧を構築
        /// </summary>
        /// <param name="startNodeIds">scope の開始ノード ID 一覧</param>
        /// <param name="incomingTime">scope の開始時刻</param>
        /// <param name="seedSalt">評価単位を分けるシード salt</param>
        /// <param name="terminalNodeId">scope の終端ノード ID</param>
        /// <param name="nextNodeIdsByNodeId">ノード ID ごとの後続ノード ID 一覧</param>
        /// <param name="joinStates">JoinNode の build 中状態辞書</param>
        /// <param name="buildContext">スケジュール build 全体の状態</param>
        /// <returns>スケジュール済みノード一覧</returns>
        private float BuildScheduledNodes(IReadOnlyList<string> startNodeIds, float incomingTime, int seedSalt, string terminalNodeId,
            Dictionary<string, IReadOnlyList<string>> nextNodeIdsByNodeId, Dictionary<string, JoinBuildState> joinStates, ref ScheduleBuildContext buildContext) {
            var requests = new List<ScheduleRequest>(nextNodeIdsByNodeId.Count);
            var scopeEndTime = incomingTime;
            for (var i = 0; i < startNodeIds.Count; i++) {
                var startNode = _nodeMap[startNodeIds[i]];
                requests.Add(new ScheduleRequest(startNode, incomingTime, buildContext.RequestOrder));
                buildContext.RequestOrder++;
            }

            while (requests.Count > 0) {
                var requestIndex = 0;
                for (var i = 1; i < requests.Count; i++) {
                    var timeComparison = CompareTime(requests[i].IncomingTime, requests[requestIndex].IncomingTime);
                    if (timeComparison < 0) {
                        requestIndex = i;
                        continue;
                    }

                    if (timeComparison == 0 && requests[i].StableOrder < requests[requestIndex].StableOrder) {
                        requestIndex = i;
                    }
                }

                var request = requests[requestIndex];
                requests.RemoveAt(requestIndex);
                var node = request.Node;
                if (!TryResolveJoinArrival(node, request.IncomingTime, joinStates, out var resolvedIncomingTime)) {
                    continue;
                }

                var seed = CreateNodeSeed(buildContext.GraphSeed, node.NodeId, seedSalt);
                var executor = (INodeExecutor)node;
                var delay = ValidateTimeValue(executor.CalculateDelay(seed, buildContext.Context), "delay", node);
                var duration = ValidateTimeValue(executor.CalculateDuration(seed, buildContext.Context), "duration", node);
                var startTime = resolvedIncomingTime + delay;
                var endTime = startTime + duration;
                AddScheduledNode(new ScheduledNode(node, startTime, delay, duration, seed, buildContext.StableOrder), ref buildContext);
                buildContext.StableOrder++;

                if (node is LoopNode loopNode && node.NodeId != terminalNodeId) {
                    endTime = BuildLoop(loopNode, endTime, seedSalt, ref buildContext);
                }

                scopeEndTime = Mathf.Max(scopeEndTime, endTime);

                if (!nextNodeIdsByNodeId.TryGetValue(node.NodeId, out var nextNodeIds)) {
                    throw new InvalidOperationException($"Node '{node.NodeId}' has no resolved next node ids");
                }

                for (var i = 0; i < nextNodeIds.Count; i++) {
                    var nextNode = _nodeMap[nextNodeIds[i]];
                    requests.Add(new ScheduleRequest(nextNode, endTime, buildContext.RequestOrder));
                    buildContext.RequestOrder++;
                }
            }

            return scopeEndTime;
        }

        /// <summary>
        /// Schedule node 数の上限を確認して追加
        /// </summary>
        /// <param name="scheduledNode">追加する ScheduledNode</param>
        /// <param name="buildContext">スケジュール build 全体の状態</param>
        private static void AddScheduledNode(ScheduledNode scheduledNode, ref ScheduleBuildContext buildContext) {
            if (buildContext.MaxScheduledNodeCount > UnlimitedScheduledNodeCount
                && buildContext.ScheduledNodes.Count >= buildContext.MaxScheduledNodeCount) {
                throw new InvalidOperationException($"Schedule node count exceeded limit ({buildContext.MaxScheduledNodeCount})");
            }

            buildContext.ScheduledNodes.Add(scheduledNode);
        }

        /// <summary>
        /// LoopNode のループ内容を指定回数だけ展開
        /// </summary>
        /// <param name="loopNode">展開する LoopNode</param>
        /// <param name="incomingTime">LoopNode の終了時刻</param>
        /// <param name="seedSalt">親 scope のシード salt</param>
        /// <param name="buildContext">スケジュール build 全体の状態</param>
        /// <returns>LoopNode のループ終了時刻</returns>
        private float BuildLoop(LoopNode loopNode, float incomingTime, int seedSalt, ref ScheduleBuildContext buildContext) {
            var loopBodyNodeIds = BuildLoopBodyNodeIdSet(loopNode, _nodeMap);
            if (loopBodyNodeIds.Count == 0) {
                return incomingTime;
            }

            var loopStartNodeIds = GetLoopStartNodeIds(loopNode, loopBodyNodeIds);
            var loopEndTime = incomingTime;
            for (var i = 0; i < loopNode.LoopCount; i++) {
                var iterationSeedSalt = CreateNodeSeed(seedSalt, loopNode.NodeId, i);
                loopEndTime = BuildScope(loopStartNodeIds, loopEndTime, iterationSeedSalt, null, loopBodyNodeIds, ref buildContext);
            }

            return loopEndTime;
        }

        /// <summary>
        /// LoopNode の開始ノード ID 一覧を取得
        /// </summary>
        /// <param name="loopNode">取得対象の LoopNode</param>
        /// <param name="loopBodyNodeIds">LoopNode の body node ID 一覧</param>
        /// <returns>LoopNode の開始ノード ID 一覧</returns>
        private IReadOnlyList<string> GetLoopStartNodeIds(LoopNode loopNode, HashSet<string> loopBodyNodeIds) {
            var incomingNodeIds = new HashSet<string>(loopBodyNodeIds.Count, StringComparer.Ordinal);
            var physicalNextNodeIds = new List<string>();
            foreach (var loopBodyNodeId in loopBodyNodeIds) {
                var bodyNode = _nodeMap[loopBodyNodeId];
                CollectPhysicalNextNodeIds(bodyNode, physicalNextNodeIds);
                for (var j = 0; j < physicalNextNodeIds.Count; j++) {
                    if (loopBodyNodeIds.Contains(physicalNextNodeIds[j])) {
                        incomingNodeIds.Add(physicalNextNodeIds[j]);
                    }
                }
            }

            var startNodeIds = new List<string>();
            var addedStartNodeIds = new HashSet<string>(StringComparer.Ordinal);
            var loopNodeIds = loopNode.LoopNodeIds;
            for (var i = 0; i < loopNodeIds.Count; i++) {
                if (!loopBodyNodeIds.Contains(loopNodeIds[i]) || incomingNodeIds.Contains(loopNodeIds[i])) {
                    continue;
                }

                startNodeIds.Add(loopNodeIds[i]);
                addedStartNodeIds.Add(loopNodeIds[i]);
            }

            foreach (var loopBodyNodeId in loopBodyNodeIds) {
                if (incomingNodeIds.Contains(loopBodyNodeId) || addedStartNodeIds.Contains(loopBodyNodeId)) {
                    continue;
                }

                startNodeIds.Add(loopBodyNodeId);
            }

            if (startNodeIds.Count == 0) {
                throw new InvalidOperationException($"LoopNode '{loopNode.NodeId}' has no loop start node");
            }

            return startNodeIds;
        }

        /// <summary>
        /// JoinNode への到着を解決
        /// </summary>
        /// <param name="node">到着先ノード</param>
        /// <param name="incomingTime">先行ノードの終了時刻</param>
        /// <param name="joinStates">JoinNode の build 中状態辞書</param>
        /// <param name="resolvedIncomingTime">解決済みの到着時刻</param>
        /// <returns>スケジュールを続行する場合は true</returns>
        private bool TryResolveJoinArrival(Node node, float incomingTime, Dictionary<string, JoinBuildState> joinStates, out float resolvedIncomingTime) {
            resolvedIncomingTime = incomingTime;
            if (!(node is JoinNode joinNode)) {
                return true;
            }

            if (!joinStates.TryGetValue(node.NodeId, out var joinState)) {
                throw new InvalidOperationException($"JoinNode '{node.NodeId}' has no build state");
            }

            if (joinState.IsScheduled) {
                return false;
            }

            joinState.ArrivedCount++;
            if (joinNode.JoinType == JoinType.Any) {
                joinState.JoinTime = incomingTime;
                joinState.IsScheduled = true;
                joinStates[node.NodeId] = joinState;
                resolvedIncomingTime = incomingTime;
                return true;
            }

            joinState.JoinTime = Mathf.Max(joinState.JoinTime, incomingTime);
            if (joinState.ArrivedCount < joinState.RequiredArrivalCount) {
                joinStates[node.NodeId] = joinState;
                return false;
            }

            joinState.IsScheduled = true;
            joinStates[node.NodeId] = joinState;
            resolvedIncomingTime = joinState.JoinTime;
            return true;
        }

        /// <summary>
        /// build 時点で展開する後続ノード ID 一覧を取得
        /// </summary>
        /// <param name="node">取得元ノード</param>
        /// <param name="graphSeed">グラフ全体のシード</param>
        /// <param name="seedSalt">評価単位を分けるシード salt</param>
        /// <param name="context">評価コンテキスト</param>
        /// <returns>展開対象の後続ノード ID 一覧</returns>
        private IReadOnlyList<string> GetNextNodeIds(Node node, int graphSeed, int seedSalt, IAnimationGraphContext context) {
            if (node is BranchNode branchNode) {
                var seed = CreateNodeSeed(graphSeed, node.NodeId, seedSalt);
                return branchNode.EvaluateNextNodeIds(seed, context);
            }

            return node.NextNodeIds;
        }

        /// <summary>
        /// 条件評価に依存しない物理的な後続ノード ID 一覧を収集
        /// </summary>
        /// <param name="node">取得元ノード</param>
        /// <param name="nodeIds">収集先のノード ID 一覧</param>
        private static void CollectPhysicalNextNodeIds(Node node, List<string> nodeIds) {
            nodeIds.Clear();
            var nextNodeIds = node.NextNodeIds;
            for (var i = 0; i < nextNodeIds.Count; i++) {
                nodeIds.Add(nextNodeIds[i]);
            }

            if (node is BranchNode branchNode) {
                for (var i = 0; i < branchNode.ExtensionPortCount; i++) {
                    var extensionNodeIds = branchNode.GetExtensionNodeIds(i);
                    for (var j = 0; j < extensionNodeIds.Count; j++) {
                        nodeIds.Add(extensionNodeIds[j]);
                    }
                }
            }
        }

        /// <summary>
        /// 時間値をスケジュール可能な値として検証
        /// </summary>
        /// <param name="value">検証対象の時間値</param>
        /// <param name="label">例外表示用の値名</param>
        /// <param name="node">例外対象ノード</param>
        /// <returns>検証済み時間値</returns>
        private float ValidateTimeValue(float value, string label, Node node) {
            if (float.IsNaN(value) || float.IsInfinity(value)) {
                throw new InvalidOperationException($"Node '{node.NodeId}' returned invalid {label}");
            }

            if (value < 0.0f) {
                throw new InvalidOperationException($"Node '{node.NodeId}' returned negative {label}");
            }

            return value;
        }

        /// <summary>
        /// ノード評価用のシードを生成
        /// </summary>
        /// <param name="graphSeed">グラフ全体のシード</param>
        /// <param name="nodeId">ノード ID</param>
        /// <param name="seedSalt">評価単位を分けるシード salt</param>
        /// <returns>ノード評価用のシード</returns>
        private int CreateNodeSeed(int graphSeed, string nodeId, int seedSalt) {
            unchecked {
                var hash = FnvOffsetBasis;

                void AppendByte(byte value) {
                    hash ^= value;
                    hash *= FnvPrime;
                }

                void AppendInt(int value) {
                    AppendByte((byte)value);
                    AppendByte((byte)(value >> 8));
                    AppendByte((byte)(value >> 16));
                    AppendByte((byte)(value >> 24));
                }

                void AppendString(string value) {
                    if (string.IsNullOrEmpty(value)) {
                        return;
                    }

                    for (var i = 0; i < value.Length; i++) {
                        var character = value[i];
                        AppendByte((byte)character);
                        AppendByte((byte)(character >> 8));
                    }
                }

                AppendInt(graphSeed);
                AppendString(nodeId);
                AppendInt(seedSalt);
                return (int)hash;
            }
        }
    }
}
