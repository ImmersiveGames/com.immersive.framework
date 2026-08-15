# IF-TRACK — Immersive Framework

Status: **Active — Player Certified / Camera ADR-022 Technically Certified**  
Last updated: **2026-08-15**

## Authority model

```text
Accepted ADRs
  -> normative architecture

Reconciliation records
  -> current alignment / reopen / certification evidence

Tracker
  -> mutable current delivery state

Historical certification
  -> evidence for the contract tested at that date
```

## Current reviewed repository baselines

### Package

```text
ImmersiveGames/com.immersive.framework
master
b645f8db57673cbdc3531ce12b6d399225a4d0cb
commit message: ADR22
```

This package baseline contains the ADR-022 C1-C4 implementation.

### QA

At documentation time the remote QA branch remains:

```text
rinnocenti/QAFramework
main
02f2d5589ba9bee88ac512d429f435e1dd1ba584
```

The 2026-08-15 Full Camera certification was produced from the active QA working
tree layered on that baseline, including the ADR-022 presentation QA, C9R
installer reconciliation and Full Camera orchestrator.

Synchronizing those QA changes is repository traceability work. It is not a new
framework implementation dependency and does not reopen the 53/53 result.

## Current Player architecture freeze

```text
Session owns admitted physical Player after successful admission.

Manager-Provisioned
  Framework supplies candidate
  -> Session owns after admission

Scene-Provided
  scene supplies candidate
  -> Framework adopts
  -> Session owns after admission

Activity
  owns projection / activation / gameplay / camera / readiness / contextual bindings
  owns its current Activity RuntimeContent scope
  does not own terminal physical Player lifetime

Activity A -> Activity B
  same physical Player by default
  preserve ordinary gameplay pose by default
  new contextual Activity occurrence

No Activity representation
  contextual authority may be absent
  Session physical preparation may remain authoritative

Leave / Session termination
  terminal physical release boundaries
```

Current closure record:  
[Player Physical Lifetime Recertification — 2026-08-15](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-RECERTIFICATION-2026-08-15.md)

## Full Player certification

```text
PLAYER QA CERTIFIED
mandatoryContracts = 25
executedContracts = 25
passedContracts = 25
```

Player implementation remains closed unless new evidence demonstrates a contract
regression.

## Current Camera architecture freeze

```text
Camera Output
  one persistent Session output
  explicit Unity Camera + CinemachineBrain

Camera request authority
  Session / Route / Activity / eligible Local Player
  typed publication
  deterministic arbitration
  transactional logical/physical synchronization

CameraRigComposer
  one local rig
  one local CinemachineCamera
  presentation intent/materialization
  not output authority

Presentation
  Fixed
  Follow
  Mounted
  Third Person

Materialization
  Editor-owned
  model-specific
  exact-reference ownership evidence
  preflight before mutation
  external/unknown conflicts block
  no silent fallback

Runtime output
  presentation-agnostic
```

Current technical closure record:  
[Camera Presentation Technical Certification — 2026-08-15](../Reconciliation/IMMERSIVE-FRAMEWORK-CAMERA-PRESENTATION-TECHNICAL-CERTIFICATION-2026-08-15.md)

## Full Camera certification

Terminal result:

```text
CAMERA QA CERTIFIED
mandatoryCases = 53
executedCases = 53
passedCases = 53
```

Breakdown:

```text
ADR-022 Presentation Models    14/14
C9R canonical authority        11/11
ADR-004B negative integrity    18/18
ADR-004C owner lifetime        10/10
                              -----
aggregate                       53/53
```

Supporting existing Follow pipeline:

```text
C9M Follow Pipeline             6/6
```

## ADR status

