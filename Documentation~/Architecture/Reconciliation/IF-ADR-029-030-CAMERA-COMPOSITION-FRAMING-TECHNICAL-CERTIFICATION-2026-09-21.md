# IF-ADR-029 / IF-ADR-030 Camera Composition and Framing Technical Certification — 2026-09-21

Status: **TECHNICALLY VALIDATED / CERTIFIED**

Scope:
- IF-ADR-029 — Camera Composition, Group Presentation and Camera View Removal
- IF-ADR-030 — Camera Subject Framing Evidence

Consumer prerequisite:
- LocalMultiplayer current Group Camera consumer manual Play Mode PASS — 2026-09-20

## 1. Certification basis

This record certifies the current post-CAMERA-029 / CAMERA-030 architecture boundary from
the evidence that actually executed successfully in the QAFramework Camera rail.

Current model:

```text
Camera Subject(s)
  -> Camera Composition
  -> CameraRigComposer
  -> CameraRequest
  -> CameraOutputSession
  -> Camera Output
```

Group framing extension:

```text
Target.Object = Subject.Observation
Target.Radius = Subject.FramingRadius when specified
              = GroupBehavior.MemberRadius otherwise
```

## 2. Structural evidence

The Full Camera run entered runtime only after the current structural regression completed:

```text
structuralCases='10/10'
```

This provides current evidence for the authored Composition / Output topology used by the
CAMERA-029 boundary.

## 3. Shared Camera evidence

The current Shared proof completed all ten causal cases:

```text
1  shared-baseline-zero-subject-default
2  p1-join-fallback-framing
3  invalid-camera-authoring-rejected
4  fallback-root-subject
5  stale-replacement-rejected
6  explicit-replacement-framing-radius
7  p2-two-member-group
8  p1-leave-shrinks-group
9  p1-rejoin-fresh-occurrence
10 cleanup-zero-subject-default
```

Terminal evidence:

```text
phase='shared'
status='Passed'
cases='10/10'
playerIndependence='PASS'
fallbackObservation='PASS'
staleReplacement='PASS'
subjectFraming='ExplicitAndFallbackPASS'
idempotence='PASS'
explicitChild='True'
staleP1A='False'
compositionRequest='Explicit'
cleanup='TerminalClean'
```

This directly proves the current Group lifecycle and the CAMERA-030 explicit/fallback framing
contract.

## 4. Generic request/output evidence

The same run completed the current generic arbitration fixture:

```text
status='Passed'
phase='canonical-override-fixture'
cases='11/11'
```

The completed set included:

```text
default-presentation
activity-request
route-request
session-request
session-release-restores-route
route-release-restores-activity
activity-release-restores-default
duplicate-request
duplicate-release
activity-lifecycle-cleanup
route-lifecycle-cleanup
```

This supplies current request/output evidence for the IF-ADR-029 Composition participation
boundary rather than relying only on the LocalMultiplayer consumer.

## 5. Consumer evidence

LocalMultiplayer manual Play Mode on 2026-09-20 independently confirmed the current
application consumer:

```text
dedicated Group ActorProfiles / Presentations
-> ActorCameraSubjectAuthoring
-> shared Camera Composition
-> Group presentation
-> current Camera Output

two Players
-> both prepared / gameplay-ready
-> Group Camera functional

per-Subject framing
-> functional in consumer authoring
```

The consumer record remains:

[IF-ADR-030 Local Multiplayer Consumer Unity Proof — 2026-09-20](IF-ADR-030-LOCAL-MULTIPLAYER-CONSUMER-UNITY-PROOF-2026-09-20.md)

## 6. Full Camera harness distinction

The broader Full Camera run continued into historical ADR-004B negative-integrity coverage
after all evidence listed above had already passed.

It then reported:

```text
case='17-duplicate-output-id'
status='Failed'
```

The failure is in the QA harness adapter contract:

```text
producer evidence:
  duplicate-output-id-rejected

legacy consumer expectation:
  duplicate-output-id
```

The underlying duplicate-Output structural behavior had already executed inside the current
10/10 structural regression. The later failure therefore does not demonstrate a
CAMERA-029/030 runtime defect.

This record does **not** relabel the complete historical Full Camera suite as certified.
It certifies only the current IF-ADR-029 and IF-ADR-030 boundaries from their direct executed
evidence.

## 7. Disposition

```text
IF-ADR-029 implementation                PASS
IF-ADR-029 consumer integration          PASS
IF-ADR-029 structural QA                 10/10 PASS
IF-ADR-029 Shared Group lifecycle        10/10 PASS
IF-ADR-029 generic request arbitration   11/11 PASS
IF-ADR-029 terminal Shared cleanup       PASS
IF-ADR-029 technically validated         YES
IF-ADR-029 certified                     YES

IF-ADR-030 implementation                PASS
IF-ADR-030 consumer integration          PASS
IF-ADR-030 explicit Subject radius       PASS
IF-ADR-030 Group fallback radius         PASS
IF-ADR-030 fresh occurrence evidence     PASS
IF-ADR-030 terminal cleanup              PASS
IF-ADR-030 technically validated         YES
IF-ADR-030 certified                     YES

Full historical Camera aggregate         NOT RELABELED BY THIS RECORD
legacy ADR-004B harness adapter cleanup  MAINTENANCE / NON-BLOCKING FOR 029/030
```
