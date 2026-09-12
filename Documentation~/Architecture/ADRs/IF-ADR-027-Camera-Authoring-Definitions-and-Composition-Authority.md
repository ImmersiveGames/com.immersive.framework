# IF-ADR-027 — Camera Authoring Definitions and Composition Authority

Status: **Accepted — CAMERA-027-A/B/C/D implemented; CAMERA-027-E deferred; CAMERA-027-F next consumer cut**  
Proposed: **2026-09-11**  
Accepted: **2026-09-12**  
Type: architecture / product authoring / Camera composition  
Extends: IF-ADR-002 — Product Authoring Model; IF-ADR-010 — Editor and Inspector Product Surface Authority; IF-ADR-014 — Authored Definition and Stable Identity Authority  
Preserves: IF-ADR-004 — Camera Requests and Output Authority; IF-ADR-022 — Camera Rig Presentation Models and Materialization Authority; IF-ADR-026 — Camera Subjects, Assignment and Multi-Output Topology  
Implementation: **CAMERA-027-A through D implemented on `master`; CAMERA-027-E is optional/deferred and is not required for acceptance; CAMERA-027-F is the next official Samples/FIRSTGAME migration cut.**  
Technical evidence: **Full Camera QA certified 2026-09-12 — 39/39 mandatory cases, ADR-026 phases 2/2, certified dimensions 9/9.**  
Current certification: [Camera Full Technical Certification — 2026-09-12](../Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)  
Primary authoring evidence: FIRSTGAME / Getting Started MinimalGame migration that exposed the IF-ADR-026 authoring problem, 2026-09-11

> IF-ADR-027 does not replace the IF-ADR-026 runtime topology.
> It raises the normal Camera authoring surface from low-level identities and technical
> bindings to typed reusable definitions and explicit designer-facing associations.
>
> Runtime remains modular. Authoring must be understandable without private knowledge of
> Camera IDs, assignment ownership or topology implementation details.

## 1. Context

IF-ADR-026 established the accepted Camera architecture:

```text
Camera Subject
      ↓
Camera Assignment
      ↓
Camera View
      ↓
Camera Rig / Presentation
      ↓
Camera Output
```

That separation is correct and must remain explicit at runtime.

The first real consumer migration to that architecture exposed a different problem:
normal authoring required the consumer to understand and manually correlate technical
identity and topology details that are not gameplay intent.

The MinimalGame migration that motivated this ADR required the consumer to configure and
cross-check:

```text
ActorCameraSubjectAuthoring
CameraSharedComposition
CameraRigComposer
CameraOutputAuthoring
CameraViewOutputPolicyAuthoring

CameraViewId
CameraOutputId
ViewAssignmentContextId
CameraSubjectAssignmentOwnerId
View → Output binding
Viewport
```

The resulting runtime behavior was correct, but the authoring path was too dependent on
internal Framework knowledge. CAMERA-027-A through D implement the accepted replacement
surface; this section records the proposal-time problem rather than the current normal
workflow.

## 2. Implementation-driving audit snapshot

The following surfaces describe the low-level authoring state that motivated IF-ADR-027.
They are retained as historical rationale; sections 4 onward define the accepted current
authority.

### 2.1 Shared Camera View / Assignment

`CameraSharedComposition` serialized:

```text
viewId
viewDescription
assignmentContextId
assignmentOwnerId
subjectPolicy
outputId
```

The component was simultaneously responsible for normal user intent and the technical
identity values needed by the runtime Assignment context.

Its custom Inspector generated/displayed the View identity and asked the consumer to
choose an Output while the Assignment context and owner identities remained directly
serialized fields.

### 2.2 View-to-Output topology

`CameraViewOutputPolicyAuthoring` serialized a list of bindings containing:

```text
viewId
outputId
viewport
```

The normal Inspector exposed an explicit `Binding`, required the View ID text and Output
identity to match the independently authored View and Output, and then built the runtime
`CameraViewOutputTopology`.

The runtime topology was valid and remains explicit. The manual transport of the technical
identities was the authoring problem.

### 2.3 Physical Output

`CameraOutputAuthoring` owns one physical output. The proposal-time surface serialized:

```text
outputId
Unity Camera
CinemachineBrain
Default Camera Rig
```

The physical references correctly belong to scene/prefab authoring. CAMERA-027-A/B replace
the normal raw Output identity authoring path with the typed Output Definition while
preserving stable `CameraOutputId` as projected runtime/boundary evidence.

### 2.4 Camera Rig behavior

`CameraRigComposer` remains the correct local materialization authority. Before
CAMERA-027-C, the component stored the settings for every accepted Presentation family:

```text
Fixed
Follow
Mounted
Third Person
```

including Follow, Shared Follow, Mounted and Third Person tuning on the same component.
The proposal identified this as the seam for reusable behavior definitions without moving
materialization authority out of the Composer.

CAMERA-027-C implements that seam through typed Camera Rig Behavior definitions while
preserving `CameraRigComposer` validation, Apply/Rebuild and materialization provenance.

### 2.5 Camera Subject

`ActorCameraSubjectAuthoring` remains an appropriate direct authoring surface:

```text
Observation Transform = exact authored Transform
```

It does not need a definition asset merely for symmetry. The exact Transform belongs to
the concrete Actor Presentation instance and remains explicit.

## 3. Product problem

The problem is not that the runtime has too many responsibilities.

The problem is that runtime modularity must not leak into the normal product surface.

A consumer should be able to express:

```text
observe this Subject
through this View
with this Rig behavior
on this Output
using this viewport
```

without needing to know:

```text
how View IDs are generated
how Output IDs are generated
which Inspector owns the canonical text value
which text must be copied into another component
how Assignment context ownership is identified
which technical binding object realizes View → Output
```

The following is therefore rejected as the canonical normal workflow:

```text
copy ID
open another Inspector
paste ID
cross-check another component
create technical binding
manually create infrastructure ownership identities
```

## 4. Decision — authored intent uses typed definitions and references

Normal Camera authoring uses typed authored definitions and direct typed references.
Stable Camera IDs remain part of the runtime/boundary model, but the designer does not
transport those IDs manually between official Camera authoring surfaces.

Normative rule:

> When one authored Camera concept must be shared by multiple official authoring surfaces,
> the consumer shares a typed definition reference, not the textual value of its stable ID.

The relationship is:

```text
Typed authored definition
        ↓
stable Camera identity projection
        ↓
runtime topology / snapshots / diagnostics
```

The stable identity remains explicit and inspectable, but is not the normal linking
mechanism in the Inspector.

## 5. Camera View Definition

The Framework uses a reusable authored Camera View definition concept.

Its responsibility is:

```text
Camera View authored identity
human-readable description / intent
stable CameraViewId projection
```

Normal authoring references the exact View definition asset.

Conceptually:

```text
Gameplay View
  exact authored definition reference
  stable CameraViewId
  description
```

Under IF-ADR-014 semantics:

```text
exact typed asset reference
  = authored definition authority

CameraViewId
  = stable runtime / persistence / diagnostics projection
```

Two distinct View definitions carrying the same stable ID are a collision and must block;
they must not silently become the same authored definition.

## 6. Camera Output Definition

The Framework uses a reusable authored Camera Output definition concept.

Its responsibility is:

```text
Camera Output authored identity
human-readable description / intent
stable CameraOutputId projection
```

The Output definition does **not** own scene objects.

The concrete physical output remains scene/prefab authored through the existing physical
boundary:

```text
CameraOutputAuthoring
  Output Definition
  Unity Camera
  CinemachineBrain
  Default Camera Rig
```

The Output definition must not contain runtime references to:

```text
Unity Camera instance
CinemachineBrain instance
CameraRigComposer instance
scene Transform
Player / Actor occurrence
```

The same authored Output definition may be referenced wherever the same logical Output
identity must be expressed, while physical binding remains owned by the concrete
`CameraOutputAuthoring` instance admitted into the current Session composition.

## 7. Camera Rig Behavior Definition

The accepted IF-ADR-022 Presentation family remains:

```text
Fixed
Follow
Mounted
Third Person
```

Normal reusable behavior and tuning are expressed through typed Camera Rig Behavior
definition assets.

Conceptually:

```text
First Person Mounted
  Presentation = Mounted
  Position Damping
  Rotation Damping

Standard Third Person
  Presentation = Third Person
  Shoulder Offset
  Vertical Arm Length
  Camera Side
  Camera Distance
  Damping
```

A Behavior definition describes reusable Presentation intent and tuning.

It does **not** become materialization authority.

The authority remains:

```text
Camera Rig Behavior Definition
  reusable authored intent
        ↓
CameraRigComposer
  concrete local rig owner
  validation
  Apply / Rebuild
  materialization provenance
        ↓
Cinemachine materialization
```

A Behavior definition must not:

```text
search the scene
select Camera Subjects
select Players
select Outputs
add/remove scene components on its own
own runtime mutable target state
own request arbitration
```

The normal Inspector shows only settings meaningful to the selected behavior.

## 8. Unity Presets boundary

IF-ADR-022 correctly rejected a Camera Profile asset merely because several Presentation
models existed and retained Unity Presets as a reusable-value mechanism.

