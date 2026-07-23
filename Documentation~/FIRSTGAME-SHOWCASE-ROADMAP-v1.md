# FIRSTGAME Showcase — Roadmap v1

Status: Proposed  
Date: 2026-07-23  
Target: `ImmersiveGames/planet-devourer` as the real-game consumer and public showcase of `com.immersive.framework`

## 1. Decision summary

FIRSTGAME should become a small, polished game that demonstrates the framework through normal play. It must not look like a QA scene, a feature gallery, or a collection of debug buttons.

The first release target is one complete vertical slice:

```text
Boot
-> Title
-> Start
-> explicit single-player admission
-> short playable objective
-> completion
-> replay or return to title
```

The recommended working concept is:

## Planet Devourer — Core Harvest

The player controls a small planetary devourer in a compact arena. The immediate goal is to absorb three energy fragments and feed them into a central core. Completing the core finishes the run and returns the player to a clear result/replay choice.

Target session:

```text
2 to 4 minutes
one small arena
one controllable Actor
three objective items
one completion target
no combat required
```

This concept is intentionally mechanically small. It gives the framework real responsibilities without requiring enemy AI, inventory, progression, procedural generation, multiplayer, or save data in the first release.

## 2. Why this is the correct minimum

The vertical slice can naturally exercise:

| Framework surface | Real use in the demo |
|---|---|
| Game Application | Declares the application and startup flow |
| Route / Activity | Separates title and playable context |
| Player participation | Admits one explicit local Player |
| Actor selection/composition | Assigns the one playable Actor explicitly |
| Input | Drives game-specific movement and actions |
| Camera | Follows the admitted Player through the official request/output path |
| Global UI | Hosts persistent output, transition, loading and pause surfaces |
| Pause / InputMode / Gate | Pauses and restores the real playable loop |
| Reset / Activity restart | Restores the run without an ad-hoc scene reload |
| Diagnostics | Explains failures and runtime state outside the normal player experience |

Audio BGM is useful polish, but its framework adapter is still marked Experimental. It must not block the first playable release.

## 3. Source-of-truth rule

Implementation must follow the current package source and current canonical guides in Git.

The supplied `ADR-PROD-*` documents remain valuable for principles such as:

```text
designer-first product surfaces
Profiles versus Recipes versus runtime state
explicit scoped runtime authority
diagnostics are not product UX
FIRSTGAME proves usability
```

Where those documents differ from the current package, the current Git documentation wins.

Known current differences:

```text
Activity participation Projection, zero-participant behavior and Requirement Level
are currently authored directly on ActivityAsset.

Actor duplicate-selection policy is currently configured on GameApplicationAsset.

The current public Player product surfaces are
LocalPlayerProvisioningAuthoring,
LocalPlayerProvisioningHostRegistration,
LocalPlayerHostAuthoring,
SceneLocalPlayerAdmissionAuthoring,
PlayerGameplayCameraAuthoring
and related typed bindings.
```

The roadmap must not reintroduce an older product shape merely because an older ADR still describes it.

## 4. Frozen boundaries

### Package

`com.immersive.framework` owns reusable product surfaces, contracts, runtime behavior, diagnostics and current usage documentation.

### QAFramework

QAFramework proves framework contracts and regressions after an official package change exists.

### FIRSTGAME

FIRSTGAME owns game rules, movement, objectives, visual composition and the real usability proof.

FIRSTGAME may contain thin game-specific commands and adapters. It must not contain:

```text
framework compatibility facades
local replacements for official runtime contexts
copied QA fixtures
automatic repair/setup tools used to hide product friction
new implicit global managers
```

If a normal recurring workflow needs such a workaround, execution pauses and the reusable issue is evaluated in the package first.

## 5. First release definition

The first public milestone is:

```text
FIRSTGAME Showcase 0.1 — Playable Minimum
```

It is complete only when a player can:

1. launch the game;
2. understand the single objective without reading diagnostics;
3. start the run;
4. control the Player with the intended device;
5. see a stable gameplay camera;
6. collect the required fragments;
7. complete the core;
8. pause and resume safely;
9. restart the run;
10. finish and replay or return to title.

The Unity Console and QA menus are not part of this player journey.

## 6. Roadmap overview

