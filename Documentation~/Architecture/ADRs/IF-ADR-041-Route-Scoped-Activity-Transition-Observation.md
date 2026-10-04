# IF-ADR-041 — Route-Scoped Activity Transition Observation

Status: **Accepted — implementation present; EditMode execution pending**
Proposed: **2026-10-03**
Type: architecture / Activity Flow / Route composition
Depends on: **IF-ADR-001, IF-ADR-039, IF-ADR-040**
Refines: **IF-ADR-039 GameFlow consumer placement only**

## Context

Some sample policy belongs to a Route lifetime but needs to react to Activity changes within that Route. Route content currently receives Route Enter/Exit, while Activity content receives Activity Enter/Exit. A Route-scoped consumer therefore has no supported callback carrying the committed Activity transition.

## Decision

ActivityFlow remains the sole owner of Activity transition preparation, commit and state. It may notify read-only observers in the active Route composition after a transition commits. Observation cannot request, veto, defer, roll back or otherwise control Activity Flow.

The notification is delivered exactly once for each committed Activity transition, after Activity content Exit/Enter callbacks complete and before the outgoing Activity content is released. A Route exit that commits `CurrentActivity == null` is observed before the Route composition is released. Failed or uncommitted transitions produce no notification.

Observers are discovered from explicit Route composition roots and follow the binding and release lifetime in IF-ADR-040. The mechanism uses no event bus, singleton, polling or global scene lookup.

## Ownership

- **ActivityFlow:** prepares, commits and reports Activity state transitions.
- **Route composition:** owns observer instance lifetime and receives notifications while bound.
- **Observer:** reads the committed transition and applies its own sample/game policy; it has no Activity Flow authority.
- **Session Camera:** remains the single writer of Session Camera Assignment state. A Route observer may consume the existing command port but does not own Camera state.

## Minimal public API proposal

Add an immutable context and one read-only receiver contract:

```csharp
using Immersive.Framework.Authoring;

namespace Immersive.Framework.RouteLifecycle
{
    public readonly struct RouteActivityTransitionContext
    {
        public ActivityAsset PreviousActivity { get; }
        public ActivityAsset CurrentActivity { get; }
        public string Source { get; }
        public string Reason { get; }
    }

    public interface IRouteActivityTransitionObserver
    {
        void OnActivityTransitionCommitted(RouteActivityTransitionContext context);
    }
}
```

`PreviousActivity` is null on initial entry; `CurrentActivity` is null when the Route exits to Activity None. The receiver exposes no transition command or mutable Activity state. Implementations are scene-authored beneath a Route composition contribution; ActivityFlow dispatches through the current explicit Route scope.

## Scope

The GameFlow sample will host one observer and Session Camera command consumer in `SCN_GameFlow_Basic`, authored for `Route_BasicFlow`. It maps Previous/Current Activity to Assignment A/B/none and emits Activate, Replace, Clear or no command. The Route selects no Camera and owns no Assignment; this is sample policy, and Session Camera remains authoritative.

## Consequences

- Route-scoped policy can react once to each committed Activity transition without duplicating the policy in Activity scenes.
- Activity content callbacks retain their existing per-Activity ownership and ordering.
- The mechanism is generic Activity lifecycle observation; it adds no Camera-specific API.
- IF-ADR-040 gains explicit Route-scope Activity transition delivery while retaining feature-specific binding.
- IF-ADR-039 remains the Session Camera command boundary and GameFlow consumer example; its Activity-content placement is replaced by this Route-scoped observation mechanism.

## Rejected scope

- Route or Activity authority over Session Camera state.
- Activity Flow command, veto, polling or subscription/event-bus APIs for the observer.
- Camera-specific transition callbacks or changes to the Session Camera command port.

## Current implementation coverage

- Public immutable context and read-only observer contract added.
- ActivityFlow dispatches after Activity content Exit/Enter callbacks and before Activity finalization/release.
- Route clear dispatches `CurrentActivity == null` before Route content Exit and Route composition release.
- EditMode tests added for startup entry ordering, A->B exactly-once ordering after Activity Exit/Enter, A->None delivery while the Route root remains loaded, and no delivery after Route contribution release. Unity execution remains pending.
- GameFlow adapter moved to one Route contribution in `SCN_GameFlow_Basic`; A/B scene copies removed.
