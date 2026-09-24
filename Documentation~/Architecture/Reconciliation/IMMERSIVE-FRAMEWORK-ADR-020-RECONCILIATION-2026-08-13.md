# Immersive Framework — ADR-020 Reconciliation and Technical Certification

Status: **Closed for accepted architecture and package implementation; focused Manager-Provisioned public QA certified**  
Date: 2026-08-13  
Decision: [IF-ADR-020 — Session Player Leave and Resource Release Authority](../ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md)

## Purpose

This record closes the accepted IF-ADR-020 architecture/implementation boundary and
records the focused technical evidence used to certify public Manager-Provisioned Session
Player Leave.

It also reconciles the directly affected Player/lifetime/readiness documents.

The scope is deliberately explicit:

```text
Architecture acceptance                         Closed
Package implementation                          Closed
Documentation reconciliation                     Closed
Focused public Manager-Provisioned Leave QA      Certified — 26/26
Dedicated Scene-Provided Session Leave QA         Not separately evidenced here
FIRSTGAME real-consumer proof                    Pending Stage B
Experimental -> Stable promotion                 Not implied
```

## Baseline policy

The public repository documentation inspected for this reconciliation still described
IF-ADR-020 as Proposed and the tracker still described the feature as not implemented.
The validated runtime/QA closure occurred through subsequent local complete-file cuts.

This record therefore does **not** invent a Git commit SHA for the validated ADR-020
runtime/QA state. Repository commit identifiers remain historical baselines where already
recorded; terminal QA evidence is the authority for the focused local closure.

## Accepted authority

Session Player Leave is the explicit inverse of one Session Join occurrence:

```text
Join
  establishes one Session Player occurrence

Leave
  terminates that exact occurrence
```

It is not Activity exit, Scene-Provided contextual release, physical Actor destruction,
Closing Joining, device disconnect or Session termination.

### Target authority

Leave targets:

```text
exact Player Slot
+ expected current Session Player occurrence/revision
```

The stable Slot alone is insufficient for destructive mutation after Slot reuse.

A stale request for occurrence A cannot affect occurrence B later joined into the same
Slot.

### Scoped command authority

Leave uses the existing scoped Player consumer command model. Consumers request the
operation; Session/Player runtime remains the mutable authority.

No direct Slot mutation, global Player manager, service locator, reflection-based host
lookup or opportunistic scene search is introduced.

## Ownership

### Session

Owns:

```text
joined Logical Player occurrence
Slot occupancy
current occurrence/revision
occurrence-scoped Actor selection state
Manager-Provisioned technical Host after admitted Join
Session-visible current Player authority
```

### Activity

Owns current contextual representation:

```text
physical Actor occurrence
Activity-local gameplay/input admission
Activity-local Camera requests
readiness contribution
contextual bindings/references
```

### Scene-Provided consumer scene

Retains physical ownership of Scene-Provided Host/`PlayerInput`/Actor. IF-ADR-020 removes
Framework authority but does not convert those external objects into Framework-owned
destruction targets.

## Canonical Leave timeline

```text
Request Leave
  exact Slot + expected occurrence/revision
        ↓
validate target / scope / current correlation
        ↓
stage Leaving
        ↓
release current Activity representation, if present
        ↓
release provisioning-specific Session resources
        ↓
clear occurrence-owned Session associations
        ↓
terminal Session commit
        ↓
Slot -> Vacant / Available
        ↓
publish immutable terminal observation/result
```

### Manager-Provisioned physical settle

The package may issue Unity physical destruction as part of provisioning release while
Unity overload-null observability settles later in the player loop.

Therefore two boundaries are intentionally distinguished:

```text
logical Leave terminal
  status/result reports release + terminal commit

Unity physical destruction settle
  Host reference becomes Unity-null after canonical settle
```

The QA must wait the canonical settle before asserting physical Host absence. It must not
weaken the post-settle release invariant.

## Public result evidence

The successful focused path produced stage evidence:

```text
status='SucceededLeft'
activityReleased='True'
provisioningReleased='True'
terminalCommitted='True'
partialRelease='False'
```

The package's concrete request/result class names remain source-code authority. This
reconciliation records semantic fields/results and does not invent parallel DTO names.

## Joining policy

Joining Open/Closed controls **entry**, not exit.

