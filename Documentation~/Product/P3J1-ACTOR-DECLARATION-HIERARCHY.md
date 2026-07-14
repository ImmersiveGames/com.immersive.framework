# P3J.1 — Actor Declaration Hierarchy

Status: implementation cut; Unity compilation and QA pending.  
Type: technical contract correction and product Inspector alignment.

## Objective

Unify generic and Player Actor declarations under one canonical Actor identity authority before Local Player Host composition and contextual Logical Actor materialization are introduced.

## Final hierarchy

```text
ActorDeclaration : MonoBehaviour, IActor
└── PlayerActorDeclaration : ActorDeclaration
```

`ActorDeclaration` is intentionally no longer sealed. `PlayerActorDeclaration` remains sealed.

## Identity authority

The base declaration owns exactly one set of serialized identity fields:

```text
ActorId
Display Name
Reason
```

It also owns the generic `ActorDescriptor` creation path. The generic descriptor uses virtual effective classification, so a `PlayerActorDeclaration` viewed through `ActorDeclaration` still produces:

```text
ActorKind.Player
ActorRole.Protagonist
```

`PlayerActorDeclaration` adds only Player-specific evidence and its specialized `PlayerActorDescriptor`.

## Compatibility boundary

P3J.1 deliberately preserves:

```text
RequireComponent(PlayerInput)
PlayerActorDeclaration.PlayerInput
PlayerActorDeclaration.HasPlayerInputEvidence
PlayerActorDeclaration.TryCreateDescriptor(PlayerActorDescriptor)
```

This keeps P3G and P3H fixtures compiling. P3J.2 will introduce `LocalPlayerHostAuthoring` and migrate PlayerInput authority away from the contextual Logical Actor Host.

## Inspector

A shared custom Inspector:

- exposes inherited identity once;
- allows generic Actor classification editing;
- shows Player classification read-only;
- preserves PlayerInput evidence while marking its pending P3J.2 migration;
- provides Advanced / Debug evidence without introducing runtime behavior.

## Files

Changed:

```text
Runtime/Actors/ActorDeclaration.cs
Runtime/Actors/PlayerActorDeclaration.cs
```

Created:

```text
Editor/PlayerParticipation/ActorDeclarationEditor.cs
Documentation~/Product/P3J1-ACTOR-DECLARATION-HIERARCHY.md
```

## Out of scope

```text
LocalPlayerHostAuthoring
ActorMount
removal of same-object PlayerInput requirement
LocalPlayerJoinResult migration
Logical Actor materialization
RuntimeContent handles
prepare / release / replace
Presentation
occupancy
camera
FIRSTGAME
```

## QA

Run outside Play Mode:

```text
Immersive Framework/QA/Player/P3J.1 Run Actor Declaration Hierarchy Smoke
```

Expected:

```text
[P3J1_ACTOR_DECLARATION_HIERARCHY_SMOKE] status='Passed' cases='14'
```

## Suggested commit

```text
P3J.1 — unify Player Actor declaration under ActorDeclaration
```
