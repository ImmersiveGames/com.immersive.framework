# IF-ADR-050 D00 — Implementation Baseline and Contract Gates

Status: **Complete — implementation task-gated**
Completed: **2026-10-17**
Framework revision: **`086efe9cd5f5aa3b52d3e0757c4fe2a7c25f617b` (`master`)**
Plan: [IF-PLAN-ADR-050 v1](../Plans/IF-PLAN-ADR-050-ACTOR-COMPOSITION-AND-OCCURRENCE-LIFECYCLE.v1.md)
Authorities: [IF-ADR-050](../ADRs/IF-ADR-050-Actor-Occurrence-Lifecycle-Authority-and-Commit.md), [IF-SPEC-050](../Specifications/IF-SPEC-050-Actor-Lifecycle-Participation-and-Composition.md), [accepted-target migration](IF-ADR-050-ACCEPTED-TARGET-CONTRACT-MIGRATION-2026-10-17.md), [ADR reconciliation](IF-ADR-050-ACTOR-LIFECYCLE-COMPOSITION-RECONCILIATION-2026-10-17.md), [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md).

## 1. Baseline and disposition

- The plan v1 is consistent with the accepted target, M01–M12 migration contract and accepted-ADR crosswalk. No material contradiction or unresolved ownership gate was found. Approval authorizes separately tasked deliverables, not batch execution.
- The checkout was clean at `f93f3c62dea1775f2b892d6812acd92f7ed52968`, four commits behind `origin/master`. It was fast-forwarded to the requested `086efe9...`; the four intervening commits changed documentation only, with no Runtime or test-source changes.
- Framework `package.json`: `com.immersive.framework` `1.2.0-preview.2`, declares Unity `6000.5.0f1`; dependencies are Foundation `0.2.2`, Logging `0.2.3`, Cinemachine `3.1.7` and Input System `1.19.0`. Unity was not launched.
- QAFramework, `planet-devourer` (FirstGame) and PEGA each declare Unity `6000.5.0f1` / editor revision `88b47c5e7076` in `ProjectSettings/ProjectVersion.txt`.
- Consumer package resolutions read from manifests/locks: QAFramework uses the local Framework path; `planet-devourer` uses the Framework Git URL with lock hash `10aeb459df42e27159f375ee6e8c6b154f1ef985`; PEGA uses registry `1.2.0-preview.2`. Git HEAD/status for those external repositories could not be read in this environment (`git -C` returned permission denied); no consumer or QA files were modified.

## 2. Contract inventory M01–M12

