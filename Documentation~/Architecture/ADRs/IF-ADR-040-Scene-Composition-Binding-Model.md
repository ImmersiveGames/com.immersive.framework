# IF-ADR-040 — Scene Composition Binding Model

Status: **Accepted — Implemented / Integrated / Validated**  
Proposed: **2026-10-03**  
Type: architecture / runtime composition / lifecycle  
Depends on: **IF-ADR-001, IF-ADR-008, IF-ADR-039**

## Context

Feature runtimes currently bind scene-authored consumers through several paths: direct Persistent Content passes, post-load Route/Activity passes, and SceneLifecycle participants. This duplicates root discovery and leaves detach behavior dependent on the feature. Persistent Content is a long-lived Session-owned root set and should follow the same composition contract as managed scenes.

## Decision

For features migrated to this model, `SceneLifecycleRuntime` coordinates composition using a feature-specific participant and explicit roots:

```text
Available(scope, roots) -> bind/register
Releasing(scope, roots, reason) -> unbind/release
```

The common internal scope distinguishes a loaded Scene from the owning Session. Persistent Content receives a Session scope and is explicitly released during host shutdown. Scene scopes are released before unload and made available again during unload compensation or already-loaded reentry.

Each feature keeps its own authority, participant, binder, ports, validation and diagnostics. The common result carries the scope, operation, success/status and diagnostic. Discovery is limited to the roots passed by composition. A binder must be idempotent for the same authority, reject a different authority, detach explicitly and idempotently, and roll back newly acquired bindings if its local bind pass fails.

Route-scoped `IRouteActivityTransitionObserver` instances use the already composed Route roots and remain alive with that Route composition. ActivityFlow directly delivers committed transition facts to those observers; this observation callback is read-only and does not bind an ActivityFlow command or authority. Route exit delivers the final transition to Activity None before Route roots are released. See IF-ADR-041 for the lifecycle extension.

Feature-specific participants may be components under Persistent Content roots. `SceneLifecycleRuntime` discovers those participants only during Session-scope composition and retains them for the Session lifetime, so optional feature assemblies can participate without adding their types to Framework Core. The retained participants receive later managed Scene scopes and are released with Persistent Content. This is participant discovery from explicit composition roots, not consumer binding or a global scan.

No universal binder, service locator, global scan, `FindObjectOfType`, or parallel scene-loaded scanner is introduced. Persistent Content is a scope, not a separate binding mechanism.

## Ownership and lifetime

- Session authorities remain owned by `FrameworkRuntimeHost`.
- Scene-authored adapters are discovered within the roots provided for that scope.
- Bind lifetime ends at the matching `Releasing` call; Persistent Content ends at Session shutdown.
- Release visits every participant and aggregates failures so one feature cannot prevent later participants from detaching.
- Feature participants own no domain authority and delegate domain rules to the existing feature binders.

## Initial migration boundary

The common model covers Pause request/surface bindings, Reset subject/request bindings, Camera command triggers, Route/Activity request triggers, CycleReset triggers, ActivityRestart triggers and Audio BGM Director injection.

Player Host/Actor/current-context binding, Player provisioning, Activity/Route content ownership and Loading/Transition remain outside this migration. `SceneProvidedLocalPlayerAuthoring` is an admission candidate and later conflict-evidence surface, not only a scene consumer. Its Session admission runtime intentionally discovers candidates from already-loaded scenes, handles scenes loaded outside Framework Scene Lifecycle, and provides lazy binding when a candidate invokes its API before reconciliation. Replacing this with managed-scene roots alone would miss valid admission/conflict candidates. The separate `UnityLocalPlayerProvisioningBackend` scene-loaded callback is also retained: it defers `PlayerInputManager.onPlayerJoined` callbacks received before Unity marks the source scene loaded. These are Player admission/provisioning contracts, not parallel consumers of the Scene Composition Binding Model.

Audio keeps its feature-specific consumer contract and BGM authority. `FrameworkBgmDirector` is discovered as a Session-lifetime composition participant from Persistent Content roots; it attaches/detaches `ActivityBgmAuthoring` and `RouteBgmAuthoring` only for the roots of each Available/Releasing scope. Session release also clears any residual scene-scoped consumer references before the authority ends. This dependency binding remains separate from Activity/Route BGM intent lifecycle and does not change sticky presentation policy.

`SessionCameraAssignmentCommandTrigger` remains an optional Unity adapter. Activate/Replace/Clear semantics remain owned by the Session Camera command authority. This ADR does not add Route/Activity Camera fields or Camera arbitration.

## Failure behavior

- Same-scope reentry with the same authority is idempotent.
- Binding to a different authority is rejected with a diagnostic.
- A feature binder rolls back only bindings newly created by its failed pass, preserving pre-existing idempotent bindings.
- Release is explicit and retries can safely call already-detached feature binders.
- SceneLifecycle reports participant failures with scope and participant identity; release still invokes remaining participants.

## Consequences

Route/Activity post-composition trigger passes and Persistent Content trigger passes are removed where SceneLifecycle now owns binding. Tests must cover first bind, reentry, release, release/compensation/rebind, authority conflict, local rollback, Session shutdown and feature regressions.

## Validation closure — 2026-10-03

The current migration boundary is validated.

Phase 1 evidence:

- Unity compile/import: PASS;
- Framework EditMode suite: 155/155 PASS;
- Camera QA-NEW-004: 9/9 PASS;
- Pause, Route/Activity request and Reset smoke coverage: PASS.

Phase 2 / Audio evidence:

- Audio Scene Composition EditMode coverage: 5/5 PASS;
- QA-NEW-005 Audio/BGM Continuity: 12/12 PASS;
- terminal cleanup: `BaselineRestored`;
- no temporary Route/Activity scenes remained loaded;
- no Route/Activity consumers remained bound or were destroyed while bound;
- `firstDivergence=''` and `cleanupIssue=''`.

Route-scoped Activity transition observation added by IF-ADR-041 is also validated:

- Framework EditMode aggregate: **169/169 PASS**;
- RouteLifecycle observer coverage: **6/6 PASS**;
- GameFlow Play Mode proves one observer retained by the Route composition across A/B/C and released with the Route, while Camera commands remain feature-owned.

`SceneProvidedLocalPlayerAuthoring` remains intentionally outside this model for the admission/candidate-discovery reasons recorded above. This is a documented exception, not an unfinished migration item.

The accepted scope of IF-ADR-040 is therefore implemented, integrated and validated. Future features may adopt this composition protocol when their ownership/lifetime matches it; this ADR does not require unrelated lifetime models to converge on Scene Composition.
