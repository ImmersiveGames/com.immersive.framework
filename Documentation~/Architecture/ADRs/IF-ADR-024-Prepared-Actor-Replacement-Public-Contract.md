# IF-ADR-024 — Prepared Actor Replacement Public Contract

Status: **Accepted — runtime implementation and QA pending**  
Accepted: **2026-09-02**  
Last updated: **2026-09-02**  
Type: architecture / Player public capability / runtime orchestration  
Related decisions: IF-ADR-003, IF-ADR-007, IF-ADR-015, IF-ADR-019, IF-ADR-020, IF-ADR-023

## Context

The delivered `RequestReplaceActorSelection` command changes only Session Actor
intent before preparation. It correctly rejects mutation once a prepared Actor
exists; it must not become physical hot-replacement.

The internal `TryReplacePreparedActor` primitive already materializes, selects,
activates and retires a Framework-owned prepared Actor. It does not own Activity
gameplay teardown, reprojection or readiness. Promoting that primitive directly
would expose internal staging and can leave Actor A gameplay authority current
after Actor B becomes prepared.

## Decision

Prepared Actor replacement is a distinct public capability. It replaces prepared
Actor A with prepared Actor B in the same Player and Activity occurrence, through
a Framework-owned orchestration operation.

```text
Prepared A
  → release A contextual gameplay authority
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

It changes the selection revision, prepared Actor identity and preparation token,
Actor Runtime Host/Presentation, gameplay admission/input/camera evidence, and the
readiness evidence needed to prove B. No gameplay authority for A may remain
current after B is committed.

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

### Failure semantics

Before B commits, stale/invalid correlation rejects without mutation. A gameplay
release failure rejects replacement while A remains current according to the
existing release transaction. Materialization or selection-commit failure retains
A and retires B when materialized. Activation failure restores A only where the
canonical primitive safely guarantees it; failed restoration is reported as a
rollback failure.

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
legitimately observe, such as Slot, current occurrence evidence and public revision
evidence. Internal preparation and lifecycle tokens remain Framework-owned unless a
separate API design establishes them as public contract evidence. The orchestration
resolves internal correlations from the validated scoped context.

The typed result and/or scoped observation must prove, without parsing a message:

- rejected, rolled back, physically committed, reprojected, degraded, or cleanup-pending outcome;
- previous and current Actor;
- preservation of the same Player occurrence;
- gameplay reprojection success/failure and current readiness evidence;
- retained old-Actor cleanup when applicable.

Exact public type, enum and member names are implementation design work and are not
frozen by this ADR.

## Accepted scope

- Public intent through `IPlayerSessionScopedAccess` and internal Activity-scoped orchestration.
- Manager-Provisioned prepared Actor replacement in the same occurrence.
- Typed outcome and observation sufficient for consumer and QA proof.
- Reuse of existing internal replacement and gameplay ownership paths.

## Rejected scope

- Mechanical public exposure of `TryReplacePreparedActor`.
- Changing `RequestReplaceActorSelection` semantics after preparation.
- Leave/Join, Host replacement, Session reassignment or synthetic Activity occurrence.
- Scene-Provided replacement in V1.
- Input-gate bypass, fallback reader fabrication, or free-form diagnostic-only evidence.

## Consequences

Implementation must add a dedicated orchestration wrapper, not a generic manager.
The wrapper belongs with the current Activity lifecycle/readiness authority and is
reached through scoped access. Existing selection commands and their rejection
contract remain unchanged.

The eventual canonical QA uses `QA_DefaultActor` as A and `QA_AlternateActor` as B.
It proves same P1/Slot/Host/Input/Activity occurrence, B selected/prepared/current,
A non-authoritative, reader A unbound, B admission/token/reader/camera evidence, and
correct readiness reconciliation. It also covers stale public correlation,
materialization/activation failures, old-release cleanup pending, ambiguous readers,
re-admission failure, blocked input gate, Scene-Provided rejection, and P2/Joining
non-interference.

## Current implementation coverage

The internal physical/logical replacement primitive exists. The public
orchestration, typed public result/observation extension, runtime implementation and
positive QA are **not implemented**. The current QA blocker that expects
`RequestReplaceActorSelection` rejection after preparation remains correct.

## Pending decisions

- Exact request/result/status naming and the minimal public correlation fields.
- Exact typed status for Scene-Provided rejection and committed-but-not-ready outcome.
- Scene-Provided physical ownership contract for a future capability.
