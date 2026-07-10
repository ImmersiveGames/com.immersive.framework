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

| Order | Cut | Goal |
|---:|---|---|
| 1 | P2A — Player control authority and runtime binding audit | Establish authority, lifetime and accepted runtime boundary. |
| 2 | P2B — Player control recipe and authoring surface | Define reusable intent and designer-facing authoring. |
| 3 | P2C — PlayerControl binding adapter | Bind validated control evidence explicitly. |
| 4 | P2D — Unity PlayerInput bridge | Integrate the Unity Input System without hidden lookup. |
| 5 | P2E — Scoped Player control runtime context | Add runtime authority with explicit scope and dependencies. |
| 6 | P2F — QA technical validation | Prove positive, negative and lifecycle cases. |
| 7 | P2G — FIRSTGAME minimal control and movement proof | Prove minimal real-game consumption after QA. |

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
