# 01 — Roadmap

Status: **canonical after camera architecture reset decision**

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
| Superseded | Previously accepted implementation or decision replaced by a newer canonical architecture. |

## Closed product blocks

- R0 documentation and roadmap baseline reconciliation.
- F49 passive Player foundation.
- PlayerRecipe MVP.
- PlayerComposer MVP.
- P2 Player control authoring, QA availability proof and FIRSTGAME movement proof.
- Cinemachine package and local rig-materialization technical proof.
- Explicit Player camera target evidence.

## Superseded camera blocks

The following are historical evidence, not current product architecture:

```text
CameraRecipe / CameraComposer single-player MVP as final runtime shape
Route/Activity explicit output apply-on-enter
FrameworkCameraDirector coordination
Route/Activity camera binding authority
PlayerView camera activation chain
direct cross-owner Cinemachine priority selection
```

Cinemachine materialization techniques may be reused where compatible with ADR-PROD-0006. Ownership and activation decisions from C3–C8 must not be retained.

## Current active block

```text
C9 — Camera Requests and Output Contexts
```

No other block is active.

### C9 sequence

| Order | Cut | Status | Goal |
|---:|---|---|---|
| 1 | C9A — Architecture ADR and documentation reset | Closed by documentation delta | Freeze requests/output contexts and supersede C3–C8 ownership decisions. |
| 2 | C9B — Destructive legacy removal | Active next | Physically remove Director, Route/Activity bindings, PlayerView activation and incompatible QA/docs without compatibility wrappers. |
| 3 | C9C — Request and output contracts | Pending | Add explicit owner, lifetime, output, target and policy contracts. |
| 4 | C9D — Single-output runtime authority | Pending | Implement one scoped `CameraOutputContext` for one Unity Camera/CinemachineBrain output. |
| 5 | C9E — Cinemachine request application | Pending | Apply the winning request through Cinemachine without reimplementing presentation. |
| 6 | C9F — Route, Activity and Player publishers | Pending | Publish and release typed requests without competing authorities. |
| 7 | C9G — QA arbitration/restoration | Pending | Prove Route -> Activity -> Player -> Activity override -> Player restoration and negative cases. |
| 8 | C9H — FIRSTGAME manual proof | Pending | Build the real flow manually and prove usability. |

## Ordered future blocks

| Order | Block | Goal | Activation condition |
|---:|---|---|---|
| 1 | P3 — Player Spawn / Runtime Materialization | Define ExistingSceneInstance and InstantiatePrefab through explicit scoped policies. | C9 closed. |
| 2 | S1 — Progression Save Runtime | Add progression save contracts and interchangeable backend. | P3 closed and FIRSTGAME has meaningful state to persist. |

## Candidates

- Transition/loading hardening.
- Pause UX advanced work.
- Additional CameraRigRecipes.
- Multiplayer Player.
- Input rebinding.
- Generic Actor/NPC spawning.
- Generic interaction product.
- Expanded templates and samples.

## Execution order

```text
architecture decision
-> destructive removal of conflicting shape
-> package contracts/runtime/tooling
-> QA technical validation
-> FIRSTGAME manual usability proof
```

## Guardrails

- Do not add new camera integration on the superseded C3–C8 ownership model.
- Do not preserve old camera classes through `[Obsolete]`, wrappers, aliases or hidden compatibility.
- Cinemachine remains mandatory and owns presentation mechanics.
- `PlayerComposer` remains a target source, not camera runtime authority.
- Request precedence belongs to `CameraOutputContext`.
- One output has one explicit scoped camera authority.
- Required configuration fails explicitly.
- No silent fallback, service locator, global manager, `Camera.main` authority or functional object-name lookup.
