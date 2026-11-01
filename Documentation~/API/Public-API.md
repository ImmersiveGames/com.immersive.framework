# Public API Reference

This is a curated map of supported consumer surfaces, not a list of every public C# type. Follow the linked guide for normal usage and check each row’s maturity before depending on it.

## Compatibility baseline

Framework `1.1.0` is the first official Stable baseline. Compatibility
guarantees begin at 1.1.0 and apply to each contract classified Stable under
[IF-GOV-001](../Architecture/Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)
and [IF-ADR-047](../Architecture/ADRs/IF-ADR-047-Initial-Stable-Baseline-and-Versioning-Policy.md).
Earlier releases, including 1.0.2 and `1.1.0-preview.*`, remain historical and
do not establish a compatibility or migration obligation to this baseline.
This baseline does not change the per-type categories below or promote an
Experimental or Internal contract.

## Maturity and categories

The package uses type-level `FrameworkApiStatusAttribute` metadata to classify types as `Stable`, `Experimental`, `Internal`, `Deferred`, `Removed` or `DevelopmentTooling`. A Stable type can still have a scope limitation, and one authoring workflow may combine types of different maturity. See [API maturity governance](../Architecture/Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md).

## API maturity metadata

| Surface | Category / maturity | Purpose |
|---|---|---|
| `FrameworkApiStatus`, `FrameworkApiStatusAttribute` | Governance metadata · Stable | Define the canonical maturity categories and type-level declaration metadata, including `FrameworkApiStatusAttribute.Status` and `Note`. They do not create Runtime authority, define a feature's full contract scope, or promote dependent/annotated contracts. Changes to category meanings are governed by IF-GOV-001. |

The canonical categories are:

| Category | Meaning for consumers |
|---|---|
| Stable | Supported consumer contract; breaking changes require an explicit architecture and migration decision. |
| Experimental | Available for controlled development without compatibility guarantees. |
| Internal | Implementation detail; do not build game code against it. |
| Deferred | Publicly visible or reserved, but not an available consumer contract. |
| Removed | Retained only as a migration/history marker; do not use. |
| Development Tooling | Editor convenience or diagnostics, not runtime/gameplay authority. |

A public type without an explicit consumer status is not automatically a recommended direct API. Prefer authored components/assets and the guide’s stated usage path.

## Application

| Surface | Category / maturity | Purpose |
|---|---|---|
| `GameApplicationAsset` | Authoring asset · Stable | Application root: Startup Route, Session policies, Persistent Content and application-scoped features. |
| `PersistentContentComposition` and Persistent Content Scene Template | Authoring configuration/template · Stable workflow | Explicit scene composition retained for application lifetime. The template does not create or register the consumer scene automatically. |
| `FrameworkValidationMode`, `FrameworkValidationModePolicy` | Authoring validation policy · Stable | Defines diagnostic strictness for `Strict`, `Standard`, `Release` and unknown values; required configuration fails in every mode. [IF-GOV-001](../Architecture/Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md) owns the policy semantics; [IF-ADR-010](../Architecture/ADRs/IF-ADR-010-Editor-and-Inspector-Product-Surface-Authority.md) owns the product diagnostics surface. |
| `FrameworkEditorPlayModeStartup` | Editor startup setting · Stable | Selects Framework startup or current-scene-only startup while entering Play Mode in the Editor; Player/runtime builds use Framework startup. See [IF-ADR-001](../Architecture/ADRs/IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md), which links the current startup-isolation reconciliation. |

Guide: [Getting Started](../Guides/Getting-Started.md), [Persistent Content Scene Template](../Guides/Persistent-Content-Scene-Template.md).

## Boundary identities

| Surface | Category / maturity | Purpose |
|---|---|---|
| `IFrameworkIdentity`, `FrameworkIdentityDomain`, `FrameworkIdentityKey`, `FrameworkIdentityValue`, `RouteId`, `ActivityId`, `PlayerSlotId` | Boundary identity primitives · Stable | Preserve typed identity domains and value validation; erased comparisons use domain plus value. `Group = 260` extends the Stable domain vocabulary under IF-ADR-047's MINOR rule; Group contracts remain Experimental. Identity contracts do not replace exact authored asset references or lifecycle/occurrence authority. See [IF-ADR-014](../Architecture/ADRs/IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md) and [IF-ADR-047](../Architecture/ADRs/IF-ADR-047-Initial-Stable-Baseline-and-Versioning-Policy.md). |

