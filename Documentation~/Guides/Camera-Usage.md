# Camera Usage

Status: **IF-ADR-032 current architecture / CAMERA-032-A..E focused evidence retained / CAMERA-032-F legacy removal implemented / aggregate Unity recertification pending**
Last updated: **2026-09-23**

Normative Camera authority:

- [IF-ADR-032 — Camera Unified Authority, Session Outputs, Presentations, Subjects and Lifecycle](../Architecture/ADRs/IF-ADR-032-Camera-Unified-Authority-Session-Outputs-Presentations-Subjects-and-Lifecycle.md)

Historical Camera reconciliation/certification records remain dated evidence only. They do not override IF-ADR-032 and do not certify the migrated IF-ADR-032 implementation.

## Target architecture

~~~text
GameApplication / Session
  -> explicit physical Camera Outputs + Defaults

Session / Route / Activity
  -> CameraPresentationDefinition
  -> CameraPresentationRuntime
  -> materialized CameraRigComposer
  -> CameraRequest
  -> CameraOutputSession
  -> Camera Output
~~~

The main boundaries are:

| Owner | Responsibility |
|---|---|
| Camera Session configuration | Declare explicit Session Output capacity and optional Player Slot -> Output bindings |
| Camera Presentation Definition | Reusable authored Rig/output/Subject-selection/request intent |
| Camera Presentation Runtime | One live occurrence: membership, presentation application, request publication/release and rollback |
| Camera Subject | Exact observable runtime evidence with occurrence safety and optional framing radius |
| CameraRigComposer | One local materialized Cinemachine Rig and supported presentation behavior |
| CameraRequest | Participate in one explicit Output |
| CameraOutputContext | Deterministically select the normal request winner |
| CameraOutputSession | Transactionally apply winner/Default and own force-default state |
| Camera Output | Unity Camera, CinemachineBrain and persistent Default Rig |
| PlayerInputManager | Own Unity automatic split-screen count and Camera.rect recomposition |

## Session Outputs

The target application declares 1..N physical Outputs as Session capacity.

Each Output requires:

~~~text
CameraOutputDefinition
Unity Camera
CinemachineBrain
Default Camera Rig
~~~

Output count is explicit and is never inferred from Player count.

Route and Activity do not create physical Outputs.

The Default Rig is not a request and has no precedence.

Selection remains:

~~~text
force-default
  -> Default

otherwise normal request winner
  -> winner Rig

otherwise
  -> Default
~~~

## Camera Presentations

Normal gameplay Camera authoring moves away from scene-owned CameraSharedComposition components.

A reusable Camera Presentation is target authoring intent similar to:

~~~text
CameraPresentationDefinition
  Rig Prefab
  Output Definition
  Subject Selection Policy
  Request Policy
~~~

The definition contains no mutable runtime state.

A live CameraPresentationRuntime occurrence owns current Subject membership, Rig application, request publication/release and rollback.

Session, Route and Activity may declare optional Camera Presentations.

## CAMERA-032-B manual authoring proof

CUT B intentionally does not create assets or prefabs automatically.

Create the first proof manually in Unity:

~~~text
1. Create or duplicate a Camera Rig prefab.
2. Keep exactly one CameraRigComposer in that prefab.
3. Assign the desired CameraRigBehaviorDefinition.
4. Run Apply/Rebuild so the prefab already contains the materialized CinemachineCamera.
5. Do not put CameraOutputAuthoring or CameraSharedComposition in the Rig prefab.

6. Create:
   Create > Immersive Framework > Camera > Camera Presentation

7. In CameraPresentationDefinition:
   Generate Stable Identity
   Output Definition = exact existing CameraOutputDefinition
   Rig Prefab = prefab from steps 1-5
   Transition Mode = Blend or Cut
   Subject Policy = AllAvailableSubjects for the first proof
   Request Precedence = explicit value appropriate for the proof topology

   Blend preserves the Camera Output Brain's authored blend/custom-blend policy.
   Cut enters this Presentation immediately without visual interpolation.

8. Open the active GameApplication.
9. Camera > Session Presentations:
   add the CameraPresentationDefinition.

10. Keep the current physical Camera Output in Persistent Content for CUT B.
    Do not move/delete it yet; physical Output migration belongs to CAMERA-032-D.
~~~

Expected boot behavior:

