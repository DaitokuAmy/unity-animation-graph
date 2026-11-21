---
name: animationgraph-tuning
description: Adjust existing Unity AnimationGraph nodes, including timing, tween endpoints, easing presets, and custom curves, through the AnimationGraph Editor JSON API. Use for tuning values on an existing graph.
---

# AnimationGraph tuning

1. Locate the installed `com.daitokuamy.unityanimationgraph` package and read its `Documentation~/ai-graph-editing.md`. Use Unity's `PackageInfo.FindForPackageName` and `resolvedPath` if the package is not embedded.
2. Use a Unity Editor C# execution tool to load the requested graph and call `AnimationGraphJsonExporter.Export`. Use the graph's node IDs, targets, and connections to identify the nodes. If the request refers to an Editor display name, inspect the corresponding nodes in the original asset.
3. Read the selected node with `ExportNode` when editing a custom curve or when more detail is needed. Prepare a patch with the node ID and only the requested fields.
4. Run `TryValidate`, then `TryApply` if validation succeeds. Read the node again to verify the changed values. Report any validation error. Save the assets when the requested edit is ready to persist.
5. If no Unity Editor C# execution tool is available, explain the missing execution path. Keep graph structure edits in Unity's graph editing workflow.
