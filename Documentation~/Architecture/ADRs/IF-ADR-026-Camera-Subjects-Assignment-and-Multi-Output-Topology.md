# IF-ADR-026 — Camera Subjects, Assignment and Multi-Output Topology

Status: **Accepted architecture — CAMERA-026-A/B/C/D/E/F/G/H implemented; Shared Camera technical QA certified; Split/full aggregate and FIRSTGAME certification pending**
Accepted: **2026-09-07**
Implementation: **CAMERA-026-A Subject availability, CAMERA-026-B logical View/Assignment, CAMERA-026-C View-to-presentation seam, CAMERA-026-D shared Follow presentation, CAMERA-026-E shared composition orchestration, CAMERA-026-F ordinary Player-request removal, CAMERA-026-G explicit 1..N Output topology and CAMERA-026-H explicit View-to-Output viewport policy implemented**
Technical QA: **Partial — Shared Camera certified 2026-09-09; Split and Full Camera aggregate pending**
FIRSTGAME proof: **Pending**
Supersedes: the single-output and canonical Local-Player-owned presentation assumptions in IF-ADR-004, IF-ADR-004C and IF-ADR-022; their implemented and certified historical boundaries remain evidence for the old baseline.
Related decisions: IF-ADR-003, IF-ADR-004, IF-ADR-004C, IF-ADR-010, IF-ADR-019, IF-ADR-020, IF-ADR-022, IF-ADR-023, IF-ADR-025
Current certification: [Shared Camera Technical Certification — 2026-09-09](../Reconciliation/IF-ADR-026-SHARED-CAMERA-TECHNICAL-CERTIFICATION-2026-09-09.md)

## Context

The superseded Camera baseline coupled ordinary Player participation to a complete
`CameraRequest`: one eligible Local Player publishes one rig and one target against the
single persistent output. That model cannot express a shared multiplayer camera,
multi-target framing or explicit multiple outputs without special-case ownership.

The architecture must separate four concerns:

```text
Player / Actor
  contributes observable Camera Subject(s)
        ↓
Camera Assignment
  selects Subject(s) for explicitly composed Camera Views
        ↓
Camera Rig / Presentation
  defines how resolved Subject(s) are observed
        ↓
Camera Output
  defines the physical rendering destination
```

## Decision

The following invariant is normative:

> Player participation, Camera subject participation, Camera presentation ownership and Camera output topology are separate architectural concerns.

### 1. Normalized concepts

| Concept | Responsibility | Does not own |
|---|---|---|
| **Camera Subject** | Something explicitly observable by Camera presentation | Camera Rig, Camera Output, presentation selection or viewport topology |
| **Camera Assignment** | Assigns available Subjects or Subject Sets to explicitly composed Camera Views | Subject lifecycle, rig materialization or physical rendering |
| **Camera View** | Logical composed view: assignment plus selected presentation intent | Physical Unity output identity by implication |
| **Camera Rig / Presentation** | Defines how resolved Subjects are observed and materializes the local Cinemachine presentation | Player lifecycle, assignment policy or output topology |
| **Camera Output** | Explicit physical rendering destination identified by `CameraOutputId` and bound to an explicit Unity `Camera` and `CinemachineBrain` | Player or Subject existence |
| **Camera Composition** | Explicit topology of Views, Outputs, assignments and presentation-selection policy for a scope | Automatic derivation from Player count |

No runtime class name is frozen for `Camera Assignment`, `Camera View` or `Camera
Composition` by this ADR. Their names describe responsibilities for the implementation
cuts to realize without a generic manager, service locator or global registry.

### 2. Camera Subject

A Camera Subject is something that may be observed. Examples include a Player Actor,
an Actor camera pivot, vehicle, world object, Player Group, Activity target, Route
target or spectator target.

A Player may contribute zero, one or more Camera Subjects. A Subject may be assigned to
zero, one or more Camera Views/Outputs. Subject availability is explicit typed evidence;
it is not inferred from object names, hierarchy, tags, `Camera.main` or the first Player.

```text
Player lifecycle != Camera lifecycle
```

Joining or leaving changes Subject availability and assignments. It does not
intrinsically create or destroy a Camera Rig, Camera View or Camera Output.

### 3. Assignment and cardinality

Camera Assignment is the authority that answers which available Subjects are consumed
by each composed Camera View.

```text
available Camera Subjects
        ↓
Camera Assignment / Composition policy
        ↓
0..N Subjects per Camera View
        ↓
Camera Rig / Presentation
        ↓
explicit Camera Output
```

Required cardinality:

