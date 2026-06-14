using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Editor preview 用の player と schedule を構築する factory。
    /// </summary>
    internal static class AnimationGraphEditorPreviewPlayerFactory {
        /// <summary>
        /// Editor preview 用の player を生成し、schedule まで構築する。
        /// </summary>
        /// <param name="graphAsset">Preview 対象の GraphAsset</param>
        /// <param name="rootGameObject">Preview root の GameObject</param>
        /// <param name="targetBindings">Runner に保存されている target binding 一覧</param>
        /// <param name="player">構築済み player</param>
        /// <param name="context">構築済み context</param>
        /// <param name="message">失敗時の message</param>
        /// <returns>構築できた場合は true</returns>
        public static bool TryCreate(
            AnimationGraphAsset graphAsset,
            GameObject rootGameObject,
            IReadOnlyList<TargetBinding> targetBindings,
            out AnimationGraphPlayer player,
            out AnimationGraphEditorPreviewContext context,
            out string message) {
            player = null;
            if (!TryCreateContext(graphAsset, rootGameObject, targetBindings, out context, out message)) {
                return false;
            }

            try {
                player = new AnimationGraphPlayer();
                player.SetContext(context);
                player.SetGraph(graphAsset);
                player.RebuildSchedule();
                message = null;
                return true;
            }
            catch (Exception exception) {
                player = null;
                context = null;
                message = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// Editor preview 表示用の schedule だけを構築する。
        /// </summary>
        /// <param name="graphAsset">Preview 対象の GraphAsset</param>
        /// <param name="rootGameObject">Preview root の GameObject</param>
        /// <param name="targetBindings">Runner に保存されている target binding 一覧</param>
        /// <param name="maxScheduledNodeCount">構築する schedule node 数の上限</param>
        /// <param name="schedule">構築済み schedule</param>
        /// <param name="message">失敗時の message</param>
        /// <returns>構築できた場合は true</returns>
        public static bool TryBuildSchedule(
            AnimationGraphAsset graphAsset,
            GameObject rootGameObject,
            IReadOnlyList<TargetBinding> targetBindings,
            int maxScheduledNodeCount,
            out AnimationGraphSchedule schedule,
            out string message) {
            schedule = null;
            if (!TryCreateContext(graphAsset, rootGameObject, targetBindings, out var context, out message)) {
                return false;
            }

            try {
                var scheduler = new AnimationGraphScheduler();
                schedule = scheduler.Build(graphAsset, context, null, maxScheduledNodeCount);
                message = null;
                return true;
            }
            catch (Exception exception) {
                schedule = null;
                message = exception.Message;
                return false;
            }
        }

        private static bool TryCreateContext(
            AnimationGraphAsset graphAsset,
            GameObject rootGameObject,
            IReadOnlyList<TargetBinding> targetBindings,
            out AnimationGraphEditorPreviewContext context,
            out string message) {
            context = null;
            if (graphAsset == null) {
                message = "GraphAsset is not selected";
                return false;
            }

            if (rootGameObject == null) {
                message = "Select a GameObject to preview";
                return false;
            }

            try {
                context = new AnimationGraphEditorPreviewContext(graphAsset, rootGameObject, targetBindings);
                message = null;
                return true;
            }
            catch (Exception exception) {
                context = null;
                message = exception.Message;
                return false;
            }
        }
    }
}
