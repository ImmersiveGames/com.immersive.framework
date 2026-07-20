# 01 - Roadmap

Status: **H2 closed and Unity-validated**
Last reconciled: **2026-07-20**
Version: **1.0.0-preview.16**

For the exact operational state and handoff, read `05-Execution-Status.md`.

## H2 objective

Replace Unity-facing runtime-host discovery with explicit narrow ports and
bindings. The package composition root owns scoped runtimes; adapters receive
only the capability they need.

## Delivered sequence

| Cut | Boundary delivered | Source status |
|---|---|---|
| H2.2.1 | Pause input-mode runtime port | Closed |
| H2.2.2-H2.2.6 | Route, Activity, cycle-reset and Activity-restart ports | Closed |
| H2.2.7-H2.2.10 | Reset execution, selection, input gate and registration ports | Closed |
| H2.2.11 | Content Anchor materialization port and bridge binding | Closed |
| H2.2.12 | Player Actor selection port and authoring binding | Closed |
| H2.2.13 | Runtime diagnostics port and QA binding hygiene | Closed |
| H2.4 | Remove static current-host authority; keep QA resolution local to its harness | Closed; Unity evidence approved |

## H2.4 approved validation evidence

```text
Framework import and compile completed
QAFramework import and compile completed
H2.4 Play Mode smoke passed
static host field absent
static lookup API absent
no package or QA static lookup invocation
QA resolver rejects ambiguous host candidates
```

Expected focused result:

```text
[H24_STATIC_HOST_AUTHORITY_REMOVAL_SMOKE]
status='Passed'
cases='10'
```

## Next selection

No post-H2 cut is ordered or active. The next cut may be selected now. Do not
reintroduce a static host registry, lookup API, global manager, service locator
or silent fallback.
