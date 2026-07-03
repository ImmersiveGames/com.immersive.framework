# Immersive Framework ADR Index

This index is the canonical ADR navigation for `com.immersive.framework`.

Current order:

1. F21 - Save / Snapshot / Preferences / Progression Save Foundation
2. F22 - Loading Operation / Progress / Readiness Boundary
3. F23 - Pause Content / Overlay / Input Intent Boundary
4. F24 - Unity Build Surface / Lifecycle Wiring
5. F25 - Activity Content Scene Composition / Operation Reset
6. F26 - Activity Discovery / Loading Progress Closeout
7. F27 - Pause UIGlobal / Input / Gate Reframe
8. F28 - Roadmap Reconciliation and Adapter Module Spine
9. F29 - Unity Input Target Ownership Proof
10. F30 - InputMode Identity and Request Result Model
11. F31 - PlayerActor Identity and Unity Input Evidence
12. F32 - InputMode Unity Adapter Application
13. F33 - Pause Runtime PlayerInput Wiring
14. F34 - Direct Pause Input Trigger for Consumer Keyboard Toggle
15. F35 - Pause TimeScale and Simple PlayerInput Map Switching
16. F36 - Global Pause Input Action
17. F37 - Transition Gate Policy
18. F38 - Unity PlayerInput Gate Adapter
14. POST-F33-A - Matrix Reconciliation Closeout
15. POST-F33-B - Officialize/Reclassify F28-F33
16. F8R-A - RuntimeContent / ContentAnchor Materialization Audit
17. F8R-B - Runtime Root / Handle / Release Policy
18. F8R-C - Runtime Materialization Adapter Boundary
19. F8R-D - Physical Release Adapter
20. F9R-A - ContentAnchor Runtime Binding Re-entry

## Roadmap ADRs