| Item | Current source and authority | Current-to-target difference / gate |
|---|---|---|
| **M01 Player participation** | Session/Player owns Join, Leave, Slot, Host and PlayerInput. `PlayerOccurrenceId` represents one joined lifetime; `PlayerSlotId` is the Stable seat identity. | Preserve the same joined Player and Activity participation through Actor change, root replacement and content recomposition. Do not synthesize Leave/Join. |
| **M02 Logical Actor identity** | `ActorId` is declared on `PlayerActorDeclaration`; `AttachedPlayerActorMaterializationAdapter` supplies its current per-materialization identity. `ActorProfileId` is authored profile identity. | Migrate `ActorId` to logical Actor identity, separate from Profile and Player occurrence. Define producer issuance/uniqueness; reconcile IF-ADR-023A without altering serialized declarations implicitly. |
| **M03 Physical incarnation** | `ActorOccurrenceId` is Experimental. `ActorOccurrenceReferenceRuntime.TryGetPlayer` creates it when resolving a physical result and removes the cached record when the physical predicate fails. | Core lifecycle allocates once at successful commit; keep it through temporary inactivity/recomposition, and replace it on a new/reused root. Candidates and aborted operations do not receive public occurrence identity. |
| **M04 Resolver and availability** | `PlayerActorPreparationRuntimeContext.TryGetCurrentPhysicalActorOccurrence` requires joined selection, matching profile/revision, prepared active handle, exact root/declaration and owner. `ActorOccurrenceReferenceRuntime` resolves against that physical source. | Preserve current physical `TryResolve` success meaning. Add a distinct committed-binding/availability observation if needed; invalidation remains owner-driven. |
| **M05 Prepared replacement** | `PlayerActorPreparationRuntimeContext.TryReplacePreparedActor` and `ActivityPlayerActorLifecycleParticipant.PreparedActorReplacement` implement the current Manager-Provisioned path. `PlayerActorMaterializationOperationId`, `PlayerActorPreparationToken` and the public selection/replacement request/result family are Experimental. Selection is changed before B activation. | Keep A committed while preparing B, then perform guarded activation and coherent binding/selection commit. Preserve current V1 interpretation until an explicit contract migration is approved. |
| **M06 Partial handoff** | Current replacement exposes typed preparation/replacement results and cleanup outcomes. | Distinguish pre-deactivation rejection, A committed-but-unavailable after failed handoff, B committed with later readiness/cleanup failure, and actual resource cleanup. Never claim callback rollback from `SetActive`. |
| **M07 Specialized gates** | `UnityPlayerInputGateAdapter`, gameplay admission and readiness are specialized Player-owned paths; no Actor-wide operational gate exists. | Retain Input, gameplay, Pause and transition authority at their current owners. Define gate timing for B; no universal Unity callback suppression guarantee. |
| **M08 Profile visual** | Experimental `ActorProfile.presentationPrefab` is instantiated directly by `AttachedPlayerActorMaterializationAdapter` under `PlayerActorRuntimeHost.VisualContentMount`. Scene-Provided adoption uses a separate path. | Migrate the visual to a Base contribution at key `visual`, using the existing mount and the single Composer path. Preserve null/no-visual and authored references with an explicit idempotent data migration. |
| **M09 Composition and resources** | Framework has no generic keyed Composer. `RuntimeContent` has owner/scope/handle and release-adapter contracts; `RuntimeContentRuntime` is internal and logical release is not physical-destruction proof. | Framework depends on independent `com.immersive.composition`; Composition has no Framework/Actor/Player dependency and does not own Actor roots or acquired resources. Keep current RuntimeContent ownership distinct. |
| **M10 Camera / Subject** | Camera assignment and Actor root/visual are separate. Camera integration and Actor occurrence references are Framework-owned; Camera membership uses `PlayerOccurrenceId`. | Preserve Player/Camera assignment across root changes. Reproject Subject after coherent occurrence commit; content-only changes leave root/occurrence stable. Composition does not own Camera assignment. |
| **M11 External/adopted Actors** | Scene-Provided Player adoption is in `PlayerActorPreparationRuntimeContext.ScenePlayerActorAdoption`; NonPlayer publication is explicit and owner-scoped through `ActorOccurrencePublication`. | Producer keeps root ownership. Composer adopts authored/foreign parts with provenance and must not recreate or release them. Scene-Provided guarantees remain weaker after callbacks have run. |
| **M12 Release and diagnostics** | `AttachedPlayerActorMaterializationAdapter.TryReleaseMaterialization` deactivates the host, detaches visuals/roots and schedules destruction, then asks RuntimeContent for logical release. | Fix the active-visual detach/release path in D01. Track request, logical completion, pending cleanup and failure separately; physical root release remains producer-owned. |

## 3. Maturity, serialization and consumer compatibility

Maturity is assigned per contract using `[FrameworkApiStatus]` and IF-GOV-001; public visibility does not imply Stable status.

| Classification | Affected surfaces / disposition |
|---|---|
| **Stable** | The bounded IF-ADR-046 Join closure: `PlayerSessionJoinTrigger`, `LocalPlayerProvisioningConsumerScope`, `LocalPlayerJoinRequest`, `LocalPlayerJoinOutcome`, `LocalPlayerJoinOutcomeCode` and `LocalPlayerJoinAdmissionState`; `PlayerSlotId`; `GameApplicationAsset`, `RouteAsset` and `ActivityAsset` authoring schemas. Preserve these contracts and Stable serialized intent from baseline `1.1.0`, including serialized fields whose configured behavior is Experimental. |
| **Experimental** | `ActorId`, `ActorProfileId`, `ActorProfile`, `PlayerOccurrenceId`, `ActorOccurrence`/`ActorOccurrenceId`, occurrence reference/publication, `PlayerActorMaterializationOperationId`, `PlayerActorPreparationToken`, other Player preparation/replacement contracts, `RuntimeContentOwner`, cancellation/release contracts, and the Player Actor host. No automatic Stable promotion follows from this plan. |
| **Internal** | `AttachedPlayerActorMaterializationAdapter`, `PlayerActorMaterializationHandle`, Player preparation runtime context/modules, `ActorOccurrenceReferenceRuntime`, and Camera integration runtime. These remain implementation details. |