Focused proof establishes:

```text
Joining Closed
+ joined P1
Request Leave P1
  -> succeeds

post Leave
  Slot Available
  Joining remains Closed
  Request Join remains blocked

later Open Joining
  -> Slot may be reused
```

No auto-reopen and no replacement auto-Join are introduced.

## Activity readiness reconciliation

The failure investigation exposed an important distinction between Session membership and
Activity projection intent.

Certified composition:

```text
Participation selection  ExplicitSlots
Requirement              GameplayReady
Zero-participant policy  Rejected
```

After the required Player leaves:

```text
Session Player occurrence removed
explicit authored Activity Slot projection remains
current contribution -> WaitingForJoin / Preparing
Activity Ready -> false
```

The incorrect shape would be either:

```text
Ready + no current required Player
```

or removing the explicit authored Slot projection merely to make readiness pass.

The correction preserves authored composition and invalidates stale occurrence readiness.

## Current-authority observation

The QA investigation also identified that retained summaries are not equivalent to live
authority.

A post-release observation may legitimately retain state objects with operational values
such as:

```text
Admission  NotAdmitted
Camera     NotEvaluated
Occupancy  Vacant
```

or equivalent released/baseline states.

Current authority must be determined from operational state + current occurrence/scope
correlation. Non-null summary presence alone is insufficient.

This rule prevents diagnostics from becoming a second mutable runtime store.

## Leave with no current Activity representation

IF-ADR-019 permits:

```text
Session Player = Joined
Current Activity Representation = Absent
```

Focused ADR-020 proof confirms Leave remains valid in that state.

The operation:

```text
skips contextual representation release
releases remaining Session/provisioning authority
commits Slot Available
creates no fake Activity representation
```

Released/baseline Camera/admission/occupancy summaries are valid evidence and do not
constitute a live gameplay chain.

## Occurrence safety and rejoin

Certified sequence:

```text
P1 occurrence A joins
A participates
Leave A succeeds
Joining remains Closed
Join attempt while Closed rejects
Joining reopens
P1 occurrence B joins as new occurrence
stale Leave A rejects
B survives
```

Slot identity is stable; Session Player occurrence identity is not reused.

## First causal divergences found during focused QA

### 1. Host destruction observation happened before Unity settle

**Owner:** QA evidence timing.

The package terminal path had already reported provisioning release/terminal commit, but
QA combined that logical terminal with immediate `host == null` observation. Unity
physical destruction is deferred.

**Correction:** reuse the existing canonical player-loop settle boundary and keep the
strong Host-absent assertion after settle.

No new lifecycle/helper/runtime authority was introduced.

### 2. Explicit Activity projection was incorrectly treated as removable Session occupancy

**Owner:** Activity projection/readiness reconciliation.

The authored `ExplicitSlots` projection is Activity intent. Session Leave removes the
Player occurrence, not the authored Slot requirement.

**Correction:** preserve explicit projection and reconcile it to `WaitingForJoin` with
`Ready=false`.

### 3. QA observer treated retained summaries as live authority

**Owner:** QA observation semantics.

Snapshot/summary existence persisted after release and was incorrectly counted as current
Activity authority.

**Correction:** current-authority checks use operational current states and occurrence
correlation.

### 4. No-Activity Leave treated baseline summaries as an active gameplay chain

**Owner:** QA observation semantics for the no-representation boundary.

`NotAdmitted`, `NotEvaluated` and `Vacant` are released/baseline evidence.

**Correction:** only genuinely current capabilities constitute live gameplay authority.
No fake representation or silent fallback was added.

## Focused QA evidence

Canonical menu entry:

```text
Immersive Framework/QA/Regressions/Player/Run ADR020-H Session Player Leave Public Manager Regression
```

Terminal result:

```text
[QA_ADR020_H_LEAVE]
status='Passed'
verdict='ADR020_H_PASS'
cases='26'
slot='PlayerSlot:player.1'
leaveAOccurrence='3'
leaveBOccurrence='8'
activityOccurrence='2'
proof='PublicLeave,ManagerProvisioned,JoiningClosed,TerminalAvailable,ResourceRelease,ReadinessInvalidation,Rejoin,StaleOccurrence,NoActivityLeave'
```

Representative completed cases include:

