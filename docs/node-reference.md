# Built-in Node Reference

この資料は、Unity Animation Graphに組み込まれているNodeの用途、設定項目、Target型、実行時の挙動をまとめたリファレンスです。

## 共通仕様

### Nodeの接続

- `In`はNodeへの入力です。
- `Next`はNodeの処理が完了した後に進む通常の出力です。
- 複数の`Next`接続は同じ開始時刻から並行して展開されます。
- Nodeの`Duration`と`Delay`は後続Nodeの開始時刻に影響します。
- Enter / Exit Signal Portを有効にしたNodeには、開始時と終了時にSignalを発火するPortを追加できます。

### Target Reference

Action Nodeは`Target Reference`を通して操作対象のComponentを解決します。

| Kind | 表示例 | 解決方法 |
| --- | --- | --- |
| `Binding` | `Binding/MoveTarget` | `AnimationGraphRunner`のTarget Bindingから単一Componentを取得します |
| `CollectionItem` | `CollectionItem/MoveTargets` | 親Loopの現在Indexに対応するCollection要素を取得します |

`CollectionItem`は参照元となるLoopのScope Nodeも保持します。Loopがネストしている場合は、最も内側のLoopだけでなく任意の祖先Loopを参照できます。

Targetが未設定、null、またはCollectionの範囲外だった場合、Action Nodeは既定で処理をスキップします。特殊な独自Action Nodeだけ、基底クラスの`AllowNullTarget`をoverrideしてnullを受け取れます。

### Tween共通設定

すべてのTween Nodeは次の設定を共有します。

| 設定 | 説明 |
| --- | --- |
| `Duration` | 補間にかける秒数です。0の場合は終了値を即時適用します |
| `Delay` | Tween開始前の待機時間です |
| `Ease` | PresetまたはCurveで補間カーブを指定します |
| `Before` | 補間開始値です |
| `After` | 補間終了値です |
| `Relative` | TargetのTween開始時点の値を基準に、指定値を相対値として扱います |
| `Ignore Mask` | 指定した軸またはColorチャンネルを変更対象から除外します |

`Before`と`After`は直値、または対応型のBlackboard Keyから取得できます。相対値Tweenの基準値はNode開始時にキャプチャされるため、再生途中でTargetの値が外部から変わっても同じ基準値で評価されます。

Editor Previewでは、各Nodeが変更対象として登録したSerializedPropertyがAnimationModeによって復元されます。

## Control Node

### Start

型: `StartNode`

Graphの開始点です。空の`AnimationGraphAsset`を初期化すると自動作成され、通常は手動で追加・削除しません。再生時は`StartNode`から接続されたNodeへ進みます。

### Delay

型: `DelayNode`  
作成メニュー: `Control/Delay`

後続Nodeの開始を指定秒数だけ遅らせます。Node自体のDurationは0です。

| 設定 | 説明 |
| --- | --- |
| `Delay` | 待機する秒数。負値は0として扱われます |

### Join

型: `JoinNode`  
作成メニュー: `Control/Join`

複数の実行経路を合流させます。

| Join Type | 説明 |
| --- | --- |
| `All` | 接続されたすべての入力が到達するまで待機します |
| `Any` | 最も早く到達した入力の時刻で後続へ進みます |

分岐や並行Tweenの終了を揃えてから次へ進みたい場合は`All`を使います。いずれかの経路が完了した時点で進みたい場合は`Any`を使います。

### Flag Branch

型: `FlagBranchNode`  
作成メニュー: `Control/Flag Branch`

bool型Blackboardの値を評価して`True`または`False`へ分岐します。

| 設定 | 説明 |
| --- | --- |
| `Flag` | 評価するbool型Blackboard Key |
| `Expected` | Flagがこの値と一致した場合に`True`へ進みます |

Flagが未設定、存在しない、またはbool型ではない場合はGraph検証エラーになります。

### Loop

型: `LoopNode`  
作成メニュー: `Control/Loop`

`Loop` Portに接続されたNode群を繰り返し実行し、すべての反復が完了した後に`Next`へ進みます。

| 設定 | 説明 |
| --- | --- |
| `Count Source` | `Fixed`または`Collection` |
| `Start Index` | 最初の反復Index。0以上の値を指定します |
| `Count` | `Fixed`時の反復回数 |
| `Count Target` | `Collection`時に反復回数を決定するCollection Target |
| `Skip Null` | `Collection`時にCount Targetのnull要素をスキップします |

