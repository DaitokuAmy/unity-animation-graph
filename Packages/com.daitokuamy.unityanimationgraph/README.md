# Unity Animation Graph

See [AI-assisted AnimationGraph tuning](Documentation~/ai-graph-editing.md) for the Editor JSON API and patch examples.

In Unity, open **Tools > Unity Animation Graph > AI** and choose the installer for your client.

| Menu item | Destination in the project | Invoke |
| --- | --- | --- |
| Install Codex Skill | `.agents/skills/animationgraph-tuning/` | `$animationgraph-tuning` |
| Install Claude Code Skill | `.claude/skills/animationgraph-tuning/` | `/animationgraph-tuning` |

Both installers copy the same bundled skill and preserve existing installations.