The consumer evidence demonstrated a stronger need than value copying alone:

```text
semantic definition identity
cross-component references
composition reuse
behavior strategy reuse
View / Output association
```

Therefore IF-ADR-027 reopens and narrows that earlier conclusion.

Unity Presets remain valid for generic value reuse where appropriate, but they are not the
canonical authority for Camera View identity, Camera Output identity, reusable Camera Rig
behavior definition or Camera composition topology.

## 9. Binding is runtime topology; association is authoring intent

IF-ADR-026 requires explicit View-to-Output topology.

That requirement is preserved.

However:

> Explicit runtime topology does not require the consumer to manually author the technical
> binding object or manually copy the stable identities that compose it.

Normal authoring expresses the association directly:

```text
Gameplay View
  Output = Main Output
  Viewport = Fullscreen
```

The Framework deterministically projects that authored association into:

```text
CameraViewOutputBinding
CameraViewOutputTopology
```

This projection adds no gameplay intent because the View, Output and viewport were already
explicitly selected by the consumer.

Normative shorthand:

> **Binding is runtime topology; association is authoring intent.**

For advanced compositions, an explicit multi-binding topology surface remains valid when
it materially improves clarity, for example:

```text
Player 1 View → Output A → Left Half
Player 2 View → Output B → Right Half
Spectator View → Output C → Fullscreen
```

The advanced surface must still use typed definition references rather than copied stable-ID
text.

## 10. Assignment Context and Owner identities are technical infrastructure

`ViewAssignmentContextId` and `CameraSubjectAssignmentOwnerId` remain valid runtime
identity/ownership evidence.

They are not normal gameplay authoring decisions.

The canonical product surface must not require a consumer to invent, copy or externally
generate those values merely to create a normal Camera View composition.

They may be:

```text
derived from an exact authored composition definition
or
owned/generated by the concrete Camera composition authoring instance
```

provided the derivation is:

```text
explicitly scoped
deterministic in ownership semantics
collision-safe
stable for the owning authored instance where required
inspectable in Advanced / Debug
```

They must never be replaced with object-name, hierarchy or timing authority.

## 11. Camera Composition Definition is conditional, not mandatory

A reusable Camera Composition definition remains accepted as a possible future product
surface only when it represents meaningful reusable intent across multiple consumers.

Conceptually it may associate:

```text
View Definition
Output Definition
Rig Behavior Definition
Subject selection policy
Viewport
```

Example:

```text
Minimal First Person Camera
  View      = Gameplay View
  Output    = Main Output
  Behavior  = First Person Mounted
  Subjects  = All Available Subjects
  Viewport  = Fullscreen
```

This does not require all Camera authoring to collapse into one mega asset.

Direct modular authoring is the current canonical surface:

```text
Camera View authoring
Camera Rig Composer
Camera Output Authoring
```

A Composition definition is justified only where the group itself is reusable or where
real consumer evidence shows that it removes repetitive/error-prone technical composition.
No such requirement is necessary to accept IF-ADR-027; therefore CAMERA-027-E is deferred.

## 12. Canonical product concepts

The normal Camera product surface speaks in these concepts:

```text
Camera Subject
  what may be observed

Camera View
  what logical view is being composed and which Subjects it consumes

Camera Rig
  how the resolved Subjects are presented

Camera Output
  where the View is physically rendered
```

The normal product surface does not require direct knowledge of:

```text
CameraViewId text
CameraOutputId text
ViewAssignmentContextId text
CameraSubjectAssignmentOwnerId text
CameraViewOutputBinding implementation details
runtime revisions / occurrence handles
```

Those remain inspectable technical evidence.

## 13. Component-count rule

IF-ADR-027 does not define success as "one Camera component".

Modularity remains an architectural requirement.

A component is justified when it represents one understandable product concept or one
necessary physical boundary.

A component is not justified in the normal authoring path merely because a runtime
contract has a separate implementation type.

The target is therefore:

```text
few coherent product-facing components
        ↓
multiple specialized runtime contracts internally
```

not:

```text
one monolithic Camera manager
```

and not:

```text
one Inspector component per low-level runtime contract
```

## 14. Camera Subject remains direct authored evidence

`ActorCameraSubjectAuthoring` remains the canonical direct Actor Presentation surface for
an exact observation Transform.

The authored relation is concrete and instance-specific:

```text
Actor Presentation
  Camera Subject
    Observation Transform = CameraMount
```

No Camera Subject definition asset is required for this normal case.

The existing rules remain:

```text
exact Transform
must belong to the authored Presentation
no Actor-root fallback once explicit authoring exists
no hierarchy-name lookup
```

