# IF-ADR-014 — Authored Definition and Stable Identity Authority

Status: **Accepted**  
Last updated: 2026-08-09  
Related decisions: IF-ADR-001, IF-ADR-003, IF-ADR-006, IF-ADR-010, IF-ADR-015  
Closed execution record: [IF-ID Identity Authority](../Archive/Plans/IF-ID-IDENTITY-AUTHORITY-EXECUTION-PLAN-2026-08-06.md)

> Current implementation, QA and FIRSTGAME integration status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage. UX observations are qualitative
> product feedback and are not part of functional completion arithmetic.

## Context

Unity asset references and stable external identifiers solve different problems.
Treating equal stable IDs as authored-definition equality can merge distinct
assets, while relying only on references breaks persistence/external boundaries.

## Decision

Authored/runtime definition equality uses the exact `RouteAsset` or
`ActivityAsset` reference.

`RouteId` and `ActivityId` are stable projections for persistence,
serialization, diagnostics and external references. Two distinct assets with the
same stable ID are a collision and must not silently become one definition.

Operational Route/Activity ownership requires a process-local
`RuntimeDefinitionToken` for the exact definition instance in addition to stable
ID.

Runtime occurrence identity remains definition-aware and occurrence-scoped.
Stable ID is not lifecycle, readiness, release, supersession or cleanup authority.

## Authority model

| Dimension | Authority |
|---|---|
| Authored definition | exact typed asset reference |
| Stable boundary identity | `RouteId` / `ActivityId` |
| Runtime occurrence | definition reference + occurrence/sequence/revision |
| Operational ownership | scoped owner + `RuntimeDefinitionToken` |
| Presentation | display name only |

## Stable-ID rules

- Stable IDs never regenerate automatically on rename, move, import or
  `OnValidate`.
- Collision repair is explicit and targeted to the selected definition.
- Definition-local cleanup/release uses exact definition/token authority, never
  stable ID alone.
- Project-wide collision diagnostics do not turn stable ID into runtime equality.

## Deferred boundary

An application-scoped stable-ID resolver remains deferred until a real
persistence/external workflow requires it. When opened it must preserve explicit
typed resolution, collision diagnostics and the distinction between stable
boundary identity and runtime occurrence/ownership.

## Reopen criteria

Reopen only if evidence shows distinct definitions collapsing through stable ID,
release authority crossing definition tokens, wrong-occurrence correlation,
implicit stable-ID mutation or a concrete external-resolution requirement.