~~~text
GameApplication
  -> Session Presentation definition
  -> Session RuntimeContent scope
  -> instantiate Rig prefab under FrameworkRuntimeHost
  -> resolve exact existing Camera Output
  -> attach Camera Subject availability
  -> CameraPresentationRuntime
  -> normal CameraRequest
  -> CameraOutputSession arbitration
~~~

The materialized Rig's CinemachineCamera is disabled before participation and is enabled only when its CameraRequest becomes the Output winner.

For the first proof, prefer a topology with no competing legacy normal Camera request. The Output Default is fine and should be replaced by the Presentation when its request becomes eligible, then restored when the Presentation is released.

### What CUT B does not change

~~~text
Persistent physical Output topology
Player Slot -> Output policy
RouteAsset Camera authoring
ActivityAsset Camera authoring
Player -> Presentation explicit selection adapter
Camera.rect / split-screen ownership
~~~

Those belong to later CAMERA-032 cuts.

## CAMERA-032-C Route and Activity Presentations

RouteAsset and ActivityAsset now expose optional Camera Presentations.

A practical precedence convention for one Output is:

~~~text
Session Presentation   100
Route Presentation     200
Activity Presentation  300
~~~

These values are authoring conventions, not a hidden hierarchy. CameraOutputContext still selects the winner using ordinary CameraRequest precedence and deterministic tie-break evidence.

Leaving a lifecycle list empty means that lifecycle contributes no CameraRequest:

~~~text
Startup HUB
  Route Presentations = []
  -> surviving Session request or Output Default

Gameplay Route
  Route Presentations = [Route Presentation]
  -> Route request participates

Activity A
  Activity Presentations = [Activity A Presentation]
  -> Activity request may win by authored precedence

Activity C
  Activity Presentations = []
  -> no Activity request; the surviving Route request becomes visible again
~~~

Every materialized occurrence uses its exact RuntimeContent owner. Activity Presentations release before the Activity scope root is removed; Route Presentations release before the Route scope root is removed.

## CAMERA-032-D Camera Session configuration

Physical Camera Outputs are now authored from the active GameApplication rather than discovered in Persistent Content.

Author one self-contained Output prefab per physical Camera:

~~~text
Camera Output Prefab
  CameraOutputAuthoring
    -> exact CameraOutputDefinition
    -> Unity Camera
    -> CinemachineBrain
    -> persistent Default Camera Rig
~~~

Then configure:

~~~text
GameApplication
  Camera
    Session Configuration
      Output Prefabs
        [0] PF_<Game>_CameraOutput_Main
        [1] PF_<Game>_CameraOutput_P2   (only when explicitly required)

      Player Output Bindings
        Player Slot 1 -> exact Output Definition 1
        Player Slot 2 -> exact Output Definition 2
~~~

The Framework materializes these prefabs under the persistent FrameworkRuntimeHost, validates the exact 1..N topology, and continues to use the existing CameraOutputSession arbitration/Default/force-default contracts.

For multi-Output Cinemachine isolation, each materialized Output occurrence receives one exclusive runtime-only Cinemachine Output channel. The Output's `CinemachineBrain.ChannelMask`, persistent Default Rig and every materialized Presentation occurrence targeting that Output use the same channel. Channel identity is occurrence routing state; it is not serialized into Presentation assets and does not require P1/P2-specific copies of the same Rig prefab.

For Player-bound Outputs, physical participation follows this lifecycle:

~~~text
0 associated Players
  -> Session Outputs remain physically available
  -> Default / Route / Session continuity remains visible
  -> automatic split-screen off

1 associated Player
  -> associated Player Output renders full-screen
  -> other Player-bound Outputs are physically dormant

2+ associated Player Outputs
  -> associated Outputs render
  -> PlayerInputManager enables/recomposes split-screen

2 -> 1 Leave
  -> departing Output becomes dormant
  -> PlayerInputManager performs the real true -> false transition
  -> surviving Player returns full-screen

1 -> 0 final Leave
  -> Player-bound Outputs return to Session continuity mode
  -> Default / surviving Session or Route presentation can render again
~~~

The Framework does not force a synthetic `false -> true -> false` split-screen toggle. Enabling Unity split-screen requires every still-registered `PlayerInput` to already have an associated Camera, so Leave recomposition uses only the real state transition and never writes `Camera.rect` directly.

### Migrating an existing Persistent Content scene

For each old physical Output hierarchy:

