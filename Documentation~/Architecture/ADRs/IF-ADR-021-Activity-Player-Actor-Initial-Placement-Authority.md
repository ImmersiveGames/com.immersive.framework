# IF-ADR-021 — Activity Player Actor Initial Placement Authority

Status: **Proposed / Reopened Before Acceptance / Current Implementation Requires Reconciliation**  
Last updated: **2026-08-14**  
Type: architecture / product authoring / runtime integration  
Related decisions: IF-ADR-003, IF-ADR-007, IF-ADR-010, IF-ADR-012, IF-ADR-016, IF-ADR-019, IF-ADR-020  
Reopen record: [2026-08-14 Player Physical Lifetime Reopen](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-REOPEN-2026-08-14.md)

> The package HEAD reviewed on 2026-08-14 contains ADR-021 implementation work, but the
> architecture must be reconciled before that implementation can be certified. The former
> assumption "new Activity representation normally means new physical Actor occurrence"
> is superseded by revised IF-ADR-019.

## Context

Initial Placement answers:

```text
Where should the admitted physical Player be positioned/rotated
when an Activity intentionally establishes a new spatial starting decision?
```

It does not own Actor creation or physical Player lifetime.

The revised Player lifetime contract means Activity transition does not automatically
create a new physical Actor and therefore must not automatically imply a teleport.

## Decision

### 1. Initial Placement is spatial authority, not lifecycle authority

Placement controls:

```text
world position
world rotation
diagnostic placement evidence
```

It does not:

```text
Join Player
select Actor
instantiate/destroy physical Player
transfer Player lifetime ownership
reparent to anchor
perform respawn/checkpoint logic
```

### 2. Placement Anchor is Activity-scoped evidence

An Activity may author explicit Slot-to-Anchor intent.

```text
Activity Player Initial Placement
  Player1 -> Anchor A
  Player2 -> Anchor B
```

Bindings are exact and deterministic. No name/tag/nearest/first fallback.

### 3. Activity-to-Activity continuity is preserved by default

Canonical ordinary transition:

```text
Activity A
  physical Player at outgoing gameplay pose
        ↓
Activity B
  same physical Player
  preserve current pose by default
```

Activity B does **not** automatically apply a fresh Initial Placement solely because B
has a new representation occurrence.

This is required for seamless continuity and remains true for other transition
presentation modes unless explicit placement intent says otherwise.

### 4. Placement requires an explicit placement reason

Placement may be applied when the runtime has a defined spatial start boundary, for
example:

```text
first physical Player introduction into active gameplay
explicit Activity transition placement policy
explicit portal/teleport/start-at-anchor operation
explicit restart policy that requires new starting pose
```

The exact product vocabulary for non-initial teleport/portal behavior may be a separate
contract. This ADR must not silently overload every Activity entry as "spawn."

### 5. First representation / first gameplay introduction

When an admitted physical Player has not yet received a valid gameplay spatial pose for
the relevant world context, an Activity may apply its exact Slot binding before readiness.

```text
physical Player exists
        ↓
Activity projects Player
        ↓
placement required for this introduction
        ↓
exact anchor by Slot
        ↓
position/rotation applied
        ↓
placement evidence
        ↓
gameplay/readiness continues
```

### 6. Preserve-current-pose is the transition default

For an already spatially active physical Player moving between Activities in the same
continuous gameplay context:

```text
Preserve Current Pose
```

is canonical.

No Activity may silently reset to:

```text
world origin
prefab pose
Host pose
new Activity anchor
```

merely because the Activity occurrence changed.

### 7. Scene-Provided authored pose

Before adoption, Scene-Provided authored Transform is valid candidate evidence.

At first adoption/first gameplay introduction, product policy may explicitly choose:

```text
Preserve Authored Pose
or
Apply Activity Placement
```

After adoption, later Activity transitions operate on the same Session-owned physical
Player and follow the same continuity rule as Manager-Provisioned.

Scene-Provided is not permanently special after admission.

### 8. Manager-Provisioned first pose

A Manager-Provisioned candidate has no implicit gameplay world pose merely because
`PlayerInputManager` created a Host.

If first gameplay introduction requires placement, exact Activity Slot-to-Anchor evidence
must resolve before readiness.

### 9. Placement target

The semantic target is the physical Logical Actor/world representation, not the technical
input Host as a hidden spatial authority.

Placement changes world pose and does not reparent to the anchor.

### 10. Occurrence evidence

Placement evidence is correlated to:

```text
Session Player occurrence
physical Player identity
Activity representation occurrence
placement decision/reason
anchor identity when used
applied pose
```

New Activity representation occurrence invalidates old Activity readiness/context evidence
but does not automatically invalidate the physical pose.

### 11. Readiness

When placement is required for the current transition/introduction, missing or invalid
exact placement blocks the relevant readiness level.

When placement is not required because continuity is preserved, the absence of a new
placement operation is not a failure.

### 12. Reset / restart / respawn boundary

Initial Placement is not generic Reset or Respawn.

A Restart that explicitly establishes a new spatial starting decision may apply placement
according to Restart policy. A preserved occurrence is not automatically repositioned.

Respawn/checkpoint remains separate.

## Authoring requirements

The designer-facing surface must expose enough intent to answer:

```text
Does this Activity require a placement decision for this Player?
If yes, which exact Slot?
Which exact Anchor?
Why will the pose change?
```

No hidden placement on every Activity entry.

## Rejected behavior

- Treating every Activity entry as physical Actor spawn.
- Destroy/recreate Actor to apply placement.
- Teleporting on every Activity change.
- Outgoing pose always discarded.
- Scene-Provided permanently using different post-admission lifetime semantics.
- Anchor as parent/lifetime owner.
- Name/tag/nearest/global lookup fallback.
- Placement becoming Respawn or generic Spawn manager.

## Implementation impact

The current ADR21Build implementation must be audited for assumptions that:

```text
new Activity occurrence
  -> new physical Actor
  -> mandatory placement
```

Those assumptions are no longer authoritative.

Certification is blocked until the implementation proves that Activity-to-Activity
physical identity and pose continuity are preserved unless explicit placement intent
requires movement.
