# IF-ADR-026 — Shared Camera Technical Certification — 2026-09-09

Status: **CERTIFIED — Shared Camera composition boundary only**
Historical boundary: IF-ADR-026 — Camera Subjects, Assignment and Multi-Output Topology  
Current normative authority: [IF-ADR-032 — Camera Unified Authority, Session Outputs, Presentations, Subjects and Lifecycle](../ADRs/IF-ADR-032-Camera-Unified-Authority-Session-Outputs-Presentations-Subjects-and-Lifecycle.md)

> Historical evidence only. This record certifies the former IF-ADR-026 boundary and does not certify IF-ADR-032.
Evidence owner: `QAFramework/Assets/ImmersiveFrameworkQA/Camera/Scripts/Runtime/QaC9RCameraOverrideAuthorityFixture.cs`

> Shared certification is not complete IF-ADR-026 certification. Split runtime, the Full
> Camera aggregate and FIRSTGAME consumer proof remain pending.

## Certified runtime boundary

| Runtime evidence | Result |
|---|---|
| Persistent Camera baseline | PASS |
| Persistent Output injection | PASS |
| Persistent Camera Subject availability injection | PASS |
| IF-ADR-026 composition readiness | PASS |
| P1 explicit-device Join | PASS |
| P1 active-Activity late-join reconciliation | PASS |
| P1 Actor preparation and Camera Subject publication | PASS |
| Shared membership `{P1}` | PASS |
| P2 explicit isolated-device Join and Actor preparation | PASS |
| Shared membership `{P1,P2}` | PASS |
| P1 Leave and shared membership `{P2}` | PASS |
| P1 rejoin as new occurrence P1-B and membership `{P1-B,P2}` | PASS |
| Stale P1-A Subject absent | PASS |
| Ordinary Player-owned CameraRequests | PASS — `0` |

Canonical runtime result:

```text
[QA_CAMERA_ADR026]
phase='shared'
status='Passed'
view='camera.view.main'
output='camera.output.main'
subjects='P1-A/P2 -> P2 -> P1-B/P2'
staleP1A='False'
ordinaryPlayerRequests='0'
```

No aggregate case count is assigned by this record because the Shared result does not
publish one as authoritative evidence.

## Architectural contract proven

```text
Player / Actor
  -> contributes Camera Subject

CameraSharedComposition
  -> owns explicit Shared View membership

View
  -> 0..N current Subjects

CameraRigComposer
  -> observes the resolved Subject set

CinemachineTargetGroup / CinemachineGroupFraming
  -> technical multi-target projection

Camera Output
  -> remains one explicit physical Output
```

The runtime proof establishes that P1 Join does not create an Output, P2 Join does not
create a second Output, P1 Leave does not destroy the View/Rig/Output, and P1-B creates a
new Subject occurrence without reviving stale P1-A evidence.

The normative invariant remains:

> Player count does not determine Camera Output count.

## Framework reconciliations exposed by certification

### Persistent Camera consumer injection

Before the correction, persisted Content roots survived source-scene unload, while
Camera injectors created afterward searched only loaded scenes and skipped those
consumers. The corrected owner is explicit persistence composition:

```text
GlobalUiSceneRuntime.PersistedRoots
  -> explicit CameraOutput injection
  -> explicit CameraSubjectAvailability injection

future loaded scenes
  -> existing sceneLoaded injection
```

Successful IF-ADR-026 composition readiness validates this correction at runtime.

### Active Activity late-join reconciliation

Before the correction, an `AllJoinedSlots` Activity entering with `{}` and
`ZeroParticipants = Allowed` completed readiness empty and did not incorporate a later
Join. The corrected lifecycle is:

```text
Session typed change
  -> active Activity reconciliation
  -> AllJoinedSlots projection update
  -> default Actor selection
  -> physical preparation
  -> contextual projection
  -> gameplay admission/readiness
  -> Camera Subject publication
```

P1 and P2 joining during the same active Activity occurrence validate this correction.
It does not redefine Camera Subject as a Join artifact:

```text
Joined Player != Prepared Actor != Camera Subject
```

These are implementation corrections exposed by certification, not new IF-ADR-026
architecture.

## QA evidence integrity corrections

The certification fixture was corrected to preserve pre-existing Joining state and to
close Joining only when it opened that policy. It also provisions distinct P1 and P2 QA
Keyboards before the first Join, uses explicit device ownership for both Players and
reuses the exact P1 Keyboard for P1-B. No device stealing or implicit Input System
assignment is accepted.

These corrections belong to QA evidence integrity, not Framework product behavior.

## Completed boundary

- CAMERA-026-A through CAMERA-026-H implementation;
- Shared multiplayer Camera runtime proof;
- P1/P2 Join, Actor preparation and multi-Subject framing;
- Leave and new-occurrence rejoin;
- stale Subject removal;
- zero ordinary Player Camera requests;
- one persistent Output throughout Shared composition.

## Pending boundary

- Split runtime certification;
- explicit two-Output viewport runtime proof;
- completion of the Full Camera aggregate;
- generic arbitration QA composition repair if it still blocks the aggregate;
- FIRSTGAME multiplayer consumer proof;
- secondary View proof if still required by IF-ADR-026.

## Frozen status

```text
IF-ADR-026
Architecture                  ACCEPTED
A-H implementation            COMPLETE
Shared technical QA           CERTIFIED — 2026-09-09
Split technical QA            PENDING
Full Camera aggregate         PENDING
FIRSTGAME proof               PENDING
```
