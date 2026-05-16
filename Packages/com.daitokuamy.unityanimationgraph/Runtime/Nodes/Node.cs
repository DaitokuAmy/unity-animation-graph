using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Animation Graph を構成するノードの基底クラス
    /// </summary>
    public abstract class Node : ScriptableObject, INodeExecutor {
        [SerializeField, Tooltip("グラフ内で一意なノード ID"), HideInInspector]
        private string _nodeId = string.Empty;
        [SerializeField, Tooltip("エディタ上のノード位置"), HideInInspector]
        private Vector2 _graphPosition;
        [SerializeField, Tooltip("後続ノード ID の一覧"), HideInInspector]
        private string[] _nextNodeIds = Array.Empty<string>();
        [SerializeField, Tooltip("ノード開始時に通知するシグナル一覧"), HideInInspector]
        private Signal[] _enterSignals = Array.Empty<Signal>();
        [SerializeField, Tooltip("ノード終了時に通知するシグナル一覧"), HideInInspector]
        private Signal[] _exitSignals = Array.Empty<Signal>();

        /// <summary>グラフ内で一意なノード ID</summary>
        public string NodeId => _nodeId;
        /// <summary>エディタ上のノード位置</summary>
        public Vector2 GraphPosition => _graphPosition;
        /// <summary>後続ノード ID の一覧</summary>
        public IReadOnlyList<string> NextNodeIds => _nextNodeIds ?? Array.Empty<string>();
        /// <summary>ノード開始時に通知するシグナル一覧</summary>
        public IReadOnlyList<Signal> EnterSignals => _enterSignals ?? Array.Empty<Signal>();
        /// <summary>ノード終了時に通知するシグナル一覧</summary>
        public IReadOnlyList<Signal> ExitSignals => _exitSignals ?? Array.Empty<Signal>();

        /// <inheritdoc/>
        float INodeExecutor.CalculateDuration(int seed, IAnimationGraphContext context) {
            return CalculateDuration(seed, context);
        }

        /// <inheritdoc/>
        float INodeExecutor.CalculateDelay(int seed, IAnimationGraphContext context) {
            return CalculateDelay(seed, context);
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
    }
}
