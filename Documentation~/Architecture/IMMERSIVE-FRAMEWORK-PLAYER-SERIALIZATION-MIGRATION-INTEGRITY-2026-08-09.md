# Immersive Framework — Player Serialization Migration Integrity

**Date:** 2026-08-09  
**Status:** **P0 TECHNICALLY CLOSED**  
**Type:** technical integrity / serialized migration identity  
**Mode:** closure reconciliation from package + QA evidence; FIRSTGAME remains a separate consumer gate

---

## 1. Objective

Verify whether the ADR-016 Player Session consolidation preserved the semantic
meaning of existing Unity-serialized Player authoring content.

This audit is intentionally narrower than general backward compatibility.

The required invariant is:

```text
Removing an old product concept
must not silently reinterpret an already serialized value
as a different supported operation.
```

The current architecture still rejects:

```text
Capacity
SetCapacity
SetDynamicCapacity
separate PlayerProvisioningProfile
per-Slot Host Provisioning override
```

This P0 does **not** propose restoring those concepts.

---

## 2. Git baselines inspected for closure

```text
com.immersive.framework
  current HEAD:
    434e73f5aa09377679acc092246c76fa3275dd43
    Add Player command serialization identity regression

  pre-R1 comparison baseline:
    cf0a37fbcbf72ad2a08556d6045c908521bfd2c1

QAFramework
  current HEAD before full-certification integration patch:
  ba06f257f19b7556ca9fe7899f77193a3bcab0d1
  Add Player command serialization identity regression

FIRSTGAME / planet-devourer
  current HEAD inspected, not modified:
  796618243c3ca76f70d582f38475320c6461420b
  Demo02 Reajuste
```

`bc851304` is documentation-only for the scope relevant to this audit. The
Player authoring/runtime schema inspected remains the R1 implementation.

The QA project references the framework through a local `file:` package path.
The captured Player QA verdict is valid execution evidence for the state
exercised by that Unity run, but the exact package Git SHA of that worktree is
not pinned by the QA project itself.

---

## 3. Closure verdict

```text
P0 Serialized Player Migration Integrity
  status = TECHNICALLY CLOSED

Package serialization identity correction
  CLOSED

QA serialization regression
  CERTIFIED — IF-PLAYER-SERIALIZATION-01 PASS 5/5

Canonical Player QA serialization gate
  INTEGRATED BY THIS PATCH — requires manual one-button retest

FIRSTGAME current Player proof
  OPEN / DEFERRED — separate redesign/rebuild
```

The audit found a real serialized identity reuse. The package correction restores the historical identities of still-supported commands and retires former Capacity value `30` instead of reusing it. The focused regression proves the five identities. FIRSTGAME was useful evidence for discovering the defect, but its current-model product proof is not a technical P0 closure condition.

---

## 4. Command enum evidence

### 4.1 Pre-R1 schema

At package baseline `cf0a37f`:

```csharp
PlayerProvisioningCommandOperation
{
    OpenJoining                  = 10,
    CloseJoining                 = 20,
    SetCapacity                  = 30,
    RequestJoin                  = 40,
    RequestDefaultActorSelection = 50
}
```

The same component serialized:

```text
requestedCapacity
```

as the input for `SetCapacity`.

### 4.2 Corrected current runtime schema

At current package HEAD:

```csharp
PlayerProvisioningCommandOperation
{
    OpenJoining                  = 10,
    CloseJoining                 = 20,

    // 30 intentionally retired.

    RequestJoin                  = 40,
    RequestDefaultActorSelection = 50
}
```

`requestedCapacity` was removed.

### 4.3 Semantic collision

Therefore:

| Serialized integer | Pre-R1 meaning | Corrected current meaning | Closure result |
|---:|---|---|---|
| `10` | Open Joining | Open Joining | stable |
| `20` | Close Joining | Close Joining | stable |
| `30` | Set Capacity | undefined / unsupported | retired explicitly; no semantic reuse |
| `40` | Request Join | Request Join | stable |
| `50` | Request Default Actor Selection | Request Default Actor Selection | stable |

