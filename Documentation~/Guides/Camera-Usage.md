# Camera Usage

Status: **IF-ADR-038 Assignment-owned Player Output mapping in active implementation; Unity import/Play Mode validation pending.**
Last updated: **2026-10-03**

Architecture status: [IF-ADR-038](../Architecture/ADRs/IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md) is Accepted; Unity validation remains pending. This guide describes the current authored model reflected by implementation and package documentation. This status is not Unity validation or promotion of Experimental Camera assets to Stable API.

## Mental model

The current primary Camera flow is:

```text
Actor occurrence
  └─ explicit Camera Subject (ObservationTransform)
       ↓
Session Camera Assignment asset (Rig Prefab + policy)
       ↓
Camera Occurrence for an exact Output
       ↓
Camera Output (Unity Camera + Cinemachine Brain)
       └─ Fallback Camera coverage when needed
```

Subjects are supplied by the current Actor occurrence. Camera configuration defines rig behavior. The Session Assignment determines membership, target policy, occurrence mode and Output mapping. Each physical Output presents one normal occurrence or its Fallback Camera.

There is no CameraRequest/precedence selection surface. Runtime Assignment changes use the explicit `SessionCameraAssignmentCommandTrigger` boundary defined by IF-ADR-039.

## Supported composition

- `ActorCameraSubjectAuthoring` explicitly supplies the Actor’s observation Transform. It may differ from the Actor root and must belong to that exact Actor occurrence. Camera does not infer a root or search by hierarchy/name.
- `SessionCameraAssignmentAsset` directly references a reusable Rig Prefab. A `CameraRigComposer` on that prefab owns concrete rig configuration and materializes the supported Cinemachine rig locally.
- `GameApplicationAsset` owns Session Camera configuration: physical Output capacity/configuration and Session Camera Assignments.
- Each `CameraOutputAuthoring` binds one explicit Unity Camera, its Cinemachine Brain and its independent Fallback Camera rig.
- Assignment authoring explicitly maps Outputs and declares occurrence mode, Player membership and target policy. Output count and Player-to-Output associations are explicit; Player count does not create Camera Outputs.
- `CameraSessionConfiguration` contains only physical Output prefabs. For `IndividualPerPlayer`, each Session Camera Assignment maps every member Slot to its Output. `PlayerCameraOutputIntegrationRuntime` derives `PlayerInput.camera` from that Assignment and current Player Host evidence. Do not author a second Player Slot → Output table.
- `SharedGroup` and `SessionScoped` Assignments do not create individual Player Output bindings. Multiple Players may share one Output in `SharedGroup`, and zero-Player SessionScoped cameras remain valid without a Player binding.

The Camera system/assets are not currently marked Stable as a whole. Check the Public API Reference for each surface’s maturity.

## Runtime behavior

- Session-scoped and Shared occurrences live for the Assignment lifetime, including when they have zero members if their target requirements permit presentation.
- Individual occurrences are created for eligible exact Player occurrences and their explicitly mapped Output.
- Join/Leave reconciles membership. It does not select a different Assignment.
- Actor replacement updates the current Subject on the existing Camera occurrence. It does not, by itself, replace the Assignment or recreate the occurrence.
- A new Route or Activity does not request a Camera and does not select a replacement. The configured Session Assignment and its occurrence remain. Route/Activity participation may temporarily make an Actor Subject ineligible.
- The Output may show its current normal occurrence or temporary/required fallback coverage. Showing fallback does not deactivate or remove the configured Assignment. Zero members alone do not select fallback.
- Only an explicit Session Camera Assignment command changes normal camera selection. `Activate` starts an Assignment on free Outputs, `Replace` transactionally swaps an explicitly identified active Assignment, and `Clear` removes an explicitly identified active Assignment so its Outputs remain on Fallback. Candidate preparation must succeed before replacement commits; failed preparation preserves the previous Assignment.

Additional gameplay cameras such as cutscenes remain game/Cinemachine-owned. The Framework may choose the individual/shared split-screen regime. `PlayerInputManager` owns viewport geometry; Framework Camera code does not write `Camera.rect` or `Camera.pixelRect`.

## Setup outline

1. On the Game Application, author explicit physical Camera Outputs and their fallback rigs.
2. Create a `Session Camera Assignment` asset, reference a Rig Prefab with `CameraRigComposer`, then set occurrence, membership and target policies and explicit Outputs. For Individual mode, map each member Slot to its Output on that Assignment.
3. Reference reusable Assignment assets in the Game Application startup list or in Session Camera command triggers. The asset owns its generated Assignment identity; consumers do not type IDs.
4. On each Actor occurrence that will be a camera target, author `ActorCameraSubjectAuthoring` and set the intended `ObservationTransform`.
5. When gameplay needs to change the active Assignment at runtime, author a `SessionCameraAssignmentCommandTrigger` in explicitly bound Persistent Content and use `Activate`, `Replace` or `Clear`. Route/Activity assets remain Camera-free.
6. Validate Camera authoring through the owning Inspector. Confirm every required Output mapping and fallback is explicit.

The concrete Output prefab and assignment settings must match the intended one-Output, shared or per-Player design. See IF-ADR-038 for cardinality and failure details.

## Common mistakes

- Expecting a Route or Activity to supply or select a camera.
- Equating temporary fallback display with removal of the configured Assignment.
- Assuming zero Players always means fallback.
- Leaving Output, membership or target selection implicit.
- Expecting a group rig to choose a follow/look-at subject automatically.
- Treating an Actor Profile or Actor root as a substitute for explicit Subject authoring.
- Relying on an old Camera Request/Presentation sample without migrating it to the current Assignment model.
- Adding Camera fields back to Route/Activity instead of issuing an explicit Session Camera Assignment command from game-owned flow/presentation composition.

## Public surfaces

See the [Public API Reference](../API/Public-API.md#camera). Session Camera Assignment and related authoring are Experimental/current implementation surfaces; Output authoring and rig composition have separately declared API status.

## Related architecture

- [IF-ADR-038 — Session Player Camera Assignments and Occurrence Lifecycle](../Architecture/ADRs/IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md)
- [IF-ADR-039 — Session Camera Assignment Command Boundary](../Architecture/ADRs/IF-ADR-039-Session-Camera-Assignment-Command-Boundary.md)
- [IF-ADR-019 — Session Player lifetime and Activity representation](../Architecture/ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md)
