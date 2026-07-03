# F07 ADR ANCHOR 001 - Content Anchor Declaration

Status: Accepted

## Context

Runtime content needs a stable way to refer to intended placement without binding or instantiating too early.

## Decision

Content Anchor is a space/placement contract.

F7 declares anchors and does not perform runtime binding.

Do not create parallel vocabulary such as `LocalSlot` or `LocalAnchor`.

## Consequences

Placement language stays unified and binding can be added later without redefining authoring terms.

## Guardrails

- Do not make Content Anchor a materializer.
- Do not bind runtime content during declaration.
- Do not introduce competing slot/anchor terms.
