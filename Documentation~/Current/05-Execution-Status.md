# 05 - Execution Status

Status: **Canonical operational source**
Last updated: **2026-07-20**
Package version: **1.0.0-preview.16**

## H2.4 delivery baseline

| Repository | Commit | Meaning |
|---|---|---|
| `ImmersiveGames/com.immersive.framework` | `5ecaf0447530fe768f4123a7a1267b3152908d5a` | H2.4 package source: static host authority removed and version advanced to preview.16. |
| `rinnocenti/QAFramework` | `57622ce6` | H2.4 QA harness: explicit loaded-scene host resolver and focused smoke. |

## Current state

| Scope | Source status | Validation status |
|---|---|---|
| H2 explicit runtime-port migration | Closed | Source changes delivered across H2.2.1-H2.2.13. |
| H2.4 static host authority removal | Closed | Unity evidence approved: import, compile and focused Play Mode smoke passed. |
| Post-H2 product lane | Not selected | May be selected without reopening H2.4. |

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
global manager, name lookup or silent fallback.
