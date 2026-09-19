# IF-ADR-028 — Camera Output Participation and Physical Presentation Ownership

Status: **Accepted / current with IF-ADR-029 participation reconciliation**
Accepted: **2026-09-12**
Reconciled: **2026-09-19**
Type: architecture / Camera output / physical presentation / integration
Preserves: IF-ADR-004 request arbitration and Output-owned Default semantics
Current participation authority: [IF-ADR-029](IF-ADR-029-Camera-Composition-Group-Presentation-and-Camera-View-Removal.md)

Historical evidence remains unchanged:

- [Camera Full Technical Certification — 2026-09-12](../Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)
- [Camera Full Technical Certification — 2026-09-16](../Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-16.md)
- [Focused physical presentation / PlayerInput validation — 2026-09-17](../Reconciliation/IF-ADR-028-FOCUSED-VALIDATION-2026-09-17.md)

Those reports certify only the dated boundaries they executed. They are not IF-ADR-029 certification.

## Context

Physical Output availability, normal request participation and screen layout are distinct concerns. Framework Camera must not derive layout from Subjects, Composition, requests, Output count or Player count.

## Decision

### Output participation

Composition participates directly through a normal `CameraRequest` addressed to an exact `CameraOutputDefinition`/`CameraOutputId`.

```text
Composition Rig
  -> CameraRequest
  -> CameraOutputSession
  -> selected physical presentation
```

The Output owns registration, arbitration, Default selection and physical rig application. There is no parallel participation topology.

### Output selection

```text
force-default owner active
  -> Default Rig

otherwise normal request winner exists
  -> winner Rig

otherwise
  -> Default Rig
```

Default is target-independent, has no request identity and carries no precedence.

### Physical presentation

Framework Camera does not own `Camera.rect`, `pixelRect`, target display, target texture, safe area, PiP or screen partitioning. External presentation remains preserved unless an explicit owner outside Camera request topology changes it.

### PlayerInputManager split-screen

```text
explicit Player Slot -> Camera Output policy
  -> PlayerInput.camera = exact Output Camera
  -> PlayerInputManager owns split count and Camera.rect recomposition
```

`PlayerInputManager` does not own Subject membership, Composition, rig presentation, Output identity or request arbitration.

### Multi-output

Session composition may register `1..N` explicit physical Outputs. An Output may be registered with no current normal request; it still presents its Default. Output count is never inferred from Player count.

## Rejected

- Camera-domain viewport writer;
- Output participation inferred from registration count;
- implicit first Output;
- alternate participation topology beside normal requests;
- gameplay and Default sharing one rig;
- `Camera.main`, scene discovery or service locator.

## Current implementation coverage

CAMERA-028-C/D behavior and Player Slot→Output integration remain preserved. CAMERA-029 replaces participation with Composition-owned normal requests without changing `PlayerInputManager` layout authority. Unity revalidation of the combined final boundary remains pending.