| Cut | Outcome | Primary proof |
|---|---|---|
| FG-0 | Consumer baseline and UX protocol | We know exactly what exists and what will be reused |
| FG-1 | Application shell | Boot, Title and gameplay Route/Activity topology works |
| FG-2 | Canonical local Player | One Player is explicitly admitted with correct Slot and Actor evidence |
| FG-3 | Movement and gameplay Camera | The admitted Player is controllable and framed through official surfaces |
| FG-4 | Core Harvest loop | The project is a real, completable game |
| FG-5 | Pause, restart, transition and loading | Framework lifecycle features operate the real loop |
| FG-6 | Showcase 0.1 release candidate | The slice is understandable, polished and reproducible from documentation |

Cuts are sequential. A cut closes only with user-run Unity evidence and a short UX record.

---

## 7. FG-0 — Consumer inventory and UX baseline

Type: integration audit / product UX  
Goal: establish the exact starting point without modifying framework contracts.

### Scope

- Update the package in FIRSTGAME to the intended reviewed revision.
- Confirm Unity version, package dependencies and current compilation state.
- Inventory existing scenes, prefabs, input actions, movement code, objective content, UI, audio and reusable visual assets.
- Identify legacy or backup content that may supply game content but must not supply architecture.
- Choose the concrete assets that will become `UIGlobal`, Title, Gameplay and the local Player Host.
- Record the current manual baseline before authoring P3 integration.

### Out of scope

- Adding framework components.
- Repairing product friction.
- Designing advanced gameplay.
- Copying assets from QA.

### Product surface affected

None yet. This cut establishes the consumer baseline against which usability will be measured.

### Expected files

- This roadmap.
- One short FIRSTGAME integration journal.
- No package or QA changes.
- No gameplay asset mutation unless required only to recover a compilable baseline.

Exact existing asset paths must be recorded after inspecting the Unity project; the roadmap does not guess them.

### Manual flow

```text
Open project
-> compile
-> inspect current scenes and assets
-> run existing preserved content
-> classify reusable game content versus obsolete integration
```

### Acceptance

Technical:

- FIRSTGAME imports and compiles against the intended package revision.
- No obsolete setup/repair tool is treated as required.
- The selected scenes and Player content are identified.

Product:

- We can explain what the game will reuse and what will be authored anew.
- The selected minimum can be completed without first building advanced systems.

### Gains

Architectural: prevents preserved gameplay content from silently restoring superseded integration.  
Usability: creates a measurable “before” state for the manual framework workflow.

Suggested commit:

```text
docs(firstgame): define showcase baseline and integration journal
```

---

## 8. FG-1 — Application shell

Type: integration real / product UX  
Goal: produce the smallest understandable application flow before adding the Player.

### Scope

- Create or confirm one explicit `GameApplicationAsset`.
- Configure the startup Route.
- Author a Title context with no required Player participation.
- Author a Gameplay Route and Activity with explicit participation fields.
- Establish the persistent `UIGlobal` surfaces required by the selected modules.
- Add one visible Start command that requests the intended transition through the framework.
- Add a simple return-to-title path for early lifecycle proof.

### Out of scope

- Player admission.
- Movement and camera follow.
- Objectives.
- Pause, reset and audio polish.

### Product surface affected

```text
GameApplicationAsset
RouteAsset
ActivityAsset
UIGlobal
bootstrap and lifecycle diagnostics
```

### Expected files

- FIRSTGAME application, Route and Activity assets.
- Title and Gameplay scene updates.
- A thin game-specific Start/Return command if UI needs one.
- No package or QA changes unless a reusable product blocker is confirmed.

### Player flow

```text
Launch
-> Title visible
-> Start
-> covered transition
-> empty Gameplay context
-> Return
-> Title restored
```

### Focused smoke

- Startup Route resolves.
- Title Activity enters.
- Gameplay Route/Activity enters and exits.
- Re-entry does not retain the previous scope.

### Acceptance

Technical:

- Required references are explicit.
- Missing configuration blocks with typed diagnostics.
- No scene-name search, singleton or fallback is added.

Product:

- A developer can reconstruct the shell using the current Framework Usage guide.
- The player sees a normal title/game transition, not a QA control panel.

### Gains

Architectural: validates lifecycle ownership before Player complexity is introduced.  
Usability: isolates whether Game Application, Route, Activity and `UIGlobal` authoring are understandable.

Suggested commit:

```text
feat(firstgame): build framework application shell
```

---

## 9. FG-2 — Canonical single local Player

Type: integration real / Player product UX  
Goal: manually assemble one official local Player path and record every point of friction.

### Decision

Use the runtime-provisioned path for the showcase minimum:

```text
manual-join PlayerInputManager
-> LocalPlayerProvisioningAuthoring
-> LocalPlayerProvisioningHostRegistration
-> LocalPlayerHostAuthoring
-> explicit Slot reservation and admission
```

