# IF-ADR-027 — Camera Authoring Definitions and Composition Authority

Status: **Accepted / reconciled by IF-ADR-029**
Accepted: **2026-09-12**
Reconciled: **2026-09-19**
Type: architecture / product authoring / Camera composition
Normative successor: [IF-ADR-029](IF-ADR-029-Camera-Composition-Group-Presentation-and-Camera-View-Removal.md)

Historical certification records remain unchanged:

- [Camera Full Technical Certification — 2026-09-12](../Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)
- [Camera Full Technical Certification — 2026-09-16](../Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-16.md)
- [Camera consumer usage reconciliation — 2026-09-11](../Reconciliation/IF-ADR-026A-Camera-Consumer-Usage-Documentation-Reconciliation-2026-09-11.md)

These are dated evidence for their original boundaries and do not certify IF-ADR-029.

## Context

Typed authored assets remain preferable to copied technical identity. The final architecture needs reusable Output identity and rig behavior definitions, but does not require a separate reusable definition between Composition and request participation.

## Decision

Normal authoring uses:

```text
ActorCameraSubjectAuthoring
  -> exact observation Transform

CameraSharedComposition
  -> Subject policy
  -> CameraOutputDefinition
  -> non-Default CameraRigComposer
  -> request precedence

CameraRigComposer
  -> CameraRigBehaviorDefinition
  -> local CinemachineCamera

CameraOutputAuthoring
  -> CameraOutputDefinition
  -> Unity Camera
  -> CinemachineBrain
  -> Fixed Default CameraRigComposer
```

### Output identity

`CameraOutputDefinition` remains reusable authored identity. Exact asset reference is authoring authority; `CameraOutputId` remains runtime/diagnostic evidence. Removing the former intermediate concept does not remove Output identity.

### Rig behavior

One behavior definition owns settings for exactly one presentation intent. `GroupCameraRigBehaviorDefinition` owns group membership/framing settings. `FollowCameraRigBehaviorDefinition` owns only single-target Follow settings.

### Composition

`CameraSharedComposition` is the authored/runtime owner of Subject membership and request participation. Its Composition Rig must be explicit and distinct from the Output Default Rig.

No `CameraCompositionDefinition` asset is required by the accepted architecture. Reusable Composition assets remain deferred until independent consumer evidence justifies them.

## Editor surface

Editor Apply/Rebuild owns materialization and preserves exact provenance. Runtime assemblies do not depend on Editor assemblies. Inspectors expose typed asset references and designer intent; stable IDs stay in diagnostics rather than becoming copied authoring input.

## Rejected

- copied identity strings as normal authoring;
- dummy compatibility assets for removed concepts;
- silent target/output discovery;
- a mega Camera asset combining Subject, behavior, request, Output and layout;
- using the gameplay rig as Output Default;
- Editor-only materialization authority in runtime.

## Current implementation coverage

The Framework product surfaces are implemented through CAMERA-029-E. CAMERA-029-F updates current docs and real consumer assets locally. Unity import/serialization and lifecycle validation remain pending.
