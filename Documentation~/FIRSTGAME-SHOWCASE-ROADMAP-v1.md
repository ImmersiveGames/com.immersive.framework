# FIRSTGAME Showcase — Roadmap v1.1

Status: Active execution roadmap  
Last updated: 2026-07-28  
Target: `ImmersiveGames/planet-devourer` as the real-game consumer and eventual public showcase of `com.immersive.framework`

## 1. Decision summary

FIRSTGAME is developed in two connected modes:

```text
consumer integration workshop
  focused Routes, scenes and prefab variants expose assembly order and UX friction

public game slice
  the proven composition is converged into a normal, polished game flow
```

The current menu may expose focused development entries while the framework is being assembled. Those entries are temporary developer UX, not the final player-facing product.

The implementation order follows how a real consumer incrementally builds a game:

```text
Game Application and Routes
-> Persistent Content
-> persistent Camera Output
-> Pause without a Player
-> Pause on the official Player
-> Scene-Provided Logical Player
-> Player gameplay Camera
-> Manager-Provisioned Logical Player
-> Session-Persistent Logical Player product cut
-> real gameplay loop
```

This order intentionally proves visible composition and ownership before advanced gameplay.

## 2. Final showcase direction

The first polished release target remains one complete vertical slice:

```text
Boot
-> Title
-> Start
-> explicit single-player participation
-> short playable objective
-> completion
-> replay or return to title
```

Recommended concept:

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

The game concept stays mechanically small so the framework carries real lifecycle, Player, input, Camera, Pause, reset and diagnostics responsibilities without requiring unrelated advanced systems.

## 3. Source-of-truth rule

Implementation and documentation follow current Git.

Baseline inspected for this roadmap update:

```text
planet-devourer
  ef66f6230cdde576d5ad88ec9ab09bb5466fa963

com.immersive.framework
  91cdb98b1bbf33cc6a48aa08614dafc4713e4344

QAFramework
  4c8fea40949143b2f663de8a6361a7f13ab51a43
```

The repositories remain read-only for planning work. Changes are delivered as explicit files or ZIP packages for manual application.

When architecture documents, current guides and source differ:

```text
current package source and current canonical guide win
ADR records the accepted decision and implementation boundary
FIRSTGAME records consumer assembly and manual proof
QA records technical contract proof
```

## 4. Frozen repository roles

### `com.immersive.framework`

Owns:

```text
runtime
contracts
product authoring surfaces
Editor tooling
validators
diagnostics
canonical guides
ADRs
official templates/samples
```

### `QAFramework`

Owns:

```text
technical contract proof
negative cases
transaction rollback
idempotence
regressions
```

### `planet-devourer`

Owns:

```text
game rules and movement
real scene/prefab composition
consumer UX proof
focused development variants
manual Play Mode records
final gameplay presentation
```

FIRSTGAME must not contain framework compatibility facades, copied QA fixtures, parallel runtime authorities, implicit global managers or automatic repair tooling that hides product friction.

## 5. Evidence vocabulary

Every cut distinguishes:

```text
Present in Git
Authoring Ready
Runtime Implemented
QA Passed
FIRSTGAME Manual Proof Passed
Blocked by Framework
```

Do not collapse those states into a generic `done`.

Source presence does not prove Play Mode behavior. Authoring validation does not prove runtime admission. A QA pass does not prove consumer usability.

## 6. Current demo inventory

### Application

```text
FG_GameApplication.asset
startup Route
four ordered local Player Slot Profiles
Actor duplicate-selection policy
Conteiner Scene as Persistent Content
```

### Enabled scenes

```text
FG_UIGlobal
FG_Menu
FG_Gameplay
Conteiner Scene
SceneProvidedGameplay
```

### Current menu entries

```text
Start Game
  general gameplay Route request

Player Local Test
  FG_PlayerSceneProvider
  SceneProvidedGameplay
```

### Current focused Player prefabs

```text
Actor_PlayerSceneProvided
Player_SceneProvided
Player_SceneProvided_With_Pause
Player_SceneProvided_With_Camera
```