```text
Camera View / Output -> 0..N Camera Subjects
Camera Subject       -> 0..N Camera Views / Outputs
Session              -> 1..N explicitly composed Camera Outputs
```

A view with zero currently assigned Subjects is valid only when its selected
presentation supports that state, such as an explicit Fixed view. A presentation that
requires Subjects must fail explicitly or enter an explicitly modeled waiting state;
it must not retain stale references or silently select another Subject.

### 4. Output topology

One output remains the default/common composition, but exactly one output is no longer a
product invariant. Output count comes from explicit Camera Composition policy, never
from Player count.

```text
2 Players + shared composition      -> 1 output
2 Players + split-screen composition -> 2 outputs
```

Each output retains an explicit `CameraOutputId`, Unity `Camera`, `CinemachineBrain`,
Default presentation and transactional projection boundary. Multiple outputs require an
explicitly composed collection of those per-output authorities; duplicate identity or
ambiguous bindings must block.

Unity `PlayerInputManager` may provide Player and viewport integration evidence for a
split-screen adapter. It does not decide Framework Camera topology, create implicit
Framework outputs, or become Camera assignment authority.

### 5. Request arbitration remains valid but separate

Typed Camera requests and deterministic arbitration remain valid for selecting a Camera
View/presentation within an explicitly identified output/composition scope. Session,
Route, Activity, Cutscene, Modal Presentation, Spectator and Debug request owners remain
meaningful.

A Camera request is not a Camera Subject declaration. A Player contributing a Subject
does not require that Player to own a Camera request. The canonical gameplay flow is:

```text
Activity
  requests/selects the gameplay Camera composition or view

Players / Actors
  contribute Subjects

Camera Assignment
  assigns those Subjects to the selected view
```

The unused `CameraRequestOwnerKind.LocalPlayer`,
`CameraRequestLifetimeKind.LocalPlayerEligibility` and
`LocalPlayerCameraRequestPublisher` surfaces were removed in CAMERA-026-F. A future
specialized Player-owned view must introduce an explicit policy in its own accepted cut;
ordinary Player gameplay never publishes a Camera request.

### 6. Independent lifetimes

Implementations must represent and validate at least these lifetimes independently:

| Lifetime | Begins/ends with | Must not intrinsically end |
|---|---|---|
| Subject | availability of the observable entity/evidence | View, Rig or Output |
| Assignment | composition/policy decision associating Subject and View | Subject or Output |
| Rig/View | selected presentation/composed view scope | Player eligibility |
| Output | explicit rendering destination scope | Subject or Player eligibility |

When a Subject disappears, its assignments are removed transactionally. Required-target
views must expose explicit unresolved/waiting/failure evidence according to their future
runtime contract. Stale references and silent fallback are rejected.

The existing IF-ADR-004C publication cleanup remains valid for request publishers. It
does not define Subject, Assignment, View/Rig or Output lifetime.

### 7. Presentation versus target selection

The accepted `Fixed`, `Follow`, `Mounted` and `Third Person` family remains presentation
semantics. In particular, `Follow` describes camera movement behavior; it does not mean
that a Camera belongs to one Player.

`CameraRigComposer` remains the designer-facing owner of local presentation intent and
safe Cinemachine materialization. The target-selection responsibility moves outside the
Composer:

```text
Camera Assignment
  resolves a Subject or Subject Set into the target contract
        ↓
CameraRigComposer
  consumes resolved target evidence
  materializes how Cinemachine observes it
```

The exact runtime projection contract from Subject Set to Cinemachine targets remains an
implementation decision. `CinemachineTargetGroup` and Group Framing may be adapters or
projection mechanisms; they are never assignment or composition authority.

### 8. Player Group and multi-target

`CameraTargetSourceKind.PlayerGroup` is useful prior vocabulary, but the current package
only declares the enum member. There is no implemented `ICameraTargetSource` for a
Player Group and `CameraResolvedTargets` currently carries only one Follow and one Look
At `Transform`.

The future Subject Set contract may reuse `PlayerGroup` as one Player-specific source or
selector. It must not make the general Subject Set abstraction Player-specific, because
vehicles, world objects, Activity targets and replay/spectator sources may also form
multi-target sets.

Multi-target framing is valid:

```text
Shared Local Multiplayer
  Output A
    View: Follow / Group Framing
    Subjects: { P1, P2, P3 }
```

Another Player joining changes the Subject Set. It does not imply another Rig or Output.

## Canonical examples

### A. Single player

```text
Camera composition: 1 output, 1 rig
P1 joins -> Subject P1 assigned
Result: still 1 output, still 1 rig
```

