# IF-ADR-019 — Session Player Lifetime and Activity Representation Authority

Status: **Accepted / Reconciled / Implemented / QA Recertified 2026-08-15**  
Previous technical certification: **Historical for superseded Activity-owned physical Actor boundary**  
Current recertification: **Full Player QA 25/25 mandatory contracts**  
Last updated: **2026-08-15**  
Type: architecture / runtime authority / player product direction  
Related decisions: IF-ADR-001, IF-ADR-003, IF-ADR-007, IF-ADR-011, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-020, IF-ADR-021  
Reopen record: [2026-08-14 Player Physical Lifetime Reopen](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-REOPEN-2026-08-14.md)  
Closure record: [2026-08-15 Player Physical Lifetime Recertification](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-RECERTIFICATION-2026-08-15.md)

## Context

The accepted product intention is:

```text
A Player is logically joined for the Session.

The Player is visually/gameplay represented only when an Activity projects it.

Changing Activity must not, by itself, destroy and recreate the Player's physical object.
```

The previous ADR-019 implementation conflated:

```text
Activity owns whether/how Player is represented
```

with:

```text
Activity owns physical Player lifetime
```

That interpretation breaks continuity, especially for seamless Activity transitions.

## Decision

### 1. Joined Logical Player is Session-scoped

A successful Join establishes one Session Player occurrence.

It remains Joined until:

```text
IF-ADR-020 explicit Session Player Leave
or
Session termination
```

Activity exit is not Leave. Activity entry is not a second Join.

### 2. Successful admission also establishes a Session-owned physical Player representation

The admitted physical representation belongs to the current Session Player occurrence.

Conceptually:

```text
Session Player occurrence
├── Slot identity / occupancy
├── Actor selection intent
├── admitted physical Player root
│   ├── Host
│   ├── PlayerInput where local
│   ├── Actor Mount where applicable
│   └── physical Actor / visual hierarchy
└── occurrence/revision evidence
```

Exact technical hierarchy may differ by integration, but lifetime ownership is Session.

### 3. Activity representation is contextual authority over the existing physical Player

Activity owns:

```text
participation projection
active/inactive representation state
gameplay admission
Camera requests
readiness contribution
interaction bindings
Activity-local references
Activity representation occurrence
Activity-owned RuntimeContent scope while the Activity is current
```

Activity does not own terminal physical Player lifetime.

### 4. Physical Player occurrence and Activity representation occurrence are different

Example:

```text
Session Player P1
Physical Player occurrence = 7

Activity A
  Representation occurrence = 31
  uses Physical occurrence 7

Activity B
  Representation occurrence = 32
  uses Physical occurrence 7
```

The new Activity occurrence requires new readiness/gameplay/camera/context evidence even though the physical object is the same instance.

### 5. Activity-to-Activity transition preserves physical identity

Canonical transition:

```text
Activity A contextual authority
        ↓ retire
release gameplay A
release camera A
release readiness A
release bindings A
        ↓
same admitted physical Player remains
        ↓
Activity B contextual authority
bind/activate for B
new readiness evidence
new gameplay/camera/context
```

Normal Activity transition must not implicitly:

```text
Destroy old Actor
Instantiate new Actor
re-Join Player
replace physical identity
```

This is true independently of presentation mode:

```text
Seamless
Fade
Covered
Loading
other transition presentation
```

Transition presentation is not lifetime authority.

### 6. No current Activity representation means inactive/absent, not destroyed

A valid state is:

```text
Session Player = Joined
Physical Player = Exists
Current Activity representation = Absent
Physical presentation = Inactive / not participating
```

A Route with no active Activity can therefore have no contextual Player authority while the admitted physical Player still exists under Session ownership.

Canonical physical truth in this state comes from Session-scoped occurrence/preparation evidence, not from a current Activity reference or hierarchy-shape heuristic.

### 7. Scene-Provided and Manager-Provisioned differ only before admission

#### Manager-Provisioned

```text
Framework supplies candidate
        ↓
successful admission
        ↓
Session owns admitted physical Player
```

#### Scene-Provided

```text
consumer scene supplies exact candidate
        ↓
validate/adopt
        ↓
successful admission
        ↓
Session owns same physical object
```

No clone is required merely to transfer lifetime authority. The intended continuity is the same Unity object instance where technically supported.

A failed Scene-Provided admission does not transfer Player ownership.

### 8. Scene-Provided promotion

A Scene-Provided candidate may originate inside the Activity scene but successful adoption promotes it out of Activity-scene physical lifetime before that scene can unload.

The runtime must provide a canonical Session-owned physical container/scope.

