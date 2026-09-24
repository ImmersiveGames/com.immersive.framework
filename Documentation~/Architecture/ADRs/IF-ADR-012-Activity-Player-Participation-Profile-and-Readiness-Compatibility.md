# IF-ADR-012 — Activity Player Participation Profile and Readiness Compatibility

Status: **Accepted**  
Last updated: 2026-08-17  
Related decisions: IF-ADR-003, IF-ADR-007, IF-ADR-010, IF-ADR-015, IF-ADR-016
Current reconciliation: [ADR-003 / ADR-012 technical reconciliation](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-003-012-RECONCILIATION-2026-08-10.md)

> Current implementation, QA and FIRSTGAME integration status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage. UX observations are qualitative
> product feedback and are not part of functional completion arithmetic.

## Context

Activities need reusable Player participation intent that can express projected
Slots, readiness requirements and compatibility without duplicating Session rules
inside each scene.

## Decision

Activity Player participation is authored through the approved Activity/Route
policy surface and resolves into one normalized effective policy with provenance.

Runtime consumes explicit Slot/Player/Actor evidence and publishes requested and
effective state plus diagnostic reasons. Invalid or incompatible states fail
explicitly.

Activity participation does not own or silently mutate Player Session
configuration.

## Session boundary

```text
PlayerSessionProfile
  owns Supported Slots
  owns Initial Joining
  owns Session Host Provisioning
  owns Actor Resolution

Activity Player policy
  projects/qualifies current Session Slots
  defines participation/readiness intent
  does not replace Session provisioning
  does not create Capacity
```

## Readiness requirement semantics

The Activity-authored Player requirement selects the minimum Player lifecycle evidence
required for the current Activity occurrence. It does not reconfigure the Session.

For the accepted baseline:

```text
None
JoinedSlots
SelectedActors
LogicalActorsPrepared
GameplayReady
```

These levels are ordered requirements, not aliases. In particular:

```text
LogicalActorsPrepared
  != GameplayReady

LogicalActorsPrepared
  -> Actor preparation requirement satisfied

GameplayReady
  -> Actor preparation plus current gameplay admission/input/camera chain satisfied
```

Consequently, an Activity that hosts gameplay code consuming the current Player gameplay
input surface must explicitly request `GameplayReady`. Authoring only
`LogicalActorsPrepared` does not authorize the runtime to continue implicitly into
gameplay admission.

This preserves the ownership split:

```text
PlayerSessionProfile
  -> initial Session configuration and provisioning policy

Activity Player policy
  -> participation projection and required readiness level for this occurrence

Gameplay consumer
  -> consumes already-established current gameplay authority
  -> does not Join, select, prepare or promote readiness
```

## Constraints

- One normalized effective participation policy is runtime input.
- Provenance and requested/effective differences remain diagnosable.
- Invalid compatibility never falls back silently.
- Activity/GameFlow tests may consume a stable Player fixture but cannot become
  Player Session configuration authority.
- Participation policy does not become a Host provisioning mode.

## FIRSTGAME boundary

Real-product integration should prove participation against the same official
package contracts after the underlying Player provisioning mode is integrated.
Any UX friction observed during that proof is qualitative and does not change the
participation contract's functional completion arithmetic.
