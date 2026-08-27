# IF-ADR-021 — Route Spatial Entry and Activity Explicit Relocation

Status: **Accepted / Reconciled — Model B; IMPLEMENTED / CURRENT QA VERIFIED**
Historical implementation QA: **ADR-021 Initial Placement 9/9**
Route spatial-entry QA: **18/18 PASS**
Activity relocation QA: **23/23 PASS**
Current Full Player aggregate: **27/27 PASS — PLAYER CURRENT AGGREGATE COMPLETE**
Historical Full Player certification: **25/25 preserved**
Last updated: **2026-08-27**
Type: architecture / spatial authority / player product direction
Related decisions: IF-ADR-003, IF-ADR-007, IF-ADR-010, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020
Reconciliation: [2026-08-23 Player Authority and Initial Placement](../Reconciliation/IF-ADR-021-Player-Authority-and-Initial-Placement-Reconciliation-2026-08-23.md)
Runtime robustness reconciliation: [2026-08-27 Activity Player Relocation Fail-Fast and Readiness Recovery](../Reconciliation/IF-ADR-021A-Activity-Player-Relocation-Fail-Fast-and-Readiness-Recovery-2026-08-27.md)
Current certification: [2026-08-24 Player Current Aggregate Recertification](../Reconciliation/IF-PLAYER-CURRENT-AGGREGATE-RECERTIFICATION-2026-08-24.md)

## Context

The historical implementation treated initial placement as Activity-owned discovery
inside `ActivityOwnedScenes`. That proved a narrow exact-binding/no-fallback
boundary, but made `ActivityContentProfile` an accidental prerequisite and excluded
the Route Primary Scene. The profile is optional and the Primary Scene remains
Route-owned.

This ADR accepts Model B to distinguish a Player's baseline spatial introduction to
a Route from an optional contextual relocation by an Activity. It does not reopen
Session Player lifetime, Join, Leave, provisioning or Actor Selection.

## Decision

### Session Player occurrence

Session remains authority for Session state, Slot, Join/Leave, Actor
selection/resolution, provisioning, logical Player occurrence and admitted physical
Player lifetime.

```text
spatial placement
  != new Player occurrence
  != physical lifetime transfer
```

Route and Activity only decide spatial intent for that Session-owned physical Player.

### Route baseline spatial entry

Route owns the **baseline spatial-entry intent** for a Session-owned Player entering
the current Route spatial occurrence. It does not own the Player.

```text
same Session Player occurrence
  != same Route spatial occurrence
```

A Route policy must explicitly choose an equivalent of:

```text
Preserve Current/Authored Pose
Apply Explicit Route Placement
```

Route change evaluates the new Route policy but does not mandate a teleport. The
entry contract applies whether the Player already exists, joins through manager
provisioning while a Route is active, is adopted from a scene, the Route has no
Activity, or the current Activity has `ActivityContentProfile = null`.

When explicit Route placement is required, its normative identity is:

```text
RouteId + PlayerSlotId -> Anchor
```

The eligible discovery scope is only the current Route spatial composition:

```text
Route Primary Scene
current Route Content scenes
```

Activity Content, Persistent Content, editor-open scenes, unrelated loaded scenes
and global discovery are not Route-entry sources. Route identity is never inferred
from scene membership.

### Activity explicit relocation

Activity owns an **optional explicit contextual relocation intent**. It is not
initial spatial introduction, Join, admission, physical Player creation or an
automatic consequence of Activity transition.

```text
Activity transition by default
  -> preserve current pose

Activity explicit relocation
  -> opt-in spatial operation
```

Activity enter, reenter or change alone cannot create a Player/Actor or authorize an
implicit teleport. The normative identity for requested relocation is:

```text
ActivityId + PlayerSlotId -> Anchor
```

This permits distinct Activities in the same Route Primary Scene to have different
anchors for the same Slot. Their bindings are not duplicates of one another.

For the current Activity occurrence, relocation discovery may use only:

```text
current Route Primary Scene
current Route Content scenes
current Activity Content scenes
```

This access does not make Route scenes Activity-owned. Scene ownership never
substitutes `RouteId` or `ActivityId` as the semantic binding identity. Persistent,
unrelated and arbitrary loaded scenes remain excluded.

### Determinism and materialization

For an explicit Route placement, exactly one `RouteId + PlayerSlotId` binding is
required. For explicit Activity relocation, exactly one `ActivityId + PlayerSlotId`
binding is required.

```text
0 exact bindings  -> fail
1 exact binding   -> apply
>1 exact bindings -> fail duplicate
```

Bindings of another Route or Activity are ignored semantically rather than counted as
duplicates. No fallback is permitted through hierarchy, name, tag, first-found,
default anchor, scene membership or arbitrary loaded-scene lookup. Applying a binding
changes world pose only; it does not make an anchor a parent or lifetime owner.

## Readiness and evidence

Explicit Route placement must produce observable spatial-preparation evidence before
the Player is considered spatially prepared for that Route occurrence. Missing,
invalid or duplicate exact bindings fail explicitly.

Activity relocation contributes to readiness only when that Activity has declared an
explicit relocation intent. A non-relocating Activity requests no placement evidence,
moves no Player and preserves pose. Relocation failure must remain observable and must
not silently lose the prior pose. Final API and readiness-level names remain an
implementation decision.

For a projected Slot that is not yet Joined, an Activity may legitimately remain
`WaitingForJoin`; explicit relocation is not failed merely because a future Player may
later require it. Once a projected Slot is Joined and is about to advance into Actor
selection/preparation/materialization, the exact Activity relocation binding must be
preflighted before the first Actor mutation.

