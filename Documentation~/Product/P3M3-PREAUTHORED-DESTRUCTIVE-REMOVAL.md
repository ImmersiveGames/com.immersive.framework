# P3M3 — Destructive Removal of the Alternative Player Composer

Status: Source prepared; Unity validation pending
Date: 2026-07-17

## Objective

Delete the experimental alternative Player authoring lane after P3M2 proved that canonical Camera and Player flows no longer need it.

## Type

```text
destructive removal
technical cleanup
product-surface consolidation
QA cleanup
```

## In scope

```text
remove Composer runtime component
remove reusable Recipe asset
remove custom Inspector
remove editor Apply/Rebuild materializer
remove dedicated P3B smoke
remove temporary Camera compatibility member
make camera eligibility consume resolved target evidence
repair legacy QA scene Missing Scripts explicitly
update current-state documentation
```

## Out of scope

```text
promoting Scene Local Player Admission
changing PlayerInputManager join authority
changing Slot allocation
changing ActorProfile materialization
changing Camera precedence
FIRSTGAME integration
```

## Canonical surface after removal

```text
Manual local join
  LocalPlayerProvisioningAuthoring
  -> PlayerInputManager
  -> LocalPlayerHostAuthoring
  -> Player preparation and gameplay admission

Camera
  explicit transforms or Actor-owned ICameraTargetSource
  -> CameraRigComposer
  -> eligibility validates resolved targets
  -> CameraRequest
  -> CameraOutputContext
```

## Removed authority

The deleted Composer was editor-first materialization and never became P3 runtime authority. It must not be adapted into join or replaced by a compatibility wrapper.

## Failure behavior

```text
missing required Follow target -> explicit rejection
invalid target-source component -> explicit rejection
resolved targets differ from Player authoring -> explicit rejection
old QA serialized component -> Missing Script removed by explicit setup repair
```

No silent target fallback, object-name lookup, tag lookup, hierarchy search, singleton or service locator is introduced.

## Technical acceptance

```text
package compiles
QA compiles
no deleted symbol in Runtime or Editor source
no P3B menu
C9M 6/6 PASS
C9R 11/11 PASS
canonical P3 31/31 PASS
no Missing Script
```

## Product acceptance

```text
only one supported Player creation/admission direction remains visible
Camera remains independently authorable
removed menu and asset type are no longer offered
future Scene Local Player Admission starts from the canonical P3 domain
```

## Architectural gain

A second Player product model and its editor-only materialization authority are eliminated. Camera eligibility now depends on resolved target evidence rather than historical authoring identity.

## Usability gain

Designers no longer see two competing Player setup flows. The remaining path is the P3 participation and ActorProfile lane, with Scene Local Player Admission reserved for its dedicated product cut.

## Suggested commit

```text
Remove: delete PreAuthored Player surface
```
