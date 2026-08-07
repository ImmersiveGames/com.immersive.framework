# IF-ADR-014 — Authored Definition and Stable Identity Authority

Status: Accepted  
Last updated: 2026-08-07  
Implementation completion: **Complete for current accepted scope**  
Implementation classification: **IF-ID-02..06 and IF-ID-08 complete; IF-ID-07 deferred by design**  
Related decisions: IF-ADR-001, IF-ADR-003, IF-ADR-006, IF-ADR-010, IF-ADR-015  
Closed execution record: [IF-ID-IDENTITY-AUTHORITY-EXECUTION-PLAN-2026-08-06](../Archive/Plans/IF-ID-IDENTITY-AUTHORITY-EXECUTION-PLAN-2026-08-06.md)

> IF-ID is closed for the current framework boundary. The application-scoped
> resolver in IF-ID-07 remains explicitly deferred until a real save/external
> boundary requires it and does not block this ADR's implementation closure.

## Context

Unity asset references and stable external identifiers solve different problems. Treating equal stable IDs as authored-definition equality can merge distinct assets, while relying only on references breaks persistence and external boundaries.

## Decision

Authored/runtime definition equality uses the exact `RouteAsset` or `ActivityAsset` reference. `RouteId` and `ActivityId` are stable projections for persistence, serialization, boundary evidence, diagnostics, and external references. Two distinct assets with the same stable ID are a collision and must not silently become the same authored definition.

Operational ownership (`RuntimeContentOwner`) for Route and Activity requires a process-local `RuntimeDefinitionToken` for the exact definition instance in addition to the stable ID.

Runtime occurrence identity remains definition-aware and occurrence-scoped. Stable ID is not lifecycle, readiness, release, supersession, or cleanup authority.

## Architectural constraints

- Runtime authority must be scoped, typed, and lifetime-explicit.
- Required invalid configuration must fail explicitly and diagnostically.
- Consumer code must not depend on internal runtime modules, reflection, object-name inference, or implicit global lookup.
- Editor tooling must be idempotent, non-destructive, and expose technical evidence through Advanced/Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability; permanent solutions belong in the package.
- Stable IDs must never regenerate automatically on rename, move, import, or `OnValidate`.
- Definition-local cleanup and release must use exact definition/token authority, never stable ID alone.

## Authority model

| Dimension | Question | Authority |
|---|---|---|
| Definition reference | Which exact authored asset? | exact typed asset reference |
| Stable boundary identity | Persistence / external / diagnostic identity | `RouteId` / `ActivityId` through explicit stable-ID semantics |
| Runtime occurrence | Which concrete execution? | definition reference + occurrence/sequence/revision |
| Operational ownership | Who may release resources? | scoped owner + `RuntimeDefinitionToken` |
| Presentation | What text is shown? | display name only |

## Status classification

| Area | Status |
|---|---|
| Package vocabulary + reference authority | **Complete** |
| Required operational definition tokens | **Complete** |
| Validation scopes + regenerate UX | **Complete** |
| Package runtime/Editor tests | **Passed** |
| Lifecycle/ownership/readiness QA matrix | **Passed** |
| QA idempotency / second execution | **Passed** |
| FIRSTGAME duplication/remediation workflow | **Passed** |
| Application-scoped ID resolver | **Deferred (IF-ID-07)** |

## Implemented in package

- `HasSameStableId`; obsolete `HasSameIdentity` for Route/Activity authored definition vocabulary.
- Definition equality via exact asset reference across lifecycle, readiness, content matching, and related request equality.
- `RuntimeDefinitionToken` + required Route/Activity owner tokens.
- Same stable ID + different definition tokens remains operationally distinct.
- Definition-local validation for the selected asset; collision findings point to the conflicting asset.
- Project identity audit remains a separate Advanced/Debug surface.
- Startup identity chain validation covers Startup Route + Startup Activity.
- Explicit `Regenerate Stable ID...` with confirmation, previous/new evidence, Undo, and selected-only mutation.
- Rename and move do not implicitly mutate stable identity.

## Package proof

Package runtime and Editor identity suites passed for the accepted scope, including:

- stable-ID vs exact-reference semantics;
- token requirement;
- distinct owners/hashes for same stable ID + different definition tokens;
- `FromUnityObject` stability;
- authoring collision context;
- unrelated collision non-blocking behavior;
- explicit regeneration and Undo.

## QAFramework proof

Canonical runner:

```text
Immersive Framework QA/Game Flow/Run Identity Authority Regression
```

Cases:

```text
baseline-authority-snapshot
route-collision-transition
activity-collision-transition
ownership-release-isolation
readiness-collision-isolation
legitimate-supersession-preservation
```

Validated in Unity `6000.5.0f1` with two consecutive executions in the same Play Mode session.

Observed closure evidence:

```text
status=Passed
executed=6
completed=6
failed=0
executionFailure=<none>
cleanupFailure=<none>
teardownFailure=<none>
roots before=3
roots after=3
```

The second execution recreated temporary definitions with new definition tokens while preserving the original Route/Activity authority and root counts, proving cleanup/idempotency for this QA surface.

The Activity collision case intentionally proves Activity lifecycle authority; the synthetic case does not require content bindings to close the IF-ID authority contract.

## FIRSTGAME proof — IF-ID-08

The consumer workflow was manually executed successfully:

```text
duplicate Route/Activity
→ local collision is visible
→ open the conflicting asset
→ regenerate only the copied stable ID
→ validate clean
→ use the repaired definitions in Play Mode
→ rename and move the repaired assets
→ confirm IDs/references remain stable
→ run again successfully
```

The final FIRSTGAME state is valid; no deliberate stable-ID collision is retained as product state.

This proves that a normal consumer can diagnose and repair a copied definition using package-owned authoring UX without local compatibility tooling or QA fixtures.

## Deferred

### IF-ID-07 — Application-scoped stable-ID resolver

Deferred until a real persistence/external boundary requires application-scoped resolution.

When opened, it must preserve:

- explicit typed resolution;
- collision diagnostics;
- no global implicit locator;
- no use of stable ID as runtime occurrence or release authority.

Broader application graph identity walking beyond the current Startup identity chain also remains outside the closed scope until a concrete product need exists.

## Completion criteria

- Distinct assets never compare equal solely because stable IDs collide. **Done**
- Stable-ID collision does not suppress legitimate Route/Activity transitions as already active. **Done**
- Operational owners of distinct assets do not share release authority through stable ID alone. **Done**
- Readiness/waits remain correlated to the correct authored definition/occurrence. **Done**
- Legitimate supersession preserves typed interruption evidence. **Done**
- Collisions are scoped, visible, navigable, and repaired only by explicit action. **Done**
- QA cleanup is idempotent across repeated execution. **Done**
- FIRSTGAME proves the real duplication/remediation workflow. **Done**
- Persistence/external application-scoped resolution. **Deferred IF-ID-07; not a closure blocker**

## Closure assessment

```text
Normative status: Accepted
IF-ID current scope: Closed
Package: IF-ID-02..06 complete
Package tests: Passed
QAFramework: 6/6 passed twice; cleanup/idempotency passed
FIRSTGAME IF-ID-08: Passed
IF-ID-07: Deferred by design
Unity minimum validated for this program: 6000.5.0f1
```

## Reopen criteria

Do not reopen IF-ID for general cleanup.

Reopen only if new evidence shows one of the following:

- distinct Route/Activity definitions collapse due to stable-ID equality;
- release/cleanup authority can cross definition tokens;
- readiness or supersession can correlate to the wrong definition/occurrence;
- stable identity mutates implicitly;
- a real persistence/external workflow requires IF-ID-07.
