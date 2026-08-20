# IF-ADR-001 — Core Lifecycle and Runtime Authority

Status: **Accepted / Reconciled**  
Last updated: **2026-08-20**  
Related decisions: IF-ADR-003, IF-ADR-005, IF-ADR-006, IF-ADR-007, IF-ADR-008, IF-ADR-010, IF-ADR-011, IF-ADR-014, IF-ADR-019, IF-ADR-020, IF-ADR-021  
Current Player lifetime reconciliation: [2026-08-14 Player Physical Lifetime Reopen](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-REOPEN-2026-08-14.md)  
Current Editor startup isolation reconciliation: [IF-ADR-001A — Editor Play Mode Startup Isolation](../Reconciliation/IF-ADR-001A-Editor-Play-Mode-Startup-Isolation-2026-08-20.md)

> Current implementation and certification state is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative. The 2026-08-14
> reconciliation corrects the former interpretation that Activity representation
> ownership implied Activity ownership of the Player's physical lifetime.
>
> The 2026-08-20 reconciliation makes the existing rule that Editor authoring
> never becomes runtime authority concrete for Unity Editor Play Mode startup.

## Context

The framework requires explicit owners for application/session composition, Route
and Activity lifecycle, scene/content ownership and feature runtime bindings without
globally discoverable mutable state.

Session-scoped participation may outlive Route and Activity changes. Therefore
contextual gameplay ownership must not be confused with Session participant or physical
Player lifetime.

## Decision

`com.immersive.framework` owns framework-specific lifecycle and product modules.
`FrameworkRuntimeHost` is the internal application/session composition root. It must not
expose a static current-host registry, service locator, hierarchy lookup or implicit
singleton access path.

Runtime dependencies are supplied through narrow typed ports and explicit composition.

```text
Game Application / Session
  -> Session-scoped authorities and participants
     -> Joined Logical Players
     -> admitted physical Player representations
  -> Route
     -> Activity
        -> contextual projection / activation
        -> readiness
        -> gameplay / camera / interaction bindings
```

Route and Activity do not own Session participant identity.

After successful Player admission, Route and Activity also do not own the terminal
lifetime of the admitted physical Player representation.

## Editor Play Mode startup isolation

Unity Editor authoring state is not an implicit runtime composition input.

When Project Settings selects:

```text
Editor Play Mode Startup
  FrameworkStartup
```

the framework owns an Editor-only startup isolation step before ordinary runtime
composition begins.

Canonical flow:

```text
scenes currently open for authoring
        ↓
Play
        ↓
package-owned neutral bootstrap scene
        ↓
FrameworkRuntimeHost
        ↓
Startup Route Primary Scene
        ↓
Persistent Content
        ↓
remaining Route / Activity composition
```

The neutral bootstrap scene is Editor-only infrastructure. It must contain no gameplay,
framework authoring, persistent composition, Camera, EventSystem, Player, GameApplication
or other runtime product object.

For `FrameworkStartup`:

1. scenes currently open for authoring are not admitted as the initial runtime scene set;
2. their GameObjects do not receive Play lifecycle before Framework startup;
3. they cannot contribute arbitrary `DontDestroyOnLoad` state before the framework owns
   application/session composition;
4. `FrameworkRuntimeHost` is created from the neutral bootstrap context;
5. `SceneLifecycle` then materializes the Startup Route Primary Scene through the normal
   runtime lifecycle;
6. application-persistent content is composed only through the explicit Game Application
   / Persistent Content path;
7. failure to resolve the required neutral bootstrap scene is blocking;
8. `FrameworkStartup` must never silently fall back to executing the currently open
   authoring scene.

This is isolation of Editor startup state. It does not create a new runtime authority.

When Project Settings selects:

```text
Editor Play Mode Startup
  CurrentSceneOnly
```

the current Editor scene is intentionally executed and the Framework startup path is
skipped according to the existing Editor Play Mode policy.

`CurrentSceneOnly` is therefore an explicit opt-in to execute authoring scene content,
not a fallback from failed `FrameworkStartup`.

Player/runtime builds are unchanged by this Editor-only mechanism.

