# IF-ADR-026 — Camera Subjects, Assignment and Multi-Output Topology

Status: **Reopened — core Subject / Assignment / View / Rig / Output separation remains accepted; corrected Output participation and viewport-free View→Output topology are certified; Player→Camera integration boundary remains open**  
Accepted: **2026-09-07**  
Reopened: **2026-09-12**  
Type: architecture / Camera runtime topology  
Implementation state: **CAMERA-026-A through G remain implemented; CAMERA-026-H viewport ownership is superseded by IF-ADR-028; CAMERA-026-H2 is implemented/certified; CAMERA-028-B + CAMERA-027-D2 removed viewport from active Camera topology/authoring; CAMERA-026-I Player→Camera integration-boundary reconciliation is pending**

Technical evidence: **CAMERA-026-H2 partial Output participation was technically certified 8/8 on 2026-09-13 and revalidated 8/8 on 2026-09-14. The corrected 2026-09-14 Full Camera run passed 39/39 established cases, ADR-026 phases 2/2 and 8/8 active dimensions with `viewOutputAssociation='PASS'`. The 2026-09-12 Full Camera QA 39/39 remains dated evidence only for the earlier viewport-bearing boundary it executed.**

Related decisions: IF-ADR-003, IF-ADR-004, IF-ADR-004C, IF-ADR-010, IF-ADR-019, IF-ADR-020, IF-ADR-022, IF-ADR-023, IF-ADR-025, IF-ADR-027, IF-ADR-028  
Historical certification: [Camera Full Technical Certification — 2026-09-12](../Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)  
Current reconciliation: [Camera Output Participation and Layout Authority Reconciliation — 2026-09-12](../Reconciliation/IF-CAMERA-OUTPUT-LAYOUT-AUTHORITY-RECONCILIATION-2026-09-12.md)

> The architecture remains based on explicit Camera Subjects, Assignments, Views, Rigs and Outputs.
> This reopening corrects two boundaries discovered after the initial certification:
>
> 1. a physical Output being available in the Session is not the same as that Output being
>    actively associated with a Camera View; and
> 2. screen layout / viewport ownership is not Camera topology authority.

## 1. Context

The older Camera model coupled ordinary Player participation to a complete `CameraRequest`:
one eligible Local Player published one rig and one target against a single persistent output.
IF-ADR-026 separated that model into explicit concerns:

```text
observable entity
      ↓
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

That separation remains accepted.

The post-implementation audit found that the first multi-output implementation introduced a
new coupling later in the pipeline:

```text
registered physical Outputs
      ==
Outputs that must have View bindings now
      ==
Outputs whose screen rectangle Camera owns
```

That equivalence is rejected by this reopening.

## 2. Normative invariant

> Player participation, Camera Subject participation, Camera View composition, Camera Rig
> presentation, Camera Output availability, active View→Output association and physical
> screen layout are separate architectural concerns.

No implementation may derive one of those concerns from another unless an accepted explicit
integration policy says so.

## 3. Normalized concepts

| Concept | Owns | Does not own |
|---|---|---|
| **Camera Subject** | Typed evidence that something may be observed | Player lifetime, Camera request, View, Rig, Output or screen layout |
| **Camera Assignment** | Which available Subjects feed a logical View | Subject lifetime, physical Output or screen layout |
| **Camera View** | Logical composed view and resolved Subject set | Physical screen region or implicit Output discovery |
| **Camera Rig / Presentation** | How resolved Subjects are observed and local Cinemachine materialization | Player lifecycle, assignment policy, Output registration or screen layout |
| **Camera Output** | Explicit physical Camera destination identified by `CameraOutputId`, Unity `Camera`, `CinemachineBrain` and Default Rig | Player existence, active View association or screen placement |
| **View→Output Association** | Which logical View feeds which available Output in the current Camera topology | Viewport, RenderTexture, display selection or split-screen layout |
| **Output Presentation / Layout** | Where/how an Output is presented to a display surface | Camera Subject selection, View composition, request arbitration or rig materialization |

IF-ADR-028 is the normative authority for Output Presentation / Layout.

## 4. Camera Subject

A Camera Subject is something that may be observed. Examples include:

```text
Player Actor observation Transform
vehicle
world object
Activity target
Route target
spectator target
replay target
Subject Set / group
```

A Player may contribute zero, one or more Camera Subjects. A Subject may be assigned to zero,
one or many Views. Subject availability is explicit typed evidence and must not be inferred
from object names, tags, hierarchy, `Camera.main`, Player index or first-Player ordering.

```text
Player lifecycle != Camera lifecycle
```

Joining, leaving or replacing a Player Actor may change Subject availability. It does not
intrinsically create or destroy a Camera View, Rig or Output.

### 4.1 Player→Camera dependency direction

The behavior remains accepted:

```text
Player / Actor Presentation evidence
      ↓
