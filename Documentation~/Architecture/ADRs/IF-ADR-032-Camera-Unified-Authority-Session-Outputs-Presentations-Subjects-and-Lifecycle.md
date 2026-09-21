# IF-ADR-032 — Camera Unified Authority, Session Outputs, Presentations, Subjects and Lifecycle

Status: **Accepted target architecture / CAMERA-032-A technically validated / migration continues**  
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

### CAMERA-032-B — Presentation definition and Rig prefab

- introduce CameraPresentationDefinition or equivalent reusable authored recipe;
- reference a materialized Rig prefab rather than a scene Rig instance;
- materialize/release through RuntimeContent;
- prove one Presentation against the existing Output baseline.

### CAMERA-032-C — Route / Activity lifecycle integration

- add optional Camera Presentation configuration to RouteAsset and ActivityAsset;
- materialize through exact Route/Activity RuntimeContent scopes;
- release on exact lifecycle exit;
- prove Activity -> Route restoration and Route replacement.

### CAMERA-032-D — Camera Session configuration

- introduce explicit Session Camera configuration on GameApplication;
- materialize 1..N Output prefabs at Session boot;
- move Player Slot -> Output configuration out of Persistent scene authoring;
- remove target dependency on Persistent Camera roots;
- prove transition force-default continuity.

### CAMERA-032-E — Player explicit selection migration

- bind Player Slot current Subject evidence to live Presentation occurrences;
- remove direct serialized Player -> CameraSharedComposition references;
- validate Slot -> Output and Slot -> Presentation -> Output coherence;
- prove two-player independent ThirdPerson Presentations.

### CAMERA-032-F — Legacy removal and recertification

- remove obsolete overrides/policies/injection paths that no longer serve a retained use case;
- remove Composition request owner/lifetime if fully redundant;
- remove or reduce CameraTargetSourceDescriptor from CameraRequest if unused;
- update validators, inspectors, templates, samples and usage docs;
- migrate QA to IF-ADR-032 contracts;
- execute Unity/QA consumer proof and publish a new technical certification.

## 28. Acceptance criteria

The migrated architecture is not technically closed until all mandatory cases below are proven.

### Session / Output

~~~text
[ ] Session materializes every explicitly configured Output once
[ ] every Output has exact identity, Unity Camera, Brain and Default Rig
[ ] no normal request -> Default
[ ] force-default -> Default without destroying normal requests
[ ] release force-default -> current normal winner or Default
[ ] Route/Activity never infer Output count from Player count
~~~

### Game Flow

~~~text
[ ] Session Presentation can participate
[ ] Route Presentation can participate
[ ] Activity Presentation can participate
[ ] Activity exit restores Route/Session/Default correctly
[ ] Route exit restores Session/Default correctly
[ ] Route replacement never requires outgoing gameplay Camera survival
~~~

### Subject / Presentation

~~~text
[ ] zero/one/many membership transitions are deterministic
[ ] required-target Presentation releases when no required Subject is current
[ ] Fixed may present with zero Subjects
[ ] rejoin/replacement creates fresh Subject occurrence
[ ] stale availability/selection/membership evidence is rejected
[ ] stale rollback cannot overwrite newer Presentation state
~~~

### Multiplayer

~~~text
[ ] two explicit Outputs may coexist in one Session
[ ] P1 maps explicitly to Output P1
[ ] P2 maps explicitly to Output P2
[ ] P1 Presentation consumes only P1 current Subject
[ ] P2 Presentation consumes only P2 current Subject
[ ] leave/rejoin does not reuse stale Subject occurrence
[ ] PlayerInputManager remains Camera.rect writer
~~~

### Authoring

~~~text
[ ] Persistent Content is not required to contain gameplay Camera composition
[ ] Route/Activity camera intent is visible on the owning asset surface
[ ] Presentation definition contains reusable intent only
[ ] runtime state never lives in the ScriptableObject
[ ] no Camera.main / name / hierarchy fallback exists
~~~

### Transaction integrity

~~~text
[ ] failed Rig application preserves previous coherent state
[ ] failed request admission rolls back Presentation state
[ ] failed request release preserves recoverable previous state
[ ] rollback failure is explicit terminal evidence
[ ] owner-safe release cannot tear down another scope's Presentation
~~~

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
