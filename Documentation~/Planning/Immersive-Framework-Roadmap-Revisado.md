# Immersive Framework Roadmap

This is the canonical roadmap for `com.immersive.framework`.

The roadmap is organized around three implementation tracks:

1. Framework Core / Contracts
2. Unity Build Surface / Lifecycle Wiring
3. Adapter Modules / Gameplay & Subsystems

## 1. Rule of Reading

Read the plan from stable core contracts to Unity build surfaces, then to adapter modules.

Framework contracts define lifecycle language, identity, state, diagnostics and ownership boundaries. Unity build surfaces prove those contracts through minimal real Unity wiring. Adapter modules consume the proven contracts and surfaces without changing framework ownership.

Do not read adapter module work backward into earlier phases. F23 does not build visuals. F24 does not build gameplay modules. F25-F26 stabilize Activity scene operation/loading progress. F27 validates Pause surface/input evidence. F28 reorganizes the roadmap before module implementation resumes.

## 2. Closed Baseline

| Phase | Name | Status | Result |
|---|---|---|---|
| F00-F10 | Baseline, identities, route/activity, anchors, runtime materialization, input, pause and snapshot foundations | Closed | Core ownership language established. |
| F11-F16 | Reset, object entry, Unity reset adapters, player participant entry | Closed | Reset and participant foundations established without gameplay adapter ownership. |
| F17 | Gate Boundary / Advanced Consumers Boundary | Closed | Gate and advanced consumer boundaries established. |
| F18 | Transition Orchestration | Closed | Transition language and orchestration contracts established. |
| F19 | Transition Effects Boundary | Closed | Transition effect adapter boundary established. |
| F20 | Pause State and Gate | Closed | Logical Pause state and Gate relationship established. |
| F21 | Save / Snapshot / Preferences / Progression Save Foundation | Closed | Save tracks split into Snapshot, Preferences and Progression Save boundaries. |
| F22 | Loading Operation / Progress / Readiness Boundary | Closed | Loading operation/progress/readiness language established without visual loading implementation. |
| F23 | Pause Content / Overlay / Input Intent Boundary | Closed | Pause content, presentation and input intent contracts established without Unity build surface. |
| F24 | Unity Build Surface / Lifecycle Wiring | Closed | Transition, loading and Pause-facing Unity surfaces validated as framework-owned build evidence. |
| F25 | Activity Content Scene Composition / Operation Reset | Closed | Activity content, operation planning, scene loading/release and ledger ownership stabilized. |
| F26 | Activity Discovery and Loading Progress | Closed | Activity discovery and loading progress closed through F26F. |
| F27 | Pause UIGlobal Surface and Input Wiring | Frozen after F27D | Pause UIGlobal surface and narrow PauseToggle input path validated; F27E cancelled. |

## 3. Current Phase

F28-F33 are closed. F28-F33 are official only as controlled anticipation of the Input / Pause / Unity `PlayerInput` axis after the F27D freeze.

F33 does not select or authorize the next feature or implementation phase. Any prior F34 or gameplay next-phase wording is superseded by `F33E1` and `POST-F33-A`.

`POST-F33-B — Officialize/Reclassify F28-F33` officially reclassifies F28-F33 as documentation-only governance. After that reclassification, the first technical candidate remains `F8R-A — RuntimeContent / ContentAnchor Materialization Audit`, unless the user explicitly selects another axis.

F27E is cancelled because ordinary input consumers should not each query Gate as the primary Pause/Input implementation. That cancellation exposes a larger missing chain:

```text
adapter/module ownership
  -> Player/Actor ownership
  -> Unity Input target ownership
  -> typed InputMode
  -> Pause requests InputMode
  -> later Camera/Audio/Save/RuntimeSpawned/Gameplay adapters
```

## 4. Phase Map From the Freeze Forward

### F28 — Roadmap Reconciliation and Adapter Module Spine

F28 is documentation-first. It creates the dependency and ownership map for the next implementation phases.

| Cut | Name | Status | Output |
|---|---|---|---|
| F28A | Frozen Baseline Reconciliation | Closed / docs-only | Authoritative reading of package docs, project docs, QA assets and the cancelled F27E path. |
| F28B | Completion Dependency Map | Closed / docs-only | Ordered graph for remaining product-completion tracks. |
| F28C | Adapter Module Taxonomy | Closed / docs-only | Module families, owner kind, placement rule and dependency category. |
| F28D | Player / Actor / Input Ownership Plan | Closed / docs-only | Player object ownership, `PlayerInput` target ownership and first input target proof. |
| F28E | InputMode and Pause Integration Plan | Closed / docs-only | Typed InputMode semantics and Pause-driven mode requests after ownership is clear. |
| F28F | Next Implementation Closeout | Closed / docs-only | Selects F29 — Unity Input Target Ownership Proof as the next code phase. |

