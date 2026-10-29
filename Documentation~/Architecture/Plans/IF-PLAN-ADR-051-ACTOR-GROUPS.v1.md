# IF-PLAN-ADR-051 — Actor Groups

Status: **Approved — implementation task-gated; immutable**  
Version: **v1**  
Date: **2026-10-28**  
Supersedes: **none**  
Superseded by: **none**  
Architecture authority: [IF-ADR-051 — Actor Groups](../ADRs/IF-ADR-051-Actor-Groups-Identity-Membership-and-Player-Slot-Associations.md)  
Identity compatibility: [IF-ADR-047 — Stable baseline and versioning](../ADRs/IF-ADR-047-Initial-Stable-Baseline-and-Versioning-Policy.md)  
API governance: [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)  
Actor lifecycle dependency: [IF-ADR-050](../ADRs/IF-ADR-050-Actor-Occurrence-Lifecycle-Authority-and-Commit.md) and [its implementation plan](IF-PLAN-ADR-050-ACTOR-COMPOSITION-AND-OCCURRENCE-LIFECYCLE.v1.md)  
Execution status: [IF-TRACK-Framework](../Tracking/IF-TRACK-Framework.md)

**Goal:** Add generic Group identity, optional reusable Profiles, and mutable many-to-many membership by logical `ActorId` in Framework Core.

**Architecture:** A Group Definition asset supplies the canonical `GroupId`; that same value identifies its live runtime Group record. A separate optional Group Profile supplies reusable game-neutral configuration. One Session-scoped Core authority owns each runtime Group's registration, membership and exact lifecycle-owner binding; member presence does not control Group lifetime.

**Tech Stack:** Unity 6.5 / `6000.5`, C#, the existing `com.immersive.framework` package, ScriptableObject authoring, and the package's current Unity Editor/NUnit test assemblies. No new package or technical dependency.

**Spec:** [IF-ADR-051](../ADRs/IF-ADR-051-Actor-Groups-Identity-Membership-and-Player-Slot-Associations.md) is normative. Apply [IF-ADR-050](../ADRs/IF-ADR-050-Actor-Occurrence-Lifecycle-Authority-and-Commit.md) for logical Actor identity continuity, [IF-ADR-047](../ADRs/IF-ADR-047-Initial-Stable-Baseline-and-Versioning-Policy.md) for Stable enum/version compatibility, and [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md) for API maturity and evidence.

## Global constraints

- `GroupId` and `GroupProfileId` remain separate typed identities; append only the accepted domain values 260 and 270, preserving every existing Stable value.
- Membership is keyed by `ActorId`, never `ActorOccurrenceId`; no physical occurrence is required.
- Group identity, Profile reference, membership and lifecycle owner remain independent. A Profile or member does not create a Group.
- Membership is many-to-many and mutable; game-specific admission, permanence and capability rules remain consumer-owned.
- Groups may be empty for the full lifetime of their exact owner. Owner release, not member count, removes a Group.
- Do not add Player Slot↔Group relations, GOAP, gameplay capabilities, a generic data bag, a global allocator, singleton or service locator.
- New Group contracts are Experimental. The Stable identity-domain extension follows IF-ADR-047's additive MINOR rule.

## Review Focus

| Input or failure mode | Expected behavior | Owning gate |
|---|---|---|
| Definition asset is loaded, enabled, or reused | No runtime Group is created implicitly; explicit creation uses the definition's sole `GroupId`. A second live creation in the same Session context rejects without changing the first Group. | G01, G02 |
| Distinct Groups share one Profile | Their `GroupId`, owner, membership and mutable state remain independent; the Profile is read-only runtime configuration. | G01, G02 |
| Group has no members or its last member leaves | It remains queryable until its exact lifecycle owner closes. | G02, G03 |
| Owner closes while other owners still have Groups | Only Groups registered to the exact closing owner and their edges are removed; Session, Route and Activity owner hooks release once. | G02 |
| Actor has no occurrence, or its logical identity changes | Valid `ActorId` membership works without a physical Actor; a preserved ID retains its edge, while a new logical ID receives no implicit membership transfer. | G03 |

---

## 1. Purpose and boundary

Define the implementation route for the accepted IF-ADR-051 core: independently identified Groups, optional reusable Profiles, logical Actor membership, and lifecycle owned by an explicit Framework runtime scope. The feature belongs to Framework Core in `com.immersive.framework`; it does not warrant another technical package.

The route does not implement or authorize Runtime changes by itself. Each deliverable remains a separately issued and reviewed task. The plan covers Framework source, its tests, the public API inventory, and one canonical user guide when the new authoring/runtime surface exists.

