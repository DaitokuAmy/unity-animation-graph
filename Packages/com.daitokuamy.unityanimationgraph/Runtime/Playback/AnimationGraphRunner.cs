using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityAnimationGraph {
    /// <summary>
    /// AnimationGraphRunner が自動 Tick する Unity 更新タイミング
    /// </summary>
    public enum AnimationGraphRunnerUpdateType {
        /// <summary>Update で Tick を実行</summary>
        Update,
        /// <summary>LateUpdate で Tick を実行</summary>
        LateUpdate,
        /// <summary>自動 Tick を行わず、外部から ManualUpdate を呼ぶ</summary>
        ManualUpdate,
    }

    /// <summary>
    /// AnimationGraphAsset を MonoBehaviour として再生するコンポーネント
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AnimationGraphRunner : MonoBehaviour, IAnimationGraphContext {
        [SerializeField, Tooltip("再生する AnimationGraphAsset")]
        private AnimationGraphAsset _graphAsset;
        [SerializeField, Tooltip("OnEnable 時に自動再生する場合は有効")]
        private bool _playOnEnabled;
        [SerializeField, Tooltip("自動 Tick の Unity 更新タイミング")]
        private AnimationGraphRunnerUpdateType _updateType = AnimationGraphRunnerUpdateType.Update;
        [SerializeField, Tooltip("GraphAsset ごとに保持する target binding 一覧")]
        private AnimationGraphTargetBindingGroup[] _targetBindingGroups = Array.Empty<AnimationGraphTargetBindingGroup>();

        private readonly AnimationGraphPlayer _player = new();

        private AnimationGraphBlackboardValue[] _blackboardValues = Array.Empty<AnimationGraphBlackboardValue>();
        private int _currentTargetBindingGroupIndex = -1;
        private bool _isGraphStatePrepared;
        private bool _isInitialized;

        /// <summary>再生対象の AnimationGraphAsset</summary>
        public AnimationGraphAsset GraphAsset {
            get => _graphAsset;
            set => SetGraph(value);
        }

        /// <summary>OnEnable 時に自動再生する場合は true</summary>
        public bool PlayOnEnabled {
            get => _playOnEnabled;
            set => _playOnEnabled = value;
        }

        /// <summary>自動 Tick の Unity 更新タイミング</summary>
        public AnimationGraphRunnerUpdateType UpdateType {
            get => _updateType;
            set => _updateType = value;
        }

        /// <summary>設定中の評価コンテキスト</summary>
        public IAnimationGraphContext Context => this;
        /// <summary>Runner が保持する Blackboard 現在値一覧</summary>
        public IReadOnlyList<AnimationGraphBlackboardValue> BlackboardValues => _blackboardValues ?? Array.Empty<AnimationGraphBlackboardValue>();
        /// <summary>現在の GraphAsset に対応する target binding 一覧</summary>
        public IReadOnlyList<AnimationGraphTargetBinding> TargetBindings => GetCurrentTargetBindings();
        /// <summary>構築済みスケジュール</summary>
        public AnimationGraphSchedule Schedule => _player.Schedule;
        /// <summary>現在の再生状態</summary>
        public AnimationGraphPlayerState State => _player.State;
        /// <summary>再生中の場合は true</summary>
        public bool IsPlaying => _player.IsPlaying;
        /// <summary>現在時刻</summary>
        public float CurrentTime => _player.CurrentTime;
        /// <summary>スケジュール全体の長さ</summary>
        public float Duration => _player.Duration;
        /// <summary>Tick の deltaTime に乗算する再生速度</summary>
        public float TimeScale {
            get => _player.TimeScale;
            set => _player.TimeScale = value;
        }

        /// <inheritdoc/>
        T IAnimationGraphContext.GetTarget<T>(string key) {
            var group = GetCurrentTargetBindingGroup();
            if (group != null && group.TryGetTarget(key, out T target)) {
                return target;
            }

            throw new InvalidOperationException($"Target '{key}' is not registered or does not match {typeof(T).Name}");
        }

        /// <inheritdoc/>
        bool IAnimationGraphContext.TryGetTarget<T>(string key, out T target) {
            var group = GetCurrentTargetBindingGroup();
            if (group != null && group.TryGetTarget(key, out target)) {
                return true;
            }

            
            target = null;
            return false;
        }

        /// <inheritdoc/>
        bool IAnimationGraphBlackboard.TryGetBlackboardValue(string key, out bool value) {
            if (TryGetBlackboardValue(key, out var blackboardValue) && blackboardValue.TryGetValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        bool IAnimationGraphBlackboard.TryGetBlackboardValue(string key, out int value) {
            if (TryGetBlackboardValue(key, out var blackboardValue) && blackboardValue.TryGetValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        bool IAnimationGraphBlackboard.TryGetBlackboardValue(string key, out float value) {
            if (TryGetBlackboardValue(key, out var blackboardValue) && blackboardValue.TryGetValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        bool IAnimationGraphBlackboard.TryGetBlackboardValue(string key, out string value) {
            if (TryGetBlackboardValue(key, out var blackboardValue) && blackboardValue.TryGetValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        bool IAnimationGraphBlackboard.TryGetBlackboardValue(string key, out Vector2 value) {
            if (TryGetBlackboardValue(key, out var blackboardValue) && blackboardValue.TryGetValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        bool IAnimationGraphBlackboard.TryGetBlackboardValue(string key, out Vector3 value) {
            if (TryGetBlackboardValue(key, out var blackboardValue) && blackboardValue.TryGetValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc/>
        bool IAnimationGraphBlackboard.TryGetBlackboardValue(string key, out Color value) {
            if (TryGetBlackboardValue(key, out var blackboardValue) && blackboardValue.TryGetValue(out value)) {
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 現在設定中の AnimationGraphAsset を再生
        /// </summary>
        /// <returns>再生完了を待機する handle</returns>
        public AnimationGraphPlayHandle Play() {
            EnsureGraphAsset();
            InitializePlayer();
            return _player.Play();
        }

        /// <summary>
        /// AnimationGraphAsset を差し替えて再生
        /// </summary>
        /// <param name="graphAsset">再生する AnimationGraphAsset</param>
        /// <returns>再生完了を待機する handle</returns>
        public AnimationGraphPlayHandle Play(AnimationGraphAsset graphAsset) {
            SetGraph(graphAsset);
            return Play();
        }

        /// <summary>
        /// 再生を一時停止
        /// </summary>
        public void Pause() {
            if (!_isInitialized) {
                return;
            }

            _player.Pause();
        }

        /// <summary>
        /// 再生を停止
        /// </summary>
        public void Stop() {
            if (!_isInitialized) {
                return;
            }

            _player.Stop();
        }

        /// <summary>
        /// 0 秒から指定時刻までを順方向に評価
        /// </summary>
        /// <param name="time">評価する時刻</param>
        public void Seek(float time) {
            EnsureGraphAsset();
            InitializePlayer();
            _player.Seek(time);
        }

        /// <summary>
        /// ManualUpdate 設定時に再生時間を進めて評価
        /// </summary>
        /// <param name="deltaTime">進める時間</param>
        public void ManualUpdate(float deltaTime) {
            if (_updateType != AnimationGraphRunnerUpdateType.ManualUpdate) {
                return;
            }

            Tick(deltaTime);
        }

        /// <summary>
        /// 再生する AnimationGraphAsset を設定
        /// </summary>
        /// <param name="graphAsset">再生する AnimationGraphAsset。null の場合は設定を解除</param>
        public void SetGraph(AnimationGraphAsset graphAsset) {
            if (_isInitialized) {
                _player.SetGraph(graphAsset);
            }

            _graphAsset = graphAsset;
            ResetBlackboardValues(graphAsset);
            EnsureTargetBindingGroup(graphAsset);
            _isGraphStatePrepared = true;
        }

        /// <summary>
        /// target Component を設定
        /// </summary>
        /// <param name="key">target key</param>
        /// <param name="target">設定する Component</param>
        /// <returns>設定できた場合は true</returns>
        public bool SetTarget(string key, Component target) {
            var group = GetCurrentTargetBindingGroup();
            return group != null && group.SetTarget(key, target);
        }

        /// <summary>
        /// 指定した GraphAsset GUID に対応する target binding 一覧を取得
        /// </summary>
        /// <param name="graphAssetGuid">GraphAsset の asset GUID</param>
        /// <returns>指定した GraphAsset GUID に対応する target binding 一覧</returns>
        internal IReadOnlyList<AnimationGraphTargetBinding> GetTargetBindingsByGraphAssetGuid(string graphAssetGuid) {
            var group = GetTargetBindingGroupByGraphAssetGuid(graphAssetGuid);
            return group?.Bindings ?? Array.Empty<AnimationGraphTargetBinding>();
        }

        /// <summary>
        /// Blackboard の bool 現在値を設定
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="value">設定する bool 値</param>
        /// <returns>設定できた場合は true</returns>
        public bool SetBlackboardValue(string key, bool value) {
            if (!TryGetBlackboardValueForSet(key, out var valueIndex, out var blackboardValue)) {
                return false;
            }

            if (!blackboardValue.TrySetValue(value)) {
                return false;
            }

            _blackboardValues[valueIndex] = blackboardValue;
            return true;
        }

        /// <summary>
        /// Blackboard の int 現在値を設定
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="value">設定する int 値</param>
        /// <returns>設定できた場合は true</returns>
        public bool SetBlackboardValue(string key, int value) {
            if (!TryGetBlackboardValueForSet(key, out var valueIndex, out var blackboardValue)) {
                return false;
            }

            if (!blackboardValue.TrySetValue(value)) {
                return false;
            }

            _blackboardValues[valueIndex] = blackboardValue;
            return true;
        }

        /// <summary>
        /// Blackboard の float 現在値を設定
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="value">設定する float 値</param>
        /// <returns>設定できた場合は true</returns>
        public bool SetBlackboardValue(string key, float value) {
            if (!TryGetBlackboardValueForSet(key, out var valueIndex, out var blackboardValue)) {
                return false;
            }

            if (!blackboardValue.TrySetValue(value)) {
                return false;
            }

            _blackboardValues[valueIndex] = blackboardValue;
            return true;
        }

        /// <summary>
        /// Blackboard の string 現在値を設定
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="value">設定する string 値</param>
        /// <returns>設定できた場合は true</returns>
        public bool SetBlackboardValue(string key, string value) {
            if (!TryGetBlackboardValueForSet(key, out var valueIndex, out var blackboardValue)) {
                return false;
            }

            if (!blackboardValue.TrySetValue(value)) {
                return false;
            }

            _blackboardValues[valueIndex] = blackboardValue;
            return true;
        }

        /// <summary>
        /// Blackboard の Vector2 現在値を設定
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="value">設定する Vector2 値</param>
        /// <returns>設定できた場合は true</returns>
        public bool SetBlackboardValue(string key, Vector2 value) {
            if (!TryGetBlackboardValueForSet(key, out var valueIndex, out var blackboardValue)) {
                return false;
            }

            if (!blackboardValue.TrySetValue(value)) {
                return false;
            }

            _blackboardValues[valueIndex] = blackboardValue;
            return true;
        }

        /// <summary>
        /// Blackboard の Vector3 現在値を設定
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="value">設定する Vector3 値</param>
        /// <returns>設定できた場合は true</returns>
        public bool SetBlackboardValue(string key, Vector3 value) {
            if (!TryGetBlackboardValueForSet(key, out var valueIndex, out var blackboardValue)) {
                return false;
            }

            if (!blackboardValue.TrySetValue(value)) {
                return false;
            }

            _blackboardValues[valueIndex] = blackboardValue;
            return true;
        }

        /// <summary>
        /// Blackboard の Color 現在値を設定
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="value">設定する Color 値</param>
        /// <returns>設定できた場合は true</returns>
        public bool SetBlackboardValue(string key, Color value) {
            if (!TryGetBlackboardValueForSet(key, out var valueIndex, out var blackboardValue)) {
                return false;
            }

            if (!blackboardValue.TrySetValue(value)) {
                return false;
            }

            _blackboardValues[valueIndex] = blackboardValue;
            return true;
        }

        /// <summary>
        /// スケジュールを再構築
        /// </summary>
        /// <param name="overrideSeed">グラフのシードを一時的に上書きする値</param>
        public void RebuildSchedule(int? overrideSeed = null) {
            EnsureGraphAsset();
            InitializePlayer();
            _player.RebuildSchedule(overrideSeed);
        }

        /// <summary>
        /// Unity の初期化時に player を準備
        /// </summary>
        private void Awake() {
            InitializePlayer();
        }

        /// <summary>
        /// Unity の有効化時に自動再生を開始
        /// </summary>
        private void OnEnable() {
            if (_playOnEnabled && _graphAsset != null) {
                Play();
            }
        }

        /// <summary>
        /// Unity の Update 時に自動再生を進める
        /// </summary>
        private void Update() {
            if (_updateType != AnimationGraphRunnerUpdateType.Update) {
                return;
            }

            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Unity の LateUpdate 時に自動再生を進める
        /// </summary>
        private void LateUpdate() {
            if (_updateType != AnimationGraphRunnerUpdateType.LateUpdate) {
                return;
            }

            Tick(Time.deltaTime);
        }

        /// <summary>
        /// 破棄時に再生中ノードへ中断を通知
        /// </summary>
        private void OnDestroy() {
            Stop();
        }

        /// <summary>
        /// 再生時間を進めて評価
        /// </summary>
        /// <param name="deltaTime">進める時間</param>
        private void Tick(float deltaTime) {
            if (!_isInitialized || !_player.IsPlaying) {
                return;
            }

            _player.Tick(deltaTime);
        }

        /// <summary>
        /// player の初期設定を保証
        /// </summary>
        private void InitializePlayer() {
            if (_isInitialized) {
                return;
            }

            if (!_isGraphStatePrepared) {
                ResetBlackboardValues(_graphAsset);
                EnsureTargetBindingGroup(_graphAsset);
                _isGraphStatePrepared = true;
            }

            _player.SetContext(this);
            if (_graphAsset != null) {
                _player.SetGraph(_graphAsset);
            }

            _isInitialized = true;
        }

        /// <summary>
        /// 再生対象の AnimationGraphAsset が設定済みであることを検証
        /// </summary>
        private void EnsureGraphAsset() {
            if (_graphAsset == null) {
                throw new InvalidOperationException("AnimationGraphAsset is not set");
            }
        }

        /// <summary>
        /// GraphAsset の default value から Blackboard 現在値を作り直す
        /// </summary>
        /// <param name="graphAsset">参照する AnimationGraphAsset</param>
        private void ResetBlackboardValues(AnimationGraphAsset graphAsset) {
            if (graphAsset == null) {
                _blackboardValues = Array.Empty<AnimationGraphBlackboardValue>();
                return;
            }

            var definitions = graphAsset.BlackboardDefinitions;
            var blackboardValues = new AnimationGraphBlackboardValue[definitions.Count];
            for (var i = 0; i < definitions.Count; i++) {
                blackboardValues[i] = new AnimationGraphBlackboardValue(definitions[i]);
            }

            _blackboardValues = blackboardValues;
        }

        /// <summary>
        /// GraphAsset に対応する target binding group を保証
        /// </summary>
        /// <param name="graphAsset">参照する AnimationGraphAsset</param>
        private void EnsureTargetBindingGroup(AnimationGraphAsset graphAsset) {
            if (graphAsset == null) {
                _currentTargetBindingGroupIndex = -1;
                return;
            }

            var graphAssetGuid = graphAsset.AssetGuid;
            if (string.IsNullOrEmpty(graphAssetGuid)) {
                Debug.LogWarning("AnimationGraphAsset の AssetGuid が空です。GraphAsset を初期化してから Runner に設定してください", graphAsset);
                _currentTargetBindingGroupIndex = -1;
                return;
            }

            var groupIndex = FindTargetBindingGroupIndex(graphAssetGuid);
            if (groupIndex < 0) {
                groupIndex = AddTargetBindingGroup(graphAssetGuid, graphAsset.TargetDefinitions);
            }
            else {
                _targetBindingGroups[groupIndex].SetTargetDefinitions(graphAsset.TargetDefinitions);
            }

            _currentTargetBindingGroupIndex = groupIndex;
        }

        /// <summary>
        /// 現在の target binding group を取得
        /// </summary>
        /// <returns>現在の target binding group</returns>
        private AnimationGraphTargetBindingGroup GetCurrentTargetBindingGroup() {
            if (_currentTargetBindingGroupIndex < 0 || _targetBindingGroups == null || _currentTargetBindingGroupIndex >= _targetBindingGroups.Length) {
                return null;
            }

            return _targetBindingGroups[_currentTargetBindingGroupIndex];
        }

        /// <summary>
        /// 指定した GraphAsset GUID に対応する target binding group を取得
        /// </summary>
        /// <param name="graphAssetGuid">GraphAsset の asset GUID</param>
        /// <returns>指定した GraphAsset GUID に対応する target binding group</returns>
        private AnimationGraphTargetBindingGroup GetTargetBindingGroupByGraphAssetGuid(string graphAssetGuid) {
            var groupIndex = FindTargetBindingGroupIndex(graphAssetGuid);
            if (groupIndex < 0 || _targetBindingGroups == null || groupIndex >= _targetBindingGroups.Length) {
                return null;
            }

            return _targetBindingGroups[groupIndex];
        }

        /// <summary>
        /// 現在の target binding 一覧を取得
        /// </summary>
        /// <returns>現在の target binding 一覧</returns>
        private IReadOnlyList<AnimationGraphTargetBinding> GetCurrentTargetBindings() {
            var group = GetCurrentTargetBindingGroup();
            return group?.Bindings ?? Array.Empty<AnimationGraphTargetBinding>();
        }

        /// <summary>
        /// Blackboard 現在値の index を検索
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <returns>見つかった index。見つからない場合は -1</returns>
        private int FindBlackboardValueIndex(string key) {
            if (string.IsNullOrEmpty(key)) {
                return -1;
            }

            var values = _blackboardValues ?? Array.Empty<AnimationGraphBlackboardValue>();
            for (var i = 0; i < values.Length; i++) {
                if (values[i].Key == key) {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Blackboard 現在値の取得を試行
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="value">取得した Blackboard 現在値</param>
        /// <returns>取得できた場合は true</returns>
        private bool TryGetBlackboardValue(string key, out AnimationGraphBlackboardValue value) {
            var valueIndex = FindBlackboardValueIndex(key);
            if (valueIndex < 0) {
                value = default;
                return false;
            }

            value = _blackboardValues[valueIndex];
            return true;
        }

        /// <summary>
        /// 設定用 Blackboard 現在値の取得を試行
        /// </summary>
        /// <param name="key">Blackboard key</param>
        /// <param name="valueIndex">取得した Blackboard 現在値の index</param>
        /// <param name="value">取得した Blackboard 現在値</param>
        /// <returns>取得できた場合は true</returns>
        private bool TryGetBlackboardValueForSet(string key, out int valueIndex, out AnimationGraphBlackboardValue value) {
            valueIndex = FindBlackboardValueIndex(key);
            if (valueIndex < 0) {
                value = default;
                return false;
            }

            value = _blackboardValues[valueIndex];
            return true;
        }

        /// <summary>
        /// target binding group の index を検索
        /// </summary>
        /// <param name="graphAssetGuid">GraphAsset の asset GUID</param>
        /// <returns>見つかった index。見つからない場合は -1</returns>
        private int FindTargetBindingGroupIndex(string graphAssetGuid) {
            if (string.IsNullOrEmpty(graphAssetGuid)) {
                return -1;
            }

            var groups = _targetBindingGroups ?? Array.Empty<AnimationGraphTargetBindingGroup>();
            for (var i = 0; i < groups.Length; i++) {
                var group = groups[i];
                if (group != null && group.GraphAssetGuid == graphAssetGuid) {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// target binding group を追加
        /// </summary>
        /// <param name="graphAssetGuid">GraphAsset の asset GUID</param>
        /// <param name="targetDefinitions">target 定義一覧</param>
        /// <returns>追加した group の index</returns>
        private int AddTargetBindingGroup(string graphAssetGuid, IReadOnlyList<AnimationGraphTargetDefinition> targetDefinitions) {
            var groups = _targetBindingGroups ?? Array.Empty<AnimationGraphTargetBindingGroup>();
            var nextGroups = new AnimationGraphTargetBindingGroup[groups.Length + 1];
            Array.Copy(groups, nextGroups, groups.Length);
            nextGroups[groups.Length] = new AnimationGraphTargetBindingGroup(graphAssetGuid, targetDefinitions);
            _targetBindingGroups = nextGroups;
            return groups.Length;
        }
    }
}
