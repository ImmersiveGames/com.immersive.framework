# Planning

This folder contains accepted plans and historical intent. It is not the first
place to determine the current cut.

## Authority

```text
Current/05-Execution-Status.md
  current operational truth

Current/00-Current-State.md
  supported product/runtime state

Current/01-Roadmap.md
  selected ordering and validation gate

Planning/Plano de Firstgame.md
  original detailed intent and acceptance questions
```

## Current position

H2 is closed and Unity-validated in `1.0.0-preview.16`. No post-H2
implementation lane is active or ordered. The authoritative status is
`../Current/05-Execution-Status.md`.

## Planning rule

A detailed plan may describe an intended architecture or product proof that is
later rejected, simplified or executed in a different order.

When that occurs:

1. Record the accepted outcome in `Current/05-Execution-Status.md`.
2. Update `Current/00-Current-State.md` and `Current/01-Roadmap.md`.
3. Keep the original plan as intent/history.
4. Do not follow outdated unchecked boxes mechanically.

Do not treat plans in this folder as an active backlog without the current
tracker selecting that work explicitly.

## Repository rule

Git repositories are read-only. Changes are delivered as ZIP deltas.
