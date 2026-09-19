# IF-ADR-026 — Camera Subjects, Composition and Multi-Output Topology

Status: **Accepted / reconciled by IF-ADR-029**
Accepted: **2026-09-07**
Reopened: **2026-09-12**
Reconciled: **2026-09-19**
Type: architecture / Camera subjects / multi-output
Normative successor: [IF-ADR-029](IF-ADR-029-Camera-Composition-Group-Presentation-and-Camera-View-Removal.md)

Historical certification records:

- [Camera Full Technical Certification — 2026-09-12](../Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)
- [Camera Full Technical Certification — 2026-09-16](../Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-16.md)
- [Shared Camera Technical Certification — 2026-09-09](../Reconciliation/IF-ADR-026-SHARED-CAMERA-TECHNICAL-CERTIFICATION-2026-09-09.md)

Those reports remain dated evidence for the boundaries they executed. They are not relabeled as IF-ADR-029 certification.

## Context

IF-ADR-026 separated observable entities, local presentation and physical Outputs, introduced explicit Subject availability and accepted `1..N` Outputs. IF-ADR-029 later removed the redundant intermediate authority and made Composition the owner of Subject membership and request participation.

## Current decision

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

### Camera Subject

A Subject is typed evidence that something may be observed. It owns neither Player lifetime nor a request, rig, Output or physical layout.

`ActorCameraSubjectAuthoring` exposes the exact observation Transform for one Actor occurrence. Destroying or replacing that occurrence removes its Subject evidence; stale occurrence evidence cannot become current again.

### Camera Composition

Composition owns:

- selection of `1..N` available Subjects;
- membership revision and foreign/stale snapshot rejection;
- projection of current presentation input;
- application/clear of one explicit non-Default rig;
- publication/release of its normal request against one explicit Output.

Membership, presentation and request participation form one transaction. Failed forward mutation restores the previous membership, presentation and request/output state. Rollback failure remains explicit.

### Rig and presentation

`CameraRigComposer` consumes already-resolved Composition input. It owns local Cinemachine materialization and provenance only; it does not discover Subjects or decide whether it wins an Output.

The accepted presentation family is `Fixed`, `Follow`, `Mounted`, `ThirdPerson` and `Group`. `Follow` is single-target. `Group` accepts one or more Subjects and owns group membership/framing settings.

### Camera Output

Each Output owns an exact `CameraOutputDefinition`, Unity `Camera`, `CinemachineBrain`, target-independent Default Rig and `CameraOutputSession`.

Session composition supports explicit `1..N` Outputs. Player count does not determine Output count. Missing or duplicate Output identity blocks explicitly.

## Player integration

Ordinary Player/Actor participation contributes Subject evidence only. Player provisioning, Actor lifecycle, input ownership and Camera participation remain separate.

Explicit Player Slot→Output policy may assign the exact physical Camera to `PlayerInput.camera`. Unity `PlayerInputManager` remains responsible for split count, `Camera.rect` and split recomposition.

## Rejected

- implicit current Camera or first Player;
- `Camera.main`, name/tag/hierarchy lookup or service locator;
- Player-count-derived Outputs;
- ordinary per-Player gameplay requests;
- Composition and Default sharing the same rig;
- physical layout state inside Camera membership/request topology.

## Current implementation coverage

CAMERA-029 A–E are committed in the Framework. CAMERA-029-F documentation and consumer migration are implemented locally. Static validation may support the migration, but Unity execution and technical certification remain pending.
