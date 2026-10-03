# Public API Reference

This is a curated map of supported consumer surfaces, not a list of every public C# type. Follow the linked guide for normal usage and check each row’s maturity before depending on it.

## Maturity and categories

The package uses `FrameworkApiStatusAttribute` to classify types as `Stable`, `Experimental`, `Internal` or `DevelopmentTooling`. A Stable type can still have a scope limitation, and one authoring workflow may combine types of different maturity. See [API maturity governance](../Architecture/Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md).

| Category | Meaning for consumers |
|---|---|
| Stable | Supported consumer contract; breaking changes require an explicit architecture and migration decision. |
| Experimental | Available for controlled development without compatibility guarantees. |
| Internal | Implementation detail; do not build game code against it. |
| Development Tooling | Editor convenience or diagnostics, not runtime/gameplay authority. |

A public type without an explicit consumer status is not automatically a recommended direct API. Prefer authored components/assets and the guide’s stated usage path.

## Application

| Surface | Category / maturity | Purpose |
|---|---|---|
| `GameApplicationAsset` | Authoring asset · Stable | Application root: Startup Route, Session policies, Persistent Content and application-scoped features. |
| `PersistentContentComposition` and Persistent Content Scene Template | Authoring configuration/template · Stable workflow | Explicit scene composition retained for application lifetime. The template does not create or register the consumer scene automatically. |

Guide: [Getting Started](../Guides/Getting-Started.md), [Persistent Content Scene Template](../Guides/Persistent-Content-Scene-Template.md).

## Game Flow, Route and Activity

| Surface | Category / maturity | Purpose |
|---|---|---|
| `RouteAsset`, `ActivityAsset` | Authoring assets · Stable | Define Route identity/content/startup Activity and Activity content/participation/readiness/transition policy. |
| `RouteContentProfileAsset`, `ActivityContentProfileAsset` | Profiles · Stable | Declare content owned by their Route or Activity scope. |
| `RouteRequestTrigger`, `ActivityRequestTrigger` | Request components · Stable | Request a Route, request an Activity or clear the Activity from an authored scene/UI event. |

Guide: [Game Flow](../Guides/Game-Flow.md); [Activity Readiness](../Guides/Activity-Readiness.md).

## Activity Readiness

| Surface | Category / maturity | Purpose |
|---|---|---|
| `ActivityReadinessParticipant` | Authoring component · Experimental | Contributes one Required or Optional preparation condition to an Activity readiness occurrence. |
| `ActivityReadinessEvents` | Observer component · Experimental | Presents Preparing, Ready and Not Ready events; it observes readiness and does not decide it. |

Guide: [Activity Readiness](../Guides/Activity-Readiness.md).

## Player Participation and Actor

| Surface | Category / maturity | Purpose |
|---|---|---|
| `SceneProvidedLocalPlayerAuthoring` | Authoring component · Stable | Declares a scene-authored Local Player candidate, Slot/Profile references and admission timing. Its required `ActorProfile` and admission timing type have separate Experimental maturity. |
| `LocalPlayerHostAuthoring` | Authoring component · Stable | Explicit technical Host boundary for PlayerInput, ActorMount and Actor Runtime Host configuration. Manager provisioning remains Experimental. |
| `PlayerSlotProfile` | Profile · Stable for Scene-Provided use | Owns a stable Slot ID and presentation metadata; optional default Actor intent is separate from runtime allocation. |
| `ActorProfile` | Profile · Experimental | Reusable Actor identity, classification and optional visual content. It does not own an Actor occurrence or its spatial state. |
| `PlayerSessionProfile`, `PlayerHostProvisioningMode` | Session profile/configuration · Experimental | Configure supported Slots, initial Session intent and Scene-Provided or Manager-Provisioned Host mode. |
| `LocalPlayerProvisioningAuthoring` | Authoring · Experimental | Configures explicit runtime Manager-Provisioned Player creation. |
| `PlayerSessionObserver` and Player Session command triggers | Observation/request components · Experimental | Explicit Session observation and logical Join/Leave/Actor-selection commands. They do not replace physical Actor materialization. |
| `SceneProvidedLocalPlayerCreator` | Editor tooling · DevelopmentTooling | Creates an initial technical composition shell. It is not runtime ownership, admission or a completion check. |

