using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// JSON の変更差分を既存ノードの調整値へ反映する
    /// </summary>
    public static class AnimationGraphJsonPatcher {
        /// <summary>
        /// JSON の変更差分を Asset に反映せず検証する
        /// </summary>
        /// <param name="asset">対象グラフ</param>
        /// <param name="json">nodeId と set を持つ変更差分</param>
        /// <param name="error">失敗した項目と理由</param>
        public static bool TryValidate(AnimationGraphAsset asset, string json, out string error) =>
            TryProcess(asset, json, false, out error);

        /// <summary>
        /// JSON の変更差分全体を検証して一回の Undo で反映する
        /// </summary>
        /// <param name="asset">対象グラフ</param>
        /// <param name="json">nodeId と set を持つ変更差分</param>
        /// <param name="error">失敗した項目と理由</param>
        public static bool TryApply(AnimationGraphAsset asset, string json, out string error) =>
            TryProcess(asset, json, true, out error);

        /// <summary>
        /// 変更をシリアライズ済みコピー上で検証し、成功後に反映する
        /// </summary>
        private static bool TryProcess(AnimationGraphAsset asset, string json, bool apply, out string error) {
            if (asset == null || string.IsNullOrWhiteSpace(json)) {
                error = "A graph and a JSON patch are required.";
                return false;
            }

            try {
                using var reader = new JsonTextReader(new StringReader(json)) {
                    DateParseHandling = DateParseHandling.None,
                    MaxDepth = 32
                };
                var patch = JObject.Load(reader, new JsonLoadSettings {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                });
                if (reader.Read()) {
                    throw new FormatException("Expected one JSON object.");
                }

                var id = Text(patch["nodeId"]);
                if (patch.Count != 2 || patch["set"] is not JObject changes || changes.Count == 0) {
                    throw new FormatException("Expected nodeId and a nonempty set object.");
                }

                if (!asset.TryGetNode(id, out var node)) {
                    throw new FormatException($"Unknown node ID '{id}'.");
                }

                var editable = AnimationGraphJsonExporter.ReadSettings(node, false);
                using var serialized = new SerializedObject(node);
                foreach (var change in changes.Properties()) {
                    try {
                        if (!editable.ContainsKey(change.Name)) {
                            throw new FormatException("This field cannot be edited on the selected node.");
                        }

                        Set(asset, serialized, node, change.Name, change.Value);
                    }
                    catch (FormatException exception) {
                        throw new FormatException($"set.{change.Name}: {exception.Message}");
                    }
                }

                if (node is EachNode { StaggerMode: EachStaggerMode.RandomDelay }
                    && serialized.FindProperty("_minStaggerDelay").floatValue > serialized.FindProperty("_maxStaggerDelay").floatValue) {
                    throw new FormatException("set.staggerMinSeconds: Must not exceed staggerMaxSeconds.");
                }

                if (apply && serialized.hasModifiedProperties) {
                    Undo.IncrementCurrentGroup();
                    Undo.SetCurrentGroupName("Adjust Animation Graph Node");
                    serialized.ApplyModifiedProperties();
                    Undo.IncrementCurrentGroup();
                }

                error = null;
                return true;
            }
            catch (Exception exception) when (exception is JsonException or FormatException or OverflowException) {
                error = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// 公開された調整項目を既存のシリアライズ済みプロパティへ割り当てる
        /// </summary>
        private static void Set(AnimationGraphAsset asset, SerializedObject serialized, Node node, string key, JToken value) {
            switch (key) {
                case "durationSeconds":
                    if (node is PlayParticleSystemNode) {
                        var auto = value.Type == JTokenType.String && Text(value) == "auto";
                        serialized.FindProperty("_autoDuration").boolValue = auto;
                        if (auto) {
                            return;
                        }
                    }

                    serialized.FindProperty("_duration").floatValue = Seconds(value);
                    break;
                case "delaySeconds":
                    serialized.FindProperty("_delay").floatValue = Seconds(value);
                    break;
                case "staggerSeconds":
                    serialized.FindProperty("_staggerDelay").floatValue = Seconds(value);
                    break;
                case "staggerMinSeconds":
                    serialized.FindProperty("_minStaggerDelay").floatValue = Seconds(value);
                    break;
                case "staggerMaxSeconds":
                    serialized.FindProperty("_maxStaggerDelay").floatValue = Seconds(value);
                    break;
                case "count":
                    if (value.Type != JTokenType.Integer || !int.TryParse(value.ToString(), out var count) || count < 0) {
                        throw new FormatException("Expected a nonnegative integer.");
                    }

                    serialized.FindProperty("_loopCount").intValue = count;
                    break;
                case "active":
                    serialized.FindProperty("_active").boolValue = Boolean(value);
                    break;
                case "enabled":
                    serialized.FindProperty("_enabled").boolValue = Boolean(value);
                    break;
                case "from":
                case "to":
                    var endpoint = serialized.FindProperty("_tween").FindPropertyRelative(key == "from" ? "_before" : "_after");
                    SetEndpoint(asset, endpoint, value);
                    break;
                case "ease":
                    SetEase(serialized.FindProperty("_tween").FindPropertyRelative("_ease"), value);
                    break;
            }
        }

        /// <summary>
        /// 端点の直値、Blackboard 参照、相対指定を検証して設定する
        /// </summary>
        private static void SetEndpoint(AnimationGraphAsset asset, SerializedProperty endpoint, JToken value) {
            var literal = endpoint.FindPropertyRelative("_value");
            var source = endpoint.FindPropertyRelative("_source");
            var relative = endpoint.FindPropertyRelative("_relative");
            var isRelative = false;
            if (value is JObject wrapper) {
                isRelative = wrapper["relative"] != null && Boolean(wrapper["relative"]);
                if (relative == null && isRelative) {
                    throw new FormatException("This endpoint does not support relative values.");
                }

                if (wrapper["blackboard"] is { } reference) {
                    if (wrapper.Count != (wrapper["relative"] == null ? 1 : 2)) {
                        throw new FormatException("Expected blackboard and optional relative.");
                    }

                    var key = Text(reference);
                    if (!asset.TryGetBlackboardDefinition(key, out var definition)
                        || definition.ValueType.ToString() != literal.propertyType.ToString()) {
                        throw new FormatException($"Blackboard key '{key}' is missing or has the wrong type.");
                    }

                    endpoint.FindPropertyRelative("_blackboardKey").stringValue = key;
                    source.intValue = (int)ParameterSource.Blackboard;
                    if (relative != null) {
                        relative.boolValue = isRelative;
                    }
                    return;
                }

                if (wrapper["value"] == null || wrapper.Count != (wrapper["relative"] == null ? 1 : 2)) {
                    throw new FormatException("Expected value and optional relative.");
                }

                value = wrapper["value"];
            }

            literal.boxedValue = literal.propertyType switch {
                SerializedPropertyType.Float => (object)Number(value),
                SerializedPropertyType.Vector2 => new Vector2(Component(value, 2, 0), Component(value, 2, 1)),
                SerializedPropertyType.Vector3 => new Vector3(Component(value, 3, 0), Component(value, 3, 1), Component(value, 3, 2)),
                SerializedPropertyType.Vector4 => new Vector4(Component(value, 4, 0), Component(value, 4, 1), Component(value, 4, 2), Component(value, 4, 3)),
                SerializedPropertyType.Color => new Color(Component(value, 4, 0), Component(value, 4, 1), Component(value, 4, 2), Component(value, 4, 3)),
                _ => throw new FormatException("Unsupported endpoint type.")
            };
            source.intValue = (int)ParameterSource.Value;
            if (relative != null) {
                relative.boolValue = isRelative;
            }
        }

        /// <summary>
        /// プリセットまたはカスタムカーブを設定する
        /// </summary>
        private static void SetEase(SerializedProperty ease, JToken value) {
            if (value is not JObject data || data.Count != 1) {
                throw new FormatException("Expected an object containing preset or curve.");
            }

            var mode = ease.FindPropertyRelative("_mode");
            if (data["preset"] is { } preset) {
                ease.FindPropertyRelative("_easeType").intValue = (int)EnumValue<EaseType>(preset);
                mode.intValue = (int)TweenEaseMode.Preset;
            }
            else if (data["curve"] is JObject curve && curve.Count == 3 && curve["keys"] is JArray keys) {
                var frames = new Keyframe[keys.Count];
                for (var index = 0; index < keys.Count; index++) {
                    if (keys[index] is not JObject key || key.Count != 7) {
                        throw new FormatException($"curve.keys[{index}]: Expected time, value, tangents, weights and weightedMode.");
                    }

                    var time = Number(key["time"]);
                    if (index > 0 && time <= frames[index - 1].time) {
                        throw new FormatException($"curve.keys[{index}].time: Times must be strictly increasing.");
                    }

                    frames[index] = new Keyframe(time, Number(key["value"]), Tangent(key["inTangent"]),
                        Tangent(key["outTangent"]), Weight(key["inWeight"]), Weight(key["outWeight"])) {
                        weightedMode = EnumValue<WeightedMode>(key["weightedMode"])
                    };
                }

                ease.FindPropertyRelative("_animationCurve").animationCurveValue = new AnimationCurve(frames) {
                    preWrapMode = EnumValue<WrapMode>(curve["preWrap"]),
                    postWrapMode = EnumValue<WrapMode>(curve["postWrap"])
                };
                mode.intValue = (int)TweenEaseMode.Curve;
            }
            else {
                throw new FormatException("Expected a preset or a complete curve from ExportNode.");
            }
        }

        /// <summary>
        /// 名前で指定された列挙値を取得する
        /// </summary>
        private static T EnumValue<T>(JToken value) where T : struct, Enum {
            var text = Text(value);
            if (!Enum.TryParse<T>(text, out var parsed) || !Enum.IsDefined(typeof(T), parsed) || parsed.ToString() != text) {
                throw new FormatException($"Unknown {typeof(T).Name} '{text}'.");
            }

            return parsed;
        }

        /// <summary>
        /// 配列形式の成分を取得する
        /// </summary>
        private static float Component(JToken value, int count, int index) {
            if (value is not JArray array || array.Count != count) {
                throw new FormatException($"Expected an array of {count} numbers.");
            }

            return Number(array[index]);
        }

        /// <summary>
        /// 秒数を検証する
        /// </summary>
        private static float Seconds(JToken value) {
            var number = Number(value);
            return number >= 0 ? number : throw new FormatException("Seconds must be nonnegative.");
        }

        /// <summary>
        /// 接線ウェイトを検証する
        /// </summary>
        private static float Weight(JToken value) {
            var number = Number(value);
            return number >= 0 && number <= 1 ? number : throw new FormatException("Weights must be between 0 and 1.");
        }

        /// <summary>
        /// 無限大を許容する接線を取得する
        /// </summary>
        private static float Tangent(JToken value) => value?.Type == JTokenType.String ? Text(value) switch {
            "Infinity" => float.PositiveInfinity,
            "-Infinity" => float.NegativeInfinity,
            _ => throw new FormatException("Expected a number or an infinity tangent.")
        } : Number(value);

        /// <summary>
        /// 有限の JSON 数値を取得する
        /// </summary>
        private static float Number(JToken value) {
            if (value?.Type is not (JTokenType.Integer or JTokenType.Float)) {
                throw new FormatException("Expected a JSON number.");
            }

            var number = (float)value;
            return float.IsNaN(number) || float.IsInfinity(number)
                ? throw new FormatException("Expected a finite number.") : number;
        }

        /// <summary>
        /// JSON の文字列を取得する
        /// </summary>
        private static string Text(JToken value) => value?.Type == JTokenType.String
            ? (string)value : throw new FormatException("Expected a JSON string.");

        /// <summary>
        /// JSON の真偽値を取得する
        /// </summary>
        private static bool Boolean(JToken value) => value?.Type == JTokenType.Boolean
            ? (bool)value : throw new FormatException("Expected a JSON boolean.");
    }
}
