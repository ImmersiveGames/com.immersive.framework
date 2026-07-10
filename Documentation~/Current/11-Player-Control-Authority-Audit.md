# 11 — Player Control Authority Audit

Status: **P2A decision closed; P2B authoring/materialization implemented**

Date: 2026-07-10

Scope: `com.immersive.framework`, QAFramework and FIRSTGAME

## Executive decision

P2 will use one runtime authority per authored Player instance:

```text
PlayerComposer
  -> PlayerControlRuntimeContextBehaviour
       owns one PlayerControlRuntimeContext
       key = PlayerSlotDeclaration.PlayerSlotId
       participant = PlayerActorDeclaration.ActorId
```

`PlayerControlRuntimeContext` is the sole authority for bind/unbind and the current control-binding snapshot of that Player. It is not a manager, registry, singleton or service locator. `PlayerComposer` remains authoring-only and must not become runtime authority.

The context remains bound while Pause or Transition blocks gameplay. Gate changes control availability and the Unity action-map state; it does not release PlayerEntry, clear identity or rebuild the binding chain.

## Current state

- `PlayerRecipe` and `PlayerComposer` already own reusable intent, typed `PlayerInput`, gameplay action-map name, PlayerSlot/Actor intent, Validate and idempotent Apply/Rebuild.
- Apply/Rebuild materializes `PlayerActorDeclaration`, `PlayerSlotDeclaration`, Gate adapter and F52 binding/bridge/activation targets.
- `PlayerControl`, `PlayerEntry`, readiness and topology remain passive models.
- F52 adapters can bind, bridge, activate and clear explicitly, but no runtime owner calls the chain as a lifecycle.
- `UnityPlayerInputGateAdapter` blocks the configured action map from combined Pause/Transition Gate snapshots and restores only the state it changed.
- FIRSTGAME has a real PlayerComposer, PlayerInput, Gate adapter, F52 targets, consumer mover, reset participation and CameraComposer.
- QAFramework owns the dedicated P2A-QA0 PlayerComposer Apply/Rebuild regression smoke.

## P2B implementation update

P2B implements authoring and technical materialization only:

- `PlayerRecipe` stores reusable Control enabled, gameplay action map, `BindOnEnable`, requiredness and Gate participation intent; it stores no scene references.
- `PlayerComposer` exposes a dedicated Control section with explicit `PlayerInput` and control target references.
- Required control blocks missing `PlayerInput`, InputActionAsset, action map and control target.
- Validate and Apply block duplicate PlayerSlot/Actor owners and F52 targets outside the canonical Player root / `_Framework/_Bindings`; no component is selected or deleted automatically.
- Apply/Rebuild assigns the materialized `PlayerSlotDeclaration` directly to `UnityPlayerInputGateAdapter.sourceSlot`.
- The second Apply/Rebuild reuses the canonical set; `PlayerComposer` remains authoring-only.

Runtime binding, bind/unbind lifetime, PlayerInput runtime lifecycle and the scoped authority remain P2C-P2E work.

## Inventory and ownership

| Surface | Current category | Evidence | P2 decision |
|---|---|---|---|
| `PlayerRecipe` | RecipeOrProfile | Stores reusable IDs, action map and materialization policy. | Extend only with reusable control defaults; never store scene references. |
| `PlayerComposer` | ComposerOrAuthoring | Validates and materializes technical components. | Main control authoring surface; not runtime authority. |
| `PlayerSlotDeclaration` | Authoring / identity | Stable `PlayerSlotId`; PlayerInput is evidence only. | Canonical binding key. |
| `PlayerActorDeclaration` | Authoring / identity | Stable `ActorId` plus typed PlayerInput evidence. | Canonical actor identity and consistency guard. |
| `PlayerEntry` | PackagePrimitive / passive lifecycle evidence | Slot + Actor + readiness + state. | Context consumes/produces coherent entry evidence; Gate does not release it. |
| `PlayerControl` | PackagePrimitive / passive control evidence | State, slot, target and input-source diagnostics. | Preserve as evidence below the runtime context. |
| F52 adapters/targets | UnityAdapter / TechnicalMaterialization | Explicit bind, bridge, activation and reverse clear. | Reuse or refine behind the context; do not expose as main UX. |
| `UnityPlayerInputGateAdapter` | UnityAdapter | Disables action map or deactivates PlayerInput from Gate. | Canonical P2 default is `DisableActionMap`; do not duplicate Gate policy in movement code. |
| Pause Gate | FrameworkCore | Blocks `Input/InputAcceptance` while paused. | Makes bound control unavailable without unbinding. |
| Transition Gate | FrameworkCore | Mode 30 blocks input and gameplay actions. | Makes bound control unavailable during transition windows. |
| FIRSTGAME mover | Consumer gameplay | Reads `Player/Move` and applies Transform/Rigidbody movement. | Remains game-owned. |