### B. Shared local multiplayer

```text
Camera composition: 1 output, 1 shared rig
P1 joins  -> Subjects {P1}
P2 joins  -> Subjects {P1, P2}
P1 leaves -> Subjects {P2}
```

### C. Split-screen

```text
Explicit split-screen composition
Output A -> Subject P1
Output B -> Subject P2
```

Output B exists because the composition selected it, not because P2 exists.

### D. Fixed Activity camera

```text
Activity selects a Fixed view
Player may exist but is not necessarily a Camera Subject for that view
```

## Preserved decisions

- explicit Camera output authority and `CameraOutputId`;
- per-output `CameraOutputSession`, `CameraOutputContext` and transactional projection;
- `CameraOutputRigApplicator` as presentation-agnostic physical applicator;
- explicit Unity `Camera` and `CinemachineBrain` bindings;
- no `Camera.main`, name/tag/hierarchy discovery or service locator;
- typed request/publication contracts and deterministic arbitration;
- output-owned Default presentation semantics;
- `CameraRigComposer` and ownership-safe Apply/Rebuild materialization;
- `Fixed`, `Follow`, `Mounted` and `Third Person` presentation family.

## Superseded decisions

- exactly one persistent Camera Output per Session as a product invariant;
- duplicate output count as invalid without considering explicit composition identity;
- ordinary Player Camera participation requiring a Player-owned complete request;
- Local Player eligibility intrinsically controlling gameplay Rig/View lifetime;
- target selection being owned by `CameraRigComposer` rather than supplied as resolved
  assignment evidence;
- multi-output, split-screen and group framing being architecturally out of scope.

These superseded constraints are not compatibility rules. The accepted replacement
topology is implemented through CAMERA-026-H; certification remains separate.

## Current implementation coverage

The repository implements eight IF-ADR-026 runtime slices, including explicit 1..N Output and viewport topology.
CAMERA-026-A provides typed Camera Subject identity/description, scoped
availability with exact stale-safe tokens, immutable snapshots and projection from the
current Session physical Player Actor occurrence. CAMERA-026-B provides logical Views,
explicit View-to-Subject Assignments, assignment ownership/tokens, deterministic
0..N/0..N snapshots and reconciliation that removes unavailable occurrence assignments
without ending View lifetime. CAMERA-026-C projects one explicitly selected logical View
to an immutable presentation input that retains its complete ordered resolved Subject
collection and assignment/availability revision evidence. `CameraRigComposer` can consume
that external input through a separate explicit operation without reading or merging its
legacy authored target source.

The ordinary Player-owned `CameraRequest` path is removed. Player gameplay admission now
aggregates occupancy and input only; prepared physical Actor lifetime independently
projects Camera Subject availability.
The current adapter supports Fixed presentation with zero Subjects and existing
single-target presentation with exactly one Subject. A sole role-neutral observation is
used for each active Follow/Look At role; this is a terminal compatibility projection,
not a claim that those roles are semantically identical. Multiple Subjects remain valid
logical input. CAMERA-026-D materializes multiple Subjects only for Follow as one
Composer-owned `CinemachineTargetGroup` plus one Composer-owned
`CinemachineGroupFraming` on the existing `CinemachineCamera`. Membership is rebuilt
deterministically from the current View input; transitions to one or zero Subjects clear
the technical group without destroying the rig. Mounted and Third Person remain
explicitly unsupported for multiple Subjects. CAMERA-026-E adds one explicitly authored
shared composition that owns its View and exact Assignments, consumes immutable changes
from one bound Camera Subject availability context, selects all currently available
Subjects by explicit local policy, and applies the resulting A→B→C→D chain to one exact
`CameraRigComposer`. Empty membership is a valid dormant state, occurrence/revision
evidence is preserved, and teardown releases only owned relations while clearing targets
without destroying the rig or output. `CameraOutputAuthoring` keeps that same Composer as
its Default presentation, so zero ordinary Player requests are required. CAMERA-026-G
adds `CameraOutputSessionTopology`: it validates and initializes 1..N explicitly authored
Outputs, rejects missing/invalid/duplicate `CameraOutputId`, provides ordinal lookup and
snapshots, injects each consumer only into its requested Output, applies transition Default
forcing across all Outputs transactionally and tears the Session topology down deterministically.
Each Output retains an independent Context, Default, request set, winner, Unity Camera and
Cinemachine Brain. CAMERA-026-H adds an immutable, deterministic `CameraViewId` to
`CameraOutputId` binding topology with a finite normalized viewport. One View may feed
multiple Outputs, but each physical Output has one binding in a policy snapshot. The
Session runtime applies only the exact Output Camera, restores authored viewports when a
binding is removed or the policy ends, and never changes Output arbitration or lifetime.
`PlayerInputManager.splitScreen` is rejected while Framework Camera composition is active;
Player join/leave remains outside Camera topology authority.
Framework-local tests were added. The Shared Camera composition boundary is technically
certified by the 2026-09-09 runtime proof. Split runtime, the Full Camera aggregate and
FIRSTGAME proof remain pending; historical Camera certification must not be relabeled as
proof of IF-ADR-026.

