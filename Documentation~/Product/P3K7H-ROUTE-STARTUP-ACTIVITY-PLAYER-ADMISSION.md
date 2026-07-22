# P3K.7H — Route Startup Activity Player Admission Integration

Status: **implementation delta ready for Unity compile and QA**  
Type: **runtime integration + Route lifecycle transaction + technical QA**

## Objective

Integrate the official Session Player gameplay runtime with a real Route switch
whose destination declares a `GameplayReady` Startup Activity.

```text
current Route / Activity / GameplayReady Players
-> validate and stage destination Startup Activity Players
-> reach P3K.7E ReadyToCommit
-> authorize Route transition presentation
-> transfer previous Activity Player exit to the reversible handoff
-> release previous Route content and compose the target Route
-> commit at the destination Startup Activity activation gate
-> adopt exact target P3J/P3K evidence
-> publish destination Route and Startup Activity
-> finalize previous Activity scope
```

## Product flow

The game issues the normal Route request. It does not stage candidates, begin a
handoff group, commit Player ownership or coordinate P3J.6 manually.

```text
FrameworkRuntimeHost.RequestRouteAsync(targetRoute, source, reason)
```

Existing authoring remains authoritative:

```text
RouteAsset.StartupActivity
ActivityParticipationProjectionProfile
PlayerParticipationRequirementsProfile
PlayerSlotProfile
ActorProfile
LocalPlayerProvisioningAuthoring
Player input and camera authoring
```


## Session-persistent Local Player hosts

`PlayerInputManager.JoinPlayer` instantiates the technical Player host in the
currently active Unity scene by default. A Route Primary Scene replacement uses
`LoadSceneMode.Single`; therefore a logically Session-scoped host must not remain
owned by the outgoing Route scene.

Before Slot admission, the provisioning bridge now performs an explicit
physical lifetime transfer:

```text
PlayerInputManager.JoinPlayer
-> resolve exact LocalPlayerHostAuthoring
-> parent host below the persistent FrameworkRuntimeHost
-> verify parent hierarchy and DontDestroyOnLoad scene
-> stage and commit Slot admission
```

Failure to enter that Session lifetime runs the existing join rollback:

```text
rollback staged host admission
release Slot reservation
reject/destroy provisioned PlayerInput host
```

The persistent hierarchy is technical materialization only. Session Player
authority remains in `PlayerParticipationRuntimeContext`.

## Pre-transition admission

Before the Route transition gate or visual presentation:

```text
resolve destination Startup Activity
require GameplayReady
validate current Activity-owned P3J/P3K evidence
create destination Activity RuntimeContent root/context
stage one candidate per projected Slot
begin P3K.7E
require exact ReadyToCommit evidence
```

A failure here rolls back candidates, group progress and the destination
Activity root. No Route transition presentation starts.

## Previous Activity exit transfer

Route lifecycle must exit the previous Activity before replacing its Route
scenes. During P3K.7H that exit does not release P3J/P3K directly.

```text
P3J.6 Exit
-> acknowledge exact Route Startup transaction
-> clear Activity lifecycle ownership record
-> retain previous physical Actor through the reversible P3K.7D lease
-> retain rollback availability until destination commit
```

Diagnostics distinguish this from the same-Route post-commit exit:

```text
SupersededAwaitingCommit
SupersededByCommittedHandoff
```

## Destination activation boundary

After target Route composition and destination Startup Activity scene
preparation:

```text
Activity activation gate
-> commit P3K.7E
-> release previous physical Actor
-> publish destination Startup Activity state
-> P3J.6 adopts exact promoted P3J/P3K tokens
```

Only after the Startup Activity completes does Route lifecycle publish the
destination Route as current.

## Previous Activity scope finalization

The previous Activity root can remain temporarily because the reversible
handoff retains its physical Actor handle until commit.

After destination adoption:

```text
cleanup previous Activity anchor bindings
-> retry exact previous Activity root removal
-> reject Route completion when cleanup still fails
```

## Failure boundaries

```text
before transition authorization
  -> full Player rollback
  -> no visual transition

before P3K.7E commit
  -> Player rollback remains available
  -> existing Route lifecycle failure remains explicit

after any Slot ownership commit
  -> no rollback
  -> target remains authoritative
  -> cleanup failure remains diagnostic
```

P3K.7H does not add silent Route restoration. Existing Route lifecycle remains
responsible for scene/content failure reporting.

## Scope

```text
different current and destination Routes
destination has Startup Activity
destination Startup Activity requires GameplayReady
current Activity exists and owns current GameplayReady Players
one or more projected joined Slots
normal FrameworkRuntimeHost Route request
exact destination P3J.6 adoption
previous Activity root finalization after commit
```

## Out of scope

```text
initial application boot Route
Route without Startup Activity
Route Startup Activity below GameplayReady
Route request with no current Activity
Activity restart
join/leave during transaction
full Route scene/content rollback after teardown begins
FIRSTGAME integration
new designer-facing Route Composer
```

## Diagnostics

`ActivityPlayerLifecycleAdmissionSnapshot` now identifies:

```text
FlowKind
PreviousRouteId and TargetRouteId on the functional admission token
PreviousRouteName and TargetRouteName as diagnostic labels only
previous and target Activity owners
transition authorization
previous exit acknowledgement
target adoption
P3K.7E group and per-Slot tokens
```

Public contracts retain no Unity object references.

## QA

```text
Immersive Framework
  > QA
    > Player
      > P3K.7H Run Route Startup Activity Player Admission Smoke
```

Expected:

```text
[P3K7H_ROUTE_STARTUP_ACTIVITY_PLAYER_ADMISSION_SMOKE]
status='Passed'
cases='48'
```

## Next cut

```text
P3K.7I — FIRSTGAME Player Activity Lifecycle Integration
```

P3K.7I should prove the completed same-Route and Route Startup product flow in
the real consumer before expanding Player admission to additional lifecycle
operations.
