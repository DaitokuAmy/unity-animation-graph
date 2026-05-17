using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Animation Graph 全体の永続データを保持するアセット
    /// </summary>
    [CreateAssetMenu(fileName = "AnimationGraph", menuName = "Unity Animation Graph/Animation Graph")]
    public sealed class AnimationGraphAsset : ScriptableObject {
        [SerializeField, Tooltip("GraphAsset を識別する Unity アセット GUID"), HideInInspector]
        private string _assetGuid = string.Empty;
        [SerializeField, Tooltip("ノード評価に使用するグラフ全体のシード"), HideInInspector]
        private int _graphSeed;
        [SerializeField, Tooltip("グラフシードをランダムに生成するか"), HideInInspector]
        private bool _randomSeed;
        [SerializeField, Tooltip("再生開始に使用する StartNode の ID"), HideInInspector]
        private string _startNodeId = string.Empty;
        [SerializeField, Tooltip("グラフに含まれるノード一覧"), HideInInspector]
        private Node[] _nodes = Array.Empty<Node>();
        [SerializeField, Tooltip("Runner がバインドする target key 定義一覧"), HideInInspector]
        private TargetDefinition[] _targetDefinitions = Array.Empty<TargetDefinition>();
        [SerializeField, Tooltip("Blackboard key と初期値の定義一覧"), HideInInspector]
        private BlackboardDefinition[] _blackboardDefinitions = Array.Empty<BlackboardDefinition>();

        /// <summary>GraphAsset の asset GUID</summary>
        public string AssetGuid => _assetGuid ?? string.Empty;
        /// <summary>グラフ全体のシード</summary>
        public int GraphSeed => _randomSeed ? CreateRandomSeed() : _graphSeed;
        /// <summary>グラフシードをランダムに生成するか</summary>
        public bool RandomSeed => _randomSeed;
        /// <summary>開始ノード ID</summary>
        public string StartNodeId => _startNodeId;
        /// <summary>グラフに含まれるノード一覧</summary>
        public IReadOnlyList<Node> Nodes => _nodes ?? Array.Empty<Node>();
        /// <summary>グラフが要求する target key 定義一覧</summary>
        public IReadOnlyList<TargetDefinition> TargetDefinitions => _targetDefinitions ?? Array.Empty<TargetDefinition>();
        /// <summary>グラフが要求する Blackboard key 定義一覧</summary>
        public IReadOnlyList<BlackboardDefinition> BlackboardDefinitions => _blackboardDefinitions ?? Array.Empty<BlackboardDefinition>();

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

        /// <summary>
        /// 指定した key に対応する target 定義の取得を試行
        /// </summary>
        /// <param name="key">取得する target key</param>
        /// <param name="definition">取得した target 定義</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetTargetDefinition(string key, out TargetDefinition definition) {
            if (string.IsNullOrEmpty(key)) {
                definition = default;
                return false;
            }

            var definitions = _targetDefinitions ?? Array.Empty<TargetDefinition>();
            for (var i = 0; i < definitions.Length; i++) {
                var current = definitions[i];
                if (current.Key != key) {
                    continue;
                }

                definition = current;
                return true;
            }

            definition = default;
            return false;
        }

        /// <summary>
        /// 指定した key に対応する Blackboard 定義の取得を試行
        /// </summary>
        /// <param name="key">取得する Blackboard key</param>
        /// <param name="definition">取得した Blackboard 定義</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetBlackboardDefinition(string key, out BlackboardDefinition definition) {
            if (string.IsNullOrEmpty(key)) {
                definition = default;
                return false;
            }

            var definitions = _blackboardDefinitions ?? Array.Empty<BlackboardDefinition>();
            for (var i = 0; i < definitions.Length; i++) {
                var current = definitions[i];
                if (current.Key != key) {
                    continue;
                }

                definition = current;
                return true;
            }

            definition = default;
            return false;
        }

        /// <summary>
        /// 指定した key に対応する bool Blackboard default value の取得を試行
        /// </summary>
        /// <param name="key">取得する Blackboard key</param>
        /// <param name="value">取得した bool default value</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetBlackboardDefaultValue(string key, out bool value) {
            if (TryGetBlackboardDefinition(key, out var definition) && definition.TryGetDefaultValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 指定した key に対応する int Blackboard default value の取得を試行
        /// </summary>
        /// <param name="key">取得する Blackboard key</param>
        /// <param name="value">取得した int default value</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetBlackboardDefaultValue(string key, out int value) {
            if (TryGetBlackboardDefinition(key, out var definition) && definition.TryGetDefaultValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 指定した key に対応する float Blackboard default value の取得を試行
        /// </summary>
        /// <param name="key">取得する Blackboard key</param>
        /// <param name="value">取得した float default value</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetBlackboardDefaultValue(string key, out float value) {
            if (TryGetBlackboardDefinition(key, out var definition) && definition.TryGetDefaultValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 指定した key に対応する string Blackboard default value の取得を試行
        /// </summary>
        /// <param name="key">取得する Blackboard key</param>
        /// <param name="value">取得した string default value</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetBlackboardDefaultValue(string key, out string value) {
            if (TryGetBlackboardDefinition(key, out var definition) && definition.TryGetDefaultValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 指定した key に対応する Vector2 Blackboard default value の取得を試行
        /// </summary>
        /// <param name="key">取得する Blackboard key</param>
        /// <param name="value">取得した Vector2 default value</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetBlackboardDefaultValue(string key, out Vector2 value) {
            if (TryGetBlackboardDefinition(key, out var definition) && definition.TryGetDefaultValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 指定した key に対応する Vector3 Blackboard default value の取得を試行
        /// </summary>
        /// <param name="key">取得する Blackboard key</param>
        /// <param name="value">取得した Vector3 default value</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetBlackboardDefaultValue(string key, out Vector3 value) {
            if (TryGetBlackboardDefinition(key, out var definition) && definition.TryGetDefaultValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 指定した key に対応する Color Blackboard default value の取得を試行
        /// </summary>
        /// <param name="key">取得する Blackboard key</param>
        /// <param name="value">取得した Color default value</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetBlackboardDefaultValue(string key, out Color value) {
            if (TryGetBlackboardDefinition(key, out var definition) && definition.TryGetDefaultValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 指定した key に対応する Vector4 Blackboard default value の取得を試行
        /// </summary>
        /// <param name="key">取得する Blackboard key</param>
        /// <param name="value">取得した Vector4 default value</param>
        /// <returns>取得できた場合は true</returns>
        public bool TryGetBlackboardDefaultValue(string key, out Vector4 value) {
            if (TryGetBlackboardDefinition(key, out var definition) && definition.TryGetDefaultValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        private static int CreateRandomSeed() {
            return BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0);
        }
    }
}
