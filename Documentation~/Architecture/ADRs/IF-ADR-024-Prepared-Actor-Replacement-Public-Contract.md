# IF-ADR-024 — Prepared Actor Replacement Public Contract

Status: **Accepted — Manager-Provisioned V1 implemented and Player QA certified**  
Accepted: **2026-09-02**  
Last updated: **2026-09-02**  
Type: architecture / Player public capability / runtime orchestration  
Related decisions: IF-ADR-003, IF-ADR-007, IF-ADR-015, IF-ADR-019, IF-ADR-020, IF-ADR-023

## Context

The delivered `RequestReplaceActorSelection` command changes only Session Actor
intent before preparation. It correctly rejects mutation once a prepared Actor
exists; it must not become physical hot-replacement.

The internal `TryReplacePreparedActor` primitive materializes, selects, activates
and retires a Framework-owned prepared Actor. It does not own Activity gameplay
teardown, reprojection or readiness. Promoting that primitive directly would expose
internal staging and can leave Actor A gameplay authority current after Actor B
becomes prepared.

## Decision

Prepared Actor replacement is a distinct public capability. It replaces prepared
Actor A with prepared Actor B in the same Player and Activity occurrence, through
a Framework-owned orchestration operation.

```text
Prepared A
  → release A contextual gameplay authority
  → release A physical gameplay occupancy when A will be replaced
  → canonical prepared-Actor replacement A → B
  → establish B contextual gameplay authority
  → reconcile readiness for the same Activity occurrence
```

This is neither selection mutation, Leave + Join, a new Player occurrence, Local
Player Host replacement, nor Session reassignment.

### Authority model

| Concern | Authority |
|---|---|
| Actor selection | `PlayerParticipationRuntimeContext` |
| Prepared Actor materialization/replacement | `PlayerActorPreparationRuntimeContext` |
| Activity gameplay chain | `PlayerGameplayCurrentContextRuntime` and `ActivityPlayerActorLifecycleParticipant` |
| Activity readiness | `ActivityPlayerActorLifecycleParticipant` |
| Scoped public intent | `IPlayerSessionScopedAccess` |

`IPlayerSessionScopedAccess` expresses intent and returns typed evidence. It does
not transfer ownership to consumers. A caller never invokes gameplay release,
prepared-Actor replacement or gameplay ensure stages itself. The orchestration
uses the existing ownership paths and must not duplicate input-consumer release,
camera/admission/occupancy release, reader resolution, or Actor materialization.

The implemented public operation is `RequestReplacePreparedActor(...)`; its request
and result surfaces are `PlayerPreparedActorReplacementRequest` and
`PlayerPreparedActorReplacementResult`.

### V1 provisioning boundary

V1 supports **Manager-Provisioned only**. The Framework owns that physical Actor
lifetime and can transact its replacement while preserving the Local Player Host,
`PlayerInput` and Manager-Provisioned Host assignment.

V1 rejects Scene-Provided explicitly and without mutation. Scene-Provided physical
composition may have authored/external ownership; the Framework must not silently
destroy or replace it. Scene-Provided replacement requires a future explicit
physical-ownership contract.

### Same-occurrence invariants

A successful or physically committed replacement preserves:

- Session identity, `PlayerSlotId`, Player occurrence and joined membership;
- `LocalPlayerHost`, `PlayerInput` and Manager-Provisioned Host assignment;
- current Activity identity and current Activity occurrence.

It changes the selection revision, general Slot revision, prepared Actor identity
and preparation token, Actor Runtime Host/Presentation, gameplay
admission/input/camera evidence, and the readiness evidence needed to prove B. No
gameplay authority for A may remain current after B is committed.

The general Slot revision is mutable revision evidence. It must not be treated as
immutable Player occurrence identity across Actor selection/replacement.

### Readiness and input gate

`JoinedSlots` is unchanged. `LogicalActorsPrepared` is satisfied by B's current
preparation evidence. For `GameplayReady`, releasing A invalidates A's evidence;
the current Activity occurrence is reconciled exclusively against B and never
reuses A's readiness proof.

If B achieves valid admission, readiness follows the existing policy. If it does
not, the Activity remains current and is `NotReady`; Actor change never creates an
Activity occurrence. A blocked input gate does not bypass this rule: B may be
selected/prepared with the existing blocked-admission state, but replacement never
forces the gate open or equates preparation with `GameplayReady`.

### Occupancy transition

Activity exit and prepared Actor replacement have different physical-lifetime
semantics.

Activity exit may release contextual admission, camera, input and reader evidence
while preserving the physical Session-owned Actor and its occupancy. Prepared Actor
replacement retires that physical Actor; therefore A's occupancy must end before B
can confirm occupancy for the same Slot.

The replacement orchestration releases only the occupancy correlated to A's exact
prepared-Actor evidence. Foreign or stale occupancy remains a conflict and is not
silently overwritten.

### Failure semantics

Before B commits, stale/invalid correlation rejects without mutation. Once release
of A's gameplay authority has started, any pre-commit failure that leaves A as the
current prepared Actor must restore A through the canonical gameplay projection
path before returning the original failure.

