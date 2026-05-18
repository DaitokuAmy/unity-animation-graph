# Unity Animation Graph

`Unity Animation Graph` は、Unity 上の演出や UI アニメーションをノードグラフとして組み立て、同じ `AnimationGraphAsset` をエディタプレビューとランタイム再生の両方で使うためのライブラリです。

Tween、待機、分岐、合流、ループ、Timeline / ParticleSystem 再生などをグラフ上で接続し、対象コンポーネントは `Target Key`、再生時に変えたい値は `Blackboard` で受け渡します。

<!-- TODO: docs/img/unity-animation-graph-editor-overview.png を追加し、Animation Graph Editor 全体のスクリーンショットを差し込む -->

## 特長

- `AnimationGraphAsset` に演出フローを保存し、Project 上のアセットとして管理できる
- GraphView ベースの専用エディタで、ノードの追加、接続、複製、削除、プレビューができる
- `Target Key` で操作対象を抽象化し、同じグラフを別 GameObject や別シーンで再利用できる
- `Blackboard` で bool / int / float / string / Vector2 / Vector3 / Vector4 / Color の初期値と実行時値を扱える
- Tween 系ノード、制御ノード、Timeline / ParticleSystem ノードを組み込みで利用できる
- 独自 `Node` と独自 `Signal` を追加して、プロジェクト固有の演出処理をグラフに載せられる

## 基本の流れ

1. `AnimationGraphAsset` を作成する
2. `Animation Graph` ウィンドウでアセットを開く
3. `Target` と `Blackboard` を定義する
4. ノードを追加して、ポート同士を接続する
5. ノードを選択して、`Target Key` や Tween 値を設定する
6. シーン上の GameObject に `AnimationGraphRunner` を追加する
7. `Graph Asset` と `Target Binding` を設定して再生する

## パッケージ

- Package name: `com.daitokuamy.unityanimationgraph`
- Display name: `Unity Animation Graph`
- Version: `0.9.1`
- Unity version: `6000.2`
- Dependencies: `com.unity.timeline` `1.8.9`
- License: MIT

## インストール

### Package Manager からインストール

Unity の `Window > Package Manager` を開き、`Add package from git URL...` から次を指定します。

```text
https://github.com/DaitokuAmy/unity-animation-graph.git?path=/Packages/com.daitokuamy.unityanimationgraph
```

### manifest.json からインストール

```json
{
  "dependencies": {
    "com.daitokuamy.unityanimationgraph": "https://github.com/DaitokuAmy/unity-animation-graph.git?path=/Packages/com.daitokuamy.unityanimationgraph"
  }
}
```

## クイックスタート

### 1. `AnimationGraphAsset` を作成する

Project Window で `Create > Unity Animation Graph > Animation Graph` を選び、`AnimationGraphAsset` を作成します。

作成したアセットは、ダブルクリックすると `Animation Graph` ウィンドウで開けます。メニューから開く場合は `Window > Unity Animation Graph > Animation Graph` を使います。

初回表示時、ノードが空の `AnimationGraphAsset` には `StartNode` が自動作成されます。すべての再生はこの `StartNode` から始まります。

<!-- TODO: docs/img/unity-animation-graph-create-asset.png を追加し、Create メニューと初期 StartNode の画面を差し込む -->

### 2. `Target` を定義する

右側下部の Schema 領域で、グラフが操作する対象を `Target` として定義します。

`Target` は `key` と Component 型の組です。たとえば Transform を動かすなら、次のような定義にします。

| Key | Component |
| --- | --- |
| `MainTransform` | `Transform` |
| `HudGraphic` | `Graphic` |
| `MainCamera` | `Camera` |

Action 系ノードは `Target Key` を使って対象 Component を解決します。グラフ側では `MainTransform` のような key だけを持ち、実際にどの GameObject の Transform を使うかは `AnimationGraphRunner` 側でバインドします。

<!-- TODO: docs/img/unity-animation-graph-schema-targets.png を追加し、Target 定義リストの設定例を差し込む -->

### 3. `Blackboard` を定義する

同じ Schema 領域で、実行時に参照したい値を `Blackboard` として定義します。

使える型:

- `Bool`
- `Int`
- `Float`
- `String`
- `Vector2`
- `Vector3`
- `Vector4`
- `Color`

`Blackboard` は主に次の用途で使います。

