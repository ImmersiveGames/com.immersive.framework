# Immersive Framework — Player QA Certification

Date: 2026-08-09  
Status: **TECHNICAL QA CERTIFIED**  
Scope: serialized command identity, Player Session, Scene-Provided, Manager-Provisioned, Actor lifecycle, public Player surface and Activity participation integration  
QA authority: `rinnocenti/QAFramework`

## Certification verdict

The previously certified Player phases remain current technical evidence, and the focused serialization regression has independently passed 5/5. After applying this integration, the canonical one-button orchestrator is expected to emit:

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

Operational QA entrypoint:

```text
Immersive Framework/QA/Player/Run Full Player QA
```

Focused menus are diagnostic entrypoints. Manual P3/M07/Q1/Q2 preparation is not the canonical certification workflow.

The exact combined summary above requires the manual one-button retest after applying the QA patch. This document does not invent an unexecuted post-patch Unity result.

## Proven phases

| Phase | Verdict | Representative certified evidence |
|---|---|---|
| Serialized Command Identity | PASS | IF-PLAYER-SERIALIZATION-01 — 5 cases |
| Player Session | PASS | Player Participation Authoring — 7 cases |
| Scene-Provided | PASS | Route Transition / Negative Matrix — 25 cases |
| Manager-Provisioned | PASS | Public Contract — 9 cases; Waiting Projection — 14 cases |
| Actor lifecycle | PASS | Actor Selection Runtime Binding — 13 cases; Player Gameplay Admission — 114 cases |
| Public Player Surface | PASS | Q1 — 28 cases; Q2 — 36 cases |
| Activity Participation integration | PASS | Activity Session Projection — 30 cases |

## Serialized command identity certification

`IF-PLAYER-SERIALIZATION-01` is part of the canonical one-button Player certification and remains independently available through its focused diagnostic menu.

```text
OpenJoining                         10  PASS
CloseJoining                        20  PASS
retired former Capacity value       30  PASS — unsupported as expected
RequestJoin                         40  PASS
RequestDefaultActorSelection        50  PASS

IF-PLAYER-SERIALIZATION-01
  PASS — 5/5
```

The full orchestrator delegates these assertions to `QaPlayerSerializationIdentityRegression.Execute(...)`; it does not duplicate them. A serialization failure stops the master certification before the Play Mode-dependent phases and produces `PLAYER QA NOT CERTIFIED` with failed phase `Serialization Identity`.

## Certified Player Session model

The certification is for the accepted IF-ADR-016 model:

```text
PlayerSessionProfile
├── Supported Slots
├── Initial Joining
├── Host Provisioning
│   ├── Scene Provided
│   └── Manager Provisioned
└── Actor Resolution
```

The following former concepts are **not** part of the certified contract:

```text
PlayerProvisioningProfile
PlayerSlotProvisioningOverride
Initial Capacity
Current Capacity
Dynamic Capacity
SetCapacity
SetDynamicCapacity
per-Slot Host Provisioning override
```

Join admission uses:

```text
Joining Open
+ first vacant Supported Slot in authored order
```

If no Supported Slot is available, rejection is explicit. There is no runtime Capacity fallback.

## Provisioning-mode isolation

The QA run prepared independent provisioning fixtures.

Manager-Provisioned evidence:

```text
application='GameApplication'
session='CanonicalPlayerSessionProfile'
supportedSlots='2'
maxPlayers='2'
```

Scene-Provided evidence:

```text
hostProvisioning='SceneProvided'
supportedSlots='2'
```

This proves Scene-Provided and Manager-Provisioned as peer Session provisioning modes rather than a shared mutable fixture accidentally carrying state between phases.

For Manager-Provisioned Player, the Input System bridge is materialized from the Session structure:

```text
serialized PlayerInputManager player limit
=
PlayerSessionProfile.SupportedSlotCount
```

That bridge is a derived technical constraint. It is not domain Capacity and is not runtime Session authority.

## Public surface certification

The accepted IF-ADR-015 consumer vocabulary is technically certified:

```text
Open Joining
Close Joining
Request Join
Request Default Actor Selection
```

The public surface uses typed scoped access and immutable observation. It does not expose internal Slot reservation, Actor preparation/materialization, gameplay admission or Activity reconcile authorities.

Q2 intentionally exercises negative states including rejected operations, stale/wrong/destroyed scope and unbound triggers. Expected framework error diagnostics emitted by those cases are evidence; Q2 completed 36/36 and returned PASS.

## Actor and participation evidence

The canonical QA run also certifies the distinction between:

```text
Join
Host creation/adoption
Slot admission
Actor selection
Logical Actor preparation
physical Actor materialization
gameplay admission
Activity participation/readiness projection
```

Scene-Provided and Manager-Provisioned both converge on the same Session/Slot/Actor contracts without collapsing Host and Actor identity.

## Package documentation consequence

This certification closes the previous documentation state that said:

```text
Unity validation pending
QA revalidation pending
current consumer vocabulary not yet claimed implemented
```

Those statements are superseded for the tested Player technical surface.

The serialized migration-integrity P0 is technically closed:

```text
Package serialization identity correction  CLOSED
QA serialization regression                 CERTIFIED
P0 technical migration integrity            CLOSED
```

The remaining Player gates are separate product/consumer gates, not missing technical QA:

```text
FIRSTGAME current Player evidence — OPEN / DEFERRED; not current-model certified
next consumer action — redesign/rebuild separately
P5 creation-workflow/tooling disposition after real usage
Session-Persistent Player — separate future contract
Leave / disconnect / reconnect — separate future contract
```

Technical certification does not automatically promote Experimental/preview API stability metadata.

## Provenance note

Package Git baseline inspected while preparing this documentation:

```text
ImmersiveGames/com.immersive.framework
434e73f5aa09377679acc092246c76fa3275dd43
Add Player command serialization identity regression
```

QA source baseline inspected before applying the full-certification integration patch:

```text
rinnocenti/QAFramework
ba06f257f19b7556ca9fe7899f77193a3bcab0d1
Add Player command serialization identity regression
```

The focused `IF-PLAYER-SERIALIZATION-01` 5/5 result is existing Unity evidence. The full-orchestrator integration delivered with this cut still requires the one-button Unity retest. The QA project references the framework through a local `file:` package path, so the package Git SHA above is the documentation inspection baseline and must not be misrepresented as the exact package Git SHA exercised by Unity. No post-patch QA commit SHA is invented: the patch applies on top of `ba06f257`.

## Normative interpretation

```text
QA proves the Player technical contracts are green.
Serialized migration integrity is technically closed.
FIRSTGAME product/consumer proof remains open/deferred and will be redesigned separately.
The package remains the official owner of the solution and its public contracts.
```

This document supersedes Player-certification statements in older historical audits when those statements refer to the former Capacity / separate provisioning Profile model.
