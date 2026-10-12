# IF-ADR-012 — Activity Player Participation Profile and Readiness Compatibility

Status: **Accepted**  
Last updated: 2026-10-12
Amended: **2026-09-27 — GameplayReady admission versus entry-gate block; 2026-10-12 — Stable ActivityAsset serialization with Experimental Player behavior boundary.**
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

### Maturity and serialized authoring boundary

`ActivityAsset` remains a Stable authoring asset. Under
[IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md),
the Stable authoring contract includes preservation and interpretation of its
existing serialized Player participation data, independently of the maturity
of the behavior selected by that data. This is a data-schema guarantee, not a
promise of byte-for-byte Unity YAML formatting.

The following configured behaviors remain Experimental and are not promoted by
the Stable asset that stores them:

- Slot projection and zero-participant policy, stored in
  `playerParticipationProjectionMode`,
  `playerParticipationZeroParticipantPolicy`, and the ordered
  `playerParticipationExplicitSlotProfiles` references.
- Progressive Player readiness, stored in
  `playerParticipationRequirementLevel`.
- Contextual Player relocation, stored in `playerRelocationPolicy`; its
  behavioral authority and limits remain
  [IF-ADR-021](IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md).

The current serialized enum values are part of the existing data schema:
projection `NoSlots = 0`, `AllJoinedSlots = 10`, `ExplicitSlots = 20`; zero
participants `Allowed = 0`, `Rejected = 10`; readiness `None = 0`,
`JoinedSlots = 10`, `SelectedActors = 20`, `LogicalActorsPrepared = 30`, and
`GameplayReady = 40`. The explicit Slot reference list retains its authored
order. These values document the current schema; they do not promote the
associated behavior to Stable.

The existing field identities, serialized enum values, and authored reference
data must remain interpretable. A change that loses or silently reinterprets
existing data requires an explicit ADR and migration, even when the affected
behavior remains Experimental. These boundaries do not alter Session
provisioning ownership, promote Player participation/readiness, or reduce any
other Stable `ActivityAsset` guarantee.

The enum types `ActivityParticipationProjectionMode`,
`ActivityParticipationZeroParticipantPolicy`,
`PlayerParticipationRequirementLevel`, and `ActivityPlayerRelocationPolicy`
currently lack `FrameworkApiStatusAttribute`. This ADR establishes the maturity
of the scoped behaviors, not type-level metadata; reconciliation of those
attributes remains separate and must not be inferred for other types.

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

### Entry-gate provisional admission

Amended 2026-09-27, with IF-ADR-007. `All Joined Slots` plus `GameplayReady` still
requires a usable gameplay chain after a Route and Activity transition. The Session
Player, its Host, and its PlayerInput stay Session-owned. The contextual admission
is what the transition rebuilds.

While the only blocker is the entry/transition gate, `BlockedByInputGate` may be
retained and may satisfy the Player contribution enough to release that gate. The
readiness record must still report `gameplayReady` from `admission.GameplayReady`.
A blocked admission is not `gameplayReady`.

When the gate is gone, that admission converges to `Ready` and the contracted
gameplay action map is enabled before the next normal gameplay read. No new owner,
service, or event is introduced for that convergence.

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