F28 output is a clean plan, not runtime contracts.

F28A closure confirms the freeze baseline and source boundary:

```text
F24-F27 = frozen Unity-facing evidence
F27E = cancelled
Assets/ = project-facing operational source
Packages/com.immersive.framework/ = framework source
```

F28B closes the dependency graph. F28C closes the adapter module taxonomy. F28D closes Player/Actor/Input ownership. F28E closes InputMode/Pause semantics. F28F closes F28 by selecting F29 — Unity Input Target Ownership Proof as the next implementation phase.

F28B dependency order:

```text
adapter module taxonomy
  -> Player / Actor ownership
  -> Unity Input target ownership
  -> InputMode semantics
  -> Pause requests InputMode
  -> optional Camera / Audio / Save / RuntimeSpawned lanes
  -> gameplay modules
```

F28B reference note:

```text
Assets/_Documentation/Notes/F28B-Completion-Dependency-Map.md
```

F28C accepted taxonomy:

```text
module family
  -> owner kind
  -> dependency category
  -> placement rule
  -> evidence surface
  -> first proof
```

F28C separates official Unity packages, optional Immersive packages, external tools, project assets/config, personal assets and QA fixtures. F28D closes Player / Actor / Unity Input ownership. F28E closes typed InputMode and Pause integration semantics.

F28C reference note:

```text
Assets/_Documentation/Notes/F28C-Adapter-Module-Taxonomy.md
```

F28D accepted ownership split:

```text
Project assets own concrete player prefabs and InputActionAssets.
Unity Input adapter owns translation from Unity Input System targets to framework language.
Framework Core owns typed InputMode language.
Pause requests InputMode later; it does not own PlayerInput.
Runtime-spawned player/actor lifetime is deferred until runtime roots, handles and release policy exist.
```

F28D reference note:

```text
Assets/_Documentation/Notes/F28D-Player-Actor-Input-Ownership-Plan.md
```

F28E accepted InputMode/Pause semantics:

```text
Gameplay = gameplay command posture
PauseOverlay = pause UI posture over gameplay
FrontendMenu = reserved non-gameplay menu posture
InputLocked = reserved transition/loading/exceptional hard suppression posture

Pause may request Gameplay/PauseOverlay after InputMode exists.
Pause does not own PlayerInput, action-map names, player lifecycle, movement, Time.timeScale or gameplay adapters.
UI/Pause input remains available during PauseOverlay; gameplay commands stop driving gameplay.
```

F28E reference note:

```text
Assets/_Documentation/Notes/F28E-InputMode-Pause-Integration-Plan.md
```

### F29 — Unity Input Target Ownership Proof

F29 is the first implementation phase after F28. F28F selects it as the next code phase.

Expected purpose:

```text
prove explicit Unity Input target ownership before InputMode behavior or Pause-driven action-map changes are implemented
```

F29 is closed. F29A added passive target role/id/descriptor/set/issue vocabulary, a Unity-facing declaration component, a validator and `Unity Input Target Ownership Smoke` for valid, missing and duplicate target configurations. F29B closed QA fixture evidence by adding canonical StartupScene declarations and a loaded-scene smoke step. F29C closed the phase after smoke evidence passed and selected F30 — InputMode Identity and Request Result Model.

F29 does not implement full InputMode, action-map switching, PlayerInput ownership, player movement, player/actor spawning, camera, audio, save, runtime-spawned gameplay or per-consumer Gate checks.

Reference plan:

```text
Assets/_Documentation/Plans/F29-PLAN-Unity-Input-Target-Ownership-Proof.md
```

### F30+ — Product-Facing Module Tracks

F30 is closed through F30E. Logical Pause state/result can map to `PauseOverlay` or `Gameplay` `InputModeRequest` values, but F30 still does not own Unity `PlayerInput`, does not own `PlayerInputManager`, does not switch action maps and does not dispatch runtime Pause behavior. F31 is closed through PlayerActor and Session PlayerInputManager reference evidence.


The later tracks should be opened incrementally instead of as a single large subsystem pass.

