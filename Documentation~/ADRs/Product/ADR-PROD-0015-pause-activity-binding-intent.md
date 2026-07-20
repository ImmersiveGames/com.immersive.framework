# ADR-PROD-0015 — Pause Activity Binding Intent

Status: Accepted  
Last updated: 2026-07-20  
Supersedes: none  
Superseded by: none

## Context

`PauseProductBindingRuntimeContext` is session-owned, while an official Local
Player Host is provisioned/admitted by the Player lifecycle and is not a root
of an Activity scene. A scene-local `PausePlayerInputBinding` declaration
cannot express that an Activity wants Pause for this admitted host.

## Decision

An Activity may declare `PauseActivityBindingAuthoring`. Its immutable
`PauseActivityBindingIntent` means that product Pause binding is required for
the officially admitted Local Player of that Activity.

The declaration is Activity-owned; the Pause runtime remains session-owned.
The future registration token is Activity-scoped. The first policy supports one
eligible Local Player and one binding. Multiple declarations or eligible
players must fail explicitly; no implicit selection is allowed.

## Accepted scope

- Passive designer-facing authoring component.
- Immutable intent and explicit-root/declaration validation.
- Zero declarations as valid absence and one declaration as required intent.
- Blocking duplicate declaration diagnostic.

## Rejected scope

- Player/host discovery, runtime composition, token registration or release.
- Host, `PlayerInput`, slot, prefab, QA asset, scene-path or singleton references.
- Changes to Pause, Player, Activity, Scene Lifecycle or host runtime behavior.
- Multiplayer policy beyond explicit first-cut rejection.

## Consequences

P2.1B must consume typed Activity Player admission evidence rather than inspect
scene hierarchy. P2.1C must register and release the exact binding in the
ordered Activity lifecycle, restoring Pause state before unload.

## Current implementation coverage

P2.1A provides authoring, immutable contracts, explicit-root validation and
contract tests only. No Pause runtime, binding, token or lifecycle integration
is created by this decision.

## Pending decisions

- Exact typed admitted-host evidence exposed to the Pause materializer.
- Activity participant ordering relative to Player admission and scene release.
- Product policy and contracts for more than one eligible Local Player.
