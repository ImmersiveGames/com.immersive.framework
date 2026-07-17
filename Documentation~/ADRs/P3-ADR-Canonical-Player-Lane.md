# P3 ADR — Canonical Player Lane

Status: Accepted  
Last updated: 2026-07-16  
Supersedes: F45 passive Slot identity and F49/F51/F52 passive binding decisions  
Extended by: `Product/ADR-PROD-0013-scene-local-player-admission.md`  
Superseded by: none

## Context

The previous passive topology duplicated authority already owned by the P3 Session participation, admission, Actor preparation and gameplay-admission chain.

The canonical runtime-created Player path uses manual local join. A second valid product construction places one local Player Host directly in an Activity scene. That construction requires admission without pretending that the framework provisioned the physical object.

## Decision

P3 is the only canonical Player participation lane:

```text
profile -> participation -> physical source -> host admission -> Actor
-> preparation -> occupancy -> input -> camera -> admission -> Activity
```

The supported physical sources are:

```text
Manual local join
  PlayerInputManager provisions the Host after an authorized Slot reservation.

Scene Local Player Admission
  an explicitly referenced Activity-scene Host is admitted without provisioning.
```

The participation context assigns `PlayerSlotId`. Player prefabs and scene Hosts do not pre-author Slot identity. Pause/InputMode requires the canonical P3 provisioning/admission evidence rather than a passive Slot declaration.

## Accepted scope

- preserve `PlayerSlotId`, `PlayerSlotProfile`, allocation snapshot/state/token;
- preserve Actor selection, preparation, gameplay stages and Activity admission;
- preserve `PlayerInputManager` as the sole runtime provisioner for local Players it creates;
- add explicit admission for an externally owned scene-existing local Player Host;
- validate both paths through typed issues shared by runtime and Editor;
- migrate FIRSTGAME only after Framework and QA pass the required gate for the selected cut.

## Rejected scope

- F49/F51/F52 passive topology and binding modules;
- `PlayerSlotDeclaration`, `PlayerSlotOccupancy` and passive Slot sets;
- `SessionPlayerInputManagerDeclaration` and manager evidence wrappers;
- a second local Player spawner beside `PlayerInputManager`;
- compatibility aliases, shims, fallback discovery or pre-authored Slot identity;
- using `PreAuthoredPlayerComposer` as P3 admission authority.

## Consequences

The removals are intentionally destructive. A failed gate is fixed inside the owning cut; removed APIs are never restored as rollback.

`PreAuthoredPlayerComposer` may remain temporarily only while Camera, shared Editors and QA are decoupled. It is not a canonical P3 source and is removed before Scene Local Player Admission becomes the supported replacement surface.

## Current implementation coverage

H0–H4 are implemented in Framework/QA source. H5 import, compile and smokes remain manual gates and are not claimed as passed by source inspection.

The read-only source baseline inspected on 2026-07-16 is:

```text
com.immersive.framework  385c957a8fefb53f0daf395c662ffa7d5fedc996
QAFramework               993f8e698edb8c826054c9f8faa8bd344fbc8013
package version           1.0.0-preview.15
```

`ADR-PROD-0013` freezes the next architecture. The next implementation cut is the decoupling of Camera, shared Editors and QA from `PreAuthoredPlayerComposer`.

## Pending implementation

```text
P3M2 — PreAuthored consumer decoupling
P3M3 — destructive PreAuthored removal
P3M4 — Scene Local Player Admission package promotion
P3M5 — QA transaction and regression proof
P3M6 — FIRSTGAME usability proof
```