### Current source coverage

| Player source | Package | FIRSTGAME |
|---|---|---|
| Scene-Provided | runtime and authoring implemented | active focused test |
| Manager-Provisioned | runtime and authoring implemented | next consumer assembly |
| Session-Persistent | accepted architecture only | blocked |

## 7. Roadmap overview

| Cut | Type | Outcome | Current state |
|---|---|---|---|
| FG-0 | integration | Game Application, menu and Route foundation | Present in Git |
| FG-1 | UX/product integration | Persistent Content and persistent Camera Output | Present in Git; manual record maintained in FIRSTGAME |
| FG-2 | UX/product integration | Pause outside and on the Player | Present in Git; manual record maintained in FIRSTGAME |
| FG-3 | Player integration | Scene-Provided Player admission | Current focused proof |
| FG-4 | Camera integration | Scene-Provided Player gameplay Camera | Current focused proof |
| FG-5 | Player integration | Manager-Provisioned comparison path | Next |
| FG-6 | package product cut | Session-Persistent Player source | Blocked by package gap |
| FG-7 | real gameplay | Core Harvest vertical slice | Planned after source comparison |
| FG-8 | release | Showcase 0.1 release candidate | Planned |

Cuts are sequential by dependency, but documentation may be updated whenever Git state moves. A cut closes only with the evidence required for its type.

---

## 8. FG-0 — Application, menu and Route foundation

### Objective

Create the smallest understandable application shell and development entry menu.

### Scope

- one explicit `GameApplicationAsset`;
- startup Route;
- Player-independent menu navigation;
- general gameplay Route;
- focused Route entry for integration tests;
- explicit return-to-menu behavior;
- enabled scenes recorded in Build Settings.

### Out of scope

- requiring a Player for basic navigation;
- gameplay objective;
- multiplayer;
- implicit scene lookup.

### Type

`integration real + product UX`

### Files affected

```text
FG_GameApplication.asset
FG_Menu.unity
FG_UIGlobal.unity
FG_Gameplay.unity
Route and Activity assets
EditorBuildSettings.asset
```

### Product surface

```text
GameApplicationAsset
RouteAsset
ActivityAsset
RouteRequestTrigger
Persistent/global composition
```

### Expected use flow

```text
Boot
-> Menu
-> select one explicit Route
-> enter declared primary scene
-> exit
-> Menu restored
```

### Technical smoke

- Route navigation works without an admitted Player.
- Required references are explicit.
- Invalid requests produce typed diagnostics.
- Re-entry does not retain the prior Route scope.

### Technical acceptance

- compiles;
- no scene-name discovery fallback;
- no singleton or parallel navigation authority;
- Route and Activity identity remain typed.

### Product acceptance

- developer can see which menu entry opens which test;
- menu remains understandable without diagnostics;
- focused test entries are clearly development-only.

### Gains

Architectural: isolates Game Flow from Player availability.  
Usability: gives the consumer a visible, incremental entry point.

### Suggested commit

```text
feat(firstgame): build application and route test shell
```

---

## 9. FG-1 — Persistent Content and Camera Output

### Objective

Establish Session-owned persistent composition before contextual gameplay Cameras.

### Scope

- `Conteiner Scene` assigned through `GameApplicationAsset` Persistent Content;
- one physical Camera Output;
- persistent presentation/loading surfaces;
- scoped/session Camera override authoring;
- explicit restoration after contextual release.

### Out of scope

- Player gameplay Camera;
- creating output Camera from a Player rig;
- `Camera.main` fallback;
- multiple physical outputs.

### Type

`UX/product integration`

### Files affected

```text
Conteiner Scene.unity
FG_GameApplication.asset
persistent Camera authoring assets/components
```

### Product surface

```text
Persistent Content composition
CameraOutputSessionBinding
session/scoped override bindings
CameraOutputContext
```

### Expected use flow

```text
Boot Persistent Content
-> default physical output active
-> enter contextual Route
-> contextual request/override selected
-> exit
-> previous/default state restored
```

### Technical smoke

