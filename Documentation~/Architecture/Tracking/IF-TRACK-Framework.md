# IF-TRACK — Immersive Framework

Status: Active  
Last updated: 2026-08-09  
Package version: `1.0.0-preview.17`

## Source baseline

```text
com.immersive.framework
  cf0a37fbcbf72ad2a08556d6045c908521bfd2c1
  P4 — IF-PLAYER-SURFACE-06 — Status / Diagnostics Binding

QAFramework
  Git baseline inspected: 52a31aa9cd237d934ed3241392b87b7990f11dc8
  Player Surface Unity Play Mode certification executed 2026-08-09

FIRSTGAME
  last documented baseline: ab1bfe65c09af8988c2fe21ce06db780fe12aa70
  Demo03Etapa04
```

Git baselines identify the repository state inspected for documentation. The final Q1/Q2 behavioral verdict is manual/local Unity Play Mode evidence and must not be misrepresented as a Git commit result.

## Summary

The package has one internal application/session composition root and explicit typed feature boundaries. Product areas include lifecycle, Player, Camera, Pause/Input/Gate, Reset, Activity readiness, Loading/Transition, persistence foundations and diagnostics.

`FrameworkRuntimeHost` remains Internal. Required runtime dependencies are supplied through typed bindings and explicit scope/lifetime. There is no public static host registry or service-locator API.

The major 2026-08-09 status change is the Player Surface:

```text
P1 scoped Player provisioning consumer access       CLOSED
P2 immutable consumer observation                    CLOSED
P3 designer command authoring                        CLOSED
P4 status / diagnostics binding                      CLOSED
Q1 public positive QA                                PASS 29/29
Q2 negative/lifecycle QA                             PASS 36/36
joint Player Surface verdict                         QA CERTIFIED
```

IF-ADR-015 remains Proposed because product closure still requires FIRSTGAME real-consumer proof, post-FIRSTGAME P5 creation-workflow disposition and final ADR/documentation acceptance.

## Track board

| Track | Real status | Proven coverage | Pending work | Next action |
|---|---|---|---|---|
| Runtime authority | Closed for current boundary | internal host composition; narrow typed ports | preserve boundary | reject static/global lookup |
| Package hygiene | Closed for current boundary | package + QA import discipline | ongoing | do not restore compatibility facades |
| Player — Scene-Provided | **Closed / approved** | authoring, Route/Activity admission, Slot, Host, Actor adoption, readiness, release/reentry/restart/teardown | broader automated matrix desirable | preserve baseline |
| PLAYER-DIAG-1 | **Closed / approved** | safe diagnostic formatting and immutable last-operation evidence | optional expansion | preserve semantics |
| Player — Manager-Provisioned | **Implemented / technical QA certified** | IF-ADR-016 initialization + P1–P4 consumer surface + Q1 29/29 + Q2 36/36 | FIRSTGAME manual proof; P5 UX/tooling disposition; final ADR closure | document → FIRSTGAME → P5 |
| Player — Session-Persistent | **Blocked** | origin reserved in architecture | authoring, admission, lifetime contracts | separate approved cut required |
| Activity readiness + reveal | **Implemented (Experimental); core QA certified** | WaitVisible/WaitCovered, terminal recovery, startup parity, Player public WaitingForJoin/WaitCovered path | focused ObserveOnly/product guidance | preserve semantics |
| WaitCovered Loading progress | **Implemented (Experimental); core QA certified** | participant-aware progress/terminal plus Player public pending-then-terminal proof | presentation guidance / polish | preserve aggregate-only authority boundary |
| Camera | Closed for current single-output scope | persistent output and Player request/restoration | split-screen / multiple outputs | preserve one-output boundary |
| Pause / Input / Gate | Closed for current single-player scope; hardening remains | Player-bound Pause, resume, gate semantics | broader negative matrix | preserve declared authority model |
| Reset | Implemented (mostly Experimental) | Object/Group/Cycle Reset, Activity Restart | unload recomposition finding | separate Reset cut |
| Activity transaction | **IF-TXN-01/02/03A CLOSED / CERTIFIED** | current approved transaction/gate boundaries | concrete exceptional paths only | no generic transaction manager |
| Persistence / ProgressionSave | Foundation | contracts/store exist | product authoring + real consumer proof | product decision |
| ObjectEntry / Local visibility | Implemented (Experimental) | adapters/declarations exist | product guides deferred | keep advanced/foundation |
| Editor product surface (IF-ADR-010) | **Proposed** | Editor authoring standard + many Custom Editors | consistent product application | preserve designer-first direction |
| ADR-012 participation profile | **Accepted / substantially implemented** | requirement levels, evidence, runtime compatibility | product consolidation | preserve contract |
| Authored identity (IF-ADR-014) | **Closed / approved** | IF-ID package/tests/QA/FIRSTGAME proof | IF-ID-07 deferred by design | preserve exact-reference + token authority |
| IF-ADR-015 provisioning command/observation surface | **Proposed / 80%** | P1–P4 shipped; Q1/Q2 certified | FIRSTGAME real-consumer proof; P5 disposition; final docs/acceptance | next Player product phase |
| IF-ADR-016 Session initialization Profiles | **Proposed / 90%** | Profiles/resolver/runtime + 05/05B/07 QA green | FIRSTGAME proof; full Route/Activity non-reapply integration evidence | preserve configuration authority |

