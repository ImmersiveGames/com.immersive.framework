# ADR-PROD-0008 — Actor Profile, Logical Actor Host and Presentation Materialization

Status: Accepted  
Date: 2026-07-12  
Amended: 2026-07-22 — reconciled with the P3 Local Player Host / Actor Mount model  
Package: `com.immersive.framework`  
Area: Actor Product Surface / Runtime Materialization / Presentation  
Related: `ADR-PROD-0007`, `ADR-PROD-0009`, `ADR-PROD-0010`, `F45-ADR-ACTOR-001`, `F08-ADR-RUNTIME-001`, `ADR-PROD-0004`, `ADR-PROD-0006`

## Context

The framework needs a stable product identity that represents an Actor option before any Actor instance exists.

Examples include:

```text
Mage
Knight
Player Ship
Enemy Type A
Menu Avatar
Spectator Avatar
```

This object must support:

```text
default selection for a Player Slot
selection screens and lobbies
Session-persistent selection
save and network identity
explicit authoring references
runtime logical Actor materialization
product metadata and diagnostics
```

The existing `ActorDeclaration` and `PlayerActorDeclaration` are Unity components attached to concrete `GameObject` instances.

`ActorDeclaration` answers:

```text
"This GameObject is an Actor that exists now."
```

`PlayerActorDeclaration` is not a second declaration attached beside it. It is the specialized declaration type for a Player Actor and inherits the complete Actor declaration contract.

Neither component answers:

```text
"Which Actor identity/profile was selected before an instance existed?"
```

`Recipe` is not the correct product term for this responsibility. Within the framework vocabulary, a Recipe represents reusable configuration intent that may be shared or applied to several profiles or instances. The selected Actor object represents stable product identity and therefore uses `Profile`.

## Decision

Introduce `ActorProfile` as the canonical selectable, immutable and reusable Actor identity asset.

The complete model is:

```text
ActorProfile
  selected product identity and static metadata
  references the canonical Logical Actor Host prefab

Local Player Host
  technical Unity Input System host provisioned by PlayerInputManager
  contains PlayerInput, LocalPlayerHostAuthoring and an explicit Actor Mount
  is not itself a Logical Actor and does not own ActorId
  starts with an empty Actor Mount on the provisioned path

Logical Actor Host
  runtime Actor object with identity, contracts and behavior endpoints
  owned by an explicit Route or Activity context
  physically created through the official materialization path for its Actor shape

Actor Declaration Hierarchy
  ActorDeclaration for a generic Actor
  PlayerActorDeclaration : ActorDeclaration for a Player Actor
  exactly one declaration component per Logical Actor Host

Actor Presentation / Skin
  visual and presentation content initialized after the Logical Actor Host exists
  materialized through an explicit presentation contract
```

These layers are distinct and must not be collapsed.

## Canonical terminology

### ActorProfile

Answers:

```text
"Which Actor option/profile was selected?"
```

It exists before any runtime Actor instance.

It is immutable runtime product data.

### Logical Actor Host

Answers:

```text
"What Actor object currently exists and participates in runtime?"
```

It is a concrete runtime `GameObject`.

For generic Actors, the Profile-to-host relationship is materialized through the official RuntimeContent boundary.

For local Players, `PlayerInputManager` provisions only the configured technical `Local Player Host` after an explicit framework-authorized manual join. The host contains `PlayerInput`, `LocalPlayerHostAuthoring` and an explicit empty `Actor Mount`; it is not the Logical Actor.

When the owning Route or Activity requires an Actor and an `ActorProfile` is resolved, the framework materializes the Profile's canonical Logical Actor Host inside that Actor Mount. The technical host may therefore exist and the Slot may be `Joined` while logical Actor preparation remains unresolved.

The provisioned path must not place `PlayerActorDeclaration` on the technical host root and must not pre-author a Logical Actor below its Actor Mount. The separate scene-owned admission path may reference an already-authored Logical Actor below its Actor Mount, but it does not use `PlayerInputManager.JoinPlayer`.

### Actor Presentation / Skin

Answers:

```text
"How is this logical Actor represented visually in the current context?"
```

It may be created, replaced or released independently from the logical Actor identity.

### ActorDeclaration

Answers:

