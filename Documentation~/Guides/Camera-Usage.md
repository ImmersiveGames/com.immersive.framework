# Camera Usage

Status: **IF-ADR-038 Camera model in active implementation; package Camera/sample migration and Unity import/Play Mode validation pending.**
Last updated: **2026-09-30**

Architecture status: [IF-ADR-038](../Architecture/ADRs/IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md) is marked Proposed while normative consolidation is in progress. This guide describes the current authored model reflected by implementation and package documentation. This status is not Unity validation or promotion of Experimental Camera assets to Stable API.

## Mental model

The current primary Camera flow is:

```text
Actor occurrence
  └─ explicit Camera Subject (ObservationTransform)
       ↓
Camera Definition / Rig configuration
       ↓
Session Camera Assignment
       ↓
Camera Occurrence for an exact Output
       ↓
Camera Output (Unity Camera + Cinemachine Brain)
       └─ Fallback Camera coverage when needed
```

Subjects are supplied by the current Actor occurrence. Camera configuration defines rig behavior. The Session Assignment determines membership, target policy, occurrence mode and Output mapping. Each physical Output presents one normal occurrence or its Fallback Camera.

There is no supported Camera Request selection surface in the current model.

## Supported composition

- `ActorCameraSubjectAuthoring` explicitly supplies the Actor’s observation Transform. It may differ from the Actor root and must belong to that exact Actor occurrence. Camera does not infer a root or search by hierarchy/name.
- `CameraDefinition` references reusable rig configuration. A `CameraRigComposer` owns the concrete rig configuration and materializes the supported Cinemachine rig locally.
- `GameApplicationAsset` owns Session Camera configuration: physical Output capacity/configuration and Session Camera Assignments.
- Each `CameraOutputAuthoring` binds one explicit Unity Camera, its Cinemachine Brain and its independent Fallback Camera rig.
- Assignment authoring explicitly maps Outputs and declares occurrence mode, Player membership and target policy. Output count and Player-to-Output associations are explicit; Player count does not create Camera Outputs.

The Camera system/assets are not currently marked Stable as a whole. Check the Public API Reference for each surface’s maturity.

## Runtime behavior

- Session-scoped and Shared occurrences live for the Assignment lifetime, including when they have zero members if their target requirements permit presentation.
- Individual occurrences are created for eligible exact Player occurrences and their explicitly mapped Output.
- Join/Leave reconciles membership. It does not select a different Assignment.
- Actor replacement updates the current Subject on the existing Camera occurrence. It does not, by itself, replace the Assignment or recreate the occurrence.
- A new Route or Activity does not request a Camera and does not select a replacement. The configured Session Assignment and its occurrence remain. Route/Activity participation may temporarily make an Actor Subject ineligible.
- The Output may show its current normal occurrence or temporary/required fallback coverage. Showing fallback does not deactivate or remove the configured Assignment. Zero members alone do not select fallback.
- Only an explicit Session Camera Assignment change changes normal camera selection. Candidate preparation must succeed before replacement commits; failed preparation preserves the previous Assignment.

Additional gameplay cameras such as cutscenes remain game/Cinemachine-owned. `PlayerInputManager` owns physical split-screen viewport layout; Camera Output assignment does not write viewport geometry.

## Setup outline

1. On the Game Application, author explicit physical Camera Outputs and their fallback rigs.
2. Create a `Camera Definition` and configure its rig behavior through a `CameraRigComposer`.
3. Configure Session Camera Assignments on the Game Application, mapping each Assignment to explicit Outputs and choosing its target and membership policy.
4. On each Actor occurrence that will be a camera target, author `ActorCameraSubjectAuthoring` and set the intended `ObservationTransform`.
5. Validate Camera authoring through the owning Inspector. Confirm every required Output mapping and fallback is explicit.

The concrete Output prefab and assignment settings must match the intended one-Output, shared or per-Player design. See IF-ADR-038 for cardinality and failure details.

## Common mistakes

- Expecting a Route or Activity to supply or select a camera.
- Equating temporary fallback display with removal of the configured Assignment.
- Assuming zero Players always means fallback.
- Leaving Output, membership or target selection implicit.
- Expecting a group rig to choose a follow/look-at subject automatically.
- Treating an Actor Profile or Actor root as a substitute for explicit Subject authoring.
- Relying on an old Camera Request/Presentation sample without migrating it to the current Assignment model.

## Public surfaces

See the [Public API Reference](../API/Public-API.md#camera). Session Camera Definition and related authoring are Experimental/current implementation surfaces; Output authoring and rig composition have separately declared API status.

## Related architecture

- [IF-ADR-038 — Session Player Camera Assignments and Occurrence Lifecycle](../Architecture/ADRs/IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md)
- [IF-ADR-019 — Session Player lifetime and Activity representation](../Architecture/ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md)
