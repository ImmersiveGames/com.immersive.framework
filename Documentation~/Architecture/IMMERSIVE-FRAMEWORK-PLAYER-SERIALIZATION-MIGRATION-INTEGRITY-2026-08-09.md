# Immersive Framework — Player Serialization Migration Integrity

**Date:** 2026-08-09  
**Status:** **P0 CONFIRMED / OPEN**  
**Type:** technical integrity / migration / real-consumer compatibility  
**Mode:** Git read-only audit; no repository changes performed

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

## 2. Git baselines inspected

```text
com.immersive.framework
  documentation HEAD:
    bc851304347df0b8460affaa2695fdba5a32fbe6
    Docs

  Player runtime migration baseline:
    4662fade4e27e2c06b6daf4485d2829e4fb24096
    R1 — Consolidar Player Session Authoring

  pre-R1 comparison baseline:
    cf0a37fbcbf72ad2a08556d6045c908521bfd2c1

QAFramework
  219cc22e2267d8222da7665807f1175edb64042c
  Player QA

FIRSTGAME / planet-devourer
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

## 3. Verdict

```text
P0 Serialized Player Migration Integrity
  status = FAILED / DEFECT CONFIRMED
```

A serialized `PlayerProvisioningCommandOperation` numeric value changed semantic
meaning across R1.

The current FIRSTGAME contains a concrete affected component.

This is stronger than "FIRSTGAME has not been revalidated". The current consumer
contains serialized content whose authored meaning can be reinterpreted by the
new enum mapping.

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

### 4.2 R1/current runtime schema

At R1/current runtime:

```csharp
PlayerProvisioningCommandOperation
{
    OpenJoining                  = 10,
    CloseJoining                 = 20,
    RequestJoin                  = 30,
    RequestDefaultActorSelection = 40
}
```

`requestedCapacity` was removed.

### 4.3 Semantic collision

Therefore:

| Serialized integer | Pre-R1 meaning | R1 meaning | Migration result |
|---:|---|---|---|
| `10` | Open Joining | Open Joining | stable |
| `20` | Close Joining | Close Joining | stable |
| `30` | Set Capacity | Request Join | **silent semantic remap** |
| `40` | Request Join | Request Default Actor Selection | **silent semantic remap** |
| `50` | Request Default Actor Selection | undefined | explicit/invalid if preserved as 50 |

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

For the current schema:

```text
operation: 40
  = Request Default Actor Selection
```

Therefore the `operation: 40` component is a **confirmed semantic migration
collision**.

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

## 7. Why this is a package integrity issue, not only a FIRSTGAME issue

FIRSTGAME exposes the defect, but the collision originates in a serialized
package enum.

The package owns the canonical authoring component:

```text
PlayerProvisioningCommandTrigger
```

Changing the numeric identity of serialized enum members can reinterpret
existing scene/prefab content without producing an unsupported enum.

Therefore the permanent correction belongs in the package.

FIRSTGAME should then reauthor/re-save its current Player assets as real-consumer
proof.

---

## 8. Recommended package disposition

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

## 9. Required QA cut

After the package correction, QA should add one focused migration-integrity
regression.

Suggested cut:

```text
IF-PLAYER-SERIALIZATION-01
```

Cases:

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

Also prove:

```text
unsupported numeric operation never executes another supported command
```

This QA is about serialized product-contract stability. It complements, rather
than duplicates, Q1/Q2 runtime command certification.

---

## 10. Required FIRSTGAME cut

After package + QA:

```text
1. Open FIRSTGAME against the corrected package.
2. Reauthor Demo02_Session_SceneProvided.
3. Reauthor Demo02_Session_ManagerProvided.
4. Explicitly author:
     Supported Slots
     Initial Joining
     Host Provisioning
     Actor Resolution
5. Re-save the assets in Unity.
6. Verify command triggers in Inspector:
     Open Joining
     Request Join
     Request Default Actor Selection where actually intended
7. Validate Scene-Provided flow.
8. Validate Manager-Provisioned flow.
9. Validate command/status surface using only official package surfaces.
10. Record the real-consumer evidence.
```

Do not hand-edit YAML as the normal migration flow. The point of FIRSTGAME is to
prove the current authoring experience a real user receives in Unity.

---

## 11. ADR impact

### IF-ADR-003

```text
Technical lifecycle
  remains strongly QA-certified.

FIRSTGAME evidence
  blocked until current Player authoring is reauthored and exercised.
```

### IF-ADR-012

```text
Technical participation integration
  remains QA-certified.

FIRSTGAME participation proof
  is not current until the underlying Session/Player composition is current.
```

### IF-ADR-015

```text
Current command/observation runtime surface
  remains technically QA-certified.

Product/consumer evidence
  blocked by a real serialized command-authoring collision.
```

### IF-ADR-016

```text
Accepted current Session model
  remains implemented and technically QA-certified.

Current FIRSTGAME assets
  do not encode the accepted model and must be reauthored.
```

The P0 does **not** justify lowering package runtime maturity twice. Its main
effect is to invalidate current FIRSTGAME Player product evidence and identify a
specific package authoring-integrity fix.

---

## 12. P0 closure criteria

P0 closes only when all of the following are true:

```text
PACKAGE
  supported serialized command IDs preserve semantic identity
  retired Capacity ID is not reused
  unsupported legacy value fails explicitly

QA
  migration-integrity regression passes
  no silent command remap is possible

FIRSTGAME
  Demo02 Scene-Provided Profile uses current fields
  Demo02 Manager-Provisioned Profile uses current fields
  command triggers are intentionally reauthored
  Scene-Provided works
  Manager-Provisioned works
  current command/status surface works

DOCUMENTATION
  completion summary records P0 closure
  FIRSTGAME evidence is only promoted after the real Unity proof
```

---

## 13. Current status after Git audit

```text
Package current technical model          GREEN
Canonical Player QA                      GREEN
Serialized migration integrity           RED / P0 OPEN
FIRSTGAME current Player product proof   BLOCKED
```

This P0 should be resolved before treating FIRSTGAME as evidence for
IF-ADR-003, IF-ADR-012, IF-ADR-015 or IF-ADR-016.

---

## 14. Suggested future commit messages

Package:

```text
Fix Player command serialized operation identities
```

QA:

```text
Add Player command serialization migration regression
```

FIRSTGAME:

```text
Migrate Demo02 Player authoring to current Session model
```

Documentation:

```text
Reconcile ADR completion after Player serialization audit
```