`RouteId`, `ActivityId` and `PlayerSlotId` are domain-specific identities;
they are not interchangeable across domains. Authored identity and stable
addressing rules are owned by [IF-ADR-014](../Architecture/ADRs/IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md)
and the feature ADRs that consume them.

## Object Entry Occurrence Queries

| Surface | Category / maturity | Purpose |
|---|---|---|
| `ObjectEntryId`, `ObjectEntryDeclaration`, `StableObjectReference` | Logical Object Entry addressing · Experimental | Identifies authored Object Entries and optionally selects one exact Route or Activity definition. The reference carries logical identity, not a retained physical object. |
| `IObjectEntryOccurrenceQuery`, `ObjectEntryOccurrenceQueryBinding`, `ObjectEntryOccurrenceResolution`, `ObjectEntryOccurrenceResolutionStatus` | Scoped occurrence query · Experimental | `ObjectEntryOccurrenceQueryBinding` is bound when its containing Route/Activity owner commits its materialized roots and unbound on rollback/release or Framework host teardown. Hand its interface to plain C# consumers explicitly. `Resolve(StableObjectReference)` reads the existing committed binding authority and returns `Resolved`, `Unavailable`, `InvalidReference` or `Ambiguous`. It does not create objects, register bindings, mutate query state or grant gameplay authorization. Results are point-in-time evidence; use them when needed instead of retaining the GameObject as identity. See [Object Entry Occurrence Queries](../Guides/Object-Entry-Occurrence-Queries.md). |

The query anchor owner defines access lifetime only; the `StableObjectReference`
selector decides which committed occurrence is addressed. An unscoped reference
is ambiguous when multiple live occurrences match. A destroyed Unity object is
unavailable even while its content owner remains committed.

Guide: [Object Entry Occurrence Queries](../Guides/Object-Entry-Occurrence-Queries.md).

## Application Frame Rate

| Surface | Category / maturity | Purpose |
|---|---|---|
| `ImmersiveFrameworkSettingsAsset` | Project Settings asset · Stable | Owns framework-level project settings. |
| `ApplicationFrameRatePolicy` | Project Settings policy · Experimental | Declares requested application frame-rate behavior within platform limits; it is not per-application configuration. |

Guide: [Application Frame Rate](../Guides/Application-Frame-Rate-Usage.md). Check the tracker for current validation scope.

## Game Flow, Route and Activity

| Surface | Category / maturity | Purpose |
|---|---|---|
| `RouteAsset`, `ActivityAsset` | Authoring assets · Stable | From the 1.1.0 baseline, Stable authoring includes preservation and interpretation of the existing serialized schema and stored intent. `RoutePlayerSpatialEntryPolicy` and `TransitionGateMode` remain Experimental behaviors despite being stored by these assets. Activity Player projection/readiness remain Experimental under [IF-ADR-012](../Architecture/ADRs/IF-ADR-012-Activity-Player-Participation-Profile-and-Readiness-Compatibility.md); Route spatial entry and Activity relocation remain Experimental under [IF-ADR-021](../Architecture/ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md). Stored fields do not promote those behaviors or require compatibility with pre-1.1.0 assets. |
| `RouteContentProfileAsset`, `ActivityContentProfileAsset` | Profiles · Stable | Declare content owned by their Route or Activity scope. |
| `RouteRequestTrigger`, `ActivityRequestTrigger` | Request components · Stable | Request a Route, request an Activity or clear the Activity from an authored scene/UI event; they do not own navigation authority. |
| `FlowRequestOutcome`, `FlowRequestEventPhase` | Request event vocabulary · Stable | Coarse public outcome and event phase only; detailed framework result kinds remain outside this contract. |
| `RouteContentSceneEntry`, `ActivityContentSceneEntry`, `ActivityContentSceneLoadMode`, `ActivityContentReleasePolicy`, `ActivityVisualTransitionMode` | Serialized content declarations · Stable | Stable declarations and stored intent; their presence does not independently guarantee that every declared scene or transition behavior executes. Runtime ownership and execution remain governed by [IF-ADR-006](../Architecture/ADRs/IF-ADR-006-Loading-Transition-Persistence-and-Diagnostics.md), [IF-ADR-007](../Architecture/ADRs/IF-ADR-007-Activity-Entry-Readiness-and-Reveal-Gating.md), [IF-ADR-008](../Architecture/ADRs/IF-ADR-008-Persistent-Application-Content-Composition.md) and [IF-ADR-011](../Architecture/ADRs/IF-ADR-011-Participant-Aware-Activity-Readiness-Loading-Progress.md). |
| `RouteRequestTriggerUnityEventBridge`, `ActivityRequestTriggerUnityEventBridge`, `RouteRequestTriggerEvent`, `ActivityRequestTriggerEvent` | Request event adapters/payloads · Stable | UnityEvent delivery and request event data for the corresponding triggers; they do not own navigation authority. |
| `RouteActivityTransitionContext`, `IRouteActivityTransitionObserver` | Lifecycle observer · Experimental | Read-only, Route-scoped observation of one committed Activity transition, including previous/current Activity, source and reason. It cannot request or control Activity Flow. |