- `FlagBranchNode` の分岐条件に使う
- Tween の `From` / `To` などを直値ではなく実行時の値から解決する
- `AnimationGraphRunner.SetBlackboardValue(...)` で再生前や再生中に差し替える

例:

| Key | Type | Default |
| --- | --- | --- |
| `IsOpen` | `Bool` | `false` |
| `MoveTo` | `Vector3` | `(0, 1, 0)` |
| `AccentColor` | `Color` | 任意の色 |

<!-- TODO: docs/img/unity-animation-graph-schema-blackboard.png を追加し、Blackboard 定義の設定例を差し込む -->

### 4. ノードを追加して接続する

GraphView 上で右クリックし、`Create` メニューからノードを追加します。

代表的な作成メニュー:

- `Create > Control > Delay`
- `Create > Control > Join`
- `Create > Control > Loop`
- `Create > Control > Flag Branch`
- `Create > Action > Built-in > Tween > Transform > Position`
- `Create > Action > Built-in > Tween > UI > Graphic Color`
- `Create > Action > Built-in > Play Timeline Asset`
- `Create > Action > Built-in > Play Particle System`

ノードの出力ポートから別ノードの `In` ポートへドラッグすると接続できます。通常の流れは `True` / `Next` ポートを使い、分岐ノードは `False` ポート、ループノードは `Loop` ポートも使います。

<!-- TODO: docs/img/unity-animation-graph-connect-nodes.png を追加し、StartNode から Tween ノードへ接続する画面を差し込む -->

### 5. ノードの設定を編集する

ノードを選択すると右側上部の Inspector に詳細設定が表示されます。

Action 系ノードでは、まず `Target Key` を選びます。`Target Key` の候補は Schema で定義した `Target` から表示されます。

Tween 系ノードでは、主に次を設定します。

- `Duration`: Tween にかける時間
- `Delay`: Tween 開始前の待機時間
- `Ease`: 補間カーブ
- `From` / `To`: 開始値と終了値

`From` / `To` などのパラメータは、直値または `Blackboard` 参照として設定できます。演出ごとに値だけ差し替えたい場合は `Blackboard` を使うと運用しやすくなります。

<!-- TODO: docs/img/unity-animation-graph-node-inspector.png を追加し、Tween ノードの Inspector 設定例を差し込む -->

### 6. エディタ上でプレビューする

`Animation Graph` ウィンドウ上部の `Play` / `Stop` / time slider で、編集中のグラフをシーン上の GameObject に対してプレビューできます。

手順:

1. Scene または Hierarchy でプレビュー対象の GameObject を選択する
2. `Animation Graph` ウィンドウ上部の `Play` を押す
3. 必要なら time slider で任意の時刻へシークする
4. `Stop` でプレビューを停止する

プレビューは Unity の `AnimationMode` を使います。`Target` は選択中 GameObject、その親子にある Component、または近くにある `AnimationGraphRunner` の `Target Binding` から解決されます。

<!-- TODO: docs/img/unity-animation-graph-editor-preview.gif を追加し、Play と time slider のプレビュー操作を差し込む -->

### 7. `AnimationGraphRunner` でランタイム再生する

シーン上の GameObject に `AnimationGraphRunner` を追加し、`Graph Asset` に作成した `AnimationGraphAsset` を設定します。

Inspector には、その GraphAsset の `Target` 定義に対応する `Target Binding Groups` が表示されます。各 key に対して、実際に操作したい Component を割り当てます。

主な設定:

- `Graph Asset`: 再生する `AnimationGraphAsset`
- `Play On Enabled`: `OnEnable` 時に自動再生する
- `Update Type`: `Update` / `LateUpdate` / `ManualUpdate`
- `Target Binding Groups`: GraphAsset ごとの Target key と Component の対応表

主なランタイム API:

