# IF-ADR-032 — Camera Unified Authority, Session Outputs, Presentations, Subjects and Lifecycle

Status: **Accepted current architecture / CAMERA-032-A..E focused evidence retained / CAMERA-032-F legacy removal implemented / aggregate Unity certified**
Accepted: **2026-09-21**  
Type: architecture / Camera / authoring / runtime lifecycle / physical output / multiplayer  
Normative authority: **This is the single current Camera architecture decision for Immersive Framework.**

> IF-ADR-032 consolidates and replaces the normative Camera decisions previously distributed across
> IF-ADR-004, IF-ADR-004C, IF-ADR-022, IF-ADR-026, IF-ADR-027, IF-ADR-028,
> IF-ADR-029, IF-ADR-030 and IF-ADR-031.
>
> Those ADR files are removed from the active ADR set. Git history and dated reconciliation /
> certification records remain historical evidence for the boundaries they executed.
>
> Where historical Camera documentation conflicts with this ADR, IF-ADR-032 is authoritative.

## 1. Context

The Camera domain evolved incrementally through several architecture cuts.

That work established important invariants:

- explicit physical Camera Outputs;
- output-owned persistent Default presentation;
- deterministic CameraRequest arbitration;
- transactional admission/release and rollback;
- CameraRigComposer materialization authority;
- typed Camera Subjects with occurrence safety;
- multi-Subject Group presentation;
- optional Subject framing evidence;
- explicit per-Composition Subject selection;
- PlayerInputManager ownership of physical split-screen layout.

The same evolution also accumulated authoring and lifecycle coupling that is no longer desirable:

- Camera Output capacity and gameplay presentation are commonly authored together in persistent scene/prefab composition;
- CameraSharedComposition combines serialized configuration, runtime occurrence state, Subject membership, Rig application, request publication and rollback;
- PlayerCameraCompositionPolicyAuthoring serializes direct references to concrete scene CameraSharedComposition components;
- Player Camera composition topology is resolved once at boot from persistent roots;
- RouteCameraOverride and ActivityCameraOverride invert ownership by placing camera intent in scene components that point back to RouteAsset or ActivityAsset;
- normal gameplay Camera rigs must already exist as scene objects;
- Camera Output and policy discovery depends on Persistent Content root scanning;
- the existing lifecycle is partly approximated through MonoBehaviour enable/disable despite the Framework now having explicit RuntimeContent Session, Route and Activity scopes.

The project is still in architecture development. Compatibility with the experimental Camera composition shape is not a goal when a simpler authority model is available.

The consolidation therefore keeps proven contracts and replaces the current product-facing composition/lifecycle structure.

## 2. Decision

> **Camera Outputs are Session-owned physical capacity. Camera Presentations are reusable authored intent owned by Session, Route or Activity lifecycle. Camera Subjects provide observable runtime evidence. Camera Requests remain the sole normal Output arbitration mechanism.**

The canonical Camera domain becomes:

~~~text
Game Flow
Session / Route / Activity
        |
        v
CameraPresentationDefinition
        |
        v
CameraPresentationRuntime
  Subject selection
  Membership
  Presentation input
  Rig occurrence
        |
        v
CameraRequest
        |
        v
CameraOutputSession
        |
        v
Camera Output
  Unity Camera
  CinemachineBrain
  Default Rig
~~~

There is no second normal winner-selection authority beside CameraOutputContext.

## 3. Authority matrix

| Authority | Owns | Does not own |
|---|---|---|
| GameApplication / Camera Session configuration | Session Camera capacity and explicit Output set | Route/Activity gameplay presentation lifetime |
| Session / Route / Activity | Declaration of optional Camera Presentation intent for that lifecycle | Physical Camera mutation or winner arbitration |
| CameraPresentationDefinition | Reusable authored presentation recipe | Mutable occurrence state |
| CameraPresentationRuntime | One live Presentation occurrence, Subject membership, presentation application, request publication/release and rollback | Physical Output topology |
| Camera Subject availability | Current observable Subject occurrences and stale-evidence safety | Player identity interpretation or winner arbitration |
| CameraRigComposer | One local materialized Cinemachine rig and behavior application | Subject-selection policy or Output winner selection |
| CameraRequest | Explicit participation intent for one Output | Direct Unity/Cinemachine mutation |
| CameraOutputContext | Admitted normal requests and deterministic winner | Subject selection or physical screen layout |
| CameraOutputSession | Transactional logical/physical synchronization, Default and force-default state | Game Flow authoring |
| Camera Output | Physical Unity Camera, Brain and persistent Default Rig | Player count or screen partition policy |
| PlayerInputManager | Unity automatic split-screen count / Camera.rect recomposition | Camera Subject, Rig, request or Output identity authority |
| RuntimeContent | Materialization/release ownership by Session / Route / Activity scope | Camera-specific winner arbitration |

## 4. Camera Session and physical Output capacity

The application declares explicit Camera capacity for one Session.

Target authoring:

~~~text
GameApplication
  Camera Session configuration
    Output A
    Output B
    ...
    optional Player Slot -> Output bindings
~~~

The concrete product type may be named CameraSessionProfile or an equivalent explicit Session configuration during implementation. The architectural requirement is the ownership, not the final type name.

Rules:

- one Session has 1..N explicitly configured Camera Outputs;
- each Output has one exact CameraOutputDefinition;
- each Output has one Unity Camera and one CinemachineBrain;
- each Output has one explicit persistent Default Rig;
- Outputs are created/materialized before normal Route/Activity Camera presentation can participate;
- Output count is never inferred from Player count;
- Route and Activity never synthesize physical Outputs from their own lifecycle;
- optional local multiplayer capacity is explicit Session configuration;
- physical Output identity remains typed and exact-reference based in normal authoring.

The target architecture keeps the Output topology stable for the Session.

Dynamic add/remove of physical Outputs is not accepted by this ADR because no current consumer requires it and it would unnecessarily broaden output registration, Player binding and rollback semantics.

## 5. Default presentation and continuity

Every physical Output owns one Default Rig.

The selection rule remains:

~~~text
force-default owner active
  -> Default Rig

otherwise a normal CameraRequest winner exists
  -> winning Rig

otherwise
  -> Default Rig
~~~

The Default:

- is not a CameraRequest;
- has no CameraRequestId;
- has no request precedence;
- has no tie-break identity;
- must remain usable without gameplay Subjects;
- is persistent for the physical Output lifetime.

Default exists to preserve valid physical presentation when no gameplay presentation is eligible and during lifecycle gaps such as Route replacement.