1. Preserve it as a prefab containing exactly one `CameraOutputAuthoring` and its physical Camera/Brain/Default Rig.
2. Add that prefab to `GameApplication > Camera > Session Configuration > Output Prefabs`.
3. Move every `Player Slot -> Output` binding from `PlayerCameraOutputPolicyAuthoring` into the GameApplication Camera Session configuration.
4. Remove `CameraOutputAuthoring` and `PlayerCameraOutputPolicyAuthoring` from Persistent Content.
5. Keep `PlayerInputManager` / Player provisioning where it belongs; automatic split-screen still owns `PlayerInput.camera` association and `Camera.rect` recomposition through the existing integration.
6. Keep Route/Activity Presentation prefabs and definitions on their lifecycle assets; they are not part of the Output prefab.

Leaving the old Output or Player Output policy in Persistent Content is rejected. This is intentional: CAMERA-032-D does not permit a legacy physical topology and the new Session topology to coexist.

The package Persistent Content template/sample cleanup remains CAMERA-032-F work; the framework does not silently rewrite consumer scenes or prefabs.

## Rig authoring

CameraRigComposer remains the local Rig authority.

Supported behavior family:

~~~text
Follow      = 10
Fixed       = 20
Mounted     = 30
ThirdPerson = 40
Group       = 50
~~~

CameraRigBehaviorDefinition contains reusable behavior tuning.

Apply/Rebuild remains Editor-owned and materializes supported Cinemachine structure before runtime.

The target runtime instantiates a prefab that already contains the materialized Rig; it does not build an arbitrary Cinemachine graph at runtime.

## Camera Subjects

Actor Camera Subjects continue to publish:

~~~text
Camera Subject
  identity
  Observation Transform
  optional Framing Radius
~~~

Framing Radius = 0 means unspecified.

Subject occurrence, availability revision and stale-token protections remain mandatory.

Selection modes remain:

~~~text
AllAvailableSubjects
ExplicitSelection
~~~

Camera core does not interpret PlayerSlotId, Player index, ActorProfile, GameObject name or hierarchy.

## Shared-output local multiplayer Group presentation

A local multiplayer game does not require one physical Camera Output or one Camera Presentation per Player when all Players share the same view.

For a shared-screen Group Camera, author the topology as:

~~~text
GameApplication
  Camera
    Session Configuration
      Output Prefabs
        PF_CameraOutput_Main

RouteAsset
  Camera Presentations
    CameraPresentation_Default_Route

ActivityAsset
  Camera Presentations
    CameraPresentation_LocalMultiplayer
      Output = CameraOutput_Main
      Rig Prefab = PF_LocalMultiplayer_Activity_Presentation
      Subject Policy = AllAvailableSubjects
      Request Precedence = 300

PF_LocalMultiplayer_Activity_Presentation
  CameraRigComposer
    Behavior = GroupCameraRigBehaviorDefinition
~~~

Do not add Player Output bindings or Player Presentation bindings merely because two local Players exist. Those bindings are only required when physical Output ownership or `ExplicitSelection` is Player-specific. A shared Group Presentation deliberately consumes all currently available Camera Subjects.

Each Actor Presentation supplies its own Camera evidence:

~~~text
Actor Presentation root
  ActorCameraSubjectAuthoring
    Observation Transform = presentation-owned Camera target/anchor
    Framing Radius = authored spatial extent for this Actor
~~~

The Observation Transform may be an empty GameObject. Its transform position identifies the observation point; it does not need Renderer, Collider or scale-derived bounds.

For a Group presentation the runtime projects each Subject into `CinemachineTargetGroup` as:

~~~text
Target.Object = Subject.Observation

Target.Radius =
  Subject.FramingRadius        when Framing Radius > 0
  GroupBehavior.MemberRadius   otherwise
~~~

Therefore it is expected that the materialized `CinemachineCamera.Follow` points to the framework-owned `CinemachineTargetGroup`, not directly to one Actor target. The Target Group object's own transform has no authored physical size; the per-member Radius values provide the framing extent.

Use `GroupCameraRigBehaviorDefinition.MemberRadius` only as a safe fallback. When Actors have materially different dimensions, author `ActorCameraSubjectAuthoring.FramingRadius` per Actor Presentation.

Expected shared-output behavior:

~~~text
zero current Subjects
  -> required Group Presentation is not eligible
  -> lower-precedence Route/Session request or Output Default remains visible

one current Subject
  -> same Activity Group Presentation participates
  -> Target Group contains one member with its authored/fallback radius