Guide: [Game Flow](../Guides/Game-Flow.md); [Activity Readiness](../Guides/Activity-Readiness.md).

## Activity Readiness

| Surface | Category / maturity | Purpose |
|---|---|---|
| `ActivityReadinessParticipant` | Authoring component · Experimental | Contributes one Required or Optional preparation condition to an Activity readiness occurrence. |
| `ActivityReadinessEvents` | Observer component · Experimental | Presents Preparing, Ready and Not Ready events; it observes readiness and does not decide it. |

Guide: [Activity Readiness](../Guides/Activity-Readiness.md).

## Loading and Transition Presentation

| Surface | Category / maturity | Purpose |
|---|---|---|
| `LoadingSurfacePolicy`, `ILoadingSurfaceAdapter`, `ILoadingSurfaceProgressPresentationAdapter`, `IAsyncLoadingSurfaceAdapter`, `UnityLoadingSurfaceAdapter` | Loading policy/presentation adapters · Experimental | Compose an explicit persistent loading presentation; determinate progress requires a progress-capable adapter. |
| `ITransitionEffectAdapter`, `IAsyncTransitionEffectAdapter`, `UnityFadeCurtainEffectAdapter` | Transition presentation adapters · Experimental | Present covered transitions; these adapters do not own Route or Activity flow. |

Guide: [Game Flow](../Guides/Game-Flow.md), [Activity Readiness](../Guides/Activity-Readiness.md). Wait Covered has explicit transition and persistent surface prerequisites.

## Player Participation and Actor