`Fixed`では`StartIndex`から`Count`回実行します。参照するCollectionの末尾を越えても反復自体は継続し、範囲外の`CollectionItem`を使うAction Nodeだけがスキップされます。

`Collection`では次の回数を実行します。

```text
max(0, CountTarget.Count - StartIndex)
```

現在のCollection Indexは`StartIndex + 反復番号`です。`Skip Null`で反復を省略してもIndexは詰められないため、複数Collectionを同じIndexで対応付けられます。

```text
MoveTargets  = [A, null, C]
ColorTargets = [X, Y,    Z]
```

`MoveTargets`をCount Targetとして`Skip Null`を有効にした場合、実行されるIndexは0と2です。body内の`CollectionItem/ColorTargets`はXとZを参照します。

CollectionはSchedule構築時にSnapshotされます。再生中のAdd / Remove / Clearは現在の再生へ反映されず、次回再生から反映されます。

## State Action Node

### Set GameObject Active

型: `SetGameObjectActiveNode`  
Target: `Component`  
作成メニュー: `Action/Built-in/State/GameObject Active`

Target Componentが属するGameObjectの`activeSelf`を設定します。処理時間は0です。

| 設定 | 説明 |
| --- | --- |
| `Active` | 設定するactive状態 |

### Set Component Enabled

型: `SetComponentEnabledNode`  
Target: `Component`  
作成メニュー: `Action/Built-in/State/Component Enabled`

Targetにpublicな読み書き可能boolプロパティ`enabled`がある場合、その値を設定します。`Behaviour`、`Renderer`、`Collider`などに利用できます。対応する`enabled`プロパティがないComponentでは処理を行いません。

| 設定 | 説明 |
| --- | --- |
| `Enabled` | 設定するenabled状態 |

## Playback Action Node

### Play Timeline Asset

型: `PlayTimelineAssetNode`  
Target: `PlayableDirector`  
作成メニュー: `Action/Built-in/Play Timeline Asset`

指定したTimeline AssetをPlayableDirectorで評価します。NodeのDurationにはTimeline Assetのdurationが使われます。

| 設定 | 説明 |
| --- | --- |
| `Timeline Asset` | 評価するTimeline Asset |

開始時にPlayableDirectorへAssetを設定し、Update Modeを`Manual`へ変更してPlayable Graphを再構築します。各評価時にはNodeのlocal timeをPlayableDirectorへ設定します。Timeline Assetがnullの場合はDuration 0で何も実行しません。

### Play Particle System

型: `PlayParticleSystemNode`  
Target: `ParticleSystem`  
作成メニュー: `Action/Built-in/Play Particle System`

ParticleSystemを手動シミュレーションします。Auto Durationが有効な場合、NodeのDurationにはParticleSystem Main Moduleの`duration`が使われます。

| 設定 | 説明 |
| --- | --- |
| `Auto Duration` | ParticleSystem Main Moduleの`duration`を実行時間として使用します |
| `Duration` | Auto Durationが無効な場合の実行時間を秒で指定します |

開始時にParticleSystemをクリアし、Graphのseedから固定random seedを設定します。これにより、同じseedと時刻では再現可能なPreviewになります。停止またはキャンセル時はParticleSystemを停止してParticleをクリアします。

## Transform Tween Node

### Tween Position

型: `TweenTransformPositionNode`  
Target: `Transform`  
作成メニュー: `Action/Built-in/Tween/Transform/Position`

`position`または`localPosition`をVector3で補間します。

| 設定 | 説明 |
| --- | --- |
| `Space` | `World`ではposition、`Self`ではlocalPositionを操作します |
| `Ignore Mask` | X / Y / Zごとに変更を除外できます |

### Tween Rotation

型: `TweenTransformRotationNode`  
Target: `Transform`  
作成メニュー: `Action/Built-in/Tween/Transform/Rotation`

`rotation`または`localRotation`をEuler角のVector3として補間し、適用時にQuaternionへ変換します。

| 設定 | 説明 |
| --- | --- |
| `Space` | `World`ではrotation、`Self`ではlocalRotationを操作します |
| `Ignore Mask` | Euler角のX / Y / Zごとに変更を除外できます |