A transition may force Default without destroying or rewriting admitted normal requests.

SessionCameraTransitionOrchestrator remains a valid application of this rule.

## 6. CameraPresentationDefinition

A Camera Presentation is reusable authored intent.

Target concept:

~~~text
CameraPresentationDefinition
  Rig Prefab
  Camera Output Definition
  Subject Selection Policy
  Request Arbitration Policy
~~~

The first implementation must remain deliberately small. Do not create a generic mega Camera definition.

A Presentation Definition may declare only information that is stable, reusable authoring intent.

It must not contain:

- current Subject occurrences;
- membership tokens;
- availability revisions;
- current Player Slot runtime state;
- materialized CameraRigComposer instances;
- admitted CameraRequest instances;
- mutable rollback state.

CameraRigBehaviorDefinition remains a separate concern:

~~~text
CameraRigBehaviorDefinition
  = how one Rig behaves

CameraPresentationDefinition
  = which reusable Presentation occurrence should be materialized and participate
~~~

The two are not competing authorities.

## 7. CameraPresentationRuntime

CameraSharedComposition as a product-facing MonoBehaviour authority is superseded.

Its proven runtime responsibilities are retained in a runtime occurrence boundary, provisionally named CameraPresentationRuntime.

One Presentation Runtime occurrence owns:

- exact occurrence identity;
- current Subject selection source/evidence;
- ordered Subject membership;
- membership revision and stale-evidence protection;
- projection of current membership to presentation input;
- application/clear of presentation on the materialized Rig;
- normal CameraRequest publication and release;
- coherent teardown;
- transactional rollback between membership, presentation and request/output state;
- diagnostics for that exact occurrence.

It does not own:

- physical Unity Camera;
- CinemachineBrain;
- Default Rig;
- Output topology;
- PlayerInputManager layout;
- global Player/Actor lookup;
- implicit scene/hierarchy discovery.

The runtime occurrence is mutable runtime state and is not a ScriptableObject.

## 8. Camera Rig authority

CameraRigComposer remains the canonical local Rig materialization/application authority unless implementation work demonstrates a smaller equivalent boundary.

Accepted presentation family remains:

~~~text
Follow      = 10
Fixed       = 20
Mounted     = 30
ThirdPerson = 40
Group       = 50
~~~

Semantics remain:

- Fixed: authored pose, target-independent unless an explicitly supported optional look target participates;
- Follow: exactly one Subject;
- Mounted: exactly one Subject/mount;
- ThirdPerson: exactly one Subject;
- Group: one or more Subjects through the supported Cinemachine group presentation.

Many Subjects with Follow are invalid and are never silently reinterpreted as Group.

Editor authority:

~~~text
CameraRigBehaviorDefinition
        |
        v
CameraRigComposer Apply/Rebuild
        |
        v
materialized Cinemachine structure in prefab/template
~~~

Runtime authority:

~~~text
materialized Rig prefab instance
        |
        v
CameraRigComposer presentation application
~~~

Runtime does not become a generic Cinemachine graph editor.

## 9. Rig prefab and runtime materialization

Normal gameplay Presentations no longer require their CameraRigComposer to be parked in Persistent Content or Route/Activity scenes.

A Presentation may reference a prefab containing the already materialized CameraRigComposer/Cinemachine structure.

Runtime instantiates and releases the presentation-owned prefab occurrence through the existing RuntimeContent lifecycle infrastructure.

Camera must not introduce a parallel global Camera materialization registry or service locator.

The implementation should reuse:

- RuntimeContentScope.Session;
- RuntimeContentScope.Route;
- RuntimeContentScope.Activity;
- RuntimeContentOwner;
- RuntimeScopeContext;
- scoped cancellation;
- explicit materialization identity;
- explicit release;
- Unity prefab materialization/release adapters where appropriate.

## 10. Camera Subjects

Camera Subject remains typed observable evidence.

Canonical evidence:

~~~text
Camera Subject
  identity
  Observation Transform
  optional Framing Radius
~~~

Framing Radius = 0 means unspecified.

Subject occurrence safety remains mandatory:

- availability context identity;
- exact occurrence token;
- monotonic availability revision;
- foreign/stale token rejection;
- rejoin/replacement creates a fresh occurrence;
- stale evidence cannot restore an older Subject occurrence.

Camera core never derives Subject identity from GameObject name, hierarchy, index, ActorProfile or Player Slot.

## 11. Subject selection

Accepted selection modes remain:

~~~text
AllAvailableSubjects
ExplicitSelection
~~~

ExplicitSelection consumes Camera-domain Subject identity only.

Camera core does not know PlayerSlotId.

A Player adapter may translate current Player/Actor occurrence evidence into Camera Subject selection, but that adapter remains outside Camera composition semantics.

Missing explicitly selected Subjects are not silently replaced by another available Subject.

## 12. Scene-provided Camera Subjects and anchors

Scenes remain valid providers of world-local Camera evidence.

A scene may provide:

- world content;
- Actors;
- ActorCameraSubjectAuthoring;
- future generic Camera Subject/anchor authoring when a concrete consumer requires it;
- other local content consumed by a Presentation.

A scene-specific Transform requirement does not justify keeping the complete Camera presentation configuration in the scene.

If a future non-Actor consumer requires an explicit scene Transform, introduce a typed Camera-domain Subject/anchor publication boundary rather than restoring Camera.main, name lookup, hierarchy search or a global registry.

## 13. Game Flow ownership

Session, Route and Activity may each declare zero or more Camera Presentations.

Target product surface:

~~~text
GameApplication
  Session Camera Presentations (optional)

RouteAsset
  Camera Presentations (optional)

ActivityAsset
  Camera Presentations (optional)
~~~

The exact serialized shape may evolve during implementation, but the ownership rule is normative.

Lifecycle:

~~~text
Session starts
  -> materialize Session-owned Camera content

Route enters
  -> materialize Route-owned Camera Presentations

Activity enters
  -> materialize Activity-owned Camera Presentations

Activity exits
  -> release only that Activity occurrence's Presentations

Route exits
  -> release only that Route occurrence's Presentations

Session ends
  -> release Session Camera content and Outputs
~~~

Release authority follows exact lifecycle ownership. One owner must never release another owner's live Presentation.

## 14. RuntimeContent integration

Camera Presentation lifetime must use existing framework lifecycle ownership instead of inventing a Camera-specific lifecycle manager.

Route and Activity already create RuntimeContent scope roots with exact authored-definition tokens.

