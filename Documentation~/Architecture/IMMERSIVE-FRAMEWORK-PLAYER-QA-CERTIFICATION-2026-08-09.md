# Immersive Framework — Player QA Certification

Date: 2026-08-09  
Status: **TECHNICAL QA CERTIFIED**  
QA authority: `rinnocenti/QAFramework`

## Certification verdict

The canonical Player orchestrator completed successfully in Unity for the current
accepted no-Capacity Player model:

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

Operational entrypoint:

```text
Immersive Framework/QA/Player/Run Full Player QA
```

Focused menus remain diagnostic entrypoints rather than alternative certification
workflows.

## Certified Player Session model

```text
PlayerSessionProfile
├── Supported Slots
├── Initial Joining
├── Host Provisioning
│   ├── Scene Provided
│   └── Manager Provisioned
└── Actor Resolution
```

Not part of the certified contract:

```text
PlayerProvisioningProfile
PlayerSlotProvisioningOverride
Initial / Current / Dynamic Capacity
SetCapacity / SetDynamicCapacity
per-Slot Host Provisioning override
```

Join admission uses Joining Open plus the first vacant Supported Slot in authored
order.

## Serialized command identity

The full certification includes the serialization identity regression:

```text
OpenJoining                         10  PASS
CloseJoining                        20  PASS
retired former Capacity value       30  PASS — unsupported as expected
RequestJoin                         40  PASS
RequestDefaultActorSelection        50  PASS
```

The retired value `30` must never be silently remapped to another supported
command.

## Proven phases

| Phase | Verdict | Representative evidence |
|---|---|---|
| Serialized Command Identity | PASS | 5 cases |
| Player Session | PASS | Player Participation Authoring — 7 cases |
| Scene-Provided | PASS | Route Transition / Negative Matrix — 25 cases |
| Manager-Provisioned | PASS | Public Contract — 9; Waiting Projection — 14 |
| Actor lifecycle | PASS | Actor Selection Runtime Binding — 13; Gameplay Admission — 114 |
| Public Player Surface | PASS | Q1 — 28; Q2 — 36 |
| Activity Participation | PASS | Activity Session Projection — 30 |

Expected error diagnostics from deliberate negative cases are evidence, not a
master certification failure, when the owning regression returns PASS.

## Provisioning-mode isolation

Manager-Provisioned and Scene-Provided use independent certified fixtures and
converge on the same Session/Slot/Actor authority model.

For Manager-Provisioned:

```text
serialized PlayerInputManager player limit
=
PlayerSessionProfile.SupportedSlotCount
```

This bridge is derived technical materialization, not domain Capacity.

## Public surface

Certified commands:

```text
Open Joining
Close Joining
Request Join
Request Default Actor Selection
```

Consumer access is typed/scoped and observation is immutable. Internal Slot
reservation, Actor preparation/materialization, gameplay admission and Activity
reconcile authorities are not public consumer commands.

## Provenance

Documentation/package baseline inspected for the current closure:

```text
com.immersive.framework
43b96a4b100b8273da1190520536007ba82dc081
ADR-010B
```

QA source baseline:

```text
rinnocenti/QAFramework
b6a45728285ddb2ce08269fc1f88ae3f1a4235e4
P0 — Serialized Player Migration Integrity
```

QAFramework references the framework through a local `file:` package path. The
captured Unity verdict is valid execution evidence for the exercised workspace;
the manifest does not independently pin the exact package Git SHA exercised.

## FIRSTGAME boundary

Technical certification is not the real-game integration proof.

Current committed FIRSTGAME Player content is not current-model certified and
must be reauthored/rebuilt against the accepted package surface.

```text
QAFramework
  proves Player technical contracts

FIRSTGAME
  proves the same contracts integrate in a real product
```

UX observations found during that integration are qualitative and are not part of
this technical certification.
