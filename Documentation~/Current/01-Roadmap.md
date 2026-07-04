# 01 — Roadmap

Status: **canonical roadmap after Reset Reform closure**.

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
| POST-RESET-A — Documentation reconciliation | Active | Consolidate current docs, numbered history and roadmap after Reset Reform. |

## Closed stable lanes

| Lane | Status | Notes |
|---|---|---|
| Bootstrap / Route / Activity baseline | Closed/Frozen | Core app navigation baseline. |
| UIGlobal / Transition / Loading / Pause | Closed/Frozen | Visual/runtime surfaces exist and are validated at baseline level. |
| Gate / Unity PlayerInput gate | Closed/Frozen | Practical Pause/Transition input blocking exists. |
| Save boundaries | Closed/Frozen | Snapshot/preferences/progression-adapter boundary exists; save engine remains future. |
| Reset Reform preview.12 | Closed | Current reset/restart model. |
| FIRSTGAME reset usage model | Closed | Real usage proof passed. |

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

Choose explicitly. Do not run all in parallel.

| Option | Candidate lane | Why it matters | Risk if skipped |
|---|---|---|---|
| A | FIRSTGAME Usage Model Hardening | Turn validated pieces into clear developer/game-designer usage. | Framework works but usage remains tribal knowledge. |
| B | Transition / Loading Surface Hardening | Improve progress/fade/readiness evidence and failure clarity. | Visual lifecycle becomes hard to debug. |
| C | Runtime Spawned Object / Materialization Track | Clarify runtime prefab/materialization ownership beyond reset. | Runtime objects remain ad-hoc per game. |
| D | Player / Actor / Camera Practical Track | Move from reset/flow into player-facing gameplay usage. | FIRSTGAME remains a reset demo instead of a gameplay proof. |
| E | Progression Save Adapter Track | Start real game-save handoff while keeping engine interchangeable. | Save boundary stays theoretical. |

## Recommendation

Next selected lane should be:

```text
Option A — FIRSTGAME Usage Model Hardening
```

Reason: after Reset Reform, the largest near-term risk is not missing core behavior; it is unclear developer usage. Lock the model with examples before expanding runtime scope.
