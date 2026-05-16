using System;
using System.Collections;
using System.Runtime.CompilerServices;

namespace UnityAnimationGraph {
    /// <summary>
    /// AnimationGraphPlayer の再生完了を待機する handle
    /// </summary>
    public readonly struct AnimationGraphPlayHandle : IEnumerator {
        private readonly AnimationGraphPlayer _player;
        private readonly int _version;

        /// <summary>再生 handle が有効な場合は true</summary>
        public bool IsValid => _player != null;
        /// <summary>再生 handle が完了済みの場合は true</summary>
        public bool IsDone => _player == null || _player.IsPlayDone(_version);
        /// <summary>再生が最後まで完了した場合は true</summary>
        public bool IsCompleted => _player != null && _player.IsPlayCompleted(_version);
        /// <summary>再生が中断された場合は true</summary>
        public bool IsInterrupted => _player == null || _player.IsPlayInterrupted(_version);
        /// <inheritdoc/>
        object IEnumerator.Current => null;

        /// <summary>
        /// AnimationGraphPlayHandle を生成
        /// </summary>
        /// <param name="player">再生元 player</param>
        /// <param name="version">再生 version</param>
        internal AnimationGraphPlayHandle(AnimationGraphPlayer player, int version) {
            _player = player;
            _version = version;
        }

        /// <summary>
        /// await 用の awaiter を取得
        /// </summary>
        /// <returns>再生完了 awaiter</returns>
        public Awaiter GetAwaiter() {
            return new Awaiter(this);
        }

        /// <inheritdoc/>
        bool IEnumerator.MoveNext() {
            return !IsDone;
        }

        /// <inheritdoc/>
        void IEnumerator.Reset() {
        }

        /// <summary>
        /// 再生完了時に実行する continuation を登録
        /// </summary>
        /// <param name="continuation">再開処理</param>
        private void RegisterContinuation(Action continuation) {
            if (continuation == null) {
                throw new ArgumentNullException(nameof(continuation));
            }

            if (_player == null) {
                continuation();
                return;
            }

            _player.RegisterPlayContinuation(_version, continuation);
        }

        /// <summary>
        /// AnimationGraphPlayHandle の awaiter
        /// </summary>
        public readonly struct Awaiter : INotifyCompletion {
            private readonly AnimationGraphPlayHandle _handle;

            /// <summary>await が完了済みの場合は true</summary>
            public bool IsCompleted => _handle.IsDone;

            /// <summary>
            /// Awaiter を生成
            /// </summary>
            /// <param name="handle">待機対象 handle</param>
            internal Awaiter(AnimationGraphPlayHandle handle) {
                _handle = handle;
            }

            /// <summary>
            /// await の戻り値を取得
            /// </summary>
            /// <returns>自然完了した場合は true</returns>
            public bool GetResult() {
                return _handle.IsCompleted;
            }

            /// <summary>
            /// await の continuation を登録
            /// </summary>
            /// <param name="continuation">再開処理</param>
            public void OnCompleted(Action continuation) {
                _handle.RegisterContinuation(continuation);
            }
        }
    }
}
