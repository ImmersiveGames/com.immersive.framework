# IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface

Status: **Accepted / Reconciled / Implemented / QA Certified 2026-08-15**  
Last updated: **2026-08-15**  
Related decisions: IF-ADR-002, IF-ADR-003, IF-ADR-006, IF-ADR-010, IF-ADR-012, IF-ADR-014, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021  
Reopen record: [2026-08-14 Player Physical Lifetime Reopen](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-REOPEN-2026-08-14.md)  
Closure record: [2026-08-15 Player Physical Lifetime Recertification](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-RECERTIFICATION-2026-08-15.md)

## Context

Route- and Activity-owned consumers need supported Player operations and immutable Session evidence without becoming Player authority.

The core separates:

```text
Join / Slot allocation
Actor selection
physical Player acquisition/adoption
Activity representation
Session Player Leave
```

## Decision

The package exposes typed scoped consumer access, bounded commands and immutable observation. Existing Session/Player authorities remain the single mutable truth.

## Public command vocabulary

Accepted bounded consumer intent includes:

```text
Open Joining
Close Joining
Request Join
Request Default Actor Selection
Request Leave
```

Separately reconciled bounded commands may include:

```text
Request Join To Slot
Request Actor Selection
```

No command named or shaped as "recreate Player for Activity" is part of the normal Activity transition surface.

## Request Leave

Leave targets:

```text
exact Player Slot
expected current Session Player occurrence/revision
source
reason
```

Successful Leave:

```text
retire current Activity representation when present
release admitted physical Player resources owned by occurrence
terminate Session Player occurrence
commit Slot -> Vacant / Available
```

A stale request for occurrence A cannot affect later occurrence B.

## Session lifetime observation

Observation should distinguish:

```text
Session
  Slot Joined / Available
  current occurrence/revision
  Actor selection/revision
  provisioning origin
  admitted physical Player identity/state
  physical preparation token/evidence

Current Activity
  participating / excluded
  representation Active / Inactive / Absent
  representation occurrence
  readiness
  gameplay/input/camera/context bindings
```

The same admitted physical Player identity may appear across multiple successive Activity representation occurrences.

This is expected and should be diagnosable.

## No-Activity observation

A current Activity reference is not required to prove Session physical truth.

Valid state:

```text
Session Player = Joined
Current Activity representation = Absent
Session physical preparation = Present
physical Player = same retained Session-owned instance
```

Consumer/QA observation must resolve this through canonical Session-scoped occurrence/preparation evidence. It must not infer lifetime from hierarchy shape, `childCount`, scene membership, `FindObjectOfType*` or first-compatible-object search.

`Contextual=Absent` is contextual truth, not physical-destruction evidence.

## Scene-Provided observation

Before successful admission:

```text
physical candidate owner = consumer scene
```

After successful adoption:

```text
physical admitted owner = Session Player occurrence
origin = SceneProvided
```

Observation must not continue reporting the adopted object as externally scene-owned runtime lifetime authority.

## Manager-Provisioned observation

After successful admission:

```text
physical admitted owner = Session Player occurrence
origin = ManagerProvided
```

The post-admission lifetime contract is the same as Scene-Provided.

## Scoped access

Consumer access remains typed, Route/Activity scoped, lifetime-explicit, stale-scope rejecting and free of global lookup.

Activity-scoped access may request/observe Session operations but does not become Session authority.

## Observation integrity

Retained summaries are not automatically current authority.

Current authority is determined through operational state + current scope/occurrence correlation.

Failure evidence must also be read from the layer that owns the failure:

```text
Activity lifecycle / authoring failure
  may block Activity readiness
  may exist before a public Player admission operation/result exists
```

The observation surface must not fabricate a terminal public admission result merely to represent a lifecycle failure that is already canonically exposed through Activity content/readiness evidence.

Likewise:

```text
Route Request succeeded
```

is navigation-commit evidence and is not by itself proof that the startup Activity reached `Ready`.

## Rejected scope

- Direct Slot mutation.
- Direct Actor materialization/recreation by Activity consumer code.
- Implicit physical rebuild on Activity entry.
- Simulating Leave through GameObject destruction.
- Scene unload as Session Leave.
- Hierarchy-shape or global object lookup as physical observation authority.
- Fabricated public failure results for failures owned by another lifecycle layer.
- Global Player manager/service locator.
- Silent fallback between provisioning modes.

## Certification

The 2026-08-15 Full Player QA completed `25/25` mandatory contracts.

The Public Surface phase passes Join, Actor selection, normal lifecycle preparation/materialization/admission, Activity contextual handoff, exclusion/reentry, Leave and Session termination.

The negative matrix additionally certifies:

```text
no-Activity physical evidence is resolved from Session authority
failed first Scene adoption blocks readiness without fabricated admission success
failed contextual reprojection preserves Activity scope without Player physical handoff
no physical handoff occurs on failed contextual paths
```