```text
"This runtime GameObject is an Actor."
```

It is the concrete and reusable declaration component for a generic Actor.

It owns the common Actor identity and descriptor surface:

```text
ActorId
ActorKind
ActorRole
Actor display and diagnostic evidence
generic Actor descriptor creation
```

`ActorDeclaration` remains concrete because non-Player Actors use it directly.

It must be inheritable and therefore must not be `sealed`.

### PlayerActorDeclaration

Answers:

```text
"This runtime Actor is specialized to participate as a Player."
```

Canonical inheritance:

```text
PlayerActorDeclaration : ActorDeclaration
```

A Player Logical Actor Host contains `PlayerActorDeclaration` instead of a separate `ActorDeclaration`.

The same `GameObject` must not contain both declaration components.

Because the derived declaration is an `ActorDeclaration`, generic Actor systems continue to discover and process Player Actors through:

```text
ActorDeclaration
IActor
generic Actor descriptors and validators
```

`PlayerActorDeclaration` adds only Player-specific Actor contracts and bridges, such as:

```text
runtime PlayerSlot assignment/configuration
local Player participation evidence
Player-specific readiness and diagnostics
```

It must not duplicate:

```text
ActorId
ActorKind
ActorRole
Actor display name
generic Actor descriptor authority
```

Those remain inherited from `ActorDeclaration`.

`PlayerActorDeclaration` does not require `PlayerInput` on the same `GameObject`. In the provisioned local-player path, `PlayerInput` belongs to the parent technical Local Player Host while `PlayerActorDeclaration` belongs to the contextual Logical Actor materialized below the Actor Mount.

The framework binds the Logical Actor to the host's input through an explicit typed and scoped binding. It must not discover that relation through name, tag or unrestricted hierarchy lookup.

`PlayerInput` alone does not declare a Player Actor. It declares the provisioned local-input host; `PlayerActorDeclaration` declares the Logical Player Actor.

### ActorId

Answers:

```text
"Which concrete logical Actor instance is this?"
```

It is runtime identity. It is not the Profile identity and is not a prefab key.

## ActorProfile as the authoring reference

Authoring surfaces reference the `ActorProfile` asset directly.

Example:

```text
Player Slot Default
  Actor Profile: [Mage]
```

They must not duplicate the Profile ID as manually typed text:

```text
incorrect:
  Actor Profile Id: "actor.mage"

correct:
  Actor Profile: [Mage]
```

The Profile owns its stable identity text in one place.

Representative shape:

```text
ActorProfile
  ActorProfileId
  Display Name
  Actor Kind
  Actor Role
  Logical Actor Host Prefab
```

Optional product metadata may include:

```text
Icon
Description
Selection metadata
Designer-facing tags
```

The Profile must not contain instance-specific or mutable runtime state such as:

```text
ActorId
PlayerSlotId
runtime scope
spawn point
PlayerInput user or device
camera request id
current occupancy
current health
runtime progression
```

## ActorProfile identity

`ActorProfile` has a stable identity distinct from `ActorId`.

```text
ActorProfileId
  identifies the reusable product identity, such as "actor-profile.mage"

ActorId
  identifies one concrete runtime Actor instance
```

Example:

```text
ActorProfileId:
  actor-profile.mage

ActorId:
  session-42.route-gameplay.slot-1.actor
```

The same `ActorProfile` may produce many runtime Actors across Sessions or contexts.

The ScriptableObject reference is the primary authoring link. The stable Profile ID is used for:

```text
save
network transport
content reconciliation
diagnostics
resolution after persistence
```

The Profile ID text is authored only inside the Profile asset.

## Direct Logical Actor Host reference

`ActorProfile` directly references its canonical Logical Actor Host prefab.

Example:

```text
Mage Actor Profile
  Logical Actor Host Prefab: [MageActorHost]
```

This direct reference is preferred over authoring a string map such as:

```text
ActorProfileId -> prefab path
```

The Logical Actor Host prefab defines how the Actor is constructed as a runtime entity.

A Logical Actor Host contains exactly one Actor declaration component.

Generic Actor host:

```text
ActorDeclaration
Presentation / Skin contract
```

Local Player Actor host:

