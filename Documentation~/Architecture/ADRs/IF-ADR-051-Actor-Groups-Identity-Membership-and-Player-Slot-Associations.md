# IF-ADR-051 — Actor Groups: Identity, Membership and Player Slot Associations

Status: **Accepted — target architecture**<br>
Accepted: **2026-10-28**<br>
Last updated: **2026-10-29**<br>
Supersedes: **none**<br>
Superseded by: **none**<br>
Related: [IF-ADR-003](IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md), [IF-ADR-038](IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md), [IF-ADR-042](IF-ADR-042-Session-Camera-Shared-Group-Assignment-Projection.md), [IF-ADR-047](IF-ADR-047-Initial-Stable-Baseline-and-Versioning-Policy.md), [IF-ADR-050](IF-ADR-050-Actor-Occurrence-Lifecycle-Authority-and-Commit.md), [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)

## Context

The Framework has logical Actor identity, physical Actor occurrences, reusable Actor Profiles, Player Slots, and a camera `SharedGroup` projection. These contracts have different owners and lifetimes. None represents a logical collective entity whose essential meaning is its own identity and its Actor membership.

PEGA has two confirmed, distinct Group use cases:

1. **Gangues:** NPC Actors belong to a collectively identified Gangue. Gangue membership is used by PEGA gameplay knowledge and cooperation.
2. **P.E.G.A. company:** Rick and Petra belong to the player-founded organization as logical Actors, independent of which Player Slots control them. This affiliation represents collective identity and membership and requires no Group-specific gameplay capability.

These cases are not merely two Gangue instances or profile variants. The first consumes the common identity/membership model plus an optional game-owned knowledge capability; the second uses the same core without a gameplay capability. The PEGA GDD describes Rick and Petra founding the P.E.G.A. security company after not being accepted into the official police P.G.A.; this decision concerns the P.E.G.A. company and does not assign them to the official police (`PEGA GDD/GDD.md`, §Visão; `PEGA GDD/MVP.md`, §Players). Current PEGA Gangue code demonstrates the first consumer capability; the second remains a confirmed design requirement, and neither context is claimed to have migrated to the Framework Group runtime.

The applicable architecture documentation skill requires at least two concrete use cases, not two different consumers or production games. This threshold is satisfied by the two PEGA contexts above. The `planet-devourer` `SharedGroup` sample remains a Session Camera projection over explicit Player Slots and current Actor Subjects; it is not needed as evidence for this decision. Cross-game adoption remains future reuse evidence, not an approval gate for the generic Group core.

## Decision

**Accepted target:** Framework Core owns a generic logical Group contract whose essential responsibilities are collective identity, Actor membership and Group lifecycle. A Group can exist with no Profile and no gameplay capability. It does not own a game's group taxonomy, membership policy, command authority, or the data and rules of systems that use Groups as references.

### Identity and profile

- A Group has its own typed `GroupId`, independent of ActorIds, Group Profiles, Player Slots, membership, and any physical occurrence. Reusing a Profile never merges Group identity or runtime state.
- A reusable authored Group Profile is optional and has a distinct typed `GroupProfileId`. Multiple Groups may reference one Profile. Profile data is read-only runtime configuration; mutable per-Group state is keyed by `GroupId` and is not stored by mutating the shared Profile. Neither Profile nor its configuration is required for Group identity or membership.
- `GroupId` and `GroupProfileId` have distinct `FrameworkIdentityDomain` values, appended as `Group = 260` and `GroupProfile = 270`. Existing domain values remain unchanged. Group contracts are Experimental; using the Stable identity-domain primitive does not promote them to Stable.
- Creating a Group is explicit and requires a valid `GroupId`; Group formation is never inferred from shared Profile, Actor membership, Actor occurrence, or Slot association. A `GroupId` is unique among all live Groups in its Framework runtime context, and creation rejects a collision. The owner does not form part of Group identity. Authored or dynamically generated ID supply must be explicit; this ADR does not add a global allocator or singleton.

### Membership and lifecycle