```text
play-mode-required
setup-confirmed
runtime-started
public-fixture-resolved
consumer-access-ready
fresh-session-confirmed
joining-opened
player-a-joined
player-a-selected
activity-entered-ready
joining-closed-before-leave
leave-a-succeeded
leave-a-stage-evidence
slot-a-available
joining-remains-closed
manager-host-authority-released
activity-stale-ready-cleared
join-blocked-while-closed
joining-reopened
player-b-rejoined-new-occurrence
stale-leave-a-rejected
player-b-survives-stale-leave
activity-cleared
leave-b-without-activity-succeeded
slot-b-available
public-scan-clean
```

This provides one-button focused public Manager-Provisioned proof for the accepted Leave
transaction.

## Scene-Provided certification scope

The accepted architecture includes Scene-Provided Leave ownership semantics:

```text
Framework authority releases
Session occurrence ends
Slot becomes Vacant / Available after required framework release
external scene-owned Host/Actor are not destroyed by Framework Leave
```

However, this reconciliation record does not contain a dedicated Scene-Provided **Session
Leave** regression terminal comparable to ADR020-H. ADR-019 Scene-Provided transition
proof establishes contextual reprojection/ownership, but contextual release is
intentionally not the same operation as Session Leave.

Therefore:

```text
Scene-Provided Leave architecture semantics     Accepted
Package boundary                               Included in ADR-020 closure
Dedicated Scene-Provided Session Leave QA       Open / not evidenced in this record
```

This is a certification-scope statement, not a package-bug finding.

## Documentation reconciliation

### IF-ADR-003

- explicit Leave now terminates one Session Player occurrence;
- Activity exit remains contextual only;
- Leave may include contextual release without collapsing lifecycles.

### IF-ADR-012

- old occurrence readiness is invalidated;
- explicit authored Slot projection remains under the certified policy;
- Activity returns to `WaitingForJoin` instead of stale Ready.

### IF-ADR-015

- `Request Leave` is added to the accepted scoped consumer vocabulary;
- explicit Slot + occurrence-safe targeting is documented;
- retained summary versus current authority is clarified.

### IF-ADR-016

- Joining controls admission only;
- Leave does not reopen Joining or auto-populate replacement;
- profile/default population is initial Session configuration, not post-Leave policy.

### IF-ADR-019

- individual Session Player Leave is no longer deferred;
- Activity exit remains not Leave;
- Session termination remains separate aggregate lifecycle.

### IF-ADR-021

- remains Proposed;
- related decision references now treat IF-ADR-019/020 as accepted facts;
- Initial Placement gains no Leave/resource ownership.

### Guide / tracker / documentation maps

- `Guides/Player-Usage.md` now documents the accepted Leave workflow and diagnostics.
- `Tracking/IF-TRACK-Framework.md` records ADR-020 as accepted/reconciled/implemented and
  focused Manager QA 26/26.
- documentation indexes now list ADR-019 through ADR-022 and ADR-020 reconciliation.

## Static validation requirements

The closure package must contain no normative stale statement equivalent to:

```text
ADR-020 is Proposed / not implemented
Session Player Leave does not exist
Session Player Leave remains a future contract under ADR-019
ADR-020 through ADR-022 all remain proposed
there is no ADR-020 reconciliation record
```

Historical statements inside dated records may remain only when explicitly labeled as
historical and followed by the 2026-08-13 closure.

## Remaining risk / open proof

1. Dedicated Scene-Provided Session Leave technical certification is not evidenced in
   this record.
2. FIRSTGAME normal consumer-surface proof is pending Stage B.
3. API maturity promotion is not implied by architecture/QA closure.
4. Proposed IF-ADR-021 and IF-ADR-022 remain independent future cuts.

## Verdict

```text
ADR-020 ARCHITECTURE                  CLOSED / ACCEPTED
ADR-020 PACKAGE IMPLEMENTATION        CLOSED
ADR-020 DOCUMENTATION RECONCILIATION  CLOSED
MANAGER-PROVISIONED PUBLIC QA         CERTIFIED — 26/26
SCENE-PROVIDED SESSION LEAVE QA       OPEN / NOT EVIDENCED HERE
FIRSTGAME REAL-CONSUMER PROOF         PENDING STAGE B
```
