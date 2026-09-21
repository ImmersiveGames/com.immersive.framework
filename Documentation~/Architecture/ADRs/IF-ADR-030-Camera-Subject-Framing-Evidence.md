# IF-ADR-030 — Camera Subject Framing Evidence

Status: **Accepted architecture — CAMERA-030-A implemented**
Proposed: **2026-09-20**
Accepted: **2026-09-20**
Type: architecture / Camera Subject / Group framing
Reconciles: IF-ADR-026 and IF-ADR-029

Implementation: **CAMERA-030-A implemented; package-local contract test authored**
Consumer integration: **YES — LocalMultiplayer current Group Camera consumer**
Consumer Unity tested: **YES — manual Play Mode, 2026-09-20**
Package-local Unity tests executed: **NO**
QAFramework tested: **NO**
Technically validated: **NO**
Certified: **NO**

## 1. Context

IF-ADR-029 establishes Group as a first-class presentation using
`CinemachineTargetGroup` and `CinemachineGroupFraming`.

The current Group projection assigns the same authored
`GroupCameraRigBehaviorDefinition.memberRadius` to every member. That is sufficient as a
composition fallback, but it cannot represent Subjects whose visual presentation extents
differ.

The size belongs to the materialized presentation of the observable Subject, not to the
logical Actor profile and not to Player participation. Renderer/collider discovery would
also make Camera behavior depend on incidental hierarchy and component composition.

## 2. Decision

> **A Camera Subject may publish an optional presentation-space framing radius centered on its exact Observation Transform.**

The canonical evidence becomes:

```text
Camera Subject
  identity
  Observation Transform
  optional Framing Radius
```

`Framing Radius = 0` means unspecified.

For Group presentation:

```text
Target.Object = Subject.Observation
Target.Weight = GroupBehavior.MemberWeight
Target.Radius = Subject.FramingRadius when specified
              = GroupBehavior.MemberRadius otherwise
```

This cut does not introduce a second framing-center Transform. The Observation remains the
single authored positional anchor.

## 3. Ownership

Actor Presentation authoring may provide the framing radius through
`ActorCameraSubjectAuthoring`.

The Camera Subject owns only descriptive evidence about the observable presentation.

The Group rig continues to own composition behavior:

```text
member weight
default/fallback member radius
framing size
damping
FOV range
dolly range
orthographic size range
```

Composition continues to own which Subjects participate. Output/request ownership is
unchanged.

## 4. Validation and compatibility

A specified framing radius must be finite and greater than zero. Zero is the explicit
unspecified value.

Existing Subject producers and existing Actor Presentations remain compatible because the
existing `CameraSubject` constructor continues to publish no explicit framing radius.

Existing Group behavior remains compatible because `memberRadius` is retained as the
fallback for every Subject without explicit framing evidence.

Subject equality/conflict evidence includes the framing radius so one Subject identity
cannot silently coexist with divergent presentation evidence.

## 5. Rejected alternatives

Rejected for this cut:

```text
store presentation size on ActorProfile
derive radius automatically from Renderer bounds
derive radius automatically from Collider bounds
discover a framing target by GameObject name or hierarchy
make Group presentation choose Actor-specific sizes
replace the radius with a generic shape abstraction without a current consumer
add a second framing-center Transform before consumer evidence requires it
remove GroupBehavior.memberRadius fallback
```

## 6. Implementation cut

### CAMERA-030-A — Optional Subject framing radius

Required result:

```text
ActorCameraSubjectAuthoring exposes optional Framing Radius
Player Actor Camera integration publishes that evidence
CameraSubject carries the optional radius
Group projection uses Subject radius per member
Group Behavior memberRadius remains fallback
no Renderer/Collider/hierarchy discovery
existing Subject producers remain source-compatible
```

Package-local tests may prove the projection contract, but they are not treated as executed
QA evidence until run through the accepted QAFramework environment.


## 7. Consumer integration evidence — 2026-09-20

Dated consumer evidence is preserved in:

[IF-ADR-030 Local Multiplayer Consumer Unity Proof — 2026-09-20](../Reconciliation/IF-ADR-030-LOCAL-MULTIPLAYER-CONSUMER-UNITY-PROOF-2026-09-20.md)

The Local Multiplayer consumer uses dedicated Group Actor Presentations with explicit
`ActorCameraSubjectAuthoring` observation anchors and per-Subject framing radii. Manual
Play Mode confirmed the current consumer path is functional with the dedicated P1/P2 Actor
profiles, local-multiplayer movement and shared Group Camera presentation.

This evidence proves consumer integration of CAMERA-030-A. It does not relabel the
package-local test as executed QA evidence and does not certify the broader Camera suite.
