# Immersive Framework Architecture Documentation

## Current canonical baseline

The current package baseline approved for the next FIRSTGAME Stage B task is
recorded in:

- [Stage A Canonical Package Baseline Closure](Reconciliation/IMMERSIVE-FRAMEWORK-STAGE-A-CANONICAL-BASELINE-CLOSURE-2026-08-11.md)

Baseline package commit:

```text
7a20ec748e4e5f5f3764bdc34ee249c1fe1c1da6
```

The reverse-audit sequence RA-01 through RA-04 is closed for the current accepted
Stage A boundaries. The active program phase is FIRSTGAME real-consumer/product
validation.

## Normative architecture

[ADRs/](ADRs/) contains accepted architectural decisions. ADRs define the
normative boundary; they are not mutable delivery reports.

## Cross-cutting governance

[Governance/](Governance/) records cross-cutting policy that is already owned by
accepted ADRs or framework-wide compatibility rules.

Current governance entry:

- [IF-GOV-001 — API Maturity and Validation Governance](Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)

Governance records do not create feature authority and do not replace ADRs. A
change that alters an accepted architectural boundary still requires the
appropriate ADR or migration decision.

## Current reconciliation and certification

[Reconciliation/](Reconciliation/) contains current technical reconciliations and
certification records. They distinguish Stage A technical evidence from Stage B
real-consumer evidence.

Current closure records:

- [RA-03 — Object Entry Ownership Reconciliation](Reconciliation/IMMERSIVE-FRAMEWORK-RA-03-OBJECT-ENTRY-OWNERSHIP-RECONCILIATION-2026-08-11.md)
- [RA-04 — Architecture Governance Hygiene](Reconciliation/IMMERSIVE-FRAMEWORK-RA-04-ARCHITECTURE-GOVERNANCE-HYGIENE-2026-08-11.md)
- [Stage A Canonical Package Baseline Closure](Reconciliation/IMMERSIVE-FRAMEWORK-STAGE-A-CANONICAL-BASELINE-CLOSURE-2026-08-11.md)

## Current framework status

[Tracking/IF-TRACK-Framework.md](Tracking/IF-TRACK-Framework.md) is the single
mutable view of current delivery status and open work.

The tracker must treat:

```text
Stage A
  -> accepted technical boundary / package / QA / reconciliation

Stage B
  -> FIRSTGAME real-consumer integration / product / usability proof
```

A Stage B usability finding does not automatically reopen a closed Stage A
technical boundary.

## Architecture guides and history

[Guides/](../Guides/) explains current framework usage. [Archive/](Archive/)
preserves historical audits, completion summaries, rebaseline reports and plans;
archived records are not current authority.

ADRs define normative decisions. Governance records make cross-cutting policy
explicit without inventing new feature authority. Reconciliation records describe
current alignment and certification. The Tracker summarizes current delivery
state. Archive documents are historical and non-authoritative.
