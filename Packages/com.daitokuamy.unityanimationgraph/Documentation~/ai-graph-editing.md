# AI-assisted AnimationGraph tuning

Use these Editor APIs from C# in the project that owns the GraphAsset. Run the examples through a Unity Editor C# execution tool or an Editor script. The Codex skill supplies workflow instructions. The execution tool must be available separately.

Load the asset and inspect the graph:

```csharp
using UnityEditor;
using UnityAnimationGraph;
using UnityAnimationGraph.Editor;

// Inside an Editor method:
var graph = AssetDatabase.LoadAssetAtPath<AnimationGraphAsset>("Assets/Example.asset");
var overview = AnimationGraphJsonExporter.Export(graph);
var detail = AnimationGraphJsonExporter.ExportNode(graph, nodeId);
```

Use the ID from `overview` as `nodeId`. Use node type, target, connections, and scope to select the node. To inspect a display name, resolve the ID with `graph.TryGetNode(nodeId, out var node)` and read `node.DisplayName`.

Send one patch per node. Include only fields you want to change:

```json
{
  "nodeId": "<existing node ID>",
  "set": {
    "durationSeconds": 0.3,
    "ease": { "preset": "EaseOutQuad" }
  }
}
```

Validate and apply the JSON string:

```csharp
if (!AnimationGraphJsonPatcher.TryValidate(graph, patchJson, out var error)) {
    throw new System.InvalidOperationException(error);
}
if (!AnimationGraphJsonPatcher.TryApply(graph, patchJson, out error)) {
    throw new System.InvalidOperationException(error);
}
// Re-export the node to verify the values. Save when ready to persist.
AssetDatabase.SaveAssets();
```

- Patch fields use the same names and value shapes as `tuning` in the export. Time fields ending in `Seconds` use seconds. Use the preset spelling returned by export.
- The overview summarizes a custom curve with `keyCount`. For updates, use the complete `ease.curve` object from `ExportNode`, including wrap modes, keys, tangents, and weights.
- A patch with an invalid field is rejected before any values are applied. A successful patch is one Unity Undo operation.
- Connections and scopes provide context for selecting nodes. The patch API edits supported values and curves on existing nodes. Use the graph editor for changes to graph structure.
- Custom nodes can appear in the overview. Their custom tuning fields are available only when supported by the patch API.

## Verify the motion in Unity

Re-export the edited node with `ExportNode` to confirm the values were applied. Then use the graph preview or `AnimationGraphRunner.Seek(graph, time)` to check the motion at meaningful points, such as midair, landing, and the final pose.

For a full turn, the start and end orientations can look identical. Sample the rotation at intermediate times and accumulate signed angle changes to verify the number and direction of turns. For example, confirm that a two-turn drop completes 720 degrees by impact and settles at the intended orientation.
