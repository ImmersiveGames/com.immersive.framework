# Camera Delivery Reconciliation

Status: **canonical closed evidence matrix**  
Last audited: **2026-07-12**  
Decision: `ADR-PROD-0006-camera-requests-output-contexts.md`

This matrix reconciles current package source, QAFramework and FIRSTGAME. A
fixture, installer or serialized scene is not a Play Mode PASS by itself.

| ID | Objective / key source | Package | QA | FIRSTGAME | Status |
|---|---|---|---|---|---|
| C9A | ADR-PROD-0006 | accepted | n/a | n/a | closed |
| C9B | remove old authority | removed | legacy smoke removed | old paths removed | closed |
| C9C | typed request/output contracts | implemented | fixture | n/a | closed |
| C9D | `CameraOutputContext` | implemented | fixture | n/a | closed |
| C9E | `CameraOutputRigApplicator` | implemented | fixture | n/a | closed |
| C9F | `CameraOutputSession` | implemented | fixture | n/a | closed |
| C9G | typed Route/Activity publishers | implemented | fixture | n/a | closed |
| C9H | trial lifecycle adapters | removed | smoke removed | n/a | superseded |
| C9I | canonical Route/Activity lifecycle | implemented | eight cases closed | n/a | closed |
| C9K | Local Player request source | implemented | consumed by C9L | real consumer | closed |
| C9L | Player arbitration/restoration | existing runtime | PASS, ten cases | n/a | closed |
| C9M | FIRSTGAME consumer wiring and proof | local package dependency | n/a | runtime and visual PASS | closed |
| C9N | virtual rig / physical output separation | implemented | synchronized | consumed | closed |
| C9O | Activity teardown before Route unload | existing runtime | closing evidence | n/a | closed |
| C9Q | Follow framing authoring/materialization | implemented | PASS, four cases | distinct framing visually accepted | closed |
| C9R | persistent Session output and explicit override authority | implemented | PASS, eleven cases | transition + manual override/release PASS | closed |

`C9J` has no canonical artifact. No package commit identified as `C9P` defines
another canonical cut.

## Current ownership

| Symbol | Responsibility |
|---|---|
| `CameraRigRecipe` | reusable presentation intent, including Follow Offset |
| `CameraRigComposer` | designer-facing virtual-rig authoring and Apply/Rebuild |
| `CinemachineRigMaterializer` | editor-only technical rig materialization |
| `CameraOutputSessionBinding` | persistent physical output and scoped session |
| `CameraOutputContext` | request admission, arbitration, snapshot and restoration |
| `CameraOutputSession` | transactional context/application coordination |
| `CameraOutputRigApplicator` | presents or clears the selected virtual rig |
| `PlayerGameplayAdmissionRuntimeContext` | canonical Local Player request publisher, one request per admitted Slot/output |
| `LocalPlayerCameraRequestBinding` | authoring/evidence by default; Scene Auto-Publisher only through explicit opt-in when admission publication is absent |
| `ActivityCameraOverrideBinding` | explicit Activity override availability/request/release |
| `RouteCameraOverrideBinding` | explicit Route override availability/request/release |
| `SessionCameraOverrideBinding` | transition-scoped Session override |
| `CameraOutputSessionInjectionRuntime` | injects the persistent output into loaded consumers |

## Frozen current policy

```text
output location:
  UIGlobal

precedence:
  Player 50 < Activity 100 < Route 200 < Session 300

lifecycle:
  Player Gameplay Admission publishes exactly one LocalPlayer request per Slot/output
  Scene Auto-Publisher is opt-in and must not coexist with admission publication for the same Player
  Activity entry makes override available
  Route entry makes override available
  explicit RequestOverride publishes
  explicit ReleaseOverride restores
  Session is requested only around transition coverage
```

## Evidence summary

### QA

```text
C9Q Follow Pipeline:
  PASS
  camera-materialized
  follow-pipeline-materialized
  target-assigned
  idempotent

C9R Camera Override Authority:
  PASS
  11 cases
  explicit request/release
  precedence/restoration
  duplicate-operation idempotence
  lifecycle cleanup
```

### FIRSTGAME

```text
installer:
  status='Succeeded'
  sessionAssigned='True'
  sessionRebuilt='True'

runtime:
  Session owner='Game Application'
  Session status='Available'
  Session transition request precedence='300'
  Session transition release succeeded

manual chain:
  Activity 100 wins and releases to Player 50
  Route 200 wins and releases to Player 50
  Session 300 wins and releases to Player 50

visual:
  accepted
```

## Evidence limits

- The supported baseline is one explicit output.
- Multi-output/split-screen authoring is not proven.
- The raw user console transcript is not stored in the package repository; this
  reconciliation records the accepted result.
- Visual acceptance confirms the current FIRSTGAME slice, not every future
  camera Recipe or Cinemachine mode.

## Closure decision

Camera C9 is closed. No new camera runtime behavior is selected.

The next active product block is G1, scoped as a consumer Route-loop proof rather
than a framework-owned gameplay loop.