| Track | First dependency | Expected direction |
|---|---|---|
| Player / Actor | Adapter module foundation | Decide player object lifetime, player participant binding and actor adapter placement. |
| Unity Input | Player/Input target ownership | Adapt typed input language to concrete Unity Input System targets. |
| InputMode / Pause Integration | Unity Input target path | Let Pause request `PauseOverlay` / `Gameplay` modes without consumer-side Gate scattering. |
| Camera | Content Anchor binding + player/actor targets where needed | Build camera adapter as a consumer, not framework core authority. |
| Audio | Route/Activity/Pause lifecycle evidence | Build audio as lifecycle consumer with project policy outside core. |
| Save / Progression | Snapshot/progression ports | Add replaceable initial backend adapter without making backend the contract. |
| Runtime Spawned / Pooling | Runtime handle/root/release policy | Materialize prefab/pool instances through framework runtime ownership. |
| Gameplay Capabilities | Player/Actor/RuntimeSpawned | Add inventory/combat/projectile/damage as optional modules. |

## 5. Canonical Tracks

| Track | Owns | Examples | Exclusions |
|---|---|---|---|
| Framework Core / Contracts | Pure framework contracts, identifiers, states, diagnostics, policies and request/result boundaries. | Route, Activity, Gate, Transition, Loading, Pause, Snapshot, Preferences, Progression Save. | Concrete UI, prefabs, scenes, gameplay modules, backend-specific canonical contracts. |
| Unity Build Surface / Lifecycle Wiring | Minimal Unity surfaces and lifecycle wiring that prove framework contracts. | Transition integration, curtain, loading screen, pause overlay, QA scene evidence. | Product gameplay, broad subsystem adapters, full UI systems. |
| Adapter Modules / Gameplay & Subsystems | Optional modules that adapt product/gameplay systems to framework contracts. | Player, actor, camera, audio, input, save backends, inventory, combat, projectile, damage, spawned objects. | Core contract ownership, route/activity lifecycle, hidden global pipelines. |
| Project Assets | Product-specific prefabs, scenes, configuration, UI art and game design content. | Player prefab, product pause menu, concrete camera rig, concrete audio bank. | Canonical framework ownership or reusable framework contracts. |
| External Packages | Unity official modules, optional packages, third-party tools and premium assets. | Unity Input System, Cinemachine, Addressables, future premium save backend. | Hard-coded local paths or mandatory Asset Store imports in canonical setup. |

## 6. Anti-Regression Rules

- Snapshot does not know a backend.
- Preferences do not use progression slots.
- Progression Save uses a replaceable backend port.
- JSON persistence is a future initial adapter, not the canonical contract.
- A future premium backend must swap behind the same Progression Save port.
- Loading is not fade, curtain, loading screen prefab or `SceneLifecycle` replacement.
- Loading visual remains a Unity build surface or adapter, not the Loading contract.
- Pause F23 remains intent/requirement language.
- F24-F27 surfaces are evidence, not gameplay modules.
- F25 is Activity scene operation history, not the adapter module foundation phase.
- Adapter modules do not own Route/Activity lifecycle.
- Adapter modules do not redefine Gate, Transition, Loading, Pause, Save, Snapshot, Preferences or Progression Save.
- No framework identity may be fabricated from Unity names or paths.
- No module should create service locator behavior to avoid an ownership decision.

## 7. F27 Pause UIGlobal / Input / Gate Reframe

`F27A` validates the Pause UIGlobal surface. `F27B` validates the narrow Unity Input System `PauseToggle` adapter. `F27C` audits the Gate/Input boundary and accepts that Gate is capability/admission language, not a component blocker. `F27D` applies the diagnostic/runtime vocabulary correction by using `Input/InputAcceptance` and `Interaction/InteractionAcceptance` blockers instead of broad gameplay/component blockers.

F27 is frozen after F27D. `F27E - Input Consumers Respect Gate` is cancelled and must not be applied.

Corrected F27 rule:

```text
Pause produces state and may produce blockers.
Gate remains passive admission / hard-lock / diagnostics.
InputMode must be planned before action-map behavior.
Gameplay components are not paused directly by Gate.
Ordinary input consumers should not each query Gate just to implement Pause.
TimeScale freeze policy is separate and deferred.
```

See:

