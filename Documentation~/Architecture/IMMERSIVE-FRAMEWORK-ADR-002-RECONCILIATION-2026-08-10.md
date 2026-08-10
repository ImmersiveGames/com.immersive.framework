# Immersive Framework — IF-ADR-002 Reconciliation

**Date:** 2026-08-10  
**Type:** documentation / architectural reconciliation  
**Decision:** current accepted cross-cutting boundary closed; no implementation cut required

## Objective

Reconcile IF-ADR-002 status documentation with its current normative decision and
with the authoring shapes already present in the official package.

The reconciliation removes completion debt that was being inferred from generic
tooling, generic QA and generic FIRSTGAME requirements that IF-ADR-002 explicitly
does not require.

## Source baselines

```text
com.immersive.framework
  18a6c5079f7436cd86ffa1158cabfe12278855da
  Adr13A-Audio

QAFramework
  f4ce36335878113e4b64e79d337c0645f6499707
  Fix

FIRSTGAME / planet-devourer
  796618243c3ca76f70d582f38475320c6461420b
  Demo02 Reajuste
```

Repositories were inspected read-only.

## Scope

```text
IF-ADR-002 normative status
current package authoring-model evidence
IF-ADR-010 relationship
current Tracker classification
existing ADR-002 / ADR-009 reconciliation record
current-boundary completion interpretation
```

## Out of scope

```text
new runtime behavior
new Composer / Wizard / Apply-Rebuild system
feature-specific Editor redesign
new generic QA suite
new generic FIRSTGAME proof
reconciliation of other ADR scores
```

## Reconciled result

```text
IF-ADR-002 — Product Authoring Model

Normative status
  ACCEPTED

Architecture / Contract
  CLOSED for current accepted boundary

Package authoring model
  IMPLEMENTED

Product Surface / Diagnostics
  IMPLEMENTED for the cross-cutting contract

Generic Technical QA
  NOT APPLICABLE

Generic FIRSTGAME gate
  NOT APPLICABLE

Current-scope blocker
  NONE IDENTIFIED

Normalized planning estimate
  100% over applicable Architecture + Package + Surface dimensions
```

## Why the previous scores were inconsistent

Three status representations described the same accepted boundary differently:

```text
ADR-002 header / audit history
  29 / 30 package assessment

Tracker
  79% with generic QA and FIRSTGAME points deducted

ADR-002 / ADR-009 reconciliation
  99% normalized
```

The current normative ADR already states that:

- the cross-cutting authoring model is mature;
- no generic tooling gap is identified;
- direct/manual authoring is a valid first-class product model;
- Profile/Template/Composer/Apply/Rebuild/Wizard are conditional shapes rather
  than mandatory layers;
- generic Technical QA for ADR-002 is not applicable;
- generic FIRSTGAME is not an ADR-002 completion gate;
- no cross-cutting implementation cut is currently justified.

Therefore the former `29/30` missing point has no accepted current-scope contract
to implement. Keeping it as active completion debt would manufacture work solely
to improve a score.

## Current package evidence

The package already demonstrates multiple legitimate authoring shapes:

```text
Direct / manual
  Pause / Reset / Activity Restart / Input Gate
  Route and Activity triggers
  Readiness participants
  Activity Local Visibility
  Optional BGM intent

Reusable intent / Profile
  PlayerSessionProfile
  typed Activity / Route / configuration assets

Template
  Persistent Content Scene Template

Justified materialization
  Camera Rig / CameraRigComposer
```

This diversity is evidence for ADR-002, not inconsistency with it.

## Product surface affected

Documentation/status only.

No Inspector, authoring component, asset schema, creation flow or runtime behavior
is changed by this cut.

## Expected use flow

For each concrete feature:

```text
classify the real authoring lifecycle
        ↓
choose the smallest valid authoring shape
        ↓
apply IF-ADR-010 product-surface rules
        ↓
keep runtime authority with the owning runtime contract
        ↓
attach objective QA/FIRSTGAME evidence to the owning feature ADR
```

Do not introduce a generic authoring layer merely because another feature uses
one.

## Technical smoke expected

No new smoke.

This cut changes documentation only. Existing feature-specific QA remains the
authority for objective Editor/runtime contracts.

## Technical acceptance criteria

```text
ADR-002 remains Accepted
no runtime contract changes
no new global authority or implicit lookup
no generic tooling requirement invented
generic ADR-002 QA remains N/A
generic ADR-002 FIRSTGAME remains N/A
feature-specific evidence remains owned by feature ADRs
Tracker and reconciliation records agree with the normative ADR
```

## Product acceptance criteria

```text
manual explicit authoring remains a first-class default
reusable intent is added only where reuse is real
materialization exists only where derivation is real
Wizard remains exceptional
ADR-010 remains the minimum Editor/Inspector surface standard
future improvements require a concrete feature need or observed friction
```

## Architectural gain

Removes an artificial incentive to create generic authoring infrastructure and
keeps ADR-002 as a cross-cutting selection model rather than a universal
Composer/Wizard mandate.

## Usability gain

Keeps product work attached to real consumer friction. A feature may remain simple
when direct explicit authoring is understandable, while more complex features can
still use Profile, Template or Composer patterns where they materially reduce
error or internal knowledge requirements.

## Files created / altered / removed

### Edited

- `Documentation~/Architecture/ADRs/IF-ADR-002-Product-Authoring-Model.md`
- `Documentation~/Architecture/Tracking/IF-TRACK-Framework.md`
- `Documentation~/Architecture/IMMERSIVE-FRAMEWORK-ADR-002-009-RECONCILIATION-2026-08-10.md`

### Created

- `Documentation~/Architecture/IMMERSIVE-FRAMEWORK-ADR-002-RECONCILIATION-2026-08-10.md`

### Removed

- none

## Suggested commit message

```text
Reconcile ADR-002 product authoring status
```
