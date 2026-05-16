using UnityEditor;
using UnityEditor.Callbacks;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// AnimationGraphAsset の open 操作を Animation Graph EditorWindow に接続するクラス
    /// </summary>
    internal static class AnimationGraphAssetOpenHandler {
        /// <summary>
        /// Project Window で Asset が開かれたときの処理
        /// </summary>
        /// <param name="instanceId">開かれた Asset の instance ID</param>
        /// <param name="line">開かれた行番号</param>
        /// <returns>AnimationGraphAsset を処理した場合は true</returns>
        [OnOpenAsset]
        private static bool OnOpenAsset(int instanceId, int line) {
            if (EditorUtility.InstanceIDToObject(instanceId) is not AnimationGraphAsset graphAsset) {
                return false;
            }

            AnimationGraphEditorWindow.Open(graphAsset);
            return true;
        }
    }
}
