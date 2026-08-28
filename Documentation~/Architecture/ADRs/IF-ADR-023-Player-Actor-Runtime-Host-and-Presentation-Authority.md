# IF-ADR-023 — Player Actor Runtime Host and Presentation Authority

Status: **Accepted / Implementation Pending**  
Last updated: **2026-08-28**  
Type: architecture / Player Actor composition / runtime host / presentation authority  
Related decisions: IF-ADR-003, IF-ADR-007, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021

## Context

The current Player implementation correctly separates the stable Local Player Host from Actor selection:

```text
LocalPlayerHostAuthoring
├── PlayerInput
└── ActorMount
```

Scene-Provided supplies/adopts that technical Host from a consumer scene. Manager-Provisioned creates the same technical Host through the existing provisioning authority. After successful admission both modes converge on the same Session-owned Player occurrence.

The current Actor composition, however, gives `ActorProfile` a broader responsibility:

```text
ActorProfile
└── LogicalActorHostPrefab
```

That prefab is materialized or adopted under `ActorMount` and currently carries a mixture of concerns:

```text
framework Actor declaration/runtime evidence
gameplay-owned composition
Actor-specific presentation
```

The concrete FIRSTGAME Character Selection composition demonstrates the problem. Farmer and Cow use the same underlying logical/gameplay composition and differ primarily by the visual content placed under the visual mount. The selected `ActorProfile` therefore does not need to select a different framework/runtime Host merely to select a different character presentation.

The architecture audit also found no evidence that `PlayerSlotProfile` should become the owner of an Actor or Player runtime prefab. Slot identity/configuration and physical/runtime composition remain separate concerns.

## Decision

### 1. Preserve the Local Player Host boundary

`LocalPlayerHostAuthoring` remains the stable technical Host for one local Player.

It continues to own the technical Player boundary, including:

```text
PlayerInput
ActorMount
Slot admission evidence
```

It is not an Actor, does not select an `ActorProfile`, and does not execute gameplay.

Scene-Provided and Manager-Provisioned continue to differ only in how this technical Host is acquired before successful admission.

### 2. PlayerSlotProfile does not provide Actor runtime infrastructure

`PlayerSlotProfile` remains Slot configuration/identity authority.

It may continue to reference the configured `DefaultActorProfile` as Actor-selection intent, but it does not become the source of:

```text
Local Player Host prefab
Actor Runtime Host prefab
presentation prefab
```

Moving the current `LogicalActorHostPrefab` from `ActorProfile` to `PlayerSlotProfile` is rejected.

### 3. Introduce an Actor-independent Player Actor Runtime Host

The physical/runtime Actor composition is split into an Actor-independent runtime shell and Actor-specific presentation.

Target model:

```text
LocalPlayerHostAuthoring
├── PlayerInput
└── ActorMount
    └── PlayerActorRuntimeHost
        ├── framework-required Actor runtime infrastructure
        └── PresentationMount
            └── ActorProfile.PresentationPrefab
```

The exact public type/serialized field names may be finalized by the implementation cut, but the ownership boundary in this ADR is normative:

```text
Local Player Host composition
  -> supplies the reusable Actor Runtime Host shape

ActorProfile
  -> supplies Actor-specific Presentation
```

There is one reusable runtime Host shape per authored Player Host composition, not one framework/runtime Host per selectable Actor merely because presentation differs.

### 4. Actor Runtime Host is materialized only after Actor selection

This decision preserves the accepted `LeaveUnresolved` behavior.

Canonical flow:

```text
Join
→ Slot Joined
→ Actor unresolved
→ WaitingForActorSelection
→ Select ActorProfile
→ materialize/adopt Player Actor Runtime Host
→ materialize/adopt selected Presentation
→ Actor preparation satisfied
→ GameplayReady when the remaining Activity requirements are satisfied
```

For Manager-Provisioned composition, `ActorMount` therefore remains free of a prepared Actor runtime before selection.

This ADR does not move Actor runtime creation to Player Join and does not require a permanently pre-authored Actor shell inside every Local Player Host.

### 5. ActorProfile becomes presentation authority, not runtime-host authority

The intended minimum `ActorProfile` responsibility is:

```text
ActorProfile
├── ActorProfileId
├── DisplayName
├── Description
├── Icon
├── ActorKind
├── ActorRole
└── PresentationPrefab
```

`LogicalActorHostPrefab` is removed from the target model.

`PresentationPrefab` is intentionally broader than `ModelPrefab`. It may represent a complete presentation composition such as:

```text
visual root / model
Animator
presentation-specific VFX anchors
presentation-specific audio emitters
presentation-owned behaviours
```

It must not become authority for:

```text
Session state
Slot state
Player lifetime
Player admission
authoritative gameplay state
framework Player authority
```

### 6. Do not introduce ActorPresentationProfile yet

No intermediate `ActorPresentationProfile` asset is required by this cut.

The minimum accepted authoring surface is:

```text
ActorProfile.PresentationPrefab : GameObject
```

A future asset abstraction may be introduced only when concrete reusable presentation requirements justify it.

### 7. Skills, stats and character-sheet systems are outside this decision

This ADR does not add framework fields or dependencies for:

```text
Skills
Abilities
Stats
Character Sheet
Inventory
Progression
```

Those systems may later integrate with or extend Actor definition outside this Player runtime/presentation split. They are not used to justify a broader `ActorProfile` in the current cut.

### 8. Gameplay composition is distinct from Presentation

The current monolithic Logical Actor prefabs also contain sample/gameplay-owned components such as character controllers, locomotion and gameplay camera authoring.

Those components must not be reclassified as Presentation merely to complete the split.

Likewise, they must not automatically become mandatory framework infrastructure merely because they currently live beside `PlayerActorDeclaration`.

The implementation cut must classify the current prefab contents into:

```text
framework-required Actor runtime infrastructure
gameplay-owned composition
presentation-owned composition
```

Only the first category belongs intrinsically to the generic Player Actor Runtime Host.

The exact sample-owned gameplay composition mechanism may remain explicit authoring and does not require a new `GameplayProfile` in this ADR.

### 9. Scene-Provided must adopt runtime Host + Presentation, not monolithic Profile-owned Actor prefab

The current Scene-Provided evidence requires the authored Scene Actor to correspond to the exact `ActorProfile.LogicalActorHostPrefab`.

That evidence model is superseded by this decision and must be migrated with the implementation.

Target authority:

```text
Scene-Provided Local Player
├── exact Local Player Host
├── exact Player Actor Runtime Host / declaration evidence
└── selected ActorProfile
    └── matching Presentation under the Actor runtime presentation boundary
```

The consumer scene may author the candidate composition, but the `ActorProfile` no longer owns the entire runtime Actor hierarchy.

Scene-Provided remains deterministic, explicit and conflict-safe. Mismatched or ambiguous Actor/runtime/presentation evidence must reject rather than silently repair or replace consumer content.

### 10. Manager-Provisioned Host provisioning remains unchanged up to Actor materialization

Manager-Provisioned continues to use the existing technical Host provisioning path:

```text
LocalPlayerProvisioningAuthoring
→ PlayerInputManager
→ Local Player Host
→ Slot admission
```

The change begins only at Actor preparation/materialization:

```text
selected ActorProfile
→ reusable Player Actor Runtime Host
→ selected ActorProfile.PresentationPrefab
```

`ActorProfile` does not become Player Host provisioning authority.

### 11. Session physical lifetime remains unchanged

IF-ADR-019 remains authoritative for physical Player lifetime.

After successful admission/preparation, ordinary Activity transitions do not implicitly recreate the Session-owned physical Player occurrence.

This ADR changes composition ownership, not the Session-vs-Activity lifetime boundary.

### 12. Readiness semantics are preserved before any rename

The current `LogicalActorsPrepared` readiness condition remains unchanged during the first structural cut unless implementation proves that a semantic rename is required.

No isolated cosmetic rename is authorized by this ADR.

The accepted product guarantee remains that an Actor-dependent prepared representation exists and matches the current Actor selection/revision before the corresponding readiness condition is satisfied.

A later rename such as `ActorRuntimePrepared` requires an explicit follow-up decision or reconciliation after the new structure is implemented and observed.

## Ownership model

### Current

```text
Local Player Host
└── ActorMount
    └── ActorProfile.LogicalActorHostPrefab
        ├── framework Actor infrastructure
        ├── gameplay composition
        └── presentation
```

### Accepted target

```text
Local Player Host
└── ActorMount
    └── Player Actor Runtime Host
        ├── framework Actor infrastructure
        ├── explicit gameplay composition where authored
        └── PresentationMount
            └── ActorProfile.PresentationPrefab
```

The important invariant is not a specific hierarchy depth. It is the authority separation:

```text
Player Host composition owns reusable Actor runtime infrastructure.
ActorProfile owns Actor-specific presentation.
Gameplay remains explicitly gameplay-owned.
```

