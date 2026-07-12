# 01 — Roadmap

Status: **canonical after C9 closure; G1 Route-loop scope selected**  
Last reconciled: **2026-07-12**  
Decision: `ADR-PROD-0006-camera-requests-output-contexts.md`  
Evidence matrix: `Camera-Delivery-Reconciliation.md`

For the exact operational next step, read `05-Execution-Status.md`.

## Status vocabulary

| Status | Meaning |
|---|---|
| Closed | Implemented and supported by the required package, QA or consumer evidence. |
| Active | The single selected execution block. |
| Ordered | Accepted future block with a fixed position after the active block. |
| Candidate | Valuable future work not selected for execution. |
| Superseded | Trial or historical shape removed from the supported product. |

## C9 canonical sequence

| ID | Objective | Type | Status | Closure/evidence |
|---|---|---|---|---|
| C9A | Freeze request/output-scoped Camera architecture. | architecture | closed | ADR-PROD-0006 accepted. |
| C9B | Remove Director, activation and legacy ownership paths. | removal | closed | package, QA and FIRSTGAME legacy paths removed. |
| C9C | Add typed request/output contracts. | runtime/contracts | closed | package contracts and QA fixture. |
| C9D | Add one-output arbitration and restoration. | runtime | closed | `CameraOutputContext`. |
| C9E | Apply the winner to an explicit Cinemachine rig. | runtime/Unity adapter | closed | `CameraOutputRigApplicator`. |
| C9F | Make arbitration and presentation transactional. | runtime | closed | `CameraOutputSession`. |
| C9G | Add typed Route and Activity publishers. | runtime | closed | typed publishers and QA fixture. |
| C9H | Trial lifecycle adapters. | integration | superseded | removed after canonical lifecycle integration. |
| C9I | Bind Route and Activity to canonical lifecycle. | authoring/runtime/QA | closed | lifecycle QA, eight cases. |
| C9K | Add Local Player eligibility publication. | authoring/runtime | closed | binding, publisher and QA consumption. |
| C9L | Prove Player arbitration and restoration. | QA | closed | ten-case QA PASS. |
| C9M | Wire and prove the FIRSTGAME consumer integration. | integration | closed | persistent output, injected consumers, manual runtime and visual proof. |
| C9N | Separate virtual rig materialization from physical output creation. | authoring/editor | closed | current Composer/materializer shape. |
| C9O | Prove Activity teardown before Route unload. | QA | closed | teardown evidence and Hub return without blockers. |
| C9Q | Author and materialize Follow framing. | UX/product/editor | closed | Follow Pipeline four-case PASS and FIRSTGAME distinct framing proof. |
| C9R | Add persistent Session output, explicit override authority and transition integration. | runtime/product/QA/integration | closed | QA eleven-case PASS; FIRSTGAME transition and manual override/release PASS. |

`C9J` has no canonical implementation artifact. No package commit identified as
`C9P` defines an additional canonical cut; missing letters must not be filled by
inference.

## Camera closure

The accepted single-player precedence is:

```text
Local Player 50 < Activity 100 < Route 200 < Session 300
```

The output lives in `UIGlobal`. Route and Activity become available through
their lifecycle but publish only through explicit override requests. Session is
requested around transitions and released before destination content is
revealed.

Camera C9 is therefore closed at the current single-output product level.

## Active block

```text
G1 — Consumer Route Loop
```

### Corrected goal

Prove that the existing framework systems support a complete application flow
through real Routes:

```text
Bootstrap
-> Menu Route
-> Gameplay Route
-> Ending Route or Menu Route
-> controlled return/re-entry
```

This is a framework lifecycle/integration proof, not a generic gameplay system.

### First cut

```text
G1A — FIRSTGAME Route Loop Audit and Scope Lock
```

G1A must inventory the real FIRSTGAME Routes and incorporate the additional
requirements selected for that block before implementation.

It must answer:

```text
Which Route is the entry/menu?
Which Route is gameplay?
Does an ending Route exist, or does gameplay return directly to menu?
Which Route requests already exist?
Which transition/loading/pause/input/camera surfaces participate?
What state must be released or restored across the loop?
What additional FIRSTGAME-specific proof is intentionally included?
```

### Not mandatory for framework closure

```text
generic objective system
generic interaction system
combat
mission state
win-condition authority
resettable gameplay object
framework-owned movement or gameplay controller
```

FIRSTGAME may include singular gameplay content, but that content does not
become a package contract merely because it participates in the demonstration.

## Ordered continuation

| Order | Block | Goal | Activation condition |
|---:|---|---|---|
| 1 | P3 — Player Spawn / Runtime Materialization | Define explicit existing-instance and instantiate-prefab policies with scoped lifetime. | G1 closed or explicitly deferred. |
| 2 | S1 — Progression Save Runtime | Add progression save contracts and interchangeable backend. | FIRSTGAME has meaningful state worth persisting. |

Camera multi-output, split-screen, additional camera modes, transition/loading
hardening, input rebinding, generic interaction and multiplayer remain
candidates, not active lanes.

## Execution guardrails

- Only one block is active.
- Do not create gameplay authority in the framework to satisfy G1.
- Movement and game rules remain consumer-owned.
- Package contracts are added only when a real integration gap proves they are
  necessary.
- Required invalid configuration fails explicitly.
- No silent fallback, global manager, service locator or functional name lookup.