- `Play()` / `Play(AnimationGraphAsset)`: 再生を開始し、`AnimationGraphPlayHandle` を返す
- `Pause()` / `Stop()`: 再生の一時停止、停止
- `AnimationGraphPlayHandle.Pause()` / `Resume()` / `Stop()` / `Complete()`: 取得した handle に対応する再生を操作する
- `ManualUpdate(deltaTime)`: `UpdateMode` が `AnimationGraphRunner.UpdateType.ManualUpdate` のときだけ手動で時間を進める
- `SetTarget(key, component)`: 現在の GraphAsset に対応する Target Binding をコードから差し替える
- `SetTarget(graphAsset, key, component)`: 指定した GraphAsset の Target Binding をコードから差し替える
- `GetTarget<T>(key)` / `TryGetTarget<T>(key, out target)`: 現在の GraphAsset に対応する Target Binding から Component を取得する
- `GetTarget<T>(graphAsset, key)` / `TryGetTarget<T>(graphAsset, key, out target)`: 指定した GraphAsset の Target Binding から Component を取得する
- `SetBlackboardValue(key, value)`: Blackboard の現在値を差し替える
- `UpdateMode`: 自動 Tick の Unity 更新タイミングを切り替える
- `TimeScale`: `Update` / `LateUpdate` / `ManualUpdate` で進む時間に倍率をかける
- `SubscribeSignal<TSignal>(callback)`: Signal 通知を購読し、戻り値の `IDisposable` で個別に購読解除する
- `ClearSignalSubscriptions()`: Signal 通知の購読をすべて解除する

<!-- TODO: docs/img/unity-animation-graph-runner-inspector.png を追加し、AnimationGraphRunner の Target Binding 設定例を差し込む -->

コードから再生する場合:

```csharp
using UnityEngine;
using UnityAnimationGraph;

public sealed class AnimationGraphPlayExample : MonoBehaviour {
    [SerializeField]
    private AnimationGraphRunner _runner;

    private void OnEnable() {
        _runner.Play();
    }
}
```

再生完了を Coroutine で待つ場合:

```csharp
using System.Collections;
using UnityEngine;
using UnityAnimationGraph;

public sealed class AnimationGraphCoroutineExample : MonoBehaviour {
    [SerializeField]
    private AnimationGraphRunner _runner;

    private IEnumerator Start() {
        var handle = _runner.Play();
        yield return handle;

        if (handle.IsCompleted) {
            Debug.Log("Animation graph completed.");
        }
    }
}
```

`AnimationGraphPlayHandle` は `await` でも待機できます。戻り値は最後まで自然完了した場合に `true` になります。
また、handle から `Pause()` / `Resume()` / `Stop()` / `Complete()` で、その handle に対応する再生を操作できます。

```csharp
using UnityEngine;
using UnityAnimationGraph;

public sealed class AnimationGraphAwaitExample : MonoBehaviour {
    [SerializeField]
    private AnimationGraphRunner _runner;

    private async void OnEnable() {
        var completed = await _runner.Play();
        if (completed) {
            Debug.Log("Animation graph completed.");
        }
    }
}
```

`ManualUpdate` で外部から時間を進める場合:

```csharp
using UnityEngine;
using UnityAnimationGraph;

public sealed class ManualAnimationGraphTicker : MonoBehaviour {
    [SerializeField]
    private AnimationGraphRunner _runner;

    private void Awake() {
        _runner.UpdateMode = AnimationGraphRunner.UpdateType.ManualUpdate;
    }

    private void Update() {
        _runner.ManualUpdate(Time.deltaTime);
    }
}
```

`Target Binding` をコードから差し替える場合:

```csharp
using UnityEngine;
using UnityAnimationGraph;

public sealed class AnimationGraphTargetExample : MonoBehaviour {
    [SerializeField]
    private AnimationGraphRunner _runner;
    [SerializeField]
    private AnimationGraphAsset _openGraph;
    [SerializeField]
    private AnimationGraphAsset _closeGraph;
    [SerializeField]
    private Transform _actor;

    private void Awake() {
        _runner.SetTarget(_openGraph, "Actor", _actor);
        _runner.SetTarget(_closeGraph, "Actor", _actor);

        var closeActor = _runner.GetTarget<Transform>(_closeGraph, "Actor");
        Debug.Assert(closeActor == _actor);
    }
}
```

`Blackboard` を差し替えてから再生する場合:

```csharp
using UnityEngine;
using UnityAnimationGraph;

public sealed class AnimationGraphBlackboardExample : MonoBehaviour {
    [SerializeField]
    private AnimationGraphRunner _runner;

    public void Open(Vector3 destination) {
        _runner.SetBlackboardValue("IsOpen", true);
        _runner.SetBlackboardValue("MoveTo", destination);
        _runner.Play();
    }
}
```

## 組み込みノード

### 制御ノード

| Node | 用途 |
| --- | --- |
| `StartNode` | グラフの開始点。初期化時に自動作成されます |
| `DelayNode` | 指定時間待機します |
| `JoinNode` | 複数の流れを合流します |
| `LoopNode` | 指定したノード群を指定回数ループします |
| `FlagBranchNode` | bool Blackboard 値で true / false に分岐します |

