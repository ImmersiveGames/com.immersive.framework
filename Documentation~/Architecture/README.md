# Immersive Framework Architecture Documentation

Use this page to navigate architecture records. It does not own mutable implementation status or duplicate feature contracts.

## Sources of truth

- [ADRs directory](ADRs/) contains accepted, proposed and superseded architecture decisions. Accepted decisions define the normative architecture; proposed decisions are not yet normative. Current status is indexed by the [Framework tracker](Tracking/IF-TRACK-Framework.md#current-adr-status).
- [Framework tracker](Tracking/IF-TRACK-Framework.md) is the mutable status and evidence index. Check it for current implementation, validation gates and certification scope.
- `Reconciliation/` preserves dated, bounded evidence and historical reconciliation. A record proves only the stated boundary at its stated date.
- [Plans](Plans/README.md) index implementation plans. Follow only plans explicitly marked active; historical and superseded plans remain traceability records.
- [Governance](Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md) defines public API maturity, validation, and risk-proportionate evidence policy.
- [IF-ADR-047](ADRs/IF-ADR-047-Initial-Stable-Baseline-and-Versioning-Policy.md) records 1.1.0 as the first official Stable baseline and the SemVer policy from that baseline.
- [IF-ADR-048](ADRs/IF-ADR-048-Framework-1.1.0-Stable-Readiness-Roadmap.md) defines the Accepted readiness phases and explicit GO/NO-GO gate for that baseline; acceptance does not certify phase completion or authorize publication.
- [Archive](Archive/README.md) retains superseded documents and dated tracker deltas for traceability; archived material is not current guidance.
- `Audits/` contains scoped audit records; check tracker and ADR status before treating an audit as current.

## ADR navigation by concern

- Application setup, Persistent Content and framework boundaries: [ADR-001](ADRs/IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md), [ADR-002](ADRs/IF-ADR-002-Product-Authoring-Model.md), [ADR-008](ADRs/IF-ADR-008-Persistent-Application-Content-Composition.md), [ADR-045](ADRs/IF-ADR-045-Persistent-Content-Scene-Reference-Player-Builds.md) (Accepted; implementation and validation pending).
- Game Flow, Routes, Activities and content: [ADR-006](ADRs/IF-ADR-006-Loading-Transition-Persistence-and-Diagnostics.md), [ADR-007](ADRs/IF-ADR-007-Activity-Entry-Readiness-and-Reveal-Gating.md), [ADR-009](ADRs/IF-ADR-009-Activity-Local-Visibility-Rules.md), [ADR-012](ADRs/IF-ADR-012-Activity-Player-Participation-Profile-and-Readiness-Compatibility.md), [ADR-021](ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md), [ADR-040](ADRs/IF-ADR-040-Scene-Composition-Binding-Model.md), [ADR-045](ADRs/IF-ADR-045-Persistent-Content-Scene-Reference-Player-Builds.md) (Accepted identity contract).
- Player Participation and Actor: [ADR-003](ADRs/IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md), [ADR-015](ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md), [ADR-015B certification](Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md), [ADR-019](ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md), [ADR-024](ADRs/IF-ADR-024-Prepared-Actor-Replacement-Public-Contract.md), [ADR-025](ADRs/IF-ADR-025-Local-Player-Input-Ownership-and-Device-Association.md), [ADR-033](ADRs/IF-ADR-033-Session-Player-Post-Admission-Convergence-and-Contextual-Binding-Authority.md), [ADR-044](ADRs/IF-ADR-044-Consumer-Controlled-Player-Runtime-Gameplay-Availability.md), [ADR-046](ADRs/IF-ADR-046-Stable-Manager-Provisioned-Player-Join-Boundary.md) (Accepted with Amendments 001–003; the bounded Player Join closure is APPROVED Stable; adjacent APIs and Framework-wide readiness remain separate), [ADR-049](ADRs/IF-ADR-049-Public-Actor-Occurrence-Reference-and-Resolution.md) (Accepted; occurrence reference contract remains Experimental).
- Proposed Actor lifecycle and keyed-content architecture: [IF-ADR-050](ADRs/IF-ADR-050-Actor-Occurrence-Lifecycle-Authority-and-Commit.md) remains non-normative. Its [companion specification](Specifications/IF-SPEC-050-Actor-Lifecycle-Participation-and-Composition.md) defines the proposed generic `com.immersive.composition` kernel, Actor identity, guarded replacement, content delta/revision and failure outcomes. The [dated accepted-ADR reconciliation crosswalk](Reconciliation/IF-ADR-050-ACTOR-LIFECYCLE-COMPOSITION-RECONCILIATION-2026-10-17.md) maps historical/current guarantees to proposed changes; **it does not approve them**. Forward-reconciliation notices in ADR-003, ADR-024, ADR-038 and ADR-049 do not supersede their accepted runtime contracts.
- Camera and occurrence assignment: [ADR-038](ADRs/IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md), [ADR-039](ADRs/IF-ADR-039-Session-Camera-Assignment-Command-Boundary.md), [ADR-042](ADRs/IF-ADR-042-Session-Camera-Shared-Group-Assignment-Projection.md), [ADR-043](ADRs/IF-ADR-043-Individual-Player-Camera-Output-Physical-Participation.md).
- Input, Pause and Reset: [ADR-005](ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md), [ADR-035](ADRs/IF-ADR-035-Reset-Composition-Ownership-Membership-and-Targeting.md).
- Persistence and performance: [ADR-017](ADRs/IF-ADR-017-Application-Frame-Rate-Project-Authority.md), [ADR-018](ADRs/IF-ADR-018-Progression-Save-Backend-Independence-and-Persistence-Domain-Boundaries.md).
- Transition observation: [ADR-041](ADRs/IF-ADR-041-Route-Scoped-Activity-Transition-Observation.md).
- Stable release readiness: [ADR-047](ADRs/IF-ADR-047-Initial-Stable-Baseline-and-Versioning-Policy.md) (Accepted baseline policy); [ADR-048](ADRs/IF-ADR-048-Framework-1.1.0-Stable-Readiness-Roadmap.md) (Accepted readiness roadmap; execution and release decision remain pending).

This is a navigation aid, not a replacement for reading the linked decisions. Confirm each decision's status in the tracker; IF-ADR-036 is Accepted and defines the separation between structural GameplayReady and operational RuntimeGameplayAvailable.

## Document maintenance

Keep one canonical guide per consumer workflow and one public API maturity map in [Documentation](../README.md) and [API](../API/Public-API.md). Put package contracts in versioned package documentation; update mutable status only in the tracker. Do not copy release-specific consumer package paths or snapshots into the global discovery skill.
