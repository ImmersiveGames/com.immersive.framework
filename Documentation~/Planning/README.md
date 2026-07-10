# Planning

This folder contains detailed accepted plans. It is not the first place to determine the current cut.

## Authority

```text
Current/05-Execution-Status.md
  current operational truth

Current/00-Current-State.md
  supported product/runtime state

Current/01-Roadmap.md
  block ordering and status

Planning/Plano de Firstgame.md
  detailed scope, questions and acceptance criteria
```

## Main plan

[Plano Consolidado — Retomada do Immersive Framework após CameraComposer](Plano%20de%20Firstgame.md)

Accepted sequence:

```text
R0
-> P2
-> G1
-> P3
-> C9
-> S1
```

Current position:

```text
P2 closed
G1 active
G1A next
```

## Planning rule

A detailed plan may describe an intended architecture that is later rejected or simplified by implementation evidence.

When that occurs:

1. record the accepted outcome in `Current/05-Execution-Status.md`;
2. update `Current/00-Current-State.md` and `Current/01-Roadmap.md`;
3. keep the original plan as intent/history;
4. do not follow outdated unchecked boxes mechanically.

## Repository rule

Git repositories are read-only. Changes are delivered as ZIP deltas.
