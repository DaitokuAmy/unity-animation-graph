using System;
using UnityEditor;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Graph ウィンドウにアセットのインポート完了を通知する
    /// </summary>
    internal sealed class AnimationGraphAssetImportObserver : AssetPostprocessor {
        /// <summary>
        /// インポートされたアセットのパスを通知するイベント
        /// </summary>
        internal static event Action<string[]> AssetsImported;

        /// <summary>
        /// インポートされたアセットを通知する
        /// </summary>
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths) {
            if (importedAssets.Length > 0) {
                AssetsImported?.Invoke(importedAssets);
            }
        }
    }
}