- one Unity Camera and AudioListener authority;
- request identity is explicit;
- duplicate entry does not duplicate override identity;
- release restores previous state.

### Technical acceptance

- no output created by a local Camera rig;
- no global lookup;
- scoped release is deterministic;
- diagnostics show current/default/override state.

### Product acceptance

- Inspector explains physical output versus contextual request;
- a consumer can author the persistent scene without reading runtime internals.

### Gains

Architectural: separates physical output from presentation requests.  
Usability: makes persistence visible before Player complexity.

### Suggested commit

```text
feat(firstgame): prove persistent camera output composition
```

---

## 10. FG-2 — Pause surfaces

### Objective

Prove Pause as an application capability and as Player-bound physical input without conflating the two.

### Scope

- application-only authored Pause control;
- Player `PausePlayerInputBinding` variant;
- `UnityPlayerInputGateAdapter` gameplay action-map blocking;
- resume and scope release;
- diagnostics for execution mode and binding state.

### Out of scope

- multiplayer Pause policy;
- a second global Player;
- hidden input-map repair;
- networking.

### Type

`UX/product integration`

### Files affected

```text
Pause authoring in persistent/route scenes
Player_SceneProvided_With_Pause.prefab
Player input action references
```

### Product surface

```text
PauseRequestTrigger
PausePlayerInputBinding
UnityPlayerInputGateAdapter
Pause runtime/presentation context
```

### Expected use flow

```text
Application-only
  authored button -> logical Pause -> resume

Player-bound
  Player Pause action -> logical Pause -> gameplay map gated -> resume/restored
```

### Technical smoke

- application-only Pause works without Player binding;
- Player Pause requires one eligible admitted Player;
- only the configured gameplay map is gated;
- prior map state restores;
- exit releases bindings.

### Technical acceptance

- no implicit Player discovery;
- no silent fallback from Player-bound to application-only input;
- execution mode is diagnostic;
- TimeScale/presentation follow policy.

### Product acceptance

- consumer can explain button Pause versus Player physical input;
- default Inspector shows required references;
- Advanced/Debug exposes binding evidence.

### Gains

Architectural: separates request authority from physical input source.  
Usability: allows Pause to be tested before and after Player admission.

### Suggested commit

```text
feat(firstgame): prove application and player pause paths
```

---

## 11. FG-3 — Scene-Provided Logical Player

### Objective

Admit a Player already authored in the Route Primary Scene and adopt its existing Logical Actor without duplication.

### Scope

- explicit `PlayerSlotProfile` and `ActorProfile`;
- canonical Actor and outer Player prefab boundaries;
- `LocalPlayerHostAuthoring`;
- `SceneLocalPlayerAdmissionAuthoring`;
- Route Primary Scene admission;
- Actor correlation/adoption;
- Activity readiness and reverse-order release;
- re-entry proof.

### Out of scope

- `PlayerInputManager` provisioning;
- second Player;
- split-screen;
- Session-Persistent identity;
- character replacement.

### Type

`Player integration + product UX`

### Files affected

```text
Actor_PlayerSceneProvided.prefab
Player_SceneProvided.prefab
SceneProvidedGameplay.unity
FG_PlayerSceneProvider.asset
Activity_PlayerLocalProvider.asset
Player Slot and Actor Profile assets
```

### Product surface

```text
PlayerSlotProfile
ActorProfile
LocalPlayerHostAuthoring
SceneLocalPlayerAdmissionAuthoring
PlayerParticipationRuntimeContext
Activity participation fields
```

### Expected use flow

```text
Player Local Test
-> Route Primary Scene loads
-> explicit Scene-Provided composition resolved
-> Slot admitted
-> existing Host validated
-> existing Logical Actor adopted
-> Activity reaches configured readiness
-> exit releases contextual evidence
```

### Technical smoke

- one typed Slot assignment;
- one Host identity;
- one current Actor correlation;
- source records external scene ownership;
- no provisioning call;
- no duplicate Actor;
- second entry does not duplicate admission.

### Technical acceptance

