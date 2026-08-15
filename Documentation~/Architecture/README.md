# Immersive Framework Architecture Documentation

Last updated: **2026-08-15**

## Normative architecture

`ADRs/` contains accepted and proposed architecture decisions.

```text
Accepted
  -> normative architecture

Proposed
  -> pending architecture

Reopened
  -> architecture has been corrected/reconfirmed but implementation and/or prior
     certification must be reconciled before the boundary is called closed again
```

## Player physical lifetime closure

The Player lifetime reopen from 2026-08-14 is now technically closed.

Current closure authority:

[Player Physical Lifetime Recertification — 2026-08-15](Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-RECERTIFICATION-2026-08-15.md)

Historical reopen record:

[Player Physical Lifetime Reopen — 2026-08-14](Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-REOPEN-2026-08-14.md)

Frozen and certified model:

```text
Session
  owns Joined Player occurrence
  owns admitted physical Player after successful admission
  owns retained physical preparation until Leave / Session termination

Activity
  owns contextual projection / activation / gameplay / camera / readiness
  owns its current Activity RuntimeContent scope
  does not own terminal physical Player lifetime
```

Scene-Provided and Manager-Provisioned differ in acquisition origin and converge on Session ownership after successful admission.

Normal Activity-to-Activity transition preserves the exact physical Player and ordinary gameplay pose while establishing a fresh contextual occurrence.

No current Activity representation is compatible with retained Session physical preparation.

A current Activity may also be `CommittedNotReady`: Activity scope lifetime and Player contextual admission are separate authorities.

## Current affected ADR disposition

- IF-ADR-003 — Accepted / reconciled / Player QA recertified.
- IF-ADR-007 — Accepted / reconciled / Player readiness boundary recertified.
- IF-ADR-011 — Accepted / reconciled for the Player readiness interaction.
- IF-ADR-012 — Accepted / reconciled / Player QA recertified.
- IF-ADR-015 — Accepted / reconciled / Public Surface certified.
- IF-ADR-016 — Accepted / reconciled / implementation certified.
- IF-ADR-019 — Accepted / reconciled / implementation recertified.
- IF-ADR-020 — Accepted / reconciled / implementation recertified.
- IF-ADR-021 — Accepted / reconciled / implementation certified.

The terminal Full Player QA result is:

```text
PLAYER QA CERTIFIED
mandatoryContracts = 25
executedContracts = 25
passedContracts = 25
```

## Historical certification records

Dated ADR-019 and ADR-020 certification records remain historical evidence and are not rewritten to pretend they tested the revised contract.

The 2026-08-15 recertification record is the current certification authority for the revised physical-lifetime boundary.

## Current delivery state

See:

[Tracking/IF-TRACK-Framework.md](Tracking/IF-TRACK-Framework.md)

The Tracker is the mutable delivery summary. ADRs remain normative architecture.
