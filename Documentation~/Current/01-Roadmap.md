# 01 — Roadmap

Status: **P3M3 source prepared; Unity validation pending**
Last reconciled: **2026-07-17**
Decisions: `../ADRs/P3-ADR-Canonical-Player-Lane.md`, `../ADRs/Product/ADR-PROD-0013-scene-local-player-admission.md`

For the exact operational state, read `05-Execution-Status.md`.

## Status vocabulary

| Status | Meaning |
|---|---|
| Closed | Decision or implementation cut completed with its required evidence. |
| Active | The single selected execution cut. |
| Source prepared | Files are prepared, but required Unity evidence is still pending. |
| Ordered | Accepted future cut with a fixed position after the active cut. |
| Blocked | Cannot start until an explicit preceding gate closes. |
| Superseded | Historical shape removed from the supported product. |

## Closed baseline

Camera C9 remains closed at the accepted single-output product level:

```text
Local Player 50 < Activity 100 < Route 200 < Session 300
```

P3M2 is closed with C9M, C9R, canonical P3 and pre-removal P3B evidence supplied from Unity.

## Selected P3M sequence

| Order | Cut | Type | Objective | Status |
|---:|---|---|---|---|
| 0 | P3M0 | baseline/documentation | Freeze the read-only package and QA source baseline without claiming Unity PASS. | closed |
| 1 | P3M1 | architecture/documentation | Define Scene Local Player Admission and reconcile Player/Camera decisions. | closed |
| 2 | P3M2 | technical | Decouple Camera, shared Editors and QA from the alternative Player Composer. | closed — Unity evidence supplied |
| 3 | P3M3 | destructive removal | Remove alternative runtime, Editor, menus, serialized consumers and dedicated smoke. | active — source prepared, Unity validation pending |
| 4 | P3M4 | technical + UX/product | Promote Scene Local Player Admission atomically into `com.immersive.framework`. | blocked by P3M3 validation |
| 5 | P3M5 | QA | Prove admission, rollback, release, retry, multi-binding compensation and manual-join regression. | ordered |
| 6 | P3M6 | integration real | Prove the official surface in FIRSTGAME. | blocked by P3M5 |
| 7 | P3M7 | documentation/product | Add the official sample and concise usage guide. | blocked by P3M6 |

## Active cut — P3M3

### Removal shape

```text
remove alternative Player Composer runtime and Recipe
remove Composer Editor and Apply/Rebuild
remove P3B alternative smoke
remove the temporary Camera compatibility bridge
repair old QA scene Missing Scripts explicitly
preserve canonical Player and Camera contracts
```

### Files and surfaces affected

```text
Runtime/PlayerAuthoring
Editor/PlayerAuthoring
CameraRigComposer
PlayerGameplayCameraEligibilityRuntimeContext
PlayerGameplayCameraEligibilityStatus
C9R scene installer
P3B QA smoke
Current documentation
```

### Required validation

```text
Framework import / compile
QAFramework import / compile
C9R setup succeeds without removal warnings
C9M PASS — 6 cases
C9R PASS — 11 cases
canonical P3 aggregate PASS — 31 cases
P3B menu absent
no Missing Script
```

## P3M4 entry gate

P3M4 may start only after P3M3 closes. It must promote Scene Local Player Admission as a complete product unit, not adapt the removed Composer and not copy staging paths blindly.

## Guardrails

- Git repositories remain read-only to the agent; changes are delivered in `.zip` form.
- The package contains official reusable contracts and authoring.
- QA proves technical contracts before FIRSTGAME integration.
- No compatibility shim, fallback discovery, global manager or service locator.
- Required failures remain explicit and diagnostic.
- Only one cut is active.

## Suggested commit after validation

```text
Remove: delete PreAuthored Player surface
```
