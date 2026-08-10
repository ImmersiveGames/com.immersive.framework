# IF-ADR-003 — Player Participation and Actor Lifecycle

Status: **Accepted**  
Last updated: 2026-08-09  
Related decisions: IF-ADR-001, IF-ADR-007, IF-ADR-012, IF-ADR-015, IF-ADR-016
Current reconciliation: [ADR-003 / ADR-012 technical reconciliation](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-003-012-RECONCILIATION-2026-08-10.md)

> Current implementation, QA and FIRSTGAME integration status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage. UX observations are qualitative
> product feedback and are not part of functional completion arithmetic.

## Context

A Logical Player is a Session participant while an Actor is contextual gameplay
content. Joining, Host provisioning/adoption, Actor selection, logical
preparation, physical materialization, gameplay admission, readiness contribution
and contextual release must remain distinct and diagnosable.

## Decision

Player participation is Session-scoped and keyed by typed Slot identity.
Route/Activity may project eligible Players and own contextual Actor
materialization, but they do not own Session participant identity.

```text
Session Slot configuration
Joining / admission
Local Player Host provisioning or adoption
Logical Player participation
Actor selection
Logical Actor preparation
physical Actor materialization
input / camera / gameplay admission
Activity readiness contribution
contextual release / reconcile
```

Scene-Provided and Manager-Provisioned are peer provisioning modes. They converge
on the same Session/Slot/Actor authority without collapsing Host and Actor
identity.

Reconciliation is idempotent, occurrence-aware and revision-correlated.
Consumers do not invoke internal preparation or reconcile authority.

## Player Session dependency

IF-ADR-016 owns initial Session intent:

```text
PlayerSessionProfile
  Supported Slots
  Initial Joining
  Host Provisioning
  Actor Resolution
```

There is no independent Session Capacity and no per-Slot Host Provisioning
override in the current model.

## Readiness and control-plane boundary

An Activity may project a required Slot before that Slot has Joined. When the
requirement is `JoinedSlots` or stronger, the Player contribution may remain:

```text
Preparing / WaitingForJoin
```

This is not failure and must not be silently converted to Ready, optional
participation or timeout success.

For `WaitCovered`, any operation required to advance readiness must remain
reachable through an external/control-plane path. `RequestJoin` may be such an
operation and is distinct from normal gameplay input.

Validation may warn about unreachable compositions but must not auto-change
readiness policy, participation requirement, Slot projection or Joining state.

## Rejected behavior

- Capacity as a second Session admission limit.
- Separate Player provisioning Profile.
- Per-Slot Host Provisioning overrides in the current Session model.
- Consumer Slot reservation.
- Consumer Actor preparation/materialization authority.
- Fake readiness, automatic Join or silent fallback.
- Global Player manager/service locator.

## Future contracts

Session Player Leave, device disconnect/reconnect and Session-Persistent Player
require separate explicit contracts when opened.
