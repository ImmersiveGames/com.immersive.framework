# IF-ADR-004D — Camera Default Output Presentation Authority

Status: **CLOSED — implemented on master / Sample 00 consumer proof PASS**  
Date: **2026-08-17**  
Type: **Architecture reconciliation / runtime authority / product authoring**  
Primary system: **Camera**  
Normative target: **IF-ADR-004 — Camera Requests and Output Authority**  
Product-surface governance: **IF-ADR-010 — Editor and Inspector Product Surface Authority**

## Purpose

004D closes a real-consumer Camera authority defect found while validating Sample 00.
The defect was not missing Camera precedence. It was a semantic conflation between:

```text
persistent output Default presentation
```

and:

```text
normal Session Camera request arbitration
```

Before this cut the reusable persistent Camera composition represented its normal/default
view through `SessionCameraOverrideBinding` with ordinary request precedence. When the
request set had no winner, the output path could clear instead of presenting a persistent
Default, and transition Camera presentation was structurally conditional on the presence
of that Session override.

004D separates those authorities.

## Accepted authority split

```text
CameraOutputSessionBinding
  owns one explicit persistent Default Camera Rig

CameraOutputSession
  owns Default presentation state
  owns normal logical/physical synchronization
  owns independent idempotent force-default owners

CameraOutputContext
  owns normal admitted Camera requests only
  owns normal deterministic winner arbitration only

SessionCameraOverrideBinding
  remains an optional real Session-scoped Camera request
  is not the Default Camera
```

The Default Camera Rig is therefore **not a CameraRequest** and has no precedence or
tie-break identity.

## Output selection contract

The physical output selection order is:

```text
force-default presentation active
  -> Default Camera Rig

otherwise, CameraOutputContext has a normal winner
  -> winning Camera request rig

otherwise
  -> Default Camera Rig
```

`Clear()` is reserved for true output teardown. Normal absence of an admitted request is
not teardown.

This means no magic request such as:

```text
precedence = 0
precedence = 301
special request id for Default
```

is accepted as a substitute for output-owned Default semantics.

## Transition integration

`SessionCameraTransitionOrchestrator` now receives `CameraOutputSessionBinding`
directly and forces/releases Default through `CameraOutputSession`.

The application composition root no longer requires a `SessionCameraOverrideBinding` in
order to install Camera-aware transition orchestration.

The force-default surface is owner-based and idempotent so overlapping system
presentation owners can coexist without one caller accidentally releasing another
caller's force-default state.

Current 004D wiring uses this mechanism for Transition. 004D does **not** introduce a
Pause-to-Camera binding and does not claim Loading/Pause wiring that is not implemented.

## Product authoring contract

`CameraOutputSessionBinding` requires:

```text
Camera Output ID
Unity Camera
Cinemachine Brain
Default Camera Rig
```

The Default Camera Rig is a persistent explicit `CameraRigComposer` reference.

The custom Inspector exposes `Default Camera Rig` under the primary **Output Components**
section rather than hiding it in Advanced/Diagnostics. Button-driven authoring validation
reports a missing Default as a blocking issue.

Runtime also fails explicitly when the field is absent:

```text
Camera Output Session Binding requires an explicit Default Camera Rig.
```

No runtime discovery or synthesized fallback is allowed.

## Package implementation

Implementation branch:

```text
camera/default-output-authority-cut
688f34e23096c26d2f8e644a432094c64c117ac4
```

Merged to `master`:

```text
8591385d14b646b612b32defc7180e71f21a2beb
Merge branch 'camera/default-output-authority-cut'
```

The final implementation cut changes exactly these product/runtime surfaces:

```text
Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs
Runtime/Camera/Bindings/CameraOutputSessionBinding.cs
Runtime/Camera/Lifecycle/SessionCameraTransitionOrchestrator.cs
Runtime/Camera/Output/CameraOutputRigApplicator.cs
Runtime/Camera/Output/CameraOutputSession.cs
Editor/Camera/Bindings/CameraOutputSessionBindingEditor.cs
Editor/CameraAuthoring/CameraOutputSessionBindingAuthoringValidator.cs
```

`CameraOutputContext` and `SessionCameraOverrideBinding` were intentionally not changed.

## Sample 00 consumer proof

The first run after the package cut correctly failed because the existing consumer scene
had not yet authored its new required Default:

```text
Camera Output Session Binding
  status = Blocked
  Default Camera Rig = missing

Activity
  readiness = NotReady
  reason = ActivityContentExecutionBlockingFailure

MinimalFirstPersonLocomotion
  hasBinding = false
  gameplayReady = false
```

After `MinimalGame_Persistent` assigned its existing `Session Camera Rig` as the explicit
Default Camera Rig, runtime evidence became:

```text
Camera Output Session Binding
  status = Initialized
  output = camera.output.main
  defaultRig = Session Camera Rig

Activity
  readiness = Ready
  blockingIssues = 0

MinimalFirstPersonLocomotion
  READY
  hasBinding = true
  gameplayReady = true
  bindingRevision = 1

LOOK_INPUT received
MOVE_INPUT received
```

This proves the Default-output authoring contract in a real consumer and confirms that
the previous locomotion/input-not-ready symptom was downstream of Camera output
initialization failure.

The Sample run does **not** prove transition force-default behavior because that consumer
run had no configured Transition adapter. That behavior remains package implementation
evidence until focused runtime QA is added or rerun.

## Relationship to prior Camera certification

The Full Camera `53/53` certification dated 2026-08-15 predates 004D.

It remains valid historical evidence for the boundary it tested:

```text
ADR-022 Presentation  14/14
C9R                   11/11
ADR-004B              18/18
ADR-004C              10/10
```

004D must not rewrite that record to imply the aggregate tested Default-output or
force-default semantics.

Current evidence for 004D is:

```text
package implementation merged to master
real Sample 00 Default authoring proof PASS
Activity readiness PASS
Gameplay input binding PASS
Move / Look consumer proof PASS
new aggregate Camera QA run NOT RECORDED
```

## Persistent Content migration note

Persistent Content scenes created before 004D must be migrated by explicitly assigning
the intended persistent Default `CameraRigComposer` to `CameraOutputSessionBinding`.

`SessionCameraOverrideBinding` must not be retained merely to emulate Default behavior.
It may remain only when the game actually wants a normal Session-scoped override.

At the 004D documentation cut, the package's existing
`PersistentContentTemplateSource.unity` still reflects the pre-004D serialized output
shape: it contains the Session Camera rig/override composition but does not yet serialize
`defaultCameraRig` on `CameraOutputSessionBinding`. The template source/template artifact
must therefore be refreshed in a separate authoring-artifact cut before it is treated as
004D-conformant for new consumer scene creation.

This authoring-artifact follow-up does not change the accepted runtime authority split.

## Current result

```text
IF-ADR-004D
  CLOSED architecture/runtime reconciliation

Default Camera
  output-owned persistent presentation
  explicit required authoring
  not a request

Normal Session Camera override
  optional real Camera request
  unchanged arbitration semantics

CameraOutputContext
  normal requests only
  unchanged

Transition
  forces/releases Default through CameraOutputSession
  no dependency on SessionCameraOverrideBinding

Sample 00
  DEFAULT OUTPUT CONSUMER PROOF PASS
  Activity Ready
  gameplay input bound
  Move / Look consumed

Focused post-004D Camera QA
  NOT YET RECORDED

Persistent Content template artifact refresh
  FOLLOW-UP REQUIRED
```
