using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UnityAnimationGraph.Editor {
    /// <summary>
    /// GraphAsset を AI が参照する JSON に変換する
    /// </summary>
    public static class AnimationGraphJsonExporter {
        /// <summary>既存グラフの構造と調整値を JSON で出力する</summary>
        /// <param name="asset">読み取り対象のグラフ</param>
        /// <returns>グラフの JSON</returns>
        public static string Export(AnimationGraphAsset asset) {
            if (asset == null) {
                throw new ArgumentNullException(nameof(asset));
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var nodes = new List<Dictionary<string, object>>();
            foreach (var node in asset.Nodes) {
                if (node == null || string.IsNullOrEmpty(node.NodeId) || !ids.Add(node.NodeId)) {
                    throw new InvalidOperationException("Graph contains a missing node or an invalid node ID.");
                }

                nodes.Add(DescribeNode(node, false));
            }

            var graph = new Dictionary<string, object> {
                ["version"] = 1,
                ["entry"] = asset.StartNodeId,
                ["nodes"] = nodes
            };
            if (asset.TargetDefinitions.Count > 0) {
                graph["targets"] = asset.TargetDefinitions.Select(target => new Dictionary<string, object> {
                    ["key"] = target.Key,
                    ["multiplicity"] = target.Multiplicity.ToString(),
                    ["typeGuid"] = target.MonoScriptGuid
                }).ToArray();
            }

            if (asset.BlackboardDefinitions.Count > 0) {
                graph["blackboard"] = asset.BlackboardDefinitions.Select(DescribeBlackboard).ToArray();
            }

            return FormatValue(graph);
        }

        /// <summary>元の Asset から ID でノードを取得し、カーブを含む詳細を出力する</summary>
        /// <param name="asset">読み取り対象のグラフ</param>
        /// <param name="nodeId">グラフの JSON に含まれるノード ID</param>
        /// <returns>対象ノードの JSON</returns>
        public static string ExportNode(AnimationGraphAsset asset, string nodeId) {
            if (asset == null) {
                throw new ArgumentNullException(nameof(asset));
            }

            if (!asset.TryGetNode(nodeId, out var node)) {
                throw new ArgumentException($"Unknown node ID '{nodeId}'.", nameof(nodeId));
            }

            return FormatValue(DescribeNode(node, true));
        }

        /// <summary>
        /// ノードを特定する情報と調整値を取得する
        /// </summary>
        private static Dictionary<string, object> DescribeNode(Node node, bool includeCurveKeys) {
            var type = node.GetType();
            var data = new Dictionary<string, object> {
                ["id"] = node.NodeId,
                ["type"] = type.FullName
            };
            if (type.Assembly != typeof(Node).Assembly) {
                data["assembly"] = type.Assembly.GetName().Name;
            }

            if (node is BranchNode branch) {
                data["branches"] = Enumerable.Range(0, branch.PortCount).Select(index => new Dictionary<string, object> {
                    ["port"] = branch.GetPortName(index),
                    ["next"] = branch.GetBranchNodeIds(index)
                }).ToArray();
            }
            else if (node.NextNodeIds.Count > 0) {
                data["next"] = node.NextNodeIds;
            }

            if (node is ScopedControlNode scoped) {
                data["body"] = scoped.BodyNodeIds;
            }

            if (node is ActionNode action) {
                var target = action.TargetReference;
                data["target"] = target.TargetKey;
                if (target.Kind == TargetReferenceKind.CollectionItem) {
                    data["scopeId"] = target.ScopeNodeId;
                }
            }

            if (node is LoopNode loop) {
                data["startIndex"] = loop.StartIndex;
                if (loop.CountSource == LoopCountSource.Collection) {
                    data["collection"] = loop.LoopCountTargetKey;
                    data["skipNullItems"] = loop.SkipNullItems;
                }
            }
            else if (node is EachNode each) {
                data["collection"] = each.CollectionTargetKey;
                data["skipNullItems"] = each.SkipNullItems;
            }
            else if (node is JoinNode join) {
                data["join"] = join.JoinType.ToString();
            }
            else if (node is FlagBranchNode) {
                using var serialized = new SerializedObject(node);
                data["flag"] = serialized.FindProperty("_flagKey").stringValue;
                data["expected"] = serialized.FindProperty("_expectedValue").boolValue;
            }

            var tuning = ReadSettings(node, includeCurveKeys);
            if (tuning.Count > 0) {
                data["tuning"] = tuning;
            }

            return data;
        }


        /// <summary>
        /// 組み込みノードで調整対象として公開する設定を取得する
        /// </summary>
        internal static Dictionary<string, object> ReadSettings(Node node, bool includeCurveKeys) {
            var settings = new Dictionary<string, object>();
            if (node.GetType().Assembly != typeof(Node).Assembly) {
                return settings;
            }

            using var serialized = new SerializedObject(node);
            switch (node) {
                case DelayNode:
                    Add(settings, "delaySeconds", serialized.FindProperty("_delay"));
                    break;
                case LoopNode loop when loop.CountSource == LoopCountSource.Fixed:
                    Add(settings, "count", serialized.FindProperty("_loopCount"));
                    break;
                case EachNode each:
                    if (each.StaggerMode == EachStaggerMode.Interval) {
                        Add(settings, "staggerSeconds", serialized.FindProperty("_staggerDelay"));
                    }
                    else {
                        Add(settings, "staggerMinSeconds", serialized.FindProperty("_minStaggerDelay"));
                        Add(settings, "staggerMaxSeconds", serialized.FindProperty("_maxStaggerDelay"));
                    }
                    break;
                case PlayParticleSystemNode:
                    settings["durationSeconds"] = serialized.FindProperty("_autoDuration").boolValue
                        ? "auto" : (object)serialized.FindProperty("_duration").floatValue;
                    break;
                case SetGameObjectActiveNode:
                    Add(settings, "active", serialized.FindProperty("_active"));
                    break;
                case SetComponentEnabledNode:
                    Add(settings, "enabled", serialized.FindProperty("_enabled"));
                    break;
                default:
                    var tween = serialized.FindProperty("_tween");
                    if (tween != null) {
                        WriteTween(settings, serialized, tween, includeCurveKeys);
                    }
                    break;
            }

            return settings;
        }

        /// <summary>
        /// Tween の時間、端点、イージングを出力する
        /// </summary>
        private static void WriteTween(Dictionary<string, object> settings, SerializedObject serialized, SerializedProperty tween, bool includeCurveKeys) {
            Add(settings, "durationSeconds", serialized.FindProperty("_duration"));
            Add(settings, "delaySeconds", serialized.FindProperty("_delay"));
            WriteEndpoint(settings, "from", tween.FindPropertyRelative("_before"));
            WriteEndpoint(settings, "to", tween.FindPropertyRelative("_after"));
            var ease = tween.FindPropertyRelative("_ease");
            if (ease.FindPropertyRelative("_mode").intValue == (int)TweenEaseMode.Curve) {
                var curve = ease.FindPropertyRelative("_animationCurve").animationCurveValue;
                settings["ease"] = new Dictionary<string, object> {
                    ["curve"] = includeCurveKeys ? DescribeCurve(curve) : new Dictionary<string, object> { ["keyCount"] = curve.length }
                };
            }
            else {
                var preset = ease.FindPropertyRelative("_easeType");
                settings["ease"] = new Dictionary<string, object> { ["preset"] = preset.enumNames[preset.enumValueIndex] };
            }
        }

        /// <summary>
        /// 端点の直値または Blackboard 参照を取得する
        /// </summary>
        private static void WriteEndpoint(Dictionary<string, object> settings, string key, SerializedProperty endpoint) {
            var relative = endpoint.FindPropertyRelative("_relative")?.boolValue ?? false;
            var value = endpoint.FindPropertyRelative("_value").boxedValue;
            if (endpoint.FindPropertyRelative("_source").intValue == (int)ParameterSource.Blackboard) {
                settings[key] = new Dictionary<string, object> {
                    ["blackboard"] = endpoint.FindPropertyRelative("_blackboardKey").stringValue,
                    ["relative"] = relative
                };
            }
            else {
                settings[key] = relative ? new Dictionary<string, object> { ["value"] = value, ["relative"] = true } : value;
            }
        }

        /// <summary>
        /// 調整項目を追加する
        /// </summary>
        private static void Add(Dictionary<string, object> settings, string key, SerializedProperty property) {
            settings[key] = property.boxedValue;
        }

        /// <summary>
        /// カーブのキーと補間設定を取得する
        /// </summary>
        private static Dictionary<string, object> DescribeCurve(AnimationCurve curve) => new() {
            ["preWrap"] = curve.preWrapMode.ToString(),
            ["postWrap"] = curve.postWrapMode.ToString(),
            ["keys"] = curve.keys.Select(key => new Dictionary<string, object> {
                ["time"] = key.time,
                ["value"] = key.value,
                ["inTangent"] = key.inTangent,
                ["outTangent"] = key.outTangent,
                ["inWeight"] = key.inWeight,
                ["outWeight"] = key.outWeight,
                ["weightedMode"] = key.weightedMode.ToString()
            }).ToArray()
        };


        /// <summary>
        /// Blackboard の型と初期値を取得する
        /// </summary>
        private static Dictionary<string, object> DescribeBlackboard(BlackboardDefinition definition) => new() {
            ["key"] = definition.Key,
            ["type"] = definition.ValueType.ToString(),
            ["default"] = definition.ValueType switch {
                BlackboardValueType.Bool => definition.DefaultBoolValue,
                BlackboardValueType.Int => definition.DefaultIntValue,
                BlackboardValueType.Float => definition.DefaultFloatValue,
                BlackboardValueType.String => definition.DefaultStringValue,
                BlackboardValueType.Vector2 => definition.DefaultVector2Value,
                BlackboardValueType.Vector3 => definition.DefaultVector3Value,
                BlackboardValueType.Vector4 => definition.DefaultVector4Value,
                BlackboardValueType.Color => definition.DefaultColorValue,
                _ => throw new NotSupportedException($"Unsupported Blackboard type '{definition.ValueType}'.")
            }
        };

        /// <summary>
        /// 出力モデルを JSON に変換する
        /// </summary>
        private static string FormatValue(object value) => value switch {
            null => "null",
            string text => Quote(text),
            bool flag => flag ? "true" : "false",
            float number when float.IsNaN(number) || float.IsInfinity(number) => Quote(number.ToString(CultureInfo.InvariantCulture)),
            float number => number.ToString("R", CultureInfo.InvariantCulture),
            int number => number.ToString(CultureInfo.InvariantCulture),
            Vector2 vector => FormatValue(new[] { vector.x, vector.y }),
            Vector3 vector => FormatValue(new[] { vector.x, vector.y, vector.z }),
            Vector4 vector => FormatValue(new[] { vector.x, vector.y, vector.z, vector.w }),
            Color color => FormatValue(new[] { color.r, color.g, color.b, color.a }),
            Dictionary<string, object> map => "{" + string.Join(",", map.Select(pair => Quote(pair.Key) + ":" + FormatValue(pair.Value))) + "}",
            System.Collections.IEnumerable items => "[" + string.Join(",", items.Cast<object>().Select(FormatValue)) + "]",
            _ => throw new NotSupportedException($"Unsupported JSON value type '{value.GetType()}'.")
        };


        /// <summary>
        /// 文字列を一行のリテラルへ変換する
        /// </summary>
        private static string Quote(string value) {
            var quoted = new StringBuilder("\"");
            foreach (var character in value ?? string.Empty) {
                switch (character) {
                    case '"': quoted.Append("\\\""); break;
                    case '\\': quoted.Append("\\\\"); break;
                    default:
                        if (char.IsControl(character) || character is '\u2028' or '\u2029') {
                            quoted.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else {
                            quoted.Append(character);
                        }
                        break;
                }
            }

            return quoted.Append('"').ToString();
        }

    }
}