## Architectural decisions

### Runtime authority

Accepted:

- `PlayerControlRuntimeContext`: non-global authority for exactly one PlayerSlot/Actor pair.
- `PlayerControlRuntimeContextBehaviour`: Unity lifecycle adapter generated under the PlayerComposer technical root.
- One canonical binding target set under `_Framework/_Bindings`.
- Explicit dependencies only.

Rejected:

- global Player manager;
- one session-wide dictionary as the public control authority;
- PlayerComposer executing runtime control;
- static lookup, name lookup or “first ready Player” selection as product behavior.

The existing `BindFirstReadyControl` helper remains useful for synthetic QA but is not the canonical product path. Product runtime binds the exact authored PlayerSlot.

### Lifetime

The authority is Player-instance scoped:

```text
OnEnable + valid required dependencies
  -> bind exact Player
  -> remain bound across Pause, Transition and Activity Restart
OnDisable / OnDestroy / explicit Release
  -> unbind in reverse order
  -> release context
```

If the Player GameObject persists, its context persists. If route unload destroys the Player, the context ends. Activity change alone does not imply unbind. Join, respawn and cross-route persistent-player policies remain outside P2.

### Identity sources

- Slot identity: `PlayerSlotDeclaration.PlayerSlotId`.
- Actor identity: `PlayerActorDeclaration.ActorId`.
- Operational input: the exact typed `PlayerInput` referenced by PlayerComposer.
- `PlayerSlotDeclaration.PlayerInputEvidence`, `PlayerActorDeclaration.PlayerInput` and PlayerComposer.PlayerInput must resolve to the same instance when present; mismatch blocks.
- `PlayerSlotOccupancy` and PlayerEntry evidence must match both canonical identities.
- Action-map names are validated configuration, not identity.
- `GameObject.name`, display names and hierarchy labels are diagnostics only.

Runtime context must not fall back to serialized ID strings when declarations are required.

### PlayerComposer control authoring shape

P2B extends the existing product surface instead of creating another facade:

```text
Designer
  Control = Enabled / Disabled
  PlayerInput = explicit reference
  Gameplay Action Map = validated name
  Control Target = explicit Transform (defaults to Player root only when authored policy allows)
  Startup = Bind On Enable (only P2 MVP policy)

Advanced
  canonical technical root
  requiredness
  Gate adapter reference

Debug
  slot / actor
  binding state
  control availability
  PlayerInput / action map
  last operation / failure
```

`PlayerRecipe` may store Control enabled, startup policy, action-map default and requiredness. It must not store concrete PlayerInput or scene Transform references.

The existing `materializePassiveEntryViewControl` flag is not the P2 product switch. P2B must define an explicit migration before changing or removing serialized fields.

### Binding flow

```text
1. Resolve exact typed declarations and PlayerInput from PlayerComposer materialization.
2. Validate unique canonical owners and coherent Slot/Actor/PlayerInput evidence.
3. Build control-specific readiness; PlayerView readiness is not a control prerequisite.
4. Bind PlayerControl evidence to the exact control target.
5. Bridge the exact PlayerInput.
6. Activate the validated gameplay action map.
7. Publish one context snapshot.
```

Unbind is strictly reversed:

```text
1. remove Gate-applied temporary block owned by the Player input adapter;
2. clear action-map activation and restore its captured previous map;
3. clear PlayerInput bridge;
4. clear PlayerControl binding;
5. publish Released.
```

Failure at any bind step rolls back already-applied steps. Partial binding is never accepted.

### Unity PlayerInput bridge

P2D owns only:

- typed PlayerInput association;
- validated gameplay action-map activation/restoration;
- Gate-driven enable/disable of that gameplay map;
- structured diagnostics.

It does not read gameplay actions or pair devices through PlayerInputManager. The P2 canonical Gate mode is action-map disabling, not full PlayerInput deactivation.

Current bridge/activation targets duplicate `expectedPlayerSlotId` as serialized strings. P2 must replace that authority with a typed `PlayerSlotDeclaration` reference or a context-supplied `PlayerSlotId`; the strings may remain diagnostic/migration data only.

### Gate blocking model

Binding state and availability are separate axes:

```text
Binding: Unbound | Binding | Bound | Releasing | Released | Failed
Availability: Allowed | BlockedByGate
```

- Pause contributes `InputAcceptance` blockers.
- Transition can contribute `InputAcceptance` and `GameplayAction` blockers.
- Bound control becomes unavailable while either relevant blocker exists.
- Gate release restores only state changed by the adapter.
- Gate does not mutate PlayerSlot, ActorId, occupancy or PlayerEntry identity.