```text
PlayerActorDeclaration : ActorDeclaration
Presentation / Skin contract
```

Provisioned technical Local Player Host:

```text
PlayerInput
LocalPlayerHostAuthoring
Actor Mount
  empty before contextual Actor preparation
```

Representative optional contents may include:

```text
PlayerComposer when the Actor is player-controllable
input and control endpoints
camera target anchors
reset endpoints
gameplay capability endpoints
diagnostic evidence
```

Movement, camera targets, attributes, reset, combat, abilities and other gameplay behaviors are optional compositions shared by Player and non-Player Actors. They are not part of the universal Player identity.

The prefab must not contain fixed runtime identity for every instance.

Instance data is assigned when the host is materialized:

```text
ActorId
PlayerSlotId when applicable
runtime owner scope
local or remote participation evidence
input association
occupancy relation
```

## Logical Actor Host validity

The final product validity rules are:

### Generic Actor

```text
ActorDeclaration
Presentation / Skin contract
```

### Local Player Actor

```text
PlayerActorDeclaration : ActorDeclaration
Presentation / Skin contract
```

A Local Player Actor is therefore a specialized Actor, not a second parallel entity system.

The specialized declaration preserves all generic Actor behavior while adding Player-only bridges.

### Current P3 implementation gate

Actor Presentation / Skin has not yet been ported into the new framework.

The reconciled executable P3 minimum is split across two objects:

```text
Technical Local Player Host
  PlayerInput
  LocalPlayerHostAuthoring
  empty Actor Mount

Contextual Logical Player Actor, when prepared
  PlayerActorDeclaration : ActorDeclaration
```

The architectural requirement remains:

```text
Presentation / Skin is mandatory for the final Actor and Player product shape.
```

P3 must not introduce a fake placeholder component merely to claim that Presentation exists. The missing Presentation implementation must remain explicit in validation, documentation and follow-up planning.

A generic object that participates only in reset, snapshot, timers or another framework subsystem is not automatically an Actor and does not receive `ActorDeclaration` unless it actually represents an Actor entity.

## Materialization authority

`ActorProfile` does not instantiate anything.

### Generic Actor path

The owning Route or Activity context:

```text
resolves the selected ActorProfile
reads its Logical Actor Host prefab
materializes the host through the official RuntimeContent boundary
assigns runtime identity and contextual participation
admits the Actor into the context
```

### Local Player path

The local Player path is specialized by `ADR-PROD-0010`:

```text
framework or authorized product adapter requests manual join
-> PlayerInputManager provisions the technical Local Player Host
-> framework receives the created PlayerInput instance
-> validates LocalPlayerHostAuthoring, PlayerInput and the empty Actor Mount
-> binds PlayerSlotProfile and admits the Slot as Joined
-> Route/Activity later resolves the selected/default ActorProfile when required
-> framework materializes the Logical Player Actor inside the Actor Mount
-> validates PlayerActorDeclaration on the Logical Actor
-> assigns ActorId and admits the Actor into the contextual lifetime
```

This specialized technical provisioner does not transfer Slot, Profile, occupancy or contextual lifetime authority to `PlayerInputManager`.

The technical host and Logical Actor must remain separate. `PlayerInputManager` must not instantiate a prefab that already collapses both layers on the provisioned path.

The Logical Actor Host does not choose its own Route or Activity lifetime.

Supported lifetime shapes remain defined by `ADR-PROD-0007`:

```text
Route-owned
Activity-owned
Route-owned with Activity activation
```

## Presentation / Skin initialization

After the Logical Actor Host exists and has valid runtime identity, it may initialize its visual presentation.

Canonical flow:

```text
ActorProfile selected
-> Route/Activity materializes Logical Actor Host
-> runtime identity and context are assigned
-> Logical Actor Host becomes initialized
-> host requests or coordinates Actor Presentation / Skin materialization
-> presentation content is attached to the logical host
```

The Logical Actor Host is the runtime owner/source of presentation intent because it now exists and can provide:

```text
Actor identity
presentation anchors
current contextual requirements
current skin or presentation selection
release lifetime
diagnostic source
```

However, the host must not perform uncontrolled `Instantiate` or `Destroy`.

Presentation materialization must use an explicit, typed and scoped presentation boundary or adapter.

