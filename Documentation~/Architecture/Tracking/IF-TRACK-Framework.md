# IF-TRACK — Immersive Framework

Status: Active  
Last updated: 2026-08-09  
Package version: `1.0.0-preview.17`

## Source baseline

```text
com.immersive.framework
  Git baseline inspected for this documentation:
  4662fade4e27e2c06b6daf4485d2829e4fb24096
  R1 — Consolidar Player Session Authoring

QAFramework
  certification record baseline:
  219cc22e2267d8222da7665807f1175edb64042c
  Player QA
  canonical Unity Player QA executed 2026-08-09

FIRSTGAME
  last documented baseline:
  ab1bfe65c09af8988c2fe21ce06db780fe12aa70
  Demo03Etapa04
```

Git baselines identify repository states inspected for documentation. The Player QA verdict is Unity execution evidence and certifies the package/runtime state exercised by that run. It must not be misrepresented as a claim that package Git commit `4662fade` alone contains every local R2–R4 implementation edit used during certification.

## Summary

The package has one internal application/session composition root and explicit typed feature boundaries. Product areas include lifecycle, Player, Camera, Pause/Input/Gate, Reset, Activity readiness, Loading/Transition, persistence foundations and diagnostics.

`FrameworkRuntimeHost` remains Internal. Required runtime dependencies are supplied through typed bindings and explicit scope/lifetime. There is no public static host registry or service-locator API.

### Player status — 2026-08-09

The canonical Player technical surface is now QA-certified against the accepted no-Capacity Session model:

```text
Player Session                         PASS
Scene-Provided                        PASS
Manager-Provisioned                   PASS
Actor lifecycle                       PASS
Public Player Surface                 PASS
Activity Participation integration    PASS

Final verdict
  PLAYER QA CERTIFIED
```

Representative evidence:

```text
Player Participation Authoring        7 cases PASS
Scene-Provided route/negative matrix  25 cases PASS
Manager public contract               9 cases PASS
Manager waiting projection            14 cases PASS
Actor selection runtime binding       13 cases PASS
Player gameplay admission             114 cases PASS
Public Surface Q1                     28 cases PASS
Public Surface Q2                     36 cases PASS
Activity Session Projection           30 cases PASS
```

The previous `29/29` Q1 + Capacity-era certification is historical evidence only. The current Q1 result is `28/28` under Supported-Slots semantics.

See `../IMMERSIVE-FRAMEWORK-PLAYER-QA-CERTIFICATION-2026-08-09.md`.

## Track board

| Track | Real status | Proven coverage | Pending work | Next action |
|---|---|---|---|---|
| Runtime authority | Closed for current boundary | internal host composition; narrow typed ports | preserve boundary | reject static/global lookup |
| Package hygiene | Closed for current boundary | package + QA import discipline | ongoing | do not restore compatibility facades |
| Player — Session | **Technical QA certified** | Supported Slots, Initial Joining, uniform Host Provisioning, Actor Resolution, frozen effective config, first-available Slot admission | FIRSTGAME product proof | preserve accepted IF-ADR-016 model |
| Player — Scene-Provided | **Technical QA certified** | independent Scene-Provided fixture; Route/Activity ownership; Slot/Host/Actor lifecycle; release/reentry negatives | FIRSTGAME/manual product walkthrough | keep PlayerInputManager out of this mode |
| Player — Manager-Provisioned | **Technical QA certified** | derived PlayerInputManager bridge, public contract, waiting projection, Join/Host/Slot flow | FIRSTGAME manual product proof | prove manual real-consumer composition |
| Player — Actor lifecycle | **Technical QA certified** | selection, preparation, materialization, gameplay admission and lifecycle separation | Leave/disconnect separate | preserve Host != Actor |
| Player — Public Surface (IF-ADR-015) | **Implemented + technical QA certified** | scoped access, immutable observation, Open/Close, Request Join, default Actor selection, negative stale/unavailable scope | FIRSTGAME proof + P5 disposition | do not reopen Capacity-era commands |
| Player — Activity Participation | **Technical QA certified for current integration** | Activity Session projection and canonical Player fixture integration | broader product consolidation | GameFlow consumes Player; does not own Session config |
| Player — Session-Persistent | **Blocked / not productized** | origin reserved in architecture | authoring, admission, lifetime contracts | separate approved cut required |
| PLAYER-DIAG-1 | Closed / approved | safe diagnostic formatting and immutable last-operation evidence | optional expansion | preserve semantics |
| Activity readiness + reveal | Implemented (Experimental); core QA certified | WaitVisible/WaitCovered, terminal recovery, startup parity, Player waiting/join path | focused ObserveOnly/product guidance | preserve semantics |
| WaitCovered Loading progress | Implemented (Experimental); core QA certified | participant-aware progress/terminal plus Player pending-then-terminal proof | presentation guidance | preserve aggregate-only authority boundary |
| Camera | Closed for current single-output scope | persistent output and Player request/restoration | split-screen / multiple outputs | preserve one-output boundary |
| Pause / Input / Gate | Closed for current single-player scope; hardening remains | Player-bound Pause, resume, gate semantics | broader negative matrix | preserve declared authority model |
| Reset | Implemented (mostly Experimental) | Object/Group/Cycle Reset, Activity Restart | unload recomposition finding | separate Reset cut |
| Activity transaction | **IF-TXN-01/02/03A CLOSED / CERTIFIED** | current approved transaction/gate boundaries | concrete exceptional paths only | no generic transaction manager |
| Persistence / ProgressionSave | Foundation | contracts/store exist | product authoring + real consumer proof | product decision |
| ObjectEntry / Local visibility | Implemented (Experimental) | adapters/declarations exist | product guides deferred | keep advanced/foundation |
| Editor product surface (IF-ADR-010) | Proposed | Editor authoring standard + many Custom Editors | consistent product application | preserve designer-first direction |
| ADR-012 participation profile | Accepted / substantially implemented | requirements, evidence, runtime compatibility; Player integration QA green | product consolidation | preserve contract |
| Authored identity (IF-ADR-014) | Closed / approved | IF-ID package/tests/QA/FIRSTGAME proof | IF-ID-07 deferred | preserve exact-reference + token authority |
| IF-ADR-015 provisioning command/observation surface | **Proposed; implementation technical QA certified** | current no-Capacity consumer vocabulary and scoped observation certified | FIRSTGAME + P5 + final ADR disposition | real-consumer proof |
| IF-ADR-016 Session initialization | **Accepted; implementation technical QA certified** | consolidated Profile, Supported Slots, uniform provisioning, Actor Resolution, frozen effective config | FIRSTGAME product proof | preserve model; no compatibility rail |

