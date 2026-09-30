# Game Flow: Route and Activity

## Purpose

Game Flow owns application navigation between Routes and Activities. It does not require a Player to exist before it can select the current Route or Activity.

## Core concepts

- `GameApplicationAsset` names the Startup Route and owns application-scoped configuration.
- `RouteAsset` defines a Route occurrence, including its primary scene, optional Route content and optional Startup Activity.
- `ActivityAsset` defines contextual gameplay inside a Route, including Activity content, Player participation/readiness, optional relocation and transition policy.

A Route is a larger navigation scope. An Activity is the current gameplay context within that Route.

```text
GameApplication / Session
  └─ current Route
       └─ current Activity (optional)
```

## Supported composition and setup

1. Create the application, Route and Activity assets.
2. Assign the initial Route to `GameApplicationAsset > Startup Route`.
3. Configure each Route’s primary scene, its optional Route content profile and optional Startup Activity.
4. Configure each Activity’s content profile, Player participation projection and readiness level only when the Activity needs them.
5. Place `RouteRequestTrigger` or `ActivityRequestTrigger` in a scene whose lifetime should own that request surface.
6. Connect the trigger’s public request method to a UnityEvent or UI control.

`RouteRequestTrigger` requests its configured target Route. `ActivityRequestTrigger` requests its configured target Activity; its clear operation removes the current Activity while leaving the Route current. Triggers bind to the exact active scene/runtime scope and are released with that scene.

## Runtime behavior and ownership

- The startup Route is requested during application boot.
- A Route change runs the destination Route lifecycle and then its configured Startup Activity, if any.
- An Activity change replaces contextual Activity authority inside the current Route. It does not create a new Route.
- Route and Activity content profiles declare their respective scene content and release behavior. Content scene lifetime follows the owning scope.
- Player Session membership and admitted physical Player lifetime are Session-scoped. A normal Activity transition does not re-Join, Leave, destroy or recreate that physical Player.
- A Route transition also leaves the Session Player joined. The physical occurrence persists across Route and Activity changes unless the consumer explicitly leaves or the Session ends.
- Activity participation, gameplay admission, readiness and Activity-local bindings are contextual. The next Activity establishes its own evidence.
- Route placement is independent from Activity relocation. Route Spatial Entry runs for a new Route occurrence; Activity relocation is an explicit Activity policy for an already admitted Player.
- Validation mode changes diagnostic severity, not which Route or Activity becomes authoritative.

An Activity may be current but not ready. Navigation admission and `GameplayReady` are separate: missing Player readiness can leave the destination committed as `NotReady` with gameplay capabilities gated. A handoff is used only when the origin has complete, compatible `GameplayReady` evidence. Normal absence of Player evidence uses the target’s normal lifecycle; contradictory partial handoff evidence fails before authority commit.

Request outcomes distinguish failure before commit from failure after commit. `FailedBeforeCommit` retains origin authority. `CommittedNotReady` and `CommittedFinalizationFailed` retain destination authority and report its incomplete terminal state.

## Common mistakes

- Treating Activity as a Route or expecting an Activity request to load another Route.
- Making a Player a prerequisite for all navigation.
- Treating Activity readiness failure as if navigation had rolled back.
- Assuming a Player is destroyed when Activity-owned gameplay context ends.
- Using a scene scan or global object lookup to find request/runtime authority.
- Putting a long-lived navigation trigger in a scene that unloads before it is needed.

## Public surfaces

- Authoring assets: `GameApplicationAsset`, `RouteAsset`, `ActivityAsset`, `RouteContentProfileAsset` and `ActivityContentProfileAsset`.
- Requests: `RouteRequestTrigger` and `ActivityRequestTrigger`.
- Activity readiness detail: [Activity Readiness](Activity-Readiness.md).

## Related architecture

- [IF-ADR-001 — Core lifecycle and runtime authority](../Architecture/ADRs/IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md)
- [IF-ADR-006 — Loading, transition persistence and diagnostics](../Architecture/ADRs/IF-ADR-006-Loading-Transition-Persistence-and-Diagnostics.md)
- [IF-ADR-007 — Activity entry readiness and reveal gating](../Architecture/ADRs/IF-ADR-007-Activity-Entry-Readiness-and-Reveal-Gating.md)
- [IF-ADR-019 — Session Player lifetime and Activity representation](../Architecture/ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md)
- [IF-ADR-021 — Route entry and Activity relocation](../Architecture/ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md)
- [IF-ADR-038 — Camera participation is contextual, selection is Session-owned](../Architecture/ADRs/IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md)
