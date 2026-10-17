# IF-ADR-049 — Public Actor Occurrence Reference and Resolution

Status: **Accepted**
Proposed: **2026-10-16**
Accepted: **2026-10-16**
Type: architecture / Actor / runtime identity / public API
Related decisions: [IF-ADR-003](IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md), [IF-ADR-014](IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md), [IF-ADR-038](IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md), [IF-ADR-040](IF-ADR-040-Scene-Composition-Binding-Model.md)
Governance: [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)

## 1. Context

The Immersive Framework distinguishes authored Actor identity, runtime Actor occurrences, physical materialization, and lifecycle ownership.

However, game systems currently lack a canonical public contract for resolving the physical Unity object corresponding to a live Actor occurrence.

This limitation affects gameplay consumers such as NPC behavior, interactions, scripted sequences, targeting, and presentation.

A concrete PEGA use case requires an NPC to obtain the current physical Player Actor to determine its movement intention. The movement mechanic itself is already independent of Player input.

### 1.1 Existing Player infrastructure

The Framework already retains physical Player Actor evidence through:

- `PlayerActorPreparationRuntimeContext`
- `PlayerActorMaterializationHandle`
- `IPlayerPreparedActorOccurrenceSource`
- `PlayerActorDeclaration`

Physical preparation is contextualized by Route/Activity participation but uses Session physical ownership.

For Manager-Provisioned Players, the Framework materializes the Actor under the Session-owned Player Host.

For Scene-Provided Players, the original scene composition is adopted into Session physical lifetime after successful admission.

Activity exit releases contextual participation without implicitly destroying the admitted physical Player Actor.

The internal `IPlayerPreparedActorOccurrenceSource` provides physical Actor evidence to Framework subsystems, including Camera integration. Its current resolution path depends on contextual Player evidence and is not an unrestricted physical-occurrence query.

These mechanisms must be reused rather than duplicated.

### 1.2 Existing generic Actor infrastructure

`IActor`, `ActorDeclaration`, `ActorId`, and `ActorSet` provide Actor identity, classification, and validation.

They do not own materialization or lifecycle and do not establish a generic runtime reference registry.

IF-ADR-014 additionally establishes an owner-aware physical binding model for Object Entries through `StableObjectBindingRegistry`.

That model provides important precedents:

- Runtime references are associated with exact physical occurrences.
- Binding and lifecycle ownership are separate.
- Resolution respects explicit owners and scopes.
- Missing or ambiguous references fail explicitly.
- Released or destroyed occurrences cannot resolve as current objects.

Nevertheless, `ObjectEntryId` is not interchangeable with `ActorId`, and the existing Object Entry binding implementation must not become an implicit Actor registry.

## 2. Decision

The Framework will provide an **Experimental, public, read-only Actor Occurrence Reference and Resolution contract**.

This contract allows gameplay and presentation consumers to obtain the current physical Unity occurrence of a Framework-recognized Actor without obtaining authority over its materialization, ownership, replacement, or release.

The contract applies to Actors independently of `ActorKind`, including Player and NonPlayer Actors.

The Framework does not become the universal lifecycle owner of those Actors.

The canonical design separates:

1. Actor occurrence publication by its authoritative producer.
2. Actor occurrence reference resolution by consumers.
3. Runtime binding and invalidation according to the authoritative lifecycle.

The public contract must be host-scoped and must not introduce a process-global Actor service.

## 3. Identity and reference semantics

The following identities remain distinct:

| Concept | Meaning |
|---|---|
| `ActorProfileId` | Reusable authored Actor option |
| `ActorId` | Typed Actor identity |
| `PlayerSlotId` | Stable Player Slot identity |
| Runtime Actor occurrence | One concrete physical Actor instance within a lifecycle |
| Runtime owner | Authority responsible for that occurrence's physical lifetime |
| Physical object | Unity `GameObject` representing the Actor's physical root |

An `ActorProfile` does not identify a unique live Actor occurrence.

A `PlayerSlotId` identifies the Player Slot, not a particular Actor incarnation.

