# IF-ADR-050 — Actor Occurrence Lifecycle, Authority and Commit

Status: **Proposed — non-normative pending architectural review**<br>
Proposed: **2026-10-17**<br>
Type: architecture / Actor / lifecycle / runtime authority<br>
Related decisions: [IF-ADR-001](IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md), [IF-ADR-003](IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md), [IF-ADR-014](IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md), [IF-ADR-020](IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md), [IF-ADR-024](IF-ADR-024-Prepared-Actor-Replacement-Public-Contract.md), [IF-ADR-038](IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md), [IF-ADR-040](IF-ADR-040-Scene-Composition-Binding-Model.md), [IF-ADR-049](IF-ADR-049-Public-Actor-Occurrence-Reference-and-Resolution.md)<br>
Governance: [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)<br>
Companion proposal: [Actor Lifecycle Participation and Composition Integration Contracts](../Specifications/IF-SPEC-050-Actor-Lifecycle-Participation-and-Composition.md)

## Context

Actor authoring identity, a physical Actor candidate, the current physical occurrence, public resolution, operational authorization and gameplay readiness are distinct concerns. Current Player preparation already stages an Actor inactive, but replacement changes selected-profile evidence before B is active and registered as current. The current input gate covers a specialized PlayerInput path, not arbitrary Actor behavior.

Existing decisions remain authoritative while this proposal is under review. In particular, IF-ADR-024 defines the accepted Manager-Provisioned replacement contract and IF-ADR-049 defines the Experimental occurrence reference contract. This proposal does not amend either decision by itself.

## Decision

Propose a hybrid lifecycle authority:

- Framework Core owns Actor occurrence identity, candidate validation, logical transitions, the committed current binding, commit, publication/invalidation and occurrence-scoped operational authorization.
- The authorized producer retains physical ownership and performs creation/adoption, activation, quiesce and physical release under its ownership model.
- The independent `com.immersive.actor-composition` package depends on Framework Core and coordinates occurrence-bound contributions. Core defines public extension contracts but does not reference the package's implementation.
- Resource adapters own concrete resources. Composition coordinates their dependencies and outcomes but does not become a universal Actor or resource owner.

The proposed model distinguishes authored definition/profile, `ActorId`, `ActorOccurrenceId`, physical candidate, operation identity, owner, committed current binding and resolved runtime configuration. A candidate receives a non-reused occurrence identity when Core accepts it; that identity does not itself mean committed, active, resolvable, authorized or ready. The current Player path uses `PlayerActorDeclaration.ActorId` as runtime occurrence identity; separating that use from `ActorOccurrenceId` is a proposed semantic/API migration, not a description of current behavior.

The proposed logical progression is `Candidate → Prepared → Activating → Committed/Current → Retiring → Released`. Pre-commit rejection leads to `Aborted`; physical cleanup may remain pending or fail after logical invalidation. Readiness and operational authorization are orthogonal to these lifecycle states. This proposal does not define public enums.

For A→B replacement, A remains the committed current binding while B is prepared and activated with operational gates closed. At handoff, Core revalidates the expected A binding, B candidate, owner and operation generation. A's operational entry may be suspended before commit. Core then performs one conditional logical commit of the new binding and authorization epoch; publication and notifications follow that coherent update. If the handoff fails, A remains logically current, but restoring its operational entry may itself fail and must be reported. A failure after B commits does not simulate rollback.

Operational authorization is scoped to an exact occurrence and validated by cooperative operations/adapters. Unity callbacks and direct effects from non-cooperative components cannot be universally suppressed by Core. Physical activation, logical commit, public resolution, operational authorization and gameplay readiness remain separate facts.

Closure first invalidates logical authority and pending commits. Composition coordinates cancellation and dependency-ordered disassociation; producers and adapters release only resources they own. Release requested, release confirmed, cleanup pending and cleanup failure are distinct evidence. Forced scene unload or external destruction receives no guarantee of successful callback execution or external-resource cleanup.

## Proposed scope

- Manager-Provisioned, Scene-Provided/adopted and externally produced NonPlayer occurrences, with guarantees qualified by each producer's ownership and ability to stage/gate.
- Occurrence-preserving replacement of associated content versus new occurrence identity for physical Actor replacement.
- Public Core contracts for producer participation, candidate context, conditional commit, scoped authorization, lifecycle observation and cleanup outcomes.
- An independently versioned Composition package with per-contribution identity/revision, explicit dependencies and adapters.

## Rejected scope

- A global Actor Manager, global registry/service locator, reflection-based package discovery or a Core dependency on `com.immersive.actor-composition`.
- Treating `ActorProfile` as a live occurrence, an active GameObject as committed, a committed occurrence as ready, or a release request as physical destruction confirmation.
- Universal rollback of Unity callbacks or resources owned by other systems.
- Reusing RuntimeContent internals as Actor lifecycle authority.
- Final C# signatures, Stable API promotion, package creation, implementation or consumer migration in this proposal.

## Consequences

The accepted Player replacement contract and current Actor identity/reference contracts require explicit reconciliation before implementation. In particular, adoption/publication timing and Player `ActorId` versus `ActorOccurrenceId` semantics must not be changed silently. The detailed contract boundaries, package architecture, current-source evidence, failure matrix and open decisions are in the companion specification.

Acceptance would authorize a later implementation plan, not implementation itself. Proposed contracts start without Stable guarantees; API maturity is assigned only through the normal contract governance and evidence process.

## Current implementation coverage

- Player materialization stages under an inactive root; `PlayerActorMaterializationHandle` activates physical roots before marking its own handle active.
- Player replacement changes selection evidence before B activation and records B after activation; current physical occurrence resolution depends on selection and preparation records.
- The PlayerInput gate controls its specialized action-map path; there is no generic Actor operational authorization contract.
- Player occurrence reference resolution uses the existing preparation authority; current Player `ActorId` usage and occurrence identity require reconciliation before introducing the proposed separate ID.
- RuntimeContent separates logical scope/handle transitions from physical release adapters. It does not provide the Actor lifecycle or Composition participant contract proposed here.
- `com.immersive.actor-composition` and the proposed Core extension/commit contracts are not implemented.

These statements describe reviewed source, not target guarantees. Source references and scope are recorded in the companion specification.

## Pending decisions

See the companion specification's decision register. Architectural review must settle the requirement signal for mandatory Composition, gate coverage, partial handoff outcomes, cleanup receipt ownership, adapter-specific release confirmation, bounded recovery policy and late-adoption guarantees before the relevant public contracts are stabilized.

## Normative dependency disposition

Until this ADR is accepted and the affected existing decisions are reconciled, IF-ADR-003, IF-ADR-024, IF-ADR-038 and IF-ADR-049 remain in force at their recorded scope. No current runtime or published API behavior is changed by this proposal.