### Action ノード

| Node | 対象 |
| --- | --- |
| `TweenTransformPositionNode` | `Transform.position` |
| `TweenTransformRotationNode` | `Transform.rotation` |
| `TweenTransformScaleNode` | `Transform.localScale` |
| `TweenRectTransformAnchoredPositionNode` | `RectTransform.anchoredPosition` |
| `TweenRectTransformSizeDeltaNode` | `RectTransform.sizeDelta` |
| `TweenGraphicColorNode` | `Graphic.color` |
| `TweenImageFillAmountNode` | `Image.fillAmount` |
| `TweenCanvasGroupAlphaNode` | `CanvasGroup.alpha` |
| `TweenSpriteRendererColorNode` | `SpriteRenderer.color` |
| `TweenSpriteRendererAlphaNode` | `SpriteRenderer.color.a` |
| `TweenCameraFieldOfViewNode` | `Camera.fieldOfView` |
| `TweenCameraOrthographicSizeNode` | `Camera.orthographicSize` |
| `TweenLightColorNode` | `Light.color` |
| `TweenLightIntensityNode` | `Light.intensity` |
| `TweenAudioSourceVolumeNode` | `AudioSource.volume` |
| `TweenAudioSourcePitchNode` | `AudioSource.pitch` |
| `PlayParticleSystemNode` | `ParticleSystem` |
| `PlayTimelineAssetNode` | `PlayableDirector` / `TimelineAsset` |
| `SetGameObjectActiveNode` | `GameObject.activeSelf` |
| `SetComponentEnabledNode` | `Component.enabled` |

## エディタ操作

| 操作 | 内容 |
| --- | --- |
| 右クリック | `Create` メニューからノードを追加 |
| ポートをドラッグ | ノード同士を接続 |
| ノード選択 | Inspector に詳細を表示 |
| `Ctrl+C` / `Cmd+C` | 選択ノードをコピー |
| `Ctrl+V` / `Cmd+V` | コピーしたノードを貼り付け |
| `Ctrl+D` / `Cmd+D` | 選択ノードを複製 |
| `Delete` / `Backspace` | 選択ノードまたは Edge を削除 |
| ノードをドラッグ | ノード位置を移動 |

## 独自ノードを追加する

独自処理をグラフに追加したい場合は、`ActionNode<TTarget>` または `ControlNode` を継承したクラスを作ります。通常、Component を操作するノードは `ActionNode<TTarget>` を使います。

例: Transform の localPosition を、直値または Vector3 Blackboard 値へ Tween するノード

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityAnimationGraph;

namespace App.Animation {
    [NodeInfo("Move Local Position", "Custom/Move Local Position")]
    public sealed class MoveLocalPositionNode : ActionNode<Transform> {
        [SerializeField, Min(0.0f)]
        private float _duration = 0.4f;
        [SerializeField, Min(0.0f)]
        private float _delay;
        [SerializeField]
        private Vector3 _from;
        [SerializeField]
        private Vector3 _to = Vector3.up;
        [SerializeField]
        private EaseType _ease = EaseType.EaseOutCubic;
        [SerializeField, BlackboardKey(BlackboardValueType.Vector3)]
        private string _toKey = string.Empty;

        protected override float CalculateDuration(int seed, Transform target, IAnimationGraphBlackboard blackboard) {
            return Mathf.Max(0.0f, _duration);
        }

        protected override float CalculateDelay(int seed, Transform target, IAnimationGraphBlackboard blackboard) {
            return Mathf.Max(0.0f, _delay);
        }

        protected override IEnumerable<string> GetPreviewProperties(Transform target) {
            yield return PreviewPropertyPaths.Transform.LocalPositionX;
            yield return PreviewPropertyPaths.Transform.LocalPositionY;
            yield return PreviewPropertyPaths.Transform.LocalPositionZ;
        }

        protected override void Evaluate(int seed, Transform target, float localTime, float calculatedDuration, IAnimationGraphBlackboard blackboard) {
            var to = ResolveTo(blackboard);
            var ratio = calculatedDuration <= 0.0f ? 1.0f : Mathf.Clamp01(localTime / calculatedDuration);
            var easedRatio = Easing.Evaluate(_ease, ratio);
            target.localPosition = Vector3.LerpUnclamped(_from, to, easedRatio);
        }