Compatibility gates:

- IF-GOV-001 §2 protects serialized `GameApplicationAsset`, `RouteAsset` and `ActivityAsset` data from the Stable `1.1.0` baseline. Route spatial-entry and transition-gate fields remain Stable schema even where configured behavior is Experimental. Any touched serialized intent requires an explicit migration and compatibility decision.
- Preserve exactly the bounded Stable Join closure listed above. IF-ADR-046 does not promote Session/Slot/Actor/Leave participation generally, broad scoped access, readiness, or the Framework.
- `ActorProfile` itself is Experimental, but its serialized `presentationPrefab`, authored `ActorProfileId`, null values and asset references are active migration inputs. `planet-devourer` has serialized Farmer/Cow profiles; PEGA has serialized Rick Ronda/Petra Patrol profiles; QAFramework fixtures include serialized profile assets. Preserve references/GUIDs and report each migrated asset; do not infer that Experimental means unused.
- The current `ActorId` materialization semantics, IF-ADR-049 physical resolver semantics and IF-ADR-024 V1 result interpretation remain in force until their individual migrations are implemented and validated. `PlayerSlotId` and Stable authoring schemas require explicit compatibility protection.
- The Framework declares no Composition dependency at this baseline. Target direction is `com.immersive.framework → com.immersive.composition`; the generic package must not reference Framework. D00 creates neither package nor dependency.

## 4. Integration points and reusable evidence

| Boundary | Current implementation evidence |
|---|---|
| Identity and preparation | `Runtime/Actors/ActorId.cs`, `ActorProfileId.cs`; `Runtime/PlayerParticipation/Contracts/PlayerOccurrenceId.cs`, `PlayerActorMaterializationOperationId.cs`, `PlayerActorPreparationToken.cs`; `Runtime/PlayerParticipation/Runtime/PlayerActorPreparationRuntimeContext.cs` and `.PhysicalOccurrence.cs`. |
| Occurrence publication/resolution | `Runtime/Actors/References/ActorOccurrence.cs`, `ActorOccurrenceReferenceRuntime.cs`, `ActorOccurrenceReference.cs`, `ActorOccurrencePublication.cs`. Current ID issuance is resolver/publication-time, not occurrence-commit-time. |
| Visual and adoption | `Runtime/Actors/ActorProfile.cs`; `Runtime/PlayerParticipation/Authoring/PlayerActorRuntimeHost.cs`; `AttachedPlayerActorMaterializationAdapter.cs`; `PlayerActorMaterializationHandle.cs`; `PlayerActorPreparationRuntimeContext.ScenePlayerActorAdoption.cs`. These are the single existing Manager-Provisioned visual path and the separate adopted Scene path to converge. |
| Route and Camera | `PlayerActorPreparationRuntimeHostModule.RouteSpatialEntry.cs` / `RoutePlayerSpatialEntryRuntime`; `Runtime/Camera/Assignments/` and `Runtime/Camera/Integration/PlayerCameraOutputIntegrationRuntime.cs`. Route placement and Camera membership stay with their existing Framework owners. |
| Resource release | `Runtime/RuntimeContent/RuntimeContentRuntime.cs`, `RuntimeScopeCancellationToken.cs`, `IRuntimeMaterializationAdapter.cs`, `IRuntimeReleaseAdapter.cs`, and the attached Player adapter above. Cancellation token is passive; logical release is not physical destruction confirmation. |

Reusable evidence (scope retained; no tests were rerun):

