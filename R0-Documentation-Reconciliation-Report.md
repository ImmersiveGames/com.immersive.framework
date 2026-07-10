# R0 Documentation Reconciliation Report

## Objective

Reconcile canonical package documentation with the implemented PlayerComposer, CameraComposer and explicit Route/Activity Cinemachine output model; remove superseded Current guidance and select one next lane.

## Repositories audited

- `com.immersive.framework`: code, Editor tooling, package assets and Documentation.
- `QAFramework`: current CameraComposer regression/single-player smokes and camera fixtures.
- `planet-devourer` / FIRSTGAME: gameplay scene and PlayerComposer/CameraComposer consumer proof.

QAFramework and FIRSTGAME were read-only.

## State confirmed from code

1. `PlayerRecipe` exists and stores reusable authoring intent.
2. `PlayerComposer` exists as a designer-first surface.
3. PlayerComposer exposes Validate and Apply/Rebuild Editor flow. FIRSTGAME carries applied/idempotent scene evidence, but a dedicated current PlayerComposer Apply/Rebuild smoke was not found in QAFramework.
4. `CameraRecipe` exists.
5. `CameraComposer` exists.
6. CameraComposer resolves an explicit PlayerComposer or explicit transforms.
7. Apply/Rebuild materializes/reuses Unity Camera, Cinemachine Camera and Brain.
8. Camera QA and FIRSTGAME CameraComposer proof code require a second Apply/Rebuild to create zero objects; FIRSTGAME PlayerComposer scene evidence reports an already-valid second materialization state.
9. No functional `FrameworkCameraDirector` exists in the package.
10. No functional `Camera.main` fallback exists.
11. Camera runtime authority does not resolve by object name.
12. Route/Activity bindings consume explicit `FrameworkCinemachineCameraOutputSource`.
13. Bindings apply output on enter.
14. Exit only logs deferred cleanup; automatic release/restoration is absent.
15. FIRSTGAME gameplay scene contains both PlayerComposer and CameraComposer with explicit linkage.
16. FIRSTGAME gameplay does not contain Route/Activity camera bindings.
17. PlayerView remains passive evidence and is not CameraComposer authority.
18. PlayerControl has contracts/adapters but no complete product runtime for binding/control/movement.

QA retains old serialized camera references in legacy scenes and a legacy helper under `Assets/_Project`; these are not current package runtime or current CameraComposer QA evidence.

The audited QA repository does not contain the dedicated PlayerComposer smoke planned under `Assets/ImmersiveFrameworkQA/PlayerComposer/`. R0 therefore does not claim dedicated PlayerComposer QA PASS.

## Documentation inventory classification

| Group | Classification | Decision |
|---|---|---|
| `Current/00-04` | Current canonical | Reconciled. |
| `Current/05-09` | Superseded but worth archiving / duplicate | Consolidated into History 070 and removed from Current. |
| `Current/10` | Mixed current and superseded | Reduced to current decisions; historical body consolidated into History 070. |
| `Guides/Usage/index.html` | Current canonical, stable path | Updated in place; structure/CSS/navigation retained. |
| `Guides/Camera-Product-Usage.md` | Current canonical | Retained after code audit. |
| Three F50-F52 standalone guides | Duplicate / superseded | Consolidated and removed. |
| Architecture flow guides | Useful maintainer reference | Retained. |
| `Product/` manifests/specs/plans | Useful historical | Retained outside primary navigation. |
| `ADRs/` | Useful historical decisions | Retained; index compacted. |
| `Planning/` | Draft/user-owned | Retained; pre-existing work untouched. |
| `History/` | Useful historical | Updated with product transition and removal map. |
| IDE metadata under documentation | Obsolete but unrelated pre-existing state | Not changed by R0. |

## Files changed

- Package `README.md`.
- Canonical Current files `00`, `01`, `02`, `03`, `04`, `10`.
- `Documentation~/README.md`.
- HTML Usage Guide and its README.
- History index, phase history, guide history and removed-files history.
- ADR index.
- One historical Product audit link.

## Files created

- `Documentation~/History/070-Player-Binding-and-Composer-History.md`.
- `R0-Removed-Files.md`.
- This report.

## Files moved

F50-F53 Current/guide content was semantically moved and consolidated into History 070; no one-file-per-cut copies were retained.

## Files removed

See `R0-Removed-Files.md`.

## Obsolete guidance eliminated

- F49M as active lane.
- PlayerView Binding Adapter as mandatory next step.
- legacy director/anchor/rig-applier setup from the active HTML guide.
- Camera.main/name lookup/global manager authority.
- F53C1/F53C2/F53C3/F53D and CanonicalPlayerBindingAuthoring as current sequence.
- PlayerView as gameplay-camera authority.

## Current canonical product model

```text
PlayerRecipe -> PlayerComposer -> Validate -> Apply/Rebuild
PlayerComposer -> CameraComposer -> Validate -> Apply/Rebuild
```

Recipes are optional reusable intent. Composers are product authoring surfaces. Generated contracts/adapters are Advanced/Debug materialization.

## Active lane selected

`P2 — Player Control Product`, with P2A through P2G as the only active sequence.

## Remaining known gaps

- PlayerControl product runtime, input binding/activation and movement.
- Dedicated PlayerComposer Apply/Rebuild QA smoke.
- Player spawn/materialization.
- Camera output automatic release and restoration on lifecycle exit.
- Progression save runtime.
- Transition/loading hardening.

## Link validation

Validated locally by static link/anchor checks after edits. The stable HTML guide path was preserved. Unity compile/import/smoke was intentionally not run for this documentation-only cut.

## Acceptance result

Documentation-only structural/content acceptance is satisfied by static validation. Full requested state acceptance remains **partial** because dedicated PlayerComposer Apply/Rebuild QA coverage is absent from the audited QA repository. Runtime, code, asmdefs, scenes, prefabs and Unity assets were not changed. Unity operational PASS is not claimed or required for doc-only edits.
