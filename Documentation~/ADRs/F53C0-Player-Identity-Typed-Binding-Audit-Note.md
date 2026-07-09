# ADR Note - F53C0 Player Identity and Typed Binding Audit

Status: Accepted
Last updated: 2026-07-09
Supersedes: none
Superseded by: none

## Context

F52A-F52C proved the PlayerControl / Unity PlayerInput chain in QA. F53B proved the current real FIRSTGAME `PlayerPrototype` can carry the F52 targets with the real `PlayerInput`.

Before creating new adapters or a facade, the player identity chain must separate identity, reference, binding and diagnostic evidence.

## Decision

The player identity model is:

- `PlayerSlotDeclaration` owns slot identity.
- `PlayerActorDeclaration` owns player actor identity.
- `UnityResetSubjectAdapter` should source real player reset identity from `PlayerActorDeclaration`.
- `PlayerInput`, camera `Transform`, `Camera` and declaration components are runtime references and should be direct typed references.
- `GameObject.name` is diagnostic only.
- Action map/action names are tolerated strings only when validated against `InputActionAsset`.

## Accepted Scope

- Document the current string-based binding risks.
- Document canonical identity sources.
- Document FIRSTGAME real player component responsibility.
- Define the safe order before an authoring facade.

## Rejected Scope

- No runtime implementation.
- No new adapter or facade.
- No movement, command routing, actor spawning or save/progression.
- No scene, prefab, asset or asmdef edits.

## Consequences

- Existing F51/F52 contracts remain valid.
- FIRSTGAME editor tooling that finds `PlayerPrototype` by name is marked for replacement.
- Repeated slot/action-map strings remain tolerated only as current evidence and validation guards.
- A future facade must consume the existing targets and fail fast rather than adding fallback lookup.

## Current Implementation Coverage

- FIRSTGAME real player has `PlayerSlotDeclaration`, `PlayerActorDeclaration`, `PlayerSlotOccupancy`, `UnityPlayerInputGateAdapter`, `UnityResetSubjectAdapter`, `PlayerInput` and F52 target components.
- FIRSTGAME real player does not yet prove the F51 PlayerView / camera target / camera activation chain.
- FIRSTGAME camera currently uses typed `FrameworkCameraAnchorHost` `Transform` references, but its editor setup finds the player by object name.

## Pending Decisions

- Whether the real camera target should be player root `Transform` or a child anchor.
- Whether F52 target components should keep serialized `expectedPlayerSlotId` after facade creation or derive it from `PlayerSlotDeclaration`.
- Whether the player reset scope remains `Activity` once save/progression and route persistence are designed.
