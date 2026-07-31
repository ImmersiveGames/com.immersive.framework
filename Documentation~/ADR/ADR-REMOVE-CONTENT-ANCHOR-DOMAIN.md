# Remove Experimental Content Anchor Domain

Status: Accepted

## Decision

Remove the experimental Content Anchor domain without a compatibility layer.

## Context

- Passive discovery had no demonstrated product consumer.
- Route and Activity scene composition already determine content ownership.
- Root, Slot, and Point had no operational semantics.
- The materialization bridge duplicated Transform, owner, and anchor metadata instead of consuming scene-authored anchors.
- Any future staging, preload, or deferred-content product requires a different architecture.

## Consequences

- No compatibility layer or migration to staging is provided.
- No preload or staging system is introduced by this decision.
- A future deferred-content product starts from new requirements.
- Generic Runtime Content infrastructure remains when independent from the removed domain.
