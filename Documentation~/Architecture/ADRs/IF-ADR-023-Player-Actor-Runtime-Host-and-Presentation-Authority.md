# IF-ADR-023 — Player Actor Runtime Host and Presentation Authority

Status: **Accepted — Scene-Provided authored-composition implementation complete**
Accepted: **2026-08-28**  
Last updated: **2026-08-31**  
Related decisions: IF-ADR-003, IF-ADR-007, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021  
Historical technical certification: [2026-08-29](../Reconciliation/IF-ADR-023-PLAYER-ACTOR-RUNTIME-TECHNICAL-CERTIFICATION-2026-08-29.md)
Occurrence-identity boundary: [IF-ADR-023A](../Reconciliation/IF-ADR-023A-PLAYER-ACTOR-OCCURRENCE-IDENTITY-BOUNDARY-2026-08-31.md)

## Context

`LocalPlayerHostAuthoring` is the technical Local Player Host. `ActorProfile`
selects the Player presentation. These responsibilities remain separate.

The previous Scene-Provided implementation accumulated Editor-derived references
and made Apply / Rebuild an artificial runtime prerequisite. That state duplicated
the authored physical composition and incorrectly made an Editor operation appear
to be the authority for a Scene-Provided Player.

## Decision

### Composition baseline

Both origins use the same technical composition:

```text
LocalPlayerHostAuthoring
├── PlayerInput
└── ActorMount
    └── PlayerActorRuntimeHost
        ├── PlayerActorDeclaration
        └── PresentationMount
            └── ActorProfile.PresentationPrefab
```

`LocalPlayerHostAuthoring.PlayerActorRuntimeHostPrefab` remains the canonical
Host reference. `ActorProfile.PresentationPrefab` remains the canonical
Presentation reference. `PlayerSlotProfile`, `ActorProfile`, `LocalPlayerHost`,
admission timing and the physically authored hierarchy remain authoring authority.

`PlayerActorRuntimeHost` is the generic runtime occurrence container. The exact
root Transform of the selected Presentation is the concrete spatial authority for
that Actor embodiment. The Runtime Host does not require or own a
`CharacterController`, `Rigidbody`, locomotion body, or transform synchronization
with the Presentation.

Root GameObject names match their prefab filenames without `.prefab`.

### Scene-Provided target contract

Scene-Provided means an already physically authored consumer composition:

```text
consumer-authored physical composition
→ Framework validates authoring
→ Framework deterministically resolves the exact authored composition
→ Framework adopts it at runtime
```

The exact runtime Host and Presentation are resolved structurally:

```text
LocalPlayerHost
→ ActorMount
→ exact PlayerActorRuntimeHost
→ PresentationMount
→ exact Presentation
```

No name, tag, global search or implicit convention is an allowed fallback.

Scene-Provided does not configure, materialize or persist derived composition at
runtime. Apply / Rebuild is not a required step, a composition authority, a runtime
precondition, or a way to make Scene-Provided valid.

### Manager-Provisioned target contract

Manager-Provisioned receives provisioning intent and owns runtime
materialization:

```text
Framework receives provisioning intent
→ Framework instantiates Local Player Host
→ Framework materializes PlayerActorRuntimeHost
→ Framework materializes selected Presentation
→ Framework prepares the runtime occurrence
```

The flows share prefab/profile sources but are not equivalent materialization
strategies: Scene-Provided adopts physical composition; Manager-Provisioned creates
physical runtime composition.

### Validation and adoption boundaries

Editor/authoring validation owns prefab provenance and verifies the correct Host and
Actor Mount, exactly one Runtime Host, compatible Host prefab, canonical
`PlayerActorDeclaration`, exact Presentation Mount, compatible presentation prefab,
and absence of ambiguous composition.

Runtime validation/adoption verifies the current Host and Slot, selected
`ActorProfile`, exact structural mounts, runtime occurrence identity, preparation,
adoption and runtime content. Runtime must not depend on a serialized
`PrefabUtility` certificate merely because Editor validation can establish
provenance.

### Player Actor occurrence identity

For a generic reusable Player Actor prefab:

```text
PlayerActorDeclaration.ActorId = EMPTY
```

The physical preparation/adoption transaction assigns the runtime occurrence
identity. The authored declaration must not receive a persistent identity to
satisfy validation or Scene-Provided authoring. `ActorProfileId`, `PlayerSlotId`
and runtime `ActorId` remain separate identities.

### Create Local Player

`Create Local Player` remains development tooling that can create the initial
technical structure: `PlayerInput`, `LocalPlayerHostAuthoring`,
`UnityPlayerInputGateAdapter`, `ActorMount` and
`SceneProvidedLocalPlayerAuthoring`. It is not runtime authority, does not replace
explicit authoring intent, is not Apply / Rebuild, and need not materialize the
final Actor or Presentation.

## Rejected scope

- serialized derived Runtime Host or Presentation references as composition authority;
- duplicate `ActorProfile` or `PresentationPrefab` evidence;
- stamps that only prove Apply / Rebuild ran;
- a persistent generic `PlayerActorDeclaration.ActorId`;
- runtime fallback by name, tag, scene scan or inferred hierarchy;
- a new resolver API, resolver name, field-removal order, serialized-data migration,
  Inspector design, test design, or Presentation gameplay composition in this cut.

## Consequences

- Scene-Provided has one source of truth: physical authoring plus canonical
  prefab/profile intent.
- Manager-Provisioned remains the only origin that materializes its Player Actor and
  Presentation at runtime.
- Editor owns provenance validation; runtime remains strict about current structure,
  selection, identity and adoption.
- Scene-Provided has no derived evidence type, serialized provenance cache or
  Player Apply / Rebuild dependency.

## Current implementation coverage

The implementation resolves authored Scene-Provided composition transiently before
admission and adoption, and uses prepared physical evidence after commit. Derived
references/evidence, runtime validation of that evidence, Player Apply / Rebuild
and `ScenePlayerActorPresentationEvidence` are removed. Historical 2026-08-29
certification remains evidence only for the boundary it executed.

## Pending decisions

- Concrete Camera/Input/Locomotion composition inside a Presentation remains
  consumer-owned.