The critical problem is not that an old value becomes unsupported.

The critical problem is:

```text
old supported operation A
  serialized as integer N

new supported operation B
  reuses integer N
```

Unity serialized content can therefore remain syntactically valid while meaning
something different.

---

## 5. Concrete FIRSTGAME evidence

Current FIRSTGAME contains two
`PlayerProvisioningCommandTrigger` components in:

```text
Assets/_Project/Demo02/Scenes/ManagerProvisionedPlayer/Additive/
  SceneManagerPlayerMenu.unity
```

Observed serialized values:

```yaml
operation: 40
requestedCapacity: 1
```

and:

```yaml
operation: 10
requestedCapacity: 1
```

The presence of `requestedCapacity` proves these component payloads were authored
with the pre-R1 serialized schema.

For that schema:

```text
operation: 40
  = Request Join
```

For the corrected current schema:

```text
operation: 40
  = Request Join
```

Therefore the previously confirmed collision is no longer present in the package enum. This does not certify the rest of FIRSTGAME Player authoring against the current model.

The `operation: 10` component remains semantically stable.

No `operation: 30` or `operation: 50` was found in the inspected
`Demo02 Reajuste` patch content. This statement is scoped to the changed content
inspected for the current FIRSTGAME Player cut; it is not a claim that no such
serialized values can exist in every historical branch or external consumer.

---

## 6. PlayerSessionProfile migration evidence

Current FIRSTGAME also contains:

```text
Assets/_Project/Demo02/Data/PlayerSession/
  Demo02_Session_ManagerProvided.asset
  Demo02_Session_SceneProvided.asset
```

Both still serialize the superseded shape:

```yaml
supportedSlots:
initialCapacity: 1
initialJoiningOpen: 0
playerProvisioningProfile: ...
```

The current package `PlayerSessionProfile` serializes instead:

```text
supportedSlots
initialJoiningOpen
hostProvisioning
actorResolutionPolicy
```

There is no current:

```text
initialCapacity
playerProvisioningProfile
```

This proves that FIRSTGAME is not current evidence for the accepted ADR-016
authoring model.

For the Session Profile fields, this audit does **not** infer an exact Unity
deserialization result for missing/new fields without a real Editor import.
The safe conclusion is:

```text
the current committed assets do not explicitly encode
the accepted Host Provisioning / Actor Resolution shape
and must be deliberately reauthored/re-saved before certification.
```

No silent compatibility fallback should be added.

---

## 7. Why the technical fix belongs to the package

FIRSTGAME exposes the defect, but the collision originates in a serialized
package enum.

The package owns the canonical authoring component:

```text
PlayerProvisioningCommandTrigger
```

Changing the numeric identity of serialized enum members can reinterpret
existing scene/prefab content without producing an unsupported enum.

Therefore the permanent correction belongs in the package and is now present. FIRSTGAME product proof is handled separately and may be redesigned/rebuilt rather than treated as an in-place migration obligation.

---

## 8. Applied package disposition

Preserve the historical numeric identity of still-supported commands and retire
the removed Capacity value without reusing it:

```csharp
PlayerProvisioningCommandOperation
{
    OpenJoining                  = 10,
    CloseJoining                 = 20,

    // 30 intentionally retired.
    // Former SetCapacity serialized value.
    // Do not reuse.

    RequestJoin                  = 40,
    RequestDefaultActorSelection = 50
}
```

Important:

```text
Do NOT restore SetCapacity.
Do NOT add a compatibility Capacity command.
Do NOT map legacy 30 to RequestJoin.
Do NOT silently repair 30 into another supported operation.
```

The current trigger already has the correct failure shape for unsupported enum
values:

```text
TryValidateConfiguration
  unsupported operation -> false + explicit issue

InvokeConfiguredOperation
  default -> diagnostic "operation ... is not supported"
```