Reason: it uses the framework's canonical single-player/multiplayer-compatible path and tests the product surface that most strongly needs real-game UX proof.

`SceneLocalPlayerAdmissionAuthoring` remains a valid later comparison case, not a second simultaneous Player authority.

### Scope

- Create one `PlayerSlotProfile`.
- Create or identify one explicit `ActorProfile`.
- Add the Slot to `GameApplicationAsset` in allocation order.
- Configure the explicit Actor selection/default policy on the Game Application.
- Configure one manual-join `PlayerInputManager` in `UIGlobal`.
- Prepare the local Player prefab with `PlayerInput`, `LocalPlayerHostAuthoring` and an empty Actor Mount.
- Issue one authorized join through the official typed path.
- Configure the Gameplay Activity participation Projection, zero-participant behavior and Requirement Level.
- Prove admission, selection, preparation, occupancy and release as distinct facts.

### Out of scope

- A character-selection screen.
- A second local Player.
- Split screen.
- Disconnect/reconnect.
- Online play.
- Replacing the selected Actor after preparation.

### Product surface affected

Player Slot, Actor selection, local provisioning, Activity participation and diagnostics.

### Expected files

- FIRSTGAME Slot and Actor Profile assets.
- Local Player Host prefab updates.
- `UIGlobal` provisioning authoring.
- Gameplay Activity authoring.
- At most one thin game-specific “start single-player” command.

### Player flow

```text
Start selected
-> authorized local join
-> first configured Slot reserved
-> PlayerInputManager provisions host
-> framework admits Player
-> default Actor selected/prepared
-> Gameplay Activity becomes eligible
```

### Focused smoke

- Correct Slot is reserved.
- Failed provisioning releases the reservation.
- `playerIndex` is not used as `PlayerSlotId`.
- Activity admission blocks when required evidence is missing.
- Exit releases admission cleanly.

### Acceptance

Technical:

- Manual join is the only local Player provisioning path.
- The join has a correlated pending operation.
- No local Player is silently adopted.
- No second framework Player spawner exists.

Product:

- A developer can identify which asset answers Slot, Actor and concrete Player Host.
- The default Inspector does not require understanding internal runtime IDs.
- Advanced/Debug explains the admission result.

### Gains

Architectural: proves the current canonical Player authority in a consumer.  
Usability: exposes whether Player setup is genuinely product-ready rather than merely QA-ready.

Suggested commit:

```text
feat(firstgame): integrate canonical local player admission
```

---

## 10. FG-3 — Movement and gameplay Camera

Type: integration real / gameplay foundation  
Goal: make the admitted Actor pleasant to control and view.

### Scope

- Keep movement as FIRSTGAME gameplay code.
- Bind movement to the admitted Player's `PlayerInput`.
- Apply `PausePlayerInputBinding` on the relevant Player object, ready for FG-5.
- Create a reusable `CameraRigRecipe`.
- Add and configure `CameraRigComposer`.
- Validate and run Apply/Rebuild twice.
- Configure the persistent single output in `UIGlobal`:

```text
Unity Camera
CinemachineBrain
CameraOutputSessionBinding
SessionCameraOverrideBinding
```

- Add `PlayerGameplayCameraAuthoring` to the admitted Player Actor.
- Prove Player camera publication and release without a duplicate publisher.

### Out of scope

- Activity and Route camera overrides.
- Cutscene camera.
- Multiple outputs.
- Split screen.
- Framework-owned movement.

### Product surface affected

Input binding, Camera Recipe/Composer, Player camera eligibility and output-scoped runtime authority.

### Player flow

```text
Gameplay admission
-> input becomes eligible
-> Player request becomes eligible
-> CameraOutputContext selects Player request
-> movement and follow operate together
```

### Focused smoke

- Apply/Rebuild is idempotent.
- Exactly one physical output exists.
- Exactly one Player publisher exists.
- Missing targets fail explicitly.
- Exit releases camera and input eligibility.

### Acceptance

Technical:

- No `Camera.main`, scene search, name lookup or direct Camera toggling.
- Cinemachine performs presentation; framework owns request selection.
- Movement code does not become framework code.

Product:

- Movement and framing feel intentional.
- A developer can author the rig without manually manipulating internal request priority.
- Advanced/Debug identifies output, request, targets and winner.

### Gains

Architectural: preserves the boundary between game movement and framework participation/camera authority.  
Usability: validates the most visible Composer and Apply/Rebuild workflow in a real scene.

Suggested commit:

