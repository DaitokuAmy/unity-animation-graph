using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// PlayTimelineAssetNode の EditMode テスト
    /// </summary>
    public sealed class PlayTimelineAssetNodeTests {
        private const string TargetKey = "director";
        private const string ActionTargetKeyPropertyName = "_targetKey";
        private const string TimelineAssetPropertyName = "_timelineAsset";

        private PlayTimelineAssetNode _node;
        private TimelineAsset _timelineAsset;
        private GameObject _gameObject;

        /// <summary>
        /// テストで生成した Unity Object を破棄
        /// </summary>
        [TearDown]
        public void TearDown() {
            DestroyObject(_node);
            DestroyObject(_timelineAsset);
            DestroyObject(_gameObject);
        }

        /// <summary>
        /// Enter で TimelineAsset と Manual 更新を PlayableDirector に反映する
        /// </summary>
        [Test]
        public void Enter_ConfiguresPlayableDirectorForManualTimeline() {
            _node = ScriptableObject.CreateInstance<PlayTimelineAssetNode>();
            _timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
            _gameObject = new GameObject("PlayableDirector");
            var playableDirector = _gameObject.AddComponent<PlayableDirector>();
            var context = new TestAnimationGraphContext();
            context.SetTarget(TargetKey, playableDirector);
            SetNodeProperties(_node, TargetKey, _timelineAsset);

            _node.ExecuteEnter(0, context);

            Assert.That(playableDirector.playableAsset, Is.SameAs(_timelineAsset));
            Assert.That(playableDirector.timeUpdateMode, Is.EqualTo(DirectorUpdateMode.Manual));
        }

        /// <summary>
        /// Evaluate で localTime を PlayableDirector の time に反映する
        /// </summary>
        [Test]
        public void Evaluate_AppliesLocalTimeToPlayableDirector() {
            _node = ScriptableObject.CreateInstance<PlayTimelineAssetNode>();
            _timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
            _gameObject = new GameObject("PlayableDirector");
            var playableDirector = _gameObject.AddComponent<PlayableDirector>();
            var context = new TestAnimationGraphContext();
            context.SetTarget(TargetKey, playableDirector);
            SetNodeProperties(_node, TargetKey, _timelineAsset);

            _node.ExecuteEnter(0, context);
            _node.ExecuteEvaluate(0, 1.25f, 2.0f, context);

            Assert.That(playableDirector.time, Is.EqualTo(1.25).Within(0.0001));
        }

        /// <summary>
        /// TimelineAsset 未設定の場合は何もしない
        /// </summary>
        [Test]
        public void Enter_ReturnsWhenTimelineAssetIsMissing() {
            _node = ScriptableObject.CreateInstance<PlayTimelineAssetNode>();
            _gameObject = new GameObject("PlayableDirector");
            var playableDirector = _gameObject.AddComponent<PlayableDirector>();
            var context = new TestAnimationGraphContext();
            context.SetTarget(TargetKey, playableDirector);
            SetNodeProperties(_node, TargetKey, null);

            _node.ExecuteEnter(0, context);

            Assert.IsNull(playableDirector.playableAsset);
        }

        /// <summary>
        /// PlayableDirector target を解決できない場合は何もしない
        /// </summary>
        [Test]
        public void Evaluate_ReturnsWhenPlayableDirectorIsMissing() {
            _node = ScriptableObject.CreateInstance<PlayTimelineAssetNode>();
            _timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
            var context = new TestAnimationGraphContext();
            SetNodeProperties(_node, TargetKey, _timelineAsset);

            Assert.DoesNotThrow(() => _node.ExecuteEvaluate(0, 1.25f, 2.0f, context));
        }

        /// <summary>
        /// PlayTimelineAssetNode の serialized property を設定
        /// </summary>
        /// <param name="node">設定対象ノード</param>
        /// <param name="targetKey">設定する target key</param>
        /// <param name="timelineAsset">設定する TimelineAsset</param>
        private static void SetNodeProperties(PlayTimelineAssetNode node, string targetKey, TimelineAsset timelineAsset) {
            var serializedNode = new SerializedObject(node);
            serializedNode.FindProperty(ActionTargetKeyPropertyName).stringValue = targetKey;
            serializedNode.FindProperty(TimelineAssetPropertyName).objectReferenceValue = timelineAsset;
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Unity Object を即時破棄
        /// </summary>
        /// <param name="target">破棄対象</param>
        private static void DestroyObject(UnityEngine.Object target) {
            if (target == null) {
                return;
            }

            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
