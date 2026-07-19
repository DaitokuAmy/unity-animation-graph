using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// schedule build 時点の target collection snapshot を共有する registry
    /// </summary>
    internal sealed class CollectionSnapshotRegistry {
        private readonly Dictionary<string, IReadOnlyList<Component>> _snapshots = new(StringComparer.Ordinal);
        private readonly IAnimationGraphContext _context;

        /// <summary>
        /// CollectionSnapshotRegistry を生成
        /// </summary>
        public CollectionSnapshotRegistry(IAnimationGraphContext context) {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// collection snapshot の取得を試行
        /// </summary>
        public bool TryGetSnapshot(string targetKey, out IReadOnlyList<Component> snapshot) {
            if (string.IsNullOrEmpty(targetKey)) {
                snapshot = Array.Empty<Component>();
                return false;
            }

            if (_snapshots.TryGetValue(targetKey, out snapshot)) {
                return true;
            }

            if (!_context.TryGetTargets<Component>(targetKey, out var targets)) {
                snapshot = Array.Empty<Component>();
                return false;
            }

            var copiedTargets = new Component[targets.Count];
            for (var i = 0; i < targets.Count; i++) {
                copiedTargets[i] = targets[i];
            }

            snapshot = copiedTargets;
            _snapshots.Add(targetKey, snapshot);
            return true;
        }

        /// <summary>
        /// collection の index に対応する要素の取得を試行
        /// </summary>
        public bool TryGetItem<T>(string targetKey, int index, out T target) where T : Component {
            if (!TryGetSnapshot(targetKey, out var snapshot)) {
                target = null;
                return false;
            }

            if (index < 0 || index >= snapshot.Count || snapshot[index] == null) {
                target = null;
                return true;
            }

            if (snapshot[index] is T typedTarget) {
                target = typedTarget;
                return true;
            }

            target = null;
            return false;
        }
    }
}