Guide: [Player Participation and Local Player](../Guides/Player-Usage.md). Scene-Provided is Stable authoring that requires Experimental Actor/session-profile configuration; the end-to-end composition therefore has a mixed maturity boundary.

## Camera

| Surface | Category / maturity | Purpose |
|---|---|---|
| `CameraOutputAuthoring` | Unity authoring component · Stable | Binds an explicit Unity Camera, Cinemachine Brain and Fallback rig to an Output. |
| `CameraRigComposer` | Authoring/materialization component · Stable | Configures and materializes a local Cinemachine rig; it does not select an active camera or own the Unity Camera/Brain. |
| `CameraDefinition`, `CameraOutputDefinition` | Assets · Experimental | Reusable Session Camera definition and explicit physical Output identity. |
| `ActorCameraSubjectAuthoring` | Actor authoring component · Experimental | Supplies an explicit Observation Transform for an exact Actor occurrence. |
| Session Camera Assignments on `GameApplicationAsset` | Session authoring · current implementation, Experimental boundary | Configure occurrence mode, membership/target policy and explicit Output mapping. Individual Assignments are the sole Player Slot → Output authority; `CameraSessionConfiguration` owns only physical Output capacity. IF-ADR-038 is Accepted; Unity validation remains pending. |
| `SessionCameraAssignmentCommandTrigger` | Runtime request component · Experimental | Explicitly Activate, transactionally Replace, or Clear a Session Camera Assignment through Session Camera authority. It does not reintroduce CameraRequest, precedence or Route/Activity Camera ownership. |

There is no Camera Request/Presentation arbitration API in the current model. Runtime Assignment mutation is exposed only through `SessionCameraAssignmentCommandTrigger`. `SessionCameraAssignmentAuthoring` remains serialized authoring data; its C# visibility alone does not make it a recommended direct runtime integration surface.

Guide: [Camera Usage](../Guides/Camera-Usage.md).

## Input and Pause

| Surface | Category / maturity | Purpose |
|---|---|---|
| `PlayerPauseInput` | Authoring component · Stable, single-player scope | Binds a Pause action to the admitted Local Player Host. |
| `PauseRequestTrigger` | Request component · Stable, single-player scope | Requests Pause, Resume or Toggle without owning Pause state. |
| `ActivityPauseAuthoring` | Activity authoring · Stable, single-player scope | Declares required/optional Activity Pause binding intent. |
| `UnityPlayerInputGateAdapter` | Unity Input adapter · Stable, single-player scope | Applies the explicit PlayerInput/Gameplay Action Map gate. |
| `PlayerInputActionMapReference` | Input reference value · Stable, single-player scope | Identifies a map by stable map ID rather than runtime name lookup. |
| `UnityPauseSurfaceAdapter`, `UnityPauseResidentSurfaceAdapter` | Unity presentation adapters · Stable | Project Pause state onto explicit authored surfaces. |
| `IPauseSurfaceAdapter` | Consumer adapter contract · Stable | Presents a Pause snapshot; does not own Pause, input or time scale. |

These Stable surfaces do not define multiplayer Pause policy.

Guide: [Input and Pause](../Guides/Pause-Usage.md).

## Progression Save

| Surface | Category / maturity | Purpose |
|---|---|---|
| `IProgressionSaveStore` | Backend adapter contract · Stable | Backend-neutral slot read/write/delete contract for custom or third-party storage adapters. |
| `ProgressionSaveProfile` | Authoring profile · Experimental | Selects the application backend; it does not hold live save state or issue gameplay requests. |
| `JsonProgressionSaveStore` | Built-in backend · Experimental | Optional minimum local JSON implementation; physical transaction files are implementation details. |

