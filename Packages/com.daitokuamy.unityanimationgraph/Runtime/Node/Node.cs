using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Animation Graph を構成するノードの基底クラス
    /// </summary>
    public abstract class Node : ScriptableObject, INodeExecutor, ISerializationCallbackReceiver {
        [SerializeField, Tooltip("GraphView のタイトルとして表示する名前。空の場合はノード型の表示名を使用する")]
        private string _displayName = string.Empty;
        [SerializeField, Tooltip("GraphView に表示する Signal Port")]
        private NodeSignalPortSettings _signalPorts;
        [SerializeField, Tooltip("グラフ内で一意なノード ID"), HideInInspector]
        private string _nodeId = string.Empty;
        [SerializeField, Tooltip("エディタ上のノード位置"), HideInInspector]
        private Vector2 _graphPosition;
        [SerializeField, Tooltip("後続ノード ID の一覧"), HideInInspector]
        private string[] _nextNodeIds = Array.Empty<string>();
        [SerializeField, HideInInspector]
        private bool _enableEnterSignalPort;
        [SerializeField, HideInInspector]
        private bool _enableExitSignalPort;
        [SerializeField, HideInInspector]
        private int _signalPortSettingsVersion;
        [SerializeField, Tooltip("ノード開始時に通知するシグナル一覧"), HideInInspector]
        private Signal[] _enterSignals = Array.Empty<Signal>();
        [SerializeField, Tooltip("ノード終了時に通知するシグナル一覧"), HideInInspector]
        private Signal[] _exitSignals = Array.Empty<Signal>();

        /// <summary>GraphView 上の表示名</summary>
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? NodeMetadata.GetDisplayName(GetType()) : _displayName;
        /// <summary>グラフ内で一意なノード ID</summary>
        internal string NodeId => _nodeId;
        /// <summary>エディタ上のノード位置</summary>
        internal Vector2 GraphPosition => _graphPosition;
        /// <summary>後続ノード ID の一覧</summary>
        internal IReadOnlyList<string> NextNodeIds => _nextNodeIds ?? Array.Empty<string>();
        /// <summary>Enter シグナル用の出力 Port を GraphView に表示する場合は true</summary>
        internal bool EnableEnterSignalPort => _signalPorts.EnterEnabled || (_signalPortSettingsVersion == 0 && _enableEnterSignalPort);
        /// <summary>Exit シグナル用の出力 Port を GraphView に表示する場合は true</summary>
        internal bool EnableExitSignalPort => _signalPorts.ExitEnabled || (_signalPortSettingsVersion == 0 && _enableExitSignalPort);
        /// <summary>ノード開始時に通知するシグナル一覧</summary>
        internal IReadOnlyList<Signal> EnterSignals => _enterSignals ?? Array.Empty<Signal>();
        /// <summary>ノード終了時に通知するシグナル一覧</summary>
        internal IReadOnlyList<Signal> ExitSignals => _exitSignals ?? Array.Empty<Signal>();

        /// <inheritdoc/>
        void ISerializationCallbackReceiver.OnBeforeSerialize() {
        }

        /// <inheritdoc/>
        void ISerializationCallbackReceiver.OnAfterDeserialize() {
            if (_signalPortSettingsVersion != 0) {
                return;
            }

            _signalPorts = new NodeSignalPortSettings(
                _signalPorts.EnterEnabled || _enableEnterSignalPort,
                _signalPorts.ExitEnabled || _enableExitSignalPort);
            _enableEnterSignalPort = false;
            _enableExitSignalPort = false;
            _signalPortSettingsVersion = 1;
        }

        /// <inheritdoc/>
        float INodeExecutor.CalculateDuration(int seed, IAnimationGraphContext context) {
            return CalculateDuration(seed, context);
        }

        /// <inheritdoc/>
        float INodeExecutor.CalculateDelay(int seed, IAnimationGraphContext context) {
            return CalculateDelay(seed, context);
        }

        /// <inheritdoc/>
        IEnumerable<(UnityEngine.Object Target, string PropertyPath)> INodeExecutor.GetPreviewProperties(IAnimationGraphContext context) {
            return GetPreviewProperties(context);
        }

        /// <inheritdoc/>
        void INodeExecutor.Enter(int seed, IAnimationGraphContext context) {
            Enter(seed, context);
        }

        /// <inheritdoc/>
        void INodeExecutor.Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context) {
            Evaluate(seed, localTime, calculatedDuration, context);
        }

        /// <inheritdoc/>
        void INodeExecutor.Exit(int seed, IAnimationGraphContext context) {
            Exit(seed, context);
        }

        /// <inheritdoc/>
        void INodeExecutor.Cancel(int seed, IAnimationGraphContext context) {
            Cancel(seed, context);
        }

        /// <summary>
        /// ノードの実行時間を計算
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        /// <returns>ノードの実行時間</returns>
        protected abstract float CalculateDuration(int seed, IAnimationGraphContext context);

        /// <summary>
        /// ノードの開始遅延を計算
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        /// <returns>ノードの開始遅延</returns>
        protected abstract float CalculateDelay(int seed, IAnimationGraphContext context);

        /// <summary>
        /// Preview 再生時に AnimationMode へ登録するプロパティを取得
        /// </summary>
        /// <param name="context">評価コンテキスト</param>
        /// <returns>登録対象の Object と SerializedProperty path の一覧</returns>
        protected virtual IEnumerable<(UnityEngine.Object Target, string PropertyPath)> GetPreviewProperties(IAnimationGraphContext context) {
            return Array.Empty<(UnityEngine.Object Target, string PropertyPath)>();
        }

        /// <summary>
        /// ノードを評価
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="localTime">ノード開始時刻からの経過時間</param>
        /// <param name="calculatedDuration">計算済みの実行時間</param>
        /// <param name="context">評価コンテキスト</param>
        protected abstract void Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context);

        /// <summary>
        /// ノード開始時の処理を行う
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        protected virtual void Enter(int seed, IAnimationGraphContext context) {
        }

        /// <summary>
        /// ノード終了時の処理を行う
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        protected virtual void Exit(int seed, IAnimationGraphContext context) {
        }

        /// <summary>
        /// 実行中のノードをキャンセル
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        protected virtual void Cancel(int seed, IAnimationGraphContext context) {
        }

        /// <summary>
        /// Node の設定検証エラーメッセージを取得
        /// </summary>
        /// <param name="context">検証コンテキスト</param>
        /// <returns>検証エラーメッセージ。エラーがない場合は空文字列</returns>
        protected virtual string Validate(NodeValidationContext context) {
            return string.Empty;
        }

        /// <summary>
        /// Node の設定検証エラーメッセージを取得
        /// </summary>
        /// <param name="context">検証コンテキスト</param>
        /// <returns>検証エラーメッセージ。エラーがない場合は空文字列</returns>
        internal string GetValidationMessage(NodeValidationContext context) {
            return Validate(context);
        }
    }
}