| Surface | Category / maturity | Purpose |
|---|---|---|
| `SceneProvidedLocalPlayerAuthoring` | Authoring component · Stable | Declares a scene-authored Local Player candidate, Slot/Profile references and admission timing. Its required `ActorProfile` and admission timing type have separate Experimental maturity. |
| `LocalPlayerHostAuthoring` | Authoring component · Stable | Explicit technical Host boundary for PlayerInput, ActorMount and Actor Runtime Host configuration. Manager provisioning remains Experimental. |
| `PlayerSlotProfile` | Profile · Stable for Scene-Provided use | Owns a stable Slot ID and presentation metadata; optional default Actor intent is separate from runtime allocation. |
| `ActorProfile` | Profile · Experimental | Reusable Actor identity/classification and ordered references to Base content part assets. The `visual` key uses the Actor root's existing `VisualContentMount`; the profile does not own an Actor occurrence or its spatial state. Existing inline or legacy serialized data requires the explicit part-asset migration before materialization; no Runtime fallback is used. |
| `GroupId` | Identity value · Experimental | Typed identity of one logical Group, independent of `ActorId`, membership and physical occurrences. |
| `GroupProfile` | Group authoring asset · Experimental | The sole asset that authors a Group's `GroupId` and optional game-neutral presentation. Referencing or loading it does not create runtime state. |
| `ActorGroupSnapshot` | Runtime Group view · Experimental | Read-only value exposing a live Group's `GroupId` and optional `GroupProfile` reference. It does not expose membership or own Group lifecycle. |
| `ActorGroupAccessBinding` | Scoped Group access · Experimental | Binds through a committed Session, Route or Activity content owner. Explicitly creates a Group from its `GroupProfile` or from a dynamic `GroupId`, looks up live Groups, and joins, leaves or queries logical `ActorId` membership. Member and Group list queries return read-only copies within a caller-supplied maximum count and fail rather than truncate. Exact owner release unbinds the component and releases Groups created under that owner. See the [Actor Groups guide](../Guides/Actor-Groups.md). |
| `ActorOccurrenceReference`, `ActorOccurrenceReferenceKind`, `ActorOccurrenceId`, `ActorOccurrence`, `ActorOccurrenceResolution`, `ActorOccurrenceResolutionStatus` | Read-only Actor occurrence contract · Experimental | Selects a Player Slot or ActorId, resolves one current physical root with owner/incarnation evidence, and reports invalid, unavailable, stale or ambiguous outcomes. Exact occurrence selectors become stale after replacement or release. |
| `ActorOccurrenceReferenceBinding`, `ActorOccurrenceReferenceChange`, `ActorOccurrenceReferenceChangeKind` | Scene binding and observer · Experimental | Binds an authored Player Slot or Actor declaration to the host-scoped resolver, exposes `Current` / `TryGetCurrent`, and notifies available, replaced, unavailable, invalidated and scope-release changes. C# consumers use the component without inheriting from it. |
| `IActorOccurrenceQuery`, `ActorOccurrenceQueryBinding` | Scoped on-demand query · Experimental | `ActorOccurrenceQueryBinding` is bound by the commit of the Session, Route or Activity content owner whose roots contain it, and unbound on that owner's rollback/release or at Framework host teardown. Hand its `IActorOccurrenceQuery` to plain C# consumers explicitly. `Resolve(ActorOccurrenceReference)` reads committed state once with the same resolver as the binding. It never throws, dispatches no notification and mutates no binding. Unbound queries return `Unavailable`. The owner defines lifetime only: it does not filter what can be resolved and grants no gameplay authorization. For exact selectors, `Stale` means not physically available now, not end of life. Use it per event or decision, not per frame. See [Query on demand](../Guides/Actor-Occurrence-References.md#query-on-demand). |
| `ActorOccurrencePublication`, `ActorOccurrencePublicationScope`, `ActorOccurrencePublicationToken` | Explicit NonPlayer publication · Experimental | A publication marker must sit on the same GameObject as its NonPlayer `ActorDeclaration`. Owner-registered static Route, Activity and Persistent Content roots open contributions directly on `ActorOccurrencePublication`; dynamic producers use an explicitly composed Scope and exact token. |
| `PlayerActorCompositionAccess`, `IPlayerActorCompositionAccess`, `PlayerActorCompositionContributionRequest`, `PlayerActorCompositionContribution`, `PlayerActorCompositionPart`, `PlayerActorCompositionOperationResult` | Player Actor keyed contribution · Experimental | `PlayerActorCompositionAccess` receives a Route/Activity-scoped binding for game components and opens contributions for the exact current Player/Actor occurrence through the existing Composer. Results include occurrence identities, revision, status and issue. |
| `ActorOccurrenceCompositionPart`, `ActorOccurrenceCompositionContribution`, `ActorOccurrenceCompositionOperationResult` | NonPlayer keyed contribution · Experimental | Adds, removes or adopts keyed parts for an exact current NonPlayer occurrence. Static owner-registered content opens through its root publication; dynamic content uses its publication token. Results retain occurrence, owner, revision, status and issue. The authored mount is explicit and separate from camera observation. |
| `PlayerActorSelectionDuplicatePolicy` | Session Actor-selection policy · Experimental | Selects whether ActorProfile identities may repeat across joined Slots. The type carries `FrameworkApiStatusAttribute(Experimental)` and is stored by Stable `GameApplicationAsset`; its existing serialized data remains covered by that asset's Stable authoring schema. See [IF-ADR-003](../Architecture/ADRs/IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md). |
| `PlayerSessionProfile`, `PlayerHostProvisioningMode` | Session profile/configuration · Experimental | Configure supported Slots, initial Session intent and Scene-Provided or Manager-Provisioned Host mode. |
| `LocalPlayerProvisioningAuthoring` | Authoring · Experimental | Configures explicit runtime Manager-Provisioned Player creation. |
| `PlayerSessionJoinTrigger`, `LocalPlayerProvisioningConsumerScope`, `LocalPlayerJoinRequest`, `LocalPlayerJoinOutcome`, `LocalPlayerJoinOutcomeCode`, `LocalPlayerJoinAdmissionState` | Manager-Provisioned Join boundary · Stable | The single public Join request boundary under IF-ADR-046. The trigger delegates through the active Route/Activity occurrence; the bounded outcome does not expose Session/Slot, Host or Actor evidence. `InputDevice` is part of the request contract. |
| `PlayerSessionObserver` and remaining Player Session command triggers | Observation/request components · Experimental | Explicit Session observation and Leave/Actor-selection/Joining commands. They do not replace physical Actor materialization and do not provide a second Join request path. The inherited `PlayerSessionScopedAccessConsumer.TryGetAccess` helper is protected for derived implementations; external consumers use Observer reads and authored command triggers. |
| `IPlayerSessionScopedAccess` | Scoped Player access contract · Experimental | Exposes independent Session observation, Leave, Joining, Slot/Actor operations and Gameplay Availability block/release. It is not a Join request entry point. Its broader Session contract remains Experimental. |
| `LocalPlayerJoinStatus` | Runtime diagnostic value · Experimental | Used by `PlayerActorPreparationRuntimeHostSnapshot.LastJoinStatus`; it is separate from the bounded `LocalPlayerJoinOutcome` returned by the Join boundary. |
| `PlayerGameplayAvailabilityBlockToken`, `PlayerGameplayAvailabilityBlockResult`, `PlayerGameplayAvailabilityBlockStatus` | Gameplay Availability values · Stable | Stable contract for independent occurrence-scoped block ownership and operation outcome. Result Message is diagnostic text, not a control value or localization contract. These blocks affect Runtime Gameplay Availability, not structural `GameplayReady`; they do not promote `IPlayerSessionScopedAccess`. |
| `PlayerGameplayAvailabilityBlockTrigger` | Authoring component · Stable | UnityEvent/UI surface for one consumer-owned block targeting one authored `PlayerSlotProfile`. It owns/releases only its token, binds through Route/Activity scoped access and does not represent turns or select among Players. Its scoped runtime dependency `IPlayerSessionScopedAccess` remains Experimental; arbitrary Persistent Content does not receive Activity binding. |
| `SceneProvidedLocalPlayerCreator` | Editor tooling · DevelopmentTooling | Creates an initial technical composition shell. It is not runtime ownership, admission or a completion check. |