```text
Slot not Joined
  -> WaitingForJoin

Slot Joined + ApplyExplicitRelocation
  -> preflight exact ActivityId + PlayerSlotId
  -> only then may Actor selection/preparation/materialization begin
```

Missing, duplicate or invalid exact bindings therefore fail the Activity Player
reconcile before Actor mutation. The failure remains an Activity readiness/lifecycle
failure; a Session Join that already succeeded remains a successful Session operation.
The runtime diagnostic must identify the Activity, ActivityId, Player Slot, policy and
matching-binding count so the authoring defect is directly actionable.

## Provisioning and transition consequences

```text
Manager-Provisioned Join
  -> Session provisions and owns physical Player
  -> Player enters current Route spatial occurrence
  -> Route spatial-entry policy evaluates
```

This works with `ActivityContentProfile = null` and with a Route Primary Scene
anchor. Scene-Provided admission may preserve its authored/current pose through the
Route policy; after adoption the Session owns lifetime and the source scene has no
lifecycle authority.

```text
Route A -> Route B
  -> same Session physical Player may continue
  -> Route B is a new spatial occurrence
  -> B policy decides preserve or explicit placement

Activity A -> Activity B
  -> same physical Player
  -> same pose by default
  -> only B's explicit relocation may change pose
```

The Activity invariant holds when Activities share a Primary Scene, have different
content, or have no `ActivityContentProfile`.

## Rejected scope

- Route ownership of Player lifetime, provisioning, Slot or Actor authority.
- Activity transition as Join, Player/Actor recreation or implicit teleport.
- Reclassifying the Route Primary Scene as Activity-owned.
- Requiring `ActivityContentProfile` for Route spatial entry.
- Persistent Content, arbitrary loaded scenes or Editor-open scenes as default spatial sources.
- Scene membership as Route/Activity semantic identity.
- Fallback to first/default/name/tag/hierarchy/global discovery.
- Reset, respawn, checkpoint or generic teleport semantics hidden inside this contract.

## Historical certification and superseded boundary

The historical ADR-021 implementation and `9/9` QA remain valid evidence for the
former Activity-owned-scene boundary. The historical Full Player `25/25`
certification remains valid for the 2026-08-15 Session physical-lifetime and
continuity boundary it executed.

The following historical scene/discovery clauses are superseded by Model B and are
not evidence for the reconciled contract:

```text
Activity placement evidence -> ActivityOwnedScenes only
AnchorOutsideOwnedSceneRejected
  when “owned” meant exclusively Activity-owned scene
```

`ForeignSceneIgnored`, exact Slot matching, missing-binding failure, duplicate
failure, no fallback, Scene-Provided pose preservation and physical-lifetime
separation remain useful historical evidence only to the extent that their tested
semantics were not replaced above. They are not relabeled as current Model B QA.

## Current implementation coverage

Historical Activity-owned Initial Placement remains historical evidence. The Route
baseline cut is implemented: `RouteLifecycleRuntime` publishes an occurrence only
after Route composition yields `RouteContentDiscoveryScope`; Player preparation
consumes it for prepared Players and late admissions. The Route policy is explicit,
Route-owned discovery is restricted to Primary plus Route-owned content, and the
per-Host guard is keyed by Route occurrence plus physical representation.

Activity explicit relocation is implemented as a separate optional pipeline. It uses
explicit `ActivityId + PlayerSlotId` authoring, discovers only current Route scope
plus current Activity content, applies to existing or subsequently prepared Session
physical Players, and keys idempotence by Activity occurrence plus representation.
No-relocation Activities request no relocation evidence and retain their pose.
Route and Activity evidence remain separate.

The 2026-08-27 robustness cut preflights Activity explicit relocation before Actor
mutation for a Joined projected Slot. Invalid composition returns
`FailedPreparation` with explicit diagnostics and leaves Actor selection, logical
preparation and physical materialization untouched. When the cause later disappears
—for example because the failed Joined Slot leaves—the canonical current readiness
evaluation may recover within the same Activity occurrence according to IF-ADR-007.
An unrelated Session revision does not clear the failure while the invalid Joined
condition still exists.

## Current certification

Replacement certification is complete for the implemented Model B boundary:

```text
Route Spatial Entry
  18/18 PASS
  ADR-021 MODEL B ROUTE SPATIAL ENTRY VERIFIED

Activity Explicit Relocation
  23/23 PASS
  ADR-021 MODEL B ACTIVITY RELOCATION VERIFIED

Full Player current aggregate
  mandatoryContracts=27
  executedContracts=27
  passedContracts=27
  PLAYER CURRENT AGGREGATE COMPLETE
```

The Full Player aggregate also passes Scene-Provided, Manager-Provisioned, Actor
Lifecycle, public surface, Leave, Session termination, failed first-scene adoption,
failed contextual reprojection and no-physical-handoff coverage under the current
runtime composition.

The 2026-08-27 fail-fast/readiness-recovery sequence is additional manual Play Mode
consumer/regression evidence. It does not increment, replace or relabel the technical
QA counts above. See the dated runtime robustness reconciliation for the exact tested
sequence and evidence boundary.

The bootstrap-order correction used by this certification preserves the authority
model: Player Session core is composed first, canonical Game Flow/Route lifecycle
authority next, Player Actor Preparation attaches after that authority exists, and
Route startup follows. QA Play Mode synchronization was aligned to canonical Host
readiness; that synchronization change does not create new runtime authority or alter
this ADR's semantics.

See the current certification record for the exact aggregate disposition and the
preserved historical evidence boundary.
