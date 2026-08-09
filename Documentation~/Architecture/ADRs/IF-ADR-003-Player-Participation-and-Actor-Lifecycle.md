# IF-ADR-003 — Player Participation and Actor Lifecycle

Status: Accepted  
Last updated: 2026-08-09  
Implementation classification: **Runtime substantially implemented; canonical Player technical QA certified; product and future lifecycle gaps remain**  
Related decisions: IF-ADR-001, IF-ADR-007, IF-ADR-012, IF-ADR-015, IF-ADR-016

## Context

A Logical Player is a Session participant, while an Actor is contextual gameplay content. Joining, Host provisioning/adoption, Actor selection, logical preparation, physical materialization, input/camera admission, readiness contribution, Activity exit and reentry must remain distinct and diagnosable operations.

## Decision

Player participation is Session-scoped and keyed by typed Slot identity. Route/Activity may project eligible Players and own contextual Actor materialization, but they do not own Session participant identity.

The lifecycle separates:

```text
Session Slot configuration
Joining / admission
Local Player Host provisioning or adoption
Logical Player participation
Actor selection
Logical Actor preparation
physical Actor materialization
input / camera / gameplay admission
Activity readiness contribution
contextual release / reconcile
```

Scene-Provided and Manager-Provisioned are supported peer provisioning modes. They converge on the same Session/Slot/Actor authority model without collapsing Host and Actor identity.

Reconciliation must be idempotent, occurrence-aware, revision-correlated and free of silent fallback or consumer access to internal authority operations.

## Player Session dependency

IF-ADR-016 defines the canonical initialization model:

```text
PlayerSessionProfile
  Supported Slots
  Initial Joining
  Host Provisioning
  Actor Resolution
```

There is no independent Session Capacity and no per-Slot Host Provisioning override.

## Readiness and Player control-plane boundary

An Activity may project a Slot that is not Joined yet. When its Player requirement is `JoinedSlots` or stronger, the package may represent the required Player contribution as:

```text
Preparing / WaitingForJoin
```

This is not a failure and must not be silently converted to Ready, NoParticipants, Optional participation or timeout success.

When the same Activity uses `WaitCovered`, operations required to advance Player readiness must remain reachable through an external/control-plane path. `RequestJoin` is such a control-plane operation; it is not normal Player gameplay input.

The package does not infer Canvas visibility, raycast reachability or game-specific UI intent. Product validation may warn about risky compositions but must not auto-change readiness policy, participation requirement, Slot projection or Joining state.

## Architectural constraints

- Runtime authority is scoped, typed and lifetime-explicit.
- Required invalid configuration fails explicitly and diagnostically.
- Consumer code does not depend on internal runtime modules, reflection, object-name inference or implicit global lookup.
- Editor tooling is idempotent, non-destructive and exposes technical evidence in Advanced / Debug.
- QA proves technical contracts; FIRSTGAME proves real consumer usability.
- Authoring/composer components do not execute gameplay by accident.

## Current implementation coverage

The package supports:

```text
Scene-Provided Host adoption
Manager-Provisioned explicit Join
stable Slot admission
Actor selection
Logical Actor preparation
physical Actor materialization
gameplay admission
exact readiness contribution evidence
Activity occurrence / Session revision reconciliation
contextual release / reentry
scoped public consumer commands and immutable observation
```

The late-join/active-Activity reconciliation gap is no longer an open technical-QA blocker for the current certified Player surface.

## Current QA evidence — certified 2026-08-09

Canonical master verdict:

```text
PLAYER QA CERTIFIED
```

Relevant evidence:

```text
Scene-Provided route/negative matrix  PASS — 25 cases
Manager waiting projection            PASS — 14 cases
Actor selection runtime binding       PASS — 13 cases
Player gameplay admission             PASS — 114 cases
Public Surface Q1                     PASS — 28 cases
Public Surface Q2                     PASS — 36 cases
Activity Session Projection           PASS — 30 cases
```

This replaces the previous statement that canonical Player QA needed to be rebuilt/revalidated.

See `../IMMERSIVE-FRAMEWORK-PLAYER-QA-CERTIFICATION-2026-08-09.md`.

## Current FIRSTGAME evidence

FIRSTGAME remains the product/usability gate. Technical QA success does not prove that a developer unfamiliar with the internals can compose the feature comfortably.

The next manual proof should separate the two provisioning modes:

```text
Demo02 — Scene-Provided
Demo03 — Manager-Provisioned
```

and demonstrate Single before Multiplayer variants.

## What remains

- FIRSTGAME manual product proof for the accepted Scene-Provided and Manager-Provisioned authoring flows.
- P5 decision on whether any additional Create-menu/template/Composer support is justified by observed friction.
- Session Player Leave and device disconnect/reconnect as separate future contracts.
- Session-Persistent Player source as a separate future contract.
- Broader negative/hardening matrices only where new evidence identifies a concrete gap.

Do not reintroduce Capacity, separate provisioning Profile or per-Slot Host Provisioning overrides to satisfy historical tests.

## Completion criteria

- Join, Host provisioning/adoption, Actor selection, preparation, materialization, admission, release and reconcile have explicit typed evidence.
- Late Join and Activity reentry are deterministic and occurrence-safe.
- Consumer UI never invokes internal preparation/reconcile modules.
- Authoring, QA and FIRSTGAME use the same official package contracts.
- Player operations required to establish Activity readiness can be composed independently from gameplay capabilities gated until Ready.
