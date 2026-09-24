# Immersive Framework — Player Physical Lifetime Recertification

Status: **Closed / Full Player QA Certified**  
Date: **2026-08-15**  
Documentation repository baseline reviewed: `474eb0c2a7fe1461debb47919899ed3307b099be` (`master`)  
Supersedes the open delivery disposition recorded in: [Player Physical Lifetime Reopen — 2026-08-14](IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-REOPEN-2026-08-14.md)

> Runtime certification evidence was produced by the 2026-08-15 Full Player QA run supplied from the active package/QA working trees. This record certifies the observed architecture and behavior. If implementation changes from that run are not yet present on the GitHub baseline above, repository synchronization remains separate source-control hygiene and does not change the QA result recorded here.

## Purpose

Close the Player physical-lifetime reopen by recording the reconciled implementation and the terminal technical certification of the revised Session/Activity ownership model.

## Certified architecture

```text
Session
  owns Joined Player occurrence
  owns admitted physical Player after successful admission
  owns physical preparation/lifetime until Leave or Session termination

Activity
  owns contextual projection / activation / gameplay / camera / readiness
  owns its Activity RuntimeContent scope while it is current
  does not own terminal physical Player lifetime
```

Scene-Provided and Manager-Provisioned differ in candidate acquisition origin and converge on Session ownership after successful admission.

## Full Player terminal certification

The terminal run reported:

```text
[QA_PLAYER_FULL]
status='Completed'
verdict='PLAYER QA CERTIFIED'
mandatoryContracts='25'
executedContracts='25'
passedContracts='25'
```

Certified phases:

```text
Serialization                         PASS
Session                               PASS
Placement                             PASS
Scene Provided                        PASS
Scene Provided Leave                  PASS
Scene Provided Leave Without Activity PASS
Scene Provided Session Termination    PASS
Manager Provisioned                   PASS
Manager Join Without Activity         PASS
Manager Session Termination           PASS
Actor Lifecycle                       PASS
Public Surface                        PASS
Leave                                 PASS
Failed First Scene Adoption           PASS
Failed Contextual Reprojection        PASS
No Physical Handoff                   PASS
```

## Physical lifetime proofs

The revised IF-ADR-019 boundary is now technically proven:

```text
SceneProvided A -> B -> A
  same physical Player identity
  pose preserved across ordinary contextual handoff
  fresh Activity contextual authority per occurrence
  no re-Join
  no implicit physical replacement

No current Activity representation
  contextual authority absent
  Session physical preparation remains authoritative
  physical Player remains the same Session-owned instance
```

QA observation is required to use canonical Session/occurrence evidence. Hierarchy shape, `childCount`, scene scanning and global object lookup are not physical-lifetime authority.

## Leave and Session termination proofs

IF-ADR-020 is recertified for both provisioning origins and the no-Activity boundary.

Certified behavior includes:

```text
SceneProvided Leave with Activity
  contextual teardown reaches terminal stages before physical/session release

SceneProvided Leave without Activity
  no fabricated Activity required
  retained Session physical Player resolves from Session preparation evidence
  physical resource releases terminally

Session termination without Activity
  remaining Session-owned physical resources release

Manager Leave / rejoin
  occurrence-safe
  stale Leave rejected
  Joining Closed does not block Leave
```

## Initial Placement proof

IF-ADR-021 is accepted and certified against the revised lifetime model.

Dedicated ADR-021 QA completed `9/9`, including:

```text
exact Manager logical Actor target
missing binding without fallback
duplicate exact Slot rejection
foreign scene ignored
anchor outside owned scene rejected
SceneProvided preserve authored pose
SceneProvided apply explicit Activity placement
failed placement evidence
```

Ordinary Activity handoff preserves the current physical pose unless explicit placement intent requires movement.

## Public surface proof

The public Player surface now completes Join, Actor selection, preparation/materialization/admission, Activity contextual handoff, exclusion/reentry, Leave and Session termination without bypassing the IF-ADR-021 initial-placement gate.

A public command result must be interpreted at the layer it represents. Lifecycle/authoring failure is not required to fabricate a terminal public admission result when no such public admission operation was established.

## Committed-not-ready semantics

The negative matrix established an important Game Flow/Player boundary:

```text
Route committed successfully
!=
startup Activity Ready
```

A Route may be current while its startup Activity is:

```text
Activity = Active
ActivityReadiness = NotReady
ActivityTransition = CommittedNotReady
blockingIssues > 0
```

This is not a contradiction. Navigation commit and Activity readiness are separate truths.

Likewise, when failed contextual reprojection commits Activity B but Player admission for B fails:

```text
Activity B may remain current
Activity-owned RuntimeContent root for B may legitimately remain
Player contextual admission for B remains absent/failed
Session-owned physical Player is not handed off or replaced
```

The Activity root is released when Activity B exits/releases, not by Player admission rollback.

## No physical handoff

Negative-path certification proves that failed contextual reprojection does not silently:

```text
replace the physical Player
transfer preparation to another object
create a duplicate Actor
reuse stale contextual authority
turn Activity-owned RuntimeContent into Player physical ownership
```

## ADR disposition

The 2026-08-14 reopen is closed for the technical Player boundary.

```text
IF-ADR-003  Accepted / Reconciled / Player QA recertified
IF-ADR-007  Accepted / Reconciled / Player readiness boundary recertified
IF-ADR-011  Accepted / Reconciled for the Player readiness interaction
IF-ADR-012  Accepted / Reconciled / Player QA recertified
IF-ADR-015  Accepted / Reconciled / Public Surface certified
IF-ADR-016  Accepted / Reconciled / implementation certified
IF-ADR-019  Accepted / Reconciled / implementation recertified
IF-ADR-020  Accepted / Reconciled / implementation recertified
IF-ADR-021  Accepted / Reconciled / implementation certified
```

Historical dated ADR-019/ADR-020 certification records remain unchanged. They continue to describe the contract tested at their original dates; this record is the certification authority for the revised physical-lifetime boundary.

## FIRSTGAME

The technical Player blocker is closed. FIRSTGAME may now validate the accepted Player boundary as a real consumer when that integration cut is scheduled. FIRSTGAME remains consumer proof, not the primary technical certification harness.
