# 01 — Roadmap

Status: **canonical roadmap after F49M passive player binding foundation consolidation**.

## Reading rule

```text
Current capability first
Historical ADRs second
Candidate tracks only after explicit selection
```

Do not treat historical phase numbers as an active queue. Some phases are closed, some are superseded, and some were documentation-only.

## Status vocabulary

| Status | Meaning |
|---|---|
| Closed | Implemented/documented and validated by the appropriate evidence. |
| Frozen | Validated baseline; new behavior needs a new cut. |
| Superseded | Historical decision retained for traceability but no longer the current model. |
| Documentation-only | Planning/governance work without runtime behavior. |
| Candidate | Possible future lane; not selected. |
| Active | Current selected lane. Keep one active lane unless explicitly stated. |

## Active lane

| Lane | Status | Scope |
|---|---|---|
| F49M — Player Passive Binding Foundation Consolidation | Closed / Documentation-only | F49 passive player contracts, topology validators, readiness and diagnostics are closed as a validated foundation. |

## Active lane rule

F49 ran package-first and QA-first:

```text
1. Implement or adjust contracts/runtime/editor/docs in com.immersive.framework.
2. Validate technical behavior in QAFramework.
3. Validate practical usability in planet-devourer / FIRSTGAME only after QA is clean and the cut requires real usability proof.
```

FIRSTGAME was intentionally not used for F49 passive contracts because no real binding behavior exists yet.

## Closed stable lanes

| Lane | Status | Notes |
|---|---|---|
| Bootstrap / Route / Activity baseline | Closed/Frozen | Core app navigation baseline. |
| UIGlobal / Transition / Loading / Pause | Closed/Frozen | Visual/runtime surfaces exist and are validated at baseline level. |
| Gate / Unity PlayerInput gate | Closed/Frozen | Practical Pause/Transition input blocking exists. |
| Save boundaries | Closed/Frozen | Snapshot/preferences/progression-adapter boundary exists; save engine remains future. |
| Reset Reform preview.12 | Closed | Current reset/restart model. |
| FIRSTGAME reset usage model | Closed | Real usage proof passed. |
| Consumer role rule | Frozen | QA proves technical behavior; FIRSTGAME proves game-start usability; package owns canonical docs/contracts. |
| Consumer project separation POST-RESET-B1-B6F | Closed | Package, QA Project and FIRSTGAME roles are documented and frozen; consumer cleanup remains bounded by Unity serialization rules. |
| F49 passive player binding foundation | Closed | Actor readiness, PlayerEntry, topology, view, control, readiness and diagnostics are validated passively through QA. |

## F49 closed sequence

```text
F49A — ADR normalization and package boundary cleanup
F49B — Actor Readiness passive contracts
F49C — Actor Readiness Unity adapter + QA smoke
F49D — PlayerEntry passive model
F49E — PlayerEntry Unity adapter
F49F — PlayerTopology passive validation
F49G — PlayerView passive contract
F49H — PlayerView topology validation
F49I — PlayerControl passive contract
F49J — PlayerControl topology validation
F49K — Player binding readiness summary
F49L — Player binding diagnostic reporter
F49M — Documentation consolidation and next-phase handoff
```

## F49 boundary

The closed F49 foundation remains passive:

```text
viewBinding = false
controlBinding = false
cameraActivation = false
inputActivation = false
movement = false
actorSpawning = false
```

## Recommended next implementation block

Do not continue adding passive taxonomy unless a concrete missing invariant is found.

Recommended order:

| Order | Candidate cut | Purpose |
|---:|---|---|
| 1 | Player Binding Authoring Validator | Editor/QA validation for complete authored chain before runtime binding. |
| 2 | PlayerView Binding Adapter | First real view binding surface, still behind readiness diagnostics. |
| 3 | PlayerControl Binding Adapter | First real control binding surface, still without broad gameplay movement assumptions. |
| 4 | Optional Unity PlayerInput Bridge | Explicit Unity Input System bridge after control binding boundary is stable. |
| 5 | FIRSTGAME usability proof | Real game integration only after QA technical behavior is clean. |

## Candidate next lanes

Do not run these in parallel with a selected player binding implementation unless explicitly decided.

| Option | Candidate lane | Why it matters | Risk if skipped |
|---|---|---|---|
| A | Player Binding Authoring Validator | Turns the passive foundation into editor-diagnosable authoring feedback. | Runtime binding begins without clear authoring gates. |
| B | PlayerView Binding Adapter | Connects validated PlayerView evidence to real camera/view selection. | View ownership remains theoretical. |
| C | PlayerControl Binding Adapter | Connects validated PlayerControl evidence to real control ownership. | Control remains theoretical. |
| D | Unity PlayerInput Bridge | Integrates optional Unity Input after ownership is explicit. | Input integration remains ad-hoc. |
| E | FIRSTGAME Usage Model Hardening | Proves practical usability after technical QA. | Framework works technically but remains hard to use. |
| F | Transition / Loading Surface Hardening | Improve progress/fade/readiness evidence and failure clarity. | Visual lifecycle becomes hard to debug. |
| G | Runtime Spawned Object / Materialization Track | Clarify runtime prefab/materialization ownership beyond reset. | Runtime objects remain ad-hoc per game. |
| H | Progression Save Adapter Track | Start real game-save handoff while keeping engine interchangeable. | Save boundary stays theoretical. |

## Consumer cleanup guardrail

Future cleanup must preserve the split in [`03-Consumer-Project-Roles.md`](03-Consumer-Project-Roles.md).

Do not rename or move Unity serialized scenes, prefabs, assets or attached MonoBehaviour scripts from consumer projects without a Unity Editor migration plan.