Therefore Camera Presentation materialization/release should bind to those existing scope contexts.

This preserves:

- exact Session/Route/Activity ownership;
- duplicate-safe runtime content identity;
- scoped cancellation;
- stale-scope rejection;
- owner-safe release;
- deterministic teardown.

## 15. CameraRequest authority

Normal participation continues through CameraRequest.

A request carries the evidence needed for arbitration and physical projection of one already-materialized Rig.

Target request responsibilities:

- request identity;
- explicit Output identity;
- owner;
- lifetime;
- materialized Rig reference;
- request policy / precedence / deterministic tie-break;
- release semantics;
- diagnostics.

CameraTargetSourceDescriptor inside CameraRequest is not preserved as an architectural requirement.

Current Output arbitration does not consume target-source data; targets belong to the Presentation/Rig side before the request reaches the Output.

Implementation should remove or reduce this field if no remaining consumer requires it.

## 16. Request ownership and Presentation lifecycle

The current Composition-specific request owner/lifetime exists because CameraSharedComposition has an independent MonoBehaviour lifecycle.

Under IF-ADR-032, normal Presentation occurrences belong to an actual framework lifecycle:

~~~text
Session Presentation  -> Session owner/lifetime
Route Presentation    -> Route owner/lifetime
Activity Presentation -> Activity owner/lifetime
~~~

Therefore CameraRequestOwnerKind.Composition, CameraRequestLifetimeKind.Composition and CompositionCameraRequestPublisher are not preserved as mandatory architecture.

They should be removed if the migrated runtime can express every normal Presentation through its real Session/Route/Activity owner.

Subject eligibility remains independent from owner lifetime:

- a Route may remain active while its Presentation has no required Subjects;
- losing eligibility releases that Presentation's current request;
- regaining eligible current Subjects may republish within the same Route occurrence;
- ending the Route releases the Presentation occurrence regardless of eligibility.

## 17. Request precedence

CameraOutputContext remains the only normal request winner authority.

Higher precedence wins.

Equal precedence requires deterministic unique tie-break evidence.

Release restores the next admitted valid winner or the Default.

Product authoring may define conventional precedence ranges for Session, Route and Activity, but those conventions do not create a second hierarchy or bypass CameraOutputContext.

Do not hard-code gameplay winner selection outside normal request arbitration.

## 18. Player integration

Player integration has two separate relations:

~~~text
Player Slot -> physical Camera Output

Player Slot -> current Camera Subject selection for a Presentation occurrence
~~~

They remain explicit and must not be merged into one ambiguous authority.

### 18.1 Player Slot -> Output

This is Session-level physical presentation integration.

Target authoring belongs with Session Camera configuration rather than a Persistent Scene MonoBehaviour.

Example:

~~~text
Camera Session
  Slot P1 -> Output P1
  Slot P2 -> Output P2
~~~

The binding resolves PlayerInput.camera to the exact physical Output Camera.

Player count never creates Outputs.

### 18.2 Player Slot -> Presentation Subject selection

Player Actor integration continues to publish current Camera Subject occurrences.

A Player adapter selects the current Subject for the appropriate live Presentation occurrence.

Serialized configuration must not require a direct reference to a persistent scene CameraSharedComposition.

The adapter must preserve:

- Camera core independence from Player types;
- fresh Subject occurrence after leave/rejoin or replacement;
- no P1/P2 substitution;
- explicit selection;
- deterministic release/reattach behavior.

## 19. Physical presentation and split-screen

Framework Camera does not own generic physical screen layout.

Camera does not write or infer:

- Camera.rect;
- pixelRect;
- split-screen partition;
- safe area;
- target display;
- target texture;
- Picture-in-Picture placement.

For Unity automatic local split-screen:

~~~text
Player Slot -> exact Camera Output
  -> PlayerInput.camera
  -> PlayerInputManager owns split count and Camera.rect recomposition
~~~

PlayerInputManager does not become Camera Subject, Presentation, Rig, request or Output identity authority.

## 20. Transition and lifecycle continuity

Physical Outputs and their Defaults exist before normal Route/Activity presentation participation.

During a covered Route transition:

~~~text
existing normal requests remain logically owned
        |
force-default active
        v
Default presentation visible
        |
old lifecycle content may release
new lifecycle content may materialize
        |
force-default released
        v
current normal winner or Default
~~~

The Framework must never require a gameplay scene Camera to remain alive merely to bridge scene replacement.

Terminal application shutdown is not a normal Route / Activity / Session transition. During `OnApplicationQuit`, Unity may invalidate scene-backed Camera objects before Framework callbacks finish. In that terminal-only path:

- Presentation/runtime subscriptions and RuntimeContent ownership must still be released best-effort;
- the Framework must not require normal winner restoration or Default re-application after Unity physical Camera scene validity is already gone;
- terminal cleanup may abandon remaining CameraRequest/output synchronization because there is no subsequent rendered frame or surviving Session;
- this exception is restricted to application termination and must not weaken normal Route, Activity, Session, rollback or force-default transaction semantics.

## 21. Transaction and rollback requirements

The architecture preserves the transaction guarantees proven by the previous Composition implementation and QA.

Required invariants:

- membership reconciliation is occurrence-safe;
- presentation apply failure does not partially commit newer membership/request state;
- request admission failure restores the prior coherent membership/presentation state;
- physical Output admission rollback failure remains explicit critical failure evidence;
- request release failure preserves recoverable previous publication/presentation state;
- failed teardown must not silently discard ownership;
- stale presentation rollback cannot overwrite newer applied evidence;
- stale membership rollback cannot overwrite newer membership evidence;
- competing winner remains correct after another Presentation admission rollback;
- force-default remains effective across failed normal request operations;
- retry after a successfully rolled-back failure may complete.

Implementation may reorganize classes, but these behaviors remain acceptance contracts.

## 22. Persistent Content responsibility

Persistent Content remains application-persistent scene composition for systems that genuinely require authored persistent scene content.

It is not the normal Camera topology authority under IF-ADR-032.

Target Persistent Content may contain, as needed:

- EventSystem / UI input;
- Loading presentation;
- Transition presentation;
- Pause presentation;
- Audio;
- Player provisioning;
- other genuinely persistent consumer content.

Camera Outputs and gameplay Presentations must not be required merely because Persistent Content exists.

Existing Persistent scenes containing CameraOutputAuthoring, CameraSharedComposition or Player Camera policies are migration input, not target authoring.

## 23. Removed / superseded product architecture

