# IF-ID Identity Authority — Closed Execution Record

**Opened:** 2026-08-06  
**Closed:** 2026-08-07  
**ADR:** IF-ADR-014 — Authored Definition and Stable Identity Authority  
**Status:** Closed for current scope  
**Deferred:** IF-ID-07 application-scoped resolver

> This file is the archived execution record for IF-ID. Current mutable status
> belongs in `Architecture/Tracking/IF-TRACK-Framework.md`.

## Goal

Make authored-definition equality use the exact `RouteAsset` / `ActivityAsset` reference, keep `RouteId` / `ActivityId` for stable boundary evidence, and stop treating stable-ID collisions as the same authored definition.

## Final authority model

| Dimension | Question | Authority |
|---|---|---|
| Definition reference | Which exact authored asset? | exact typed asset reference |
| Stable boundary ID | Persistence / external / diagnostics evidence | `RouteId` / `ActivityId` via stable-ID semantics |
| Runtime occurrence | Which concrete execution? | definition reference + occurrence / revision / sequence |
| Operational ownership | Who may release resources? | scope + stable ID evidence + `RuntimeDefinitionToken` |
| Presentation | What text is shown? | display name only |

## Final cut status

| Cut | Final status | Evidence |
|---|---|---|
| **IF-ID-01** | Done | Identity audit completed |
| **IF-ID-02** | Done | Vocabulary + package baseline tests |
| **IF-ID-03** | Done | Route exact-reference authority |
| **IF-ID-04** | Done | Activity exact-reference authority |
| **IF-ID-05** | Done | Required operational definition tokens + ownership isolation |
| **IF-ID-06** | Done | Validation scopes + explicit regenerate UX |
| **IF-ID-07** | Deferred | Open only for a real save/external resolution boundary |
| **IF-ID-08** | Done | FIRSTGAME duplication/remediation workflow |

## Package closure

### Vocabulary and reference authority

- `HasSameStableId` is the explicit stable-ID comparison.
- Route/Activity `HasSameIdentity` is obsolete and does not define authored-definition authority.
- Lifecycle/readiness definition equality uses exact asset references.

### Operational ownership

- `RuntimeDefinitionToken` is process-local and non-persistent.
- Route/Activity owners require a valid definition token.
- Production definitions use `RuntimeDefinitionToken.FromUnityObject`.
- Synthetic QA/tests may use `MintAnonymous`.
- Same stable ID with different definition tokens cannot collapse release authority.

### Validation and product UX

- Definition-local validation reports only the selected definition's collision.
- Collision evidence points to the conflicting asset.
- Project-wide identity audit remains an Advanced/Debug operation.
- Startup validation covers the Startup Route + Startup Activity identity chain.
- `Regenerate Stable ID...` is explicit, confirms mutation, records previous/new identity, supports Undo, and changes the selected asset only.
- Rename/move/import do not regenerate identity automatically.

## Package proof

Runtime and Editor identity test suites passed for the closed scope.

Required proof includes reference/stable-ID semantics, token requirements, owner equality/hash isolation, token stability, collision context, selected-only regeneration, and Undo.

## QAFramework closure

One canonical public IF-ID runner remains:

```text
Immersive Framework QA/Game Flow/Run Identity Authority Regression
```

Canonical cases:

```text
baseline-authority-snapshot
route-collision-transition
activity-collision-transition
ownership-release-isolation
readiness-collision-isolation
legitimate-supersession-preservation
```

Final validation:

```text
Unity: 6000.5.0f1
execution 1: Passed 6/6
execution 2: Passed 6/6
execution failures: none
cleanup failures: none
teardown failures: none
roots before/after: 3 / 3 on both runs
```

The second execution in the same Play Mode session proved the runner leaves no authority/root state required by a later run.

Legacy duplicated IF-ID smokes were consolidated so the six-case runner is the canonical technical integration surface.

## FIRSTGAME closure — IF-ID-08

Manual consumer flow passed:

```text
duplicate
→ detect local collision
→ open conflicting definition
→ regenerate copied stable ID
→ validate
→ Play Mode
→ rename
→ move
→ Play Mode again
```

Final state requirements passed:

- original definition keeps its stable ID;
- copied definition receives a new stable ID;
- renamed/moved repaired definitions preserve stable identity;
- serialized references remain valid;
- repaired Route/Activity work in the real consumer flow;
- no deliberate collision remains in the final project state.

## Deferred item

### IF-ID-07

Application-scoped stable-ID resolver remains deferred until a real persistence/external boundary needs it.

This is a deliberate scope decision, not incomplete current-runtime authority.

## Unity compatibility

```text
package.json unity: 6000.5
package.json unityRelease: 0f1
official minimum: Unity 6000.5.0f1
```

No earlier Unity support/test matrix is implied by IF-ID closure.

## Final assessment

```text
IF-ID package implementation: complete
IF-ID package tests: passed
IF-ID QA technical validation: complete
IF-ID QA repeated-run/idempotency validation: complete
IF-ID FIRSTGAME product proof: complete
IF-ID-07 application resolver: deferred
Program status: CLOSED FOR CURRENT SCOPE
```

## Reopen only when

- exact definition authority regresses;
- stable-ID collision crosses ownership/release authority;
- readiness/occurrence correlation regresses;
- identity mutates implicitly;
- or a real persistence/external workflow requires IF-ID-07.