## Future implementation cuts

1. **Subject contracts (CAMERA-026-A implemented)** — typed identity, availability,
   provider observation, stale removal and additive ordinary Player Subject contribution.
   The old Player-owned request path intentionally remains until the later migration cut.
2. **Assignment/View contracts (CAMERA-026-B implemented)** — explicit scoped Views,
   exact owned Assignments, 0..N/0..N cardinality, deterministic resolved snapshots and
   stale Subject reconciliation; request selection remains separate.
3. **Rig target-input seam (CAMERA-026-C implemented)** — immutable View-scoped input
   preserves 0..N resolved Subjects and source revisions; `CameraRigComposer` consumes it
   through an explicit path without selecting Players or merging legacy target sources.
4. **Shared multi-target projection (CAMERA-026-D implemented for Follow)** — one
   Framework-owned Cinemachine Target Group and Group Framing extension project the
   current ordered View Subjects onto the existing rig; other multi-Subject presentation
   families remain unsupported.
5. **Shared composition orchestration (CAMERA-026-E implemented)** — one explicit
   View and Composer binding continuously reconcile all available Subjects from one
   bound Camera-domain context; no Player, request or Output topology knowledge enters
   the selection policy.
6. **Migration/removal (CAMERA-026-F implemented)** — ordinary Player gameplay no longer
   resolves Camera authoring/output endpoints, owns Camera eligibility evidence or
   publishes/releases per-Player requests. No specialized Local Player request policy had
   an active consumer, so its unused publisher and enum members were removed.
7. **Output composition (CAMERA-026-G implemented)** — explicit Session collection of 1..N
   outputs, identity validation, exact per-output injection, snapshots, deterministic teardown
   and transactional independence.
8. **View-to-Output and split-screen policy (CAMERA-026-H implemented)** — explicit
   normalized viewport binding, exact Output application, isolated restoration and
   fail-fast rejection of `PlayerInputManager` automatic split-screen.

CAMERA-026-A through CAMERA-026-H are implemented. Shared Camera technical QA is
certified as of 2026-09-09. Split runtime, the Full Camera aggregate and FIRSTGAME proof
remain pending; the package does not infer or automatically generate multiplayer layout.

## Future validation contracts

Shared Camera runtime QA has proven:

- single-player subject join preserves one output/rig;
- shared two-or-more-Player join/leave mutates only the Subject Set;
- disappearing Subjects cannot leave stale target references;
- Player leave/rejoin creates a new Subject occurrence without reviving the stale one;
- Player count does not create outputs or ordinary Player-owned Camera requests.

Remaining technical QA must prove:

- Fixed Activity views need no Player target;
- one Subject can feed main, minimap, spectator, replay or picture-in-picture views;
- explicit split-screen composition creates and isolates two outputs;
- per-output arbitration, rollback, Default and teardown remain isolated;
- duplicate `CameraOutputId` and ambiguous composition block;
- `PlayerInputManager` evidence cannot override Framework composition.

FIRSTGAME must eventually prove the four canonical examples above, then add one secondary
view scenario (minimap or picture-in-picture) to prove Subject-to-many-Views cardinality.

## Rejected scope

- multi-output or split-screen implementation in CAMERA-026-F;
- automatic output creation from Player join;
- global Camera manager, singleton, service locator or target registry;
- `Camera.main`, hierarchy/name/tag discovery or implicit first-Player selection;
- PlayerInputManager as Framework Camera authority;
- Cinemachine Target Group as architectural authority;
- unnecessary redesign of the accepted presentation family or request arbitration.

## Consequences

The target architecture supports shared cameras, split-screen, fixed Activity/Route
cameras, vehicle, spectator and replay cameras, minimap and picture-in-picture without a
Player/Camera ownership assumption. The remaining cost is explicit output composition,
validators, QA and consumer authoring. The obsolete ordinary Player Camera
eligibility/request runtime is no longer an executable baseline.
