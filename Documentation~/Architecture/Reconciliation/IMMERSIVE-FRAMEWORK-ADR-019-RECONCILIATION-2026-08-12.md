# Immersive Framework — ADR-019 Reconciliation and Technical Certification

Status: **Closed / Certified**  
Date: 2026-08-12  
Decision: [IF-ADR-019 — Session Player Lifetime and Activity Representation Authority](../ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md)

## Purpose

This record reconciles the accepted Session Player lifetime boundary with the current
Player, readiness and consumer-surface architecture and records the package/QA evidence
used to close the technical cut.

The result is intentionally narrower than FIRSTGAME product certification.

```text
Architecture acceptance          Closed
Package implementation           Closed
Focused technical QA             Certified
Full Player regression           Certified
FIRSTGAME real-consumer proof    Pending Stage B
Experimental -> Stable promotion Not implied
```

## Baselines

The architecture/runtime work started from the package repository HEAD:

```text
ImmersiveGames/com.immersive.framework
7bfe77f8371338f1abbc4a1c2d9dd3fa42ce7e04
New ADrs
```

The focused QA reconciliation started from:

```text
rinnocenti/QAFramework
b7a4fc74d0fc1a0fad4443be5d1ca14ac858c8cd
```

ADR-019 package cuts A-D and QA cut E were applied as complete-file local changes after
those repository HEADs. This record does not claim a later Git commit.

## Accepted authority

The closed boundary is:

```text
Session
  Joined Logical Player
  Slot occupancy
  valid Session Actor selection intent
  Manager-Provisioned technical Host after successful Join

Activity
  participation projection
  physical Actor occurrence
  contextual Host/Actor binding where Scene-Provided
  readiness contribution
  gameplay/input/camera bindings
  contextual release
```

Normative consequences:

- Activity exit is not Session Player Leave.
- Activity entry is not a second Join for an already Joined Logical Player.
- A Joined Player may validly have no representation in the current Activity.
- Manager-Provisioned Host/`PlayerInput` is Session-owned after successful Join.
- Scene-Provided Host/Actor remains consumer-scene-owned contextual evidence.
- Actor selection may persist while the physical Actor occurrence changes.
- Session termination releases Session-owned Manager-Provisioned physical resources.
- Initial Placement remains owned by IF-ADR-021.
- Explicit Session Player Leave remains owned by IF-ADR-020.

## Package implementation reconciliation

### ADR019-A — Session Player lifetime foundation

- `PlayerParticipationRuntimeContext` remains the Session membership authority.
- `Joined + current Activity representation Absent` is a valid state.
- No parallel `SessionPlayerManager`, global registry or service locator was introduced.

### ADR019-B — Scene-Provided reprojection

- first Scene-Provided admission into a vacant Slot establishes the Session Join;
- later Activity representation for the same occupied Slot uses contextual reprojection;
- no second reservation/re-Join occurs;
- contextual Activity release does not vacate the Slot;
- valid Session Actor selection survives contextual release;
- conflicting contextual evidence fails explicitly.

### ADR019-C — Manager-Provisioned Session resource lifetime

A distinct technical admitted-release boundary was introduced for successful admitted
Player teardown.

Rejected-admission cleanup and successful admitted-resource release remain semantically
different operations.

At Session provisioning disposal:

```text
admitted Manager-Provisioned Player
  -> semantic admitted release
  -> PlayerInput/Host physical teardown
```

There is no fallback that reuses `RejectPlayer` as the accepted Session teardown
contract.

### ADR019-D — readiness / representation boundary

The effective Player readiness requirement determines whether a physical Activity
representation is required:

```text
None
JoinedSlots
SelectedActors
  -> Session-only evidence
  -> Activity Actor representation not required

LogicalActorsPrepared
GameplayReady
  -> current Activity representation required
```

Immediate entry, deferred readiness and reconciliation use the same boundary.

## Documentation reconciliation

### IF-ADR-001

The former `Session-Persistent Player` deferred direction is resolved by ADR-019.
Core composition ownership remains unchanged: persistence does not create a global Player
authority or permit arbitrary persistent Activity GameObjects.

### IF-ADR-003

Player participation remains Session-scoped. Activity contextual release is explicitly
separated from Session membership and valid Actor selection. Scene-Provided reprojection
and Manager-Provisioned Session Host lifetime now reference ADR-019.

### IF-ADR-007 / IF-ADR-011

Readiness and Loading progress distinguish Session-only requirements from
representation-required requirements. Neither system fabricates missing representation
evidence or blocks Session-only readiness merely because no physical Actor occurrence is
present.

### IF-ADR-012

Activity participation remains a projection of the current Session. Excluding a Joined
Player does not vacate the Slot. Requirement levels define whether a current physical
Activity representation is necessary.

