using UnityEngine;
using UnityEngine.Rendering;

namespace UnityAnimationGraph {
    /// <summary>
    /// Renderer の Material parameter に Tween を適用する ActionNode の基底クラス
    /// </summary>
    /// <typeparam name="TTween">使用する Tween 設定型</typeparam>
    /// <typeparam name="TValue">Tween で補間する値型</typeparam>
    public abstract class TweenRendererMaterialNode<TTween, TValue> : ActionNode<Renderer>, IPreviewCleanupNode
        where TTween : Tween {
        /// <summary>
        /// Renderer Material Tween node の実行状態
        /// </summary>
        private sealed class State : IActionNodeState {
            /// <summary>操作対象 Material</summary>
            public Material Material { get; set; }
            /// <summary>Editor Preview 用 MaterialPropertyBlock</summary>
            public MaterialPropertyBlock PropertyBlock { get; set; }
            /// <summary>相対値 Tween の基準値</summary>
            public TValue BaseValue { get; set; }
            /// <summary>操作対象 Shader property ID</summary>
            public int PropertyId { get; set; }
            /// <summary>Editor Preview の場合は true</summary>
            public bool IsPreview { get; set; }
            /// <summary>実行状態を初期化済みの場合は true</summary>
            public bool IsValid { get; set; }
        }

        [SerializeField, Min(0), Tooltip("操作する Material slot の index")]
        private int _materialIndex;
        [SerializeField, Tooltip("操作する Shader property name")]
        private string _propertyName = string.Empty;
        [SerializeField, Min(0.0f), Tooltip("Tween にかける時間")]
        private float _duration = 1.0f;
        [SerializeField, Min(0.0f), Tooltip("Tween 開始前の待機時間")]
        private float _delay;

        /// <summary>Tween 設定</summary>
        protected abstract TTween TweenSettings { get; }
        /// <summary>操作する Shader property name</summary>
        protected string ShaderPropertyName => _propertyName;

        /// <inheritdoc/>
        protected override IActionNodeState CreateState() {
            return new State();
        }

        /// <inheritdoc/>
        protected override float CalculateDuration(int seed, Renderer target, IAnimationGraphBlackboard blackboard) {
            return Mathf.Max(0.0f, _duration);
        }

        /// <inheritdoc/>
        protected override float CalculateDelay(int seed, Renderer target, IAnimationGraphBlackboard blackboard) {
            return Mathf.Max(0.0f, _delay);
        }

        /// <inheritdoc/>
        protected override void Enter(int seed, Renderer target, IAnimationGraphBlackboard blackboard, IActionNodeState state) {
            InitializeState(target, (State)state);
        }

        /// <inheritdoc/>
        protected override void Evaluate(int seed, Renderer target, float localTime, float calculatedDuration, IAnimationGraphBlackboard blackboard, IActionNodeState state) {
            var materialState = (State)state;
            if (!materialState.IsValid) {
                InitializeState(target, materialState);
            }

            if (!materialState.IsValid || TweenSettings == null) {
                return;
            }

            if (!TryEvaluateValue(TweenSettings, blackboard, materialState.BaseValue, localTime, calculatedDuration, out var value)) {
                return;
            }

            ApplyValue(materialState, value);
            if (materialState.IsPreview) {
                target.SetPropertyBlock(materialState.PropertyBlock, _materialIndex);
            }
        }

        /// <summary>
        /// Shader property type が Node の操作型と互換か判定
        /// </summary>
        /// <param name="propertyType">Shader property type</param>
        /// <returns>互換の場合は true</returns>
        protected abstract bool IsCompatiblePropertyType(ShaderPropertyType propertyType);

        /// <summary>
        /// Material から現在値を取得
        /// </summary>
        /// <param name="material">対象 Material</param>
        /// <param name="propertyId">Shader property ID</param>
        /// <returns>現在値</returns>
        protected abstract TValue GetValue(Material material, int propertyId);

        /// <summary>
        /// Editor Preview 用 MaterialPropertyBlock を考慮して現在値を取得
        /// </summary>
        /// <param name="material">対象 Material</param>
        /// <param name="propertyBlock">Editor Preview 用 MaterialPropertyBlock</param>
        /// <param name="propertyId">Shader property ID</param>
        /// <returns>現在値</returns>
        protected virtual TValue GetPreviewValue(Material material, MaterialPropertyBlock propertyBlock, int propertyId) {
            return GetValue(material, propertyId);
        }

        /// <summary>
        /// Tween 設定から指定時刻の補間値を取得
        /// </summary>
        /// <param name="tweenSettings">Tween 設定</param>
        /// <param name="blackboard">Blackboard 値の取得元</param>
        /// <param name="baseValue">相対値の基準値</param>
        /// <param name="localTime">ノード開始時刻からの経過時間</param>
        /// <param name="calculatedDuration">計算済みの実行時間</param>
        /// <param name="value">補間値</param>
        /// <returns>取得できた場合は true</returns>
        protected abstract bool TryEvaluateValue(TTween tweenSettings, IAnimationGraphBlackboard blackboard, TValue baseValue, float localTime, float calculatedDuration, out TValue value);

        /// <summary>
        /// 補間値を Material または MaterialPropertyBlock へ適用
        /// </summary>
        /// <param name="material">対象 Material</param>
        /// <param name="propertyBlock">Editor Preview 用 MaterialPropertyBlock</param>
        /// <param name="propertyId">Shader property ID</param>
        /// <param name="value">補間値</param>
        /// <param name="isPreview">Editor Preview の場合は true</param>
        protected abstract void ApplyValue(Material material, MaterialPropertyBlock propertyBlock, int propertyId, TValue value, bool isPreview);

        /// <inheritdoc/>
        void IPreviewCleanupNode.CleanupPreview(IAnimationGraphContext context) {
            if (!TryResolveTarget(context, out var target) || !TryGetSharedMaterial(target, out _)) {
                return;
            }

            target.SetPropertyBlock(null, _materialIndex);
        }

        /// <summary>
        /// Renderer と Shader property から実行状態を初期化
        /// </summary>
        /// <param name="target">操作対象 Renderer</param>
        /// <param name="state">初期化する実行状態</param>
        private void InitializeState(Renderer target, State state) {
            state.IsValid = false;
            if (string.IsNullOrEmpty(_propertyName)) {
                return;
            }

            var isPreview = !Application.IsPlaying(target.gameObject);
            if (!TryGetMaterial(target, isPreview, out var material)) {
                return;
            }

            var propertyIndex = material.shader.FindPropertyIndex(_propertyName);
            if (propertyIndex < 0 || !IsCompatiblePropertyType(material.shader.GetPropertyType(propertyIndex))) {
                return;
            }

            var propertyId = Shader.PropertyToID(_propertyName);
            state.Material = material;
            state.PropertyBlock = isPreview ? new MaterialPropertyBlock() : null;
            if (isPreview) {
                target.GetPropertyBlock(state.PropertyBlock, _materialIndex);
            }

            state.BaseValue = isPreview
                ? GetPreviewValue(material, state.PropertyBlock, propertyId)
                : GetValue(material, propertyId);
            state.PropertyId = propertyId;
            state.IsPreview = isPreview;
            state.IsValid = true;
        }

        /// <summary>
        /// 実行状態に保持した対象へ補間値を適用
        /// </summary>
        /// <param name="state">実行状態</param>
        /// <param name="value">補間値</param>
        private void ApplyValue(State state, TValue value) {
            ApplyValue(state.Material, state.PropertyBlock, state.PropertyId, value, state.IsPreview);
        }

        /// <summary>
        /// Renderer から操作対象 Material の取得を試行
        /// </summary>
        /// <param name="target">操作対象 Renderer</param>
        /// <param name="isPreview">Editor Preview の場合は true</param>
        /// <param name="material">取得した Material</param>
        /// <returns>取得できた場合は true</returns>
        private bool TryGetMaterial(Renderer target, bool isPreview, out Material material) {
            if (isPreview) {
                return TryGetSharedMaterial(target, out material);
            }

            var materials = target.materials;
            if (_materialIndex < 0 || _materialIndex >= materials.Length) {
                material = null;
                return false;
            }

            material = materials[_materialIndex];
            return material != null;
        }

        /// <summary>
        /// Renderer から共有 Material の取得を試行
        /// </summary>
        /// <param name="target">操作対象 Renderer</param>
        /// <param name="material">取得した共有 Material</param>
        /// <returns>取得できた場合は true</returns>
        private bool TryGetSharedMaterial(Renderer target, out Material material) {
            var materials = target.sharedMaterials;
            if (_materialIndex < 0 || _materialIndex >= materials.Length) {
                material = null;
                return false;
            }

            material = materials[_materialIndex];
            return material != null;
        }
    }
}