```text
ADRs/F27-ADR-GATE-INPUT-001-Capability-Gate-Boundary.md
ADRs/F28-ADR-INPUT-001-InputMode-Adapter-Boundary.md
Assets/_Documentation/Notes/F27C-Gate-Input-Capability-Audit.md
Assets/_Documentation/Notes/F27D-Pause-Capability-Gate-Reframe.md
Assets/_Documentation/Notes/F27E-CANCELLED-Input-Consumers-Gate-Replan.md
Assets/_Documentation/Plans/F28-PLAN-InputMode-And-Adapter-Boundary.md
```

## 8. F28 Roadmap Correction

F28 converts the freeze into a progressive plan.

The phase must answer:

```text
what remains to complete the framework/product bridge;
which tracks are core, Unity surface, adapter module, project asset or external package;
which track must be implemented first;
which ownership decision blocks InputMode;
which ownership decision blocks Camera, Audio, Save, RuntimeSpawned and Gameplay modules;
F29 as the first post-F28 code phase, proving Unity Input target ownership. F29A closes the declaration and diagnostics proof.
```

F28, F29, F30 and F31 are complete. F29 provides target declaration vocabulary, validator, authored QA scene evidence and Unity Input Target Ownership Smoke. F30 adds passive InputMode identity/state/request/result language, Pause-to-InputMode request mapping and Unity Input boundary correction. F31 adds canonical PlayerActor and Session PlayerInputManager evidence. No phase here introduces action-map behavior or a framework-owned input manager.

## F29 — Unity Input Target Ownership Proof

- F29A — Unity Input Target Declaration Proof: PASS by user smoke.
- F29B — Input Target QA Authoring Fixture: PASS by user smoke; canonical QA StartupScene declarations plus loaded-scene smoke validation.
- F29C — Input Target Closeout: closed; selects F30 — InputMode Identity and Request Result Model.

## F30 — InputMode Identity and Request Result Model

- F30A — InputMode Identity / State / Request Result Contracts: closed; adds passive mode/state/request/result contracts and `InputMode Contract Smoke`.
- F30B — Unity PlayerInput Integration Boundary: closed corrective docs; rejects a framework-owned input manager.
- F30C — Unity PlayerInput Component Evidence Validation: PASS by user smoke, with native Unity warning observed in duplicate manager QA step.
- F30C1 — PlayerInputManager Smoke Warning Cleanup: closed corrective patch; duplicate manager evidence now uses passive counts instead of real duplicate components.
- F30D — Pause InputMode Request Boundary: closed; passive Pause state/result -> InputMode request mapping.
- F30E — InputMode / Unity Input Boundary Closeout: closed; F30 is complete.


## F31 — PlayerActor Identity and Unity Input Evidence

- F31A — PlayerActor Identity and PlayerInput Evidence: closed; introduces minimal actor identity and `PlayerActor : IActor` evidence. A PlayerActor requires Unity `PlayerInput` evidence.
- F31B — Session PlayerInputManager Boundary: closed; `PlayerInputManager` is Session-scoped Unity integration evidence, not Activity-owned content.
- F31B1 — Session PlayerInputManager Smoke Warning Fix: closed; removes CS1718 redundant comparison.
- F31C — PlayerActor / Session Unity Input Reference Closeout: closed; F31 is complete.

F31 does not create a custom input manager, movement, actor spawning, action-map switching, join behavior or player prefab spawn.


## F30/F31 closure result

F30 and F31 close the input reference baseline.

Accepted stack for the next input phase:

```text
F29 UnityInputTargetDeclaration
F30 InputModeRequest / PauseInputModeRequestMapper
F31 PlayerActor : IActor + PlayerInput evidence
F31 SessionPlayerInputManagerDeclaration + PlayerInputManager evidence
```

The next phase may plan Unity `PlayerInput` action-map application, but it must be an explicit adapter and must not revive a framework-owned input manager.


## F32 — InputMode Unity Adapter Application

Status: Closed through F32H.

F32 is the real continuation after F30E/F31C. `F31D — PlayerInput Reference Set` is cancelled and must not be applied or counted.

Closed cuts:

- F32A — InputMode Unity Application Preview: side-effect-free evidence preview.
- F32B — InputMode Unity Action Map Preview: passive action-map evidence.
- F32C — InputMode Unity Application Plan: dry-run operation plan.
- F32D — InputMode Unity PlayerInput Adapter: first explicit `PlayerInput` side effect.
- F32E — InputMode Unity PlayerInput Application: activation + action-map selection / lock behavior.
- F32F — InputMode Unity PlayerInput Request Application: composed request-to-`PlayerInput` path.
- F32G — Pause InputMode Unity PlayerInput Application: completed `PauseResult` to explicit `PlayerInput` application.
- F32H — closeout: F32 closed; runtime wiring deferred.

