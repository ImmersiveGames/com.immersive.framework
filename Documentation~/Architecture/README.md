# Immersive Framework Architecture Documentation

Last updated: **2026-08-14**

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

## Current Player lifetime reconciliation

The current authoritative Player correction is:

[Player Physical Lifetime Reopen — 2026-08-14](Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-REOPEN-2026-08-14.md)

Frozen model:

```text
Session
  owns Joined Player occurrence
  owns admitted physical Player after successful admission

Activity
  owns contextual projection / activation / gameplay / camera / readiness
  does not own terminal physical lifetime
```

Scene-Provided and Manager-Provisioned differ in acquisition origin and converge on
Session ownership after successful admission.

Normal Activity-to-Activity transition preserves the exact physical Player.

## Current affected ADR disposition

- IF-ADR-016 — Accepted; implementation ownership reconciliation open.
- IF-ADR-019 — Accepted/revised; reopened for implementation and recertification.
- IF-ADR-020 — Accepted/revised; reopened for physical release reconciliation.
- IF-ADR-021 — Proposed; existing implementation work must be reconciled before certification.
- IF-ADR-003 / 007 / 012 / 015 / 001 — documentation reconciled to the corrected boundary.

## Historical certification records

Dated ADR-019 and ADR-020 certification records remain historical evidence and are not
rewritten to pretend they tested the revised contract.

The new reconciliation record explicitly defines what remains valid and what requires new
QA.

## Current delivery state

See:

[Tracking/IF-TRACK-Framework.md](Tracking/IF-TRACK-Framework.md)

The Tracker is the mutable delivery summary. ADRs remain normative architecture.