| Phase | ADR | Status |
|---|---|---|
| F00 | [Baseline Reconciliation](F00-ADR-BL-001-Baseline-Reconciliation.md) | Accepted |
| F00 | [Core vs Consumers](F00-ADR-BL-002-Core-vs-Consumers.md) | Accepted |
| F01 | [Framework Facts](F01-ADR-DIAG-001-Framework-Facts.md) | Accepted |
| F01 | [Typed Identity Policy](F01-ADR-ID-001-Typed-Identity-Policy.md) | Accepted |
| F02 | [Session Scope](F02-ADR-SESSION-001-Session-Scope.md) | Accepted |
| F03 | [Route Baseline](F03-ADR-ROUTE-001-Route-Baseline.md) | Accepted |
| F04 | [Activity Baseline](F04-ADR-ACTIVITY-001-Activity-Baseline.md) | Accepted |
| F05 | [Local Identity and Contribution](F05-ADR-LOCAL-001-Local-Identity-and-Contribution.md) | Accepted |
| F06 | [Route Scene Composition](F06-ADR-SCENE-001-Route-Scene-Composition.md) | Accepted |
| F06 | [Content Release](F06-ADR-RELEASE-001-Content-Release.md) | Accepted |
| F07 | [Content Anchor Declaration](F07-ADR-ANCHOR-001-Content-Anchor-Declaration.md) | Accepted |
| F08 | [Runtime Materialization](F08-ADR-RUNTIME-001-Runtime-Materialization.md) | Accepted |
| F09 | [Content Anchor Binding](F09-ADR-ANCHOR-002-Content-Anchor-Binding.md) | Accepted |
| F10 | [Input Ownership](F10-ADR-INPUT-001-Input-Ownership.md) | Accepted |
| F10 | [Pause as Consumer](F10-ADR-PAUSE-001-Pause-as-Consumer.md) | Accepted |
| F10 | [Snapshot Model](F10-ADR-SNAPSHOT-001-Snapshot-Model.md) | Accepted |
| F11 | [Cycle Reset Foundation](F11-ADR-RESET-001-Cycle-Reset-Foundation.md) | Accepted |
| F12 | [Cycle Reset Integration Authoring UX](F12-ADR-RESET-002-Cycle-Reset-Integration-Authoring-UX.md) | Accepted |
| F13 | [Object Entry Foundation](F13-ADR-OBJECT-001-Object-Entry-Foundation.md) | Accepted |
| F14 | [Local Object Reset Foundation](F14-ADR-RESET-003-Local-Object-Reset-Foundation.md) | Accepted |
| F15 | [Unity Reset Adapters](F15-ADR-RESET-004-Unity-Reset-Adapters.md) | Accepted |
| F16 | [GameObject Active State Reset](F16-ADR-RESET-005-GameObject-Active-State-Reset.md) | Accepted |
| F16 | [Player Participant Entry Baseline](F16-ADR-PLAYER-001-Player-Participant-Entry-Baseline.md) | Accepted |
| F17 | [Gate Boundary](F17-ADR-GATE-001-Gate-Boundary.md) | Accepted |
| F17 | [Advanced Consumers Boundary](F17-ADR-CONSUMERS-001-Advanced-Consumers-Boundary.md) | Accepted |
| F18 | [Transition Orchestration](F18-ADR-TRANSITION-001-Transition-Orchestration.md) | Accepted |
| F19 | [Transition Effects Boundary](F19-ADR-TRANSITION-002-Transition-Effects-Boundary.md) | Accepted |
| F20 | [Pause State and Gate](F20-ADR-PAUSE-002-Pause-State-and-Gate.md) | Accepted |
| F21 | [Save / Snapshot / Preferences / Progression Boundary](F21-ADR-SAVE-001-Save-Snapshot-Preferences-Progression-Boundary.md) | Accepted |
| F22 | [Loading Operation / Progress / Readiness Boundary](F22-ADR-LOADING-001-Loading-Operation-Progress-Readiness-Boundary.md) | Accepted |
| F23 | [Pause Content / Overlay / Input Intent Boundary](F23-ADR-PAUSE-003-Pause-Content-Overlay-Input-Boundary.md) | Closed |
| F24 | [Unity Build Surface / Lifecycle Wiring](F24-ADR-UNITY-BUILD-001-Unity-Build-Surface-Lifecycle-Wiring.md) | Accepted / Planned |
| F25 | [Adapter Module Foundation](F25-ADR-ADAPTER-001-Adapter-Module-Foundation.md) | Deferred after Activity scene operation stability |
| F25R | [Activity Scene Operation Architecture Reset](F25R-ADR-ACTIVITY-001-Activity-Scene-Operation-Architecture-Reset.md) | Accepted / Documentation reset |
| F27 | [Gate as Capability Admission Boundary](F27-ADR-GATE-INPUT-001-Capability-Gate-Boundary.md) | Accepted / F27D runtime reframe |
| F28 | [Roadmap Reconciliation and Adapter Module Spine](F28-ADR-INPUT-001-InputMode-Adapter-Boundary.md) | Accepted / F28A-F28F closed / F29 selected |
| F34 | [Direct Pause Input Trigger for Consumer Keyboard Toggle](F34-ADR-PAUSE-004-Direct-Pause-Input-Trigger.md) | Accepted / FIRSTGAME-2B PASS / preview.5 / amended by F35 |
| F35 | [Pause TimeScale and Simple PlayerInput Map Switching](F35-ADR-PAUSE-005-TimeScale-and-Simple-Map-Switching.md) | Accepted / FIRSTGAME-2C PASS / preview.6 / amended by F36 |
| F36 | [Global Pause Input Action](F36-ADR-PAUSE-006-Global-Pause-Input-Action.md) | Accepted / FIRSTGAME-2D / preview.7 |
| F37 | [Transition Gate Policy](F37-ADR-TRANSITION-001-Transition-Gate-Policy.md) | Accepted / preview.8 |
| F38 | [Unity PlayerInput Gate Adapter](F38-ADR-INPUT-001-Unity-PlayerInput-Gate-Adapter.md) | Accepted / preview.9 |
| F39 | [Object Reset Group](F39-ADR-RESET-006-Object-Reset-Group.md) | Accepted / preview.10 |
| POST-F33-A | `Assets/_Documentation/Notes/POST-F33-A-Matrix-Reconciliation-Closeout.md` | Accepted / documentation / roadmap governance |
| POST-F33-B | `Assets/_Documentation/Notes/POST-F33-B-Officialize-Reclassify-F28-F33.md` | Accepted / documentation / roadmap governance |
| F8R-A | `Assets/_Documentation/Audits/F8R-A-RuntimeContent-ContentAnchor-Materialization-Audit.md` | Draft / audit-only / documentation governance |
| F8R-B | [Runtime Root / Handle / Release Policy](F8R-B-ADR-Runtime-Root-Handle-Release-Policy.md) | Accepted / RuntimeContent / Materialization Governance |
| F8R-C | [Runtime Materialization Adapter Boundary](F8R-C-ADR-Runtime-Materialization-Adapter-Boundary.md) | Accepted / RuntimeContent / Materialization Adapter Boundary |
| F8R-D | [Physical Release Adapter](F8R-D-ADR-Physical-Release-Adapter.md) | Accepted / RuntimeContent / Physical Release Adapter Boundary |
| F9R-A | [ContentAnchor Runtime Binding Re-entry](F9R-A-ADR-ContentAnchor-Runtime-Binding-Reentry.md) | Proposed / ContentAnchor / Runtime Binding / Materialization Governance |