two current Subjects
  -> same Activity Group Presentation remains active
  -> Target Group contains both members and reframes the shared view

one Subject leaves
  -> membership shrinks
  -> the same Presentation reframes the remaining Subject
~~~

The Local Multiplayer consumer was manually proven with this shape on 2026-09-22. The proof covers one explicit Session Output, Route + Activity Presentation materialization, one/two Player Group framing, and per-Subject framing radius. It does not replace CAMERA-032-E `ExplicitSelection` or two-Output split-screen validation.

## Player and local multiplayer integration

Two relations remain separate:

~~~text
Player Slot -> Camera Output

Player Slot -> current Camera Subject selection for a Presentation occurrence
~~~

For split-screen:

~~~text
Player Slot -> exact Output
  -> PlayerInput.camera
  -> PlayerInputManager owns Camera.rect
~~~

Camera does not write screen partition layout.

On terminal application shutdown, Unity may invalidate scene-backed Camera objects before Framework teardown callbacks complete. Normal Route / Activity / Session exits still require full request arbitration and rollback semantics; application termination is different because there is no surviving rendered frame. The Framework terminal cleanup path must release ownership/subscriptions best-effort without requiring Default/winner re-application after physical Camera scene validity is already gone.

A two-player Session therefore explicitly declares two Outputs when the game needs two physical Cameras. Player count alone never creates the second Output.

The focused QA certification on 2026-09-23 proved the current two-Output Player lifecycle:

~~~text
0 -> 1 -> 2 -> 1 -> 2 -> 0
~~~

It covered zero-Player Default continuity, exclusive Cinemachine channels, one-Player ThirdPerson full-screen, P1/P2 split-screen isolation, P2 Leave restoring P1 full-screen, fresh P2 PlayerInput host on rejoin, split restoration, terminal Default continuity and canonical QA baseline restoration.

The `planet-devourer` Character Selection Multiplayer Split-Screen consumer adds concrete Unity product evidence for the same topology with explicit Actor choice:

~~~text
ActorResolution = LeaveUnresolved

Join P1
  -> no Actor
  -> choose Farmer/Cow
  -> P1 ThirdPerson -> Output P1

Join P2
  -> no Actor
  -> choose Farmer/Cow
  -> P2 ThirdPerson -> Output P2

Leave / Rejoin
  -> old Actor selection cleared
  -> fresh explicit choice required
  -> split-screen recomposes through PlayerInputManager

final Leave
  -> 0 Players
  -> no retained Actor occurrence
~~~

This consumer proof is recorded in [IF-ADR-032 Character Selection Multiplayer Split-Screen Consumer Proof — 2026-09-23](../Architecture/Reconciliation/IF-ADR-032-CHARACTER-SELECTION-MULTIPLAYER-SPLITSCREEN-CONSUMER-PROOF-2026-09-23.md).

## CAMERA-032-E Player explicit Presentation selection

Player Camera integration keeps two explicit relations:

~~~text
Player Slot -> physical Camera Output

Player Slot -> current Camera Subject selection
               for a Camera Presentation occurrence
~~~

Author both from the active GameApplication Camera Session:

~~~text
GameApplication
  Camera
    Session Configuration
      Player Output Bindings
        P1 -> Output P1
        P2 -> Output P2

      Player Presentation Bindings
        P1 -> Presentation P1
        P2 -> Presentation P2
~~~

A Player Presentation binding references a reusable `CameraPresentationDefinition`, not a scene object.

Required rules:

- the Presentation must use `ExplicitSelection`;
- the Player Slot must also have an explicit Output binding;
- the Presentation's Output Definition must be the exact same Output Definition bound to that Player Slot;
- one Presentation definition may belong to only one Player Slot;
- one Player Slot may bind several Presentation definitions when different Session / Route / Activity lifecycles need Player-specific Presentations.

At runtime the Framework waits for a matching Presentation definition to materialize. It then attaches the Player Slot's current Camera Subject evidence to that exact live occurrence. Route/Activity replacement releases the old occurrence; a later occurrence receives a new attachment.

Leave/rejoin or Actor replacement never restores the old Camera Subject occurrence. The Player Actor integration publishes fresh Camera-domain Subject identity and only the matching Slot's live Presentations consume it.

`PlayerCameraCompositionPolicyAuthoring` is not a current authoring type. Player Slot -> Presentation selection is authored on the GameApplication Camera Session.

