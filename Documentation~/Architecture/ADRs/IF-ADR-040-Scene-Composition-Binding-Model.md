# IF-ADR-040 — Scene Composition Binding Model

Status: **Accepted — Unity validation pending**  
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

No universal binder, service locator, global scan, `FindObjectOfType`, or parallel scene-loaded scanner is introduced. Persistent Content is a scope, not a separate binding mechanism.

## Ownership and lifetime

- Session authorities remain owned by `FrameworkRuntimeHost`.
- Scene-authored adapters are discovered within the roots provided for that scope.
- Bind lifetime ends at the matching `Releasing` call; Persistent Content ends at Session shutdown.
- Release visits every participant and aggregates failures so one feature cannot prevent later participants from detaching.
- Feature participants own no domain authority and delegate domain rules to the existing feature binders.

## Initial migration boundary

The common model covers Pause request/surface bindings, Reset subject/request bindings, Camera command triggers, Route/Activity request triggers, CycleReset triggers and ActivityRestart triggers.

Player Host/Actor/current-context binding, Player provisioning, Activity/Route content ownership, Loading/Transition, and Audio remain on their current lifecycles for this cut. Their differences require separate migration decisions; they do not justify a universal binder. Audio may adopt the same composition shape later while retaining Audio-specific injection and runtime ownership.

`SessionCameraAssignmentCommandTrigger` remains an optional Unity adapter. Activate/Replace/Clear semantics remain owned by the Session Camera command authority. This ADR does not add Route/Activity Camera fields or Camera arbitration.

## Failure behavior

- Same-scope reentry with the same authority is idempotent.
- Binding to a different authority is rejected with a diagnostic.
- A feature binder rolls back only bindings newly created by its failed pass, preserving pre-existing idempotent bindings.
- Release is explicit and retries can safely call already-detached feature binders.
- SceneLifecycle reports participant failures with scope and participant identity; release still invokes remaining participants.

## Consequences

Route/Activity post-composition trigger passes and Persistent Content trigger passes are removed where SceneLifecycle now owns binding. Tests must cover first bind, reentry, release, release/compensation/rebind, authority conflict, local rollback, Session shutdown and feature regressions.

Unity compile/import and EditMode/QA validation remain required before this decision is considered validated.
