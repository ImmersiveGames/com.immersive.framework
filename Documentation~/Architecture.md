# Architecture

This page describes the stable package architecture for users of `com.immersive.framework`. It is not a cut log, roadmap tracker or internal ADR index.

## Package model

`com.immersive.framework` owns framework-specific runtime contracts, Unity adapters, authoring assets and diagnostics. Generic technical primitives belong to the technical packages:

- `com.immersive.foundation`
- `com.immersive.logging`
- `com.immersive.pooling`

The framework package may consume those packages, but package docs should not present copied primitives as framework-owned architecture.

## Extension archetypes

Framework extension work uses these archetypes:

- Surface: a stable runtime capability boundary, such as Loading, Transition or Pause.
- Adapter: concrete side-effect execution with local evidence.
- Bridge: Unity authoring wrapper that reads Inspector data, validates locally, calls an explicit boundary and exposes diagnostics.
- Operation Service: non-MonoBehaviour orchestration for multi-step operations, rollback, failed stage and evidence preservation.
- Consumer: code or tooling that requests a capability and handles absence/failure explicitly.
- Validator/Evidence: readiness or result validation without side effects.
- QA Smoke Runner: validation entry point that proves a bounded contract, not product runtime policy.
- Runtime Surface Host: host for shared surface availability, such as `UIGlobal`.

This model guides development language and ownership. It does not authorize broad adapter or Surface layer expansion; that remains gated by architecture readiness work.

## Runtime shape

| Surface | Owner | Purpose |
| --- | --- | --- |
| Game Application | `GameApplicationAsset`, `FrameworkRuntimeHost` | Startup application context, startup route and optional `UIGlobal` scene loading. |
| Route | `RouteAsset`, route lifecycle runtime | Top-level navigation and route content lifecycle. |
| Activity | `ActivityAsset`, activity flow runtime | Activity selection, activity content lifecycle and activity scene operation planning/execution. |
| UIGlobal | `GlobalUiSceneRuntime` | App/session scoped scene for shared Transition, Loading and Pause visual adapters. |
| Transition | Transition runtime/effect adapters | Route/activity visual envelopes and transition effects. |
| Loading | Loading surface runtime/adapters | Loading presentation, progress and readiness reporting. |
| Pause | Pause runtime/surface/input bridges | Logical Pause state, resident Pause presentation and explicit InputMode/PlayerInput synchronization. |
| RuntimeContent | Runtime content runtime, handles and release requests | Logical content identity, handle state, materialization/release request language and lifecycle registry evidence. |
| ContentAnchor | ContentAnchor declarations, binding runtime and Unity adapters | Logical anchor declarations/bindings plus explicit Unity materialization, placement and release helpers. |

Route and Activity request logs also project lifecycle-local operation evidence through `lifecycleOperation*` fields. This is a diagnostic ledger over existing Route/Activity evidence; it is not a GameFlow envelope and does not replace Route or Activity result/status types.

F45 adds lifecycle-local content/readiness evidence projection to those request logs through `lifecycleContent*` and `lifecycleReadiness*` fields. The projection is observational: Route, ActivityFlow, RuntimeContent and ContentAnchor keep their execution and domain status ownership. It does not create a content dispatch kernel or readiness kernel.

F44, F45 and F46 close the initial lifecycle evidence stabilization. F47 accepts GameFlow request envelope as a request/diagnostics boundary. F48 adds the first internal passive runtime shell and projects `gameFlowEnvelope*` fields into Route and Activity request logs. F49 keeps envelope creation as `FrameworkRuntimeHost` diagnostics projection because that owner already has Route/Activity domain results, lifecycle projections and Loading diagnostics at log time. The shell summarizes existing evidence only: it does not execute flow, decide policy, replace Route/Activity results, migrate triggers or change Route, Activity, Scene, Content, Loading or Transition ownership.

F50 approves Route/Activity trigger migration to the shared FlowTrigger helper for local state and diagnostics only. F51 implements that migration in `RouteRequestTrigger` and `ActivityRequestTrigger`: last phase/outcome/reason/message and succeeded/ignored/failed flags are helper-backed, while target selection, Activity clear semantics, local result mapping and `FrameworkRuntimeHost` request entry points remain trigger-owned. `gameFlowEnvelope*` ownership stays in `FrameworkRuntimeHost`.