### Tween Scale

型: `TweenTransformScaleNode`  
Target: `Transform`  
作成メニュー: `Action/Built-in/Tween/Transform/Scale`

`localScale`をVector3で補間します。X / Y / Zを`Ignore Mask`で個別に除外できます。

## UI Tween Node

### Tween RectTransform Anchored Position

型: `TweenRectTransformAnchoredPositionNode`  
Target: `RectTransform`  
作成メニュー: `Action/Built-in/Tween/UI/RectTransform Anchored Position`

`anchoredPosition`をVector2で補間します。X / Yを`Ignore Mask`で個別に除外できます。

### Tween RectTransform Size Delta

型: `TweenRectTransformSizeDeltaNode`  
Target: `RectTransform`  
作成メニュー: `Action/Built-in/Tween/UI/RectTransform Size Delta`

`sizeDelta`をVector2で補間します。X / Yを`Ignore Mask`で個別に除外できます。

### Tween Graphic Color

型: `TweenGraphicColorNode`  
Target: `Graphic`  
作成メニュー: `Action/Built-in/Tween/UI/Graphic Color`

Image、TextなどGraphic派生Componentの`color`を補間します。R / G / B / Aを`Ignore Mask`で個別に除外できます。

### Tween Image Fill Amount

型: `TweenImageFillAmountNode`  
Target: `Image`  
作成メニュー: `Action/Built-in/Tween/UI/Image Fill Amount`

Imageの`fillAmount`をfloatで補間します。Filled Imageのゲージや円形進捗表示に利用できます。

### Tween Canvas Group Alpha

型: `TweenCanvasGroupAlphaNode`  
Target: `CanvasGroup`  
作成メニュー: `Action/Built-in/Tween/UI/Canvas Group Alpha`

CanvasGroupの`alpha`をfloatで補間します。UIグループ全体のフェードに利用できます。

## Sprite Renderer Tween Node

### Tween Sprite Renderer Color

型: `TweenSpriteRendererColorNode`  
Target: `SpriteRenderer`  
作成メニュー: `Action/Built-in/Tween/Sprite Renderer/Color`

SpriteRendererの`color`を補間します。R / G / B / Aを`Ignore Mask`で個別に除外できます。

### Tween Sprite Renderer Alpha

型: `TweenSpriteRendererAlphaNode`  
Target: `SpriteRenderer`  
作成メニュー: `Action/Built-in/Tween/Sprite Renderer/Alpha`

SpriteRendererの`color.a`だけをfloatで補間し、RGBは維持します。

## Camera Tween Node

### Tween Camera Field Of View

型: `TweenCameraFieldOfViewNode`  
Target: `Camera`  
作成メニュー: `Action/Built-in/Tween/Camera/Field Of View`

Cameraの`fieldOfView`をfloatで補間します。Perspective Camera向けです。

### Tween Camera Orthographic Size

型: `TweenCameraOrthographicSizeNode`  
Target: `Camera`  
作成メニュー: `Action/Built-in/Tween/Camera/Orthographic Size`

Cameraの`orthographicSize`をfloatで補間します。Orthographic Camera向けです。

## Light Tween Node

### Tween Light Color

型: `TweenLightColorNode`  
Target: `Light`  
作成メニュー: `Action/Built-in/Tween/Light/Color`

Lightの`color`を補間します。R / G / B / Aを`Ignore Mask`で個別に除外できます。

### Tween Light Intensity

型: `TweenLightIntensityNode`  
Target: `Light`  
作成メニュー: `Action/Built-in/Tween/Light/Intensity`

Lightの`intensity`をfloatで補間します。

## Audio Source Tween Node

### Tween Audio Source Volume

型: `TweenAudioSourceVolumeNode`  
Target: `AudioSource`  
作成メニュー: `Action/Built-in/Tween/Audio Source/Volume`

AudioSourceの`volume`をfloatで補間します。

### Tween Audio Source Pitch

型: `TweenAudioSourcePitchNode`  
Target: `AudioSource`  
作成メニュー: `Action/Built-in/Tween/Audio Source/Pitch`

AudioSourceの`pitch`をfloatで補間します。

## 関連資料

- [README](../README.md)
- [Animation Graph Technical Specification](specs/animation-graph-technical-spec.md)
