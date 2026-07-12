# Camera Delivery Reconciliation

Status: **canonical evidence matrix**  
Last audited: 2026-07-11  
Decision: `ADR-PROD-0006-camera-requests-output-contexts.md`

This matrix reconciles current source, package history/docs, QAFramework and
FIRSTGAME. A fixture, template or commit subject is not a Play Mode PASS alone.

| ID | Found name(s) | Objective / key source | Package | QA | FIRSTGAME | Status |
|---|---|---|---|---|---|---|
| C9A | C9A | ADR-PROD-0006 | accepted | n/a | n/a | closed |
| C9B | C9B | removal of old authority | removed | legacy smoke removed | old paths removed | closed |
| C9C | C9C | `Runtime/Camera/Requests` | implemented | fixture | n/a | closed implementation |
| C9D | C9D | `CameraOutputContext` | implemented | fixture | n/a | closed implementation |
| C9E | C9E | `CameraOutputRigApplicator` | implemented | fixture | n/a | closed implementation |
| C9F | C9F | `CameraOutputSession` | implemented | fixture | n/a | closed implementation |
| C9G | C9G | Route/Activity publishers | implemented | fixture | n/a | closed implementation |
| C9H | C9H | temporary lifecycle adapters | removed | smoke removed | n/a | superseded |
| C9I | C9I, `e42edee` | canonical Route/Activity bindings | implemented | eight closed lifecycle cases documented | n/a | closed |
| C9K | C9K, `0e294ac` | Local Player binding/publisher | implemented | consumed by C9L | not run independently | closed implementation |
| C9L | QA C9L | Route → Player → Activity restoration | existing runtime | PASS, 10 cases: invalid Player block, precedence/restoration chain and Hub return `blockingIssues='0'`; post-cleanup hygiene rerun pending | n/a | closed |
| C9M | FIRSTGAME C9M, `34ee718` | consumer wiring/protocol | local path dependency | n/a | output, rigs, bindings serialized; report blank | active |
| C9N | C9N; package commit subject C9L (`232447a`) | virtual rig / physical output separation | implemented | teardown installer corrected | correction requires restart | closed |
| C9O | QA C9M, `2cad3e8`, `82af11f` | Activity teardown before Route unload | existing bindings | closing smoke evidence and fixture | n/a | closed |

## Current ownership

| Symbol | Responsibility |
|---|---|
| `CameraRigRecipe` | reusable presentation intent |
| `CameraRigComposer` | designer-facing virtual rig authoring and Apply/Rebuild |
| `CinemachineRigMaterializer` | editor-only technical materialization; output only when explicitly requested |
| `CameraOutputSessionBinding` | scene-authored explicit physical output and scoped session |
| `CameraOutputContext` | one-output admission, arbitration, snapshot and restoration |
| `CameraOutputSession` | transactional context/application coordination |
| `CameraOutputRigApplicator` | applies/clears the selected virtual rig |
| Route/Activity/Local Player bindings | explicit lifecycle/eligibility publication and release |

## Evidence limits

- C9C–C9G fixtures contain expected PASS strings but no separate archived run
  transcript in QAFramework.
- C9I explicitly records its eight-case closure.
- C9L passed its ten-case Player arbitration smoke. The historical C9O probe
  contamination was removed from the C9L scene; rerun only to confirm absence
  of unrelated C9O/C9M logs, not to re-prove the functional PASS.
- C9O is closed from the supplied teardown smoke evidence, corroborated by the
  committed route, probe and post-C9N synchronization corrections; no raw
  console capture is versioned.
- FIRSTGAME C9M is scene configuration only: its validation report template has
  blank result fields.

## Next action

C9M FIRSTGAME manual validation and evidence recording is the next cut. Do not
add new Camera runtime behavior before that consumer gap is measured.
