using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Shared utility methods for AnimationGraph property drawers.
    /// </summary>
    internal static class AnimationGraphPropertyDrawerUtility {
        /// <summary>
        /// Finds the owning AnimationGraphAsset for all inspected targets.
        /// </summary>
        /// <param name="targets">Inspected serialized targets</param>
        /// <returns>AnimationGraphAsset when every target belongs to the same graph asset; otherwise null</returns>
        public static AnimationGraphAsset FindGraphAsset(IReadOnlyList<Object> targets) {
            AnimationGraphAsset graphAsset = null;
            for (var i = 0; i < targets.Count; i++) {
                if (!TryGetGraphAsset(targets[i], out var currentGraphAsset)) {
                    continue;
                }

                if (graphAsset == null) {
                    graphAsset = currentGraphAsset;
                    continue;
                }

                if (graphAsset != currentGraphAsset) {
                    return null;
                }
            }

            return graphAsset;
        }

        private static bool TryGetGraphAsset(Object target, out AnimationGraphAsset graphAsset) {
            if (target is AnimationGraphAsset currentGraphAsset) {
                graphAsset = currentGraphAsset;
                return true;
            }

            if (target is not Node) {
                graphAsset = null;
                return false;
            }

            var assetPath = AssetDatabase.GetAssetPath(target);
            if (string.IsNullOrEmpty(assetPath)) {
                graphAsset = null;
                return false;
            }

            graphAsset = AssetDatabase.LoadAssetAtPath<AnimationGraphAsset>(assetPath);
            return graphAsset != null;
        }
    }
}
