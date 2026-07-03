# F23-ADR-PAUSE-003 - Pause Content / Overlay / Input Intent Boundary

Status: Closed  
Phase: F23 - Pause Content / Overlay / Input Intent Boundary  
Type: Framework Intent / Requirement Boundary  
Last updated: 2026-06-26

## Context

F20 closed logical Pause state and Gate interaction. F21 closed Save / Snapshot / Preferences / Progression Save Foundation. F22 closed Loading Operation / Progress / Readiness Boundary.

F23 exists to define Pause intent language before any Unity build surface is created. It must not become overlay implementation, input wiring or gameplay adapter work.

## Decision

F23 is intent/requirement-only.

Canonical runtime contracts:

- `PauseContentRequirement`
- `PausePresentationIntent`
- `PauseInputSignal`
- `PauseInputIntent`

F23 may describe what Pause needs from later surfaces. It does not perform the work.

## F23 Plan

| Cut | Name | Result |
|---|---|---|
| F23A | Pause Content / Overlay / Input ADR Plan | Boundary accepted. |
| F23B | Pause Content Requirement Contracts | Content requirement language accepted. |
| F23C | Pause Presentation Intent Contracts | Presentation intent language accepted. |
| F23D | Pause Input Intent Contracts | Input signal and intent language accepted. |
| F23E | Pause Boundary Intent Smoke + Adapter Deferral | Intent-only boundary verified conceptually. |
| F23F | Closure + Usage Guide | F23 closed. |

## Explicit Deferrals

F23 does not promise or create:

- overlay adapter execution
- Content Anchor binding execution
- `RuntimeContentAnchorBinding`
- Input System wiring
- Canvas
- prefab
- scene object
- ScriptableObject
- `Time.timeScale` policy
- gameplay adapter
- asmdef change

These belong to F24 when they are Unity build-surface/lifecycle work, or to F25+ when they are adapter module/product behavior.

## Consequences

F23 keeps Pause core language stable while preserving the next boundaries:

- F24 builds minimal Unity surfaces and lifecycle wiring.
- F25 defines optional adapter modules after F24 proves the real framework path.