`UnityPlayerInputGateAdapter` currently polls `FrameworkRuntimeHost.TryGetCurrent`. P2 must not introduce another static/global access path or broaden this pattern. Any new context dependency must be typed and explicit; absence of a required Gate source is a blocking diagnostic, not a silent fallback.

### Movement boundary

Framework owns:

- control authority and binding lifetime;
- typed PlayerInput bridge;
- gameplay action-map availability;
- Gate integration and diagnostics.

Game owns:

- action semantics such as `Move`, `Jump` or `Fire`;
- reading action values;
- CharacterController, Rigidbody, Transform or custom motor;
- acceleration, collision, animation and gameplay rules.

P2 must not create a generic character controller or movement processor.

### Failure and diagnostics

Expected configuration/runtime failures return structured results. They do not escape as normal-control exceptions and do not leave partial state.

Minimum failure kinds:

```text
MissingSlotDeclaration
MissingActorDeclaration
IdentityMismatch
MissingPlayerInput
PlayerInputMismatch
MissingActionAsset
MissingActionMap
DuplicateCanonicalOwner
ControlNotReady
ControlBindRejected
PlayerInputBridgeRejected
InputActivationRejected
GateSourceMissing
RollbackFailed
```

Every result includes slot, actor, operation, previous/current binding state, availability, source, reason and rollback result. Object names may appear only as diagnostic labels.

## QA gap and P2A-QA0 decision

`P2A-QA0 — PlayerComposer Product Surface Regression Smoke` is required before P2B changes PlayerComposer.

Minimum coverage:

| Case | Required evidence |
|---|---|
| Validate | Valid Composer succeeds through the public Editor utility. |
| Apply/Rebuild | Expected declarations, Gate adapter, bindings root and F52 targets are materialized. |
| Idempotency | Second Apply/Rebuild creates no objects/components. |
| Recipe defaults | Reusable defaults copy; concrete PlayerInput/Transforms remain unchanged. |
| Required missing | Missing PlayerInput/action asset/action map and missing required root block explicitly. |
| Duplicate topology | Canonical-owner reuse is proven; duplicate target sets outside the canonical root are reported, never selected silently. |
| Diagnostics | Counts, status and first blocker remain deterministic. |
| No name authority | Renaming Player GameObject does not change identity or selected references; no GameObject.Find/name-based resolution. |

QA0 must call `PlayerComposerApplyRebuildUtility` directly, not use reflection or Inspector-private code. The first run may characterize the current external-duplicate gap; P2B cannot close until the full regression gate is green.

## Current gaps found

1. No runtime owner composes F52 bind/bridge/activation as one transaction.
2. PlayerComposer does not materialize a runtime context.
3. P2A-QA0 now owns the dedicated PlayerComposer regression baseline in QAFramework.
4. Bridge/activation targets duplicate slot identity as strings.
5. P2B now assigns the typed `sourceSlot` reference during Apply/Rebuild.
6. P2B now blocks external duplicate owners/targets without deleting them; FIRSTGAME migration remains explicit P2G work.
7. Activation and Gate both mutate PlayerInput/action-map state without one transaction owner.
8. Existing Gate integration uses static host lookup and Update polling; P2 must not replicate this access pattern.

## Implementation sequence

1. `P2A-QA0` — capture PlayerComposer regression baseline and duplicate-topology gap.
2. `P2B` — add control intent to PlayerRecipe/PlayerComposer and one canonical materialization shape.
3. `P2C` — implement exact-slot transactional binding contracts/runtime; no “first ready” product selection.
4. `P2D` — implement typed PlayerInput bridge, action-map lifecycle and Gate availability.
5. `P2E` — add the Player-instance-scoped runtime context and Unity lifecycle adapter.
6. `P2F` — validate bind, rollback, Gate, Pause, Transition, release and duplicate rejection in QA.
7. `P2G` — migrate FIRSTGAME explicitly and prove consumer-owned movement.

## Risks

- Treating passive snapshots as runtime authority.
- Allowing two components to own PlayerInput state without ordered rollback.
- Reusing `GameObject.name` or “first ready” selection in product runtime.
- Turning PlayerComposer into a runtime manager.
- Making PlayerView readiness a prerequisite for control.
- Automatically deleting duplicate consumer components instead of blocking and migrating explicitly.

## Out of scope

- Runtime or contract implementation in P2A.
- QAFramework or FIRSTGAME changes.
- PlayerInputManager join/device pairing.
- Player spawn/materialization.
- Movement/controller implementation.
- Camera authority changes.
- Save/progression.

## Do not change now

Do not alter frozen technical packages, runtime code, scenes, prefabs, assets or existing QA smokes in this audit cut.
