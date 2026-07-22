# 05 - Execution Status

Status: **Canonical operational source**
Last updated: **2026-07-22**
Package version: **1.0.0-preview.17**

## H2.4 delivery baseline

| Repository | Commit | Meaning |
|---|---|---|
| `ImmersiveGames/com.immersive.framework` | `5ecaf0447530fe768f4123a7a1267b3152908d5a` | H2.4 package source: static host authority removed and version advanced to preview.16. |
| `rinnocenti/QAFramework` | `57622ce6` | H2.4 QA harness: explicit loaded-scene host resolver and focused smoke. |

## FRAMEWORK-HYGIENE-1 delivery baseline

| Repository | Commit / worktree | Meaning |
|---|---|---|
| `ImmersiveGames/com.immersive.framework` | `fe90949e401a5d01c9f12a75dbc989ce0d8ac02e` | Hygiene source cut: 18 files modified and 130 removed; superseded Pause/InputMode bridges and UnityInputTarget model removed. |
| `rinnocenti/QAFramework` | `2a388add59da9e8829c3200a5fa6761c32b5f574` | QA migration: removed IC2 bridge fixtures; retained pure InputMode and behavioral Input Gate regressions; removed retired CycleReset probe cases; migrated Pause P1 to the official surface. |

## Current state

| Scope | Source status | Validation status |
|---|---|---|
| H2 explicit runtime-port migration | Closed | Source changes delivered across H2.2.1-H2.2.13. |
| H2.4 static host authority removal | Closed | Unity evidence approved: import, compile and focused Play Mode smoke passed. |
| FRAMEWORK-HYGIENE-1 source cleanup | Closed | Package source is committed; superseded APIs must not be restored. |
| FRAMEWORK-HYGIENE-1 release gate | Passed | User-provided and approved Unity evidence confirms package compile, QA compile, framework boot, focused regressions and Pause lifecycle/reentry. |
| Post-H2 product lane | Not selected | May be selected without reopening H2.4. |

## FRAMEWORK-HYGIENE-1 validation record

Only user-provided Unity evidence may close this gate.

| Gate | Recorded result |
|---|---|
| Package compile | `Passed` |
| QA compile | `Passed` |
| Framework boot | `Passed` |
| Runtime Host Authority Regression | `Passed` — 10 cases |
| Player Input Mode Authority Regression | `Passed` — 13 cases |
| Input Gate Regression | `Passed` — 9 cases |
| Descriptor Equality Regression | `Passed` — 3 cases |
| Pause nominal | `Passed` |
| Route exit while paused | `Passed` |
| Clean reentry | `Passed` |
| Pause and Resume after reentry | `Passed` |
| Teardown integrity | No stale or duplicate callback; `Time.timeScale` restored to `1`. |

This Unity evidence was supplied and approved by the user; Codex did not execute
Unity. `FRAMEWORK-HYGIENE-1` is closed at `1.0.0-preview.17`. No compatibility
API was restored.

## H2.4 source evidence

```text
FrameworkRuntimeHost._current is absent
FrameworkRuntimeHost.TryGetCurrent is absent
FrameworkRuntimeHost.Create remains a stateless factory
package runtime bindings receive explicit narrow ports
QA resolves loaded hosts only inside its friend-assembly harness
QA requires exactly one deduplicated candidate
```

## Approved Unity validation evidence

The approved H2.4 evidence confirms:

1. Framework and QAFramework imported and compiled without errors.
2. `Immersive Framework > QA > Game Flow > H2.4 Run Static Host Authority Removal Smoke` passed in Play Mode.
3. The focused result was:

   ```text
   [H24_STATIC_HOST_AUTHORITY_REMOVAL_SMOKE]
   status='Passed'
   cases='10'
   ```

4. The QA resolver rejects ambiguous loaded-host candidate sets.
5. Package and QA source contain no static host lookup invocation.

## Failure policy

Fix the explicit binding or composition path that owns the dependency. Do not
restore a static current-host reference, static lookup API, service locator,
global manager, name lookup or silent fallback. Do not recreate superseded
Pause/InputMode or UnityInputTarget compatibility APIs for QA consumers.
