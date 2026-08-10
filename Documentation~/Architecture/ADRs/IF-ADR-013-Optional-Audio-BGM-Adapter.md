# IF-ADR-013 — Optional Audio BGM Adapter

Status: **Accepted / Experimental**  
Last updated: 2026-08-09  
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-006, IF-ADR-010

> Current implementation, QA and FIRSTGAME integration status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage. UX observations are qualitative
> product feedback and are not part of functional completion arithmetic.

## Context

BGM integration is optional and may depend on an external audio package. The
framework needs a narrow adapter boundary without making audio-specific authority
part of Route/Activity identity or core lifecycle.

## Decision

The framework exposes a narrow optional BGM integration with Route/Activity
bindings and explicit policies such as own cue, Route cue, retained Activity cue
or silence.

Accepted requests and releases return typed evidence. Absence of the optional
audio integration must not corrupt core framework lifecycle.

## Constraints

- Core lifecycle works when the audio package/adapter is absent.
- Every accepted request has deterministic release/restoration semantics.
- Audio-specific identity does not become Route/Activity identity.
- Missing required configured audio authority fails explicitly.
- No scene search, singleton or global audio manager is introduced.

## Experimental promotion

Promotion from Experimental requires the technical policy matrix and real-game
integration to be proven for the supported boundary.

UX polish is not a promotion score. Product improvements may be made from real
use, but Experimental status is about supported integration/contract confidence,
not aesthetic Inspector maturity.
