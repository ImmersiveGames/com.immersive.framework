# 050 - Consumer Project Separation Cleanup

Date: 2026-07-04

## Roots audited

```text
C:\Projetos\ImmersivePackages
C:\Projetos\My project
C:\Projetos\planet-devourer
```

## Summary

This cleanup freezes the role split:

```text
QA proves the framework works technically.
FIRSTGAME proves the framework is usable to start a game.
The package contains contracts, runtime, editor tooling and official documentation.
```

## Audit findings

| Root | Finding | Severity | Owner | Action |
|---|---|---:|---|---|
| `ImmersivePackages/com.immersive.framework` | `Documentation~/` already had `README.md`, `Current/`, `History/`, `Guides/`, `ADRs/` and `Planning/`. | Low | Framework package | Added explicit consumer role document and index links. |
| `My project/Assets/ImmersiveFrameworkQA` | QA root exists and contains QA scenes, routes, activities, scripts and README. | Low | QA Project | Updated README to make QA-only purpose canonical locally. |
| `My project/Assets/_Documentation` | 155 markdown docs plus metas exist outside the package, including architecture/history material. | High | Framework docs, not QA assets | Not moved automatically; archive/migration should be a dedicated docs import cut. |
| `My project/Assets/_Project` | Contains serialized framework app/routes/activities/settings under a generic consumer root. | Medium | QA Project, but serialized | Deferred; moving assets can break references without Unity Editor migration. |
| `planet-devourer/Assets/_Project` | FIRSTGAME assets are already concentrated under `_Project`. | Low | FIRSTGAME | Added README. |
| `planet-devourer/Assets/_Documentation` | Empty documentation folder metas exist. | Low | FIRSTGAME | Candidate removal via Unity Editor; no markdown docs were found. |
| `planet-devourer/Assets/_Project/Scripts/FirstGamePlayerResetProbe.cs` | Temporary `Probe` name remains in active FIRSTGAME script. It is referenced by `FG_Gameplay.unity` through script GUID and editor class identifier. | Medium | FIRSTGAME | Deferred manual rename/migration through Unity Editor. |

## Changes made

- Created `Documentation~/Current/03-Consumer-Project-Roles.md`.
- Updated `Documentation~/README.md`.
- Updated `Documentation~/Current/00-Current-State.md`.
- Updated `Documentation~/Current/01-Roadmap.md`.
- Updated `Documentation~/Current/02-Usage-Map.md`.
- Updated `Documentation~/Guides/README.md`.
- Updated `Documentation~/Planning/README.md`.
- Updated `Documentation~/History/000-INDEX.md`.
- Updated `My project/Assets/ImmersiveFrameworkQA/README.md`.
- Created `planet-devourer/Assets/_Project/README.md`.
- Created `planet-devourer/Assets/_Project/README.md.meta`.
- Created this report.

## Changes not made due to Unity serialization risk

### QA Project

Deferred:

- move `Assets/_Project/ScriptableObjects/ImmersiveFramework/*` into `Assets/ImmersiveFrameworkQA/`;
- move `Assets/_Project/Settings/ImmersiveFramework/*` into `Assets/ImmersiveFrameworkQA/`;
- remove generic `_Project` folders after confirming no active serialized references remain.

Reason: these are `.asset` files and settings with `.meta` references. Move them through Unity Editor or in a dedicated migration that verifies all scene/prefab/asset references.

### FIRSTGAME

Deferred:

- rename `FirstGamePlayerResetProbe` to a final gameplay-state name such as `FirstGamePlayerResettableState`;
- rename scene object `TestProb` in `FG_Gameplay.unity`;
- reorganize FIRSTGAME MonoBehaviour scripts into `Scripts/Runtime/Player`, `Scripts/Runtime/Reset` and `Scripts/Runtime/RuntimeObjects`.

Evidence:

```text
Assets/_Project/Scripts/FirstGamePlayerResetProbe.cs.meta
  guid: 7d31b2df5fc0f6645894005132ffaa27

Assets/_Project/Scenes/Gameplay/FG_Gameplay.unity
  m_Script: {fileID: 11500000, guid: 7d31b2df5fc0f6645894005132ffaa27, type: 3}
  m_EditorClassIdentifier: Assembly-CSharp::_Project.Scripts.FirstGamePlayerResetProbe
```

## Safe moves applied

No filesystem moves were applied in this pass.

Reason: the useful candidate moves involved Unity serialized assets, MonoBehaviour scripts or `.meta` references. The safe non-serialized work was documentation and README alignment.

## Candidate removals

- `planet-devourer/Assets/_Documentation/` contains only folder metas. Remove via Unity Editor if the project no longer needs those folders.
- `My project/Assets/_Documentation/` should be archived or migrated into package history by a dedicated docs migration, not deleted.

## Text validation performed

Commands were textual only. Unity import, compile, build, playmode and smoke were not run.

| Check | Result |
|---|---|
| Search `ImmersiveFrameworkQA` inside `planet-devourer/Assets` | No matches after README wording cleanup. |
| Search `FirstGame` or `planet` inside `My project/Assets/ImmersiveFrameworkQA` | No matches. |
| Search canonical-doc markers outside package docs | Matches remain in `My project/Assets/_Documentation` and setup scripts that still mention `Assets/_Documentation`; deferred as docs migration/setup cleanup. |
| Search `Probe/Test/Smoke` in FIRSTGAME | Real deferred hits: `FirstGamePlayerResetProbe.cs`, `FG_Gameplay.unity` object `TestProb`; false positives also appear in Unity rendering fields such as `LightProbe`. |

## Manual next steps

1. In Unity Editor, migrate QA serialized assets from `Assets/_Project` to `Assets/ImmersiveFrameworkQA` if they are truly QA-owned.
2. In Unity Editor, rename `FirstGamePlayerResetProbe` and its scene object to final FIRSTGAME language after confirming Missing Script does not occur.
3. If FIRSTGAME script folder reorganization is desired, move `.cs` files with `.meta` preserved and reimport in Unity before committing.
4. Delete empty FIRSTGAME `_Documentation` folders only after Unity confirms no generated setup workflow recreates them.
5. Run Unity import/compile/smoke manually; this pass intentionally did not run Unity.

## Final frozen rule

```text
Framework package: product contracts, runtime, editor tooling, validators, diagnostics and official docs.
QA Project: synthetic smokes, artificial scenarios, negative cases, probes and QA buttons.
FIRSTGAME: minimal real game usage proof with playable flow and game-owned assets.
```

Do not mix these roles in future cuts.
