# Immersive Framework Architecture Documentation

## Normative architecture

[ADRs/](ADRs/) contains accepted architectural decisions. ADRs define the
normative boundary; they are not mutable delivery reports.

## Cross-cutting governance

[Governance/](Governance/) records cross-cutting policy that is already owned by
accepted ADRs or framework-wide compatibility rules, such as API maturity
metadata and validation-policy semantics.

Governance records do not create feature authority and do not replace ADRs. A
change that alters an accepted architectural boundary still requires the
appropriate ADR or migration decision.

## Current reconciliation records

[Reconciliation/](Reconciliation/) contains current technical reconciliations
and certification records. They distinguish Stage A technical evidence from
Stage B real-consumer evidence.

## Current framework status

[Tracking/IF-TRACK-Framework.md](Tracking/IF-TRACK-Framework.md) is the single
mutable view of current delivery status and open work.

## Architecture guides and history

[Guides/](../Guides/) explains current framework usage. [Archive/](Archive/)
preserves historical audits, completion summaries, rebaseline reports and
plans; archived records are not current authority.

ADRs define normative decisions. Governance records make cross-cutting policy
explicit without inventing new feature authority. Reconciliation records
describe current alignment. The Tracker summarizes current delivery state.
Archive documents are historical and non-authoritative.
