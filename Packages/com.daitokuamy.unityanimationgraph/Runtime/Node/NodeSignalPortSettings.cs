using System;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// Node が GraphView に表示する Signal Port の設定
    /// </summary>
    [Serializable]
    public struct NodeSignalPortSettings {
        [SerializeField]
        private bool _enterEnabled;
        [SerializeField]
        private bool _exitEnabled;

        /// <summary>Enter Signal Port を表示する場合は true</summary>
        public bool EnterEnabled => _enterEnabled;
        /// <summary>Exit Signal Port を表示する場合は true</summary>
        public bool ExitEnabled => _exitEnabled;

        /// <summary>
        /// NodeSignalPortSettings を作成
        /// </summary>
        /// <param name="enterEnabled">Enter Signal Port を表示する場合は true</param>
        /// <param name="exitEnabled">Exit Signal Port を表示する場合は true</param>
        public NodeSignalPortSettings(bool enterEnabled, bool exitEnabled) {
            _enterEnabled = enterEnabled;
            _exitEnabled = exitEnabled;
        }
    }
}