| ADR | Current architecture status | Implementation / QA disposition |
|---|---|---|
| 001 | ACCEPTED / RECONCILED | Core composition unchanged |
| 002 | ACCEPTED | No current implementation dependency |
| 003 | ACCEPTED / RECONCILED | Player Session physical lifetime certified |
| 004 | ACCEPTED / RECONCILED / RECERTIFIED | Single-output authority preserved; Full Camera 53/53 |
| 005 | ACCEPTED | No current implementation dependency |
| 006 | ACCEPTED | No current implementation dependency |
| 007 | ACCEPTED / RECONCILED | Player readiness boundary certified |
| 008 | ACCEPTED | No current implementation dependency |
| 009 | ACCEPTED | No current implementation dependency |
| 010 | ACCEPTED / RECONCILED FOR CAMERA | ADR-022 Class C Inspector/materialization conforms |
| 011 | ACCEPTED / RECONCILED FOR PLAYER BOUNDARY | No false Ready |
| 012 | ACCEPTED / RECONCILED | Context projection separated from physical lifetime |
| 013 | ACCEPTED / EXPERIMENTAL | Technical boundary certified; FIRSTGAME promotion remains |
| 014 | ACCEPTED | No current implementation dependency |
| 015 | ACCEPTED / RECONCILED | Player public command/observation surface certified |
| 016 | ACCEPTED / RECONCILED | Provisioning origin model certified |
| 017 | ACCEPTED | No current implementation dependency |
| 018 | ACCEPTED | No current implementation dependency |
| 019 | ACCEPTED / RECONCILED / RECERTIFIED | Session physical identity/lifetime certified |
| 020 | ACCEPTED / RECONCILED / RECERTIFIED | Leave/termination certified |
| 021 | ACCEPTED / RECONCILED / CERTIFIED | Initial Placement 9/9 + Full Player PASS |
| 022 | ACCEPTED / IMPLEMENTED / TECHNICALLY CERTIFIED | C1-C5 closed; Full Camera 53/53; C6 FIRSTGAME pending |

## Player lifetime work closure

```text
PLR-01 Physical ownership model               CLOSED
PLR-02 Scene-Provided adoption/promotion       CLOSED
PLR-03 Activity contextual handoff             CLOSED
PLR-04 Inactive no-Activity state              CLOSED
PLR-05 Leave                                   CLOSED
PLR-06 Initial Placement                       CLOSED
PLR-07 Focused QA / recertification            CLOSED
```

## Camera R4 / ADR-022 closure

### CAM-R4-C1 — Presentation contracts — CLOSED

```text
Undefined = 0
Follow = 10
Fixed = 20
Mounted = 30
ThirdPerson = 40
```

### CAM-R4-C2 — Safe materialization ownership — CLOSED

Exact-reference Framework ownership and `ExternalOrUnknown` preservation are
implemented.

### CAM-R4-C3 — Model materializers — CLOSED

```text
Follow
Fixed
Mounted
Third Person
```

all have explicit supported technical materialization.

### CAM-R4-C4 — Inspector / UX — CLOSED

Designer-selectable model-specific CameraRigComposer surface and Advanced /
Diagnostics evidence are implemented.

### CAM-R4-C5 — Technical QA — CLOSED / CERTIFIED

```text
Full Camera QA 53/53
```

### CAM-R4-C6 — FIRSTGAME consumer proof — PENDING

Required proof remains real consumer usage and usability/integration evidence.

C6 is not a package implementation task unless FIRSTGAME reveals a concrete
defect.

## Camera negative-path semantic clarifications

### Local presentation is not output authority

A Fixed, Follow, Mounted or Third Person rig uses the same request/output
authority.

Presentation never grants priority.

### External component protection

```text
unknown/external incompatible Body or Aim
  -> block
  -> diagnose
  -> preserve
```

Do not delete to make Apply/Rebuild succeed.

### Compatible external state is not ownership

A compatible component may be used without becoming Framework-owned.

### Blocked switch is transactionally non-partial

A cross-stage conflict must be found before owned Body/Aim replacement begins.

### Unsupported model

Unknown Presentation does not fallback to Follow.

## Known non-blocking QA hygiene

The certified Full Camera run emitted exactly three Unity warnings:

```text
The referenced script (Unknown) on this Behaviour is missing!
```

during C9R teardown.

They did not produce a `Failed` or `Blocked` case and the run completed 53/53.

Classification:

```text
QA fixture authoring hygiene
non-blocking
not package behavior
not ADR-022 certification failure
```

The QA fixture should be cleaned in a small QA-only hygiene cut.

## Historical certification policy

Do not rewrite historical dated Player or Camera certification records to imply
they tested later contracts.

Current revised authorities:

```text
Player lifetime
  Player Physical Lifetime Recertification — 2026-08-15

Camera presentation expansion
  Camera Presentation Technical Certification — 2026-08-15
```

## FIRSTGAME

Technically certified framework boundaries:

```text
Player
  ready for consumer validation

Camera ADR-004 + ADR-022 C1-C5
  ready for consumer validation
```

Current consumer-side promotion work may include:

```text
ADR-013 Audio real-game integration
ADR-022 Camera C6
Player real-game integration/usability
```

FIRSTGAME remains consumer proof, not the primary technical smoke harness.