Materialization or selection-commit failure retains A and retires B when
materialized. Activation failure restores A only where the canonical physical
primitive safely guarantees it. If physical rollback is ambiguous, or if A's
GameplayReady authority cannot be safely restored, the public operation reports a
rollback failure rather than forcing stale authority.

The pre-commit invariant is:

```text
before
  A Prepared + GameplayReady

replacement fails before B commit

after successful restoration
  A Prepared + GameplayReady
```

After B has been selected, activated and registered as the current prepared Actor,
the operation is physically committed. A later gameplay reprojection failure does
not reconstruct A to simulate atomic rollback: B remains selected/prepared, the
same Player and Activity occurrences remain current, A gameplay authority remains
released, and the Activity is `NotReady`.

If release of A fails after commit, B remains current and authoritative. A may be
physically retained solely for diagnostics/cleanup; it must not retain current
Player or gameplay authority. The result distinguishes B current from A retained
non-authoritatively.

For reader cardinality, B with one valid reader admits normally with a new binding.
B with zero readers follows the current endpoint policy without a fabricated
fallback. B with more than one reader leaves A unbound, admits no B reader, reports
reprojection failure and leaves the Activity `NotReady`; it never restores A
gameplay simply because B failed after commit.

### Public correlation and observability

The request rejects stale public intent using only correlation consumers may
legitimately observe, including Slot identity and public Session/selection revision
evidence. Internal preparation and lifecycle tokens remain Framework-owned unless a
separate API design establishes them as public request correlation.

The orchestration resolves internal correlations from the validated scoped context.
The typed result and scoped observation prove, without parsing a message:

- rejected, rolled back, physically committed, reprojected, degraded, or cleanup-pending outcome;
- previous and current Actor;
- preservation of the same Player/Activity ownership boundary;
- gameplay reprojection success/failure and current readiness evidence;
- retained old-Actor cleanup when applicable.

## Accepted scope

- Public intent through `IPlayerSessionScopedAccess` and internal Activity-scoped orchestration.
- Manager-Provisioned prepared Actor replacement in the same occurrence.
- Typed outcome and observation sufficient for consumer and QA proof.
- Reuse of existing internal replacement and gameplay ownership paths.
- Exact old-preparation occupancy release before B assumes the same Slot.
- Canonical restoration of A gameplay after recoverable pre-commit failures.

## Rejected scope

- Mechanical public exposure of `TryReplacePreparedActor`.
- Changing `RequestReplaceActorSelection` semantics after preparation.
- Leave/Join, Host replacement, Session reassignment or synthetic Activity occurrence.
- Scene-Provided replacement in V1.
- Input-gate bypass, fallback reader fabrication, foreign occupancy overwrite, or free-form diagnostic-only evidence.

## Consequences

Implementation uses a dedicated orchestration wrapper with the current Activity
lifecycle/readiness authority rather than a generic manager. Existing selection
commands and their rejection contract remain unchanged.

The canonical QA uses `QA_DefaultActor` as A and `QA_AlternateActor` as B. Its
positive same-occurrence proof verifies same P1/Slot/Host/Input/Activity occurrence,
B selected/prepared/current, A non-authoritative, reader A unbound, B
admission/token/reader evidence, correct readiness reconciliation, P2 availability
and Joining non-interference.

The positive proof must not substitute Leave + Join, call internal replacement
primitives, manually release gameplay on behalf of the caller, or weaken occupancy
conflict detection.

## Current implementation coverage

Manager-Provisioned V1 is implemented on the public scoped-access surface.

The implemented runtime transaction:

```text
release contextual gameplay A
→ release occupancy A by exact preparation ownership
→ replace physical prepared Actor A → B
→ ensure current gameplay B
→ reconcile readiness for the same Activity occurrence
```

Recoverable failures after gameplay release but before B's physical commit restore
A through canonical `TryEnsureCurrentGameplay`. Post-commit gameplay reprojection
failure keeps B authoritative and returns a typed committed/degraded result. Old-A
physical cleanup remains separately observable through cleanup-pending evidence.

The current Full Player QA positive path is certified:

```text
[QA_PLAYER_FULL]
status='Passed'
verdict='PLAYER QA CERTIFIED'
cases='16/16'
completed='access,join,observation,actor-default,actor-lifecycle,gameplay-ready-reader,reader-cardinality,actor-replace,second-player,joining-control,commands,leave,rejoin,negatives,spatial,relocation'
```

Current dated technical evidence:

[IF-ADR-024 — Prepared Actor Replacement Technical Certification — 2026-09-02](../Reconciliation/IF-ADR-024-PREPARED-ACTOR-REPLACEMENT-TECHNICAL-CERTIFICATION-2026-09-02.md)

The `16/16` record proves the Manager-Provisioned positive replacement and the
immediately dependent Player suite boundary executed in that run. It does not
claim Scene-Provided prepared Actor replacement.

## Deferred decisions

- Scene-Provided physical ownership and replacement contract for a future capability.
- Additional focused negative certification for failure branches beyond the current integrated Player QA evidence, where separately required.