Implementation may use Unity scene migration or another explicit mechanism, but:

```text
DontDestroyOnLoad by itself
```

is not semantic authority.

The authority is:

```text
Session Player occurrence owns the admitted physical object
```

### 9. Later Activities do not replace an existing admitted Scene-Provided Player silently

If the Session already owns an admitted physical Player for P1, a later Activity cannot silently replace it with another scene-authored candidate for the same occurrence.

Possible later authoring behavior must be explicit:

```text
existing Session physical Player
  -> reuse/project it

new conflicting Scene-Provided candidate
  -> reject or report redundant/conflicting evidence
  -> never silently replace
```

### 10. Actor selection survives Activity transitions

Actor selection remains Session mutable intent.

Because physical representation also persists, ordinary Activity transition does not implicitly re-resolve or rematerialize the selected Actor.

Explicit physical Actor replacement/hot-swap remains a separate contract.

### 11. Readiness remains Activity-occurrence scoped

Physical continuity does not permit stale readiness reuse.

Activity B must establish its own:

```text
representation binding
placement decision where applicable
gameplay admission
camera request
readiness contribution
```

A committed Activity may legitimately be current but `NotReady`. Failed Player contextual admission does not automatically roll back the Activity itself.

Therefore a current Activity B may retain its own Activity-scoped `RuntimeContent` root while Player contextual reprojection for B is failed/absent. That root is Activity lifetime, not evidence of a new physical Player or successful Player handoff.

### 12. Physical release boundaries

Ordinary Activity exit is not a physical release boundary.

Canonical terminal boundaries are:

```text
Session Player Leave
Session termination
explicit future physical Actor replacement/hot-swap operation
```

Exceptional unrecoverable failures may require defensive cleanup, but may not redefine normal Activity semantics.

## State model

```text
Not Joined
  physical Player may be absent or candidate-owned externally

Joined + Represented
  Session owns physical Player
  Activity owns current contextual representation
  physical presentation active as required

Joined + Not Represented
  Session owns physical Player
  physical presentation inactive/not participating

Current Activity CommittedNotReady
  Activity may own its RuntimeContent scope
  Player contextual representation may be failed/absent
  Session physical Player remains independently owned

Leaving
  no new contextual authority
  release current Activity context
  release Session-owned admitted physical Player
  terminal commit

Vacant
  no current Session Player occurrence
```

## Ownership invariants

- Session membership and admitted physical lifetime share the same terminal occurrence.
- Activity projection cannot destroy Session-owned physical identity by default.
- Scene-Provided origin does not mean permanent scene ownership after adoption.
- Manager-Provisioned and Scene-Provided converge on the same post-admission lifetime.
- Seamless requires no special persistence switch.
- Current Activity absence is compatible with physical Player existence.
- A new Activity representation occurrence does not imply a new physical occurrence.
- Activity-owned RuntimeContent is not Player physical ownership.
- Failed contextual reprojection must not cause physical handoff, duplicate Actor creation or stale authority reuse.

## Rejected behavior

- Destroy/recreate physical Actor on every Activity transition.
- Scene unload destroying an already adopted Scene-Provided Player.
- `DontDestroyOnLoad` as an unscoped authority.
- Per-Activity physical Player ownership after successful admission.
- Persistence toggle on `PlayerSessionProfile`.
- Silent replacement by a later Scene-Provided candidate.
- Reusing stale Activity readiness because physical identity persisted.
- Re-Join during Activity reprojection.
- Treating Activity-owned RuntimeContent retention as proof of Player physical retention/handoff.
- Hierarchy shape, `childCount`, scene scan or global object lookup as physical-lifetime authority.
- Global Player singleton/service locator.

## Certification

The 2026-08-12 ADR-019 QA remains historical evidence for the former boundary.

The revised contract is now recertified by the 2026-08-15 Full Player QA terminal result:

```text
PLAYER QA CERTIFIED
mandatoryContracts = 25
executedContracts = 25
passedContracts = 25
```

The recertification includes proof that:

```text
SceneProvided A -> B -> A preserves exact physical identity
ordinary Activity handoff preserves pose
new Activity occurrences establish fresh contextual authority
supplying Activity scene release does not destroy the adopted Player
no-Activity contextual absence does not end Session physical preparation
SceneProvided Leave works with and without current Activity
Session termination releases retained physical resources
failed first Scene adoption remains blocking without false readiness
failed contextual reprojection preserves correct Activity scope while denying Player context
no physical handoff occurs on the negative path
```

The implementation-reconciliation requirement opened on 2026-08-14 is therefore closed for this boundary.