The following are removed or superseded as normal product architecture:

~~~text
CameraSharedComposition as product-facing MonoBehaviour authority

SessionCameraOverride as normal Session Camera authoring
RouteCameraOverride as normal Route Camera authoring
ActivityCameraOverride as normal Activity Camera authoring
ScopedCameraOverride as the normal Game Flow camera path

PlayerCameraCompositionPolicyAuthoring with scene-object Composition references
PlayerCameraOutputPolicyAuthoring as Persistent Scene configuration

Camera Output discovery from Persistent Content roots as the target topology source
gameplay Camera rigs parked in Persistent Content as the normal reusable model
static Player -> Composition topology resolved only once at boot
Composition-owned lifecycle used instead of real Session / Route / Activity ownership
~~~

These types may remain temporarily during migration but are not target architecture.

## 24. Preserved contracts from the previous Camera work

The following remain architectural requirements:

~~~text
CameraOutputDefinition exact authored identity
CameraOutputContext deterministic arbitration
CameraOutputSession transaction/rollback
CameraOutputRigApplicator physical projection
Session 1..N Output topology
Output-owned Default semantics
force-default semantics

CameraRigComposer materialization/application authority
CameraRigBehaviorDefinition
Fixed / Follow / Mounted / ThirdPerson / Group

Camera Subject availability
Subject occurrence identity and stale protection
optional Subject framing radius
AllAvailableSubjects
ExplicitSelection
Camera core independent from Player identity

normal CameraRequest arbitration
PlayerInputManager physical split-layout authority
no Camera.main / service locator / implicit hierarchy discovery
~~~

## 25. Historical evidence

Dated Camera reconciliation and technical certification documents remain historical evidence only.

They prove the implementation boundary they executed at that date.

They do not certify IF-ADR-032 unless a later record explicitly executes the IF-ADR-032 target implementation.

In particular:

- IF-ADR-029/030 certification remains evidence for the former CameraSharedComposition boundary;
- IF-ADR-031 A/B implementation remains useful experimental evidence for explicit selection and Player adaptation;
- historical Full Camera matrices remain dated evidence for their former product surfaces;
- old certification counts must not be relabeled as IF-ADR-032 coverage.

## 26. Rejected alternatives

Rejected:

~~~text
global Camera manager or service locator
Camera.main authority
FindObjectOfType / name / tag / hierarchy discovery as functional identity
Player count inferring Output count
Route or Activity dynamically inventing physical Outputs
PlayerInputManager choosing Subjects or Rigs
Player identity embedded in CameraSubject identity
PlayerSlotId added to Camera core Subject contracts
screen layout inferred from Subjects / Presentations / requests / Output count
Default represented as a fake low-precedence CameraRequest
gameplay and Default sharing one Rig
ScriptableObject storing mutable Presentation occurrence state
generic mega Camera definition asset
parallel Camera lifecycle/materialization registry beside RuntimeContent
preserving scene-object Camera composition only for backward compatibility
~~~

## 27. Implementation migration plan

IF-ADR-032 is accepted before runtime migration. Current code remains a transitional implementation until the cuts below complete.

### CAMERA-032-A — Presentation runtime extraction — IMPLEMENTED

Delivered in this cut:

- `CameraPresentationRuntime` is the non-MonoBehaviour occurrence authority;
- membership, availability/selection subscriptions, consumed-selection revision, Rig transaction, request publication/release and rollback moved out of `CameraSharedComposition`;
- `CameraSharedComposition` is reduced to serialized authoring fields, dependency-injection interfaces and Unity enable/disable adaptation;
- existing serialized Camera authoring shape is intentionally retained for migration safety during CUT A;
- request owner/lifetime remains `Composition` in this cut and is not yet reconciled to Session/Route/Activity;
- Outputs and Game Flow authoring are intentionally unchanged;
- package tests that inspected old private composition state now inspect the extracted runtime instead.

Validation state:

~~~text
Static code review                 PASS
Unity import/compile               PASS — consumer reported
QAFramework Structural             10/10 PASS
QAFramework Shared                 10/10 PASS
QAFramework Generic request/output 11/11 PASS
Consumer migration                 NOT STARTED
~~~

Primary CUT A validation is the QAFramework Camera rail, not package NUnit tests.

Required focused evidence:

~~~text
Structural regression              10/10
Shared Camera lifecycle            10/10
Generic request/output fixture     11/11
----------------------------------------
CAMERA-032-A focused QA            31/31
~~~

Package-local NUnit tests may remain supporting implementation tests, but they are not the technical certification gate for this project.

Focused CAMERA-032-A QA is complete: 31/31 PASS.

The later Full Camera aggregate failure belongs to historical ADR-004B duplicate-Output negative-integrity evidence and does not block CUT A.

Evidence:

- [IF-ADR-032-A Camera Presentation Runtime Focused Validation — 2026-09-21](../Reconciliation/IF-ADR-032-A-CAMERA-PRESENTATION-RUNTIME-FOCUSED-VALIDATION-2026-09-21.md)

CAMERA-032-A is technically validated. Full IF-ADR-032 certification remains pending later migration cuts.

### CAMERA-032-B — Presentation definition and Rig prefab — IMPLEMENTED / CONSUMER PROVEN

Delivered in this cut:

- public `CameraPresentationDefinition` ScriptableObject with explicit stable `CameraPresentationId`;
- exact `CameraOutputDefinition` participation reference;
- explicit materialized Rig prefab reference;
- preserved `AllAvailableSubjects` / `ExplicitSelection` policy selection;
- explicit normal request precedence;
- authoring validation rejects missing identity, invalid Output, missing Rig prefab, multiple Composers, embedded CameraOutputAuthoring, embedded CameraSharedComposition, invalid Rig behavior and missing materialized CinemachineCamera;
- `CameraPresentationMaterializationRuntime` creates/releases one Rig occurrence under exact RuntimeContent ownership;
- one Presentation definition may materialize only once per exact owner;
- materialized Rig starts physically non-participating until normal CameraRequest arbitration selects it;
- GameApplication exposes optional Session Camera Presentations as the first product proof surface;
- FrameworkRuntimeHost materializes Session Presentations under the persistent runtime host transform, resolves the exact existing Session Output, attaches current Camera Subject availability and releases the occurrence before Output teardown;
- current Persistent Content Output topology is intentionally retained for CUT B and is removed only by CAMERA-032-D;
- package-local implementation tests cover definition validation, RuntimeContent owner identity, duplicate occurrence rejection and idempotent release.