## 2. Reconciled contracts and dependencies

| Concern | Evidence in the current Framework | Consequence for this route |
|---|---|---|
| Logical Actor identity | [`ActorId`](../../../Runtime/Actors/ActorId.cs) is a typed Experimental identity in domain `Actor`. [`ActorDeclaration`](../../../Runtime/Actors/ActorDeclaration.cs) carries an explicit logical ID. Physical `ActorOccurrenceId` is separate. | Membership keys only `ActorId`; it is valid without a current occurrence. Group code must not subscribe to materialization as a prerequisite for membership. |
| Replacement continuity | IF-ADR-050 D02 is recorded complete in the tracker. [`PlayerActorPreparationRuntimeContext.TryResolveReplacementActorId`](../../../Runtime/PlayerParticipation/Runtime/PlayerActorPreparationRuntimeContext.cs) retains the current `ActorId` for `PreserveLogicalActor` and issues a different ID for `StartNewLogicalActor`. | The required continuity dependency is satisfied for the implemented Player replacement contract. The feature stores edges by logical ID: preserved ID keeps membership; a new ID receives no implicit transfer. IF-ADR-050 D08’s Actor Profile migration is independent of Group membership. |
| Identity domains | [`FrameworkIdentityDomain`](../../../Runtime/Identity/FrameworkIdentityDomain.cs) is Stable. The current conformance test pins its names and numeric values. IF-ADR-051 reserves append-only values `Group = 260` and `GroupProfile = 270`. | Preserve every existing value; add only the two reserved entries. Update the conformance fixture and public API inventory. Review consumers for exhaustive/unknown-domain assumptions. This is an additive Stable identity-contract change under IF-ADR-047 and must ship under its compatible MINOR version rule; new Group contracts themselves remain Experimental. |
| Authoring precedent | [`ActorProfile`](../../../Runtime/Actors/ActorProfile.cs) is a reusable asset with its own identity and configuration, not a runtime Actor. [`SessionCameraAssignmentAsset`](../../../Runtime/CameraAuthoring/SessionCameraAssignmentAsset.cs) builds an immutable runtime definition from authoring data. | Keep Group identity, reusable Group Profile, and runtime Group instance separate. Do not reinterpret `ActorProfile` or use a Profile as the Group identity. |
| Owner and scope lifecycle | [`RuntimeContentOwner`](../../../Runtime/RuntimeContent/RuntimeContentOwner.cs) is passive owner identity, not a registry or release service. Existing Actor publication cleanup is called from [`FrameworkRuntimeHost`](../../../Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs), [`RouteLifecycleRuntime`](../../../Runtime/RouteLifecycle/RouteLifecycleRuntime.ResettableRegistration.cs), and [`ActivityFlowRuntime`](../../../Runtime/ActivityFlow/ActivityFlowRuntime.ResettableRegistration.cs) at Session, Route and Activity owner closure. | These exact close boundaries make Session, Route and Activity feasible initial Group owner scopes. A Group registry remains missing. Reuse `RuntimeContentOwner` only as an ownership key; implement Group registration and release at these Framework authorities. Passing `RuntimeContentOwner` alone does not satisfy lifecycle. |
| Current Group APIs | No Group identity, Profile, registry, membership, authoring asset, or Group access port exists. `ActorSet` is validation output; camera `SharedGroup` remains a camera assignment mode. | Add the feature as a new Core capability. Do not repurpose those existing concepts. |

IF-ADR-050’s Group-relevant dependency is the completed D02 logical identity cut, not the Actor Profile migration or the broader composition work. At execution entry, record the current Framework revision and recheck the D02 source/status; do not freeze this plan to an old source hash.

## 3. Implementation contract carried by this plan

These choices translate IF-ADR-051 into a bounded authoring/runtime route. They do not add gameplay semantics:

