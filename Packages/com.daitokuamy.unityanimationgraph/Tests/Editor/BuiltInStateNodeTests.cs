using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Tests {
    /// <summary>
    /// Built-in state node の EditMode テスト
    /// </summary>
    public sealed class BuiltInStateNodeTests {
        private const string TargetKey = "target";
        private const string ActionTargetKeyPropertyName = "_targetKey";
        private const string ActivePropertyName = "_active";
        private const string EnabledPropertyName = "_enabled";

        /// <summary>
        /// SetGameObjectActiveNode は target の GameObject activeSelf を設定する
        /// </summary>
        [Test]
        public void GameObjectActiveNode_EvaluatesActiveSelf() {
            var node = ScriptableObject.CreateInstance<SetGameObjectActiveNode>();
            var gameObject = new GameObject("Target");
            try {
                var context = CreateContext(gameObject.transform);
                SetActionTargetKey(node, TargetKey);
                SetBoolField(node, ActivePropertyName, false);

                ((INodeExecutor)node).Evaluate(0, 0.0f, 0.0f, context);

                Assert.IsFalse(gameObject.activeSelf);
            }
            finally {
                DestroyObject(node);
                DestroyObject(gameObject);
            }
        }

        /// <summary>
        /// SetGameObjectActiveNode は GameObject activeSelf を Preview 復元対象として返す
        /// </summary>
        [Test]
        public void GameObjectActiveNode_ReturnsGameObjectPreviewProperty() {
            var node = ScriptableObject.CreateInstance<SetGameObjectActiveNode>();
            var gameObject = new GameObject("Target");
            try {
                var context = CreateContext(gameObject.transform);
                SetActionTargetKey(node, TargetKey);

                var previewProperties = ((INodeExecutor)node).GetPreviewProperties(context).ToArray();

                Assert.That(previewProperties.Length, Is.EqualTo(1));
                Assert.That(previewProperties[0].Target, Is.SameAs(gameObject));
                Assert.That(previewProperties[0].PropertyPath, Is.EqualTo(PreviewPropertyPaths.GameObject.ActiveSelf));
            }
            finally {
                DestroyObject(node);
                DestroyObject(gameObject);
            }
        }

        /// <summary>
        /// SetComponentEnabledNode は target Component の enabled を設定する
        /// </summary>
        [Test]
        public void ComponentEnabledNode_EvaluatesEnabledProperty() {
            var node = ScriptableObject.CreateInstance<SetComponentEnabledNode>();
            var gameObject = new GameObject("Target");
            try {
                var light = gameObject.AddComponent<Light>();
                light.enabled = true;
                var context = CreateContext(light);
                SetActionTargetKey(node, TargetKey);
                SetBoolField(node, EnabledPropertyName, false);

                ((INodeExecutor)node).Evaluate(0, 0.0f, 0.0f, context);

                Assert.IsFalse(light.enabled);
            }
            finally {
                DestroyObject(node);
                DestroyObject(gameObject);
            }
        }

        /// <summary>
        /// SetComponentEnabledNode は enabled を持つ Component の m_Enabled を Preview 復元対象として返す
        /// </summary>
        [Test]
        public void ComponentEnabledNode_ReturnsEnabledPreviewProperty() {
            var node = ScriptableObject.CreateInstance<SetComponentEnabledNode>();
            var gameObject = new GameObject("Target");
            try {
                var light = gameObject.AddComponent<Light>();
                var context = CreateContext(light);
                SetActionTargetKey(node, TargetKey);

                var previewProperties = ((INodeExecutor)node).GetPreviewProperties(context).ToArray();

                Assert.That(previewProperties.Length, Is.EqualTo(1));
                Assert.That(previewProperties[0].Target, Is.SameAs(light));
                Assert.That(previewProperties[0].PropertyPath, Is.EqualTo(PreviewPropertyPaths.Component.Enabled));
            }
            finally {
                DestroyObject(node);
                DestroyObject(gameObject);
            }
        }

        private static TestAnimationGraphContext CreateContext(Component target) {
            var context = new TestAnimationGraphContext();
            context.SetTarget(TargetKey, target);
            return context;
        }

        private static void SetActionTargetKey(ActionNode node, string targetKey) {
            var serializedNode = new SerializedObject(node);
            serializedNode.FindProperty(ActionTargetKeyPropertyName).stringValue = targetKey;
            serializedNode.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBoolField(Object target, string propertyName, bool value) {
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void DestroyObject(Object target) {
            if (target == null) {
                return;
            }

            Object.DestroyImmediate(target);
        }
    }
}
