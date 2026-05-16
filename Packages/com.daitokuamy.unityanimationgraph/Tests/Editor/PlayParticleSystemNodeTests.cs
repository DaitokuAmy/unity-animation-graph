using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// PlayParticleSystemNode の EditMode テスト
    /// </summary>
    public sealed class PlayParticleSystemNodeTests {
        private const string TargetKey = "particle";
        private const string ActionTargetKeyPropertyName = "_targetKey";
        private const string DurationPropertyName = "_duration";

        private PlayParticleSystemNode _node;
        private GameObject _gameObject;
        private ParticleSystem _particleSystem;
        private TestAnimationGraphContext _context;

        /// <summary>
        /// テストごとの ParticleSystem と node を生成
        /// </summary>
        [SetUp]
        public void SetUp() {
            _node = ScriptableObject.CreateInstance<PlayParticleSystemNode>();
            _gameObject = new GameObject("ParticleSystem");
            _particleSystem = _gameObject.AddComponent<ParticleSystem>();
            _context = new TestAnimationGraphContext();
            _context.SetTarget(TargetKey, _particleSystem);
            SetNodeProperties(_node, TargetKey, 0.0f);
        }

        /// <summary>
        /// テストで生成した Unity Object を破棄
        /// </summary>
        [TearDown]
        public void TearDown() {
            DestroyObject(_node);
            DestroyObject(_gameObject);
        }

        /// <summary>
        /// duration が設定されている場合はノードの実行時間として使用する
        /// </summary>
        [Test]
        public void CalculateDuration_ReturnsConfiguredDuration() {
            SetNodeProperties(_node, TargetKey, 2.5f);

            var duration = ((INodeExecutor)_node).CalculateDuration(0, _context);

            Assert.That(duration, Is.EqualTo(2.5f).Within(0.0001f));
        }

        /// <summary>
        /// duration 未設定の場合は ParticleSystem の duration を実行時間として使用する
        /// </summary>
        [Test]
        public void CalculateDuration_UsesParticleSystemDurationWhenDurationIsZero() {
            SetParticleSystemDuration(_particleSystem, 3.75f);
            SetNodeProperties(_node, TargetKey, 0.0f);

            var duration = ((INodeExecutor)_node).CalculateDuration(0, _context);

            Assert.That(duration, Is.EqualTo(3.75f).Within(0.0001f));
        }

        /// <summary>
        /// Enter で seed を ParticleSystem に反映して 0 秒に初期化する
        /// </summary>
        [Test]
        public void Enter_AppliesSeedAndResetsSimulation() {
            ((INodeExecutor)_node).Enter(123, _context);

            Assert.IsFalse(_particleSystem.useAutoRandomSeed);
            Assert.That(_particleSystem.randomSeed, Is.EqualTo(123u));
            Assert.That(_particleSystem.time, Is.EqualTo(0.0f).Within(0.0001f));
        }

        /// <summary>
        /// Evaluate で localTime までシミュレーションする
        /// </summary>
        [Test]
        public void Evaluate_SimulatesParticleSystemAtLocalTime() {
            ((INodeExecutor)_node).Evaluate(456, 0.5f, 2.5f, _context);

            Assert.That(_particleSystem.time, Is.EqualTo(0.5f).Within(0.05f));
        }

        /// <summary>
        /// Enter で設定済み duration を ParticleSystem に反映する
        /// </summary>
        [Test]
        public void Enter_SyncsConfiguredDuration() {
            SetNodeProperties(_node, TargetKey, 2.5f);

            ((INodeExecutor)_node).Enter(456, _context);

            var main = _particleSystem.main;
            Assert.That(main.duration, Is.EqualTo(2.5f).Within(0.0001f));
        }

        /// <summary>
        /// Evaluate の連続呼び出しでは ParticleSystem に seed を設定しない
        /// </summary>
        [Test]
        public void Evaluate_DoesNotSetSeed() {
            ((INodeExecutor)_node).Enter(456, _context);
            ((INodeExecutor)_node).Evaluate(456, 0.5f, 2.5f, _context);

            ((INodeExecutor)_node).Evaluate(789, 1.0f, 2.5f, _context);

            Assert.That(_particleSystem.randomSeed, Is.EqualTo(456u));
            Assert.That(_particleSystem.time, Is.EqualTo(1.0f).Within(0.05f));
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// localTime 0 の Evaluate は ParticleSystem を停止して先頭状態へ戻す
        /// </summary>
        [Test]
        public void Evaluate_StopsParticleSystemWhenLocalTimeReturnsToStart() {
            ((INodeExecutor)_node).Enter(456, _context);
            ((INodeExecutor)_node).Evaluate(456, 0.5f, 2.5f, _context);

            ((INodeExecutor)_node).Evaluate(789, 0.0f, 2.5f, _context);

            Assert.That(_particleSystem.randomSeed, Is.EqualTo(456u));
            Assert.That(_particleSystem.time, Is.EqualTo(0.0f).Within(0.0001f));
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>
        /// localTime が duration を超えた場合は duration に丸めてシミュレーションする
        /// </summary>
        [Test]
        public void Evaluate_ClampsLocalTimeToDuration() {
            ((INodeExecutor)_node).Evaluate(456, 4.0f, 1.5f, _context);

            Assert.That(_particleSystem.time, Is.EqualTo(1.5f).Within(0.05f));
        }

        /// <summary>
        /// ParticleSystem target を解決できない場合は何もしない
        /// </summary>
        [Test]
        public void Evaluate_ReturnsWhenParticleSystemIsMissing() {
            var context = new TestAnimationGraphContext();

            Assert.DoesNotThrow(() => ((INodeExecutor)_node).Enter(0, context));
            Assert.DoesNotThrow(() => ((INodeExecutor)_node).Evaluate(0, 0.5f, 1.0f, context));
        }

        /// <summary>
        /// PlayParticleSystemNode の serialized property を設定
        /// </summary>
        /// <param name="node">設定対象ノード</param>
        /// <param name="targetKey">設定する target key</param>
        /// <param name="duration">設定する実行時間</param>
        private static void SetNodeProperties(PlayParticleSystemNode node, string targetKey, float duration) {
            var serializedNode = new SerializedObject(node);
            serializedNode.FindProperty(ActionTargetKeyPropertyName).stringValue = targetKey;
            serializedNode.FindProperty(DurationPropertyName).floatValue = duration;
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// ParticleSystem.main.duration を設定
        /// </summary>
        /// <param name="particleSystem">設定対象 ParticleSystem</param>
        /// <param name="duration">設定する duration</param>
        private static void SetParticleSystemDuration(ParticleSystem particleSystem, float duration) {
            var main = particleSystem.main;
            main.duration = duration;
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