- Membership is an independently mutable many-to-many relation between `GroupId` and logical `ActorId`. One Actor may belong to several Groups; the Framework imposes no universal exclusivity, quota, or permanence rule.
- Membership is keyed by logical `ActorId`, never `ActorOccurrenceId`; no physical occurrence is required for the edge to exist. Under IF-ADR-050 D02, replacing a physical occurrence while retaining `ActorId` preserves membership; selecting/creating a different logical ActorId does not transfer it implicitly. The Group contract does not itself coordinate Actor replacement.
- The creator binds each Group to an explicit lifecycle owner. Releasing that owner releases its Groups and associated structural links; removal of the final member does not release a Group. Empty Groups are valid for the full lifetime of their owner. Persistence across process/session restart is not implied.
- The Framework permits explicit join, leave, and membership changes. A consuming game may restrict or authorize those operations according to its own rules; the Framework does not encode Gangue or faction policy.

### Player Slot association and capabilities

- Player Slot association is not part of the essential Group contract and is not required for membership: a Player-controlled Actor joins by its logical `ActorId`. A consumer may later require a separate many-to-many `PlayerSlotId`↔`GroupId` relation, independently scoped from membership. If provided, it is structural only; it must not imply effective control, route input, resolve competing commands, or require a current Player occurrence. Its Session scope, vacancy/rejoin behavior and cleanup are deferred until a concrete consumer case exists. No such mechanic is currently specified for PEGA.
- Capabilities are optional. A Group may have none. Other Framework or game systems may use `GroupId` as a neutral reference and, where appropriate, `GroupProfileId` for shared defaults. Each owning system retains its schema, merge rules, provenance, and lifecycle. Framework Core does not add a generic capability bag or depend on PEGA, GOAP, knowledge, instructions, or modifiers.

## Accepted scope

This ADR accepts the target architecture only. It makes no Runtime/API, authoring, serialization, or compatibility change. The minimal Group owner is Framework Core in `com.immersive.framework`, not `foundation` and not a new technical package. Player Slot association and consumer capabilities are not prerequisites for that core.

## Rejected scope

- Reinterpreting `ActorProfile` as a Group Profile, `ActorSet` as persistent membership, or camera `SharedGroup` as a domain Group.
- Reinterpreting Actor `Kind`/`Role` classification as Group identity or membership.
- Requiring a Profile or gameplay capability for a Group to exist.
- Making Group identity a function of its members, Profile, Slot, Actor GameObject, or occurrence.
- Inferring membership from physical composition, common Player Slot, camera assignment, or shared capabilities.
- Making Player Slot association a prerequisite of Group identity or Actor membership.
- Treating official P.G.A. and the player-founded P.E.G.A. company as the same organization or assuming Players are members of the official police.
- Putting Gangue rules, shared knowledge, GOAP integration, instructions, or modifier semantics in Framework Core.
- Treating structural Slot association as command/control ownership or defining command arbitration here.
- Using `RuntimeContentOwner` as Group identity or assuming it supplies Group registration/materialization. It is passive ownership data for runtime-created content, not a Group lifecycle service.

## Consequences

- The initial Framework runtime provides explicit Group creation/release authority, duplicate-live-ID rejection, exact owner lifetime binding and membership mutation while preserving the independence of identity, membership, Profile and optional relations.
- A Group has no required gameplay behavior. PEGA's Gangue case adds shared knowledge/cooperation; the P.E.G.A. company case needs only collective identity and membership. These two confirmed use cases satisfy the architecture documentation skill's generic-abstraction threshold.
- Membership remains attached to `ActorId` across a physical replacement when IF-ADR-050 D02 preserves that logical identity. An explicit logical Actor switch supplies a different ID and receives no implicit membership transfer; Group runtime does not coordinate the replacement.
- The append-only `FrameworkIdentityDomain` additions were reviewed under [IF-ADR-047](IF-ADR-047-Initial-Stable-Baseline-and-Versioning-Policy.md) and [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md). Existing numeric values and serialized meaning remain unchanged; no implicit migration or enum normalization was authorized.
- The authored Group and Profile assets use explicit typed IDs, and runtime Group state is owner-scoped. Persistence across process/session restart is not implemented or implied; Group state is not automatically save-game state.
- PEGA remains responsible for Gangue gameplay rules, shared knowledge and GOAP access. The Framework Group contract supplies only identity and membership. P.E.G.A. company membership is separate from the official police P.G.A.
- The two cases justify the minimal generic Group core even though they are in one game. They do not claim that cross-game reuse has already been demonstrated.

## Current implementation coverage

