using System;
using UnityAnimationGraph;
using UnityEngine;

namespace Sample {
    /// <summary>
    /// Event監視役
    /// </summary>
    public sealed class SampleObserver : MonoBehaviour {
        [SerializeField, Tooltip("監視に使うRunner")]
        private AnimationGraphRunner _runner;

        private IDisposable _signalSubscription;

        private void OnEnable() {
            if (_runner == null) {
                return;
            }

            _signalSubscription = _runner.SubscribeSignal<SampleLogSignal>(OnSampleLogSignal);
        }

        private void OnDisable() {
            _signalSubscription?.Dispose();
            _signalSubscription = null;
        }

        private void OnSampleLogSignal(SignalContext<SampleLogSignal> context) {
            Debug.Log($"Observed Signal: {context.Signal.Message}", this);
        }
    }
}