An `ActorId` alone does not establish globally unique runtime ownership.

The runtime binding must preserve sufficient ownership and occurrence evidence to distinguish multiple live occurrences and reject ambiguous resolution.

Repeated authored identities must not silently overwrite existing bindings or cause accidental selection.

A replacement occurrence must be distinguishable from the occurrence it replaced.

## 4. Physical occurrence authority

### 4.1 Player Actors

The Player Session remains authoritative for the admitted Player and its current physical Actor.

Route or Activity participation may initiate preparation, but it does not acquire physical ownership of a successfully admitted Session-owned Actor.

The Player integration must obtain physical evidence from the existing Player preparation and materialization authorities.

Resolution by `PlayerSlotId` must return the current valid physical Actor for that Slot when one exists.

Activity contextual assignment must not be an implicit requirement for a **physical-existence** query.

### 4.2 NonPlayer Actors

A NonPlayer Actor may be owned by an Activity, Route, Session, or another explicitly defined runtime producer.

Its declaration does not automatically establish lifecycle ownership or runtime publication.

The responsible producer must publish the Actor occurrence through an explicit, owner-aware integration.

The publication must identify the actual physical Actor root and its valid runtime owner.

Publishing an occurrence must not transfer lifecycle authority to the Framework.

### 4.3 Physical root

The resolved physical object is the Actor occurrence root.

Optional visual content, Animator objects, Camera Subjects, and technical Player Hosts do not substitute for the physical Actor root.

Consumers may resolve additional components from the returned root according to their own gameplay contracts.

## 5. Public consumer contract

The public API will provide three capabilities.

### 5.1 Reference selection

Consumers may describe the Actor they want to reference without storing an already materialized Unity object.

At minimum, supported selection semantics must include:

- **Player Slot selection:** the current physical Actor of an explicitly selected `PlayerSlotProfile`.
- **Actor identity selection:** an Actor identified through `ActorId` and a compatible runtime ownership selector.

The authoring model must support explicit selection through Inspector-compatible references.

It must not rely on GameObject names, tags, hierarchy paths, global searches, or hardcoded Player indices.

Selectors are addressing information, not permission to control the Actor or its lifecycle.

### 5.2 Current occurrence resolution

A successful resolution exposes an immutable evidence value containing, at minimum:

- Typed Actor identity.
- Physical Unity Actor root.
- Sufficient occurrence identity or revision to distinguish incarnations.
- Relevant ownership or scope evidence.
- Current validity information.

The exact public C# type names and representation are implementation decisions.

The reference is read-only in its **authority semantics**. This does not make the returned Unity object immutable; consumers remain responsible for respecting the Actor's gameplay and spatial authority.

Resolution must distinguish success from unavailable, invalid, stale, and ambiguous references.

It must not return retained release-failure evidence as a current physical occurrence.

### 5.3 Change observation

A consumer may observe changes affecting its selected reference.

At minimum, the contract must support:

- Occurrence becoming available.
- Current occurrence being replaced.
- Occurrence becoming unavailable or invalid.
- Consumer binding or owning scope being released.

Consumers must also be able to query the current state when first bound, without relying exclusively on an event that may already have occurred.

Notifications are read-only and do not initiate Actor preparation, replacement, or release.

Callbacks must not expose intermediate, uncommitted materialization states.

## 6. Physical existence and contextual eligibility

Physical existence and gameplay eligibility are different contracts.

**Physical occurrence resolution** answers:

> Does the specified Actor currently have a valid physical occurrence, and what is its physical root?

**Contextual eligibility** answers:

> Is that Actor currently admitted or eligible to participate in a particular Activity, gameplay operation, or target-selection policy?

The new reference contract resolves physical occurrence validity.

It does not automatically imply:

- Active Activity participation.
- Gameplay readiness.
- Runtime gameplay input availability.
- NPC perception.
- Valid targeting or interaction.
- Authority to move or modify the Actor.

Existing Player participation and Camera Subject eligibility rules remain authoritative within their own domains.

