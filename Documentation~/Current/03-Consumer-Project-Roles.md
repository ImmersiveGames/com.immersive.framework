# 03 - Consumer Project Roles

Status: **canonical separation rule**.

This document freezes the role split between the framework package, the QA synthetic project and FIRSTGAME.

## Rule

```text
QA proves the framework works technically.
FIRSTGAME proves the framework is usable to start a game.
The package contains contracts, runtime, editor tooling and official documentation.
```

## Framework package

Owner:

```text
Packages/com.immersive.framework
```

Responsibilities:

- runtime contracts and runtime implementation;
- Unity authoring and editor tooling owned by the framework;
- validators and diagnostics;
- official docs under `Documentation~/`;
- ADRs, current roadmap, current usage map and history.

The package must not contain consumer project assets, scene examples, QA-only project objects or FIRSTGAME-specific gameplay.

## QA Project

Owner:

```text
C:\Projetos\My project
```

Canonical asset root:

```text
Assets/ImmersiveFrameworkQA/
```

Responsibilities:

- synthetic smokes;
- artificial scenarios;
- negative cases;
- technical probes;
- QA scenes, QA routes, QA activities and QA buttons;
- regression validation surfaces.

The QA project may reference framework concepts to validate behavior. It must not become a game example and must not contain final FIRSTGAME assets.

## FIRSTGAME

Owner:

```text
C:\Projetos\planet-devourer
```

Canonical asset root:

```text
Assets/_Project/
```

Responsibilities:

- a minimal real game flow;
- menu, gameplay and resident UI scenes;
- player and runtime object examples that behave like game code;
- real reset, restart, pause and transition usage;
- simple prefabs/materials/scripts needed by the playable flow.

FIRSTGAME must not contain QA synthetic smoke runners, QA-only objects or historical framework documentation.

## Documentation policy

Canonical framework documentation belongs only in:

```text
Packages/com.immersive.framework/Documentation~/
```

Consumer READMEs may explain local project purpose, manual flows and what should not enter the project. They must link back to package docs instead of duplicating canonical framework history.

## Framework Settings Location Policy

Runtime loads framework settings through Unity Resources:

```text
Resources.Load("ImmersiveFrameworkSettings")
```

The settings asset must be named:

```text
ImmersiveFrameworkSettings.asset
```

and must live directly inside one consumer-owned `Resources` folder. The package does not require this folder to be under `Assets/_Project`.

Consumer projects own their local settings location:

- FIRSTGAME may keep game-owned settings under `Assets/_Project/.../Resources/`.
- The Framework QA Project should keep QA operational settings under `Assets/ImmersiveFrameworkQA/.../Resources/`.
- Other consumers may choose their own asset root, provided the asset remains directly inside a `Resources` folder and keeps the required file name.

Editor tooling must discover an existing valid settings asset before creating a default. It must not recreate `Assets/_Project` when a valid settings asset already exists elsewhere, and it must not silently continue when multiple valid `ImmersiveFrameworkSettings.asset` files are present.

## Serialization policy

Unity serialized assets must not be renamed or moved automatically unless the move preserves `.meta` files and references are verified.

When a scene or prefab references a MonoBehaviour script by GUID or `m_EditorClassIdentifier`, class/file renames are deferred to a Unity Editor migration.
