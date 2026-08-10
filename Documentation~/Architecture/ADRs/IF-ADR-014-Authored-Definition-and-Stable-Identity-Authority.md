# IF-ADR-014 — Authored Definition and Stable Identity Authority

Status: **Accepted**  
Last updated: 2026-08-10  
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-003, IF-ADR-006, IF-ADR-009, IF-ADR-010, IF-ADR-013, IF-ADR-015  
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

## Conformance evidence — IF-ADR-013 Optional Audio BGM Adapter

IF-ADR-013 is a concrete consumer of this identity authority and does not change
its accepted boundary. The optional BGM integration preserves exact
Route/Activity authored-definition authority and does not use audio cue identity,
desired BGM state or confirmed BGM state as Route/Activity equality, lifecycle
ownership, release authority or occurrence identity.

The 2026-08-10 IF-ADR-013A technical certification provides supporting
conformance evidence: Route/Activity BGM precedence, Route-scoped retained-state
cleanup and optional-authority failure behavior passed inside the existing scoped
Route/Activity lifecycle. This evidence does not reopen IF-ADR-014 and does not
change its completion/status; it demonstrates that the newer optional audio
feature respects the accepted identity model.

### IF-ADR-009 conformance evidence — 2026-08-10

The ADR-009 closure adds direct evidence for this identity boundary. Activity
Local Visibility now rejects distinct authored Activity definitions that collide
on the same stable `ActivityId`, while occurrence/release/restoration ownership
continues to use definition-aware runtime authority.

This is conformance evidence only. It does not change ADR-014 status or reopen
its accepted boundary.

## Deferred boundary

An application-scoped stable-ID resolver remains deferred until a real
persistence/external workflow requires it. When opened it must preserve explicit
typed resolution, collision diagnostics and the distinction between stable
boundary identity and runtime occurrence/ownership.

## Reopen criteria

Reopen only if evidence shows distinct definitions collapsing through stable ID,
release authority crossing definition tokens, wrong-occurrence correlation,
implicit stable-ID mutation or a concrete external-resolution requirement.
