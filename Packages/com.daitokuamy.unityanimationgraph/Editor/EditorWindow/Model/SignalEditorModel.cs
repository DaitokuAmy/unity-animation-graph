using System;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// Editor MVP の Model として Signal の参照情報を提供するクラス
    /// </summary>
    public sealed class SignalEditorModel {
        private readonly Signal _signal;

        /// <summary>グラフ内で一意なシグナル ID</summary>
        public string SignalId => _signal.SignalId;
        /// <summary>シグナル型</summary>
        public Type SignalType => _signal.GetType();
        /// <summary>シグナル表示名</summary>
        public string DisplayName => SignalMetadata.GetDisplayName(SignalType);
        /// <summary>シグナル名</summary>
        public string Name => _signal.name ?? string.Empty;
        /// <summary>エディタ上のシグナル位置</summary>
        public Vector2 GraphPosition => _signal.GraphPosition;
        /// <summary>参照元の Signal</summary>
        internal Signal Signal => _signal;

        /// <summary>
        /// SignalEditorModel を作成
        /// </summary>
        /// <param name="signal">参照する Signal</param>
        internal SignalEditorModel(Signal signal) {
            _signal = signal ?? throw new ArgumentNullException(nameof(signal));
        }

        /// <summary>
        /// エディタ上のシグナル位置を設定
        /// </summary>
        /// <param name="graphPosition">エディタ上のシグナル位置</param>
        public void SetGraphPosition(Vector2 graphPosition) {
            AnimationGraphAssetUtility.SetSignalGraphPosition(_signal, graphPosition);
        }
    }
}
