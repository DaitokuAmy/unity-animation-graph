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

        /// <summary>グラフ内で一意なノード ID</summary>
        public string NodeId => _nodeId;
        /// <summary>エディタ上のノード位置</summary>
        public Vector2 GraphPosition => _graphPosition;
        /// <summary>後続ノード ID の一覧</summary>
        public IReadOnlyList<string> NextNodeIds => _nextNodeIds ?? Array.Empty<string>();

        /// <inheritdoc/>
        float INodeExecutor.CalculateDuration(int seed, IAnimationGraphContext context) {
            return CalculateDuration(seed, context);
        }

        /// <inheritdoc/>
        float INodeExecutor.CalculateDelay(int seed, IAnimationGraphContext context) {
            return CalculateDelay(seed, context);
        }

        /// <inheritdoc/>
        void INodeExecutor.BeginPlayback(int seed, IAnimationGraphContext context) {
            BeginPlayback(seed, context);
        }

        /// <inheritdoc/>
        void INodeExecutor.Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context) {
            Evaluate(seed, localTime, calculatedDuration, context);
        }

        /// <inheritdoc/>
        void INodeExecutor.EndPlayback(int seed, IAnimationGraphContext context) {
            EndPlayback(seed, context);
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
        /// 再生開始時の初期化を行う
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        protected virtual void BeginPlayback(int seed, IAnimationGraphContext context) {
        }

        /// <summary>
        /// 再生終了時の後処理を行う
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        protected virtual void EndPlayback(int seed, IAnimationGraphContext context) {
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
