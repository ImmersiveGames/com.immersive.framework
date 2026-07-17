# 05 — Execution Status

Status: Canonical operational source
Last updated: 2026-07-17

## Read-only source baseline

| Repository | Commit | Meaning |
|---|---|---|
| `ImmersiveGames/com.immersive.framework` | `385c957a8fefb53f0daf395c662ffa7d5fedc996` | source baseline used by P3M0–P3M2 artifacts |
| `rinnocenti/QAFramework` | `993f8e698edb8c826054c9f8faa8bd344fbc8013` | QA source baseline used by P3M0–P3M2 artifacts |

Package version at that baseline:

```text
1.0.0-preview.15
```

P3M3 is prepared against the locally applied and Unity-validated P3M2 state.

## Scene Local Player sequence

| Cut | Source status | Validation status |
|---|---|---|
| P3M0 source baseline freeze | Complete | source identities recorded |
| P3M1 architecture and docs | Complete | product and runtime boundaries reconciled |
| P3M2 consumer decoupling | Complete | C9M 6/6, C9R 11/11, canonical P3 31/31 and P3B 5/5 supplied |
| P3M3 destructive removal | Source prepared | Unity import, compile and focused regression pending |
| P3M4 Scene Local Player Admission promotion | Blocked | requires P3M3 PASS |
| P3M5 QA proof | Ordered | requires package implementation |
| P3M6 FIRSTGAME proof | Blocked | requires P3M5 PASS |
| P3M7 sample/docs | Blocked | requires P3M6 product proof |

## P3M3 source evidence

```text
alternative Composer and Recipe are listed for deletion
alternative Inspector and Apply/Rebuild are listed for deletion
P3B smoke is listed for deletion
CameraRigComposer contains no compatibility member
camera eligibility validates resolved targets only
C9R installer removes Missing Scripts without naming deleted types
canonical Player provisioning and participation surfaces remain untouched
```

## Required Unity gate

1. Close Unity.
2. Copy the complete Framework files over the package root.
3. Copy the complete QA files over the QA project root.
4. Delete every path in the two removal lists.
5. Open QAFramework and wait for import and compilation.
6. Run C9R Camera Override Authority Setup once to repair old scene serialization.
7. Confirm the P3B menu no longer exists.
8. Run `Immersive Framework QA/Camera/C9M Run Follow Pipeline Smoke`.
9. Run C9R Camera Override Authority from QA Hub.
10. Run `Immersive Framework/QA/Player/P3 Run Canonical Pre-FIRSTGAME Smoke`.
11. Confirm no Missing Script remains.

Expected focused results:

```text
[QA][C9M Follow Pipeline] PASS. status='Passed' cases='6'
[QA][C9R Camera Override Authority] PASS. cases='11'
[P3_CANONICAL_PREFIRSTGAME_SMOKE] status='Passed' phases='2' cases='31'
```

P3B is intentionally absent after this cut and must not be recreated as a compatibility smoke.

## Failure policy

Fix defects inside P3M3. Do not restore the deleted component, Recipe, Inspector, Apply/Rebuild utility, P3B menu, null bridge, alias or fallback discovery.
