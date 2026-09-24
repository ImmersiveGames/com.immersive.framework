# IF-ADR-032 Local Multiplayer Group Consumer Proof — 2026-09-22

Status: **CONSUMER UNITY PROOF PASS / PARTIAL IF-ADR-032 VALIDATION EVIDENCE**

Scope:
- IF-ADR-032 CAMERA-032-C Route / Activity Presentation participation;
- IF-ADR-032 CAMERA-032-D one-Output GameApplication Camera Session materialization;
- shared-output local multiplayer Group presentation with `AllAvailableSubjects`;
- Actor-owned Camera Subject observation and per-Subject framing radius.

This record does **not** certify CAMERA-032-E Player `ExplicitSelection`, two physical Outputs,
split-screen layout, force-default transition continuity, or the full IF-ADR-032 acceptance matrix.

## 1. Migrated consumer topology

The Local Multiplayer sample now uses the current IF-ADR-032 product model:

~~~text
GameApplication "Local Multiplayer"
  Camera Session
    one explicit Camera Output prefab
      -> CameraOutputDefinition
      -> Unity Camera
      -> CinemachineBrain
      -> persistent Default Rig

Route "Local Multiplayer"
  -> CameraPresentation_Default_Route
     transition = Cut
     precedence = 200

Activity "Local Multiplayer"
  -> CameraPresentation_LocalMultiplayer
     transition = Blend
     precedence = 300
     subject policy = AllAvailableSubjects
     output = same explicit Session Output
     rig prefab = Local Multiplayer Group Presentation

Group Presentation prefab
  -> CameraRigComposer
     -> GroupCameraRigBehaviorDefinition

Player Actor Presentations
  -> ActorCameraSubjectAuthoring
     -> explicit Observation Transform
     -> authored Framing Radius
~~~

No Player-specific Presentation is required for this shared-screen topology. Both current Player
Actors publish Camera Subjects and the one Activity Group Presentation consumes all available
Subjects.

## 2. Session Output evidence

The supplied Play Mode log contains the current CAMERA-032-D diagnostic:

~~~text
Camera Session Outputs materialized.
outputCount='1'
outputs='997d91356a3d4e26b3fa8878422ae6e8'
diagnostic='Camera Session materialized '1' explicit Output prefab occurrence(s).'
~~~

The physical Output initializes successfully before Route/Activity Camera Presentation
materialization.

This is direct consumer evidence that the Local Multiplayer GameApplication Camera Session
can own and materialize its one explicit physical Output. It is not evidence for the separate
two-Output case.

## 3. Route and Activity Presentation evidence

The same run reports:

~~~text
Route Camera Presentation materialized.
owner='Local Multiplayer'
presentation='CameraPresentation_Default_Route'
transitionMode='Cut'
requestPrecedence='200'
runtimeScope='Route'

Activity Camera Presentation materialized.
owner='Local Multiplayer'
presentation='CameraPresentation_LocalMultiplayer'
transitionMode='Blend'
requestPrecedence='300'
runtimeScope='Activity'
~~~

Both target the same explicit Session Output.

Manual Play Mode observation confirmed the Route entry presentation and the Activity-owned
shared Group presentation are visible and functional. This closes the basic IF-ADR-032-C
consumer participation proof for Route and Activity in this topology.

It does not close the separate Activity-exit, Route-exit or Route-replacement restoration cases.

## 4. Player / Subject evidence

The run confirms both local Player Slots join and their logical Actors are prepared,
physically materialized and gameplay-admitted.

The sample uses:

~~~text
P1 Actor Presentation
  -> Camera Subject

P2 Actor Presentation
  -> Camera Subject

CameraPresentation_LocalMultiplayer
  Subject Policy = AllAvailableSubjects
        |
        v
CinemachineTargetGroup
        |
        v
CinemachineGroupFraming
~~~

This is intentionally different from CAMERA-032-E:

~~~text
shared Group sample
  -> AllAvailableSubjects
  -> no Player Slot -> Presentation binding required

Player-specific camera sample
  -> ExplicitSelection
  -> Player Slot -> Presentation binding required
~~~

Therefore this record must not be used as CAMERA-032-E certification.

## 5. Per-Subject framing evidence

The migration exposed one expected authoring issue: the Actor observation target is an empty
GameObject and has no intrinsic presentation-space extent.

The current IF-ADR-032 Subject contract already carries the required explicit evidence:

~~~text
Camera Subject
  Observation Transform
  optional Framing Radius
~~~

The Group projection contract is:

~~~text
Target.Object = Subject.Observation

