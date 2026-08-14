# IF-TRACK — Immersive Framework

Status: **Active — Player Physical Lifetime Reconciliation Open**  
Last updated: **2026-08-14**

## Authority model

```text
Accepted ADRs
  -> normative architecture

Reconciliation records
  -> current alignment / reopen / certification evidence

Tracker
  -> mutable current delivery state

Historical certification
  -> evidence for the contract tested at that date
```

## Current reviewed package baseline

```text
ImmersiveGames/com.immersive.framework
661968297a0436c5bcafaa197b86bc486fc7ed4d
ADR21Build
```

## Current Player architecture freeze

```text
Session owns admitted physical Player after successful admission.

Manager-Provisioned
  Framework supplies candidate
  -> Session owns after admission

Scene-Provided
  scene supplies candidate
  -> Framework adopts
  -> Session owns after admission

Activity
  owns projection / activation / gameplay / camera / readiness / contextual bindings
  does not own terminal physical lifetime

Activity A -> Activity B
  same physical Player by default
  new contextual Activity occurrence

No Activity representation
  physical Player may remain existing but inactive

Leave / Session termination
  terminal physical release boundaries
```

See:
`../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-REOPEN-2026-08-14.md`.

## ADR status

| ADR | Current architecture status | Implementation / QA disposition |
|---|---|---|
| 001 | ACCEPTED / RECONCILED | Core composition unchanged; Player lifetime wording reconciled |
| 002 | ACCEPTED | No change |
| 003 | ACCEPTED / RECONCILED | Activity physical-lifetime assumption superseded; implementation audit required |
| 004 | ACCEPTED | No change |
| 005 | ACCEPTED | No change |
| 006 | ACCEPTED | No change |
| 007 | ACCEPTED / RECONCILED | Readiness now binds existing physical Player; QA delta required |
| 008 | ACCEPTED | No change |
| 009 | ACCEPTED | No change |
| 010 | ACCEPTED | Product surface may need follow-up after implementation |
| 011 | ACCEPTED | Progress semantics remain; verify no physical-occurrence assumption in QA |
| 012 | ACCEPTED / RECONCILED | Activity exclusion = inactive/absent representation, not physical destroy |
| 013 | ACCEPTED / Experimental | No change |
| 014 | ACCEPTED | No change |
| 015 | ACCEPTED / RECONCILED | Observation must separate physical identity from Activity occurrence |
| 016 | ACCEPTED / REOPENED FOR IMPLEMENTATION RECONCILIATION | Provisioning now acquisition origin; post-admission ownership converges |
| 017 | ACCEPTED | No change |
| 018 | ACCEPTED | No change |
| 019 | ACCEPTED / REOPENED / REVISED | Previous QA historical; implementation + recertification required |
| 020 | ACCEPTED / REOPENED / REVISED | ADR020-H historical/partial; Scene-Provided ownership/release recertification required |
| 021 | PROPOSED / REOPENED BEFORE ACCEPTANCE | ADR21Build exists but is not certifiable against revised lifetime |
| 022 | PROPOSED | No change |

## Reopened Player work

### PLR-01 — Physical ownership model

Implement one Session-owned admitted physical representation for both provisioning modes.

### PLR-02 — Scene-Provided adoption/promotion

Prove successful adoption transfers runtime lifetime ownership and survives supplying
Activity scene unload.

### PLR-03 — Activity contextual handoff

Prove Activity A -> B retires/rebuilds only contextual authority while preserving exact
physical Player identity.

### PLR-04 — Inactive no-Activity state

Prove Joined Player with no current representation retains physical object but is
inactive/non-participating.

### PLR-05 — Leave

Reconcile ADR-020 so adopted Scene-Provided physical resources release at Session Leave.

### PLR-06 — Initial Placement

Reconcile ADR-021 so ordinary Activity handoff preserves current pose; placement occurs
only for explicit spatial-start intent.

### PLR-07 — Focused QA / recertification

Required proof:

```text
same physical identity across Activities
no destroy/recreate during Activity transition
Scene-Provided promoted ownership
scene unload survival
inactive no-Activity state
fresh contextual readiness
Leave resource release for both provisioning origins
placement continuity
```

## Historical certification policy

Do not delete previous ADR-019/ADR-020 QA evidence.

It remains evidence of behavior tested under the former contract but must not be reported
as certification of the revised physical lifetime boundary.

## FIRSTGAME

FIRSTGAME proof for the affected Player boundary is deferred until PLR implementation and
technical QA are reconciled. FIRSTGAME remains real-consumer proof, not the primary
technical smoke harness.
