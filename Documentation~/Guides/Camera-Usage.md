# Camera Usage

Status: **CURRENT — IF-ADR-029 architecture; Unity validation pending**

Normative decisions:

- [IF-ADR-004 — Camera Requests and Output Authority](../Architecture/ADRs/IF-ADR-004-Camera-Requests-and-Output-Authority.md)
- [IF-ADR-022 — Camera Rig Presentation Models](../Architecture/ADRs/IF-ADR-022-Camera-Rig-Presentation-Models-and-Materialization-Authority.md)
- [IF-ADR-028 — Camera Output Participation and Physical Presentation Ownership](../Architecture/ADRs/IF-ADR-028-Camera-Output-Participation-and-Presentation-Layout-Authority.md)
- [IF-ADR-029 — Camera Composition, Group Presentation and Camera View Removal](../Architecture/ADRs/IF-ADR-029-Camera-Composition-Group-Presentation-and-Camera-View-Removal.md)

## Current architecture

```text
Camera Subject(s)
        ↓
Camera Composition
        ↓
CameraRigComposer
        ↓
CameraRequest
        ↓
CameraOutputSession
        ↓
Camera Output
```

The boundaries are explicit:

| Owner | Responsibility |
|---|---|
| Subject authoring | Expose typed observation evidence and an exact observation Transform |
| Composition | Select `1..N` Subjects, own membership revisions/stale protection, project current presentation input and publish/release its request |
| Rig | Own one local presentation behavior, Cinemachine materialization and provenance |
| Request | Participate in one explicit Output with deterministic arbitration evidence |
| Output | Own the Unity `Camera`, `CinemachineBrain`, Default Rig and `CameraOutputSession` |
| `PlayerInputManager` | Own split count, `Camera.rect` and split recomposition |

No intermediate logical identity or parallel topology is required between Composition and Output participation.

## Presentation intents

`CameraRigPresentationIntent` uses frozen explicit values:

```text
Follow      = 10
Fixed       = 20
Mounted     = 30
ThirdPerson = 40
Group       = 50
```

Semantics:

- `Fixed`: target-independent authored pose.
- `Follow`: exactly one Subject.
- `Mounted`: exactly one Subject/mount using hard lock and target rotation.
- `ThirdPerson`: exactly one Subject using `CinemachineThirdPersonFollow`.
- `Group`: one or more Subjects through `CinemachineTargetGroup` and `CinemachineGroupFraming`.

Many Subjects with `Follow` is rejected. It is never reinterpreted as `Group`.

## Authoring a physical Output

Create one `CameraOutputAuthoring` for each explicitly composed physical Output and assign:

```text
Output Definition
Unity Camera
CinemachineBrain
Default Camera Rig
```

The Default Rig is required, target-independent and normally uses `Fixed`. It is not a request and carries no precedence.

Output selection remains:

```text
force-default owner active
  -> Default Rig

otherwise normal CameraRequest winner exists
  -> winner Rig

otherwise
  -> Default Rig
```

One Output may serve multiple local rigs. Do not create another Unity Camera or another Output merely to distinguish Default from gameplay presentation.

## Authoring a Camera Composition

`CameraSharedComposition` owns shared gameplay participation. Assign:

```text
Subject Policy = AllAvailableSubjects
Output Definition = exact target Output definition
Composition Rig = explicit non-Default CameraRigComposer
Request Precedence = explicit policy value
```

The Composition Rig must be distinct from the Output Default Rig.

Runtime flow:

```text
required Subject available
  -> current membership input
  -> Rig presentation applied
  -> Composition request published

last required Subject unavailable
  -> Rig presentation cleared
  -> Composition request released
  -> Output restores winner or Default
```

Membership, presentation and request mutation are one transactional boundary. Failure restores the previous membership, rig presentation and request/output state; rollback failure remains explicit terminal diagnostic evidence.

## Actor Camera Subject

Use `ActorCameraSubjectAuthoring` on the Actor Presentation root:

```text
Actor Presentation
  ActorCameraSubjectAuthoring
    Observation Transform = exact CameraMount/pivot
```

The Actor supplies Subject evidence only. Do not move Camera request, rig or Output authority into `LocalPlayerHost`, `PlayerInput`, a Player prefab or the Player GameObject.

## Split-screen and multi-output

Output identity remains independent and supports explicit `1..N` Outputs. Output count is never inferred from Player count.

For Unity automatic split-screen:

```text
Player Slot -> Camera Output policy
  -> PlayerInput.camera receives exact physical Camera
  -> PlayerInputManager owns split layout
```

Framework Camera does not write `Camera.rect`, `pixelRect`, target display or target texture from Composition/request topology.

## Request arbitration

Normal requests carry explicit identity, Output, owner/lifetime, rig, precedence and deterministic tie-break evidence.

```text
higher precedence wins
equal precedence uses explicit deterministic tie-break
release restores the next winner or Default
```

Route, Activity, Session and specialized policies may publish normal requests through their accepted owners. Ordinary Player/Actor participation contributes Subjects and does not intrinsically own a request.

## Apply / Rebuild

`CameraRigComposer` Apply/Rebuild is Editor-owned and preflights the complete model switch before mutation. It may replace only components proven Framework-owned through exact provenance. Compatible external components remain external; conflicts block explicitly.

Runtime assemblies do not depend on Editor assemblies and do not discover targets through scene lookup.

## Invalid patterns

Do not introduce:

- `Camera.main` or object/name/tag/hierarchy lookup;
- service locator, global Camera registry or generic Camera manager;
- silent fallback between presentation intents;
- gameplay Camera request authoring on ordinary Player prefabs;
- Route/Activity overrides to emulate ordinary Subject availability;
- a Default Rig that requires a Subject;
- one rig acting as both Default and Composition Rig;
- Camera-domain writers for split-screen rectangles.

## Validation checklist

```text
[ ] one explicit Unity Camera and CinemachineBrain per Output
[ ] exact CameraOutputDefinition reference
[ ] Fixed target-independent Default Rig
[ ] distinct gameplay Composition Rig
[ ] explicit Composition request precedence
[ ] Actor/Subject observation Transform is exact
[ ] no request when required Subjects are absent
[ ] request active when current Subjects are available
[ ] leave restores Default; rejoin uses the new Subject occurrence
[ ] no stale Follow/LookAt or stale membership revision
[ ] PlayerInputManager remains the split-layout writer
```

Implementation and authored/static test coverage for IF-ADR-029 are present. Unity import, Edit Mode/Play Mode execution and consumer lifecycle validation must be reported separately and must not be inferred from static checks.
