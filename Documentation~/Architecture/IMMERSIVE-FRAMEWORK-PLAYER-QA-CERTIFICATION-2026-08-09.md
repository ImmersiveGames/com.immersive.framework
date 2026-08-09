# Immersive Framework — Player QA Certification

Date: 2026-08-09  
Status: **TECHNICAL QA CERTIFIED**  
Scope: Player Session, Scene-Provided, Manager-Provisioned, Actor lifecycle, public Player surface and Activity participation integration  
QA authority: `rinnocenti/QAFramework`

## Certification verdict

The canonical QAFramework Player orchestrator completed successfully in Unity:

```text
[QA_PLAYER_FULL]
status='Passed'
verdict='PLAYER QA CERTIFIED'
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

## Proven phases

| Phase | Verdict | Representative certified evidence |
|---|---|---|
| Player Session | PASS | Player Participation Authoring — 7 cases |
| Scene-Provided | PASS | Route Transition / Negative Matrix — 25 cases |
| Manager-Provisioned | PASS | Public Contract — 9 cases; Waiting Projection — 14 cases |
| Actor lifecycle | PASS | Actor Selection Runtime Binding — 13 cases; Player Gameplay Admission — 114 cases |
| Public Player Surface | PASS | Q1 — 28 cases; Q2 — 36 cases |
| Activity Participation integration | PASS | Activity Session Projection — 30 cases |

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

The remaining Player gates are product/consumer gates, not missing technical QA:

```text
FIRSTGAME manual real-consumer proof
P5 creation-workflow/tooling disposition after real usage
Session-Persistent Player — separate future contract
Leave / disconnect / reconnect — separate future contract
```

Technical certification does not automatically promote Experimental/preview API stability metadata.

## Provenance note

Package Git baseline inspected while preparing this documentation:

```text
ImmersiveGames/com.immersive.framework
4662fade4e27e2c06b6daf4485d2829e4fb24096
R1 — Consolidar Player Session Authoring
```

QA certification record inspected:

```text
rinnocenti/QAFramework
219cc22e2267d8222da7665807f1175edb64042c
Player QA
```

The QA verdict certifies the package/runtime state actually exercised by the captured Unity run. The package Git SHA above is the documentation inspection baseline and must not be misrepresented as proof that commit `4662fade` alone contains every local R2–R4 implementation edit used during certification.

## Normative interpretation

```text
QA proves the Player technical contracts are green.
FIRSTGAME still proves whether the product is understandable and usable in a real game.
The package remains the official owner of the solution and its public contracts.
```

This document supersedes Player-certification statements in older historical audits when those statements refer to the former Capacity / separate provisioning Profile model.