## 15. Physical Output remains concrete scene authority

A definition asset cannot replace the physical output boundary.

The concrete Session composition still requires explicit physical evidence:

```text
Unity Camera
CinemachineBrain
Default Camera Rig
```

`CameraOutputAuthoring` or a future semantically equivalent physical authoring surface
remains responsible for that binding.

No Output definition may create an implicit global Camera, `Camera.main` dependency,
name/tag lookup or hidden persistent runtime object.

## 16. Existing runtime authorities remain preserved

IF-ADR-027 does not change:

```text
CameraOutputSession
CameraOutputContext
CameraOutputRigApplicator
CameraSubject availability
CameraViewAssignmentContext
CameraViewPresentationInput
CameraViewOutputTopology
request arbitration
force-default ownership
Subject occurrence lifetime
Output lifetime
```

It changes how normal authored intent is captured and projected into those contracts.

## 17. No silent inference

Authoring simplification must not recreate the coupling removed by IF-ADR-026.

The Framework must not infer normal Camera topology from:

```text
Player count
Player index
first Player
nearest Actor
Camera.main
GameObject.Find
tag
object name
hierarchy name
scene traversal order
```

A simplified authoring surface is allowed to derive only technical facts from explicit
consumer choices.

Example:

```text
Authored:
  View = Gameplay View
  Output = Main Output
  Viewport = Fullscreen

Derived:
  exact CameraViewOutputBinding
```

This is accepted.

Example:

```text
No Output selected
  -> silently use first CameraOutputAuthoring
```

This is rejected.

## 18. Normal versus Advanced / Debug authoring

Normal authoring prioritizes:

```text
Definition references
Subject policy
Behavior
Output association
Viewport
physical scene references where inherently required
validation
```

Advanced / Debug may expose:

```text
stable IDs
Assignment context / owner identity
technical bindings
materialization provenance
runtime topology
revisions
resolved Subjects
current physical selection
```

Advanced / Debug is evidence, not a second authored authority.

## 19. Migration policy

The target architecture does not retain indefinite dual authority between:

```text
raw authored identity strings
and
typed definition references
```

Once a definition-backed surface becomes canonical, normal runtime resolution must not
silently fall back to legacy string fields when the definition is missing.

Migration may use explicit Editor tooling to convert existing authored content, but the
final runtime contract must have one authority.

No compatibility layer may reinterpret an invalid/missing definition through names,
hierarchy or previous serialized text.

## 20. Implementation cuts

IF-ADR-027 is delivered in independently reviewable cuts.

### CAMERA-027-A — Definition identity foundation

Status: **implemented in package** (2026-09-12).

Introduces the View and Output authored-definition model with:

```text
exact asset reference authority
stable ID projection
explicit ID generation/repair
collision validation
normal Inspector references
Advanced ID evidence
```

No runtime topology behavior change is required by this cut.

### CAMERA-027-B — Definition-backed View / Output authoring

Status: **implemented in package** (2026-09-12).

Migrates normal `CameraSharedComposition` and `CameraOutputAuthoring` identity authoring to
typed definitions.

Manual cross-Inspector stable-ID transport is removed from the canonical workflow.
Assignment context/owner technical identities are not normal Inspector inputs.

### CAMERA-027-C — Camera Rig Behavior definitions

Status: **implemented in package** (2026-09-12).

Provides reusable behavior strategy definitions for the accepted IF-ADR-022 family while
preserving `CameraRigComposer` as the sole local materialization/provenance authority.

The Composer consumes the selected Behavior definition and materializes the same supported
Cinemachine pipeline.

### CAMERA-027-D — Authoring association → runtime binding projection

Status: **implemented in package** (2026-09-12). Consumer/sample migration remains CAMERA-027-F.

Normal Camera View composition expresses:

```text
View Definition
Output Definition
Viewport
```

through typed references and deterministically builds the existing
`CameraViewOutputTopology`.

Simple one-View/one-Output composition does not require a separately hand-authored
technical Binding component.

Explicit advanced multi-binding authoring remains supported through the same typed
references.

Package evidence: `CameraDefinitionBackedAuthoringTests`, `CameraViewOutputTopologyTests`.

### CAMERA-027-E — Optional reusable Camera Composition definition

Status: **deferred / not required for IF-ADR-027 acceptance** (2026-09-12).

A reusable Composition definition may be added only after A-D plus real consumer work prove
that a grouped asset materially improves repeated authoring.

No implementation is required to close the current architecture. The direct modular
Subject / View / Rig / Output surface is canonical until evidence justifies this cut.

This cut must not introduce a mega runtime manager or collapse Subject, Rig and Output
lifetime authority.

