# Camera Usage

Status: **IF-ADR-032 target architecture / CAMERA-032-A validated / CAMERA-032-B implemented / C–F pending**  
Last updated: **2026-09-21**

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

A two-player Session therefore explicitly declares two Outputs when the game needs two physical Cameras. Player count alone never creates the second Output.

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

## Migration status

IF-ADR-032 is the accepted target. CAMERA-032-A has started the runtime migration.

Current CUT A split:

~~~text
CameraSharedComposition
  serialized transitional authoring + Unity lifecycle adapter
        |
        v
CameraPresentationRuntime
  membership
  availability / selection subscriptions
  Rig transaction
  request publication / release
  rollback
~~~

The extracted runtime is not a MonoBehaviour. Outputs and Game Flow authoring are intentionally unchanged until later cuts.

Current transitional code still contains former architecture such as:

~~~text
CameraSharedComposition
SessionCameraOverride
RouteCameraOverride
ActivityCameraOverride
PlayerCameraOutputPolicyAuthoring
PlayerCameraCompositionPolicyAuthoring
Persistent-root Camera Output discovery
~~~

Those types are migration input, not target product architecture.

Do not use their continued presence as authority to extend the former design.

Migration is tracked by CAMERA-032-A..F in IF-ADR-032. CAMERA-032-A is technically validated: Structural 10/10, Shared 10/10 and Generic request/output 11/11 (31/31 focused QA PASS). CAMERA-032-B is implemented and awaits Unity compile/manual proof using a consumer-authored Rig prefab + CameraPresentationDefinition + GameApplication Session Presentation reference. Package NUnit tests remain supporting implementation tests only.

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