## Boundary Rules

- F23 is intent/requirement-only.
- F24 is Unity build surface and lifecycle wiring.
- F25 now documents the Activity scene operation reset and the compact F25R history record through F25I.
- F25R resets Activity scene operation architecture: visual policy, LoadingSurface, TransitionSurface, scene load/release, Route startup unification and ledger ownership must be decided by `ActivityOperationPlan`.
- F25R1 is folded into the F25R history record and documents that the planner stays synchronous while the future executor boundary may use `UnityEngine.Awaitable`.
- Core contracts must not depend on concrete Unity UI, gameplay modules or backend implementations.
- Adapter modules consume framework contracts; they do not redefine route, activity, transition, pause, loading, save or reset ownership.


## F34 — Direct Pause Input Trigger

F34 accepts `PauseInputActionTrigger` as the canonical simple consumer path for keyboard Pause. It submits `Pause`, `Resume` or `Toggle` directly to `FrameworkRuntimeHost.RequestPause(...)` from Unity Input System actions. It does not switch InputMode, apply PlayerInput action maps, require Player/Actor evidence, join players, spawn actors or read gameplay commands.

The advanced path `PauseInputActionRuntimeBridgeTrigger + PauseInputModeUnityPlayerInputRuntimeBridge` remains reserved for explicit InputMode / PlayerInput ownership cuts.

## F35 — Pause TimeScale and Simple Map Switching

F35 accepts that the playable consumer Pause must pause the basic Unity simulation by default. `PauseRuntime` captures the current running `Time.timeScale`, applies `Time.timeScale = 0` when entering Pause, and restores the captured value when resuming. The Pause contract remains the runtime state/result snapshot; `Time.timeScale` is the default simulation effect, not the whole identity of Pause.

F35 also accepted optional direct Unity `PlayerInput` action map switching. F36 amends the input authoring path: Pause input now uses one global `Global/Pause` command, and direct action-map switching is optional/explicit rather than part of the normal Pause input lane.

## F36 — Global Pause Input Action

F36 replaces the duplicated `Player/Pause + UI/Pause` normal path with a single global/system action. `PauseInputActionTrigger` resolves `Global/Pause` by default, and same-frame dedupe remains only defensive. UI click/point/submit must stay on the EventSystem/UI input lane, not on a hidden keep-alive side effect inside the Pause trigger.



## F27 Gate/Input correction

F27C accepts that Gate is a capability/admission boundary and not a component blocker. F27D applies a diagnostic/runtime vocabulary correction: Pause now derives `Input/InputAcceptance` and `Interaction/InteractionAcceptance` blockers, not broad gameplay/component blockers.

F27E is cancelled. Ordinary input consumers should not each query Gate as the primary Pause/Input implementation. InputMode and PlayerInput ownership must be planned first.

## F28 roadmap correction

F28 is documentation-first. It turns the F27D freeze into a progressive completion plan: baseline reconciliation, dependency map, adapter module taxonomy, Player/Actor/Input ownership plan, InputMode/Pause integration plan and next implementation closeout. F28A is closed as documentation-only baseline reconciliation. F28B is closed as documentation-only completion dependency mapping. F28C is closed as documentation-only adapter module taxonomy. F28D is closed as documentation-only Player/Actor/Input ownership planning. F28E is closed as documentation-only InputMode/Pause integration planning. F28F is closed as documentation-only next implementation closeout and selects F29 — Unity Input Target Ownership Proof.

InputMode remains a future boundary, but it is positioned after adapter/module ownership, Player/Actor ownership and Unity Input target ownership are explicit.


## F28D Closure Note

F28D closes Player / Actor / Unity Input ownership planning. Reference: `../../../../Assets/_Documentation/Notes/F28D-Player-Actor-Input-Ownership-Plan.md`.

## F28E Closure Note

F28E closes typed InputMode and Pause integration semantics. Reference: `../../../../Assets/_Documentation/Notes/F28E-InputMode-Pause-Integration-Plan.md`.

## F28F Closure Note

