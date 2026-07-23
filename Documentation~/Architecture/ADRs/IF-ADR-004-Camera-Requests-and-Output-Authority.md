# IF-ADR-004 — Camera Requests and Output Authority

Status: Accepted
Last updated: 2026-07-23
Supersedes: Camera C1–C9 plans, audits, manifests and product ADR fragments
Superseded by: none

## Context

Virtual-rig authoring, physical output ownership and runtime winner selection
must remain separate. Route, Activity, Player and Session need scoped camera
intent without toggling cameras directly or competing through Cinemachine
priority.

## Decision

Reusable presentation intent uses `CameraRigRecipe`. A concrete
`CameraRigComposer` validates and idempotently Apply/Rebuilds the virtual
Cinemachine rig, targets and framing. It does not create the persistent physical
output or select the active rig.

The persistent single-output topology is session-owned in `UIGlobal`:

```text
Unity Camera + CinemachineBrain
-> CameraOutputSessionBinding
-> CameraOutputSession
-> CameraOutputContext
-> CameraOutputRigApplicator
```

Typed Player, Activity, Route and Session publishers submit or release requests.
`CameraOutputContext` is the only winner-selection authority. Default
precedence is:

```text
Player 50 < Activity 100 < Route 200 < Session 300
```

Player publishes normal gameplay presentation only after eligibility. Activity
and Route overrides become available with lifecycle but publish only through an
explicit request. Session override covers transitions and releases before
destination content is revealed.

Framework Core injects the persistent output into scoped consumers. Gameplay
scenes do not serialize cross-scene output references.

## Accepted scope

- Cinemachine-based virtual rig authoring and materialization.
- One persistent physical output for the current single-player product.
- Typed request/release, deterministic precedence and diagnostics.
- Explicit target sources and Player gameplay eligibility.
- Activity, Route and transition-scoped Session overrides.

## Rejected scope

- `Camera.main`, name lookup, singleton or global camera manager.
- Direct physical Camera enable/disable as selection policy.
- Independent Cinemachine-priority competition by owners.
- `CameraRigComposer` owning output/session arbitration.
- Cross-scene serialized references to the persistent output.

## Consequences

Presentation intent is reusable while runtime authority remains centralized per
output. Releasing an override deterministically restores the next valid request.

## Current implementation coverage

Recipe/Composer authoring, Cinemachine materialization, output context/session,
typed bindings, request arbitration and persistent-output injection exist.
Current documented product scope is one physical single-player output.

## Pending decisions

- Multiple physical outputs and split-screen ownership.
- Product policy for output reassignment across local Players.
