# IF-ADR-020 — Session Player Leave and Resource Release Authority

Status: **Accepted / Reconciled / Implemented / QA Recertified 2026-08-15**  
Focused QA: **ADR020-H 26/26**  
Current recertification: **Full Player QA 25/25 mandatory contracts**  
Last updated: **2026-08-15**  
Type: architecture / runtime authority / player product direction  
Related decisions: IF-ADR-003, IF-ADR-007, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019  
Reopen record: [2026-08-14 Player Physical Lifetime Reopen](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-REOPEN-2026-08-14.md)  
Closure record: [2026-08-15 Player Physical Lifetime Recertification](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-RECERTIFICATION-2026-08-15.md)

## Context

Session Player Leave is the explicit inverse of one Session Join occurrence.

The 2026-08-14 ADR-019 reconciliation corrected the physical ownership premise used by the former ADR-020 implementation:

```text
Before
  Manager admitted Host = Session-owned
  Scene-Provided Host/Actor = externally scene-owned

Revised
  both provisioning origins converge on Session ownership after successful admission
```

Leave semantics follow that corrected ownership.

## Decision

### 1. Leave is an explicit Session command

```text
terminate exactly one currently Joined Logical Player
from the current Session
```

Leave is not Activity exit, scene unload, Actor deactivation, Closing Joining, device disconnect or Session termination.

### 2. Leave targets exact current occurrence

Destructive mutation requires:

```text
PlayerSlotId
+ expected current Session Player occurrence/revision
```

A stale Leave for occurrence A cannot remove occurrence B after Slot reuse.

### 3. Joining policy controls entry, not exit

```text
Joining Closed
+ P1 Joined
Request Leave P1
  -> allowed
```

Leave does not reopen Joining or auto-Join replacement.

### 4. Leave is staged

```text
validate exact target + occurrence
        ↓
stage Leaving
        ↓
block new Activity contextual authority
        ↓
retire current Activity representation when present
        ↓
release admitted Session-owned physical Player
        ↓
clear occurrence-owned Session state
        ↓
commit Slot -> Vacant / Available
        ↓
publish terminal result
```

Slot vacancy is terminal commit.

### 5. Activity representation releases before physical Session resource release

Retire, as applicable:

```text
gameplay/input admission
Activity Camera requests
readiness contribution
contextual bindings
Activity-local references
representation activation
```

This step is not independently Leave.

### 6. Joined Player without current Activity representation may Leave

Valid precondition:

```text
Joined = true
Physical Player exists = true
Activity representation = Absent / Inactive
```

Leave skips contextual teardown that is already absent and proceeds to physical/session release.

In this state, the retained physical Player is resolved from canonical Session-scoped occurrence/preparation evidence. A missing current Activity reference is not physical-lifetime loss and must not be repaired through hierarchy scanning or fabricated contextual authority.

### 7. Manager-Provisioned release

Manager origin may require provisioning-specific release adapter behavior, but semantic ownership is Session:

```text
Manager-provided admitted physical Player
  -> semantic admitted release
  -> physical teardown
  -> terminal Session commit
```

Rejected-admission cleanup remains distinct from admitted Leave.

### 8. Scene-Provided release — revised and certified

Before successful adoption, the physical candidate is consumer-owned.

After successful adoption, it is Session-owned.

Therefore successful Leave of an adopted Scene-Provided Player releases/destroys the admitted physical representation through the canonical Session ownership boundary.

```text
Scene-provided candidate
  successful adoption
  -> Session-owned

later Leave
  -> release current Activity context when present
  -> release adopted physical Player
  -> end occurrence
  -> Slot vacant
```

A failed admission never transfers ownership and therefore cannot be destroyed as an admitted Session Player.

### 9. Physical destruction settle

Unity physical destruction may settle after the logical release operation. QA must distinguish:

```text
logical terminal Leave result
physical Unity destruction observation
```

but must retain a strong post-settle assertion that Session-owned resources are gone.

### 10. Failure semantics

Before staging, validation failure is non-mutating.

After staging, required release failure must not silently commit Slot vacancy.

Partial irreversible release is reported truthfully and remains correlated to the same occurrence. No generic rollback manager is introduced.

### 11. Rejoin

After successful Leave, a later Join creates a new Session Player occurrence.

No mutable occurrence state is inherited merely because the stable Slot is reused.

## Readiness consequence

If a required Player Leaves:

```text
old readiness evidence becomes stale
authored Activity projection remains according to policy
current occurrence absent
Activity reconciles to WaitingForJoin / Preparing when required
```

## Session termination

Session termination is an aggregate lifecycle operation and releases all remaining Session-owned admitted physical Players.

The 2026-08-15 certification includes Manager-Provisioned and Scene-Provided termination paths, including a retained Scene-Provided physical Player while no current Activity representation exists.

## Rejected behavior

- Treating Activity release as Leave.
- Destroying GameObject as proof of Slot mutation without Session commit.
- Scene unload as Leave.
- Leaving adopted Scene-Provided physical objects alive because of their original source.
- Destroying a Scene-Provided candidate after failed/non-committed adoption.
- Slot vacancy before required release succeeds.
- Stale Leave affecting a later occurrence.
- Requiring a fabricated Activity merely to resolve a no-Activity Leave target.
- Hierarchy shape or global object lookup as physical target authority.
- Global Player manager/service locator.
- Silent rollback/recreation after partial release.

## Certification

ADR020-H `26/26` remains valid focused evidence for:

```text
public Leave command
exact occurrence targeting
Joining Closed semantics
terminal Slot availability
readiness invalidation
rejoin/new occurrence
stale Leave rejection
no-Activity Leave
Manager resource release timing
```

The revised Scene-Provided ownership/release boundary is now additionally certified by the 2026-08-15 Full Player QA terminal result:

```text
PLAYER QA CERTIFIED
mandatoryContracts = 25
executedContracts = 25
passedContracts = 25
```

That run proves:

```text
SceneProvided Leave with Activity reaches terminal contextual/physical/session stages
SceneProvided Leave without Activity resolves the same retained physical instance
SceneProvided Session termination without Activity releases retained resources
Manager Session termination releases remaining resources
stale Leave cannot affect a rejoined occurrence
Activity exit remains distinct from Leave
```

The implementation-reconciliation requirement opened on 2026-08-14 is closed for this boundary.