Camera integration adapter
      ↓
Camera Subject availability
```

The dependency direction is normative:

> PlayerParticipation core must not become Camera lifecycle or Camera topology authority merely
> because Player Actors can contribute Camera Subjects.

The integration layer may consume typed Player Actor / Presentation occurrence evidence and
publish Camera Subject availability. Camera-specific projection must remain removable from a
headless or non-visual Player runtime without changing Player Session semantics.

The current `PlayerCameraSubjectAvailabilityProjection` location inside PlayerParticipation is
therefore implementation evidence to reconcile, not a new ownership rule.

## 5. Assignment and cardinality

Camera Assignment answers which available Subjects feed each logical Camera View.

```text
available Camera Subjects
        ↓
Assignment policy
        ↓
0..N Subjects per Camera View
        ↓
Camera presentation input
```

Required cardinality:

```text
Camera View    -> 0..N Camera Subjects
Camera Subject -> 0..N Camera Views
```

A zero-Subject View is valid when its presentation supports that state, for example Fixed.
A required-target presentation must expose explicit unresolved/waiting/failure evidence; it
must not retain stale occurrence references or silently select another Subject.

## 6. Output availability and active participation

This section supersedes the old IF-ADR-026-H assumption that every registered physical Output
must have exactly one View binding in every topology snapshot.

### 6.1 Available Outputs

A Session may register one or more explicit physical Outputs:

```text
Session available Outputs -> 1..N
```

Each registered Output retains:

```text
CameraOutputId
Unity Camera
CinemachineBrain
Default Camera Rig
independent CameraOutputContext
independent CameraOutputSession
```

Duplicate `CameraOutputId` and ambiguous physical bindings remain blocking errors.

### 6.2 Active View→Output associations

A Camera topology may associate a subset of the available Outputs:

```text
Available Outputs           -> 1..N
Current View→Output bindings -> 0..N subset of available Outputs
```

Therefore:

> **Output available != Output actively associated with a View.**

An available Output without a current View association is valid. Camera topology must not
fail merely because an available physical Output is currently unused by View composition.

For one association snapshot:

```text
one Output -> at most one View
one View   -> 0..N Outputs
```

Two conflicting View bindings to the same Output remain invalid.

### 6.3 Player count is not topology authority

These are all valid:

```text
2 Players + one shared View   -> one associated Output
2 Players + split Views       -> two associated Outputs
4 available Outputs + 1 View  -> one associated Output, three unassociated Outputs
0 Players + Fixed View        -> valid Camera presentation
```

Player join/leave may be evidence consumed by a higher-level policy, but Camera core never
creates or destroys Outputs from Player count.

## 7. View→Output topology

The logical Camera relation is only:

```text
Camera View
    ↓
Camera Output
```

A View→Output binding identifies:

```text
View identity
Output identity
```

It must not own:

```text
viewport rectangle
screen partition
safe-area policy
RenderTexture destination
target display
picture-in-picture placement
PlayerInputManager split layout
```

Those are Output Presentation / Layout concerns governed by IF-ADR-028.

The Camera topology may validate that a referenced Output is registered and that no Output has
conflicting View ownership. It must not require binding-count equality with all registered
physical Outputs.

## 8. Request arbitration remains valid and separate

Typed Camera requests and deterministic arbitration remain accepted.

A request is not a Subject declaration and does not define screen layout.

```text
Camera request
  -> selects presentation/request winner for an explicit Output scope

Camera Subject
  -> declares observable evidence

View→Output association
  -> selects which logical View feeds which available Output