## Session Player physical lifetime boundary

IF-ADR-019 is authoritative.

A successful admitted Player has two distinct layers:

```text
Session layer
  Joined Logical Player
  Slot occupancy
  current Player occurrence/revision
  selected Actor intent
  admitted physical Player representation

Activity layer
  participation projection
  active/inactive representation state
  readiness contribution
  gameplay admission
  Camera requests
  interaction/contextual bindings
  Activity-local references
```

The key invariant is:

```text
Activity controls whether/how the Player is represented now.
Activity does not own whether the admitted physical Player continues to exist.
```

Therefore:

```text
Activity A -> Activity B
  same admitted physical Player may continue
  contextual occurrence A retires
  contextual occurrence B begins
  no implicit physical destroy/recreate
```

A joined Player may validly have no current Activity representation:

```text
Session Player = Joined
Physical Player = Exists
Current Activity representation = Absent / Inactive
```

Absence of Activity representation does not mean physical destruction.

## Provisioning ownership

Host Provisioning describes how the physical Player is supplied before admission:

```text
Manager-Provisioned
  Framework creates/provides candidate
  -> successful admission
  -> Session owns admitted physical Player

Scene-Provided
  consumer scene provides candidate
  -> Framework validates/adopts
  -> successful admission
  -> Session owns admitted physical Player
```

Scene-Provided authored origin remains diagnosable, but successful adoption transfers
runtime lifetime ownership into the Session Player occurrence.

This does not authorize global persistence or arbitrary `DontDestroyOnLoad` objects.
Persistence is derived from explicit Session ownership and must be implemented by a
scoped runtime container/mechanism.

## Transition outcome authority

Transition results govern continuation for Game Application startup, Route, Activity,
Activity Clear and Activity Restart.

```text
accepted Transition phase
  -> Completed
  -> or intentional policy/no-visual Skipped

non-accepted Before
  -> do not advance governing lifecycle mutation
  -> preserve previous committed authority
  -> typed pre-commit failure

non-accepted After after commit
  -> never report ordinary success
  -> preserve authority that actually committed
  -> no blind rollback
  -> typed committed-target reveal failure/recovery
```

Physical Player lifetime is not inferred from Transition presentation mode.
`Seamless`, fade, covered and other presentation policies do not become Player lifetime
authorities.

## Transition Gate terminal integrity

Transition Gate is internal operation state, not an externally acquired resource with a
fallible release protocol.

```text
TransitionGateSnapshot
  -> pure Transition Gate state

ActivityEntryReadinessGateSnapshot
  -> Transition Gate + Activity Entry Readiness Recovery Gate

CurrentGateSnapshot
  -> broader operational composition
```

A committed readiness failure may validly leave readiness recovery active after the pure
Transition Gate is clean.

## Object Entry runtime-context projection

Object Entry does not introduce another lifecycle authority.

```text
Game Application / Session / Route / Activity lifecycle
        ↓ authoritative current scope and occurrence
FrameworkRuntimeHost
        ↓ derives
ObjectEntryRuntimeContextSnapshot
        ↓ read-only scoped semantic projection
Object Entry consumers
```

Object Entry declarations/descriptors cannot keep a Route/Activity alive, select an
arbitrary owner, replace occurrence identity, register global services or turn authored
metadata into lifecycle authority.

## Architectural constraints

- Runtime authority is scoped, typed and lifetime-explicit.
- Required invalid configuration fails explicitly and diagnostically.
- Consumer code does not depend on reflection, object-name inference or implicit global lookup.
- No silent fallback may change authority or policy.
- Runtime contexts/services remain scoped rather than globally discoverable.
- Editor authoring never becomes runtime authority.
- Under `FrameworkStartup`, Editor-open authoring scenes are not admitted before framework runtime composition.
- A missing required Editor bootstrap scene blocks `FrameworkStartup`; it never authorizes current-scene fallback.
- Activity representation authority is not physical Player lifetime authority.
- Physical Player persistence is not implemented by arbitrary persistent GameObjects.
- Session Player Leave remains the explicit individual terminal operation under IF-ADR-020.