F52 keeps `FrameworkRuntimeHost` as the current Route/Activity request API boundary. No public or internal GameFlow request API is introduced now. `GameFlowRuntime` remains internal execution/admission runtime, and `GameFlowRequestEnvelope` remains internal passive diagnostics rather than a public request object.

F53 closes GameFlow as the active architecture axis for now and selects Transition Surface / Effects Hardening as the next practical track toward a first usable game flow. Transition is selected because Route/Activity requests already project transition diagnostics and `UIGlobal` already hosts transition effect adapters. This does not create a broad Surface layer, does not reopen GameFlow API design and does not promote Pause visual materialization. Loading remains the bounded reference pattern for future contract/evidence questions.

F54 accepts the Transition Surface / Effects Contract. Transition is the visual envelope before/after Route/Activity operations; Transition Effects are concrete visual operations such as fade/curtain/blackout; Transition Effect Adapters execute Unity-side visual side effects and return local evidence. `FrameworkRuntimeHost` / Route/Activity request execution is the current consumer, `UIGlobal` is the explicit host for effect adapters, and Loading remains separate from Transition because Loading communicates progress/readiness rather than visual coverage.

F55 hardens Transition runtime evidence locally. Transition results now preserve named internal `TransitionEffectAdapterEvidence`, and Route/Activity request logs project additive `transitionEffectAdapterEvidence*` fields. This preserves existing visual behavior and existing `transition*`, `gameFlowEnvelope*`, `lifecycleOperation*` and `loadingAdapterEvidence*` diagnostics without adding public API.

## Current boundary rules

- `GameApplicationAsset` is the authoring root for app startup and `UIGlobal` policy.
- Route owns route lifecycle; Activity owns activity lifecycle below the active route.
- `UIGlobal` owns shared visual surfaces; route/activity content should not own global Pause, Loading or Transition surfaces.
- Loading is not a fade effect. Transition effects and Loading surfaces are separate runtime surfaces.
- Transition Effect Adapters return effect-local evidence; Transition aggregate diagnostics preserve named adapter evidence without creating a universal adapter/result type.
- Pause presentation is resident by default. RuntimeContent + ContentAnchor materialization remains available for explicit modular content paths.
- Pause input must synchronize logical Pause, `InputMode` and Unity `PlayerInput` through the runtime bridge path.
- RuntimeContent handles are logical state, not Unity object references.
- ContentAnchor binding is logical; physical placement belongs to Unity ContentAnchor adapters/services.

## Minimum authoring model

The package-level Model is the authored data/configuration a consumer project creates so the framework can run. It is not a gameplay model, save model, actor model, inventory, entity system or generic model registry.

The minimum 1.0 Model is:

- Game Application Model: `GameApplicationAsset` defines startup route, validation mode and `UIGlobal` scene policy.
- Route Model: `RouteAsset` defines route identity, primary scene, optional startup activity and optional route content profile.
- Activity Model: `ActivityAsset` defines activity identity, optional activity content profile and activity visual transition mode.
- Surface Model: existing Loading, Transition and Pause configuration through `UIGlobal` and concrete adapters; this is not a generic Surface layer.
- Content/Anchor Model: existing route/activity content profiles and `RouteContentAnchor` / `ActivityContentAnchor` declarations when those paths are used.

Runtime executes, model describes, Unity adapters apply local Unity-side effects, and bridges connect Inspector/authorship to explicit runtime boundaries. Missing required authoring must be validation evidence, not silent fallback.

## Model readiness validation

Model readiness is Editor-only and belongs to framework authoring/validation tooling, not runtime execution.

`FrameworkAuthoringModelReadinessValidator` consolidates existing authoring validation and adds package-readiness checks for the minimum 1.0 Model:

- active Game Application;
- Startup Route;
- Validation Mode and `UIGlobal` policy;
- route/activity scenes in Build Settings;
- required `UIGlobal` scene in Build Settings;
- expected Transition and Loading adapters in `UIGlobal`;
- resident Pause adapter evidence as optional unless a future serialized policy makes it required;
- route/activity content profile scene readiness;
- existing Content Anchor/materialization bridge validation when open-scene validation is included.

The validator reports issues, warnings, info and optional skips. It does not create assets, modify Project Settings, edit Build Settings, add adapters, create fallback or alter runtime behavior.

## Internal history

Historical ADRs, old roadmaps and phase notes remain under `ADRs/`, `Planning/` and `Guides/` for deep reference only. They are not the active package architecture model.