F28F closes F28 and selects F29 — Unity Input Target Ownership Proof as the first implementation phase after the documentation gate. Reference: `../../../../Assets/_Documentation/Notes/F28F-Next-Implementation-Closeout.md`.


## F29 implementation note

F29 is tracked by the project-side plan/notes rather than a new ADR. F29A implements declaration-only Unity Input target ownership proof. F29B adds authored QA fixture evidence. F29C closes the phase and selects F30 — InputMode Identity and Request Result Model. F29 does not introduce InputMode behavior, action-map switching or PlayerInput ownership.


## F29C Closure Note

F29C closes Unity Input target ownership proof after synthetic and authored QA smoke evidence passed. Reference: `../../../../Assets/_Documentation/Notes/F29C-Input-Target-Closeout.md`.

## F30 Planning Note

F30 opens as InputMode identity and request/result planning. Reference: `../../../../Assets/_Documentation/Plans/F30-PLAN-InputMode-Identity-And-Request-Result.md`.

F30A closes passive InputMode identity/state/request/result contracts and `InputMode Contract Smoke`. Reference: `../../../../Assets/_Documentation/Notes/F30A-InputMode-Identity-State-Request-Result.md`.

F30B closes a corrective Unity PlayerInput integration boundary. Unity `PlayerInput` and `PlayerInputManager` remain the official input execution components; the framework must not introduce a replacement input manager. Reference: `../../../../Assets/_Documentation/Notes/F30B-Unity-PlayerInput-Integration-Boundary.md`.

- F30C — Unity PlayerInput Component Evidence Validation: implemented as passive evidence validation; no custom input manager.
- F30C1 — PlayerInputManager Smoke Warning Cleanup: corrects QA duplicate-manager smoke to avoid creating real duplicate Unity `PlayerInputManager` components.

- F30D — Pause InputMode Request Boundary: closes passive mapping from logical Pause state/result to `InputModeRequest` (`Paused` -> `PauseOverlay`, `Running` -> `Gameplay`) without PlayerInput ownership, PlayerInputManager ownership, action-map switching or Pause dispatch. Reference: `../../../../Assets/_Documentation/Notes/F30D-Pause-InputMode-Request-Boundary.md`.


## F30E Closure Note

F30 is closed as passive InputMode and Unity Input boundary language. F30 does not introduce a framework input manager, action-map switching, PlayerInput activation/deactivation, player join or actor spawn. Reference: `../../../../Assets/_Documentation/Notes/F30E-InputMode-Unity-Input-Boundary-Closeout.md`.

## F31 — PlayerActor Identity

F31A accepts minimal `IActor` / `PlayerActor` identity and requires Unity `PlayerInput` evidence for PlayerActor declarations.

F31B accepts `PlayerInputManager` as Session-scoped Unity Input integration evidence. It is not Activity-owned and not Route-owned. Activities may consume player actors; the canonical manager belongs to Session evidence.

F31B1 removes the redundant same-variable smoke comparison that produced CS1718.

F31C closes the reference phase. Reference: `../../../../Assets/_Documentation/Notes/F31C-PlayerActor-Session-Input-Reference-Closeout.md`.


## F32 — InputMode Unity Adapter Application

F32 starts after F30E/F31C. The cancelled `F31D — PlayerInput Reference Set` is not part of the official sequence.

F32A accepts a pure InputMode Unity application preview. It consumes `InputModeRequestResult`, `UnityInputTargetSet`, `PlayerActorSet` and Session `UnityInputPlayerInputManagerEvidence` directly. It does not introduce action-map switching, PlayerInput activation/deactivation, PlayerInputManager join, actor spawning or a framework-owned input manager.

Reference: `../../../../Assets/_Documentation/Notes/F32A-InputMode-Unity-Application-Preview.md`.

- F32B — InputMode Unity Action Map Preview: passive action-map evidence only; no `PlayerInput.SwitchCurrentActionMap`.

- F32C — InputMode Unity Application Plan: side-effect-free adapter dry run after F32A/F32B.

- F32D note: `Assets/_Documentation/Notes/F32D-InputMode-Unity-PlayerInput-Adapter.md`.

- F32E — InputMode Unity PlayerInput Application: explicit PlayerInput application wrapper that activates PlayerInput before selecting Gameplay/UI action maps, delegates InputLocked to the F32D lock adapter, and still does not own PlayerInputManager, join, spawn or movement. Reference: `../../../../Assets/_Documentation/Notes/F32E-InputMode-Unity-PlayerInput-Application.md`.