### IF-ADR-015

ADR-019 adds no automatic consumer command. Existing Join semantics remain Session-level;
Activity reprojection is not a consumer re-Join and contextual release is not Leave.
Observation should distinguish Session state from current Activity occurrence evidence.

### IF-ADR-016

Session persistence is canonical runtime behavior, not a `PlayerSessionProfile` option.
Manager-Provisioned and Scene-Provided retain different physical ownership semantics.
Per-Slot Host Provisioning remains deferred.

### IF-ADR-020

ADR-020 remains Proposed, but now depends on the accepted ADR-019 lifetime boundary.
Its future Leave operation terminates one Session Player occurrence; it is not required
to establish ordinary Activity exit semantics.

## QA evidence

### Full Player QA

The complete Player orchestrator passed:

```text
[QA_PLAYER_FULL]
status='Passed'
verdict='PLAYER QA CERTIFIED'
serialization='PASS'
session='PASS'
sceneProvided='PASS'
managerProvisioned='PASS'
actor='PASS'
publicSurface='PASS'
participation='PASS'
```

### Scene-Provided transition / reprojection

The Scene-Provided integration smoke passed 28 cases, including:

```text
route-b-session-player-reprojected-without-rejoin
route-a-reentry-session-player-reprojected-without-rejoin
qa-hub-activity-representation-cleanup-complete
qa-hub-session-player-preserved
```

This proves that different Activity-owned physical occurrences can represent the same
Session Player without a second Join, and contextual cleanup does not imply Leave.

### Session participation authority

```text
[ADR19_1A_SESSION_PARTICIPATION_AUTHORITY]
status='Passed'
authority='PlayerParticipationSnapshot'
```

The canonical Session participation snapshot remains the Join truth.

### Readiness representation boundary

```text
[ADR19_READINESS_REPRESENTATION_BOUNDARY]
status='Passed'
none='SessionOnly'
joinedSlots='SessionOnly'
selectedActors='SessionOnly'
logicalActorsPrepared='ActivityRepresentationRequired'
gameplayReady='ActivityRepresentationRequired'
```

### Joined without gameplay occupancy

```text
[ADR19_1B_JOINED_WITHOUT_GAMEPLAY_OCCUPANCY]
status='Passed'
joined='1'
occupied='0'
gameplayReady='0'
inputBound='0'
```

Session membership remains valid without current gameplay occupancy.

### Joined Slot reuse safety

```text
[ADR19_1C_JOINED_SLOT_NOT_REUSED]
status='Passed'
joined='2'
occupied='0'
```

A second Join consumed a distinct vacant Slot rather than reusing the already Joined
Slot.

### Activity exit preserves Session-owned Host

```text
[ADR19_1E_ACTIVITY_EXIT_PRESERVES_PARTICIPATION]
status='Passed'
joined='1'
playerInputAlive='True'
hostAlive='True'
currentActivity='<none>'
```

This is the direct runtime proof that Activity exit is not Leave and does not tear down
the Manager-Provisioned Session Host.

### Session termination releases Session-owned Host

```text
[ADR19_1D_SESSION_TERMINATION_CLEARS_PARTICIPATION]
status='Passed'
joinedBefore='1'
participationAuthoritiesAfter='0'
playerInputAlive='False'
hostAlive='False'
```

This proves that the same resource intentionally preserved across Activity exit is
released at the Session boundary.

### Focused ADR-019 matrix

```text
[ADR19_SESSION_LIFETIME_MATRIX]
status='Passed'
cases='5'
executionOrder='readiness,B,C,E,D'
sessionTerminated='True'
```

## Expected negative diagnostics

The Full Player QA contains deliberate negative scenarios that log framework errors while
the owning QA case still passes. Examples include cancelled `WaitCovered` readiness and a
deliberately unbound `ActivityRequestTrigger`.

These are not ADR-019 failures. The negative public-surface regression completed:

```text
[QA_PLAYER_SURFACE_02]
status='Passed'
cases='36'
```

## Deferred boundaries

This reconciliation does not close:

- IF-ADR-020 explicit Session Player Leave;
- IF-ADR-021 Activity Player Actor Initial Placement;
- device disconnect/reconnect or reassignment;
- network reconnection;
- per-Slot Host Provisioning;
- physical Actor persistence across Activities;
- FIRSTGAME real-consumer usability/proof;
- API maturity promotion.

## Final disposition

ADR-019 is technically closed.

```text
Join once at Session scope
project into zero or more Activities
release contextual Activity occurrences independently
terminate Session-owned physical resources at Session termination
future explicit Leave terminates one Session Player occurrence
```

No additional package or QA cut is required for the accepted ADR-019 boundary unless a
new regression or contradiction is found.