Maturity scope: IF-ADR-044 certifies the runtime block/release scenarios at 8/8 with BaselineRestored; FirstGame manually validates trigger composition, independent P1/P2 ownership, teardown and Pause. Neither result promotes IPlayerSessionScopedAccess as a whole. Separately, user-reported QA-IF-ADR-036 Play Mode evidence is 8/8 for the supported Transition Gate sequence, including Pause rejection while the Gate is active and Pause/Resume after release; simultaneous active Pause and Transition Gate remains unsupported and uncertified. None of this expands Stable contracts. Gameplay readiness semantics remain Experimental and are defined by [IF-ADR-036](../Architecture/ADRs/IF-ADR-036-Gameplay-Readiness-and-Runtime-Gameplay-Availability-Separation.md) and [IF-ADR-012](../Architecture/ADRs/IF-ADR-012-Activity-Player-Participation-Profile-and-Readiness-Compatibility.md).

Guide: [Player Participation and Local Player](../Guides/Player-Usage.md), [Actor Composition Usage](../Guides/Actor-Composition-Usage.md), [Actor Occurrence References](../Guides/Actor-Occurrence-References.md). Scene-Provided is Stable authoring that requires Experimental Actor/session-profile configuration; the end-to-end composition therefore has a mixed maturity boundary. IF-ADR-046 and accepted Amendments 001–003 make only the `PlayerSessionJoinTrigger` + `LocalPlayerProvisioningConsumerScope` + request/outcome types listed above Stable for technical Session admission and Host commit. Actor preparation/materialization, Actor occurrence references, Actor composition, Gameplay Readiness, Session/Slot observation, Host/PlayerInput evidence, Manager-Provisioned configuration and the Framework remain outside this Stable closure. Certification accepts the partial provenance and residual risk recorded in Amendment 003; artifact hashes do not imply execution SHA linkage.

Local multiplayer is composed from Player Slots, admission/provisioning and camera Output assignments; there is no separate local multiplayer authority. See [Player Usage](../Guides/Player-Usage.md) and [Camera Usage](../Guides/Camera-Usage.md).

## Camera