## Canonical Player Surface implementation

### IF-ADR-016 initialization

```text
PlayerSlotProfile
→ PlayerProvisioningProfile
→ PlayerSessionProfile
→ GameApplicationAsset.DefaultPlayerSessionProfile
→ frozen effective Session configuration
```

This replaces legacy guidance that treated direct GameApplication Slot lists as the canonical Session-enabled authoring path.

### IF-ADR-015 runtime consumer surface

```text
Persistent Application Content
  LocalPlayerProvisioningAuthoring
  LocalPlayerProvisioningHostRegistration
  PlayerInputManager
  optional public Actor-selection authoring

Route / Activity
  LocalPlayerProvisioningConsumerAccessBinding
  optional PlayerProvisioningCommandTrigger
  optional PlayerProvisioningStatusBinding
```

P1/P2 provide scoped reachability and observation. P3/P4 provide explicit authoring/presentation. They do not create a second Player authority.

## Player Surface QA certification

```text
QA-PLAYER-SURFACE-01
  PASS — 29/29
  PublicNavigation
  ScopedAccess
  Joining / Capacity / Join
  Host / Slot observation
  Actor Selection
  normal preparation/materialization/admission
  WaitCovered pending → terminal
  exit preserves Session
  reentry no duplicate

QA-PLAYER-SURFACE-02
  PASS — 36/36
  closed joining
  invalid / exhausted capacity
  no-change
  missing / wrong / stale / destroyed scope
  exit WaitingForJoin
  stale Activity endpoint / occurrence
  stale Actor selection revision
  repeated selection stability
  unbound public navigation negative

Joint verdict
  PLAYER SURFACE QA CERTIFIED
```

Expected error logs emitted by deliberate negative cases are evidence, not certification failure, when the Q2 runner and joint orchestrator end Passed/Certified.

## Closed finding — public Slot/Host/Actor observation gap

The earlier tracker finding that no coherent public Slot–Host–Actor consumer projection existed is closed for the ADR-015 Player Surface boundary.

P2 now exposes immutable per-Slot consumer observation that can correlate joined state, Host, selected Actor, logical preparation, physical materialization, gameplay admission, Activity occurrence and Session/applied revision evidence as applicable.

Deep assignment token/owner/origin mutation remains internal QA authority and was not made public.

## Closed finding — WaitCovered + Manager-Provisioned external progression

Direct public QA now proves:

```text
Required Player
+ WaitCovered
+ no joined Player
→ WaitingForJoin / loading pending

public Join + normal Actor lifecycle
→ Ready
→ loading/gate terminal
```

The framework does not repair this with fake readiness, timeout, automatic Join or premature reveal. Games still need a reachable control path for external progression.

## Remaining Player findings

### FIRSTGAME real-consumer proof

Technical QA is no longer the blocker. The next evidence is whether a game developer can manually compose the shipped Profiles and P1–P4 surfaces without framework-internal knowledge.

### P5 creation-workflow disposition

P5 is post-FIRSTGAME. It may conclude:

```text
NO ADDITIONAL TOOLING REQUIRED
```

or justify the smallest focused Create-menu/Inspector/template/Composer support. A Wizard/Composer is not mandatory by architecture alone.

### Session-Persistent package gap

Architecture exists but product/runtime workflow remains unavailable. Do not simulate it with an unscoped Persistent Content prefab.

### Leave / disconnect

Session Player Leave and device disconnect/reconnect remain outside the current ADR-015 scope and require their own approved contract work.

## Other closed programs

### IF-TXN-01 / 02 / 03A

Closed for the currently approved transaction and Transition Gate authority boundary. Reopen only with evidence that contradicts the certified semantics.

### IF-ID

Closed for the current authored-definition/stable-identity boundary. IF-ID-07 remains deferred until a real persistence/external resolver requirement appears.

## Current execution priority

```text
1. Documentation reconciliation for shipped Player Surface
   CURRENT

2. FIRSTGAME-PLAYER-SURFACE-01
   manual real-consumer command/status/lifecycle proof

3. IF-PLAYER-SURFACE-07 (P5)
   creation-workflow/tooling disposition based on real friction

4. FIRSTGAME-PLAYER-SURFACE-02
   final UX disposition

5. IF-ADR-015 final documentation / acceptance
   after product evidence is complete
```

Do not reopen P1–P4 or Q1/Q2 without new contradicting evidence.

## API status note

Manager-Provisioned runtime/product APIs remain subject to the package's Experimental/preview status policy even though the P1–P4 technical surface is implemented and QA-certified. Technical certification does not automatically promote API stability metadata.

## Historic programs

Large readiness/M07/completeness plan documents remain historical planning context. Current mutable status belongs in this tracker; historic audits should retain their original baseline plus explicit later reconciliation notes rather than being silently rewritten as if they were produced after certification.
