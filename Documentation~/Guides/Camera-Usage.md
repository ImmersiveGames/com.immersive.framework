# Camera Usage

Status: **IF-ADR-038 runtime implemented; Unity import and Play Mode validation pending**
Last updated: **2026-09-28**

Normative architecture: [IF-ADR-038 — Session Player Camera Assignments and Occurrence Lifecycle](../Architecture/ADRs/IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md).

## Runtime model

```text
CameraDefinition
  -> Session Camera Assignment
  -> Camera Occurrence
  -> Assignment membership and current Actor Subjects
  -> exact Camera Output

Fallback Camera -> separate physical Output coverage
```

The Session Camera authority activates an explicit Assignment for its mapped
Outputs. Occurrence identity is Assignment + Output for Session/Shared modes,
and Assignment + exact PlayerOccurrence + Output for Individual mode. Reusing a
Definition creates isolated occurrence state.

Route and Activity do not declare or select the normal Camera. Join and Leave
change Assignment membership. They do not create or destroy Session/Shared
Occurrences. Actor replacement updates the current Subject on the existing
Occurrence. A Player without a current Actor can remain a member; target
requirements decide whether the normal occurrence is presentable.

## Authoring and physical Outputs

`GameApplicationAsset` owns the Session Camera Assignments and the physical
`CameraSessionConfiguration`. The configuration supplies explicit Output
prefabs. Each `CameraOutputAuthoring` binds one Unity Camera, its Cinemachine
Brain and its independent Fallback Camera Rig. Assignment Output mappings
select destinations; Player Slot -> Output bindings remain for physical local
Player camera association. `PlayerInputManager` remains the split-screen layout
owner and writes viewport geometry.

`CameraDefinition` references a reusable `CameraRigComposer` configuration.
Each runtime Occurrence materializes an independent rig instance for its Output.
The current shared-group implementation exposes all current Subjects explicitly;
it does not choose a Follow/LookAt Subject or apply a Cinemachine group-framing
policy.

## Lifecycle

- Session-scoped and Shared Occurrences may be active with zero members.
- Individual Assignments create one Occurrence only for each eligible exact
  `PlayerOccurrence` and its explicitly mapped Output.
- Leave removes that exact Player membership. It releases only an Individual
  Occurrence owned by that PlayerOccurrence.
- Rejoining with a new PlayerOccurrence creates a new Individual identity.
- Explicit Assignment replacement prepares candidates before commit, applies
  every mapped Output, and releases the previous Assignment only after commit.
  A failed candidate preserves the active Assignment and its presentation.
- Fallback covers an Output when no valid normal occurrence can be shown or
  during explicit transition coverage. Zero members alone do not select it.

Route/Activity participation and eligibility may affect which Actor Subjects are
currently valid. They never activate, arbitrate or restore a Camera Assignment.

## Verification boundary

Static source checks and `git diff --check` do not confirm Unity compilation or
scene behavior. Before release, import the package in Unity and validate
Session startup with zero Players, Session/Shared membership, Individual Join /
Leave, Actor replacement and failure-safe Assignment replacement on every
mapped Output. Re-run consumer scenes and samples after their CAMERA-038-J asset
migration.
