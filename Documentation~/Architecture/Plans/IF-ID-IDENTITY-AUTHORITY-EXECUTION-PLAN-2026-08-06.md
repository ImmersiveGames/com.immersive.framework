# IF-ID Identity Authority — Execution Plan

**Date:** 2026-08-06  
**Source audit:** [IMMERSIVE-FRAMEWORK-IF-ID-01-IDENTITY-AUTHORITY-AUDIT-2026-08-06](./IMMERSIVE-FRAMEWORK-IF-ID-01-IDENTITY-AUTHORITY-AUDIT-2026-08-06/)  
**ADR:** IF-ADR-014 — Authored Definition and Stable Identity Authority  
**Rule:** package first, QA second, FIRSTGAME proof after the official contract is stable.

## Goal

Make authored-definition equality use the exact `RouteAsset` / `ActivityAsset` reference, keep `RouteId` / `ActivityId` for stable boundary evidence, and stop treating stable-ID collisions as the same authored definition.

## Authority model (target)

| Dimension | Question | Authority |
|---|---|---|
| Definition reference | Which exact authored asset? | `ReferenceEquals` on typed asset |
| Stable boundary ID | Persistence / external / diagnostics evidence | `RouteId` / `ActivityId` via `HasSameStableId` |
| Runtime occurrence | Which concrete execution? | Reference + occurrence / revision / handle |
| Operational ownership | Who may release resources? | Stable ID + definition token (`RuntimeContentOwner`) |
| Presentation | What text is shown? | Display name only |

## Cut sequence

| Cut | Status | Objective | Behavior change? |
|---|---|---|---|
| **IF-ID-01** | Done (audit) | Map every identity use | No |
| **IF-ID-02** | **Done** | Explicit vocabulary + package baseline tests | No lifecycle semantics |
| **IF-ID-03** | **Done** | Route active-target, events, host, context, readiness OwnsRoute | Yes — reference authority |
| **IF-ID-04** | **Done** | Activity active-target, finalization, content, readiness OwnsActivity | Yes — reference authority |
| **IF-ID-05** | **Done** | Operational owner = stable ID + definition token | Yes — ownership model |
| **IF-ID-06** | **Done** | Validation scopes + Regenerate Stable ID UX | Product/editor |
| **IF-ID-07** | Deferred | Application-scoped ID resolver | Only when save boundary needs it |
| **IF-ID-08** | Pending | FIRSTGAME duplication remediation proof | Consumer proof |

## IF-ID-02 — Vocabulary

- Add `HasSameStableId` on `RouteAsset` and `ActivityAsset`.
- Mark `HasSameIdentity` obsolete; body delegates to `HasSameStableId`.
- Package tests prove: same ref, different refs/IDs, different refs/same ID.
- Do not introduce a generic `Identity` comparison API.

## IF-ID-03 — Route reference authority

Migrate definition comparisons to `ReferenceEquals` in:

- `RouteLifecycleRuntime` (`IsRouteActive`, enter/exit publish, previous-scope cleanup gate)
- `FrameworkRuntimeHost` Route state reconciliation
- `ActivityFlowRuntime.SetRouteContext` / discovery-scope rollover
- `ActivityEntryReadinessActiveOperation.OwnsRoute`
- Route-side “same definition” helpers (`RouteContentBinding`, `RouteContentRuntime`, Transition/CycleReset request equality, camera/object-entry ownership matching, player admission “distinct route” checks)

**Owner model (IF-ID-05 landed):**

`RuntimeContentOwner` equality is `Scope + OwnerIdentity (stable ID) + DefinitionToken` (`EntityId`).
Route/Activity factories pass `GetEntityId()` as the definition token so two assets that share a stable ID never share release authority. Stable ID remains boundary evidence (`HasSameStableDefinition`).

## IF-ID-04 — Activity reference authority

Migrate definition comparisons to `ReferenceEquals` in:

- `ActivityFlowRuntime.IsActivityActive`
- Transaction previous-Activity finalization gates
- Activity content enter/exit classification
- Scene ledger / composition / local visibility / discovery matching
- `ActivityEntryReadinessActiveOperation.OwnsActivity` (exact Activity reference; occurrence remains on the wait scope)
- Player admission “same activity” / reentry correlation helpers that currently use stable ID

Occurrence identity for readiness wait correlation remains `ActivityReadinessOccurrence` (reference + sequence).

## IF-ID-05 — Ownership (landed)

- `RuntimeContentOwner.DefinitionToken` participates in equality and diagnostics.
- Route/Activity owner creation passes Unity asset `EntityId` (`GetEntityId()`).
- Temporary collision skip for previous-scope removal removed (owners no longer collide on stable ID alone).
- Package tests prove distinct tokens with shared stable IDs.

## IF-ID-06 — Validation / UX (landed)

- Definition-local: missing/invalid ID + collisions involving the selected asset.
- Game Application: uniqueness among reachable Startup Route / Startup Activity graph.
- Project audit: all collisions, labeled `scope='Project audit'`, shown under Advanced/Debug only.
- `Regenerate Stable ID...` with confirmation, previous/new evidence, Undo, validation refresh.

## Out of scope / remaining

- Application catalog resolver (IF-ID-07)
- FIRSTGAME product workflow proof (IF-ID-08)
- Broader application graph walk beyond Startup Route chain
- Occurrence-sequence minting beyond definition-token uniqueness
- `PlayerInputActionMapReference.HasSameIdentity` (different domain; not Route/Activity)

## Acceptance (IF-ID-02..06)

- No new code calls `HasSameIdentity` for Route/Activity.
- Lifecycle target equality uses references when both sides are available.
- Stable-ID collisions no longer suppress Route/Activity transitions as “already active”.
- Operational owners of distinct assets never match solely because stable IDs collide.
- Local Inspectors do not block on unrelated project collisions.
- Explicit regenerate remediation exists with confirmation.
- Package identity baseline tests cover reference, stable-ID and owner-token cases.

## Suggested commits

```text
refactor(identity): clarify stable-id equality and add migration QA baseline
refactor(route): use authored definition reference as runtime target authority
refactor(activity): align lifecycle and readiness with reference authority
refactor(runtime-content): separate stable identity from scoped ownership
feat(identity): separate validation scopes and add explicit regeneration UX
```

(May land as one coordinated package commit if preferred.)
