# CAMERA-032-F — Legacy Removal Closure — 2026-09-23

Status: **IMPLEMENTED / STATIC REVIEW RECORDED / UNITY AGGREGATE CERTIFIED**

Authority: [IF-ADR-032](../ADRs/IF-ADR-032-Camera-Unified-Authority-Session-Outputs-Presentations-Subjects-and-Lifecycle.md)

This record closes the source cleanup and aggregate Unity recertification for CAMERA-032-F. The dated focused QA records for CAMERA-032-A, CAMERA-032-C, the two-Output Player lifecycle and Subject + Transaction remain supporting evidence; the aggregate result below is the terminal certification result.

## Current model

```text
Subject(s)
  -> CameraPresentationRuntime
  -> CameraRigComposer
  -> CameraRequest
  -> explicit CameraOutputSession
  -> Unity Camera + CinemachineBrain
```

Session declares explicit Outputs. Route and Activity declare Presentations and do not create Outputs. Player Slot -> Output and Player Slot -> Presentation are explicit Session bindings. `PlayerInputManager` owns `Camera.rect`. Persistent Content is not gameplay Camera topology authority.

## Classification

| Item | Class | Disposition |
|---|---|---|
| `CameraSharedComposition` MonoBehaviour | dead | removed earlier; remaining tests and QA menus migrated or deleted |
| `SessionCameraOverride` | dead | type already absent; QA installer and identity regression removed |
| `RouteCameraOverride` / `ActivityCameraOverride` / `ScopedCameraOverride` | dead | types already absent; QA override installer removed |
| `PlayerCameraOutputPolicyAuthoring` | dead | type already absent; 028-D QA removed |
| `PlayerCameraCompositionPolicyAuthoring` | dead | type already absent |
| Persistent-root Camera Output / Subject discovery | dead | `CameraOutputInjectionRuntime` and `CameraSubjectAvailabilityInjectionRuntime` removed; no remaining consumer |
| `CameraRequestOwnerKind.Composition` | dead | already absent; aggregate rejects it if it returns |
| `CameraRequestLifetimeKind.Composition` | dead | already absent; aggregate rejects it if it returns |
| `CameraTargetSourceDescriptor` | dead | removed; `CameraRequest` has no target source |
| `CameraSharedCompositionSubjectPolicyKind` | current | retained; serialized Presentation subject policy |
| `CameraSharedCompositionSnapshot` | current | retained; Presentation reconcile evidence |
| `CameraSharedCompositionReconcileStatus` | current | retained; Presentation reconcile result |
| `CameraPresentationDefinition` | current | reusable intent only |
| `CameraPresentationRuntime` | current | occurrence authority; not a MonoBehaviour or ScriptableObject |
| `CameraOutputSession` / `CameraOutputContext` | current | winner and physical projection authority |
| `CameraRigComposer` | current | Rig materialization authority |

The retained `CameraSharedComposition*` policy, snapshot and status names are the live Presentation contract. Renaming them would churn a serialized field and the certified reconcile vocabulary without changing authority. They are not a scene composition adapter.

## Aggregate QA

`Immersive Framework > QA > Camera > Run Full Camera QA` now runs:

```text
structural Presentation contract
Subject + Transaction
two-Output Player lifecycle
CAMERA-032-C Game Flow
canonical baseline restore
```

It does not certify the removed override, shared-composition, 028-C or 028-D rails.

## Static checks recorded with this cleanup

- Camera runtime source has no `Camera.main`, `GameObject.Find` or `FindObjectOfType` authority.
- `CameraPresentationDefinition` serialized fields are stable id, description, Output definition, Rig prefab, transition mode, subject policy and request precedence.
- The package Persistent Content template contains EventSystem input, not gameplay Camera topology.
- planet-devourer scenes, prefabs and scripts do not reference the removed Camera product types.

## Aggregate certification result

Unity aggregate certification was executed on 2026-09-23 through:

`Immersive Framework > QA > Camera > Run Full Camera QA`

Terminal evidence:

~~~text
[QA_CAMERA_FULL_032] status='Passed'
verdict='CAMERA_032_FULL_CERTIFIED'
camera032A='structural PASS'
playerOutput='PASS'
gameFlow='16/16 PASS'
subjectTransaction='17/17 PASS'
legacyProductTypes='0'
presentationIntent='PASS'
canonicalRestore='PASS'
~~~

The canonical baseline was verified and restored by the aggregate. Earlier interrupted or failing runs from the same investigation are retained only as historical diagnostics and do not supersede this terminal PASS.

With this result, CAMERA-032-F aggregate recertification is complete and IF-ADR-032 has no remaining Camera certification item marked pending.