- runtime recognizes active Route Primary Scene authoring;
- failure is explicit when Slot/Profile/Actor evidence is invalid;
- release preserves scene ownership and clears contextual evidence;
- no name or `playerIndex` identity.

### Product acceptance

- designer places one outer prefab;
- Inspector shows Slot, Actor and scene Actor intent;
- Apply/Rebuild is idempotent and non-destructive;
- Advanced/Debug explains adoption and ownership.

### Gains

Architectural: proves scene origin without creating a second participation lane.  
Usability: validates a common single-player authoring workflow.

### Current state

```text
Runtime Implemented
Authoring Ready in FIRSTGAME
Focused manual admission/release proof in progress
```

### Suggested commit

```text
feat(firstgame): prove scene-provided player admission
```

---

## 12. FG-4 — Scene-Provided Player gameplay Camera

### Objective

Add one contextual Player Camera without transferring physical output authority to the Player.

### Scope

- explicit Player Camera targets;
- `CameraRigComposer` inside Actor hierarchy;
- idempotent local Cinemachine materialization;
- `PlayerGameplayCameraAuthoring`;
- Player/Actor-correlated Camera eligibility;
- request precedence and output restoration;
- Pause interaction.

### Out of scope

- creating Unity Camera/Brain/AudioListener from the rig;
- cinematics;
- split-screen;
- implicit target resolution.

### Type

`Camera integration + gameplay foundation`

### Files affected

```text
Player_SceneProvided_With_Camera.prefab
SceneProvidedGameplay.unity
Actor Camera anchors
game-specific movement binding
```

### Product surface

```text
CameraRigComposer
PlayerGameplayCameraAuthoring
PlayerGameplayCameraEligibilityRuntimeContext
CameraOutputContext
```

### Expected use flow

```text
Scene-Provided Player admitted
-> Actor evidence prepared/adopted
-> Camera rig targets resolve
-> Player Camera eligibility published
-> persistent output selects request
-> exit releases request
-> prior/default output restored
```

### Technical smoke

- explicit Follow/LookAt targets;
- local Cinemachine Camera materialized once;
- no physical output duplication;
- request exists only while eligible;
- Pause/resume does not lose Camera identity;
- re-entry does not duplicate request identity.

### Technical acceptance

- no `Camera.main` lookup;
- rig belongs to the same Actor hierarchy;
- missing required targets block explicitly;
- output restoration is deterministic.

### Product acceptance

- designer understands rig versus output;
- Apply/Rebuild reports created/repaired/already valid evidence;
- default Inspector remains focused;
- debug mode exposes request and target evidence.

### Gains

Architectural: preserves output authority while adding contextual presentation.  
Usability: turns Player composition into a visible playable result.

### Current state

```text
Authoring Ready in FIRSTGAME
Focused manual arbitration/restoration proof in progress
```

### Suggested commit

```text
feat(firstgame): prove scene player camera request flow
```

---

## 13. FG-5 — Manager-Provisioned Logical Player comparison

### Objective

Build the same minimum playable capability with a runtime-created Host so a consumer can compare origin, ownership and assembly sequence.

### Scope

- separate menu entry, Route and test scene;
- Persistent Content provisioning surface;
- manual-join `PlayerInputManager`;
- `LocalPlayerProvisioningAuthoring`;
- `LocalPlayerProvisioningHostRegistration`;
- Player prefab with `PlayerInput`, `LocalPlayerHostAuthoring` and empty Actor Mount;
- explicit authorized join;
- ordered Slot reservation;
- Actor preparation;
- rollback and Slot reuse proof;
- equivalent movement/Pause/Camera integration after admission.

### Out of scope

- second simultaneous Player;
- automatic join;
- split-screen;
- device reconnect;
- network participation.

### Type

`Player integration + comparison UX`

### Files created or changed

Expected FIRSTGAME paths, finalized during implementation:

```text
new Manager-Provisioned Route asset
new focused gameplay scene
new Player_ManagerProvisioned prefab
Persistent Content provisioning prefab/composition
menu entry
Activity participation asset/update
```

No package or QA file changes unless a reusable blocker is confirmed.

