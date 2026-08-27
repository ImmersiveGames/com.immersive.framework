# IF-ADR-015B — Player Actor Selection Public Surface Certification — 2026-08-26

Status: **CLOSED / IMPLEMENTED / INTEGRATED QA CERTIFIED**  
Date: **2026-08-26**  
Primary authority: [IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface](../ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md)  
Related decisions: IF-ADR-003, IF-ADR-010, IF-ADR-012, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021

## 1. Closure scope

This record closes the public arbitrary Actor-selection delivery cut that remained proposed in the earlier Player Session public-surface baseline.

The delivered public product surface now contains eight explicit Player Session command components:

```text
PlayerSessionOpenJoiningCommandTrigger
PlayerSessionCloseJoiningCommandTrigger
PlayerSessionJoinCommandTrigger
PlayerSessionSelectActorCommandTrigger
PlayerSessionDefaultActorSelectionCommandTrigger
PlayerSessionReplaceActorSelectionCommandTrigger
PlayerSessionClearActorSelectionCommandTrigger
PlayerSessionLeaveCommandTrigger
```

The former generic enum-driven command authoring model remains superseded.

This closure does **not** promote exact-Slot Join, paired device/input ownership, Local Multiplayer contracts or physical Actor hot-swap.

## 2. Canonical Actor-selection authority

Mutable Actor selection remains Session-owned state in `PlayerParticipationRuntimeContext`.

The public consumer path is:

```text
Player Session command component
  -> PlayerSessionScopedAccessConsumer
  -> ILocalPlayerProvisioningConsumerAccess
  -> scoped Runtime Host path
  -> PlayerActorPreparationRuntimeContext
  -> PlayerParticipationRuntimeContext
  -> PlayerActorSelectionResult
```

No second Actor-selection store, global manager, service locator, scene scan or fallback authority was introduced.

## 3. Public Actor operations

### Select Actor

`PlayerSessionSelectActorCommandTrigger` requests one explicit `ActorProfile` for one exact Joined `PlayerSlotId`.

### Select Default Actor

`PlayerSessionDefaultActorSelectionCommandTrigger` requests the Session-configured default Actor and remains policy-aware:

```text
ResolveConfiguredDefault
  -> configured DefaultActorProfile only

LeaveUnresolved
  -> RejectedDefaultResolutionDisabled
```

There is no hidden default fallback.

### Replace Actor Selection

`PlayerSessionReplaceActorSelectionCommandTrigger` replaces selected intent before the preparation barrier. It does not hot-swap an already prepared/admitted physical Actor.

### Clear Actor Selection

`PlayerSessionClearActorSelectionCommandTrigger` clears selected intent before the preparation barrier. It does not tear down an already prepared/admitted physical Actor.

## 4. Lifecycle and preparation barrier

Actor selection remains a logical selection transaction, not physical Actor replacement.

Select / Replace / Clear are blocked once the canonical Actor preparation context reports a prepared or retained failure state that must be resolved first.

Observed public result example from integrated QA:

```text
command='ClearActorSelection'
bindingStatus='Bound'
outcome='Rejected'
status='RejectedLogicalActorAlreadyPrepared'
previousSelectionRevision='4'
selectionRevision='4'
```

The rejection is correct and non-mutating.

The internal `TryReplacePreparedActor` transaction remains internal and is not exposed as a consumer hot-swap command.

## 5. Revision and idempotency contract

Selection mutation is revision-aware.

Canonical behavior includes:

```text
Select A with no selected Actor
  -> SucceededSelected
  -> selection / Slot / Session revisions advance once

Select A when A is already selected
  -> idempotent success
  -> revisions unchanged

Select B while A is selected
  -> reject; use Replace
  -> revisions unchanged

Replace B before preparation
  -> SucceededReplaced
  -> revisions advance once

Clear before preparation
  -> SucceededCleared
  -> revisions advance once

stale expected selection revision
  -> RejectedStaleSelectionRevision
  -> no mutation
```

Duplicate Actor selection remains governed by the Session duplicate-selection policy.

## 6. Scope and composition reconciliation

Public Player consumers are authored as `Route` or `Activity` scoped components.

The QA reconciliation confirmed two important product rules:

```text
valid authoring configuration
  !=
current runtime scoped-access availability
```

and:

```text
component physically present in Route-discovered content
  !=
authored Route ownership
```

An Activity-scoped consumer may be present in Route content, be ignored by the Route binding pass, and later bind correctly to Activity authority.

`PlayerSessionScopedAccessConsumer.TryBind(...)` remains the canonical scope-match boundary and still rejects authored/runtime scope mismatch.

## 7. QA reconciliation performed during certification

The new Actor-selection regression exposed several obsolete QA assumptions rather than Framework runtime defects. They were corrected without changing production runtime semantics:

```text
obsolete QA assumption
  missing runtime binding -> authoring validation must fail

correct contract
  valid authored command may be Unbound at runtime
  runtime invocation must reject without fallback or mutation
```

```text
obsolete QA assumption
  Activity-scoped consumer in Route content -> invalid/wrong scope

correct contract
  Route lifecycle ignores it
  Activity lifecycle may bind it as Activity
```

```text
obsolete fixture assumption
  a deliberately destroyed Route probe must still exist during later readiness validation

correct contract
  probe is mandatory before its planned destruction
  later fixture validation must not require the destroyed object
```

These corrections remained QA-only.

## 8. Integrated certification result

Final Full Player QA terminal evidence:

```text
[QA_PLAYER_FULL]
status='Completed'
verdict='PLAYER CURRENT AGGREGATE COMPLETE'
historicalFullPlayer='25/25'
serialization='PASS'
session='PASS'
routeSpatialEntry='PASS'
activityRelocation='PASS'
sceneProvided='PASS'
sceneProvidedLeave='PASS'
sceneProvidedNoActivityLeave='PASS'
sceneProvidedNoActivityTermination='PASS'
managerProvisioned='PASS'
managerNoActivity='PASS'
managerSessionTermination='PASS'
actor='PASS'
publicSurface='PASS'
leave='PASS'
failedFirstSceneAdoption='PASS'
failedContextualReprojection='PASS'
noPhysicalHandoff='PASS'
mandatoryContracts='27'
executedContracts='27'
passedContracts='27'
```

Disposition:

```text
P2A scoped Actor Selection contract       PASS
P2B explicit Actor Selection commands     PASS
P2C lifecycle/runtime integration         PASS in integrated Player QA
P2D public Actor Selection QA layer       PASS / 27 of 27 aggregate
```

The historical `25/25` certification remains dated evidence for its earlier boundary and is not rewritten.

## 9. Package-local Editor tests

The Actor-selection runtime cut also added package-local Unity Test Framework Editor tests for canonical context transitions.

This reconciliation does **not** claim those package-local tests were executed unless a separate Unity Test Framework result is recorded. The integrated QA result above is the current certification evidence for the public/runtime composition exercised by QAFramework.

## 10. Product consequence

The public arbitrary Actor-selection blocker for the Character Selection sample is closed.

A sample may now use:

```text
PlayerSessionProfile.ActorResolution = LeaveUnresolved
Join
  -> Slot Joined, Actor unresolved

game-owned Character Selection UI
  -> explicit PlayerSessionSelectActorCommandTrigger
  -> typed PlayerActorSelectionResult
  -> existing Framework preparation / provisioning / gameplay lifecycle
```

The sample remains responsible only for presenting its game-owned Actor choices and issuing public commands. It must not own Session state, Actor preparation, materialization or fallback.

## 11. Remaining Player blockers / deferred work

Still outside this closure:

```text
exact-Slot public Join command
public Slot/device/InputUser/control-scheme ownership observation
Local Multiplayer canonical device/input contract
consumer-facing physical Actor hot-swap
PLAYER-COMMAND-SURFACE-READINESS / DEFERRED
```

The deferred command-surface readiness issue remains a product-availability concern: valid authored commands can be temporarily runtime-unbound and must reject without fallback. This closure does not add an alternate authority or hidden lookup to mask that state.

## 12. Final disposition

Arbitrary Actor selection is no longer proposed-only scope.

It is now an implemented, explicit, scoped, typed Player Session public surface with integrated Full Player QA certification.

Character Selection may proceed as the next Player sample cut. Local Multiplayer remains blocked by the public Slot/device/input contract and must remain a separate later phase.