- `Tests/Editor/ActorReferences/ActorOccurrenceReferenceRuntimeTests.cs`; tracker IF-ADR-049 records Project Owner-reported **11/11** at source `d588e1c...` / `v1.2.0-preview.2`. This supports the existing Experimental selector/resolver behavior, not commit-time identity or availability retention.
- `Tests/Editor/Camera/Assignments/ActorOccurrenceOptionalVisualContentTests.cs`, `ActorCameraSubjectAuthoringTests.cs`, `PlayerCameraOutputIntegrationTests.cs` and `SessionCameraAssignmentRuntimeTests.cs` cover adjacent existing visual/Camera boundaries; future Subject reprojection remains a changed property.
- Tracker IF-ADR-024 records historical Manager-Provisioned `actor-replace` evidence (**16/16**); current replacement recertification remains open. Reuse only for the existing V1 scope, not the new handoff contract.
- QAFramework Actor/Player fixtures, FirstGame ActorProfile assets and PEGA ActorProfile assets are serialized-consumer evidence. Their manifest/lock resolutions are recorded above; their Git metadata was inaccessible here.

## 5. Deliverable dependencies, future proof and blockers

| Deliverables | Dependency and targeted proof to inventory for their own task |
|---|---|
| **D01** | Independent release-safety cut. Prove no release-induced visual reactivation, exact acquired/adopted ownership and observable cleanup failure/pending state (N1). |
| **D02** | Identity/availability cut, independently investigable and prerequisite to D06. Prove stable ID across temporary inactive/active and re-resolution; new ID for replaced/reused root; aborted/stale owner-operation never publishes B (N4). |
| **D03 → D04 → D05** | Composition package precedes Framework visual integration; keyed recomposition follows that integration. Prove independent kernel, ownership/precedence/dependencies/stale completion, one visual materialization path, idempotent serialized migration, and content delta with stable root/Actor identity. |
| **D06** | Requires D02; candidate composition relies on D05 unless the task proves the same unified content contract. Target N2/N3/N6: selection publication only after coherent commit, no stale revision after failure, reentrancy/forced close, bounded callback ordering and typed degraded outcomes. |
| **D07 → D08** | Camera, Scene-Provided and NonPlayer convergence follows the lifecycle/content contracts; prove stable Player Camera assignment with Subject reprojection, adopted-part non-duplication/non-destruction, exact external owner release, then package/consumer integration and migration evidence. |

Focused verification to add or extend in the owning test suites:

- **D01:** release while host/root is inactive must never reactivate a detached visual; deferred destruction, cancellation, owned release, adopted-resource preservation and observable cleanup failure each retain their own outcomes.
- **D02:** temporary deactivation plus repeated resolution retains the committed `ActorOccurrenceId`; root replacement/reuse gets a new ID; aborted or stale operations publish no ID or availability.
- **D03–D05:** Composition builds without Framework references; owner/precedence/dependency and stale-completion cases are deterministic; keyed base/override/replace recomposition is idempotent, changes content without changing root/Actor identity, and migrates profile assets without duplicate visual materialization.
- **D06:** selection/occurrence publication follows coherent B activation; failed handoff leaves no stale revision; reentrant replacement and forced close produce bounded callback order and a typed degraded result without claiming Unity callback rollback.
- **D07–D08:** Camera assignment remains on the same Player while Subject follows the committed root; Scene-Provided objects are neither cloned nor destroyed; NonPlayer publication releases only through its owner; serialized consumer migration is idempotent and preserves GUIDs/null intent.

Extend the existing focused Actor Reference, Camera, Player Participation and consumer migration evidence at each owning boundary; D00 did not add or execute tests.

No material D00 blocker remains. Residual limits are recorded rather than hidden: external consumer Git HEADs/status could not be inspected because `git -C` returned permission denied; no Unity/test/QA execution was authorized or performed; the separate Composition package/version does not yet exist and belongs to D03. These limits do not make ownership or Stable-schema scope ambiguous for this documentation gate.

**D00 exit:** complete. **Next eligible:** D01, as a separately issued task. D02 may be prepared independently; neither is started by this record. No Runtime/API, asset, dependency, consumer, QAFramework or PEGA file changed.
