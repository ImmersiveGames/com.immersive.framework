# IF-ADR-040 — Scene Composition Binding Model

Status: **Accepted — implementation pending**  
Proposed: **2026-10-03**  
Type: architecture / composition / runtime binding / lifecycle  
Related: **IF-ADR-001, IF-ADR-005, IF-ADR-008, IF-ADR-013, IF-ADR-033, IF-ADR-035, IF-ADR-038, IF-ADR-039**

## 1. Context

The Framework currently associates scene-authored consumers with runtime authorities through several different mechanisms:

- Persistent Content bootstrap binding;
- SceneLifecycle participants;
- Route/Activity composition-specific bind passes;
- direct `SceneManager.sceneLoaded` scanners;
- Player Host / Actor occurrence binding;
- explicit Session-level injection;
- content lifecycle dispatch;
- persistent presentation adapter collection.

These mechanisms do not all represent the same kind of relationship, but scene-local dependency binding has become inconsistent in discovery, detach semantics, reentry behavior and diagnostics.

This creates two problems:

1. a consumer cannot infer binding lifetime from the fact that it is scene-authored;
2. features can accidentally invent parallel scene-discovery mechanisms for equivalent needs.

Persistent Content also currently receives special bootstrap treatment for several bindings even though, architecturally, it is application-scoped content with a longer lifetime rather than a different dependency model.

## 2. Decision

Adopt one common **Scene Composition Binding Model** for scene-local consumers that need a typed runtime authority.

The common lifecycle is:

```text
typed feature authority
    -> feature-specific composition participant
        -> Available(scope, roots)
            bind / register contribution
        -> Releasing(scope, roots)
            unbind / release contribution
```

The Framework standardizes the lifecycle and guarantees, not one universal binder.

Each feature keeps:

- its own authority;
- its own consumer contract;
- its own binder / participant;
- its own validation rules;
- its own result type when domain-specific evidence is useful.

No feature gains access to another feature's internals through this model.

## 3. Composition scope

A composition callback operates on an explicit scope plus an explicit root set.

Initial supported scope kinds are:

### Scene scope

Represents one concrete Unity scene composition.

Identity is based on the concrete scene instance/handle supplied by SceneLifecycle.

Scene scopes receive:

- `Available` when the composed scene becomes available;
- `Releasing` before that composed scene is released.

### Session persistent-content scope

Represents the root set retained from Persistent Content for the application/session lifetime.

Persistent Content is not treated as a special binding mechanism. It is a long-lived composition scope.

Its roots receive the same binding guarantees as scene roots, with explicit release during Session shutdown.

The original container scene being unloaded after its roots are retained does not end this scope.

## 4. Required binding semantics

For scene-local dependency binding:

- the authority is passed explicitly by Framework composition;
- discovery is limited to the supplied roots;
- no `FindObjectOfType`, hierarchy/name lookup or global mutable registry is introduced;
- rebinding the same consumer to the same authority is idempotent;
- binding the same consumer to a different concurrent authority is rejected;
- detach verifies the expected authority;
- detach is idempotent;
- `Available` may be delivered more than once for the same scope;
- `Releasing` may be followed by `Available` again if SceneLifecycle compensates a failed unload;
- a participant that partially mutates its own feature during `Available` must roll back the changes it introduced before returning failure;
- release must remove scene-owned dependencies/contributions that would otherwise outlive the scope;
- diagnostics must identify scope, consumer counts and failure reason.

A feature may expose a richer typed result, but human-readable diagnostics remain required.

## 5. SceneLifecycle relationship

SceneLifecycle is the canonical coordinator for Framework-managed scene composition.

For scenes loaded through Framework Route/Activity/content flows, features must not maintain a parallel global scene scanner merely to discover the same scene-local consumers.

Existing `OnSceneAvailable` / `OnSceneReleasing` semantics remain repeatable and require idempotent participants.

This ADR does not make SceneLifecycle a service locator. It distributes explicit root sets and lifecycle notifications to precomposed feature participants.

## 6. Persistent Content relationship

Persistent Content remains consumer-authored application-persistent composition under IF-ADR-008.

This ADR changes only how runtime binding should be understood:

```text
Persistent Content
    = Session-lifetime composition scope

not

Persistent Content
    = privileged binding mechanism
```

Features that currently bind Persistent Content through bespoke startup code should converge toward the same composition semantics when migrated.

Session shutdown must explicitly release bindings/contributions owned by the persistent scope.

## 7. Legitimate exceptions

Not every association in the Framework is scene binding.