Target.Radius =
  Subject.FramingRadius        when specified
  GroupBehavior.MemberRadius   otherwise
~~~

After an explicit `ActorCameraSubjectAuthoring.FramingRadius` was authored on the Actor
Presentation, manual Play Mode confirmed the Group framing used the intended Actor extent and
the undesired close framing was corrected.

This proves the consumer-facing purpose of Framing Radius under the migrated
`CameraPresentationRuntime` / Group Rig path. An empty observation GameObject is valid; its
size is not inferred from Transform scale, Renderer or Collider.

## 6. IF-ADR-032 evidence contribution

This consumer proof supports the following current ADR-032 statements:

~~~text
CAMERA-032-C
  Route Presentation materializes/participates                 PASS
  Activity Presentation materializes/participates              PASS
  shared Output precedence topology works in consumer           PASS

CAMERA-032-D
  GameApplication owns explicit physical Output capacity        PASS
  one configured Output prefab materializes once                PASS
  Output exists before Route/Activity Presentation participation PASS

Subject / Presentation
  one current Subject in Group consumer                         PASS
  two current Subjects in Group consumer                        PASS
  per-Subject Framing Radius affects Group framing              PASS
  Group behavior MemberRadius remains fallback                  SUPPORTED BY IMPLEMENTATION

Authoring
  Route/Activity Camera intent lives on owning assets           PASS
  gameplay Rig is reusable Presentation prefab authoring        PASS
~~~

Still pending at the time of this 2026-09-22 consumer run:

~~~text
full zero/one/many focused QA with camera diagnostics
stale Subject / stale rollback focused QA
Activity exit restoration
Route exit restoration
Route replacement continuity
force-default apply/release continuity
two explicit physical Outputs
Player Slot -> Output split-screen topology
CAMERA-032-E Player ExplicitSelection
leave/rejoin fresh Subject proof under the 032-E binding path
full CAMERA-032-F cleanup and recertification
~~~

## 7. Disposition

~~~text
Local Multiplayer migration to IF-ADR-032       PASS
one-Output Camera Session consumer proof         PASS
Route Presentation consumer participation        PASS
Activity Group Presentation participation        PASS
AllAvailableSubjects shared Group behavior       PASS
Actor Framing Radius consumer behavior           PASS

CAMERA-032-C full validation                     PARTIAL
CAMERA-032-D full validation                     PARTIAL
CAMERA-032-E validation                          NOT CLAIMED
IF-ADR-032 full certification                    PENDING
~~~

Subsequent status update on 2026-09-23:

- CAMERA-032-C focused Game Flow lifecycle certification passed 16/16 and canonical restore PASS;
- Activity -> Route, Route -> Session/Default, Route replacement and force-default apply/release continuity are no longer pending technical cases;
- CAMERA-032-D/E subsequently gained focused QA and Character Selection split-screen consumer evidence;
- Subject occurrence, stale evidence and transaction integrity subsequently passed the focused Subject + Transaction gate 17/17;
- CAMERA-032-F cleanup, remaining authoring/static reconciliation and final aggregate recertification remain pending.

Subsequent evidence:

- [IF-ADR-032-C Game Flow Lifecycle Focused Validation — 2026-09-23](IF-ADR-032-C-GAME-FLOW-LIFECYCLE-FOCUSED-VALIDATION-2026-09-23.md)
- [IF-ADR-032 Character Selection Multiplayer Split-Screen Consumer Proof — 2026-09-23](IF-ADR-032-CHARACTER-SELECTION-MULTIPLAYER-SPLITSCREEN-CONSUMER-PROOF-2026-09-23.md)
- [IF-ADR-032 Subject + Transaction Focused Validation — 2026-09-23](IF-ADR-032-SUBJECT-TRANSACTION-FOCUSED-VALIDATION-2026-09-23.md)

Related historical evidence:

- [IF-ADR-030 Local Multiplayer Consumer Unity Proof — 2026-09-20](IF-ADR-030-LOCAL-MULTIPLAYER-CONSUMER-UNITY-PROOF-2026-09-20.md)
- [IF-ADR-029/030 Camera Composition and Framing Technical Certification — 2026-09-21](IF-ADR-029-030-CAMERA-COMPOSITION-FRAMING-TECHNICAL-CERTIFICATION-2026-09-21.md)

Normative authority remains:

- [IF-ADR-032 — Camera Unified Authority, Session Outputs, Presentations, Subjects and Lifecycle](../ADRs/IF-ADR-032-Camera-Unified-Authority-Session-Outputs-Presentations-Subjects-and-Lifecycle.md)
