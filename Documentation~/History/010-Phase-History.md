# 010 — Phase History

This is a compact phase history. It replaces scattered phase-roadmap notes as the first historical read.

## Baseline sequence

| Range | Theme | Historical result |
|---|---|---|
| F0–F4 | Bootstrap, route and activity baseline | Closed baseline. |
| F5–F9 | Local contribution, release, content anchors, runtime materialization | Closed or superseded by later runtime materialization corrections. |
| F10–F12 | Snapshot, pause and cycle reset foundation | Baselines closed; cycle reset is not the current object reset model. |
| F13–F16 | Object entry, local reset adapters, player participant baseline | Object entry remains separate; old reset participant path superseded. |
| F17–F23 | Gate, transition, pause, save, loading | Core surfaces closed at baseline level. |
| F24–F28 | Unity build surface, adapter boundary, gate/input correction | Planning and boundary corrections. |
| F29–F32 | Unity Input / InputMode / PlayerInput planning | Mostly planning and passive evidence; not a framework input manager. |
| F34–F38 | Pause input, transition gate, Unity PlayerInput gate | Practical pause/transition input blocking closed. |
| F39–F44 | Old reset/restart/runtime participation lane | Superseded by Reset Reform preview.12. |

## Reset Reform preview.12

| Cut | Result |
|---|---|
| 12A | Reset registry and subject/participant primitives. |
| 12B | Unity reset subject adapter and built-in participants. |
| 12C | Reset executor. |
| 12D | Object reset trigger rewrite. |
| 12E | Activity restart integration. |
| 12F | Runtime prefab reset. |
| 12G | Old reset path cleanup and ADR supersession. |
| 12H | Activity restart visual ordering. |
| 12I | Unity reset subject adapter log cleanup. |
| 12J | Framework-owned `IUnityResettable` gameplay component bridge. |
| 12K | HTML guide advanced reset/programmatic usage. |

## Current conclusion

The current reset model is not the old ObjectEntry/ObjectReset path. It is:

```text
ResetSubject
ResetParticipant
ResetRegistry
ResetSelectionConfig
ResetExecutor
UnityResetSubjectAdapter
IUnityResettable
```

FIRSTGAME validated the usage model with player + runtime objects + reset room + activity restart.
