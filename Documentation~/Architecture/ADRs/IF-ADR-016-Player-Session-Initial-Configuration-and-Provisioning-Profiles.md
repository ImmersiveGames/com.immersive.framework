# IF-ADR-016 — Player Session Initial Configuration

Status: **Accepted / Reopened for implementation reconciliation**  
Last updated: **2026-08-14**  
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-003, IF-ADR-012, IF-ADR-015, IF-ADR-019, IF-ADR-020  
Current Player lifetime reconciliation: [2026-08-14 Player Physical Lifetime Reopen](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-REOPEN-2026-08-14.md)

## Context

Player Session needs one authorable source for initial intent without turning Profiles into
live Session state.

These concerns remain separate:

```text
Host Provisioning
Slot allocation / assignment
Actor selection
Session Player Leave
```

## Decision

`PlayerSessionProfile` is the only Profile required to configure initial Player Session
intent.

```text
PlayerSessionProfile
├── Supported Slots
├── Initial Joining
├── Host Provisioning
│   ├── Scene Provided
│   └── Manager Provisioned
└── Actor Resolution
    ├── Resolve Configured Default
    └── Leave Unresolved
```

An explicit creation-time Profile replaces the default completely. No field merge and no
invalid-source fallback are allowed.

## Supported Slots and Joining

`Supported Slots` is the complete structural Slot universe and untargeted Join order.

```text
Untargeted Join
  Joining Open
  -> first eligible vacant Supported Slot

Targeted Join
  Joining Open
  + exact supported vacant Slot
  -> reserve/admit exact Slot
```

Targeted Join has no fallback.

Joining Open/Closed controls entry only. Explicit Leave remains possible for a current
Joined Player even when Joining is Closed.

## Host Provisioning — revised meaning

Host Provisioning is a Session-wide **acquisition origin policy**.

It answers:

```text
How is the candidate physical Player supplied before successful admission?
```

It no longer defines divergent post-admission lifetime ownership.

### Manager Provided

```text
Framework creates/provides candidate
        ↓
validate/admit
        ↓
Session Player occurrence owns admitted physical representation
```

### Scene Provided

```text
consumer scene supplies exact candidate
        ↓
Framework validates/adopts
        ↓
successful admission
        ↓
Session Player occurrence owns admitted physical representation
```

Ownership transfer occurs only on successful adoption.

Rejected/failed Scene-Provided admission leaves the candidate consumer-owned.

## No per-Player persistence mode

There is no:

```text
Persistent Player
Session Persistent
Persist Actor Between Activities
```

authoring toggle.

Physical continuity across Activity changes is canonical post-admission Session behavior,
not an optional Profile policy.

## Actor Resolution

Actor Resolution remains independent from Host Provisioning and Slot assignment.

```text
Resolve Configured Default
or
Leave Unresolved
```

Actor selection is Session mutable intent, not physical hot-swap authority.

## Runtime authority

Profile resolves once into immutable effective configuration. The created Session owns
mutable runtime state:

```text
Joining state
Slot occupancy
Session Player occurrence/revision
Actor selection
admitted physical Player ownership/state
Leave state/result
```

Activity and Route changes do not silently reapply `PlayerSessionProfile`.

## Leave consequence

IF-ADR-020 owns individual terminal release.

Because both provisioning modes converge on Session ownership after successful admission,
Leave must release the admitted physical Player through the appropriate semantic release
path for the current occurrence.

Provisioning origin remains diagnostic and may require different acquisition/release
adapters, but it does not preserve external runtime lifetime ownership after successful
adoption.

## Rejected behavior

- Separate provisioning Profile.
- Capacity as a second Session limit.
- Per-Slot Host Provisioning.
- Per-Player physical persistence option.
- Scene-Provided remaining Activity-owned after successful admission.
- Runtime Profile reapplication on Activity changes.
- Silent fallback between provisioning modes.