Manual composition remains consumer-owned:

~~~text
Rig prefab
  -> CameraRigComposer
  -> CameraRigBehaviorDefinition
  -> Apply/Rebuild materialized CinemachineCamera

CameraPresentationDefinition
  -> exact Output Definition
  -> Rig Prefab
  -> Subject Policy
  -> Request Precedence

GameApplication
  -> Session Presentations[]
  -> CameraPresentationDefinition
~~~

Validation state at initial CUT B delivery:

~~~text
Static code review        PASS
Unity import/compile      NOT RUN in the initial CUT B slice
Manual Presentation proof NOT RUN in the initial CUT B slice
QAFramework               NOT RUN in the initial CUT B slice
~~~

Subsequent IF-ADR-032 consumer runs proved Presentation materialization, request participation and lifecycle release. CAMERA-032-B is therefore consumer-proven; the block above is retained as the historical state at initial CUT B delivery.

### CAMERA-032-C — Route / Activity lifecycle integration

Implementation state: **IMPLEMENTED / FOCUSED QA VALIDATED**

Implemented:

- optional Camera Presentation arrays on RouteAsset and ActivityAsset;
- Route/Activity Inspectors expose lifecycle-owned Presentations directly;
- authoring validation rejects missing, invalid or duplicate Presentation definitions per owner;
- materialization uses the exact existing Route/Activity RuntimeContent scope context;
- CameraPresentationRuntime derives normal CameraRequest owner/lifetime from that scope:
  - Session -> Session;
  - Route -> Route;
  - Activity -> Activity;
- RouteCameraRequestPublisher and ActivityCameraRequestPublisher are used for migrated occurrences;
- lifecycle release runs before the corresponding RuntimeContent scope root is removed;
- empty Activity Presentation configuration naturally restores the surviving Route/Session request or Default through CameraOutputContext arbitration;
- no separate Camera winner hierarchy was introduced.

Consumer evidence recorded on 2026-09-22:

- the Local Multiplayer Route materialized `CameraPresentation_Default_Route` under the Route scope against the explicit Session Output, with authored precedence 200 and Cut entry;
- the Local Multiplayer Activity materialized `CameraPresentation_LocalMultiplayer` under the Activity scope against the same Output, with authored precedence 300 and Blend entry;
- manual Play Mode confirmed the Route entry presentation and the Activity-owned shared Group presentation are functional in the migrated IF-ADR-032 topology;
- this proves Route and Activity Presentation participation in one shared-Output consumer, but does not close the restoration/transition matrix below.

Evidence:

- [IF-ADR-032 Local Multiplayer Group Consumer Proof — 2026-09-22](../Reconciliation/IF-ADR-032-LOCAL-MULTIPLAYER-GROUP-CONSUMER-PROOF-2026-09-22.md)

Remaining broader consumer proof:

- Startup HUB with no Session/Route Presentation -> Default remains a separate consumer scenario;
- the broader Basic Flow A/B/C restoration matrix remains a separate consumer proof.

The focused QA below closes the technical restoration/transition cases for CAMERA-032-C; those broader consumer scenarios are no longer blockers for the cut itself.

Focused QA authored on 2026-09-23:

~~~text
Immersive Framework > QA > Camera >
Run CAMERA-032 Game Flow Lifecycle Certification
~~~

The focused certification runs two fresh current-architecture boots. Activity clear independently proves Activity -> Route restoration. Force-default continuity is certified on Route replacement, where actual primary-scene unload/load provides a covered multi-frame physical lifecycle: normal requests remain intact while Default is forced, the outgoing primary scene is retired, and release restores the current lower-scope winner. The Session-fallback boot restores Session; the Default-fallback boot has no Session Presentation and restores the physical Output Default. Each boot owns 8 ordered cases (16 total).

Unity execution on 2026-09-23 passed both boots and canonical cleanup:

~~~text
Session fallback     8/8 PASS
Default fallback     8/8 PASS
--------------------------------
Focused total       16/16 PASS
Canonical restore          PASS
Terminal verdict           CAMERA_032_GAMEFLOW_LIFECYCLE_CERTIFIED
~~~

The certification uses generated temporary Route primary scenes containing no scene-owned Camera authority, so outgoing gameplay scene Camera survival cannot satisfy the proof accidentally. The canonical Shared Camera QA baseline is rebuilt and verified after the focused run.

Evidence:

- [IF-ADR-032-C Game Flow Lifecycle Focused Validation — 2026-09-23](../Reconciliation/IF-ADR-032-C-GAME-FLOW-LIFECYCLE-FOCUSED-VALIDATION-2026-09-23.md)

### CAMERA-032-D — Camera Session configuration — IMPLEMENTED / FOCUSED QA + CONSUMER VALIDATED

Implemented:

