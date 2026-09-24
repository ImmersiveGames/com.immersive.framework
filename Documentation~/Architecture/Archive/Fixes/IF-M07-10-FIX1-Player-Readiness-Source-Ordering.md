# IF-M07-10-FIX1 — Player readiness source ordering

## Defect

The host-scoped Player readiness source originally returned a participant only
when `playerReadinessRecord` already existed.

Activity entry ordering is:

```text
discover readiness participants
→ begin readiness occurrence
→ execute Activity content lifecycle
→ create Player lifecycle record
```

At discovery time the record did not exist, so the source returned no Player
participant. The lifecycle later reported `SucceededEnteredPreparing`, but that
state was not represented in the Activity readiness occurrence.

Observed QA evidence:

```text
Activity request: Succeeded
Activity readiness: NotReady
Player lifecycle: SucceededEnteredPreparing
FrameworkRuntimeHost child "Player Activity Readiness": absent
```

## Correction

The source now resolves the exact Player projection before content execution:

```text
Activity configuration
+ current Session snapshot
→ TryResolveProjection
→ requirement None / zero projected Slots: no contribution
→ otherwise materialize one Required host-scoped participant
```

The participant begins preparation with the occurrence before the lifecycle
record is created.

After deferred or immediate Player lifecycle record creation, the lifecycle
synchronizes:

```text
participant occurrence
→ record occurrence
→ pending record remains Preparing
→ completed record completes participant
→ failed record fails participant
```

## Preserved contracts

```text
one Player readiness participant per host
same generic Activity readiness aggregator
exact projected Slots
no second readiness authority
no global lookup
no fallback for invalid projection
Optional/Required authorable participants unchanged
```

## Expected Q3 evidence after the fix

For `waiting-exit`:

```text
Player lifecycle = SucceededEnteredPreparing
Player readiness participant = Preparing
occurrence > 0
Activity clear releases the contribution
```

The Q3 should advance beyond:

```text
waiting-player-contribution-preparing
```