| Surface | Category / maturity | Purpose |
|---|---|---|
| `CameraOutputAuthoring` | Unity authoring component · Stable | Binds an explicit Unity Camera, Cinemachine Brain and Fallback rig to an Output. |
| `CameraRigComposer` | Authoring/materialization component · Stable | Configures and materializes a local Cinemachine rig; it does not select an active camera or own the Unity Camera/Brain. |
| `FollowCameraRigBehaviorDefinition`, `FollowCameraRigMode` | Rig behavior asset and mode · Experimental | Fixed Offset remains the default for existing assets. Screen Composer Follow maps to Cinemachine `CinemachinePositionComposer` and keeps orientation fixed; Cinemachine 3.1.7 does not expose an independent Soft Zone control for this component. |
| `CameraRigPreviewController` | Editor authoring tool · Development Tooling | Enables an Edit Mode output for the original materialized `CinemachineCamera` using a temporary Unity Camera and CinemachineBrain under the owning rig. Save Configuration writes supported fields to their existing authoring owners and persists selected prefab properties; Disable restores transient target/channel state and unsaved rig edits. It does not participate in runtime camera selection. |
| `CameraOutputId`, `CameraRigReference`, `CameraRigPresentationIntent`, `CameraTargetRequirement`, `CameraResolvedTargets`, `CameraOperationStatus`, `CameraIssue`, `CameraIssueSeverity` | Camera supporting values · Stable within the per-Output Session 1..N surface | Values and diagnostics used by the supported Output/rig surface under [IF-ADR-038](../Architecture/ADRs/IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md); they do not stabilize Session Assignment assets, Actor Subject authoring, split-screen or additional Session Camera operations. |
| `SessionCameraAssignmentAsset`, `CameraOutputDefinition` | Assets · Experimental | Session Camera policy with a direct Rig Prefab reference, and explicit physical Output identity. |
| `ActorCameraSubjectAuthoring` | Actor authoring component · Experimental | Supplies an explicit Observation Transform for an exact Actor occurrence. |
| `GameApplicationAsset.StartupCameraAssignments` and `SessionCameraAssignmentAsset` | Session authoring · Experimental | Configure occurrence mode, membership/target policy, Rig Prefab and explicit Output mapping. Assignment assets are the sole Player Slot → Output authority; `CameraSessionConfiguration` owns only physical Output capacity. Focused evidence: IF-ADR-042 QA 7/7, IF-ADR-043 QA 7/7, QA-NEW-004 9/9 and Camera Editor 78/78; broader IF-ADR-038 recertification and transactional failure-path coverage remain separate. |
| `SessionCameraAssignmentCommandTrigger`, `SessionCameraAssignmentCommandKind` | Runtime request component and command selection · Experimental | The Trigger is an optional Inspector/UnityEvent adapter for activating, transactionally replacing, or clearing a Session Camera Assignment through Session Camera authority. The command kind is Experimental with this Trigger; this does not change the separate IF-ADR-039 Stable command port/consumer contract. Binding is covered by Framework Editor tests, but direct execution of all command modes through this trigger has no recorded consumer validation. Its Assignment asset dependency remains Experimental. |
| `ISessionCameraAssignmentCommandPort`, `ISessionCameraAssignmentCommandConsumer` | Runtime command/injection contract · Stable | The Session command port exposes Activate/Replace/Clear independently of UI. Scene-local gameplay components can implement the consumer contract and receive the exact port for their composition lifetime; the optional Trigger uses the same contract. The methods accept Experimental Assignment assets; this promotion does not stabilize those assets or extend evidence beyond the IF-ADR-039 flow. |

Maturity scope: the Stable command boundary is limited to IF-ADR-039 Activate/Replace/Clear, exact composition binding and the successful GameFlow sequence. Framework Editor tests cover invalid-candidate preservation/rollback; QA/consumer execution does not certify every Assignment/Output failure topology. Assignment, Output Definition and Actor Subject assets remain Experimental.

Activate/Replace/Clear belong to the Session Camera command contract and are independent of UI. Gameplay components receive the public port through `ISessionCameraAssignmentCommandConsumer`; the Trigger is one optional UnityEvent adapter implementing that contract. Consumers reference Assignment assets; their generated IDs are not manually authored integration inputs. Composition binding follows [IF-ADR-040](../Architecture/ADRs/IF-ADR-040-Scene-Composition-Binding-Model.md).

Guide: [Camera Usage](../Guides/Camera-Usage.md).

## Input and Pause

