# 01 — Roadmap

Status: **canonical roadmap after F49 lane selection**.

## Reading rule

```text
Current capability first
Historical ADRs second
Candidate tracks only after explicit selection
```

Do not treat historical phase numbers as an active queue. Some phases are closed, some are superseded, and some were documentation-only.

## Status vocabulary

| Status | Meaning |
|---|---|
| Closed | Implemented/documented and validated by the appropriate evidence. |
| Frozen | Validated baseline; new behavior needs a new cut. |
| Superseded | Historical decision retained for traceability but no longer the current model. |
| Documentation-only | Planning/governance work without runtime behavior. |
| Candidate | Possible future lane; not selected. |
| Active | Current selected lane. Keep one active lane unless explicitly stated. |

## Active lane

| Lane | Status | Scope |
|---|---|---|
| F49 — Player Topology, Player Entry and PlayerView Ownership | Active | Implement player-facing boundaries from Actor readiness to PlayerEntry, PlayerView, control binding and optional Unity PlayerInput integration. |

## Active lane rule

F49 must run package-first and QA-first:

```text
1. Implement or adjust contracts/runtime/editor/docs in com.immersive.framework.
2. Validate technical behavior in QAFramework.
3. Validate practical usability in planet-devourer / FIRSTGAME only after QA is clean.
```

FIRSTGAME is not the laboratory for new F49 contracts.

## Closed stable lanes

| Lane | Status | Notes |
|---|---|---|
| Bootstrap / Route / Activity baseline | Closed/Frozen | Core app navigation baseline. |
| UIGlobal / Transition / Loading / Pause | Closed/Frozen | Visual/runtime surfaces exist and are validated at baseline level. |
| Gate / Unity PlayerInput gate | Closed/Frozen | Practical Pause/Transition input blocking exists. |
| Save boundaries | Closed/Frozen | Snapshot/preferences/progression-adapter boundary exists; save engine remains future. |
| Reset Reform preview.12 | Closed | Current reset/restart model. |
| FIRSTGAME reset usage model | Closed | Real usage proof passed. |
| Consumer role rule | Frozen | QA proves technical behavior; FIRSTGAME proves game-start usability; package owns canonical docs/contracts. |
| Consumer project separation POST-RESET-B1-B6F | Closed | Package, QA Project and FIRSTGAME roles are documented and frozen; consumer cleanup remains bounded by Unity serialization rules. |

## Superseded reset history

These historical ADRs remain for traceability, but the implementation model is now Reset Reform preview.12:

| Historical item | Current replacement |
|---|---|
| F39 Object Reset Group old path | `ResetSelectionConfig` + `ResetExecutor`. |
| F40 Activity Restart via old group dependency | `ActivityRestartTrigger` owns selection and executes inside restart transition. |
| F41 Reset Selection draft | Current `ResetSelectionConfig`. |
| F42 Awaitable reset draft | Current `ResetExecutor.ExecuteAsync` and restart composition. |
| F44 Runtime Object Participation | `UnityResetSubjectAdapter` runtime subject id generation. |

## Candidate next lanes

Do not run these in parallel with F49 unless explicitly selected by a later decision.

| Option | Candidate lane | Why it matters | Risk if skipped |
|---|---|---|---|
| A | FIRSTGAME Usage Model Hardening | Turn validated pieces into clear developer/game-designer usage. | Framework works but usage remains tribal knowledge. |
| B | Transition / Loading Surface Hardening | Improve progress/fade/readiness evidence and failure clarity. | Visual lifecycle becomes hard to debug. |
| C | Runtime Spawned Object / Materialization Track | Clarify runtime prefab/materialization ownership beyond reset. | Runtime objects remain ad-hoc per game. |
| D | Progression Save Adapter Track | Start real game-save handoff while keeping engine interchangeable. | Save boundary stays theoretical. |

## F49 recommendation

Current selected sequence:

```text
F49A — ADR normalization and package boundary cleanup
F49B — Actor Readiness passive contracts
F49C — Actor Readiness Unity adapter + QA smoke
F49D — PlayerEntry passive model
F49E — PlayerTopology policy contracts + validator foundation
F49F — PlayerEntry transition rules
F49G — PlayerView passive declaration + camera precedence contract
F49H — CameraDirector integration point for Active PlayerView
F49I — ControlBinding boundary + permission diagnostics
F49J — Optional PlayerInput / PlayerInputManager bridge
F49K — FIRSTGAME validation pass
F49L — Documentation, ADR acceptance and next-phase handoff
```

## Consumer cleanup guardrail

Future cleanup must preserve the split in [`03-Consumer-Project-Roles.md`](03-Consumer-Project-Roles.md).

Do not rename or move Unity serialized scenes, prefabs, assets or attached MonoBehaviour scripts from consumer projects without a Unity Editor migration plan.