- `GameApplicationAsset` owns an explicit inline `CameraSessionConfiguration`;
- Camera Session declares 1..N physical Output prefabs and optional Player Slot -> Output bindings;
- each Output prefab is validated as one self-contained physical hierarchy with exactly one `CameraOutputAuthoring`, exact `CameraOutputDefinition`, Unity Camera, CinemachineBrain and persistent Default Rig;
- `CameraSessionOutputMaterializationRuntime` instantiates the configured Output prefabs under the persistent FrameworkRuntimeHost and builds the existing `CameraOutputSessionTopology`;
- Output arbitration, Default semantics, force-default and rollback remain in the existing Output runtime rather than being reimplemented by the materializer;
- Player Slot -> Output projection now consumes the GameApplication Camera Session bindings directly;
- Persistent Content is no longer used to discover physical Outputs or `PlayerCameraOutputPolicyAuthoring`;
- runtime and authoring validation reject transitional `CameraOutputAuthoring` / `PlayerCameraOutputPolicyAuthoring` left in Persistent Content, preventing dual Camera Session topologies;
- `PlayerInputManager` remains the split-layout authority and may remain in Persistent Content / Player provisioning;
- Session Output teardown is owned by FrameworkRuntimeHost after Presentation/request integrations release;
- each materialized physical Output occurrence receives one exclusive runtime-only Cinemachine Output channel (`Default`, then `Channel01`..`Channel15`);
- the Output's `CinemachineBrain.ChannelMask` and persistent Default Rig `CinemachineCamera.OutputChannel` are configured to that same exclusive channel before activation;
- every materialized Camera Presentation occurrence inherits the exclusive channel of its exact target Output before Player selection attachment and request participation;
- Player Slot -> Output binding does not erase Session Output/Default continuity: while no Player Output association exists, configured Player-bound Outputs remain physically available so their persistent Default or surviving Session/Route Presentation can render without gameplay Subjects;
- once at least one exact Player Output association exists, physical participation for Player-bound Outputs follows current association: associated Outputs render, unassociated Player-bound Outputs are disabled so they cannot cover another Player's viewport; unbound Session Outputs are not governed by this Player adapter;
- when the last Player Output association releases, Player-bound Outputs return to Session continuity mode instead of remaining dormant;
- PlayerInputManager split-screen is treated as an authored capability rather than a permanently-on runtime state: zero/one associated Player Output keeps automatic split-screen off/full-screen, two or more associated Player Outputs enable it, and Camera never writes `Camera.rect` directly;
- PlayerInputManager recomposition is never forced through a synthetic `false -> true -> false` toggle: enabling split-screen calls Unity Input System `UpdateSplitScreen()`, which requires every still-registered `PlayerInput` to already have a Camera; Leave therefore uses only the real `true -> false` transition when the associated count falls below two;
- Join remains bracketed until the exact PlayerInput -> Camera association exists; Leave/release removes the association, disables only an unassociated Player Output while another Player association survives, and asks PlayerInputManager to restore the surviving camera to full-screen;
- Player Slot -> Output bindings require exclusive physical Output ownership: one physical Output cannot be assigned to multiple Player Slots in the same Session;
- the channel is occurrence routing state only; it is not serialized into `CameraOutputDefinition` or `CameraPresentationDefinition`, so one reusable Rig prefab remains valid across multiple Outputs;
- Camera Session validation rejects more than 16 simultaneous physical Outputs because Cinemachine 3 exposes 16 distinct Output channels;
- package-local focused tests cover Camera Session configuration validation, exact Output materialization/teardown, direct Player binding projection and two-Output Cinemachine channel isolation.

Consumer migration required for Unity proof:

~~~text
Existing Persistent Content Output hierarchy
  -> make/maintain as a prefab containing CameraOutputAuthoring
  -> assign GameApplication > Camera > Session Configuration > Output Prefabs
  -> remove the physical Output hierarchy from Persistent Content

Existing PlayerCameraOutputPolicyAuthoring
  -> move each Player Slot -> Output binding
     to GameApplication > Camera > Session Configuration
  -> remove the component from Persistent Content
~~~

Validation state:

~~~text
Static code review                          PASS
Unity import/compile                        PASS
Local Multiplayer explicit Output prefab    PASS — one Output consumer proof
Session Output materialization diagnostic   PASS — outputCount=1 consumer proof
shared-output Route/Activity integration     PASS — consumer proof
two-Output Session materialization          PASS — focused QA
two-Output Cinemachine channel isolation    PASS — focused QA
zero-Player Default continuity              PASS — focused QA
0 -> 1 -> 2 -> 1 -> 2 -> 0 lifecycle       PASS — focused QA 14/14
2 -> 1 split-screen recomposition           PASS — focused QA / no PlayerInput camera error observed
transition force-default continuity         QA AUTHORED — execution pending
QAFramework                                 PASS — focused Player Output lifecycle 14/14
~~~

The 2026-09-22 Local Multiplayer consumer proves that GameApplication Camera Session configuration can materialize the sample's one explicit physical Output and support migrated Route/Activity Presentations.

The focused QA run on 2026-09-23 closes the independent two-Output Player lifecycle slice. It materialized two explicit Session Outputs, verified exclusive Cinemachine channels, preserved Default continuity with zero Players, projected P1/P2 to exact Outputs, exercised independent ThirdPerson ExplicitSelection, recomposed split-screen through 2 -> 1 -> 2, restored Default continuity after the final Leave, and restored the canonical QA baseline. The captured run completed 14/14 cases and contained no `Player has no camera associated with it` Input System error.

Evidence:

- [IF-ADR-032 Local Multiplayer Group Consumer Proof — 2026-09-22](../Reconciliation/IF-ADR-032-LOCAL-MULTIPLAYER-GROUP-CONSUMER-PROOF-2026-09-22.md)
- [IF-ADR-032 Two-Output Player Lifecycle Focused QA — 2026-09-23](../Reconciliation/IF-ADR-032-TWO-OUTPUT-PLAYER-LIFECYCLE-FOCUSED-QA-2026-09-23.md)

The Persistent Content template asset itself is intentionally left for CAMERA-032-F template/sample cleanup; CAMERA-032-D changes the runtime and validation authority now and does not silently rewrite consumer scenes or prefabs.

### CAMERA-032-E — Player explicit selection migration — IMPLEMENTED / FOCUSED QA VALIDATED

Implemented:

- Camera Session configuration now exposes optional explicit Player Slot -> Camera Presentation bindings alongside the separate Player Slot -> Output bindings;
- serialized Player selection authoring references reusable `CameraPresentationDefinition` assets, never scene `CameraSharedComposition` occurrences;
- one Player Slot may supply explicit selection to multiple Presentation definitions across Session / Route / Activity lifecycles;
- one Presentation definition may be bound to at most one Player Slot, preventing P1/P2 selection aliasing;
- bound Presentations must use `ExplicitSelection`;
- authoring validation requires every Player Presentation binding to have an explicit Player Slot -> Output binding and requires exact Output coherence:
  - Slot -> Output Definition;
  - Slot -> Presentation Definition -> Output Definition;
  - both Output references must be the same authored definition;
- `PlayerCameraPresentationSelectionRuntime` consumes current Player Actor Camera Subject evidence and attaches a Camera-domain selection context to each matching live `CameraPresentationRuntime` occurrence;
- Session, Route and Activity Presentation materialization all attach Player selection before enabling the occurrence;
- Subject change, leave and rejoin update only live Presentation occurrences bound to that exact Player Slot;
- release forgets the exact occurrence without keeping a stale materialized Presentation reference;
- Persistent Content `PlayerCameraCompositionPolicyAuthoring` is rejected by the 032-E runtime/validator path and remains only as legacy code pending CAMERA-032-F removal;
- package-local tests cover Output/Presentation coherence, one-Presentation/one-Player ownership, ExplicitSelection enforcement, P1/P2 isolation and fresh Subject identity after rejoin.

Diagnostics added in this cut for CAMERA-032-D evidence closure:

