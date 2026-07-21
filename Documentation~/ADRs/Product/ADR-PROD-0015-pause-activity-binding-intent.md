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
- P2.1C lifecycle integration through a single session-scoped host module.
- Activation only after the official Player participant materializes/adopts the
  target host; release before Player exit and Activity scene release.

## Rejected scope

- Player/host discovery or implicit player selection.
- Host, `PlayerInput`, slot, prefab, QA asset, scene-path or singleton references.
- Multiplayer policy beyond explicit first-cut rejection.

## Consequences

P2.1C consumes typed Activity Player lifecycle evidence rather than inspecting
scene hierarchy. `ActivityFlowRuntime` resolves authoring only from explicit
materialized Activity roots, uses its canonical transition sequence, activates
after Player admission/materialization, and releases before Player teardown or
Activity scene unload. A release failure remains blocking and retains P2.1B
evidence for retry.

## Current implementation coverage

P2.1A provides authoring, immutable contracts and explicit-root validation.
P2.1B provides the reusable internal registration context, its immutable
operation/snapshot contracts, transactional component behavior and package
contract tests. P2.1C composes that context once in `FrameworkRuntimeHost` and
connects it to the ordered Activity lifecycle. QA does not use the feature and
no FIRSTGAME validation was performed.

## Pending decisions

- Product policy and contracts for more than one eligible Local Player.
