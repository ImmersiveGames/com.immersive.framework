# Immersive Framework — FIRSTGAME Demo01 Consumer Revalidation

Date: 2026-08-09  
Consumer: `ImmersiveGames/planet-devourer` — Demo01  
Classification: **real-consumer revalidation / post-Player non-regression evidence**  
Runtime result reported by consumer: **PASS**

## Scope

Demo01 remains the canonical FIRSTGAME demonstration for:

```text
M01 — Route and Activity
M02 — Lifecycle Events
M03 — Activity Readiness
```

The revalidation was performed after the Player architecture / Player Surface work. Demo01 does not configure Player Slots in its `GameApplicationAsset`, so this run is intentionally a Player-independent consumer proof.

## Confirmed consumer behavior

The current Demo01 composition demonstrates:

- Game Application boot into authored Route flow;
- Primary Scene + Route-owned + Activity-owned content composition;
- Activity replacement without reloading the whole Route;
- Scene / Route / Activity lifecycle callbacks;
- `ObserveOnly`, `WaitVisible` and `WaitCovered` Activity readiness policies;
- Required versus Optional readiness behavior;
- participant-aware Loading progress;
- successful terminal reveal only after required readiness reaches Ready;
- exit and reentry with a fresh Activity occurrence;
- no observed duplicate content during the manual revalidation.

## Player non-regression interpretation

Current Demo01 `GameApplicationAsset` has no Player Slots configured.

Therefore this evidence means:

```text
Player architecture changes
  did not regress
  Route / Activity / Lifecycle / Readiness / Loading
  in this Player-independent real consumer demonstration.
```

It does **not** certify Player Session, Join, Actor Selection, Manager-Provisioned Player or the ADR-015/016 consumer workflow.

## ADR evidence classification

| ADR | Demo01 contribution | Interpretation |
|---|---|---|
| IF-ADR-001 | Strong | Real Application → Route → Activity lifecycle and scoped content ownership |
| IF-ADR-006 | Strong secondary | Covered/visible Loading, readiness wait, reveal, cleanup and reentry happy path |
| IF-ADR-007 | Strong primary | ObserveOnly / WaitVisible / WaitCovered and occurrence-scoped readiness |
| IF-ADR-008 | Secondary | Consumes persistent application content successfully; does not prove Apply/Rebuild authoring |
| IF-ADR-009 | Medium/Strong | Contextual hidden/covered content and reveal ordering |
| IF-ADR-011 | Strong primary | Participant-aware determinate Loading progress and terminal completion |
| IF-ADR-002 | Product evidence | Manual authoring is usable, while existing findings expose repetitive/error-prone wiring |
| IF-ADR-010 | Gap evidence | Existing findings support the need for consistent creation/remediation/Advanced-Debug UX |

## Percentage disposition

This revalidation **does not by itself change ADR completion percentages**.

Reason:

- the relevant ADRs already count FIRSTGAME consumer evidence;
- the run refreshes and strengthens that evidence after recent framework changes;
- remaining completion gaps are mostly QA-negative, product-authoring, diagnostics or unrelated lifecycle boundaries.

Percentages should move only when a remaining completion criterion is actually closed.

## Existing Demo01 UX findings retained

The Demo01 findings remain valid product evidence, including:

- inactive readiness participants can still be discoverable by scope;
- repeated UnityEvent wiring is error-prone;
- duplicate Participant Id authoring is too easy;
- manual navigation visibility updates are fragile;
- deterministic Loading composition requires careful manual wiring;
- diagnostics are dense for normal users;
- the distinction between aggregate participants and independent participants needs clearer guidance.

These findings should inform IF-ADR-002 / IF-ADR-010 work. They are not runtime failures of Demo01.

## Explicit non-coverage

Demo01 does not prove:

```text
Player Session Profiles
Manager-Provisioned Player
Scene-Provided Player
Join / Leave
Actor Selection
Player participation policies
IF-ADR-015 Player consumer surface
IF-ADR-016 Session initial configuration
Camera request authority in isolation
Pause / Reset
optional BGM
```

## Current disposition

```text
Demo01
  HEALTHY / CURRENTLY PASSING

Primary:
  M01 Route and Activity
  M02 Lifecycle Events
  M03 Activity Readiness

Player dependency:
  NONE

Post-Player regression:
  NONE OBSERVED
```

## Evidence caveat

`planet-devourer/Packages/manifest.json` references `com.immersive.framework` through a local `file:` package path.

For a formal release record, capture the package Git SHA used by the local Unity workspace alongside the manual run evidence. The consumer Git revision alone does not uniquely identify the package bytes executed.