### Product surface

```text
PlayerInputManager
LocalPlayerProvisioningAuthoring
LocalPlayerProvisioningHostRegistration
LocalPlayerHostAuthoring
Slot reservation/current assignment diagnostics
Actor preparation
```

### Expected use flow

```text
select Manager-Provisioned test
-> enter Route
-> issue explicit join
-> reserve Slot
-> PlayerInputManager creates Host
-> validate and admit Logical Player
-> prepare Actor
-> enable input/Camera/gameplay
-> exit and release
```

### Technical smoke

- framework reserves Slot before physical provisioning;
- exactly one Host is created;
- `playerIndex` remains diagnostic;
- failed join rolls back reservation and Host evidence;
- successful exit allows Slot reuse;
- re-entry is deterministic.

### Technical acceptance

- one provisioning authority;
- no automatic join lane;
- no silent Host adoption;
- no fallback Slot;
- rollback is explicit and diagnostic.

### Product acceptance

- developer can distinguish provisioning Host, Slot, Actor and Activity policy;
- Inspector explains where the Player prefab is configured;
- join command and result are discoverable;
- negative recovery does not require internal code inspection.

### Gains

Architectural: proves the runtime-created source against the same canonical authority.  
Usability: reveals which Player source is clearer for real consumers.

### Suggested commit

```text
feat(firstgame): add manager-provisioned player comparison
```

---

## 14. FG-6 — Session-Persistent Logical Player product cut

### Objective

Create an official package source for Logical Player identity that exists at Session scope outside all Routes and Activities.

### Current state

`Blocked by Framework`.

### Scope

Package first:

```text
authoring component or composer
explicit admission request/result
typed ownership and lifetime evidence
Slot assignment policy
optional Host/Actor adoption
contextual materialization reconciliation
Session versus Activity release diagnostics
short canonical guide
```

Then QA:

```text
admission
Route changes
Activity projection
Actor preparation/adoption
release ordering
invalid configuration
idempotence
```

Then FIRSTGAME:

```text
separate comparison Route
persistent identity across Menu and Gameplay
contextual Actor/Camera/input proof
```

### Out of scope

- FIRSTGAME local workaround;
- arbitrary persistent prefab treated as participation authority;
- global singleton or service locator;
- network account/session system.

### Type

`package product + QA + consumer integration`

### Files created or changed

Must be declared by the dedicated package cut. FIRSTGAME files are not created until the official contract exists.

### Product surface affected

```text
Game Application / Session composition
PlayerParticipationRuntimeContext
new Session-Persistent authoring surface
Actor/materialization reconciliation
Diagnostics
```

### Expected use flow

```text
Session starts
-> explicit Session-Persistent Logical Player admitted
-> Menu Route projects or ignores it
-> Gameplay Route projects it
-> contextual Actor/input/Camera prepared
-> Gameplay exits
-> contextual parts release
-> Logical Player Session identity remains
```

### Technical smoke

- one Session participation authority;
- Route exit does not destroy Logical Player identity;
- contextual evidence releases independently;
- existing valid Host/Actor parts are adopted, not duplicated;
- missing required policy fails explicitly.

### Technical acceptance

- no global lookup;
- no parallel Slot registry;
- lifetime and ownership are typed;
- invalid state has no silent fallback;
- QA proves transitions and rollback.

### Product acceptance

- designer can see Session intent separately from Activity intent;
- Inspector explains what persists and what is contextual;
- Apply/Rebuild is explicit and idempotent;
- FIRSTGAME can assemble it without internal contract knowledge.

### Gains

Architectural: completes the three-source model through one authority.  
Usability: supports persistent local participation without ad-hoc managers.

### Suggested commits

Package:

```text
feat(player): add session-persistent logical player source
```

QA:

```text
test(player): prove session-persistent participation lifecycle
```

FIRSTGAME:

```text
feat(firstgame): prove session-persistent player integration
```

---

## 15. FG-7 — Core Harvest real gameplay loop

### Objective

Converge the selected production Player source into one short, complete game loop.

