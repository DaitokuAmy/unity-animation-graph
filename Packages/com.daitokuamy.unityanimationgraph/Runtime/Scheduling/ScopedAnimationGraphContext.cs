using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// target 反復 scope を基底 context に追加する評価 context
    /// </summary>
    internal sealed class ScopedAnimationGraphContext : IAnimationGraphContext {
        private readonly IAnimationGraphContext _baseContext;
        private readonly CollectionSnapshotRegistry _collectionSnapshots;
        private readonly IterationScope _scope;

        /// <summary>
        /// ScopedAnimationGraphContext を生成
        /// </summary>
        /// <param name="baseContext">委譲先 context</param>
        /// <param name="collectionSnapshots">target collection snapshot registry</param>
        /// <param name="scope">現在の反復 scope</param>
        public ScopedAnimationGraphContext(IAnimationGraphContext baseContext, CollectionSnapshotRegistry collectionSnapshots, IterationScope scope) {
            _baseContext = baseContext;
            _collectionSnapshots = collectionSnapshots;
            _scope = scope;
        }

        /// <inheritdoc/>
        public T GetTarget<T>(string key) where T : Component {
            return _baseContext.GetTarget<T>(key);
        }

        /// <inheritdoc/>
        public bool TryGetTarget<T>(string key, out T target) where T : Component {
            return _baseContext.TryGetTarget(key, out target);
        }

        /// <inheritdoc/>
        public bool TryGetTargets<T>(string key, out IReadOnlyList<T> targets) where T : Component {
            return _baseContext.TryGetTargets(key, out targets);
        }

        /// <summary>
        /// scope node ID に対応する現在要素の取得を試行
        /// </summary>
        /// <param name="scopeNodeId">scope を提供する node ID</param>
        /// <param name="targetKey">取得する collection target key</param>
        /// <param name="target">取得した現在要素</param>
        /// <typeparam name="T">取得する Component 型</typeparam>
        /// <returns>scope が存在し、現在要素が指定型または null の場合は true</returns>
        public bool TryGetScopedTarget<T>(string scopeNodeId, string targetKey, out T target) where T : Component {
            for (var scope = _scope; scope != null; scope = scope.Parent) {
                if (scope.ScopeNodeId != scopeNodeId) {
                    continue;
                }

                return _collectionSnapshots.TryGetItem(targetKey, scope.IterationIndex, out target);
            }

            target = null;
            return false;
        }

        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out bool value) => _baseContext.TryGetBlackboardValue(key, out value);
        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out int value) => _baseContext.TryGetBlackboardValue(key, out value);
        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out float value) => _baseContext.TryGetBlackboardValue(key, out value);
        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out string value) => _baseContext.TryGetBlackboardValue(key, out value);
        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out Vector2 value) => _baseContext.TryGetBlackboardValue(key, out value);
        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out Vector3 value) => _baseContext.TryGetBlackboardValue(key, out value);
        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out Color value) => _baseContext.TryGetBlackboardValue(key, out value);
        /// <inheritdoc/>
        public bool TryGetBlackboardValue(string key, out Vector4 value) => _baseContext.TryGetBlackboardValue(key, out value);
    }
}
