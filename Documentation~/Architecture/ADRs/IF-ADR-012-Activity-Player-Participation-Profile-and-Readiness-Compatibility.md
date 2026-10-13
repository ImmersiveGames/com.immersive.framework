# IF-ADR-012 — Activity Player Participation Profile and Readiness Compatibility

Status: **Accepted**  
Last updated: 2026-10-12
Amended: **2026-10-12 — Stable ActivityAsset serialization with Experimental Player behavior boundary; structural GameplayReady semantics.**
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
- Activity Transition Gate selection, stored in `transitionGateMode`. Its
  operational behavior remains Experimental under the existing [Gate](IF-ADR-005-Input-Pause-Gate-and-Reset.md)
  and [Loading/Transition](IF-ADR-006-Loading-Transition-Persistence-and-Diagnostics.md)
  decisions; the Stable Activity schema preserves its serialized value without
  promoting those contracts.
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

The current Runtime source annotates `ActivityParticipationProjectionMode`,
`ActivityParticipationZeroParticipantPolicy`, and
`PlayerParticipationRequirementLevel` as Experimental under this ADR, and
`ActivityPlayerRelocationPolicy` as Experimental under IF-ADR-021. These
attributes match the behavior classifications above. They do not change the
Stable serialization guarantee for the existing `ActivityAsset` fields or
promote adjacent Player contracts.

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
  -> current Activity gameplay representation and admission are structurally complete
```

Structural `GameplayReady` requires the current gameplay Input binding and its
identity-consistent admission evidence. It does not require that gameplay input
is currently consumable: Pause, Input Gate and physical action-map availability
are operational conditions. Camera readiness is independent and is not implied
by `GameplayReady`.

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

### Structural readiness while gameplay is gated

`GameplayReady` requires a current structural gameplay Input binding, but not
current physical input availability. A Transition Gate or Pause may therefore
keep `RuntimeGameplayAvailable` false while the Activity's structural readiness
contribution remains satisfied. Gate release changes operational availability
and physical input posture; it does not promote/recreate admission or require
Activity readiness reconciliation solely because the Gate changed.

There is no provisional `BlockedByInputGate` admission state or readiness
exception. The current source-level contract is specified by
[IF-ADR-036](IF-ADR-036-Gameplay-Readiness-and-Runtime-Gameplay-Availability-Separation.md).
The user-reported `QA-IF-ADR-036` Play Mode proof passed 8/8 for the supported
Transition Gate sequence, including the expected rejection of Pause while the
Gate is active and Pause/Resume after Gate release. Simultaneous active Pause
and Transition Gate blockers remain unsupported by the current Pause policy;
IF-ADR-044 does not certify that composition or expand the Stable boundary.

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
