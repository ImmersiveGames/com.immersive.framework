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
P2.1B adds an internal reusable registration context whose Activity scope is
`PauseActivityBindingScope`. It is a narrow Pause projection of the canonical
Activity runtime owner plus lifecycle-provided entry sequence; it is not an
admission token or a new lifecycle counter. This protects against foreign and
stale releases through the full active Activity lifetime. The first policy
supports one eligible Local Player and one co-located binding. Multiple
declarations, duplicate host evidence or eligible players fail explicitly; no
implicit selection is allowed.

## Accepted scope

- Passive designer-facing authoring component.
- Immutable intent and explicit-root/declaration validation.
- Zero declarations as valid absence and one declaration as required intent.
- Blocking duplicate declaration diagnostic.
- Internal activity-scoped runtime supplied with explicit host evidence and the
  official Pause binding port.
- Transactional binding registration/release, including retry after release
  failure and foreign/stale scope rejection.

## Rejected scope

- Player/host discovery or implicit player selection.
- Host, `PlayerInput`, slot, prefab, QA asset, scene-path or singleton references.
- Integration in `FrameworkRuntimeHost`, Activity/Route/Scene lifecycle or
  Player provisioning/runtime modules.
- Multiplayer policy beyond explicit first-cut rejection.

## Consequences

P2.1B consumes typed Activity Player admission evidence rather than inspecting
scene hierarchy. P2.1C must invoke its activation/release operations in the
ordered Activity lifecycle, restoring Pause state before unload.

## Current implementation coverage

P2.1A provides authoring, immutable contracts and explicit-root validation.
P2.1B provides the reusable internal registration context, its immutable
operation/snapshot contracts, transactional component behavior and package
contract tests. `FrameworkRuntimeHost` does not call it, Activity lifecycle is
not connected, and QA does not use the feature.

## Pending decisions

- Activity participant ordering relative to Player admission and scene release.
- Product policy and contracts for more than one eligible Local Player.
