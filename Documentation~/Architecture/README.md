# Immersive Framework Architecture Documentation

Last updated: **2026-08-13**

## Current canonical baseline

The historical package baseline approved for FIRSTGAME Stage B on already-closed Stage A
boundaries is recorded in:

- [Stage A Canonical Package Baseline Closure](Reconciliation/IMMERSIVE-FRAMEWORK-STAGE-A-CANONICAL-BASELINE-CLOSURE-2026-08-11.md)

Baseline package commit:

```text
7a20ec748e4e5f5f3764bdc34ee249c1fe1c1da6
```

The reverse-audit sequence RA-01 through RA-04 is closed. Subsequent Player decisions
ADR-019 and ADR-020 were handled as scoped architecture/package/QA reconciliation cuts;
this documentation does not invent a later Git baseline SHA for those locally validated
changes.

## Normative architecture

[ADRs/](ADRs/) contains accepted and proposed architecture decisions.

```text
Accepted
  -> normative architecture

Proposed
  -> pending architecture; not implementation/certification authority
```

Current Player expansion:

- [IF-ADR-019 — Session Player Lifetime and Activity Representation Authority](ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md) — **Accepted / Reconciled / Implemented / QA Certified**
- [IF-ADR-020 — Session Player Leave and Resource Release Authority](ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md) — **Accepted / Reconciled / Implemented**; focused Manager public QA 26/26
- [IF-ADR-021 — Activity Player Actor Initial Placement Authority](ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md) — **Proposed**
- [IF-ADR-022 — Camera Rig Presentation Models and Materialization Authority](ADRs/IF-ADR-022-Camera-Rig-Presentation-Models-and-Materialization-Authority.md) — **Proposed**

ADRs define normative boundaries; they are not mutable completion reports.

## Cross-cutting governance

[Governance/](Governance/) records cross-cutting policy already owned by accepted ADRs or
framework-wide compatibility rules.

Current governance entry:

- [IF-GOV-001 — API Maturity and Validation Governance](Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)

Governance records do not create feature authority or replace ADRs.

## Current reconciliation and certification

[Reconciliation/](Reconciliation/) contains current technical reconciliation and
certification records. They distinguish Stage A technical evidence from Stage B
real-consumer evidence.

Current closure records include:

- [ADR-019 — Session Player Lifetime reconciliation](Reconciliation/IMMERSIVE-FRAMEWORK-ADR-019-RECONCILIATION-2026-08-12.md)
- [ADR-020 — Session Player Leave reconciliation](Reconciliation/IMMERSIVE-FRAMEWORK-ADR-020-RECONCILIATION-2026-08-13.md)
- [RA-03 — Object Entry Ownership Reconciliation](Reconciliation/IMMERSIVE-FRAMEWORK-RA-03-OBJECT-ENTRY-OWNERSHIP-RECONCILIATION-2026-08-11.md)
- [RA-04 — Architecture Governance Hygiene](Reconciliation/IMMERSIVE-FRAMEWORK-RA-04-ARCHITECTURE-GOVERNANCE-HYGIENE-2026-08-11.md)
- [Stage A Canonical Package Baseline Closure](Reconciliation/IMMERSIVE-FRAMEWORK-STAGE-A-CANONICAL-BASELINE-CLOSURE-2026-08-11.md)

ADR-020's record is intentionally scope-precise: architecture/implementation are closed;
focused Manager-Provisioned public Leave QA is certified 26/26; dedicated Scene-Provided
Session Leave QA is not separately claimed without terminal evidence.

## Current framework status

[Tracking/IF-TRACK-Framework.md](Tracking/IF-TRACK-Framework.md) is the single mutable view
of delivery status and open work.

```text
Stage A / scoped technical reconciliation
  accepted ADR -> package -> technical QA -> reconciliation

Stage B
  accepted package boundary -> FIRSTGAME -> real integration / product / usability proof
```

A Stage B usability finding does not automatically reopen a closed technical boundary.

## Architecture guides and history

[Guides/](../Guides/) explains current framework usage. [Archive/](Archive/) preserves
historical audits, completion summaries, rebaseline reports and plans; archived records
are not current authority.

ADRs decide. Governance makes cross-cutting policy explicit. Reconciliation records
technical alignment/evidence. Tracker summarizes current state. Archive is historical.
