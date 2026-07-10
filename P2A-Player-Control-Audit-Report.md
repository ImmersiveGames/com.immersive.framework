# P2A Player Control Audit Report

## Objective

Close Player Control Product authority, lifetime, identity, binding, Gate and movement boundaries from current code evidence.

## Repositories audited

- `com.immersive.framework`: Player authoring, passive models, F52 adapters, Gate, Pause, Transition and runtime host.
- `QAFramework`: Player topology/readiness, control binding, PlayerInput bridge/activation and Gate evidence.
- `planet-devourer`: real PlayerComposer, PlayerInput, Gate, F52 targets, movement, reset/restart and CameraComposer.

Consumer repositories were read-only.

## Primary decision

`PlayerControlRuntimeContext` is the per-Player runtime authority. A generated `PlayerControlRuntimeContextBehaviour` owns its Unity lifetime. PlayerComposer authors and materializes the dependencies but does not execute control.

Lifetime is tied to the Player instance: bind on enabled/valid authored Player, unbind on disable/destroy/explicit release, and remain bound across Pause, Transition and Activity Restart.

## Evidence summary

| Finding | Severity | Evidence | Decision |
|---|---|---|---|
| No control runtime authority exists. | High | F52 adapters explicitly disclaim lifecycle ownership. | Add one per-Player context in P2E. |
| PlayerComposer already owns correct product entry. | Architectural | Recipe, typed PlayerInput, Validate and Apply/Rebuild exist. | Extend PlayerComposer in P2B; do not add another facade. |
| Identity sources are typed and separated. | Architectural | PlayerSlotDeclaration and PlayerActorDeclaration. | Slot is binding key; Actor is participant guard. |
| F52 operations are explicit but not transactional. | High | Separate Bind, Bridge, Activate and Clear calls. | Context owns ordered apply/rollback. |
| Gate already blocks FIRSTGAME input. | Architectural | UnityPlayerInputGateAdapter + Pause/Transition Gate snapshots. | Gate changes availability without unbinding. |
| Slot identity is duplicated as strings. | Medium | Both F52 Unity targets serialize `expectedPlayerSlotId`. | Replace authority with typed slot evidence/context value. |
| Composer-created Gate adapter may miss typed slot reference. | High | Apply utility writes string candidates, while `sourceSlot` is an object reference. | P2B must assign PlayerSlotDeclaration explicitly. |
| FIRSTGAME has duplicate F52 target sets. | High | Targets exist on Player root and `_Framework/_Bindings`. | Block ambiguity; migrate explicitly in P2G. |
| Dedicated PlayerComposer QA is missing. | High | QA contains Composer only as Camera fixture. | P2A-QA0 is mandatory before P2B. |
| Existing Gate adapter uses static host lookup/polling. | Medium | `FrameworkRuntimeHost.TryGetCurrent` from Update. | Do not duplicate or expand this pattern in P2. |

## QA inventory

Existing QA covers:

- passive PlayerControl and topology;
- Player binding readiness/diagnostics;
- successful and negative PlayerControl binding;
- typed PlayerInput bridge;
- action-map activation and restoration;
- slot mismatch and missing dependencies;
- Gate slot evidence and Pause/InputMode paths.

Missing:

- PlayerComposer Validate/Apply/Rebuild regression;
- Recipe default preservation;
- Composer idempotency as a dedicated smoke;
- duplicate canonical-owner detection;
- transactional bind rollback;
- combined Gate + activation ownership;
- per-Player context lifetime and release.

## Required next gate

`P2A-QA0 — PlayerComposer Product Surface Regression Smoke` must precede P2B.

It establishes the current supported baseline and exposes the duplicate-topology gap before PlayerComposer serialization/materialization changes.

## Approved sequence

```text
P2A-QA0 -> P2B -> P2C -> P2D -> P2E -> P2F -> P2G
```

Detailed decisions: `Documentation~/Current/11-Player-Control-Authority-Audit.md`.

## Risks

- partial binding after a later step fails;
- competing action-map owners;
- hidden identity fallback;
- duplicate target ambiguity;
- coupling movement semantics to framework runtime;
- widening existing global host lookup.

## Out of scope confirmed

No runtime, contracts, smokes, PlayerComposer code, QAFramework, FIRSTGAME, scenes, prefabs or assets were changed.

## Validation

Static/read-only audit only. No Unity compile/import, Play Mode or smoke was run; no operational PASS is claimed.
