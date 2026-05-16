using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Animation Graph を通過した瞬間を外部へ通知するシグナルの基底クラス
    /// </summary>
    public abstract class Signal : ScriptableObject, ISignalExecutor {
        [SerializeField, Tooltip("グラフ内で一意なシグナル ID"), HideInInspector]
        private string _signalId = string.Empty;
        [SerializeField, Tooltip("エディタ上のシグナル位置"), HideInInspector]
        private Vector2 _graphPosition;

        /// <summary>グラフ内で一意なシグナル ID</summary>
        public string SignalId => _signalId;
        /// <summary>エディタ上のシグナル位置</summary>
        public Vector2 GraphPosition => _graphPosition;

        /// <inheritdoc/>
        void ISignalExecutor.Dispatch(int seed, IAnimationGraphContext context) {
            Dispatch(seed, context);
        }

        /// <summary>
        /// シグナル通知時の処理を行う
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        protected virtual void Dispatch(int seed, IAnimationGraphContext context) {
        }
    }
}
