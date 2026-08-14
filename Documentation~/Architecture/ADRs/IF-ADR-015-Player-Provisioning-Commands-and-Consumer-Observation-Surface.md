# IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface

Status: **Accepted / Reconciled**  
Last updated: **2026-08-14**  
Related decisions: IF-ADR-002, IF-ADR-003, IF-ADR-006, IF-ADR-010, IF-ADR-012, IF-ADR-014, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021  
Current Player lifetime reconciliation: [2026-08-14 Player Physical Lifetime Reopen](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-REOPEN-2026-08-14.md)

## Context

Route- and Activity-owned consumers need supported Player operations and immutable Session
evidence without becoming Player authority.

The core separates:

```text
Join / Slot allocation
Actor selection
physical Player acquisition/adoption
Activity representation
Session Player Leave
```

## Decision

The package exposes typed scoped consumer access, bounded commands and immutable
observation. Existing Session/Player authorities remain the single mutable truth.

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

No command named or shaped as "recreate Player for Activity" is part of the normal
Activity transition surface.

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

Current Activity
  participating / excluded
  representation Active / Inactive / Absent
  representation occurrence
  readiness
  gameplay/input/camera/context bindings
```

The same admitted physical Player identity may appear across multiple successive Activity
representation occurrences.

This is expected and should be diagnosable.

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

Observation must not continue reporting the adopted object as externally scene-owned
runtime lifetime authority.

## Manager-Provisioned observation

After successful admission:

```text
physical admitted owner = Session Player occurrence
origin = ManagerProvided
```

The post-admission lifetime contract is the same as Scene-Provided.

## Scoped access

Consumer access remains typed, Route/Activity scoped, lifetime-explicit,
stale-scope rejecting and free of global lookup.

Activity-scoped access may request/observe Session operations but does not become Session
authority.

## Observation integrity

Retained summaries are not automatically current authority.

Current authority is determined through operational state + current
scope/occurrence correlation.

## Rejected scope

- Direct Slot mutation.
- Direct Actor materialization/recreation by Activity consumer code.
- Implicit physical rebuild on Activity entry.
- Simulating Leave through GameObject destruction.
- Scene unload as Session Leave.
- Global Player manager/service locator.
- Silent fallback between provisioning modes.