Output layout
  -> decides where/how an Output is presented
```

`CameraOutputContext` remains responsible for admitted normal requests and deterministic
winner resolution.

Normative rule:

> **Scope is lifetime/ownership context. Precedence is arbitration policy. Scope is not
> precedence.**

Defaults such as Activity `100`, Route `200` and Session `300` may remain authoring
conventions. They are not hard-coded semantic hierarchy in Camera core.

The output-owned Default presentation remains outside the normal request precedence ladder.
Force-default ownership remains output presentation selection, not screen-layout authority.

## 9. Independent lifetimes

Implementations must model these independently:

| Lifetime | Begins/ends with | Must not intrinsically end |
|---|---|---|
| Subject | observable occurrence/evidence availability | View, Rig, Output |
| Assignment | policy relation between Subject and View | Subject occurrence authority, Output |
| View | logical composition scope | Player eligibility, Output availability |
| Rig | selected local presentation/materialization scope | Player eligibility, Output registration |
| Output | physical destination registration | Subject, Player, View association |
| View→Output association | topology decision | Output physical lifetime |
| Output layout | presentation/layout policy | Camera request, Subject or View lifetime |

Stale Subject occurrence and stale ownership tokens remain rejected explicitly.

## 10. Presentation versus target selection

`Fixed`, `Follow`, `Mounted` and `Third Person` remain presentation semantics.

`CameraRigComposer` remains local materialization authority and consumes already-resolved
presentation input:

```text
Camera Assignment
  resolves Subject / Subject Set
        ↓
CameraRigComposer
  materializes how Cinemachine observes it
