# IF-ADR-021A — Activity Player Relocation Fail-Fast and Readiness Recovery

Status: **IMPLEMENTED / MANUAL PLAY MODE VERIFIED — NOT A NEW QA CERTIFICATION**  
Date: **2026-08-27**  
Package commit: `c8427d6a8fb925e8aa02b92501495ac0d5059392` (`Fast Fail`)  
Related decisions: IF-ADR-007, IF-ADR-012, IF-ADR-019, IF-ADR-020, IF-ADR-021

## Purpose

This reconciliation records the runtime correction discovered while exercising the
Manager-Provisioned `Joining Control` consumer path with Activity explicit relocation.
It does not replace the existing ADR-021 Model B certification counts. It records a
post-certification runtime robustness and diagnostics cut proven manually in Play Mode.

The cut closes two connected defects:

1. invalid Activity explicit relocation could be detected only after Actor
   selection/preparation/materialization had already begun; and
2. after a relocation-driven Player readiness failure, a later valid reevaluation
   could compute `WaitingForJoin` while the canonical readiness participant remained
   latched in `Failed`.

## Authority preserved

The accepted authority split remains unchanged:

```text
Session
  -> Join / Leave
  -> Slot membership
  -> provisioning
  -> Actor selection/resolution authority
  -> admitted physical Player lifetime

Activity
  -> participation projection
  -> contextual readiness
  -> optional explicit relocation intent
```

A Session Join that succeeds remains a successful Session operation even if the
current Activity cannot satisfy its contextual relocation requirement. The downstream
failure belongs to Activity Player reconciliation/readiness; it does not retroactively
convert Session Join into a failed Session transaction.

## Activity relocation fail-fast

For an Activity using `ApplyExplicitRelocation`, relocation is required only when a
projected Slot is actually about to advance into the representation/preparation path.
A projected Slot that is not yet Joined remains a legitimate pending condition:

```text
Slot not Joined
  -> WaitingForJoin
  -> readiness Preparing
  -> no relocation preflight failure
```

Once the Slot is Joined and the Activity lifecycle is about to select, prepare or
materialize its Actor representation, relocation is preflighted first.

```text
Joined projected Slot
  -> resolve exact ActivityId + PlayerSlotId relocation
  -> only then may Actor mutation begin
```

The preflight uses the same canonical Model B discovery boundary:

```text
current Route Primary Scene
current Route Content scenes
current Activity Content scenes
```

and the same deterministic identity rule:

```text
0 exact bindings  -> fail
1 exact binding   -> continue
>1 exact bindings -> fail duplicate
invalid binding   -> fail
```

Bindings authored for another Activity or another Slot are ignored semantically and
do not satisfy the current pair. No hierarchy, name, first-found, default-anchor or
arbitrary-loaded-scene fallback is introduced.

### Failure semantics

A relocation preflight failure occurs before:

- default Actor selection;
- logical Actor preparation;
- physical Actor materialization;
- relocation application.

The reconcile returns `FailedPreparation`, publishes the Activity Player readiness
failure and emits an explicit runtime `ERROR` containing at least the Activity,
ActivityId, Player Slot, relocation policy and exact matching-binding count.

Because no Actor mutation has occurred, this path does not require rollback of Actor
selection/preparation state and must not create secondary cleanup failures.

## Readiness recovery from current evidence

The canonical readiness contribution represents the current condition of the active
Activity occurrence, not the worst historical condition previously observed during
that occurrence.

A failed contribution may therefore return to `Preparing` only when a new canonical
reevaluation proves that the condition which caused the failure no longer applies.
For the Joining Control case:

```text
Joined
  -> relocation invalid
  -> Failed

Leave
  -> Slot Available
  -> projected Slot becomes pending WaitingForJoin
  -> Failed -> Preparing
```

This is not a generic failure clear and is not driven by command type or Session
revision alone.

```text
current reevaluation still Failed
  -> remain Failed

current reevaluation PendingResolution
  -> Preparing with current reason

current reevaluation satisfied
  -> Complete as Completed
```

The internal readiness resume preserves the same Activity readiness occurrence. It
publishes the normal canonical state-change signal without firing a new
`preparationStarted` callback and without creating a new Activity occurrence.

The Player lifecycle does not manipulate the readiness gate directly. Gate state is
recomposed from the canonical readiness participant. Therefore a recovered
`WaitingForJoin` contribution returns to its normal `Preparing` gate behavior, while a
still-failing contribution remains failed.

## Manual Play Mode evidence — 2026-08-27

The following consumer/regression sequence was exercised in Unity Play Mode against
the implemented package cut:

| Case | Observed result |
| --- | --- |
| Activity starts with projected Slot not Joined | `WaitingForJoin`, readiness `Preparing`, gate held |
| Open Joining, then Join with zero exact relocation bindings | Session Join `SucceededJoined`; explicit relocation error reports `Matching bindings: 0`; reconcile `FailedPreparation` |
| Actor state after relocation failure | no selected Actor, no logical preparation, no physical materialization |
| Leave after relocation failure | `SucceededLeft`; Slot returns `Available`; no partial release |
| Reevaluation after Leave | `SucceededProgressed`; `pending=1`, `failed=0`, `WaitingForJoin`; readiness `Failed -> Preparing`; gate held again |
| Rejoin without correcting composition | relocation preflight fails again; failure is not masked by recovery |
| Close Joining while Slot remains Joined and relocation is still invalid | Session revision changes, but readiness remains `Failed`; relocation failure is reevaluated and preserved |

The final case proves that Session revision alone is not a recovery trigger.

## Certification boundary

This record is manual Play Mode/consumer regression evidence for the 2026-08-27
runtime cut. It does **not** alter or relabel the existing technical certification:

```text
Route Spatial Entry      18/18 PASS
Activity Relocation      23/23 PASS
Full Player aggregate    27/27 PASS
```

Historical ADR-021 Initial Placement `9/9` and historical Full Player `25/25` remain
dated evidence for their original boundaries.

Automated regression coverage for the fail-fast/recovery sequence may be added in a
future QA cut without changing the authority model recorded here.