- FrameworkRuntimeHost emits `Camera Session Outputs materialized.` with explicit Output count and IDs;
- SessionCameraTransitionOrchestrator emits `Camera transition force-default applied.` and `Camera transition force-default released.`;
- force-default diagnostics include Output identity, force-default owner state, admitted normal-request count and preserved normal winner;
- these diagnostics make Default continuity auditable without treating force-default as a normal request.

Focused Player proof completed:

~~~text
Unity import/compile                                      PASS
GameApplication Player Slot -> Output bindings           PASS
GameApplication Player Slot -> Presentation bindings     PASS
one Player ThirdPerson ExplicitSelection                 PASS
two explicit Outputs + independent P1/P2 ThirdPerson    PASS
P1/P2 selected Subject isolation                         PASS
P2 Leave restores P1 full-screen                         PASS
P2 rejoin uses a fresh PlayerInput host                  PASS
zero-Player Default continuity after terminal Leave      PASS
~~~

Still required outside this focused slice:

~~~text
explicit fresh Camera Subject identity assertion after rejoin/replacement
force-default apply/release continuity diagnostics
Route/Activity restoration and Route replacement matrix
stale-evidence and rollback integrity coverage
~~~

Validation state:

~~~text
Static code review                                      PASS
Package focused implementation tests                    SUPPORTING ONLY
FIRSTGAME non-Player 032-D happy path                    PASS — consumer log
Local Multiplayer shared Group proof                     PASS — does not exercise 032-E
explicit 032-D diagnostic rerun                          PASS — Local Multiplayer outputCount=1
Player ExplicitSelection migration                       PASS — focused QA
two-player independent ThirdPerson proof                 PASS — focused QA
split-screen leave/rejoin lifecycle                      PASS — focused QA
Character Selection Multiplayer Split-Screen consumer    PASS — Unity Play Mode 2026-09-23
terminal application-quit Camera teardown                PASS — consumer rerun 2026-09-23
QAFramework                                              PASS — focused 14/14
~~~

The Local Multiplayer proof intentionally uses `AllAvailableSubjects` on one shared Activity Presentation. It validates the shared-Group consumer shape retained by IF-ADR-032, but it does not validate CAMERA-032-E Player Slot -> Presentation `ExplicitSelection` bindings.

The 2026-09-23 Character Selection Multiplayer Split-Screen consumer exercises the concrete two-Output `ExplicitSelection` product shape with `ActorResolution = LeaveUnresolved`: P1 and P2 join unresolved, make independent Farmer/Cow choices, reach independent ThirdPerson Presentations on exact Outputs, Leave/Rejoin without retaining Actor selection, and return to zero Players cleanly. The consumer also verifies that deferred Session observation removes the transient `RegisteredHost.NotRegistered` ownership diagnostic seen when UI observation occurred synchronously inside the Join mutation.

Evidence:

- [IF-ADR-032 Character Selection Multiplayer Split-Screen Consumer Proof — 2026-09-23](../Reconciliation/IF-ADR-032-CHARACTER-SELECTION-MULTIPLAYER-SPLITSCREEN-CONSUMER-PROOF-2026-09-23.md)

### CAMERA-032-F — Legacy removal and recertification — IMPLEMENTED / UNITY AGGREGATE CERTIFIED

Implemented in source:

- removed the scene `CameraSharedComposition` adapter, Session/Route/Activity Camera overrides, scoped override authoring, Persistent Player Output/Composition policies and the persistent-root Output/Subject injection scan;
- `CameraRequestOwnerKind.Composition` and `CameraRequestLifetimeKind.Composition` stay removed; no current call site needs them;
- `CameraTargetSourceDescriptor` is removed; `CameraRequest` does not carry a target source;
- `CameraPresentationDefinition` remains reusable intent only, and `CameraPresentationRuntime` remains the non-MonoBehaviour occurrence authority;
- the subject-policy and reconcile types keep their existing names because they are the serialized Presentation contract, not the removed scene adapter;
- QA aggregate `Run Full Camera QA` now runs the structural Presentation contract, Subject + Transaction, two-Output Player lifecycle and CAMERA-032-C Game Flow gates;
- historical override, 028-C and 028-D menus are not current certification.

Aggregate Unity recertification was completed on 2026-09-23 after the CAMERA-032-F cleanup and the runtime correction that gates Player Slot -> Output projection behind an enabled Player Session. The current aggregate terminal verdict is `CAMERA_032_FULL_CERTIFIED`.

Recorded aggregate evidence:

~~~text
structural Presentation contract     PASS
Player Output                        PASS
Game Flow                            16/16 PASS
Subject + Transaction                17/17 PASS
legacy product types                 0
Presentation intent                  PASS
canonical baseline restore           PASS
terminal verdict                     CAMERA_032_FULL_CERTIFIED
~~~

Earlier interrupted or failing aggregate runs remain historical diagnostics only; they are not the certification result.

Closure record:

- [CAMERA-032-F Legacy Removal Closure — 2026-09-23](../Reconciliation/CAMERA-032-F-LEGACY-REMOVAL-CLOSURE-2026-09-23.md)

## 28. Acceptance criteria

The migrated architecture is not technically closed until all mandatory cases below are proven.

### Session / Output

~~~text
[x] Session materializes every explicitly configured Output once — two-Output focused QA 2026-09-23
[x] every Output has exact identity, Unity Camera, Brain and Default Rig — two-Output focused QA 2026-09-23
[x] no normal request -> Default — zero-Player Default continuity focused QA 2026-09-23
[x] force-default -> Default without destroying normal requests — CAMERA-032-C focused QA 2026-09-23
[x] release force-default -> current normal winner or Default — CAMERA-032-C focused QA 2026-09-23
[x] Route/Activity never infer Output count from Player count — Session materializes the explicit Output set; Route/Activity only declare Presentations
~~~

### Game Flow

~~~text
[x] Session Presentation can participate — CAMERA-032-B FIRSTGAME consumer proof
[x] Route Presentation can participate — Local Multiplayer consumer proof 2026-09-22
[x] Activity Presentation can participate — Local Multiplayer shared Group consumer proof 2026-09-22
[x] Activity exit restores Route/Session/Default correctly — CAMERA-032-C focused QA 2026-09-23
[x] Route exit restores Session/Default correctly — Session + Default fallback boots 2026-09-23
[x] Route replacement never requires outgoing gameplay Camera survival — generated camera-free Route replacement QA 2026-09-23
~~~

### Subject / Presentation