```text
feat(firstgame): add player control and gameplay camera
```

---

## 11. FG-4 — Core Harvest playable loop

Type: gameplay / integration real  
Goal: turn the framework integration into a complete game.

### Scope

- Create a compact arena.
- Place three explicit energy fragments.
- Add collection feedback and an objective counter.
- Add a central core that accepts collected fragments.
- Complete the run when all fragments are delivered.
- Provide a result and Replay/Return choice through normal game UI.
- Keep objective state in FIRSTGAME runtime code.
- Register only the relevant gameplay state with framework Reset where restart requires it.

### Out of scope

- Combat.
- Enemy AI.
- Inventory system.
- Procedural level generation.
- Save/progression.
- Multiple playable Actors.

### Product surface affected

The framework is used by a real gameplay loop, but does not own the loop's rules.

### Player flow

```text
Enter arena
-> locate fragment
-> absorb fragment
-> deliver to core
-> repeat three times
-> core completes
-> result
-> replay or title
```

### Focused smoke

- Each fragment counts once.
- Completion occurs once.
- Re-entering the Activity starts from a valid state.
- No objective state leaks across an unintended scope.

### Acceptance

Technical:

- FIRSTGAME owns collection, delivery and completion rules.
- Framework APIs are used only for lifecycle, participation, camera, reset and transitions.
- Runtime state is not written into Profile/Recipe assets.

Product:

- A first-time player understands the objective.
- A run can be completed without debug controls.
- The loop is short enough for repeated framework demonstrations.

### Gains

Architectural: proves the framework can support a game without absorbing game-specific rules.  
Usability: changes the proof from “configured correctly” to “usable to build something complete.”

Suggested commit:

```text
feat(firstgame): implement core harvest gameplay loop
```

---

## 12. FG-5 — Lifecycle product minimum

Type: framework integration / UX validation  
Goal: prove the framework features that make the slice feel production-ready.

### Scope

- Add an interactive Pause UI.
- Bind pause to the officially admitted local Player.
- Prove pause/resume and Activity exit while paused.
- Configure Object Reset for fragments, core and run-local state where appropriate.
- Configure `ActivityRestartTrigger` for Replay/Restart.
- Use the canonical transition/loading cover for Route/Activity changes.
- Verify return to Title and repeated Start.
- Add BGM only if the optional Audio adapter can be integrated without blocking the release.

### Out of scope

- Multiplayer pause policy.
- Save slots.
- Checkpoints.
- A generic menu framework.
- Custom transition engine.

### Product surface affected

Pause/InputMode/Gate, Reset, Activity restart, loading/transition and optionally Audio BGM.

### Player flow

```text
Play
-> pause
-> resume
-> restart
-> Activity resets and re-enters
-> complete
-> transition to result/title
-> play again
```

### Focused smoke

- `Time.timeScale` returns to the correct state after resume, restart and exit.
- Gameplay input is blocked while paused and restored once.
- Reset participants execute in declared order.
- Activity restart uses canonical lifecycle ordering.
- Transition cover does not expose invalid intermediate camera/content state.
- Repeated play does not duplicate persistent `UIGlobal` authorities.

### Acceptance

Technical:

- Pause has one binding and one physical state writer.
- Restart is not implemented as an ad-hoc scene reload.
- Required Reset failures block explicitly.
- Optional BGM remains isolated from Framework Core.

Product:

- Pause, restart and replay are accessible through normal UI.
- No diagnostic or smoke menu is needed to recover the game.
- Failure messages identify the missing owner or binding.

### Gains

Architectural: validates lifecycle composition across several framework domains.  
Usability: turns the playable prototype into a credible minimum product.

Suggested commit:

```text
feat(firstgame): complete pause reset and transition lifecycle
```

---

## 13. FG-6 — Showcase 0.1 release candidate

Type: product polish / documentation / release validation  
Goal: make the minimum demonstrable to someone who did not build it.

### Scope

- Improve tutorial prompt, objective feedback and result feedback.
- Normalize input prompts for the supported device set.
- Perform visual and audio polish that does not introduce new framework scope.
- Remove temporary debug UI from the player-facing build.
- Keep Advanced/Debug evidence available to developers.
- Write a short “How this showcase uses the framework” guide.
- Re-run the complete manual journey several times.
- Record actual setup friction and package follow-ups.

### Out of scope

- Adding features because they are available in the framework.
- Large content expansion.
- Architecture refactoring without a confirmed blocker.

### Expected files