### CAMERA-027-F — Consumer migration and stale surface removal

Status: **next consumer cut** (2026-09-12).

Migrate official Samples/FIRSTGAME content to the canonical definition-backed authoring
surface and remove obsolete normal-authoring paths that would create dual authority.

Documentation must describe the designer-facing concepts before the technical topology.

CAMERA-027-F is consumer/product proof. It must not reopen the certified runtime topology
without new technical evidence.

## 21. QA obligations

QA proves deterministic technical contracts introduced by the authoring model.

Required coverage includes, where implemented:

```text
View definition stable identity
Output definition stable identity
exact definition-reference authority
ID collision rejection
rename/move stability
asset duplication/collision behavior
missing definition rejection
no fallback to legacy raw IDs
behavior definition validation
behavior materialization equivalence
Apply/Rebuild idempotence preserved
ownership-safe model switching preserved
authored association projects the exact View→Output binding
viewport projection preserved
multi-output isolation preserved
no Player-count inference
no implicit Output discovery
```

The 2026-09-12 full Camera certification closes the integrated runtime/topology regression
boundary after A-D with `39/39`, ADR-026 phases `2/2` and certified dimensions `9/9`.
Package-local definition/materialization tests remain evidence for the authoring-specific
contracts they execute.

QA does not certify subjective Inspector attractiveness.

## 22. FIRSTGAME / consumer proof

FIRSTGAME and official Samples prove the authoring improvement as a consumer workflow.
This work is CAMERA-027-F and remains the next delivery step after ADR acceptance.

The minimum consumer proof for the first migrated sample is:

```text
no View ID copied manually
no Output ID copied manually
no Assignment Context ID manually invented
no Assignment Owner ID manually invented
no hand-authored technical View→Output binding for the normal 1 View / 1 Output case
Camera Subject remains explicit
Camera Rig behavior remains explicit
physical Output remains explicit
Play Mode behavior remains equivalent
```

The expected functional chain remains:

```text
Actor Camera Subject
        ↓
Camera View / Assignment
        ↓
Camera Rig Composer
        ↓
Camera Output
```

## 23. Acceptance closure

IF-ADR-027 is **Accepted as of 2026-09-12**.

The architecture review confirms:

```text
1. typed View and Output definitions preserve IF-ADR-014 identity rules;
2. behavior strategy definitions preserve IF-ADR-022 materialization authority;
3. simple composition no longer requires copied stable IDs;
4. simple View→Output association no longer requires manual technical binding authoring;
5. Assignment context/owner identities are infrastructure rather than normal user input;
6. advanced multi-output/split composition remains explicit and inspectable;
7. no hidden runtime/global authority is introduced;
8. component reduction is semantic, not monolithic;
9. existing Camera runtime contracts remain reusable without redesign.
```

Implementation evidence is CAMERA-027-A through D on `master`. Integrated Camera QA after
the current authoring reconciliation is certified on 2026-09-12. CAMERA-027-E is explicitly
optional/deferred and is not an acceptance criterion. CAMERA-027-F is the next consumer
migration/proof cut.

## 24. Consequences

Positive:

```text
Camera authoring speaks in product concepts
stable identities remain strong without being manually transported
reusable behavior becomes first-class
normal Inspector complexity falls
runtime topology remains explicit
multi-output architecture remains intact
physical Camera authority remains scene-owned
QA continues proving the same runtime contracts
```

Tradeoffs:

```text
new authored definition assets require collision validation and creation UX
sample content requires migration
simple and advanced topology authoring need a clear boundary
existing raw-ID authoring cannot remain a silent parallel authority
```

The former Composer-local behavior serialization concern is reconciled by CAMERA-027-C.
Sample migration remains intentionally separate under CAMERA-027-F.

## 25. Normative summary

```text
Runtime modularity is preserved.
Authoring complexity is reduced.

Designer authors:
  Subject
  View
  Rig behavior
  Output
  viewport / association

Framework derives:
  stable identity projections
  Assignment infrastructure identities
  technical View→Output bindings
  runtime topology objects

Stable IDs remain strong runtime/boundary evidence.
Typed definition references are the normal authoring link.

CameraRigComposer remains the local materialization authority.
CameraOutputAuthoring remains the physical Output boundary.
IF-ADR-026 Subject / Assignment / View / Rig / Output separation remains intact.

Binding is runtime topology; association is authoring intent.

Simpler authoring must never become silent inference or a global Camera manager.

Current closure:
  CAMERA-027-A/B/C/D = implemented
  CAMERA-027-E       = deferred / optional
  CAMERA-027-F       = next Samples/FIRSTGAME migration cut
```
