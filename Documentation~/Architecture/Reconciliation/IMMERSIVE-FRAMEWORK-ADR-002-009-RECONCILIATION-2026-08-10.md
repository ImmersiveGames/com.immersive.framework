# Immersive Framework — ADR-002 / ADR-009 Documentation Reconciliation

**Date:** 2026-08-10  
**Scope:** documentation/status reconciliation after ADR-009 package + QA closure

## Result

IF-ADR-009 is closed for its current accepted boundary. Its package behavior and
46 focused QA cases now provide concrete evidence for the cross-cutting authoring
principles of IF-ADR-002.

## ADR-009 direct relationships

```text
IF-ADR-001  scoped runtime/lifecycle authority
IF-ADR-002  product authoring shape
IF-ADR-006  transition and diagnostic discipline
IF-ADR-007  Activity readiness/reveal boundary
IF-ADR-010  Editor/Inspector product-surface contract
IF-ADR-014  authored definition and stable identity authority
```

ADR-003, IF-ADR-011 and IF-ADR-012 are composition neighbors but do not own the
current visibility contract, so they were not promoted to direct ADR-009
relationships.

## ADR-002 reconciliation

ADR-002 is explicitly cross-cutting. Its relationship map now includes the
feature ADR portfolio and distinguishes authoring authority from each feature's
runtime authority. Current evidence includes:

- direct/manual authoring: Pause, Reset, Input Gate, Activity/Route triggers, Readiness, Activity Local Visibility and Optional BGM;
- reusable intent/Profile: PlayerSessionProfile and typed Activity/Route/configuration assets;
- reusable Template: Persistent Content Scene Template;
- justified materialization: Camera Rig.

Generic QA and generic FIRSTGAME are not ADR-002 completion gates. Objective QA
and real-consumer evidence stay attached to the feature ADR that owns the
contract.

## Tracker correction

```text
IF-ADR-002 100% normalized — CLOSED for current accepted cross-cutting boundary
IF-ADR-009 100% normalized — CLOSED, QA 46/46
```

The former ADR-002 `99% normalized` value treated the historical `29/30` package
audit score as an active missing point even though the accepted ADR identifies no
corresponding current-scope requirement. The dedicated ADR-002 reconciliation
therefore supersedes that percentage interpretation.

The former `Portfolio mean 90.9%` in this dated combined reconciliation is
historical and is not current Tracker authority. Current portfolio planning
values live only in `Tracking/IF-TRACK-Framework.md`.

The percentage remains a planning aid, not release certification.

## Historical records

Existing dated completion summaries and previous closure ZIPs remain historical
snapshots and were not overwritten.
