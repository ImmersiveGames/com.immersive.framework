# C9S — Camera Closure and G1 Scope Correction

Status: **documentation closure delivered**  
Type: documentation / governance / product-boundary reconciliation  
Date: **2026-07-12**

## Objective

Reconcile canonical documentation with the actual closed Camera implementation,
QA evidence and FIRSTGAME consumer proof, then select the next lane without
turning gameplay into a framework responsibility.

## Camera closure recorded

```text
CameraRigRecipe / CameraRigComposer
persistent UIGlobal output
CameraOutputContext arbitration
Player request
Activity override
Route override
Session transition override
typed output injection
Follow Offset authoring
explicit release/restoration
```

Evidence:

```text
C9Q Follow Pipeline QA:
  PASS, 4 cases

C9R Camera Override Authority QA:
  PASS, 11 cases

FIRSTGAME:
  persistent Session materialization PASS
  runtime injection PASS
  transition request/release PASS
  manual precedence/restoration PASS
  visual inspection accepted
```

Default precedence:

```text
Player 50 < Activity 100 < Route 200 < Session 300
```

## G1 correction

The previous “minimal playable loop” wording incorrectly made a simple
objective, interaction and resettable gameplay state appear mandatory for
framework closure.

The corrected G1 boundary is:

```text
framework:
  Bootstrap
  Route request/exit/entry
  loading and transition
  input Gate restoration
  Session camera transition authority
  normal camera restoration
  diagnostics

consumer:
  objective
  interaction
  win condition
  combat
  mission state
  movement/gameplay semantics
```

A Menu → Gameplay → Ending/Menu → re-entry flow is a valid framework loop.

## Active lane

```text
G1 — Consumer Route Loop
```

First cut:

```text
G1A — FIRSTGAME Route Loop Audit and Scope Lock
```

G1A must incorporate the user's additional G1 requirements before any
implementation.

## Files modified

```text
Documentation~/Current/00-Current-State.md
Documentation~/Current/01-Roadmap.md
Documentation~/Current/02-Usage-Map.md
Documentation~/Current/05-Execution-Status.md
Documentation~/Current/Camera-Delivery-Reconciliation.md
Documentation~/Guides/Camera-Architecture-Flow.md
Documentation~/Guides/Camera-Product-Usage.md
Documentation~/Planning/README.md
Documentation~/README.md
```

## File created

```text
Documentation~/Product/C9S-CAMERA-CLOSURE-AND-G1-SCOPE-CORRECTION.md
```

## Files removed

None.

## Out of scope

```text
runtime changes
Editor tooling changes
QA changes
FIRSTGAME scene changes
new gameplay contracts
rewriting the historical detailed plan
```

## Technical acceptance

```text
[ ] Current docs no longer mark C9M or C9C–C9R as pending.
[ ] Current docs use Override binding names.
[ ] Session output and precedence are documented.
[ ] QA C9R and FIRSTGAME closure are recorded.
[ ] Only G1 is active.
[ ] P3 and S1 are not active in parallel.
```

## Product acceptance

```text
[ ] Designer can identify the current Camera authoring flow.
[ ] Technical user can identify runtime authority and precedence.
[ ] G1 does not imply framework-owned gameplay.
[ ] FIRSTGAME-specific content remains consumer-owned.
[ ] Additional G1 requirements can be added during G1A without contradicting the roadmap.
```

## Suggested commit

```text
Docs: close Camera C9 and redefine G1 as consumer Route loop
```