The following remain outside this common scene-composition contract unless a later ADR changes their ownership:

- constructor injection between runtimes;
- Player Host / Actor / PlayerOccurrence contextual binding;
- Session-level Player provisioning references;
- Route/Activity content entry/exit dispatch;
- operation-scoped dependencies such as loading progress reporters;
- exact Player Slot / Host authority relationships;
- external Unity callbacks used for engine integration where the relevant lifetime is not Framework scene composition.

Reset subject registration may remain distinct from execution-port binding because registration has its own owner/readiness semantics. The two lifetimes must remain explicit.

The existence of a scene GameObject alone is not sufficient reason to move a relationship into SceneLifecycle.

## 8. Feature-specific participants

The intended shape is feature-local, for example:

```text
PauseProductBindingSceneLifecycleParticipant
ResetProductBindingSceneLifecycleParticipant
SessionCameraCommandBindingSceneLifecycleParticipant
GameFlowRequestBindingSceneLifecycleParticipant
AudioBgmBindingSceneLifecycleParticipant
```

Names are illustrative; implementation may retain existing names where they already express the responsibility.

There is no `UniversalBindingManager`, generic dependency injector or service locator.

## 9. Camera consequence

IF-ADR-039 remains the Camera command authority decision.

Its scene-authored adapters must no longer depend on Persistent Content bootstrap as their only binding path.

A `SessionCameraAssignmentCommandTrigger` located in a Framework-composed additive scene must be able to receive and release the Session Camera command authority through this model.

The Trigger remains an optional Unity adapter. Gameplay code and contextual adapters may use the same typed Camera command boundary without depending on UI.

This ADR does not change IF-ADR-039 command semantics:

- Activate;
- Replace;
- Clear.

## 10. Audio consequence

The Audio director remains the BGM authority defined by IF-ADR-013.

The current direct `SceneManager.sceneLoaded` injection runtime is migration input, not the target scene-composition model for Framework-managed scenes.

Audio may preserve separate handling only for scenes intentionally loaded outside Framework SceneLifecycle if that product requirement is explicitly retained.

## 11. Route, Activity, Pause and Reset consequence

Pause's request-trigger bind/unbind behavior is the current reference implementation for symmetric scene binding.

Route/Activity request triggers, Cycle Reset, Activity Restart and Camera should converge away from one-off post-composition bind passes where the same roots/lifetime are already represented by SceneLifecycle.

Reset must distinguish:

- binding of runtime ports;
- registration of reset subjects.

Port binding should gain explicit detach semantics even if subject registration retains separate readiness/retry behavior.

## 12. Non-goals

This ADR does not:

- make every runtime scene-bound;
- replace domain-specific binders with a generic binder;
- create a global dependency container;
- expose runtime authorities through static access;
- redefine Player Host/Actor occurrence ownership;
- merge Activity/Route content lifecycle with scene lifecycle;
- require all features to migrate in one implementation cut;
- change gameplay semantics merely to normalize infrastructure.

## 13. Migration order

Migration should proceed incrementally:

1. establish common scope/result/lifecycle semantics and Session persistent-scope release;
2. preserve Pause as the reference symmetric bind/unbind implementation;
3. close missing detach semantics in Reset port bindings;
4. migrate Camera and Route/Activity/CycleReset/Restart scene-local command adapters;
5. migrate Audio away from parallel scanning for Framework-managed scenes;
6. evaluate Scene-Provided Player discovery separately after confirming its eligible scene lifetime matches SceneLifecycle.

Player Host/Actor occurrence binding and explicit provisioning remain unchanged unless independently justified.

## 14. Validation

Each migrated feature must prove, as applicable:

- bind on newly available scene;
- idempotent reentry / already-loaded notification;
- explicit release on scene releasing;
- release followed by compensation/re-available;
- rejection of a different authority;
- local rollback after partial bind failure;
- persistent-scope bind and Session shutdown release;
- no stale binding after scope end;
- no duplicate global scene scanner for the migrated responsibility.

Implementation, tests, integration and Unity validation must be reported separately.

## 15. Normative summary

```text
Scene-local runtime binding has one predictable lifecycle:

scope + roots become available
    -> feature participant binds/registers

scope + roots release
    -> feature participant unbinds/releases

The lifecycle is common.
The authorities, binders and domain rules remain feature-specific.

Persistent Content is a Session-lifetime scope, not a privileged binding system.

SceneLifecycle coordinates Framework-managed scene composition.

Player occurrence/context binding and other non-scene-owned relationships remain
outside this contract.

No universal binder, service locator or global scene-discovery layer is introduced.
```