That means a retired numeric value can fail explicitly rather than being
silently reinterpreted.

This is serialization identity hygiene, not a compatibility rail for the
superseded Capacity model.

---

## 9. QA certification

The focused migration-integrity regression exists and has produced:

```text
IF-PLAYER-SERIALIZATION-01
PASS — 5/5
```

Certified cases:

```text
legacy serialized 10
  -> Open Joining

legacy serialized 20
  -> Close Joining

legacy serialized 40
  -> Request Join

legacy serialized 50
  -> Request Default Actor Selection

legacy serialized 30
  -> unsupported / validation failure
  -> no Join
  -> no Capacity fallback
  -> explicit diagnostic
```

The focused regression also owns the invariant:

```text
unsupported numeric operation never executes another supported command
```

This QA is about serialized product-contract stability. It complements, rather than duplicates, Q1/Q2 runtime command certification. The canonical `Run Full Player QA` now delegates to `QaPlayerSerializationIdentityRegression.Execute(...)` in Edit Mode before starting Play Mode-dependent phases; the master does not copy these assertions.

---

## 10. Separate FIRSTGAME consumer gate

FIRSTGAME is not modified by this P0 closure and is not claimed current-model certified.

```text
FIRSTGAME current Player evidence
  OPEN / DEFERRED

next consumer action
  redesign/rebuild separately
```

Do not hand-edit YAML or add package compatibility behavior merely to preserve the current consumer state. When the separate consumer redesign begins, it must prove the current package model using official authoring/public surfaces.

---

## 11. ADR impact

### IF-ADR-003

```text
Technical lifecycle
  remains strongly QA-certified.

FIRSTGAME evidence
  current product evidence remains absent/deferred; this no longer means the technical P0 is open.
```

### IF-ADR-012

```text
Technical participation integration
  remains QA-certified.

FIRSTGAME participation proof
  remains absent/deferred until the redesigned consumer composition is proven.
```

### IF-ADR-015

```text
Current command/observation runtime surface
  remains technically QA-certified.

Product/consumer evidence
  remains absent/deferred; serialized migration integrity itself is closed.
```

### IF-ADR-016

```text
Accepted current Session model
  remains implemented and technically QA-certified.

Current FIRSTGAME assets
  are not current-model proof; consumer redesign/rebuild is separate.
```

The P0 does **not** justify lowering package runtime maturity twice. Its main
effect is to invalidate current FIRSTGAME Player product evidence and identify a
specific package authoring-integrity fix.

---

## 12. P0 closure criteria — satisfied

```text
PACKAGE
  supported serialized command IDs preserve semantic identity
  retired Capacity ID is not reused
  unsupported legacy value fails explicitly

QA
  IF-PLAYER-SERIALIZATION-01 passes 5/5
  no silent command remap is possible
  canonical Player QA includes the serialization regression as a required gate

DOCUMENTATION
  P0 technical integrity is recorded CLOSED
  FIRSTGAME consumer proof remains explicitly OPEN / DEFERRED
```

FIRSTGAME current-model product evidence is a separate gate and is not required to keep the package serialization defect technically closed.

---

## 13. Current status after technical closure

```text
Package current technical model          GREEN
Focused serialization QA                 GREEN — 5/5
Canonical Player QA serialization gate   INTEGRATED — manual one-button retest required
Serialized migration integrity           CLOSED
FIRSTGAME current Player product proof   OPEN / DEFERRED
```

Do not treat FIRSTGAME as current evidence for IF-ADR-003, IF-ADR-012, IF-ADR-015 or IF-ADR-016 until the separate redesigned consumer proof exists. That evidence gap no longer reopens the serialized migration-integrity P0.

---

## 14. Suggested commit messages for this closure cut

QA:

```text
Integrate Player serialization identity into full certification
```

Package documentation:

```text
Close Player serialized migration integrity P0
```

No FIRSTGAME commit belongs to this cut.