### Scope

- compact arena;
- one controllable Actor;
- three collectible energy fragments;
- central core completion target;
- clear objective UI;
- completion result;
- restart/replay and return-to-title;
- game-specific movement and objective code remain in FIRSTGAME.

### Out of scope

- combat;
- inventory;
- progression;
- save data;
- procedural generation;
- required multiplayer;
- advanced audio system.

### Type

`real gameplay integration`

### Files created or changed

```text
gameplay scenes/prefabs
FIRSTGAME objective components
UI presentation
Activity restart/completion commands
selected production Player prefab/composition
```

### Product surface affected

```text
Route/Activity lifecycle
Player participation
input and Camera
Pause
reset/restart
loading/transition
Global UI
Diagnostics
```

### Expected use flow

```text
Title
-> Start
-> Player ready
-> collect three fragments
-> feed central core
-> completion
-> replay or return
```

### Technical smoke

- objective state belongs to the Activity/run;
- restart clears objective state without ad-hoc scene reload authority;
- Pause gates gameplay correctly;
- exit releases Player contextual evidence;
- second run is clean.

### Technical acceptance

- compiles and passes relevant QA package gates;
- no hidden fallback;
- failures are diagnostic;
- framework contracts remain preserved.

### Product acceptance

- player understands objective without debug UI;
- normal play path does not expose validator/smoke controls;
- setup is documented from a clean consumer perspective;
- selected Player source is justified by prior comparison.

### Gains

Architectural: proves framework responsibilities in a real loop.  
Usability: converts integration pieces into a usable game product.

### Suggested commit

```text
feat(firstgame): build core harvest playable loop
```

---

## 16. FG-8 — Showcase 0.1 release candidate

### Objective

Make the slice understandable, polished and reproducible.

### Scope

- final title and result presentation;
- loading/transition polish;
- short setup documentation;
- remove or hide development-only menu entries from release configuration without deleting test scenes;
- negative diagnostics remain available in Advanced/Debug workflows;
- final manual regression record.

### Out of scope

- broad feature expansion;
- new Player source;
- unproven experimental adapter as release blocker.

### Type

`release + documentation`

### Files created or changed

```text
release scenes/UI
consumer setup guide
final scenario records
build configuration
```

### Product surface affected

All surfaces proven by the vertical slice.

### Expected use flow

```text
launch
-> understand title
-> start
-> play and complete
-> pause/restart safely
-> replay or return
```

### Technical smoke

- clean boot;
- complete run;
- Pause/resume;
- restart;
- return/re-entry;
- no missing references or duplicate authorities;
- logs remain concise and diagnostic.

### Technical acceptance

- compiles;
- relevant QA technical gates pass;
- manual FIRSTGAME scenarios pass;
- no silent fallback;
- package and consumer docs agree.

### Product acceptance

- no QA-style control panel in player journey;
- one clear objective;
- stable input and Camera;
- reproducible authoring steps;
- Advanced/Debug remains available for technical inspection.

### Gains

Architectural: establishes a maintained real consumer baseline.  
Usability: demonstrates that the framework can build an understandable game, not only valid contracts.

### Suggested commit

```text
release(firstgame): prepare showcase 0.1 candidate
```

## 17. UX observation protocol

For every manual assembly or validation session, record:

```text
task
starting surface
steps taken
time to first valid Play result
ambiguous terms
repeated references
error quality
recovery path
technical fields exposed by default
Advanced/Debug usefulness
Apply/Rebuild idempotence
whether the issue belongs to FIRSTGAME, package, QA or docs
```

Severity:

```text
Observation
  understandable friction that does not block completion

Product issue
  recurring workflow is unnecessarily difficult or unclear

Blocker
  consumer cannot complete the official workflow without internal knowledge,
  local workaround or silent invalid state
```

## 18. Definition of success

The roadmap succeeds when:

```text
a developer can follow the assembly sequence
Player source differences are explicit
framework materializes contracts without hiding architecture
FIRSTGAME proves real use
QA proves technical behavior
final player journey contains no QA-style setup flow
```