### 032-D/E camera diagnostics

Expected debug evidence now includes:

~~~text
Camera Session Outputs materialized.
  outputCount=<N>
  outputs=<explicit Output IDs>

Camera transition force-default applied.
  output=<Output ID>
  forceDefaultActive=True
  normalRequestCount=<preserved request count>
  normalWinner=<preserved normal request or none>

Camera transition force-default released.
  output=<Output ID>
  forceDefaultActive=False
  normalRequestCount=<preserved request count>
  normalWinner=<current normal winner or none>
~~~

The force-default log deliberately reports normal-request evidence because force-default does not destroy or replace normal CameraRequest arbitration.

## Game Flow lifecycle

Target lifecycle:

~~~text
Session starts
  -> Session Outputs / Session Presentations materialize

Route enters
  -> Route Presentations materialize

Activity enters
  -> Activity Presentations materialize

Activity exits
  -> release only Activity Presentations

Route exits
  -> release only Route Presentations

Session ends
  -> release Session camera content
~~~

Materialization/release uses RuntimeContent Session/Route/Activity scopes rather than a Camera-specific global manager.

## Transition continuity

Output Defaults remain alive across gameplay presentation changes.

During covered transitions the Session Camera transition orchestration may force Default.

This allows outgoing Route/Activity presentation to release while loading/transition remains physically visible.

## Invalid patterns

Do not introduce:

- Camera.main authority;
- FindObjectOfType/name/tag/hierarchy discovery as functional identity;
- a global Camera manager/service locator;
- Player count -> Output count inference;
- Route/Activity-created physical Outputs;
- gameplay and Default sharing one Rig;
- mutable runtime state in CameraPresentationDefinition;
- Player identity inside Camera core Subject contracts;
- Camera request topology writing Camera.rect or other split-layout properties;
- a second normal winner-selection system beside CameraOutputContext.

## Current model

IF-ADR-032 is the current Camera architecture. The live path is:

~~~text
Subject(s)
  -> CameraPresentationRuntime
  -> CameraRigComposer
  -> CameraRequest
  -> explicit CameraOutputSession
  -> Unity Camera + CinemachineBrain
~~~

`CameraPresentationRuntime` is not a MonoBehaviour and is not a ScriptableObject. `CameraPresentationDefinition` stores reusable intent only. Session configuration declares physical Outputs. Route and Activity declare Presentations. They do not create Outputs. Player Slot -> Output and Player Slot -> Presentation bindings are explicit on the GameApplication Camera Session. `PlayerInputManager` remains the owner of `Camera.rect`.

The following are not current product types:

~~~text
CameraSharedComposition
SessionCameraOverride
RouteCameraOverride
ActivityCameraOverride
ScopedCameraOverride
PlayerCameraOutputPolicyAuthoring
PlayerCameraCompositionPolicyAuthoring
CompositionCameraRequestPublisher
CameraRequestOwnerKind.Composition
CameraRequestLifetimeKind.Composition
CameraTargetSourceDescriptor
Persistent-root Camera Output discovery
~~~

`CameraSharedCompositionSubjectPolicyKind`, `CameraSharedCompositionSnapshot` and `CameraSharedCompositionReconcileStatus` remain the current Presentation subject-policy and reconcile contract. The names predate the MonoBehaviour removal. They are not a scene composition authority.

CAMERA-032-A remains the dated 31/31 presentation-runtime evidence. CAMERA-032-B has consumer proof. CAMERA-032-C focused Game Flow QA passed 16/16 on 2026-09-23. CAMERA-032-D/E focused Player lifecycle QA passed 14/14, with the Character Selection split-screen consumer. Subject + Transaction passed 17/17. CAMERA-032-F removes the legacy product surface and points the aggregate QA menu at those current gates plus the structural Presentation contract. Unity execution of that aggregate was not run in this cleanup. Package NUnit tests remain supporting implementation tests only.

## Validation target

The migrated architecture must prove at minimum:

~~~text
Default -> Presentation -> Default
Activity -> Route restoration
Route -> Session/Default restoration
transition force-default continuity
fresh Subject occurrence after rejoin/replacement
stale evidence rejection
transaction rollback integrity
2 explicit Outputs for 2-camera local multiplayer
P1/P2 Subject isolation
PlayerInputManager remains Camera.rect writer
Persistent Content contains no required gameplay Camera composition
~~~