Contextual eligibility may be evaluated by separate consumers or existing domain contracts. It must not silently redefine physical reference resolution.

## 7. Lifecycle and invalidation

Publication must follow the authoritative physical lifecycle.

A reference becomes available only after successful physical preparation/adoption and the relevant authoritative commit.

Failed preparation and rollback must not publish a valid occurrence.

For Player Actors:

- Successful preparation/adoption makes the physical occurrence resolvable.
- Activity exit alone must not invalidate a still-valid Session-owned physical occurrence.
- Successful Actor replacement updates the current reference.
- Leave, physical release, or Session termination invalidates it.
- Rejoin or rematerialization establishes a distinguishable occurrence.

For other Actors:

- Publication follows successful registration by their lifecycle owner.
- Scope release or explicit physical destruction invalidates the corresponding occurrence.
- A subsequent materialization establishes a new runtime occurrence, even when authored identity is reused.

A consumer must not receive a stale occurrence after authoritative invalidation.

Failed replacement must preserve the previously valid occurrence if the authoritative transaction preserves it.

Release failures must be reported explicitly; retained cleanup evidence is not treated as a valid current reference.

## 8. Runtime composition

The resolution infrastructure must belong to the active Framework application/runtime composition.

It must not be implemented as a static global dictionary, singleton, or automatic scan of all scene Actors.

Player occurrence publication must integrate with the existing Player preparation authority.

Other producers may participate through explicit owner-scoped publication and release contracts.

For managed scene content, integration should reuse the established composition and lifecycle infrastructure where compatible, respecting IF-ADR-040.

Player admission and materialization remain governed by their existing specialized lifecycle contracts; generic composition binding must not replace those mechanisms.

The implementation must evaluate reuse of existing binding primitives before introducing any new storage or registration subsystem.

## 9. Authoring surface

Provide a designer-facing reference consumer or binding component suitable for Unity scene and prefab composition.

Its responsibilities are limited to:

- Holding an explicit Actor reference selector.
- Receiving the appropriate runtime resolver through supported composition.
- Exposing current resolved occurrence information.
- Notifying interested consumers of changes and invalidation.
- Releasing its binding when the owning consumer scope ends.

It does not instantiate Actors, determine Player membership, execute gameplay actions, or manage physical ownership.

The same reference semantics must be usable from ordinary C# consumers without requiring all gameplay systems to inherit from a specific MonoBehaviour.

The concrete component and interface names will be selected during API design.

## 10. Compatibility with existing architecture

This decision extends existing Framework responsibilities without replacing them.

- **IF-ADR-003:** Player participation, selection, preparation, and lifecycle remain under Session authority.
- **IF-ADR-014:** Stable identities are not runtime ownership authority; exact owner and occurrence semantics remain mandatory.
- **IF-ADR-038:** The Actor occurrence root remains the physical/spatial authority. Camera Subject semantics remain separate.
- **IF-ADR-040:** Scene composition remains explicit, scoped, and lifecycle-aware.
- **IF-GOV-001:** The new public contract begins as Experimental, with maturity tracked by the consumer contract rather than the package version.

The internal `IPlayerPreparedActorOccurrenceSource` and existing materialization handles must not be promoted directly as public API merely to satisfy consumers.

No existing Stable contract is implicitly changed or promoted by this decision.

## 11. Rejected alternatives

**Expose internal Player Actor preparation APIs directly.**
Rejected because those contracts include internal lifecycle and contextual assumptions inappropriate for general consumers.

**Use `GameObject.Find`, tags, or hierarchy-based discovery.**
Rejected as a canonical reference contract because they do not preserve explicit identity, occurrence, or lifecycle authority.

**Create a universal Actor Manager.**
Rejected because Actor materialization and lifecycle remain distributed among their actual authorities.

**Register every `ActorDeclaration` automatically on enable.**
Rejected because declaration alone does not establish a valid owner or committed runtime occurrence.

**Reuse `ActorProfile` as a unique runtime key.**
Rejected because authored Actor options and concrete occurrences are separate concepts.

