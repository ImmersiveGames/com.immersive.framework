# Player — Canonical P3 Architecture Flow

Status: Current
Last updated: 2026-07-15

## Authoring

- `PlayerSlotProfile` defines ordered Session capacity and optional default Actor profile.
- `GameApplication` owns the `UIGlobal` composition root. Its single `LocalPlayerProvisioningHostRegistration` references the `LocalPlayerProvisioningAuthoring`, which owns the explicit Unity `PlayerInputManager` endpoint.
- its Player prefab contains `LocalPlayerHostAuthoring`, one `PlayerInput` and an empty Actor Mount.
- `PlayerComposer` authors Actor/input/camera presentation only; it never assigns a Slot.

Required provisioning configuration fails with typed
`LocalPlayerProvisioningIssue` values. The manager uses manual join and C# event
notifications. No singleton, name, tag or hierarchy path is a functional key.

## Runtime flow

```text
GameApplication
  -> UIGlobal
  -> LocalPlayerProvisioningHostRegistration
  -> LocalPlayerProvisioningAuthoring
  -> FrameworkRuntimeHost typed attachment
  -> PlayerSlotProfile
  -> PlayerParticipationRuntimeContext
  -> LocalPlayerProvisioningBridge reservation
  -> PlayerInputManager manual join
  -> LocalPlayerHostAuthoring staged PlayerSlotId
  -> reservation commit
  -> Actor selection/materialization/preparation
  -> gameplay occupancy/input/camera/admission
  -> Activity lifecycle handoff
```

Reservation is created before `JoinPlayer`. Join is single-flight. Commit binds
the typed `PlayerSlotId`; rollback releases the reservation and clears the host
association. A synchronous result may retain callback confirmation as `Pending`;
the late callback updates bridge diagnostics without changing join identity.

`FindObjectsByType` is not a provisioning bootstrap path. The temporary
`LocalPlayerProvisioningAuthoringDiscovery` is migration/diagnostic-only and
must be explicitly enabled and fail closed unless it finds exactly one surface;
normal Pause/InputMode use also requires its
explicit provisioning authoring reference.

## Pause and InputMode

`PauseInputModeUnityPlayerInputRuntimeBridge` receives an explicit
`LocalPlayerProvisioningAuthoring`; it does not discover this dependency from
loaded scenes. Missing required provisioning is rejected. Action maps are
applied to the explicit `PlayerInput` evidence.

## Removed architecture

`PlayerSlotDeclaration`, `PlayerSlotOccupancy`, `SessionPlayerInputManagerDeclaration`
and all F49/F51/F52 passive topology/binding types are removed. They must not be
reintroduced through compatibility components or reflection.

## Manual validation

1. Import and compile Framework and QAFramework.
2. Run P3B, P3G2/G3/G4, P3J2–J6 and P3K2–K7.
3. Run the focused Pause/InputMode provisioning cases.
4. Run P3K.7I and confirm `status='Passed' cases='16'`.
5. Migrate FIRSTGAME only after these gates pass.
