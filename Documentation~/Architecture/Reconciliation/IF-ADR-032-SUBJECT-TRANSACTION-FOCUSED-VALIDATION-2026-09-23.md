# IF-ADR-032 Subject + Transaction Focused Validation — 2026-09-23

Status: **FOCUSED QA VALIDATED — Subject 9/9 / Transaction 8/8 / 17/17 total**

Normative authority:

- [IF-ADR-032 — Camera Unified Authority, Session Outputs, Presentations, Subjects and Lifecycle](../ADRs/IF-ADR-032-Camera-Unified-Authority-Session-Outputs-Presentations-Subjects-and-Lifecycle.md)

QA repository:

- `rinnocenti/QAFramework`
- menu: `Immersive Framework > QA > Camera > Run CAMERA-032 Subject + Transaction Certification`

## Scope

This record captures focused Unity Edit Mode technical evidence for the remaining IF-ADR-032 Camera Subject occurrence, stale-evidence and Presentation transaction contracts.

The certification intentionally uses transient preview scenes and the existing `CameraSharedComposition` only as the public Unity adapter over the current non-MonoBehaviour `CameraPresentationRuntime`. It does not introduce a parallel runtime, service, manager, arbitration path or production fault-injection API.

The run does not modify the canonical QA Hub, Persistent Content, GameApplication or Build Settings.

## Final Unity execution

Observed on 2026-09-23:

~~~text
Subject / stale evidence     9/9 PASS
Transaction integrity        8/8 PASS
-------------------------------------
Focused total               17/17 PASS

Terminal verdict
CAMERA_032_SUBJECT_TRANSACTION_CERTIFIED

Cleanup
TransientPreviewScenesClosed
~~~

## Subject / Presentation evidence

The 9/9 Subject/stale block proved:

~~~text
membership-zero-one-many-one-zero              PASS
required-target-release-restores-default       PASS
fixed-zero-subject-publishes                   PASS
fresh-subject-occurrence-after-rejoin          PASS
stale-availability-cannot-reactivate           PASS
stale-selection-cannot-overwrite-current       PASS
stale-membership-token-rejected                PASS
stale-presentation-rollback-rejected           PASS
stale-membership-rollback-rejected             PASS
~~~

### Deterministic membership

One Group Presentation was exercised through:

~~~text
0 -> 1 -> many -> 1 -> 0
~~~

The Presentation kept one logical request occurrence while membership changed, ordered Group membership remained deterministic, and the final zero-Subject state released normal participation back to Default.

Disposition:

~~~text
zero/one/many membership transitions are deterministic    PASS
~~~

### Required-target release

A Follow Presentation with one required Subject released its Camera request after the exact current Subject occurrence became unavailable.

Observed terminal state:

~~~text
membership count = 0
request published = false
normal request winner = none
physical Output = Default
Follow target = null
~~~

Disposition:

~~~text
required-target Presentation releases when no required Subject is current    PASS
~~~

### Fixed with zero Subjects

A Fixed Presentation published and remained physically presentable with zero Camera Subjects and without synthesizing target membership.

Disposition:

~~~text
Fixed may present with zero Subjects    PASS
~~~

### Fresh occurrence after rejoin

The same logical Camera Subject id was removed and re-added.

The second occurrence produced:

~~~text
new availability token
new membership token
new observation Transform
current Presentation consumes only the new occurrence
~~~

Disposition:

~~~text
rejoin/replacement creates fresh Subject occurrence    PASS
~~~

### Stale evidence rejection

The certification independently rejected:

~~~text
stale availability snapshot
stale explicit selection snapshot
stale membership occurrence token
~~~

None of those older evidence paths overwrote the current Subject occurrence.

Disposition:

~~~text
stale availability/selection/membership evidence is rejected    PASS
~~~

### Stale rollback rejection

Both stale Presentation-state rollback and stale membership rollback were attempted after newer evidence had already been committed.

Both were rejected and the newer state remained current.

Disposition:

~~~text
stale rollback cannot overwrite newer Presentation state    PASS
~~~

## Transaction integrity evidence

The 8/8 transaction block proved:

~~~text
rig-apply-failure-preserves-coherent-state             PASS
target-projection-failure-preserves-current-state      PASS
request-admission-failure-rolls-back                   PASS
force-default-survives-failed-admission                PASS
request-release-failure-restores-state                 PASS
release-rollback-retry-completes                       PASS
critical-rollback-failure-explicit                     PASS
owner-safe-release-preserves-other-presentation        PASS
~~~

### Rig / target failure

Invalid Rig application and unsupported target projection were injected using the existing runtime seams.

The previously coherent membership, presentation and request state remained intact.

Disposition:

~~~text
failed Rig application preserves previous coherent state    PASS
~~~

### Request admission rollback

A conflicting normal request blocked Presentation admission.

The failed Presentation request was not left admitted and the previous winner, membership and physical presentation were restored.

Disposition:

~~~text
failed request admission rolls back Presentation state    PASS
~~~

### Force-default preservation

The same failed normal admission was repeated while force-default was active.

Observed:

~~~text
force-default remains active
physical Output remains Default
normal winner remains intact
failed Presentation request is not admitted
~~~

This complements the dedicated CAMERA-032-C transition force-default lifecycle certification.

### Request release rollback and retry

A release failure was induced after a valid Follow Presentation had been published.

The runtime restored:

~~~text
published request evidence
membership
Follow target
physical previous winner
~~~

After the failure source was removed, a retry completed and returned the Output to Default.

Disposition:

~~~text
failed request release preserves recoverable previous state    PASS
~~~

### Explicit critical rollback failure

The QA injected a rollback conflict after a release failure.

The Presentation reported:

~~~text
CameraSharedCompositionReconcileStatus.CriticalRollbackFailure
IsReady = false
blocking diagnostic contains rollback evidence
~~~

Disposition:

~~~text
rollback failure is explicit terminal evidence    PASS
~~~

### Owner-safe release

Two live Fixed Presentation occurrences shared one explicit Output with independent request-owner scopes.

The certification released the non-winning occurrence through the explicit Output-session boundary, reattached it, then released the winning occurrence.

At each step the other owner's request remained admitted and arbitration restored the surviving winner correctly.

Disposition:

~~~text
owner-safe release cannot tear down another scope's Presentation    PASS
~~~

## First-run QA correction

The first run completed 15/17. Its two failures were traced to certification assertions, not product defects:

~~~text
fixed-zero-subject-publishes
  QA incorrectly expected SubjectCount = 1
  contract requires SubjectCount = 0

owner-safe-release-preserves-other-presentation
  QA used Edit Mode GameObject activation as the release trigger
  corrected to use explicit DetachOutputSession / AttachOutputSession boundary
~~~

No Framework product code changed for those corrections.

The corrected rerun completed 17/17.

## Acceptance criteria closed by this evidence

~~~text
Subject / Presentation
  [x] zero/one/many membership transitions are deterministic
  [x] required-target Presentation releases when no required Subject is current
  [x] Fixed may present with zero Subjects
  [x] rejoin/replacement creates fresh Subject occurrence
  [x] stale availability/selection/membership evidence is rejected
  [x] stale rollback cannot overwrite newer Presentation state

Transaction integrity
  [x] failed Rig application preserves previous coherent state
  [x] failed request admission rolls back Presentation state
  [x] failed request release preserves recoverable previous state
  [x] rollback failure is explicit terminal evidence
  [x] owner-safe release cannot tear down another scope's Presentation
~~~

Combined with existing CAMERA-032-E package rejoin identity tests, the fresh-occurrence proof also closes the multiplayer stale-occurrence acceptance criterion.

## Explicitly not closed by this run

This focused certification does not close:

~~~text
remaining Authoring/static acceptance criteria
CAMERA-032-F legacy removal
migration of aggregate Full Camera QA to IF-ADR-032
final Unity/QA aggregate technical certification
~~~

## Result

~~~text
Subject / stale evidence                  9/9 PASS
Transaction integrity                     8/8 PASS
Focused total                            17/17 PASS
Cleanup                                   PASS
Terminal verdict                          CAMERA_032_SUBJECT_TRANSACTION_CERTIFIED

Subject / Presentation acceptance block   TECHNICALLY CLOSED
Transaction integrity acceptance block    TECHNICALLY CLOSED
Full IF-ADR-032 certification              PENDING CAMERA-032-F + final aggregate recertification
~~~
