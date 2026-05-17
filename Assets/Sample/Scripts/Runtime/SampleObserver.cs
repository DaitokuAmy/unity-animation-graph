using UnityAnimationGraph;
using UnityEngine;

namespace Sample {
    /// <summary>
    /// Event監視役
    /// </summary>
    public sealed class SampleObserver : MonoBehaviour {
        [SerializeField, Tooltip("監視に使うRunner")]
        private AnimationGraphRunner _runner;

        private void OnEnable() {
            if (_runner == null) {
                return;
            }

            _runner.SubscribeSignal<SampleLogSignal>(OnSampleLogSignal);
        }

        private void OnDisable() {
            if (_runner == null) {
                return;
            }

            _runner.ClearSignalSubscriptions();
        }

        private void OnSampleLogSignal(SampleLogSignal signal) {
            Debug.Log($"Observed Signal: {signal.Message}", this);
        }
    }
}
