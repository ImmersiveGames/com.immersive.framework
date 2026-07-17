# 01 — Roadmap

Status: **P3 Scene Local Player sequence selected**  
Last reconciled: **2026-07-16**  
Decisions: `../ADRs/P3-ADR-Canonical-Player-Lane.md`, `../ADRs/Product/ADR-PROD-0013-scene-local-player-admission.md`

For the exact operational state, read `05-Execution-Status.md`.

## Status vocabulary

| Status | Meaning |
|---|---|
| Closed | Decision or implementation cut completed with its required evidence. |
| Active | The single selected execution cut. |
| Ordered | Accepted future cut with a fixed position after the active cut. |
| Blocked | Cannot start until an explicit preceding gate closes. |
| Candidate | Valuable future work not selected for execution. |
| Superseded | Historical shape removed from the supported product. |

## Closed baseline

Camera C9 remains closed at the accepted single-output product level:

```text
Local Player 50 < Activity 100 < Route 200 < Session 300
```

The output is session-owned in `UIGlobal`; request publication and release are explicit and output-scoped.

The canonical P3 join/ActorProfile lane exists in Framework and QA source. Its H5 Unity regression gate remains manual and is not inferred from Git state.

## Selected P3M sequence

| Order | Cut | Type | Objective | Status |
|---:|---|---|---|---|
| 0 | P3M0 | baseline/documentation | Freeze the read-only package and QA source baseline without claiming Unity PASS. | closed by this patch |
| 1 | P3M1 | architecture/documentation | Define Scene Local Player Admission and reconcile Player/Camera decisions. | closed by this patch |
| 2 | P3M2 | technical | Decouple Camera, shared Editors and QA from `PreAuthoredPlayerComposer`. | active |
| 3 | P3M3 | destructive removal | Remove PreAuthored runtime, Editor, menus, serialized consumers and dedicated smoke. | ordered |
| 4 | P3M4 | technical + UX/product | Promote Scene Local Player Admission atomically into `com.immersive.framework`. | ordered |
| 5 | P3M5 | QA | Prove admission, rollback, release, retry, multi-binding compensation and manual-join regression. | ordered |
| 6 | P3M6 | integration real | Prove the official surface in FIRSTGAME. | blocked by P3M5 |
| 7 | P3M7 | documentation/product | Add the official sample and concise usage guide. | blocked by P3M6 |

## Active cut — P3M2

### Objective

Remove every direct dependency from Camera, shared Editor surfaces and Camera/Player QA fixtures to:

```text
PreAuthoredPlayerComposer
PreAuthoredPlayerRecipe
```

The PreAuthored files remain present during P3M2. Removal occurs only in P3M3 after all external consumers are migrated.

### Product surface affected

```text
CameraRigComposer target source
LocalPlayerCameraRequestBinding target evidence
Camera Inspector
Player declaration Inspector
QA camera fixtures and installers
```

### Required shape

```text
prepared Player/Actor target provider
or explicit Transform target source
-> CameraRigComposer
-> typed CameraRequest
-> CameraOutputContext
```

### Technical acceptance

```text
zero Camera runtime dependency on PreAuthoredPlayerComposer
zero Camera Editor dependency on PreAuthoredPlayerComposer
zero Camera QA dependency on PreAuthoredPlayerComposer
required target failures remain explicit
no Camera.main, name, tag or hierarchy fallback
manual join lane remains unchanged
package and QA compile in Unity
```

### Product acceptance

```text
Camera Inspector speaks in target-source terms
Camera can consume Player, Actor or explicit Transform evidence
Designer does not need a Player Composer to configure a rig
Advanced/Debug still exposes resolved Follow and LookAt targets
```

## Guardrails

- Git repositories remain read-only; every change is delivered as a `.zip` preserving relative paths.
- The package contains official reusable solutions.
- QA proves technical contracts before FIRSTGAME integration.
- FIRSTGAME proves usability and must not become the permanent home of official contracts.
- No compatibility shim, fallback discovery, global manager or service locator.
- Failures of required state remain explicit and diagnostic.
- Only one cut is active.

## Suggested commit for this documentation cut

```text
Docs: define Scene Local Player Admission sequence
```