Final F32 behavior:

```text
Gameplay -> ActivateInput + SwitchCurrentActionMap(Player)
PauseOverlay -> ActivateInput + SwitchCurrentActionMap(UI)
FrontendMenu -> ActivateInput + SwitchCurrentActionMap(UI)
InputLocked -> DeactivateInput
```

Guardrails:

- no framework input manager;
- no `PlayerInputManager.JoinPlayer`;
- no player prefab spawn;
- no PlayerActor movement;
- no gameplay command reading;
- no automatic `PauseRuntime` wiring;
- no automatic `FrameworkRuntimeHost` wiring.

Selected follow-up phase after F32H:

```text
F33 — Pause Runtime PlayerInput Wiring
```

F33 defines runtime ownership and opt-in authoring before connecting the F32 application path to live Pause runtime events.

### F33 — Pause Runtime PlayerInput Wiring

F33 follows F32H and is closed through F33E. It introduces opt-in runtime wiring from logical Pause requests to explicit Unity `PlayerInput` application.

F33A adds `PauseInputModeUnityPlayerInputRuntimeBridge` and `Pause Runtime PlayerInput Bridge Smoke`.

F33B adds `PauseInputActionRuntimeBridgeTrigger` and `Pause InputAction Runtime Bridge Trigger Smoke`.

F33C retires the legacy direct Pause InputAction adapter.

F33D flattens Pause input diagnostics so the accepted F33 path remains readable in QA logs.

F33E closes the phase. The canonical authored path is `PauseInputActionRuntimeBridgeTrigger` -> `PauseInputModeUnityPlayerInputRuntimeBridge` -> `PauseResult` -> `InputMode` -> Unity `PlayerInput`.

It remains outside automatic `FrameworkRuntimeHost` registration and does not call `PlayerInputManager.JoinPlayer`, spawn player prefabs, move actors, read gameplay commands or create a framework-owned input manager.

Next phase after F33 is not selected by this closeout. A later roadmap/plan decision must select the next implementation phase before new work starts.

### POST-F33-A — Matrix Reconciliation Closeout

Status: Accepted / documentation governance.

POST-F33-A closes the matrix reconciliation after F33. It confirms that F33 is closed but does not select F34, gameplay or any other feature phase.

F28-F33 remain official only as controlled anticipation of the Input / Pause / Unity `PlayerInput` axis. They do not close the RuntimeContent, ContentAnchor, materialization, runtime root, runtime handle or release-policy blockers identified by the matrix.

The authorized next steps are:

| Step | Type | Scope |
|---|---|---|
| POST-F33-B — Officialize/Reclassify F28-F33 | Docs-only | Reclassify F28-F33 against the matrix before new implementation. |
| F8R-A — RuntimeContent / ContentAnchor Materialization Audit | Audit-only | Re-evaluate F8/F9 materialization and binding blockers before consumers. |

References:

```text
Assets/_Documentation/Notes/POST-F33-A-Matrix-Reconciliation-Closeout.md
Assets/_Documentation/Notes/POST-F33-B-Officialize-Reclassify-F28-F33.md
Assets/_Documentation/Plans/POST-F33-PLAN-Matrix-Reconciliation.md
```

### POST-F33-B — Officialize/Reclassify F28-F33

Status: Accepted / documentation governance.

POST-F33-B officially reclassifies F28-F33 against the matrix without reopening those phases for code and without selecting a new implementation phase.

| Phase | Official status | Classification |
|---|---|---|
| F28 | Official | Official planning/governance |
| F29 | Official | Official Unity Input target evidence |
| F30 | Official | Official passive InputMode / Pause request language |
| F31 | Official | Official PlayerActor identity and Session PlayerInputManager evidence |
| F32 | Official closed | Controlled anticipation — explicit PlayerInput application lane |
| F33 | Official closed | Controlled anticipation — opt-in Pause runtime to PlayerInput wiring |

F34/gameplay remains unauthorized. Camera, audio, save/progression, pooling/runtime-spawned and actor materialization remain unselected.

`F8R-A — RuntimeContent / ContentAnchor Materialization Audit` is the first technical candidate after this reclassification. It remains a candidate, not a selected implementation phase.
