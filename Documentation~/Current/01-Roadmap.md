# 01 — Roadmap

Status: **canonical operational tracker — Camera C9 reconciliation**
Decision: `ADR-PROD-0006-camera-requests-output-contexts.md`
Evidence matrix: `Camera-Delivery-Reconciliation.md`

This is the mutable status board for Camera. Product ADRs remain decisions;
historical notes and commit subjects are not the canonical sequence alone.

## Canonical sequence

| ID | Objective | Type | Status | Closure/evidence |
|---|---|---|---|---|
| C9A | Freeze request/output-scoped Camera architecture. | architecture | closed | ADR-PROD-0006 and package `c16c248`. |
| C9B | Remove superseded Director, activation and legacy bindings. | removal | closed | package, QA and FIRSTGAME paths removed. |
| C9C | Add typed request/output contracts. | runtime/contracts | closed | current request source; QA fixture exists. |
| C9D | Add one-output arbitration and restoration. | runtime | closed | `CameraOutputContext`; QA fixture exists. |
| C9E | Apply winner to explicit Cinemachine output. | runtime/Unity adapter | closed | `CameraOutputRigApplicator`; QA fixture exists. |
| C9F | Make context mutation and presentation transactional. | runtime | closed | `CameraOutputSession`; QA fixture exists. |
| C9G | Add typed Route and Activity publishers. | runtime | closed | current publishers and QA fixture. |
| C9H | Trial Route/Activity lifecycle adapters. | integration | superseded | removed after C9I; do not restore. |
| C9I | Bind Route and Activity to canonical lifecycle callbacks. | authoring/runtime/QA | closed | QA records eight lifecycle cases and Route exit cleanup. |
| C9K | Add Local Player eligibility publisher and binding. | authoring/runtime | closed | current binding and publisher. |
| C9L | Prove Route → Local Player → Activity restoration. | QA | closed | Player arbitration smoke PASS: ten cases, explicit invalid-Player block, restoration chain and Hub return with `blockingIssues='0'`. Fixture hygiene revalidation remains pending. |
| C9M | Wire the FIRSTGAME manual Camera integration. | integration | active | scene wiring exists; final Play Mode report is absent. |
| C9N | Separate virtual rig materialization from physical output creation. | authoring/editor | closed | current Composer/materializer; commit `232447a` had conflicting subject `C9L`. |
| C9O | Prove Activity teardown before Route unload. | QA | closed | Activity released before content disable; Route released; Hub return `blockingIssues='0'`. |

`C9J` has no implementation artifact and is intentionally unassigned.

## Historical ID mapping

| Found identifier | Canonical interpretation | Resolution |
|---|---|---|
| Package commit subject `C9L` (`232447a`) | C9N — rig/output separation | Retain Git history; current docs use the added C9N note and actual scope. |
| QA C9L | C9L — Player arbitration QA | Retained; it is the unambiguous canonical C9L scope. |
| FIRSTGAME C9M | C9M — manual consumer integration | Retained. |
| QA C9M | C9O — Activity-before-Route teardown QA | Renumbered in active tracking to avoid collision; historical paths stay C9M. |
| Product note C9N | C9N — rig/output separation | Retained. |

## First open gap and selected next cut

With C9L closed, the first real gap is C9M FIRSTGAME manual Camera integration.
Its scene wiring exists, but the final Play Mode validation report is absent.

**Next cut: C9M FIRSTGAME manual validation and evidence recording.** Use the
existing scene and protocol; do not add Camera runtime behavior, a scene builder
or a package API. C9M is not a PASS until that consumer result is recorded.

Multi-output registry/authoring, split-screen setup and online local/remote
eligibility are deferred—not selected—because the single-output consumer flow
is not closed.
