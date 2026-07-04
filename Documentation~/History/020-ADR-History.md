# 020 — ADR History

Detailed ADR files remain in `../ADRs/`. This file is the compact navigation layer.

## How to read ADRs

| Need | Read |
|---|---|
| Current behavior | `../Current/00-Current-State.md` first. |
| Current roadmap | `../Current/01-Roadmap.md`. |
| Detailed decision | Specific ADR in `../ADRs/`. |
| Whether an ADR is still current | This history and `../ADRs/ADR-INDEX.md`. |

## ADR groups

| Group | Meaning |
|---|---|
| F00–F04 | Baseline reconciliation, identity, diagnostics, route/activity. |
| F05–F09 / F8R / F9R | Local contribution, release, scene/runtime materialization and content anchors. |
| F10–F12 | Input, pause, snapshot and cycle reset foundation. |
| F13–F16 | Object entry, local object reset, Unity adapters and player baseline. |
| F17–F23 | Gate, transition, pause, save and loading. |
| F24–F28 | Unity build surface, adapter boundary and input/gate planning. |
| F34–F38 | Pause input, transition gate and Unity PlayerInput gate adapter. |
| F39–F44 | Reset/restart/runtime participation decisions superseded or amended by Reset Reform. |
| FXX | Cross-cutting consolidation notes. |

## Superseded reset ADR group

| ADR | Current status |
|---|---|
| F39 — Object Reset Group | Historical. Current group reset is `ObjectResetGroupTrigger` + `ResetSelectionConfig` + `ResetExecutor`. |
| F40 — Activity Restart via Object Reset Group | Amended. Current restart owns reset selection and executes inside one transition window. |
| F41 — Reset Selection Policy | Superseded by current `ResetSelectionConfig`. |
| F42 — Runtime Awaitable Reset/Restart | Amended by current `ResetExecutor.ExecuteAsync` and restart composition. |
| F43 — Reset/Restart Authoring Validation | Current validation concept remains relevant. |
| F44 — Runtime Object Participation Foundation | Superseded by `UnityResetSubjectAdapter` runtime id generation. |

## ADR retention policy

Keep detailed ADR files even when superseded. They explain why the shape changed. Do not use them as setup instructions unless `Current/` points to them explicitly.