The exact presentation API and the source of the selected Skin/Presentation Profile or Recipe remain separate implementation decisions.

## Two materialization layers

### Logical Actor materialization

```text
ActorProfile
-> Logical Actor Host prefab
-> runtime Actor instance
```

Owned by:

```text
Route or Activity contextual lifetime
```

Produces:

```text
exactly one declaration:
  ActorDeclaration
  or PlayerActorDeclaration : ActorDeclaration

ActorId
runtime contracts and endpoints
camera/input/reset evidence when applicable
```

### Presentation materialization

```text
Logical Actor Host
-> selected Presentation / Skin intent
-> visual presentation instance
```

Owned or coordinated by:

```text
the initialized Logical Actor Host
through a scoped presentation subsystem
```

Produces:

```text
renderers
animator
visual model
effects
visual anchors
presentation-specific content
```

The presentation is not the Actor identity.

## Expected flow — Player Actor

```text
Session
-> Player 1 selected ActorProfile Mage

Gameplay Route enters
-> projects Player 1
-> resolves Mage ActorProfile
-> resolves Player 1 technical Local Player Host and its Actor Mount
-> contextual materialization creates MageActorHost inside the Actor Mount
-> finds PlayerActorDeclaration inherited as ActorDeclaration
-> binds the Logical Actor to the host's PlayerInput through the official typed bridge
-> assigns ActorId and PlayerSlotId
-> initializes Player-specific contracts
-> confirms contextual occupancy
-> MageActorHost requests Mage presentation
-> presentation subsystem materializes Mage visual content
-> input and camera bindings attach to the logical host
```

On Route exit:

```text
camera and input bindings release
-> effective occupancy clears
-> presentation releases
-> Logical Actor Host releases through the official contextual materialization boundary
-> Session keeps Player 1 selection = Mage ActorProfile
-> technical Local Player Host may remain until explicit leave or Session release
```

## Expected flow — presentation replacement

```text
Mage logical Actor remains active
-> presentation selection changes to Mage Armored
-> current Mage presentation releases
-> Mage Armored presentation materializes
-> ActorId and PlayerSlot occupancy remain unchanged
```

This enables Skins and contextual presentations without replacing the logical Actor.

## Profile versus Recipe

```text
Profile
  stable product identity
  selected or referenced as "which one"
  immutable at runtime
  owns its canonical Profile ID

Recipe
  reusable configuration intent
  shared or applied as "how to configure"
  does not represent a unique product identity
```

Examples:

```text
ActorProfile Mage
  identifies Mage and points to MageActorHost

CameraRigRecipe Follow Close
  reusable rig configuration that several cameras may apply
```

A future Actor configuration Recipe may be referenced by several ActorProfiles, but it must not replace ActorProfile identity.

## Product authoring direction

```text
Create Actor Profile
-> assign stable ActorProfileId
-> assign display metadata
-> assign Logical Actor Host prefab
-> validate prefab contracts
-> use ActorProfile in Slot defaults, selection lists or runtime requests
```

Expected advanced diagnostics:

```text
selected ActorProfile
ActorProfileId
Logical Actor Host prefab
runtime ActorId
context owner
active presentation
presentation materialization status
release evidence
```

## Guardrails

```text
Do not use ActorId as ActorProfile identity.

Do not use a prefab path, GameObject name, tag or hierarchy path as Profile identity.

Do not duplicate ActorProfileId strings in authoring consumers.

Do not mutate ActorProfile at runtime.

Do not treat the ActorProfile asset as a runtime Actor instance.

Do not let ActorProfile instantiate or destroy content.

Do not treat the visual presentation prefab as the logical Actor.

Do not assign fixed ActorId or PlayerSlotId to every prefab instance.

Do not let the Logical Actor Host silently choose its own Route/Activity lifetime.

Do not allow uncontrolled Instantiate/Destroy for Actor Presentation.

Do not require every ActorProfile to be player-controllable.

Do not attach ActorDeclaration and PlayerActorDeclaration together.

Do not implement PlayerActorDeclaration as a sibling component that repeats Actor identity.

Do not infer Player Actor identity from PlayerInput alone.

Do not attach PlayerActorDeclaration to the provisioned technical Local Player Host root.

Do not pre-populate the provisioned host's Actor Mount with a Logical Actor.

Do not assign ActorId merely because the technical Local Player Host joined.

Do not duplicate ActorId, ActorKind, ActorRole or generic Actor descriptor authority
inside PlayerActorDeclaration.

Do not make movement, camera, health, reset, combat, abilities or attributes
mandatory merely because an Actor is a Player.

Do not treat reset/snapshot/timer participants as Actors unless they are actual
Actor entities.

Do not move game-specific movement, combat or progression authority into ActorProfile.
```