        private Vector3 ResolveTo(IAnimationGraphBlackboard blackboard) {
            if (!string.IsNullOrEmpty(_toKey) && blackboard.TryGetBlackboardValue(_toKey, out Vector3 value)) {
                return value;
            }

            return _to;
        }
    }
}
```

ポイント:

- `[NodeInfo("表示名", "メニュー/パス")]` を付けると、GraphView の右クリックメニューに表示されます
- `ActionNode<TTarget>` は Schema の `Target` と Runner の `Target Binding` から対象 Component を解決し、`Evaluate` に渡します
- `CalculateDuration` と `CalculateDelay` はスケジュール構築時に使われます。時間を持つ Animation ノードでは明示的に返します
- `Evaluate` は再生時やプレビュー時に呼ばれ、`localTime / calculatedDuration` から進捗を作れます
- Blackboard 値を使う場合は、`BlackboardKey` 属性で候補を絞り、`blackboard.TryGetBlackboardValue(...)` で型付きに取得します
- Editor Preview で変更対象を復元したい場合は、`GetPreviewProperties` で `PreviewPropertyPaths` の SerializedProperty path を返します
- GameObject など Component 以外を復元対象にしたい場合は、`ActionNode<TTarget>.GetPreviewObjectProperties` で target object と path を返します
- ノード設定を EditorWindow 上で検証したい場合は、`Validate(NodeValidationContext context)` を override してエラーメッセージを返します

## 独自 Signal を追加する

ノードの Enter / Exit の瞬間に処理を差し込みたい場合は、`Signal` を継承します。ノード Inspector で `Enable Enter Signal Port` または `Enable Exit Signal Port` を有効にすると、Signal ポートの右クリックから Signal を追加できます。

```csharp
using UnityEngine;
using UnityAnimationGraph;

namespace App.Animation {
    [SignalInfo("Debug Log", "Custom/Debug Log")]
    public sealed class DebugLogSignal : Signal {
        [SerializeField]
        private string _message = "Signal";

        protected override void Dispatch(int seed, IAnimationGraphContext context) {
            Debug.Log(_message);
        }
    }
}
```

`SignalInfo` を付けると Signal 作成メニューの表示名とパスを指定できます。ランタイム側では `AnimationGraphRunner.SubscribeSignal<TSignal>(...)` で特定の Signal 型を購読できます。購読 callback には `SignalContext<TSignal>` が渡され、発火した `Signal` と通知時の `IAnimationGraphContext` を参照できます。購読解除は戻り値の `IDisposable.Dispose()` で行います。

## サンプル

このリポジトリには動作確認用のサンプルがあります。

- シーン: `Assets/Sample/Scenes/Sample.unity`
- グラフアセット: `Assets/Sample/Data/AnimationGraph_01.asset`
- グラフアセット: `Assets/Sample/Data/AnimationGraph_02.asset`
- グラフアセット: `Assets/Sample/Data/AnimationGraph_03.asset`
- サンプルノード: `Assets/Sample/Scripts/Runtime/Node/SampleMoveLocalPositionNode.cs`
- サンプルノード: `Assets/Sample/Scripts/Runtime/Node/SampleChangeColorNode.cs`
- サンプル Signal: `Assets/Sample/Scripts/Runtime/Signal/SampleLogSignal.cs`
- Signal 購読例: `Assets/Sample/Scripts/Runtime/SampleObserver.cs`

まず挙動を見たい場合は、`Assets/Sample/Scenes/Sample.unity` を開き、Hierarchy 上の対象 GameObject と `Animation Graph` ウィンドウを見比べながら `Play` プレビューまたは Play Mode で確認してください。

## テスト

Editor テストは `Packages/com.daitokuamy.unityanimationgraph/Tests/Editor` にあります。Unity Test Runner の EditMode で、Scheduler / Player / Runner / Tween / Timeline / ParticleSystem / EditorModel 周辺の動作を確認できます。

## 補足

- `AnimationGraphAsset` の `GraphSeed` はスケジュールや乱数評価の基準になります
- `RandomSeed` を有効にすると、再生ごとに異なる seed を使います
- `AnimationGraphRunner.Play()` は `AnimationGraphPlayHandle` を返し、Coroutine の `yield return` や `await` パターンで完了待機できます
- `AnimationGraphPlayHandle` は対象の再生に対して `Pause()` / `Resume()` / `Stop()` / `Complete()` を実行できます
- `AnimationGraphRunner.Stop()` は再生中ノードへキャンセルを通知して停止します