- F32F — InputMode Unity PlayerInput Request Application: composed explicit request-to-PlayerInput application path; no PlayerInputManager join/spawn/movement.


## F32H Closure Note

F32H closes F32 as the explicit Unity `PlayerInput` application lane for typed `InputMode` requests and completed logical Pause results.

Accepted side effects are limited to explicit `PlayerInput` adapter/application calls:

```text
ActivateInput()
SwitchCurrentActionMap(actionMapName)
DeactivateInput()
```

F32H explicitly excludes automatic `PauseRuntime` wiring, automatic `FrameworkRuntimeHost` wiring, `PlayerInputManager.JoinPlayer`, player prefab spawn, movement, gameplay command reading and any framework-owned input manager.

Reference: `../../../../Assets/_Documentation/Notes/F32H-InputMode-Unity-PlayerInput-Application-Closeout.md`.

## F33 — Pause Runtime PlayerInput Wiring

F33 is closed through F33E after F32H. F33A accepts an opt-in scene-authored runtime bridge from logical Pause requests to explicit Unity `PlayerInput` application.

Accepted:

```text
preflight before Pause state mutation;
PauseRequest submission through FrameworkRuntimeHost;
PlayerInput application through the explicit F32 path;
no automatic host registration.
```

Rejected:

```text
framework-owned input manager;
PlayerInputManager.JoinPlayer;
player prefab spawn;
PlayerActor movement;
gameplay command reading;
hidden PauseRuntime side effects.
```

Reference: `../../../../Assets/_Documentation/Notes/F33A-Pause-Runtime-PlayerInput-Bridge.md`.


F33B accepts an opt-in Unity `InputAction` trigger for the F33A bridge. The trigger is the canonical authored Pause input path for Unity Input System actions after F33.

F33C retires the older F27B `UnityPauseInputActionAdapter` as an active runtime path. The class may remain as an inert migration stub, but it must not submit Pause requests directly because that bypasses the accepted `Pause -> InputMode -> PlayerInput` synchronization lane. Reference: `../../../../Assets/_Documentation/Notes/F33C-Legacy-Pause-InputAction-Adapter-Retirement.md`.

F33D flattens Pause input diagnostics. Trigger diagnostics may expose bridge status summary fields, but must not embed a full bridge diagnostic string, and bridge diagnostics must not embed the full PlayerInput application diagnostic string. Reference: `../../../../Assets/_Documentation/Notes/F33D-Pause-Input-Diagnostics-Flattening.md`.

F33E closes Pause Runtime PlayerInput Wiring. The canonical path is `PauseInputActionRuntimeBridgeTrigger` plus `PauseInputModeUnityPlayerInputRuntimeBridge`; the phase keeps automatic FrameworkRuntimeHost wiring, PlayerInputManager join, player spawn, movement, gameplay command reading and framework-owned input managers out of scope. Reference: `../../../../Assets/_Documentation/Notes/F33E-Pause-Runtime-PlayerInput-Wiring-Closeout.md`.

F33E1 corrects the F33E next-phase wording: F33 closes the Pause input path but does not select or authorize the next implementation phase. Reference: `../../../../Assets/_Documentation/Notes/F33E1-Next-Phase-Selection-Correction.md`.

## POST-F33-A — Matrix Reconciliation Closeout

Scope: documentation / roadmap governance.

Status: Accepted.

POST-F33-A accepts the matrix reconciliation closeout. F28-F33 are official only as controlled anticipation of the Input / Pause / Unity `PlayerInput` axis. F33 remains closed, but it does not select F34, gameplay or any other next feature phase. RuntimeContent, ContentAnchor, materialization, runtime roots, handles and release policy must be re-audited before consumer work resumes.

Reference: `Assets/_Documentation/Notes/POST-F33-A-Matrix-Reconciliation-Closeout.md`.

## POST-F33-B — Officialize/Reclassify F28-F33

Scope: documentation / roadmap governance.

Status: Accepted.

POST-F33-B officially reclassifies F28-F33 against the matrix. F28 is official planning/governance, F29 is official Unity Input target evidence, F30 is official passive InputMode / Pause request language, F31 is official PlayerActor identity and Session PlayerInputManager evidence, F32 is controlled anticipation for the explicit PlayerInput application lane, and F33 is controlled anticipation for opt-in Pause runtime to PlayerInput wiring.

POST-F33-B does not authorize F34/gameplay and does not select camera, audio, save/progression, pooling/runtime-spawned or actor materialization. `F8R-A — RuntimeContent / ContentAnchor Materialization Audit` remains the first technical candidate after this reclassification.