## Out of scope

This ADR does not decide:

```text
the exact ActorProfile C# fields and validation API
the final ActorProfileId serialization primitive
how persisted Profile IDs are resolved to loaded assets
Addressables or DLC catalog behavior
the exact Presentation / Skin component and ported runtime API
the final runtime materialization request/result shape
the source and structure of Skin or Presentation selection
presentation pooling
network spawning and authority
```

Those decisions must preserve the layered separation established here, including the
technical Local Player Host boundary used by provisioned local Players.

## Technical acceptance criteria

```text
ActorProfile is an immutable ScriptableObject referenced directly by authoring surfaces.

ActorProfile owns one stable Profile ID used for persistence and diagnostics.

ActorProfile identifies the canonical logical Actor product content.

Generic Actors are materialized through RuntimeContent.

Local Players are provisioned through PlayerInputManager after an explicit authorized
manual join. This creates the technical Local Player Host, not the Logical Actor.

The technical host receives PlayerSlot participation state after join. ActorId is assigned
only when the contextual Logical Actor is materialized.

A generic Actor host contains ActorDeclaration.

A local Player Logical Actor contains PlayerActorDeclaration inheriting ActorDeclaration,
and does not contain a second ActorDeclaration component.

The provisioned technical Local Player Host contains PlayerInput, LocalPlayerHostAuthoring
and an explicit empty Actor Mount, but no Actor declaration.

Generic Actor systems recognize PlayerActorDeclaration through ActorDeclaration/IActor.

PlayerActorDeclaration does not duplicate generic Actor identity authority.

Presentation materialization occurs only after the logical host exists and is initialized.

Presentation can be replaced without changing ActorId or PlayerSlot occupancy.

Release is ordered and diagnostic:
bindings -> occupancy -> presentation -> logical host.

No functional string lookup, name lookup, singleton or service locator is introduced.
```

## Product acceptance criteria

```text
A designer selects ActorProfile assets rather than typing IDs in several Inspectors.

A minimal ActorProfile clearly shows which logical host will be constructed.

Prefab validation clearly reports whether the host is:
generic Actor,
local Player Actor,
or invalid.

A designer does not add both ActorDeclaration and PlayerActorDeclaration.

A selected Profile can persist across Route transitions while its host is released.

The same logical Actor can change Skin or presentation without changing identity.

Advanced/Debug distinguishes:
Profile,
logical host,
runtime Actor,
presentation.
```

## Consequences

### Positive

```text
Selection, runtime identity and visual presentation are separated.

ActorProfile provides a safe designer-facing identity and reference.

The logical Actor can survive presentation changes.

Skins, variants and contextual visuals do not require Actor replacement.

Player and non-player Actors share the same foundational Profile and declaration model.

Player specialization does not require two declaration components or a parallel Actor system.

RuntimeContent remains the official generic Actor host materialization boundary.

PlayerInputManager is the explicit specialized physical provisioner for technical Local Player Hosts.

Logical Player Actors remain contextually materialized below the Actor Mount.
```

### Cost

```text
ActorProfile requires validation of its logical host prefab.

A runtime configure step must assign instance identity and contextual data.

Presentation initialization requires an explicit subsystem or adapter.

ActorDeclaration must become inheritable without weakening its common identity authority.

PlayerActorDeclaration remains the derived Logical Actor declaration and requires an
explicit typed binding to the technical host's PlayerInput, not a same-GameObject dependency.

Existing PlayerRecipe and PlayerComposer responsibilities must be reconciled
with ActorProfile without creating competing product authorities.
```

## Suggested commit message

```text
Docs: reconcile Local Player Host and Logical Actor materialization
```