## Canonical Player Session contract

```text
PlayerSessionProfile
├── Supported Slots
├── Initial Joining
├── Host Provisioning
│   ├── Scene Provided
│   └── Manager Provisioned
└── Actor Resolution
    ├── Resolve Configured Default
    └── Leave Unresolved
```

Rules:

```text
Supported Slots.Count = structural maximum
Joined/Occupied         = current runtime players
Joining Open/Closed     = admission intent
Join                    = first vacant Supported Slot in authored order
no vacant Slot          = explicit rejection
```

Removed and rejected from the canonical model:

```text
PlayerProvisioningProfile
PlayerSlotProvisioningOverride
Initial/Current/Dynamic Capacity
SetCapacity / SetDynamicCapacity
per-Slot Host Provisioning override
```

## Canonical provisioning modes

### Scene-Provided

```text
Session Host Provisioning = Scene Provided
Host already exists in the active composition
framework discovers/adopts within explicit Route/Activity composition scope
no PlayerInputManager bridge
```

### Manager-Provisioned

```text
Session Host Provisioning = Manager Provisioned
explicit Join creates Host through PlayerInputManager
serialized PlayerInputManager player limit = SupportedSlotCount
```

The `PlayerInputManager` limit is a derived materialized technical constraint, not Session Capacity and not runtime authority.

## Canonical public Player surface

```text
Persistent Application Content
  LocalPlayerProvisioningAuthoring
  LocalPlayerProvisioningHostRegistration
  PlayerInputManager                  # Manager-Provisioned only
  optional Actor-selection authoring

Route / Activity content
  LocalPlayerProvisioningConsumerAccessBinding
  optional PlayerProvisioningCommandTrigger
  optional PlayerProvisioningStatusBinding
```

Accepted command vocabulary:

```text
Open Joining
Close Joining
Request Join
Request Default Actor Selection
```

Observation remains immutable and scoped. Internal reservation, Actor preparation/materialization, gameplay admission and Activity reconcile authorities are not public consumer commands.

## Current QA certification

Canonical entrypoint:

```text
Immersive Framework/QA/Player/Run Full Player QA
```

Final evidence:

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

Expected error logs emitted by deliberate Q2 negative cases are evidence, not certification failure, when the Q2 runner and master orchestrator return PASS.

## Remaining Player findings

### FIRSTGAME real-consumer proof

Technical QA is no longer the blocker. The next evidence is whether a developer can manually create, configure, understand and use the Player feature without framework-internal knowledge.

Recommended consumer proof order:

```text
Demo02 — Scene-Provided
  Single / Route-Owned
  Single / Activity-Owned
  Multiplayer

Demo03 — Manager-Provisioned
  Single
  Multiplayer / late Join
```

### P5 creation-workflow disposition

P5 is post-FIRSTGAME and may validly conclude:

```text
NO ADDITIONAL TOOLING REQUIRED
```

or justify the smallest focused Create-menu, Inspector, template or Composer support. A Wizard/Composer is not mandatory by architecture alone.

### Leave / disconnect

Session Player Leave and device disconnect/reconnect remain outside current ADR-015 scope and require their own approved contract work.

### Session-Persistent

Architecture exists but no canonical product/runtime workflow is approved. Do not simulate it with arbitrary persistent GameObjects.

## Current execution priority

```text
1. FIRSTGAME Scene-Provided manual product proof
2. FIRSTGAME Manager-Provisioned manual product proof
3. IF-PLAYER-SURFACE-07 / P5 tooling disposition from observed friction
4. Separate approved cuts for Leave/disconnect or Session-Persistent only when required
```

Do not reopen the removed Capacity / separate provisioning Profile model to satisfy old QA or historical documentation.

## API status note

Technical certification does not automatically promote Experimental/preview API stability metadata.

## Historic programs

Large readiness/M07/P3/completeness plan documents remain historical planning context. Current mutable status belongs in this tracker. Historical audits should retain their original baseline and must not be treated as current product truth when they reference superseded Player configuration contracts.
