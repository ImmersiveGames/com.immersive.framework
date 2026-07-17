# P3M4B2B — Scene Player Actor Adoption

## Objective

Promote a Logical Player Actor that already exists in an Activity scene into the canonical
P3 preparation pipeline without instantiating, disabling or destroying that external
object.

## Type

```text
Runtime contract + product integration + technical QA
```

## Product flow

```text
Scene Local Player Admission
  -> exact Slot/Host admission
  -> explicit ActorProfile selection
  -> ExternalSceneOwned Actor adoption
  -> canonical PlayerActorPreparationSummary
  -> canonical Activity Player lifecycle
  -> gameplay readiness when the remaining gameplay authoring is valid
```

## Ownership model

```text
Local Player Host       ExternalSceneOwned
Scene Logical Actor     ExternalSceneOwned
Technical release proxy FrameworkOwned
RuntimeContent evidence FrameworkOwned and Activity-scoped
```

The technical proxy contains no Actor declaration, gameplay module, presentation or input.
It exists only so the canonical preparation release path has a disposable physical handle.

## Enter transaction

1. Admit the exact configured Player Slot and Host.
2. Commit the explicit ActorProfile selection.
3. Validate serialized Scene Logical Actor evidence.
4. Capture the Actor declaration's authored identity and PlayerInput evidence.
5. Assign contextual runtime Actor identity and Host PlayerInput evidence.
6. Register Activity-scoped RuntimeContent evidence.
7. Create and activate one empty technical release proxy.
8. Register the canonical prepared Actor summary and token.
9. Execute the existing Activity Player lifecycle participant.

Every failure rolls back registration, proxy, contextual declaration evidence, selection and
admission in reverse order.

## Exit transaction

1. Let the canonical Player lifecycle release gameplay and preparation.
2. Finalize the Scene adoption record with the expected typed token.
3. Restore the Actor declaration's authored ActorId, display name, reason and prior
   PlayerInput evidence.
4. Clear the Activity-owned Actor selection.
5. Release Host admission and return the Slot to Available.

The external Host and Actor are never physical release targets.

## Diagnostics

`SceneLocalPlayerAdmissionAuthoring` exposes in Advanced / Debug:

```text
Actor ownership
Adoption status
Adoption token and diagnostic
canonical admission state
```

Required failures are explicit:

```text
invalid authoring
Slot not Joined
selection mismatch
Host mismatch
Actor/evidence mismatch
preparation conflict
foreign or stale adoption token
RuntimeContent registration failure
activation failure
release failure
rollback failure
```

## Scope

Included:

```text
ExternalSceneOwned adoption
canonical preparation registration
LogicalActorsPrepared support
GameplayReady handoff to the canonical pipeline
idempotent enter/exit
foreign/stale token rejection
identity restoration
external object preservation
```

Out of scope:

```text
new gameplay modules
new camera/input authority
new Player provisioning path
FIRSTGAME scene composition
multi-Activity persistence policy for external scene objects
```

## Acceptance

Technical:

```text
compiles in Unity 6.5
B2B smoke passes
B2A, B1, A and canonical regressions pass
no global lookup
no silent fallback
no Runtime -> Editor dependency
external Host/Actor are preserved
no retained proxy, preparation or adoption token after Exit
```

Product:

```text
designer keeps using Scene Local Player Admission
no manual internal contract assembly is added
Advanced / Debug shows external ownership and adoption evidence
LogicalActorsPrepared works with the scene Actor
GameplayReady reaches the canonical gameplay pipeline
```

## Suggested commit message

```text
Feat: adopt external Scene Player Actors canonically
```
