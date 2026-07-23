# PROD-ASSET-1C — Actor Selection Policy Simplification

Status: implementation complete; Unity validation pending  
Date: 2026-07-22  
Type: UX/product, runtime contract migration, QA and documentation

## Objective

Remove the non-reusable `PlayerActorSelectionPolicyProfile` and make the Actor duplicate-selection rule a direct `GameApplicationAsset` decision.

## Scope

```text
GameApplication authoring
Player participation runtime composition
selection snapshots and operation results
Actor selection validators and templates
QA authoring/runtime fixtures
canonical Actor selection documentation
```

## Out of scope

```text
ActorProfile and PlayerSlotProfile identity
Activity participation Projection and Requirements Profiles
join authority or PlayerInputManager behavior
logical Actor or presentation materialization
FIRSTGAME assets, which will be recreated separately
```

## Product surface

```text
Game Application
  Local Player Participation
    Actor Duplicate Selection
      Allow Duplicates
      Unique Across Joined Slots
    Local Player Slots
```

New `GameApplicationAsset` instances explicitly default to `AllowDuplicates`.
`Unspecified` and undefined serialized values remain invalid and diagnostic; runtime never repairs them silently.

## Runtime contract

```text
GameApplicationAsset.PlayerActorSelectionDuplicatePolicy
  -> PlayerParticipationRuntimeHostModule
  -> PlayerParticipationRuntimeContext
  -> PlayerParticipationSnapshot.ActorSelectionDuplicatePolicy
  -> PlayerActorSelectionResult.DuplicatePolicy
```

The Session receives a copied enum value. Mutable selected-Actor state remains in the scoped runtime context.

## Removed surface

```text
PlayerActorSelectionPolicyProfile type and Create menu
Actor Selection Policy template assets
Profile reference in GameApplicationAsset
Profile reference in runtime snapshots and operation results
Profile-specific Inspector and validation
```

## Expected use

1. Create or select a Game Application.
2. Choose the Actor duplicate-selection rule directly under Local Player Participation.
3. Configure ordered Player Slot Profiles.
4. Runtime composition copies the selected enum into the Session context.
5. Advanced diagnostics and operation results report the effective enum value.

## Acceptance

Technical:

```text
framework and QA compile
no PlayerActorSelectionPolicyProfile reference remains in code or assets
Unspecified policy is rejected explicitly
UniqueAcrossJoinedSlots still rejects duplicate ActorProfileId selection atomically
snapshots/results expose the effective enum
no fallback, singleton or runtime asset mutation is introduced
```

Product:

```text
designer makes the decision in one GameApplication Inspector
no separate policy asset or template navigation is required
new GameApplication assets have an explicit default
validation remains visible for invalid serialized state
```

## QA

Run the existing Player participation authoring, Actor selection runtime and runtime-host Actor selection regressions. The P3H4 fixture now writes `UniqueAcrossJoinedSlots` directly to the canonical QA Game Application.

## Gain

Architecture:

```text
one authoring authority
runtime transports a value rather than an unrelated Unity asset identity
immutable Profile semantics remain reserved for reusable definitions
```

Usability:

```text
one less asset type
one less Create menu and template set
no cross-asset navigation for a single enum
```

## Suggested commit

```text
refactor(player): move actor selection policy into game application
```