- Add typed `GroupId` and `GroupProfileId` values in the Actors domain, backed by the reserved `FrameworkIdentityDomain` entries. Both new contracts start Experimental. Existing identity domains and serialized values remain unchanged.
- Keep three concepts distinct. An authored Group Definition is source data, not a live Group and not a lifecycle owner. Its explicit `GroupId` is the sole collective identity; the runtime Group record is created with that same `GroupId`, not a second definition or instance ID. The optional reusable Group Profile has its own `GroupProfileId`; it supplies only framework-owned, game-neutral defaults/presentation. Neither asset stores membership, runtime state, lifecycle ownership, knowledge, GOAP data, instructions, or modifiers. Reusing a Profile never merges Groups. Loading/enabling a definition does not create a runtime Group.
- Runtime-created Groups may be supplied an explicit `GroupId` and optional Profile through the same creation authority. IDs are never inferred from asset names, Profile IDs, Actors, membership, Slots, or occurrences; no global allocator or singleton is introduced. A duplicate live `GroupId` in one Session runtime context is rejected. The same authored definition may be materialized in separate Framework runtime contexts with independent runtime state.
- Framework Core owns one Group authority per active `FrameworkRuntimeHost` Session runtime context and an explicit consumer access path. It must be resolved through that Framework runtime scope, not a static lookup and not by extending a Player-only access contract solely because some members may be Players.
- Bind each Group to exactly one lifecycle owner for its lifetime. One owner may own multiple Groups; ownership does not participate in Group identity or membership and is not transferable during this initial cut. Use the existing `RuntimeContentOwner` only as the exact owner key, never as Group identity or lifecycle authority. The Group registry is responsible for registering Groups and removing them when the actual Session, Route or Activity owner closes. These are the supported initial scopes; Transient and unknown scopes reject because this route does not establish an exact close authority for them. Releasing an owner removes its Groups and their membership edges; losing the final member does not. Empty Groups remain valid until owner release.
- Store membership as a many-to-many relation between `GroupId` and `ActorId`. Join/leave changes are explicit. Membership does not require a materialized Actor or Player Slot, and the Framework does not infer, authorize, restrict, or transfer membership when gameplay changes an Actor’s logical identity.
- Do not persist Group state across process/session restart in this cut. Do not add Player Slot↔Group associations, capability registries, generic data bags, or consumer gameplay integrations.

The first contract cut finalizes public type/member names and validates the authored definition/Profile UX. Those are bounded API-design details, not unresolved architecture. The owner-close source paths are already present and identified above; G02 must wire Group release into those exact authorities, not add another lifecycle abstraction. If implementation evidence disproves one of those concrete paths, stop that task and submit a plan revision; do not hide the gap behind polling, a global service, or `RuntimeContentOwner` alone.

## 4. Ordered deliverables

Each deliverable is a separate task. Do not advance automatically after a blocked or failed gate.

### Entry preflight — baseline and IF-ADR-050 dependency check (not an implementation gate)

**Repository:** Framework source and tracker only.  
**Work:** Record exact Framework revision and package manifest at execution entry. Confirm D02 is still complete and source preserves logical `ActorId` under `PreserveLogicalActor`; confirm a logical switch changes identity. Record other package/QA resolutions only if the scoped task actually depends on them.

**Acceptance:** evidence agrees across current source and tracker; no claim depends on an unverified Actor occurrence or profile migration. This is a precondition, not a reason to repeat the entire IF-ADR-050 program. IF-ADR-050 D08 is closed at its recorded scope and is not a dependency of Group membership.

### G01 — Identity, authoring and compatibility surface

**Target:** `Runtime/Identity/FrameworkIdentityDomain.cs`; new `Runtime/Actors/` identity and authoring files; `Tests/Editor/Identity/FrameworkIdentityConformanceTests.cs`; new `Tests/Editor/Actors/ActorGroupAuthoringTests.cs`; relevant API status metadata coverage; and `Documentation~/API/Public-API.md`.

**Work:** Add the two reserved typed identity wrappers and domain values. Add separately authored Group Definition and optional Group Profile assets. Validate explicit IDs and profile references; do not auto-generate IDs or serialize members. Keep the profile reusable and runtime-read-only; keep its initial data game-neutral. Mark new Group contracts Experimental.

**Proof and acceptance:** update the existing domain conformance assertions to show existing names/values remain unchanged and new values are exactly 260 and 270; verify typed values reject invalid defaults and compare only within their domains; add authoring tests for missing/invalid IDs, invalid optional Profile references, and two definitions sharing one Profile while retaining distinct Group IDs. The authored asset is not a live Group and introduces no second Group identity. Update the Stable API inventory and classify the additive domain extension under IF-ADR-047. No Stable type is silently promoted or broken.

### G02 — Group creation, explicit access and owner lifecycle

**Target:** a new Core-owned Actors runtime authority in `Runtime/Actors/`, its explicit Framework runtime access binding, and new `Tests/Editor/Actors/ActorGroupRuntimeTests.cs`; lifecycle integration at the existing Session, Route and Activity owner-close paths.

