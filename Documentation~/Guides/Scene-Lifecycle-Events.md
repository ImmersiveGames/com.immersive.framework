# Scene Lifecycle Events

`SceneLifecycleEvents` is the Inspector bridge for callbacks emitted by the
official `SceneLifecycleRuntime`. Add it to any object in a scene managed by
the framework, then connect `Available` and `Releasing` to explicit UnityEvent
receivers.

The component does not subscribe to `SceneManager`, poll, resolve a runtime
host, or create an independent lifecycle. Zero receivers are valid. Its
Advanced / Debug section exposes the last event and counters.

Each loaded instance receives `Available` at most once until it receives
`Releasing`; therefore an idempotent availability notification does not invoke
the callback again. Exceptions thrown by a receiver reject the lifecycle with
an explicit diagnostic.

For Route and Activity scopes, use `RouteContentContribution` +
`RouteContentLifecycleEvents` and `ActivityContentContribution` +
`ActivityContentLifecycleEvents` respectively. Use `ActivityVisibilityRule` separately when presentation visibility is intended.

## Hierarchy and scope

Scene, Route and Activity authoring components may be placed directly on an
explicit loaded scene root or on any of its descendants, including inactive
objects. Hierarchy is only an authoring organization choice: Route and Activity
assets, and each `localContentId`, remain explicit.

Discovery starts exclusively from the loaded roots supplied by the framework;
it does not perform a global lookup. Route bindings are filtered by their
assigned Route, while Activity adapters are filtered by their assigned
Activity. A callback receiver is dispatched only within its owning binding
scope, so callbacks do not cross Route or Activity boundaries.