- FIRSTGAME player-facing polish.
- A short showcase usage/architecture guide.
- A final UX findings list classified by owner.
- Package/QA changes only through separate approved cuts.

### Acceptance

Technical:

- Clean import and compile.
- No framework error diagnostics on the happy path.
- Repeated Title → Play → Pause → Restart → Complete → Replay/Title is stable.
- No leaked Route, Activity, Player, Camera, Pause or Reset scope.

Product:

- The game is understandable without framework knowledge.
- The framework contribution is explainable without showing QA tools.
- Another developer can reproduce the official integration from current guides and the showcase guide.

### Gains

Architectural: produces real consumer evidence for the next package decisions.  
Usability: establishes the first credible public showcase baseline.

Suggested commit:

```text
release(firstgame): prepare showcase 0.1 playable minimum
```

## 14. Mandatory UX evaluation protocol

Every manual cut records:

| Field | Question |
|---|---|
| Task | What was the developer trying to accomplish? |
| Starting point | Which current package guide or Create surface was used? |
| Steps | How many meaningful manual actions were required? |
| Time | How long did the first successful setup take? |
| Ambiguity | Which field, term or object ownership was unclear? |
| Error quality | Did failure identify the missing owner/reference and corrective action? |
| Repetition | Was the same identity/reference entered more than once? |
| Technical exposure | Did the default Inspector expose internal contracts unnecessarily? |
| Recovery | Could the developer fix the issue without a repair script? |
| Idempotency | Did Apply/Rebuild or repeated setup preserve intentional work? |
| Classification | Game issue, guide issue, product-surface issue, runtime defect or QA gap? |

Severity:

```text
UX-0  observation
UX-1  minor wording or ordering friction
UX-2  repeated confusion or avoidable manual wiring
UX-3  common workflow requires technical-contract knowledge
UX-4  common workflow requires consumer facade, hidden fallback or repair tool
```

`UX-3` and `UX-4` block promotion of that product surface until disposition is explicit.

## 15. Where a finding belongs

| Finding | Owner |
|---|---|
| Game objective, movement feel, arena or UI theme | FIRSTGAME |
| Reusable Create/Inspector/Apply workflow is missing or confusing | Package |
| Public runtime contract is incorrect or incomplete | Package, then QA |
| Official contract needs a regression proof | QAFramework |
| Current guide omits or teaches the wrong public flow | Package documentation |
| One-off consumer glue expresses a game rule | FIRSTGAME |
| Consumer glue recreates framework authority | Stop and redesign in Package |

## 16. Advanced roadmap after Showcase 0.1

Advanced work begins only after FG-6 closes.

### Showcase 0.2 — Camera and Activity storytelling

- Add a short arrival Activity.
- Add an explicit Activity camera override.
- Release it back to the Player camera.
- Add a completion/result Activity.
- Validate request restoration and transition timing.

### Showcase 0.3 — Actor presentation and choice

- Add at least two `ActorProfile` options.
- Add a small selection Activity.
- Keep Slot, Actor selection, logical host and presentation visibly distinct.
- Do not start until the current Actor Presentation/Skin product surface is ready enough to avoid local replacement architecture.

### Showcase 0.4 — Local cooperative proof

- Add a second ordered `PlayerSlotProfile`.
- Open a real join window.
- Add a second Player and differentiated Slot presentation.
- Add split-screen only after multi-output Camera becomes official package scope.
- Add multiplayer Pause policy only after it is explicitly decided.

### Showcase 0.5 — Persistence and progression

- Save selected Actor and completed showcase goals.
- Add a small persistent unlock or best-run result.
- Use typed persistence boundaries; do not mutate Profile assets.
- First mature package authoring and QA coverage for persistence.

### Showcase 1.0 — Public framework demo

- Package-ready onboarding guide.
- Stable build.
- Clear feature attribution.
- Accessibility and input pass.
- Performance and release QA.
- No experimental framework dependency on the critical path.

## 17. Features deliberately deferred

```text
online multiplayer
network authority
reconnect state machines
split-screen before multi-output Camera
multiplayer Pause policy
combat framework
inventory framework
procedural generation framework
generic quest system
save/progression before a real product requirement
```

Deferral is part of the product design. These features would dilute the first consumer usability proof and make framework failures harder to isolate.

## 18. Recommended next action

Start only FG-0.

Its output should be a concrete inventory of the current FIRSTGAME project and a proposed asset-by-asset mapping for FG-1. Do not author the Player, Camera or gameplay loop during that audit.

After FG-0, close the exact FG-1 file manifest based on the real project tree and execute the application shell manually.