| Existing contract | Current meaning | Relevance to this decision |
|---|---|---|
| [`ActorId`](../../../Runtime/Actors/ActorId.cs) and [`ActorOccurrence`](../../../Runtime/Actors/References/ActorOccurrence.cs) | Logical Actor identity is distinct from an exact physical occurrence; occurrence APIs are Experimental. | `ActorId` is the proposed membership key; occurrence identity is not. |
| [`ActorProfile`](../../../Runtime/Actors/ActorProfile.cs) and [`ActorProfileId`](../../../Runtime/Actors/ActorProfileId.cs) | Reusable Actor option/configuration identity; profile does not own runtime Actor or occupancy state. | Establishes a nearby precedent, but has incompatible Actor semantics and is not reusable as Group Profile. |
| [`ActorSet`](../../../Runtime/Actors/ActorSet.cs) | A validation/result set of Actor descriptors and issues. | Does not own persistent membership or lifecycle. |
| [`PlayerSlotId`](../../../Runtime/PlayerSlots/PlayerSlotId.cs), [`PlayerSlotProfile`](../../../Runtime/PlayerParticipation/Authoring/PlayerSlotProfile.cs) | Slot identity and reusable participation/presentation metadata; the Profile may carry Actor selection intent. | Neither defines Group association nor effective control. |
| [`FrameworkIdentityDomain`](../../../Runtime/Identity/FrameworkIdentityDomain.cs), [`GroupId`](../../../Runtime/Actors/GroupId.cs), [`GroupProfileId`](../../../Runtime/Actors/GroupProfileId.cs) | Stable identity-domain enum preserves prior values and includes `Group = 260` and `GroupProfile = 270`; typed Group identities are Experimental. | Group identity and Profile identity are distinct and append-only; no existing Stable value is repurposed. |
| [`GroupDefinitionAsset`](../../../Runtime/Actors/GroupDefinitionAsset.cs), [`GroupProfile`](../../../Runtime/Actors/GroupProfile.cs) | Experimental authoring surfaces provide an explicit Group identity and optional reusable game-neutral Profile. | Asset loading does not create a runtime Group; sharing a Profile does not merge identity or runtime state. |
| [`ActorGroupSnapshot`](../../../Runtime/Actors/ActorGroupSnapshot.cs), [`ActorGroupAccessBinding`](../../../Runtime/Actors/ActorGroupAccessBinding.cs), [`ActorGroupRuntime`](../../../Runtime/Actors/ActorGroupRuntime.cs) | Experimental Session-runtime registry with explicit Group creation/access, owner-scoped lifecycle and mutable many-to-many membership by logical `ActorId`. | Group lifetime is independent of occurrence/member presence. Membership operations do not introduce Player Slot or consumer gameplay semantics. |
| [Camera `SharedGroup`](IF-ADR-042-Session-Camera-Shared-Group-Assignment-Projection.md) | Session Camera projection over explicit Player Slots and current Actor Subjects. | A consumer of grouped framing, not an Actor Group contract. |
| [`RuntimeContentOwner`](../../../Runtime/RuntimeContent/RuntimeContentOwner.cs) | Passive exact owner identity/scope used by the Group runtime; it is not itself a registry or release authority. | The Group runtime binds each live Group to one exact supported owner and performs release at that owner's close boundary. |

The initial Group core now provides typed Group/Profile identities, optional authoring, a live runtime view/access binding and `ActorId` membership. There is no PlayerSlot-to-Group API; that relation remains deferred. Existing Actor occurrence/profile contracts remain Experimental; `PlayerSlotId` and the additive `FrameworkIdentityDomain` values retain their separately declared Stable guarantees. See the [Public API Reference](../../API/Public-API.md#player-participation-and-actor), the [Actor Groups guide](../../Guides/Actor-Groups.md) and the [Framework tracker](../Tracking/IF-TRACK-Framework.md) for implementation and validation status.

PEGA's `PEGANpcGangMembership` and `PEGANpcGangKnowledgeState` are game-owned implementation of the Gangue knowledge capability, not a generic Group API. The GDD defines Gangue as the cooperation/knowledge/reservation unit. It also establishes P.E.G.A. Larópolis as the company founded by Rick and Petra. The confirmed Player Actor affiliation is the design case for identity and membership; it does not claim existing PEGA Runtime membership code.

## Pending decisions

No architectural decision remains open for the accepted core. The initial implementation uses explicit `GroupId` supply and duplicate-live-ID rejection, optional reusable authoring Profiles, exact Session/Route/Activity lifecycle ownership and mutable membership by logical `ActorId`; implementation and Unity evidence are tracked separately. Player Slot association remains deferred until a consumer has a concrete use case.