| Surface | Category / maturity | Purpose |
|---|---|---|
| `PlayerPauseInput` | Authoring component · Stable, single-player scope | Binds a Pause action to the admitted Local Player Host. |
| `PauseRequestTrigger` | Request component · Stable, single-player scope | Requests Pause, Resume or Toggle without owning Pause state. |
| `ActivityPauseAuthoring` | Activity authoring · Stable, single-player scope | Declares required/optional Activity Pause binding intent. |
| `GateBlocker`, `GateSnapshot`, `GateDecision`, `GateDecisionStatus`, `GateEvaluationResult`, `GateScope`, `GateDomain` | Gate values and evaluation · Stable, single-player Pause/Input/Gate scope | Expresses logical blockers and evaluation results. It does not own a generic Gate service or physically mutate PlayerInput. |
| `UnityPlayerInputGateAdapter` | Unity Input adapter · Stable, single-player scope | Projects the logical Gate onto the explicitly configured PlayerInput/Gameplay Action Map when its canonical binding and physical write succeed. `IsBlockedByAdapter` is true only while the adapter owns a block and read-back confirms the exact map is disabled; bounded automatic retries and explicit retry controls do not guarantee physical suppression after persistent failure. See [IF-ADR-005](../Architecture/ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md). |
| `PlayerInputActionMapReference` | Input reference value · Stable, single-player scope | Identifies a map by stable map ID rather than runtime name lookup. |
| `UnityPauseSurfaceAdapter`, `UnityPauseResidentSurfaceAdapter` | Unity presentation adapters · Stable | Project Pause state onto explicit authored surfaces. |
| `IPauseSurfaceAdapter` | Consumer adapter contract · Stable | Presents a Pause snapshot; does not own Pause, input or time scale. |
| `PauseState`, `PauseSnapshot`, `PauseRequest`, `PauseRequestId`, `PauseRequestKind`, `PauseRequestStatus`, `PauseResult`, `PauseIssue`, `PauseIssueSeverity`, `PauseInputActionId`, `PauseInputCommandKind`, `PauseInputIntent`, `PauseInputSignal`, `PauseInputSourceKind`, `PauseActivityBindingIntent`, `PauseActivityBindingIntentResolution`, `PauseActivityBindingIntentStatus`, `PauseActivityBindingRequiredness`, `PausePresentationIntent`, `PauseVisualSurfaceKind`, `PauseGateBlockerPolicy` | Pause supporting values/policy · Stable, single-player scope | Typed request and observation vocabulary plus passive logical Gate projection. The policy does not own Pause/Gate state or physical input; see [IF-ADR-005](../Architecture/ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md). |
| `ActivityPauseAuthoringValidator` | Explicit-root authoring validator · Stable | Resolves supplied Activity Pause declarations only; it does not perform global discovery or create runtime bindings. |
These Stable surfaces do not define multiplayer Pause policy. InputMode was
removed as a public subsystem, including its former Stable vocabulary. The
Pause binding performs its internal transaction within the Pause owner; the
Gate remains logical authorization, and the Unity adapter/writer remains the
only authority that applies physical `PlayerInput` posture. See [IF-ADR-005](../Architecture/ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md)
for the explicit breaking decision and migration.

Guide: [Input and Pause](../Guides/Pause-Usage.md).

## Progression Save

| Surface | Category / maturity | Purpose |
|---|---|---|
| `IProgressionSaveStore` | Backend adapter contract · Stable | Backend-neutral slot read/write/delete contract for custom or third-party storage adapters. |
| `ProgressionSaveBackendId`, `ProgressionSaveRecordId`, `ProgressionSaveSlotId`, `ProgressionSavePayload`, `ProgressionSavePayloadFormat`, `ProgressionSaveSlotRecord`, `ProgressionSaveReadStatus` / `ProgressionSaveReadResult`, `ProgressionSaveWriteStatus` / `ProgressionSaveWriteResult`, `ProgressionSaveDeleteStatus` / `ProgressionSaveDeleteResult` | Backend contract values · Stable | Data and operation vocabulary for the Stable store port. This does not promote `ProgressionSaveProfile`, the built-in JSON backend, autosave or gameplay snapshotting. See [IF-ADR-018](../Architecture/ADRs/IF-ADR-018-Progression-Save-Backend-Independence-and-Persistence-Domain-Boundaries.md). |
| `ProgressionSaveProfile` | Authoring profile · Experimental | Selects the application backend; it does not hold live save state or issue gameplay requests. |
| `JsonProgressionSaveStore` | Built-in backend · Experimental | Optional minimum local JSON implementation; physical transaction files are implementation details. |

Guides: [Progression Save authoring](../Guides/Progression-Save-Authoring.md), [backend adapter contract](../Guides/Progression-Save-Backend-Adapter-Contract.md), [built-in JSON backend](../Guides/Progression-Save-Built-In-Json-Backend.md).

## Reset

Reset authoring is Experimental. The supported consumer model is semantic authoring around Resettable, capabilities, compositions and Reset targets; registry mechanics and runtime-generated identities are not the normal gameplay API.

`UnityResetSubjectAdapter` and its independent registration contract were deliberately removed under RESET-035-H and IF-GOV-001. Existing authored Adapter components and custom implementations of its ID/registration contracts must migrate to `Resettable` owner-aware composition; no compatibility API is provided. See the migration guidance in [Reset Usage](../Guides/Reset-Usage.md).

