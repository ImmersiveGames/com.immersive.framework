# IF-ADR-021 — Player Authority and Initial Placement Reconciliation

Status: **Accepted / Reconciled — Model B selected; Route lifecycle cut implemented, replacement QA pending**
Date: **2026-08-23**  
Related decisions: IF-ADR-003, IF-ADR-015, IF-ADR-019, IF-ADR-020, IF-ADR-021

## Purpose

This reconciliation preserves the 2026-08-23 authority matrix and closes its open
model choice by accepting Model B. The original reconciliation was documentation
only; the later Route lifecycle cut is recorded in the ADR implementation coverage.

## Authority matrix

Session owns Player Session state, Slots, Join/Leave, Host Provisioning, Actor
Selection/Resolution, logical Player occurrence and admitted physical Player lifetime.

Route owns the Primary Scene, Route Content composition/lifetime, Startup Activity,
and baseline spatial-entry intent for the current Route occurrence.

Activity owns participation, readiness, contextual representation/gameplay/input/camera,
optional Activity Content, and optional explicit contextual relocation intent.

Scene owns only physical location and availability of authoring/runtime objects.

A Route- or Activity-scoped consumer surface remains an access boundary only. It
does not transfer Session Player provisioning or lifetime authority. Physical scene
ownership is not semantic Route or Activity placement ownership.

ActivityContentProfile = null is valid. The Route Primary Scene remains Route-owned
and cannot be reclassified as Activity-owned.

## Accepted Model B

### Route spatial entry

Route owns the baseline spatial-entry intent for a Session-owned Player entering the
current Route spatial occurrence. Same Session Player occurrence does not mean same
Route spatial occurrence.

The Route must explicitly preserve current/authored pose or require explicit Route
placement. A Route change authorizes policy evaluation but not an implicit teleport.

Explicit Route placement identity is RouteId + PlayerSlotId -> Anchor.

Its eligible materialization scope is the current Route Primary Scene plus current
Route Content scenes. Activity Content, Persistent Content, unrelated loaded scenes,
Editor-open scenes and arbitrary global discovery are excluded.

### Activity explicit relocation

Activity may own an opt-in explicit contextual relocation intent. An Activity
transition preserves current pose by default; only explicit relocation may resolve
and apply a contextual pose.

It is not Route entry, Join, admission, Player creation, Actor creation or physical
lifetime authority. Activity change alone never authorizes relocation.

Explicit relocation identity is ActivityId + PlayerSlotId -> Anchor.

Its eligible materialization scope is current Route Primary, current Route Content
and current Activity Content. This does not alter scene ownership. Bindings for
other Activities are semantically ignored, not duplicates.

### Shared determinism rules

For either explicit operation, zero exact bindings fails, one applies, and more than
one fails as duplicate. No hierarchy, name, tag, first-found, default anchor,
scene-membership or arbitrary loaded-scene fallback is permitted. Anchors materialize
evidence only; they do not become parent or lifetime authority.

## Readiness and provisioning consequences

Route explicit placement produces observable spatial-entry evidence before spatial
readiness for that Route occurrence. Activity placement evidence is requested only
for an Activity that explicitly opted into relocation. Failures remain explicit;
preserved pose is not a failure and a failure must not silently discard prior pose.

Manager-Provisioned admission while a Route is active evaluates Route spatial entry
even with no Activity or with ActivityContentProfile = null. Scene-Provided admission
may preserve the authored/current pose through explicit Route policy. After admission,
both modes retain the same Session-owned physical lifetime.

## Historical boundary and replacement evidence

The runtime currently discovers Initial Placement only in ActivityOwnedScenes. That
implementation and ADR-021 9/9 result remain historical evidence for the former
narrow boundary; they are not certification of Model B.

Superseded scene/discovery clauses are: Activity placement evidence ->
ActivityOwnedScenes only; and AnchorOutsideOwnedSceneRejected when owned meant
exclusively Activity-owned scene.

The historical Full Player 25/25 certification remains intact for its Session
physical-lifetime and continuity evidence. Nothing in Model B changes those claims.

## Remaining runtime cut

- Activity relocation authoring/runtime contract and exact `ActivityId + PlayerSlotId` discovery.
- Public occurrence-correlated readiness/evidence for Route entry and Activity relocation.
- Replacement QA for Primary/Route Content, shared Activity relocation, duplicate and
  missing bindings, ineligible scenes, Scene-Provided pose preservation,
  Manager-Provisioned active-Route join and null Activity Content.