## Rejected alternatives

- `PlayerSlotProfile` as provider of the Actor Runtime Host prefab.
- `ActorProfile` continuing to provide the entire Logical Actor/runtime prefab.
- Treating Actor-specific visual differences as justification for different framework Actor Hosts.
- Pre-instantiating an Actor runtime shell on every Manager-Provisioned Local Player Host before Actor selection.
- Folding locomotion, `CharacterController`, gameplay input consumers or gameplay camera authoring into Presentation by default.
- Promoting sample-specific gameplay components to mandatory framework Actor infrastructure by default.
- Naming the new Actor-specific asset reference `ModelPrefab` when the supported composition is broader than a single model.
- Introducing `ActorPresentationProfile` without a concrete reuse/configuration requirement.
- Introducing Skills/Stats/Abilities dependencies as part of this restructuring.
- Silent compatibility fallback from `PresentationPrefab` to the legacy `LogicalActorHostPrefab`.
- Maintaining two competing prefab authorities during migration.

## Migration requirements

The implementation cut is breaking by design and must reconcile, at minimum:

```text
ActorProfile serialization/public surface
Player Actor materialization adapter
Player Actor preparation evidence
LocalPlayerHostAuthoring authoring surface
SceneLocalPlayerAdmissionAuthoring
Scene-Provided Editor materialization/validation/evidence
Manager-Provisioned Actor materialization
FIRSTGAME Player prefabs and ActorProfiles
QA fixtures/assertions that encode ActorProfile → LogicalActorHostPrefab
diagnostics/documentation
```

No silent legacy compatibility layer is required.

QA must preserve behavioral contracts rather than the superseded structural detail. In particular:

```text
LeaveUnresolved waits for explicit selection
Actor selection is Session-owned and Slot-specific
Local Player Host remains separate from Actor
Manager-Provisioned creates the Player Host before Actor materialization
Scene-Provided and Manager-Provisioned converge after admission
prepared Actor evidence matches the selected Actor/revision
Leave and Session termination release the correct runtime/presentation resources
ordinary Activity transitions do not duplicate or replace the Session physical Player
```

## Relationship to existing ADRs

### IF-ADR-003

IF-ADR-003 continues to describe the **currently implemented** Player Actor composition until this ADR is implemented.

The specific structural rule:

```text
ActorProfile.LogicalActorHostPrefab
= single authored prefab authority for the entire Scene-Provided Logical Actor
```

is superseded as the accepted forward architecture by IF-ADR-023.

Until implementation lands, this difference must be reported as:

```text
Accepted architecture: IF-ADR-023
Current implementation: legacy IF-ADR-003 structural composition
```

It must not be represented as already migrated or QA-certified.

### IF-ADR-016

Session initial configuration, Host Provisioning and Actor Resolution remain unchanged. `LeaveUnresolved` remains a complete valid initial policy.

### IF-ADR-019

Session ownership of the admitted physical Player remains unchanged. This ADR only separates internal Actor runtime/presentation composition authority.

## Implementation status

At acceptance time:

```text
Architecture decision       ACCEPTED
Runtime implementation      NOT IMPLEMENTED
Scene-Provided migration    NOT IMPLEMENTED
Manager-Provisioned migration NOT IMPLEMENTED
FIRSTGAME migration         NOT IMPLEMENTED
QA recertification          NOT RUN
```

Existing Player QA evidence remains historical/current evidence for the pre-IF-ADR-023 implementation boundary. It is not evidence that this ADR has been implemented.

## Acceptance criteria for closure

IF-ADR-023 may move to Implemented/Certified only when:

1. `ActorProfile` no longer owns the monolithic Logical Actor Host prefab.
2. the reusable Player Actor Runtime Host authority is explicit and inspectable from the Local Player Host composition;
3. Actor-specific Presentation is authored through the selected `ActorProfile`;
4. Manager-Provisioned preserves `LeaveUnresolved` and materializes Actor runtime only after selection;
5. Scene-Provided validates/adopts runtime Host + Presentation without a second competing Actor prefab authority;
6. FIRSTGAME Getting Started, Manager-Provisioned and Character Selection are migrated;
7. Farmer and Cow select distinct Presentations without duplicating framework Actor runtime infrastructure;
8. behavioral Player QA is reconciled and passes on the new boundary;
9. documentation no longer describes `ActorProfile.LogicalActorHostPrefab` as current authority after the implementation is complete.