~~~text
[x] zero/one/many membership transitions are deterministic — Subject + Transaction focused QA 2026-09-23
[x] required-target Presentation releases when no required Subject is current — Subject + Transaction focused QA 2026-09-23
[x] Fixed may present with zero Subjects — Subject + Transaction focused QA 2026-09-23
[x] rejoin/replacement creates fresh Subject occurrence — same-logical-id fresh occurrence focused QA 2026-09-23
[x] stale availability/selection/membership evidence is rejected — Subject + Transaction focused QA 2026-09-23
[x] stale rollback cannot overwrite newer Presentation state — Presentation + membership stale rollback focused QA 2026-09-23
~~~

Consumer evidence note: the 2026-09-22 Local Multiplayer sample proves the practical one-Subject -> two-Subject Group path with `AllAvailableSubjects`, plus Presentation-authored `ActorCameraSubjectAuthoring.FramingRadius` affecting Group framing. The full zero/one/many, stale-evidence and rejoin criteria remain reserved for focused QA rather than being closed from this consumer proof alone.

Focused QA authored on 2026-09-23:

~~~text
Immersive Framework > QA > Camera >
Run CAMERA-032 Subject + Transaction Certification
~~~

The focused Edit Mode gate executes the current Presentation runtime contract
through the existing transitional Unity adapter and owns 9 Subject/stale cases:
deterministic `0 -> 1 -> many -> 1 -> 0`, required-target release, Fixed with
zero Subjects, fresh occurrence after same-logical-id rejoin, stale availability,
stale explicit selection, stale membership token, stale Presentation rollback and
stale membership rollback. It uses only transient preview scenes and does not
mutate the canonical QA baseline.

Unity execution on 2026-09-23 passed all Subject/stale cases:

~~~text
Subject / stale evidence     9/9 PASS
~~~

The terminal focused gate also completed the transaction half for a combined
17/17 PASS. The Subject / Presentation acceptance criteria above are therefore
technically closed.

Evidence:

- [IF-ADR-032 Subject + Transaction Focused Validation — 2026-09-23](../Reconciliation/IF-ADR-032-SUBJECT-TRANSACTION-FOCUSED-VALIDATION-2026-09-23.md)

### Multiplayer

~~~text
[x] two explicit Outputs may coexist in one Session — focused QA 2026-09-23
[x] P1 maps explicitly to Output P1 — focused QA 2026-09-23
[x] P2 maps explicitly to Output P2 — focused QA 2026-09-23
[x] P1 Presentation consumes only P1 current Subject — focused QA 2026-09-23
[x] P2 Presentation consumes only P2 current Subject — focused QA 2026-09-23
[x] leave/rejoin does not reuse stale Subject occurrence — 032-E package rejoin identity tests + fresh same-logical-id occurrence focused QA 2026-09-23
[x] PlayerInputManager remains Camera.rect writer — focused split recomposition proof + implementation boundary 2026-09-23
~~~

### Authoring

~~~text
[x] Persistent Content is not required to contain gameplay Camera composition — CAMERA-032-C focused QA uses GameApplication Camera Session with Persistent fixture free of legacy Camera authority 2026-09-23
[x] Route/Activity camera intent is visible on the owning asset surface — Local Multiplayer consumer proof
[x] Presentation definition contains reusable intent only — serialized fields are stable id, description, Output definition, Rig prefab, transition, subject policy and request precedence
[x] runtime state never lives in the ScriptableObject — occurrence state stays on CameraPresentationRuntime
[x] no Camera.main / name / hierarchy fallback exists — static source search of the Camera runtime found no Camera.main, GameObject.Find or FindObject authority
~~~

### Transaction integrity

~~~text
[x] failed Rig application preserves previous coherent state — Subject + Transaction focused QA 2026-09-23
[x] failed request admission rolls back Presentation state — Subject + Transaction focused QA 2026-09-23
[x] failed request release preserves recoverable previous state — Subject + Transaction focused QA 2026-09-23
[x] rollback failure is explicit terminal evidence — CriticalRollbackFailure focused QA 2026-09-23
[x] owner-safe release cannot tear down another scope's Presentation — dual-Presentation owner-safe release focused QA 2026-09-23
~~~

The same focused CAMERA-032 Subject + Transaction certification owns 8
transaction cases: Rig apply failure, unsupported target projection preserving
the previous coherent state, request-admission rollback, force-default continuity
across failed normal admission, request-release rollback, retry after successful
rollback, explicit critical rollback failure and owner-safe release between two
live Presentation occurrences.

The QA uses existing Camera runtime seams plus narrowly scoped test reflection for
internal rollback evidence/fault injection. No new production fault-injection
API or Camera architecture boundary is introduced.

Unity execution on 2026-09-23 passed all transaction cases:

~~~text
Transaction integrity        8/8 PASS
Subject / stale evidence     9/9 PASS
-------------------------------------
Focused total               17/17 PASS
Terminal verdict            CAMERA_032_SUBJECT_TRANSACTION_CERTIFIED
Cleanup                     TransientPreviewScenesClosed
~~~

The transaction acceptance criteria above are therefore technically closed.

Evidence:

- [IF-ADR-032 Subject + Transaction Focused Validation — 2026-09-23](../Reconciliation/IF-ADR-032-SUBJECT-TRANSACTION-FOCUSED-VALIDATION-2026-09-23.md)

## 29. Consequences

Positive:

- one normative Camera architecture instead of a chain of partially superseded decisions;
- physical Session capacity is separate from gameplay presentation intent;
- reusable Camera authoring no longer requires parking gameplay Rigs in scenes;
- Route and Activity camera intent aligns with the Game Flow assets that own those lifecycles;
- RuntimeContent is reused instead of creating another Camera lifecycle manager;
- Player integration remains explicit without leaking Player identity into Camera core;
- existing proven transaction and stale-evidence semantics are retained.

Tradeoffs:

- current Camera authoring and QA require migration;
- existing certified Camera results become historical evidence for the former boundary;
- Stable product surfaces touched by the migration require deliberate inspector/validation updates;
- the target architecture introduces new reusable authoring types and therefore must stay small and evidence-driven.

## 30. Final rule

The Camera domain should be understandable from the following model alone:

~~~text
Session owns physical Camera capacity and Defaults.

Session / Route / Activity declare reusable Camera Presentations.

A Presentation occurrence selects current Camera Subjects,
applies one materialized Rig and publishes one normal CameraRequest.

CameraOutputContext is the only normal winner authority.

CameraOutputSession transactionally projects the winner or Default.

PlayerInputManager owns physical split-screen layout.

Scenes provide world content and Camera Subject evidence;
they are not the normal Game Flow Camera configuration authority.
~~~
