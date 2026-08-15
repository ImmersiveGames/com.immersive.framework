# IF-TRACK — Immersive Framework

Status: **Active — Player Physical Lifetime Reconciliation Closed / Full Player QA Certified**  
Last updated: **2026-08-15**

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
474eb0c2a7fe1461debb47919899ed3307b099be
master
```

The 2026-08-15 Full Player QA certification was produced from the active package/QA working trees. If runtime implementation changes from that run are not yet present on the GitHub baseline above, source synchronization remains separate repository hygiene. Do not infer that the commit hash itself was the exact tested binary unless that synchronization is confirmed.

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
  owns its current Activity RuntimeContent scope
  does not own terminal physical Player lifetime

Activity A -> Activity B
  same physical Player by default
  preserve ordinary gameplay pose by default
  new contextual Activity occurrence

No Activity representation
  contextual authority may be absent
  Session physical preparation may remain authoritative
  physical Player may remain existing but inactive

Leave / Session termination
  terminal physical release boundaries
```

Current closure record:  
[Player Physical Lifetime Recertification — 2026-08-15](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-RECERTIFICATION-2026-08-15.md)

Historical reopen record:  
[Player Physical Lifetime Reopen — 2026-08-14](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-REOPEN-2026-08-14.md)

## Full Player certification

Terminal result:

```text
PLAYER QA CERTIFIED
mandatoryContracts = 25
executedContracts = 25
passedContracts = 25
```

Certified Player boundaries include:

```text
Serialization
Session
Initial Placement
SceneProvided A -> B -> A physical identity / pose / fresh context
SceneProvided Leave with Activity
SceneProvided Leave without Activity
SceneProvided Session termination without Activity
Manager Provisioned
Manager Join without Activity
Manager Session termination
Actor Lifecycle
Public Surface
Leave / stale occurrence / rejoin
Failed First Scene Adoption
Failed Contextual Reprojection
No Physical Handoff
```

## ADR status

| ADR | Current architecture status | Implementation / QA disposition |
|---|---|---|
| 001 | ACCEPTED / RECONCILED | Core composition unchanged; Player lifetime wording aligned |
| 002 | ACCEPTED | No change |
| 003 | ACCEPTED / RECONCILED | Revised Session physical lifetime implemented and Full Player certified |
| 004 | ACCEPTED | No change |
| 005 | ACCEPTED | No change |
| 006 | ACCEPTED | No change |
| 007 | ACCEPTED / RECONCILED | Player readiness boundary and committed-not-ready semantics certified |
| 008 | ACCEPTED | No change |
| 009 | ACCEPTED | No change |
| 010 | ACCEPTED | No Player-specific delta required by this closure |
| 011 | ACCEPTED / RECONCILED FOR PLAYER BOUNDARY | Player readiness/loading interaction remains occurrence-safe; no false Ready |
| 012 | ACCEPTED / RECONCILED | Activity exclusion/context projection separated from physical lifetime and certified |
| 013 | ACCEPTED / Experimental | No change |
| 014 | ACCEPTED | No change |
| 015 | ACCEPTED / RECONCILED | Public command/observation surface certified, including no-Activity physical evidence |
| 016 | ACCEPTED / RECONCILED | Provisioning origin model implemented and certified |
| 017 | ACCEPTED | No change |
| 018 | ACCEPTED | No change |
| 019 | ACCEPTED / RECONCILED / RECERTIFIED | Exact physical identity continuity and Session ownership certified |
| 020 | ACCEPTED / RECONCILED / RECERTIFIED | Manager + SceneProvided Leave/termination boundaries certified |
| 021 | ACCEPTED / RECONCILED / CERTIFIED | Dedicated Initial Placement 9/9 + Full Player placement/continuity PASS |
| 022 | PROPOSED | No change |

## Player lifetime work closure

### PLR-01 — Physical ownership model — CLOSED

Both provisioning modes converge on Session-owned admitted physical representation.

### PLR-02 — Scene-Provided adoption/promotion — CLOSED

Successful adoption preserves the exact physical object under Session lifetime and survives supplying Activity scene release.

### PLR-03 — Activity contextual handoff — CLOSED

Activity A -> B -> A retires/rebuilds contextual authority while preserving exact physical identity.

### PLR-04 — Inactive no-Activity state — CLOSED

`Contextual=Absent` does not imply physical lifetime loss. Session preparation evidence remains the canonical physical truth.

### PLR-05 — Leave — CLOSED

SceneProvided and Manager-provisioned physical resources release at occurrence-safe Session Leave; no-Activity Leave and Session termination are certified.

### PLR-06 — Initial Placement — CLOSED

Ordinary Activity handoff preserves pose. Placement requires explicit spatial-start intent and exact authority.

### PLR-07 — Focused QA / recertification — CLOSED

Terminal Full Player QA completed `25/25` mandatory contracts.

## Negative-path semantic clarifications

### Route commit versus Activity readiness

```text
Route Request = Succeeded
```

may legitimately coexist with:

```text
current startup Activity = Active
ActivityReadiness = NotReady
ActivityTransition = CommittedNotReady
blockingIssues > 0
```

Route navigation commit is not the same truth as Activity readiness.

### Failed contextual reprojection

If Activity B is already current when its SceneProvided Player admission fails, B may legitimately retain its Activity-owned `RuntimeContent` root until Activity exit/release.

This does **not** mean:

```text
Player B admitted
physical Player handed off
Session preparation replaced
```

The Player contextual failure and Activity scope lifetime remain separate authorities.

### Observation integrity

QA and diagnostics must observe physical truth through canonical Session/occurrence preparation evidence. Hierarchy shape, `childCount`, scene scanning, `FindObjectOfType*` and first-compatible-object lookup are not lifetime authority.

## Historical certification policy

Do not delete or rewrite previous dated ADR-019/ADR-020 certification records.

They remain evidence of the behavior tested under the former contract. The 2026-08-15 recertification record is the authority for the revised physical-lifetime boundary.

## FIRSTGAME

The technical Player boundary is no longer blocked by PLR reconciliation. FIRSTGAME may proceed as real-consumer integration validation when scheduled. FIRSTGAME remains consumer proof, not the primary technical smoke harness.