**Work:** Provide one creation path for authored definitions and one for explicitly supplied dynamic identities, backed by the same runtime authority. Each runtime record uses the definition's `GroupId` (or the explicit dynamic `GroupId`) as its sole Group identity. Associate each live Group with exactly one immutable lifecycle owner; one owner may own multiple Groups. Reject invalid identity, duplicate live Group identity, mismatched owner, and unsupported owner scope with observable outcomes. On exact owner release, remove only that owner’s Groups and their edges. Do not remove a Group when it becomes empty. The registry performs lifecycle release; `RuntimeContentOwner` supplies only owner identity/scope evidence.

**Proof and acceptance:** `CreateGroup_FromDefinition_UsesDefinitionGroupIdWithoutInstantiatingOnAssetLoad` covers definition/runtime separation; `DuplicateLiveGroupId_IsRejectedAcrossOwners` covers identity collision; `CloseOwner_ReleasesOnlyGroupsOwnedByExactOwner` covers ownership and cleanup; and a Group created with no members remains registered. Each supported scope has evidence that its actual lifecycle-close path releases Groups exactly once; unsupported scopes fail explicitly. No global lookup is used.

### G03 — Many-to-many logical Actor membership

**Target:** the G02 Group authority and new `Tests/Editor/Actors/ActorGroupMembershipTests.cs`.

**Work:** Add explicit join, leave, and membership query operations over `GroupId`↔`ActorId`. Provide a bounded read-only snapshot/query surface for consumers. Keep membership independent from occurrence publication, current physical resolution, Player Slot, Group Profile, and gameplay policy.

**Proof and acceptance:** `Membership_IsManyToManyByActorIdWithoutOccurrence` covers one Actor in multiple Groups, multiple Actors in one Group, and membership before physical publication; `LeavingOneGroup_PreservesOtherMemberships` covers independent edges; `Group_RemainsLiveAfterLastMemberLeaves` covers owner-governed empty-Group lifetime; and `PreservedActorIdRetainsMembership_NewActorIdDoesNotInherit` covers the accepted logical identity behavior. Owner release clears only its Groups. Reuse D02 evidence for replacement mechanics rather than duplicating lifecycle implementation in the Group module.

### G04 — Product/API closure and focused Framework validation

**Target:** `Documentation~/API/Public-API.md`, one canonical `Documentation~/Guides/Actor-Groups.md` guide, Framework Editor tests, and the Framework tracker.

**Work:** Document how to author a Group Definition/Profile, create a runtime Group under a supported owner, change membership, query membership, and understand owner teardown. Record the Experimental boundary, supported scopes, and non-goals. Run the focused Framework EditMode suite in the owner’s Unity environment; use only a focused QAFramework scenario if a later separately authorized QA task requires host-level lifecycle evidence. PEGA and Planet Devourer remain unchanged in this route.

**Acceptance:** Framework import/compile and focused tests are confirmed by the owner; test evidence names the tested revision and scope; all owner cleanup/membership failure cases are green; Public-API, guide, status metadata, accepted ADR and source agree. No PASS is claimed from static inspection alone. Update only the mutable tracker with results.

## 5. Non-goals

- Player Slot↔Group association or command/control authority.
- PEGA Gangues, agency rules, shared knowledge, GOAP, or any consumer capability.
- Planet Devourer samples or manufactured adoption evidence.
- Persisting Groups or membership across a Session/process restart.
- Deriving Groups from Actor Kind/Role, actor hierarchy, common Profile, Slots, camera mode, or physical composition.
- Changing IF-ADR-050 identity, replacement, composition, or Actor Profile behavior.
- A new package, global manager, service locator, or generic capability/data-bag mechanism.

## 6. Gates, validation and change policy

The four implementation gates are G01–G04; the entry preflight is a bounded check before G01, not a fifth feature gate. Plan approval allows the preflight task to be issued, but does not authorize a batch execution. After approval, the route is immutable and each G01–G04 task remains separately issued and reviewed. Record progress, source revisions, owner-run Unity results, and blockers only in `IF-TRACK-Framework.md`.

The Framework repository instructions prohibit agent-run Unity build, import, Play Mode, smoke, and batchmode. Static documentation/source checks can support review but cannot close runtime gates. Required manual evidence is the focused Unity import/compile and EditMode run in G04; any broader host integration smoke must be separately scoped to its owning QA repository.

If a cut requires changing a Stable guarantee, reassigning a package/module owner, widening Group semantics, or adding a lifecycle abstraction not supported by the accepted ADR, stop and create a revised ADR/plan through the official process. Otherwise preserve this accepted architecture and resolve exact method/type names within the authorized deliverable. A materially revised route becomes v2; do not rewrite an approved v1 body to reflect execution.
