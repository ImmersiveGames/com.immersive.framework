# IF-ADR-015A — Player Session Observer and Explicit Command Surfaces — 2026-08-25

Status: **RECONCILED — IMPLEMENTED PUBLIC-SURFACE CUT / MANUAL CONSUMER EVIDENCE RECORDED**  
Decision authority: **IF-ADR-015**  
Related decisions: IF-ADR-010, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021

## 1. Purpose

This record reconciles the 2026-08-25 Player Session public-surface cut that replaced the
older status/generic-command authoring model with a read-only Observer and explicit
command components.

It records implementation shape, serialization migration and the non-blocking command
surface readiness follow-up observed during consumer testing.

## 2. Implemented Framework cut

Framework implementation commit:

```text
ImmersiveGames/com.immersive.framework
08e1f655a344b71d0d5ef37c7e41ebb58807aa00
PLAYER SESSION PUBLIC SURFACE
```

The public composition is now:

```text
Player Session authority
        ↓
scoped consumer access
   ┌────┴────┐
   │         │
 READ     REQUEST
   │         │
PlayerSessionObserver
             PlayerSessionOpenJoiningCommandTrigger
             PlayerSessionCloseJoiningCommandTrigger
             PlayerSessionJoinCommandTrigger
             PlayerSessionDefaultActorSelectionCommandTrigger
             PlayerSessionLeaveCommandTrigger
```

No new Player Session authority was introduced.

The existing runtime contracts remain the transport/authority boundary:

```text
PlayerSessionScopedAccessConsumer
ILocalPlayerProvisioningConsumerAccess
LocalPlayerProvisioningConsumerObservationSnapshot
existing typed command results
```

## 3. Observer reconciliation

The former:

```text
PlayerSessionStatus
```

was replaced by:

```text
PlayerSessionObserver
```

The new name expresses the component's actual responsibility:

```text
read-only
scoped Session observation
usable from Hub / UI / presentation / another scene
no physical Player GameObject reference required
no Player truth ownership
no command execution
```

The Observer reads the current immutable public observation and may derive presentation
labels from that evidence. It does not cache or reconcile Player state.

The previous optional command-trigger reference and all `LastOperation*` aggregation were
removed. Command result evidence now remains with the command that produced it.

## 4. Explicit command reconciliation

The former model:

```text
PlayerSessionCommandTrigger
  PlayerProvisioningCommandOperation enum
  operation-specific serialized fields
  union of last-result kinds
```

was removed from the public authoring surface.

It was replaced by five explicit components:

```text
PlayerSessionOpenJoiningCommandTrigger
PlayerSessionCloseJoiningCommandTrigger
PlayerSessionJoinCommandTrigger
PlayerSessionDefaultActorSelectionCommandTrigger
PlayerSessionLeaveCommandTrigger
```

The reason is structural, not cosmetic. These operations have different authored inputs,
validation rules and typed outcomes. A single serialized enum was changing the complete
semantic identity of one component and allowed irrelevant operation data to remain
serialized but hidden.

The shared `PlayerSessionCommandTriggerBase` is internal infrastructure only. It shares
scoped access, invocation metadata, diagnostics and common logging without becoming a
generic product command selector.

## 5. Command result ownership

Result ownership is now explicit:

```text
Open Joining
  -> LastOpenJoiningResult

Close Joining
  -> LastCloseJoiningResult

Join
  -> LastJoinResult

Default Actor Selection
  -> LastActorSelectionResult

Leave
  -> LastLeaveResult / LastLeaveRequest
```

No Observer-level "last command" union is retained.

## 6. Inspector reconciliation

The new Editor surface follows IF-ADR-010.

Observer:

```text
Scope
runtime observation in Play Mode
Validation
Advanced / Debug
```

Commands:

```text
Scope
command-specific authored intent
Validation
Advanced / Debug
```

Advanced / Debug owns diagnostic metadata, revisions/occurrence overrides, typed runtime
result evidence and manual Play Mode `Invoke` testing.

The explicit full Validation action is not replaced by automatic full validation on every
Inspector repaint.

## 7. Serialization migration

The `PlayerSessionStatus` script GUID was preserved for `PlayerSessionObserver`.

This keeps the serialized script identity stable for the renamed read-only surface while
changing the product-facing type/name to match its responsibility.

The generic command trigger was not retained as a compatibility wrapper.

Known serialized generic command instances in the Player sample were migrated explicitly
to the matching Join and Leave components. Their UnityEvents were rewired from the old
generic method to each component's public `Invoke()`.

No automatic migrator was introduced because the implementation audit found no other
local serialized usages that required one.

## 8. FIRSTGAME / Sample integration

Consumer integration commit:

```text
ImmersiveGames/planet-devourer
b8c59065ad2945b04698a6a862e2121c5f7ae983
PLAYER SESSION PUBLIC SURFACE
```

The Manager-Provisioned Player controls now use explicit command components rather than
an enum-selected generic trigger.

Canonical consumer rule:

```text
need Session information
  -> PlayerSessionObserver

need Join
  -> PlayerSessionJoinCommandTrigger

need Leave
  -> PlayerSessionLeaveCommandTrigger

need both observation and commands
  -> compose them independently
```

`PlayerSessionObserver` is not required for a command to function.

## 9. Manual consumer evidence

A Manager-Provisioned consumer run confirmed the principal Join/Leave lifecycle:

```text
Join
  -> SucceededJoined
  -> Slot Joined
  -> default Actor selected/prepared
  -> physical representation materialized
  -> gameplay admitted / GameplayReady

Leave
  -> SucceededLeft
  -> Activity representation released
  -> provisioning resources released
  -> Slot Available
  -> Actor selection cleared
  -> physical representation absent
```

The visual output also returned to the persistent Default Camera after Leave, consistent
with Player camera authority being removed through the normal lifecycle rather than by a
button-owned camera workaround.

This evidence is consumer integration evidence, not a replacement for automated Player or
Camera certification.

## 10. Deferred command-surface readiness issue

The same run exposed one timing window:

```text
first Join interaction
  bindingStatus = Unbound
  outcome = RejectedRuntimeUnavailable

scoped access binds

second Join interaction
  bindingStatus = Bound
  outcome = SucceededJoined
```

Tracking label:

```text
PLAYER-COMMAND-SURFACE-READINESS
status = DEFERRED
```

Future goal:

> make command availability distinguishable before normal consumer interaction is
> enabled, without adding a fallback lookup, alternate Session authority or hidden
> automatic command path.

This issue does not invalidate the Observer/explicit-command composition and is not a
blocker for this documentation reconciliation.

## 11. Preserved boundaries

The cut does not change these rules:

```text
Session owns mutable Player truth
Observer is read-only evidence
Command component only requests one operation
Activity/Route scope is an access boundary, not Player ownership
no global registry/service locator
no scene search or hierarchy/name fallback
no combined Join + Actor materialization command
no automatic command invocation from lifecycle callbacks
```

## 12. Closure

The current Player Session consumer model is therefore:

```text
Observer = read
Commands = request/change
```

The old `PlayerSessionStatus` terminology and enum-driven generic command authoring are
superseded for current product usage. Historical documents may retain those names only
when they are explicitly describing the earlier implementation boundary.