**Treat `StableObjectReference` as an Actor reference without distinction.**
Rejected because Object Entry and Actor identity domains have different semantics.

**Limit the public API to Players.**
Rejected because the reference contract is inherently applicable to all Framework-recognized Actors, even though Player publication has the strongest existing implementation.

## 12. Implementation and documentation requirements

A subsequent implementation plan must:

1. Audit the exact installed source revision and existing public/internal Actor, Player, RuntimeContent, and scene composition contracts.
2. Define the smallest public selector, resolved-occurrence evidence, and read-only resolver/observer contracts.
3. Integrate Player occurrence publication using the existing Session preparation authority.
4. Support explicit owner-aware publication of NonPlayer Actor occurrences without assuming Framework lifecycle ownership.
5. Implement the authoring binding and its scoped consumer lifecycle.
6. Validate availability, replacement, Activity exit, Leave, release, stale references, duplicates, ambiguous ownership, and failure rollback.
7. Update `Documentation~/API/Public-API.md`, relevant usage guides, API maturity metadata, and architecture references.
8. Demonstrate consumption from an external game project without accessing internal Framework APIs.

Validation must distinguish Framework contract behavior from consumer gameplay behavior.

The PEGA shared-movement proof is a motivating consumer, not a requirement to implement NPC AI or movement inside the Framework.

## 13. Consequences

### Benefits

- Game systems can obtain physical Actor references without depending on Player-specific internals.
- Player and NonPlayer Actors share a consistent reference model.
- References remain valid across Activity transitions when the physical occurrence persists.
- Actor replacement and release become observable without stale object retention.
- Gameplay systems remain independent of Actor lifecycle ownership.
- The design builds on existing Framework identity, Session, and runtime composition authorities.

### Costs and constraints

- Publication and invalidation must be integrated precisely with authoritative commits.
- Multiple live occurrences and owner ambiguity require explicit handling.
- Authoring references must distinguish stable identity from runtime incarnation.
- External Actor producers must provide trustworthy ownership and lifecycle evidence.
- A generic reference contract must not become an implicit global Actor registry.

## 14. Acceptance criteria

This ADR may be considered implemented when:

- A public consumer can resolve P1's current physical Actor through an authored Player Slot reference.
- The same consumer can observe replacement and invalidation without retaining stale objects.
- A Session-owned physical Player remains resolvable across Activity exit while its physical occurrence remains valid.
- A NonPlayer Actor can be published and resolved through the generic contract without becoming Session-owned.
- Duplicate or ambiguous references fail deterministically.
- Actor declarations do not automatically register themselves without owner authority.
- Neither gameplay consumers nor the new public surface depend on internal Framework types.
- The corresponding public API documentation and usage guidance are consistent with the implemented behavior.

## 15. Decision status and implementation evidence

The architecture defined by this ADR is **Accepted**. Acceptance establishes the intended consumer and ownership boundaries; it does not certify implementation, promote API maturity, or authorize a package release.

As of 2026-10-17, the Experimental Actor Occurrence Reference contracts, owner-scoped runtime, Session Player evidence adapter, authoring publication surface, and Route/Activity/Persistent Content/Scene Lifecycle integrations are included in the `1.2.0-preview.2` release candidate. Public API documentation and usage guidance are included with that source.

The Project Owner reports **11/11 PASS** for `Immersive.Framework.ActorReferences.Editor.Tests` in Unity EditMode. This is functional evidence for that test assembly and is not an external-game integration certificate. The same reported overall EditMode run had four failures in `CameraRigPreviewTests`, outside the Actor References test assembly; those corrections remain separate and are excluded from this release.

The exact candidate source commit and signed artifact provenance are recorded in the Framework tracker after publication. **Pending consumer validation:** exercise public Player Slot resolution across Activity transitions, replacement and Leave, plus owner-aware NonPlayer publication, ambiguity and release invalidation in PEGA. No PEGA integration result is claimed by this release.

The contract remains **Experimental** under IF-GOV-001. ADR acceptance, implementation completion, consumer validation, and Stable promotion are separate decisions.
