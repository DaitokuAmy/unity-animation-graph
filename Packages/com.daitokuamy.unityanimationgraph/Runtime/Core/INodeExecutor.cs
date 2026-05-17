using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Animation Graph ノードの時間計算と評価を行うインターフェース
    /// </summary>
    public interface INodeExecutor {
        /// <summary>
        /// ノードの実行時間を計算
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        /// <returns>ノードの実行時間</returns>
        float CalculateDuration(int seed, IAnimationGraphContext context);

        /// <summary>
        /// ノードの開始遅延を計算
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        /// <returns>ノードの開始遅延</returns>
        float CalculateDelay(int seed, IAnimationGraphContext context);

        /// <summary>
        /// Preview 再生時に AnimationMode へ登録するプロパティを取得
        /// </summary>
        /// <param name="context">評価コンテキスト</param>
        /// <returns>登録対象の Component と SerializedProperty path の一覧</returns>
        IEnumerable<(Component Component, string PropertyPath)> GetPreviewProperties(IAnimationGraphContext context);

        /// <summary>
        /// ノード開始時の処理を行う
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        void Enter(int seed, IAnimationGraphContext context);

        /// <summary>
        /// ノードを評価
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="localTime">ノード開始時刻からの経過時間</param>
        /// <param name="calculatedDuration">計算済みの実行時間</param>
        /// <param name="context">評価コンテキスト</param>
        void Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context);

        /// <summary>
        /// ノード終了時の処理を行う
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        void Exit(int seed, IAnimationGraphContext context);

        /// <summary>
        /// 実行中のノードをキャンセル
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        void Cancel(int seed, IAnimationGraphContext context);
    }
}
