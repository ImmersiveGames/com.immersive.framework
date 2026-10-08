# IF-ADR-044 — Consumer-Controlled Player Runtime Gameplay Availability

Status: **Accepted — implementation delivered; Unity validation pending**
Proposed: **2026-10-07**  
Type: architecture / Player gameplay / public command boundary / input gating  
Related decisions: IF-ADR-003, IF-ADR-005, IF-ADR-025, IF-ADR-033, IF-ADR-036  
Normative relationship: **This ADR extends IF-ADR-036 with a consumer-controlled transient availability gate. It does not introduce turn, round, AI, or gameplay-rule semantics into the Framework.**

## 1. Context

A game may need a Session Player to remain fully participating while temporarily unable to consume gameplay input.

Examples include alternating human control, turn-based gameplay, or game-specific authority selection. Today a consumer should not need to leave/rejoin the Player, replace the Actor, alter device pairing, mutate Camera Assignment, or directly enable/disable `PlayerInput` to express that state.

The missing boundary is a public, Player-scoped request for transient Runtime Gameplay Availability.

## 2. Decision

The Framework SHALL expose a public Player-scoped command boundary that allows a consumer to acquire and release a transient gameplay-availability block.

The command expresses only:

```text
this Player may not consume gameplay input while this block is held
```

It MUST NOT express why the Player is blocked.

Turn order, rounds, AI participation, tactical phases, active-player selection, and similar rules remain consumer gameplay concerns.

## 3. Effective availability

Consumer control composes with existing runtime gates; it does not override them.

```text
RuntimeGameplayAvailable
    = GameplayReady
    AND existing runtime/input gates allow gameplay
    AND no consumer gameplay-availability block is active
```

Releasing a consumer block MUST NOT restore gameplay consumption while another gate, such as Pause or Transition, remains blocked.

The physical `PlayerInput` posture remains owned by the canonical Framework input writer/adapter. Consumers MUST NOT become competing physical input writers.

## 4. Preserved Player state

Acquiring or releasing this block MUST NOT by itself:

- join or leave the Session Player;
- change Admission or Gameplay Readiness;
- create, destroy, or replace the Actor;
- change Slot identity;
- pair or unpair devices;
- change Camera Assignment, Output, or Subject membership;
- recreate the Player Host or `PlayerInput`.

The Player remains structurally present; only transient gameplay consumption changes.

## 5. Ownership and lifetime

The consumer owns the lifetime of the block it acquires and MUST release it when its gameplay reason ends.

The Framework owns composition of all availability conditions and projection to physical input posture.

Blocks MUST be Player-occurrence scoped and must not survive the occurrence they target.

## 6. Public API constraints

The public API SHOULD model acquire/release ownership explicitly rather than expose ambiguous `SetPlayerActive(bool)` state.

Exact API names and token/handle shape are implementation details, but the contract MUST support safe composition of independent blockers without last-writer-wins behavior.

## 7. Consequences

This enables consumer gameplay to implement patterns such as:

```text
P1 turn -> block P2
P2 turn -> release P2, block P1
AI turn -> block P1 and P2
```

without teaching the Framework what a turn or AI is.

Implementation SHALL first reuse the Runtime Gameplay Availability model established by IF-ADR-036 and existing input-gate infrastructure. A parallel availability authority or direct consumer ownership of `PlayerInput` is rejected.

## 8. Current implementation coverage

- Public acquire/release commands use independent occurrence-scoped tokens on `IPlayerSessionScopedAccess`.
- Session Player gameplay runtime composes consumer blocks with existing Gate availability and projects the result through `UnityPlayerInputGateAdapter`.
- Session Leave and runtime shutdown clear occurrence blocks and their physical projection.
- `GameplayReady`, participation, Actor, device pairing and Camera state remain independent of the consumer block.

## 9. Public authoring surface

The runtime API defined by this ADR remains the authority for acquiring and releasing Runtime Gameplay Availability blocks. The Framework also provides `PlayerGameplayAvailabilityBlockTrigger` as a public Unity authoring component for UnityEvent, UI and sample workflows.

Each component represents one consumer-owned block and retains/releases only the token it acquired. It targets one explicitly authored `PlayerSlotProfile` within its Route or Activity `Scope`; it does not select among Players. `Reason` is diagnostic operation metadata and does not add gameplay-rule semantics.

The authoring component does not represent a turn, active Player or selection policy. It does not control `PlayerInput` directly and does not alter Actor, Camera, devices, participation or `GameplayReady`.

## 10. Validation

Unity validation remains pending. QA must prove at minimum:

1. blocking one Player does not alter participation, Actor, Assignment, devices, or Gameplay Readiness;
2. releasing the block restores gameplay when no other gate is active;
3. releasing the block does not bypass Pause/Transition or another active gate;
4. multiple independent blocks compose safely;
5. Player leave/disposal removes occurrence-scoped block state without residue.
