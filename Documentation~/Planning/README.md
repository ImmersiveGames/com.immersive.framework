# Planning

This folder contains detailed accepted plans and historical intent. It is not the
first place to determine the current cut.

## Authority

```text
Current/05-Execution-Status.md
  current operational truth

Current/00-Current-State.md
  supported product/runtime state

Current/01-Roadmap.md
  block ordering and status

Planning/Plano de Firstgame.md
  original detailed intent and acceptance questions
```

## Current position

```text
R0
  closed

P2
  closed at the accepted consumer-owned movement boundary

C9
  closed at the current single-output camera product level

G1
  active as Consumer Route Loop

P3
  ordered after G1

S1
  ordered when meaningful persistent state exists
```

## G1 scope correction

The original consolidated plan described G1 as a “minimal playable loop” that
required a simple objective, interaction, resettable state and Activity Restart.

That description is retained as historical intent, but it is not the current
operational requirement.

The framework does not own gameplay objectives, win conditions, combat,
missions or interaction semantics. The accepted G1 framework proof is:

```text
Bootstrap
-> Menu Route
-> Gameplay Route
-> Ending Route or Menu Route
-> controlled return/re-entry
```

FIRSTGAME may add singular gameplay content selected for the demonstration.
That content does not become a package contract automatically.

Before G1 implementation, `G1A — FIRSTGAME Route Loop Audit and Scope Lock`
must incorporate the user's additional requirements and determine whether the
existing Route flow already closes the block.

## Planning rule

A detailed plan may describe an intended architecture or product proof that is
later rejected, simplified or executed in a different order.

When that occurs:

1. record the accepted outcome in `Current/05-Execution-Status.md`;
2. update `Current/00-Current-State.md` and `Current/01-Roadmap.md`;
3. keep the original plan as intent/history;
4. do not follow outdated unchecked boxes mechanically.

Camera C9 was executed before P3 because it became a direct blocker for the real
consumer flow. Its closure is now authoritative.

## Repository rule

Git repositories are read-only. Changes are delivered as ZIP deltas.