Guides: [Progression Save authoring](../Guides/Progression-Save-Authoring.md), [backend adapter contract](../Guides/Progression-Save-Backend-Adapter-Contract.md), [built-in JSON backend](../Guides/Progression-Save-Built-In-Json-Backend.md).

## Reset

Reset authoring is Experimental. The supported consumer model is semantic authoring around Resettable, capabilities, compositions and Reset targets; registry mechanics and runtime-generated identities are not the normal gameplay API.

| Surface | Category / maturity | Purpose |
|---|---|---|
| `Resettable` | Authoring component · Experimental | One independently executable Reset subject and hierarchy boundary for local Reset capabilities. Nested Resettables form independent boundaries. |
| `ResetComposition` | Authoring component · Experimental | Resolves a deterministic set of Resettables. It is not itself a Reset subject. |
| `ResetRequestTrigger` | Request component · Experimental | Sole normal Reset request surface for Object, Composition, CurrentActivity and CurrentRoute targets. |
| `ResetTarget`, `ResetObjectTarget`, `ResetCompositionTarget` | Target values · Experimental | Express semantic target intent and Direct/Stable addressing without exposing registry mechanics. |
| `ResetMembership` | Authoring policy · Experimental | Controls CurrentActivity/CurrentRoute inclusion independently from content ownership. |
| `ResetReferenceMode` | Addressing policy · Experimental | Selects Direct typed references or Stable IF-ADR-014 addressing for Object/Composition. |
| `UnityTransformResetParticipant` | Reset capability · Experimental | Restores an authored Transform baseline. |
| `UnityGameObjectActiveResetParticipant` | Reset capability · Experimental | Restores an authored GameObject active-state baseline. |
| `UnityResetSubjectAdapter` | Independent registration adapter · Experimental | Retained independent subject-registration contract; not the normal Resettable path and not a request surface. |

Under `Resettable`, collected capability identity is runtime-generated/deterministic; consumers do not need to author participant IDs for the normal path.

Guide: [Reset Usage](../Guides/Reset-Usage.md). Architecture: [IF-ADR-035](../Architecture/ADRs/IF-ADR-035-Reset-Composition-Ownership-Membership-and-Targeting.md).

## Optional Audio

`ActivityBgmAuthoring` and `FrameworkBgmDirector` are Experimental optional BGM surfaces. Audio is not required to start an application.

Guide: [Audio Usage](../Guides/Audio-Usage.md).

## Logging

Framework logging configuration uses the separate `com.immersive.logging` package. Its types are not Framework package APIs.

Guide: [Logging Usage](../Guides/Logging-Usage.md).

## Types not recommended for direct game use

Internal hosts, runtime modules, binders, binding results, token types and projection/evidence structures exist to implement lifecycle and diagnostics. They are not a second authoring or gameplay API. For example, use `PauseRequestTrigger` and the supported Pause components rather than wiring against internal Global UI binding results such as `GlobalUiPauseRequestTriggerBindingResult`. Do not infer support solely from a type being declared `public`.

## Related architecture

- [IF-ADR-003 — Player Participation and Actor lifecycle](../Architecture/ADRs/IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md)
- [IF-ADR-005 — Input, Pause, Gate and Reset](../Architecture/ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md)
- [IF-ADR-015 — Player provisioning commands and observation](../Architecture/ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md)
- [IF-ADR-019 — Session Player lifetime and Activity representation](../Architecture/ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md)
- [IF-ADR-038 — Session Camera and Actor occurrence authority](../Architecture/ADRs/IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md)
- [IF-ADR-039 — Session Camera Assignment command boundary](../Architecture/ADRs/IF-ADR-039-Session-Camera-Assignment-Command-Boundary.md)
