# 01 — Roadmap

Status: **canonical after R0**.

## Status vocabulary

| Status | Meaning |
|---|---|
| Closed | Implemented and supported by the appropriate package, QA or consumer evidence. |
| Active | The only selected execution lane. |
| Candidate | Valuable future work that is not authorized as the current lane. |
| Historical | Retained for traceability; not current guidance. |

## Closed product blocks

- F49 passive player foundation.
- PlayerRecipe MVP.
- PlayerComposer MVP.
- CameraRecipe MVP.
- CameraComposer MVP.
- QA Camera product proof.
- FIRSTGAME CameraComposer usage proof.
- Route/Activity explicit Cinemachine output apply-on-enter.
- Legacy camera architecture removal.

## Current active lane

```text
P2 — Player Control Product
```

No other lane is active.

## Active lane sequence

| Order | Cut | Status | Goal |
|---:|---|---|---|
| 1 | P2A — Player control authority and runtime binding audit | Closed / documentation | Authority, lifetime and accepted runtime boundary are recorded in [11-Player-Control-Authority-Audit.md](11-Player-Control-Authority-Audit.md). |
| 2 | P2A-QA0 — PlayerComposer Product Surface Regression Smoke | Closed / QA baseline | Captured the Composer baseline and the three pre-P2B materialization gaps. |
| 3 | P2B — Player control authoring | Implemented / pending Unity validation | Added designer-first control intent and canonical, blocking materialization validation. No runtime authority was created. |
| 4 | P2C — Binding contracts/runtime | Pending | Bind the exact authored PlayerSlot transactionally after P2B compile/import and QA confirmation. |
| 5 | P2D — Unity PlayerInput bridge | Pending | Own typed PlayerInput/action-map lifecycle and Gate availability. |
| 6 | P2E — Scoped runtime context | Pending | Add one explicit runtime authority per Player instance. |
| 7 | P2F — QA runtime | Pending | Prove bind, rollback, Gate, Pause, Transition and release. |
| 8 | P2G — FIRSTGAME movement proof | Pending | Prove framework control availability with game-owned movement. |

## Candidate future lanes

- Player spawn/materialization.
- Camera output lifetime/release.
- Progression save runtime.
- Transition/loading hardening.

## Execution order

```text
package audit and product decision
-> package implementation
-> QA technical validation
-> FIRSTGAME minimal consumer proof
```

## Consumer project rule

The package owns contracts, product surfaces and canonical documentation. QAFramework proves technical behavior. FIRSTGAME proves real-game usability. Consumer projects do not define framework contracts.

## Guardrails

- `CameraComposer` already resolves the main gameplay camera.
- `PlayerViewBehaviour` remains passive evidence.
- Do not turn PlayerView into camera authority merely to follow the historical F51 sequence.
- Do not resurrect F49M or PlayerView Binding Adapter as the active lane.
- Required configuration fails explicitly; there is no silent fallback, service locator or global manager.