| Surface | Category / maturity | Purpose |
|---|---|---|
| `Resettable` | Authoring component · Experimental | One independently executable Reset subject and hierarchy boundary for local Reset capabilities. Nested Resettables form independent boundaries. |
| `ResetComposition` | Authoring component · Experimental | Resolves a deterministic set of Resettables. It is not itself a Reset subject. |
| `ResetRequestTrigger` | Request component · Experimental | Sole normal Reset request surface for Object, Composition, CurrentActivity and CurrentRoute targets. |
| `ResetTarget`, `ResetObjectTarget`, `ResetCompositionTarget` | Target values · Experimental | Express semantic target intent and Direct/Stable addressing without exposing registry mechanics. |
| `ResetMembership` | Authoring policy · Experimental | Controls CurrentActivity/CurrentRoute inclusion independently from content ownership. |
| `ResetReferenceMode` | Addressing policy · Experimental | Selects Direct typed references or Stable IF-ADR-014 addressing for Object/Composition. |
| `UnityResetParticipantBehaviour`, `IUnityResettable` | Capability contracts · Experimental | Implement synchronous Reset capabilities collected within a `Resettable` hierarchy boundary; authored participant IDs are not required. |
| `IUnityResettableMetadata` | Optional capability metadata · Experimental | Supplies `ResetRequiredness`, `ResetOrder` and `ResetDisplayName` for a custom `IUnityResettable`. |
| `UnityTransformResetParticipant` | Reset capability · Experimental | Restores an authored Transform baseline. |
| `UnityGameObjectActiveResetParticipant` | Reset capability · Experimental | Restores an authored GameObject active-state baseline. |

Under `Resettable`, collected capability identity is runtime-generated/deterministic; consumers do not need to author participant IDs for the normal path.

Guide: [Reset Usage](../Guides/Reset-Usage.md). Architecture: [IF-ADR-035](../Architecture/ADRs/IF-ADR-035-Reset-Composition-Ownership-Membership-and-Targeting.md).

## Optional Audio

`ActivityBgmAuthoring` and `FrameworkBgmDirector` are Experimental optional BGM surfaces. Audio is not required to start an application.

Guide: [Audio Usage](../Guides/Audio-Usage.md).

## Logging

Framework logging configuration uses the separate `com.immersive.logging` package. Its types are not Framework package APIs.

Guide: [Logging Usage](../Guides/Logging-Usage.md).

## Scene Lifecycle

| Surface | Category / maturity | Purpose |
|---|---|---|
| `SceneLifecycleEvents` | Observer component · Experimental | Observes events for Framework-managed scene scope; it does not replace Unity SceneManager or create a general lifecycle authority. |

Guide: [Scene Lifecycle Events](../Guides/Scene-Lifecycle-Events.md).

## UI Integration

The Framework has no general-purpose UI system or standalone UI API. Use feature-owned request triggers and presentation adapters listed in the relevant sections; game screens and their composition remain consumer-owned.

## Types not recommended for direct game use

Internal hosts, runtime modules, binders, binding results, token types and projection/evidence structures exist to implement lifecycle and diagnostics. They are not a second authoring or gameplay API. Use the supported feature-specific command/request contracts and adapters; do not infer support solely from a type being declared `public`.

## Related architecture

- [IF-ADR-003 — Player Participation and Actor lifecycle](../Architecture/ADRs/IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md)
- [IF-ADR-005 — Input, Pause, Gate and Reset](../Architecture/ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md)
- [IF-ADR-015 — Player provisioning commands and observation](../Architecture/ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md)
- [IF-ADR-019 — Session Player lifetime and Activity representation](../Architecture/ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md)
- [IF-ADR-038 — Session Camera and Actor occurrence authority](../Architecture/ADRs/IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md)
- [IF-ADR-039 — Session Camera Assignment command boundary](../Architecture/ADRs/IF-ADR-039-Session-Camera-Assignment-Command-Boundary.md)
- [IF-ADR-040 — Scene Composition Binding Model](../Architecture/ADRs/IF-ADR-040-Scene-Composition-Binding-Model.md)
- [IF-ADR-041 — Route-Scoped Activity Transition Observation](../Architecture/ADRs/IF-ADR-041-Route-Scoped-Activity-Transition-Observation.md)
- [IF-ADR-049 — Public Actor Occurrence Reference and Resolution](../Architecture/ADRs/IF-ADR-049-Public-Actor-Occurrence-Reference-and-Resolution.md)