Reference: `Assets/_Documentation/Notes/POST-F33-B-Officialize-Reclassify-F28-F33.md`.

## F8R-A - RuntimeContent / ContentAnchor Materialization Audit

Scope: audit-only / documentation governance.

Status: Draft.

F8R-A records the current package state for RuntimeContent, ContentAnchor, materialization request/result, runtime roots, handles and release policy after POST-F33-B. It does not authorize F34/gameplay or any camera, audio, save/progression, pooling/runtime-spawned or actor materialization implementation.

Reference: `Assets/_Documentation/Audits/F8R-A-RuntimeContent-ContentAnchor-Materialization-Audit.md`.

## F8R-B - Runtime Root / Handle / Release Policy

Scope: RuntimeContent / Materialization Governance.

Status: Accepted.

F8R-B accepts that RuntimeContent core keeps root, handle and release policy as logical framework language. Future Unity adapters may materialize physical objects only after a separate accepted cut; physical root placement, destroy, pooling, scene unload and Addressables release stay outside core.

References:

- `F8R-B-ADR-Runtime-Root-Handle-Release-Policy.md`
- `Assets/_Documentation/Plans/F8R-B-PLAN-Runtime-Root-Handle-Release-Policy.md`
- `Assets/_Documentation/Notes/F8R-B1-Runtime-Root-Handle-Release-Policy-Acceptance.md`

## F8R-C - Runtime Materialization Adapter Boundary

Scope: RuntimeContent / Materialization Adapter Boundary.

Status: Accepted.

F8R-C accepts the boundary between pure RuntimeContent core and future physical materialization adapters. Pure core keeps request, result, identity, owner, scope, handle state, guards and diagnostics. Future Unity adapters may own physical materialization only after a separate accepted implementation cut, and physical object evidence must not leak into pure core.

References:

- `F8R-C-ADR-Runtime-Materialization-Adapter-Boundary.md`
- `Assets/_Documentation/Plans/F8R-C-PLAN-Runtime-Materialization-Adapter-Boundary.md`
- `Assets/_Documentation/Notes/F8R-C1-Runtime-Materialization-Adapter-Boundary-Acceptance.md`

## F8R-D - Physical Release Adapter

Scope: RuntimeContent / Physical Release Adapter Boundary.

Status: Accepted.

F8R-D accepts the boundary between logical RuntimeContent release and future physical cleanup adapters. RuntimeContent core keeps release as request/result/policy language, while Destroy, Pooling return, Addressables release and scene unload remain outside core until a later accepted implementation cut.

References:

- `F8R-D-ADR-Physical-Release-Adapter.md`
- `Assets/_Documentation/Plans/F8R-D-PLAN-Physical-Release-Adapter.md`
- `Assets/_Documentation/Notes/F8R-D1-Physical-Release-Adapter-Acceptance.md`

## F9R-A - ContentAnchor Runtime Binding Re-entry

Scope: ContentAnchor / Runtime Binding / Materialization Governance.

Status: Proposed.

F9R-A proposes that ContentAnchor runtime binding remains logical in the core. Binding may associate logical `RuntimeContentIdentity` / `RuntimeContentHandle` with logical ContentAnchor declarations, report diagnostics and clean logical bindings, but physical placement belongs to a future adapter.

References:

- `F9R-A-ADR-ContentAnchor-Runtime-Binding-Reentry.md`
- `Assets/_Documentation/Plans/F9R-A-PLAN-ContentAnchor-Runtime-Binding-Reentry.md`

## F37 - Transition Gate Policy

Scope: Transition / Gate / Route and Activity authoring.

Status: Accepted.

F37 adds explicit Route/Activity `TransitionGateMode` policy so visual transition windows can block lifecycle requests, input acceptance, interaction acceptance and gameplay actions without using Pause or `Time.timeScale`.

Reference: `F37-ADR-TRANSITION-001-Transition-Gate-Policy.md`

## F38 - Unity PlayerInput Gate Adapter

Scope: Unity Input / Gate / First Game gameplay input.

Status: Accepted.

F38 adds `UnityPlayerInputGateAdapter`, an opt-in Unity Input System component that disables a configured gameplay action map while the framework Gate blocks input acceptance or gameplay action. It bridges Gate to temporary/consumer gameplay input without making movement scripts framework-aware and without introducing Player/Actor lifecycle.

Reference: `F38-ADR-INPUT-001-Unity-PlayerInput-Gate-Adapter.md`
