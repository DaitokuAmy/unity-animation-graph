using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// BranchNode の拡張 Port に接続されたノード ID 一覧
    /// </summary>
    [Serializable]
    internal sealed class BranchNodeIdList {
        [SerializeField, Tooltip("拡張 Branch Port の後続ノード ID 一覧"), HideInInspector]
        private string[] _nodeIds = Array.Empty<string>();

        /// <summary>拡張 Branch Port の後続ノード ID 一覧</summary>
        internal IReadOnlyList<string> NodeIds => _nodeIds ?? Array.Empty<string>();
    }

    /// <summary>
    /// 条件に応じて進行先を選ぶ分岐ノード
    /// </summary>
    public abstract class BranchNode : ControlNode {
        [SerializeField, Tooltip("拡張 Branch Port の後続ノード ID 一覧"), HideInInspector]
        private BranchNodeIdList[] _extensionNodeIds = Array.Empty<BranchNodeIdList>();

        /// <summary>主 Branch Port の後続ノード ID 一覧</summary>
        internal IReadOnlyList<string> PrimaryNodeIds => NextNodeIds;
        /// <summary>拡張 Branch Port の数</summary>
        internal int ExtensionPortCount => Math.Max(0, GetExtensionPortCount());
        /// <summary>Branch Port の合計数</summary>
        internal int PortCount => 1 + ExtensionPortCount;

        /// <summary>
        /// 評価後に進む後続ノード ID 一覧を取得
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        /// <returns>評価後に進む後続ノード ID 一覧</returns>
        internal IReadOnlyList<string> EvaluateNextNodeIds(int seed, IAnimationGraphContext context) {
            var portIndex = EvaluateBranchPortIndex(seed, context);
            if (portIndex < 0 || PortCount <= portIndex) {
                throw new InvalidOperationException($"{GetType().Name} '{NodeId}' returned invalid branch port index '{portIndex}'");
            }

            return GetBranchNodeIds(portIndex);
        }

        /// <summary>
        /// 指定 Branch Port の表示名を取得
        /// </summary>
        /// <param name="portIndex">取得する Branch Port index。0 は主 Branch Port</param>
        /// <returns>Branch Port の表示名</returns>
        internal string GetPortName(int portIndex) {
            if (portIndex < 0 || PortCount <= portIndex) {
                throw new ArgumentOutOfRangeException(nameof(portIndex));
            }

            return GetPortNameInternal(portIndex) ?? string.Empty;
        }

        /// <summary>
        /// 指定 Branch Port の後続ノード ID 一覧を取得
        /// </summary>
        /// <param name="portIndex">取得する Branch Port index。0 は主 Branch Port</param>
        /// <returns>Branch Port の後続ノード ID 一覧</returns>
        internal IReadOnlyList<string> GetBranchNodeIds(int portIndex) {
            if (portIndex < 0) {
                throw new ArgumentOutOfRangeException(nameof(portIndex));
            }

            if (portIndex == 0) {
                return NextNodeIds;
            }

            return GetExtensionNodeIds(portIndex - 1);
        }

        /// <summary>
        /// 指定拡張 Branch Port の後続ノード ID 一覧を取得
        /// </summary>
        /// <param name="extensionIndex">取得する拡張 Branch Port index</param>
        /// <returns>拡張 Branch Port の後続ノード ID 一覧</returns>
        internal IReadOnlyList<string> GetExtensionNodeIds(int extensionIndex) {
            if (extensionIndex < 0) {
                throw new ArgumentOutOfRangeException(nameof(extensionIndex));
            }

            if (ExtensionPortCount <= extensionIndex) {
                return Array.Empty<string>();
            }

            if (_extensionNodeIds != null && extensionIndex < _extensionNodeIds.Length && _extensionNodeIds[extensionIndex] != null) {
                return _extensionNodeIds[extensionIndex].NodeIds;
            }

            return Array.Empty<string>();
        }

        /// <inheritdoc/>
        protected override float CalculateDuration(int seed, IAnimationGraphContext context) {
            return 0.0f;
        }

        /// <inheritdoc/>
        protected override float CalculateDelay(int seed, IAnimationGraphContext context) {
            return 0.0f;
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, float localTime, float calculatedDuration, IAnimationGraphContext context) {
        }

        /// <summary>
        /// 評価後に進む Branch Port index を返す
        /// </summary>
        /// <param name="seed">評価に使用するシード</param>
        /// <param name="context">評価コンテキスト</param>
        /// <returns>Branch Port index。0 は主 Branch Port</returns>
        protected abstract int EvaluateBranchPortIndex(int seed, IAnimationGraphContext context);

        /// <summary>
        /// 拡張 Branch Port の数を取得
        /// </summary>
        /// <returns>拡張 Branch Port の数</returns>
        protected abstract int GetExtensionPortCount();

        /// <summary>
        /// Branch Port の表示名を取得
        /// </summary>
        /// <param name="portIndex">取得する Branch Port index。0 は主 Branch Port</param>
        /// <returns>Branch Port の表示名</returns>
        protected virtual string GetPortNameInternal(int portIndex) {
            return portIndex.ToString(CultureInfo.InvariantCulture);
        }
    }
}
