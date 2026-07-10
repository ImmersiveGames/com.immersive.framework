# 01 — Roadmap

Status: **canonical after P2 closure and G1 selection**

For the exact operational next step, read [Execution Status](05-Execution-Status.md).

## Status vocabulary

| Status | Meaning |
|---|---|
| Closed | Implemented and supported by the appropriate package, QA or consumer evidence. |
| Active | The only selected execution block. |
| Ordered | Accepted future block with a fixed position after the active block. |
| Candidate | Valuable future work not authorized in the ordered sequence. |
| Rejected | Proposed shape was tested or reviewed and intentionally not retained. |
| Historical | Traceability only; not current guidance. |

## Closed product blocks

- R0 documentation and roadmap baseline reconciliation.
- F49 passive Player foundation.
- PlayerRecipe MVP.
- PlayerComposer MVP.
- CameraRecipe MVP.
- CameraComposer MVP.
- QA Camera product proof.
- FIRSTGAME CameraComposer usage proof.
- Route/Activity explicit Cinemachine output apply-on-enter.
- Legacy camera architecture removal.
- P2 Player control authoring, QA availability proof and FIRSTGAME movement proof.

## P2 closure record

| Item | Status | Accepted result |
|---|---|---|
| P2A | Closed | Control boundary and ownership audited. |
| P2B | Closed | Designer-first Control section and materialization. |
| P2C original runtime binding adapter | Rejected/reverted | No retained new binding authority. |
| P2D | Closed | QA PlayerInput runtime baseline, 13/13. |
| P2E | Closed | QA Gate/Pause/Transition block and restoration, 14/14. |
| P2F | Absorbed | Covered by P2D/P2E evidence. |
| P2G | Closed | FIRSTGAME Move input and game-owned movement, 11/11. |

The detailed original P2 intent remains in the consolidated plan, but the accepted final shape is governed by [Execution Status](05-Execution-Status.md).

## Current active block

```text
G1 — Minimal Playable Loop
```

No other block is active.

### G1 sequence

| Order | Cut | Status | Goal |
|---:|---|---|---|
| 1 | G1A — FIRSTGAME Minimal Playable Loop Audit | Active | Map existing objective, interaction, Reset, Activity Restart, camera and control behavior before creating anything. |
| 2 | G1B — Minimal missing loop composition | Pending | Add only the smallest singular FIRSTGAME piece shown missing by G1A. |
| 3 | G1C — Integrated runtime proof | Pending | Prove the complete loop returns to a playable initial state. |

## Ordered future blocks

| Order | Block | Goal | Activation condition |
|---:|---|---|---|
| 1 | P3 — Player Spawn / Runtime Materialization | Define ExistingSceneInstance and InstantiatePrefab through explicit scoped policies. | G1 closed. |
| 2 | C9 — Camera Output Lifetime / Release | Release Activity output, restore Route output and release Route output without fallback. | P3 closed or explicitly deferred with rationale. |
| 3 | S1 — Progression Save Runtime | Add progression save contracts and interchangeable backend. | C9 closed and FIRSTGAME has meaningful state to persist. |

## Candidates

- Transition/loading hardening.
- Pause UX advanced work.
- Additional camera modes.
- Multiplayer Player.
- Input rebinding.
- Generic Actor/NPC spawning.
- Generic interaction product.
- Expanded templates and samples.

## Execution order

```text
product/audit decision
-> package implementation when an official contract is needed
-> QA technical validation
-> FIRSTGAME real usability proof
```

For G1, the active work is consumer integration first because it composes already-supported official systems and must reveal any real product gap before new package APIs are introduced.

## Guardrails

- Do not start P3, C9 or S1 while G1 is active.
- Do not create a new Player runtime context merely to match the discarded P2 proposal.
- Movement remains game-owned.
- `CameraComposer` remains the main gameplay-camera surface.
- `PlayerViewBehaviour` remains passive evidence.
- Required configuration fails explicitly.
- No silent fallback, service locator, global manager, `Camera.main` authority or functional object-name lookup.
