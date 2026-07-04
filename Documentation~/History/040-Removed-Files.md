# 040 — Removed Files

This file records documentation files intentionally removed from the active surface during the documentation cleanup.

## Removal reason

The old documentation folder mixed:

```text
current setup docs
historical phase guides
planning drafts
ADR archive
FIRSTGAME examples
superseded reset model notes
```

The cleanup keeps current docs short and moves historical navigation into numbered history files.

## Removed or archived by summary

### Root docs merged into current docs

```text
Documentation~/Architecture.md
Documentation~/Authoring.md
Documentation~/Git-Package-Install.md
Documentation~/QA-Smokes.md
Documentation~/Runtime-Surfaces.md
Documentation~/Setup.md
Documentation~/Troubleshooting.md
```

Replacement:

```text
Documentation~/README.md
Documentation~/Current/00-Current-State.md
Documentation~/Current/02-Usage-Map.md
Documentation~/Guides/Usage/index.html
```

### Historical phase guides consolidated

```text
Documentation~/Guides/F10C-Pause-ContentAnchor-Binding-Usage.md
Documentation~/Guides/F10D-Pause-ContentAnchor-Binding-Execution-Usage.md
Documentation~/Guides/F10E-Pause-Visual-Materialization-Usage.md
Documentation~/Guides/F10F-Pause-Presentation-Model-Usage.md
Documentation~/Guides/F10G-Pause-UIGlobal-Resident-Surface-Usage.md
Documentation~/Guides/F10H-Pause-Logical-Toggle-Resident-Surface-Usage.md
Documentation~/Guides/F15-Unity-Object-Reset-Adapters-Usage.md
Documentation~/Guides/F16-GameObject-Active-Reset-Usage.md
Documentation~/Guides/F17-Gate-Foundation-Usage.md
Documentation~/Guides/F18-Transition-Orchestration-Usage.md
Documentation~/Guides/F19-Transition-Effects-Usage.md
Documentation~/Guides/F19D-Minimal-Fade-Curtain-Adapter-Setup.md
Documentation~/Guides/F20-Pause-State-Gate-Usage.md
Documentation~/Guides/F21-Save-Snapshot-Preferences-Progression-Usage.md
Documentation~/Guides/F22-Loading-Operation-Progress-Readiness-Usage.md
Documentation~/Guides/F23-Pause-Content-Overlay-Input-Usage.md
Documentation~/Guides/FIRSTGAME-2B-Pause-Input-Usage.md
Documentation~/Guides/First-Practical-Flow-Transition.md
Documentation~/Guides/immersive-framework-manual-gamedesigner.html
```

Replacement:

```text
Documentation~/Guides/Usage/index.html
Documentation~/History/030-Guide-History.md
```

### Planning drafts consolidated

```text
Documentation~/Planning/IF-FW-RESET-REFORM-Plano-Implementacao-v2.md
Documentation~/Planning/Immersive-Framework-Roadmap-Revisado.md
```

Replacement:

```text
Documentation~/Current/01-Roadmap.md
Documentation~/History/010-Phase-History.md
```

## ADR policy

ADR files are not deleted by this cleanup. They remain the detailed decision archive.