```

`CinemachineTargetGroup` and Group Framing are projection mechanisms, never Subject selection,
View assignment, Output registration or layout authority.

## 11. Canonical examples

### A. Single Player / one available Output

```text
Available Outputs: Main
View Gameplay -> Main
P1 Subject assigned to Gameplay
```

### B. Shared local multiplayer

```text
Available Outputs: Main, Secondary
Active bindings: Gameplay -> Main
Secondary remains available and unassociated
Subjects: {P1, P2}
```

No error occurs because Secondary is unused.

### C. Split-screen

```text
Available Outputs: P1, P2
View P1 -> Output P1
View P2 -> Output P2
```

The View→Output topology does not contain left/right rectangles. A separate layout authority
assigns the visual regions.

### D. Spectator capacity not currently used

```text
Available Outputs: Main, Spectator
Active bindings: Gameplay -> Main
Spectator has no View association yet
```

This is valid.

### E. Fixed Activity camera

```text
Activity selects Fixed View
Player exists but is not required as a Subject
View -> Main Output
```

## 12. Preserved decisions

The reopening does **not** invalidate:

- explicit `CameraOutputId` and exact physical Output authoring;
- per-output `CameraOutputSession`, `CameraOutputContext` and transactional request projection;
- `CameraOutputRigApplicator` as presentation-agnostic rig applicator;
- output-owned Default Rig semantics;
- force-default owner semantics;
- explicit Unity `Camera` / `CinemachineBrain` binding;
- no `Camera.main`, name/tag/hierarchy discovery or service locator;
- typed request publication and deterministic arbitration;
- Subject occurrence safety and stale-token rejection;
- Assignment ownership and deterministic Subject resolution;
- `CameraRigComposer` materialization authority;
- `Fixed`, `Follow`, `Mounted`, `Third Person` presentation family;
- Follow shared multi-target support.

## 13. Superseded decisions

The following portions of the original CAMERA-026-H boundary are superseded:

```text
all registered physical Outputs must be bound in every View→Output topology snapshot
BindingCount == physical Output count as a validity rule
viewport is part of CameraViewOutputBinding / CameraViewOutputTopology
Camera runtime owns UnityCamera.rect for normal screen composition
PlayerInputManager automatic split-screen must be globally rejected because Camera owns viewport
```

The old certification remains dated evidence that those rules were implemented consistently;
it does not make those rules current architecture.

## 14. Implementation reconciliation cuts

### CAMERA-026-H2 — Partial Output participation

Status: **implemented / technically certified — 2026-09-13; revalidated 2026-09-14**.

Required result:

```text
registered Outputs may be unassociated
View→Output topology covers only participating Outputs
no binding-count equality with all physical Outputs
conflicting duplicate Output bindings still block
```

Certification evidence:

```text
available physical Outputs                              2
participating View→Output associations                  1
Partial runtime proof                                   8/8 PASS
available unassociated Output                           PASS
Player count 0 → 1 → 0 creates no implicit association PASS
unavailable Output rejection                            PASS
Output A/B arbitration isolation                        PASS
focused certification orchestrator                      PASS
outputParticipation                                     PASS
canonical Shared baseline restore lifecycle             PASS
```

The 2026-09-14 corrected Full Camera run additionally reconfirmed the retained Camera boundary with `39/39` established cases, ADR-026 phases `2/2` and active dimensions `8/8`.

### CAMERA-026-I — Player→Camera Subject integration boundary

Status: **pending**.

Required result:

```text
PlayerParticipation core remains Player-domain authority
Camera integration consumes typed Player Actor / Presentation occurrence evidence
Camera Subject availability publication belongs to Camera/integration boundary
headless/non-visual Player runtime does not require Camera participation
```

No silent lookup or runtime-global bridge is accepted.

Screen-layout implementation cuts are defined by IF-ADR-028.

## 15. QA obligations after reopening

Current corrected certification proves:

```text
N available Outputs + strict subset associated = PASS
unassociated available Output does not block = PASS
conflicting two Views -> same Output = FAIL explicitly
View may feed multiple explicit Outputs = PASS where supported
Player count change does not mutate Output registration implicitly = PASS
request arbitration remains deterministic = PASS
viewOutputAssociation = PASS
outputParticipation = PASS
```

Retained Subject occurrence safety, ordinary Player request removal, scope/precedence and force-default behavior remain covered by the established Full Camera regression set.

The old `viewportSplitTopology` dimension is removed from the active corrected aggregate. Physical layout-authority coverage remains pending under IF-ADR-028-C/D.

## 16. Certification disposition

The 2026-09-12 Full Camera QA result:

```text
39/39 PASS
ADR-026 phases 2/2
9/9 dimensions
```

remains immutable historical evidence for the implementation boundary executed on that date.

The corrected 2026-09-14 run establishes:

```text
Persistent structural regression    12/12 PASS
CAMERA-028-A Partial                 8/8 PASS
Full Camera established cases        39/39 PASS
ADR-026 phases                       2/2 PASS
active Full Camera dimensions        8/8 PASS
viewOutputAssociation                PASS
outputParticipation                  PASS
viewportSplitTopology                REMOVED
Shared baseline restore              PASS
```

Current disposition:

```text
Subject / Assignment / View / Rig separation     accepted, implementation retained
multi-Output registration/isolation              accepted, implementation retained
ordinary Player request removal                  accepted, implementation retained
View→Output explicit identity relation           implemented / certified 2026-09-14
all-Outputs-must-bind rule                       superseded
viewport inside Camera topology                  removed / superseded
Camera-owned screen rectangle                    removed / superseded
Player→Camera integration placement              reopened for boundary reconciliation
partial Output participation                     implemented / certified, revalidated 2026-09-14
physical layout authority                        pending IF-ADR-028-C/D
```

## 17. Rejected scope

- automatic Output creation from Player join;
- implicit first-Player Camera ownership;
- global Camera manager, singleton, service locator or target registry;
- `Camera.main`, hierarchy/name/tag discovery;
- viewport or display layout hidden inside Camera topology;
- PlayerInputManager becoming Camera Subject/Assignment authority;
- Camera core becoming Player Session authority;
- unnecessary redesign of accepted request arbitration or rig presentation families.

## 18. Consequences

The corrected topology supports explicit physical Output capacity independently from current
Camera use. This enables split-screen, spectator, PiP, replay, secondary displays and future
presentation modes without requiring every available Output to participate continuously.

The architecture remains reopened because CAMERA-026-I and the physical layout/integration cuts CAMERA-028-C/D are still pending. CAMERA-026-H2, CAMERA-028-B and CAMERA-027-D2 are complete and technically certified, but they do not close the remaining Player→Camera and physical presentation boundaries.
