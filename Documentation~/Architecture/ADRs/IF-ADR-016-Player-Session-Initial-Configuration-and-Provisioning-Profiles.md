# IF-ADR-016 — Player Session Initial Configuration

Status: **Accepted / Reconciled / Implemented / Current Player QA PASS**  
Last updated: **2026-08-26**  
Related decisions: IF-ADR-001, IF-ADR-002, IF-ADR-003, IF-ADR-012, IF-ADR-015, IF-ADR-019, IF-ADR-020  
Historical reopen record: [2026-08-14 Player Physical Lifetime Reopen](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-REOPEN-2026-08-14.md)  
Historical closure record: [2026-08-15 Player Physical Lifetime Recertification](../Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-RECERTIFICATION-2026-08-15.md)  
Current Actor-selection closure: [IF-ADR-015B — Player Actor Selection Public Surface Certification — 2026-08-26](../Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)

## Context

Player Session needs one authorable source for initial intent without turning Profiles into live Session state.

These concerns remain separate:

```text
Host Provisioning
Slot allocation / assignment
Actor selection
Session Player Leave
```

## Decision

`PlayerSessionProfile` is the only Profile required to configure initial Player Session intent.

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

An explicit creation-time Profile replaces the default completely. No field merge and no invalid-source fallback are allowed.

## Supported Slots and Joining

`Supported Slots` is the complete structural Slot universe and untargeted Join order.

```text
Untargeted Join
  Joining Open
  -> first eligible vacant Supported Slot
```

The current designer-facing `PlayerSessionJoinCommandTrigger` uses this ordinary untargeted Join behavior. Exact-Slot public Join is not part of the delivered command surface.

If a future public exact-Slot Join contract is introduced, it must target one exact eligible supported Slot and must not silently fall back to another Slot.

Joining Open/Closed controls entry only. Explicit Leave remains possible for a current Joined Player even when Joining is Closed.

## Host Provisioning — reconciled meaning

Host Provisioning is a Session-wide **acquisition origin policy**.

It answers:

```text
How is the candidate physical Player supplied before successful admission?
```

It does not define divergent post-admission lifetime ownership.

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

Physical continuity across Activity changes is canonical post-admission Session behavior, not an optional Profile policy.

## Actor Resolution

Actor Resolution remains independent from Host Provisioning and Slot assignment.

```text
Resolve Configured Default
or
Leave Unresolved
```

### Resolve Configured Default

The Session may resolve only the configured `DefaultActorProfile` according to the accepted selection policy.

```text
configured default exists and is valid
  -> may select that Actor

configured default absent / invalid
  -> reject explicitly
```

No implicit substitute Actor is selected.

### Leave Unresolved

`LeaveUnresolved` is a complete valid initial policy, not an error state and not a request for hidden fallback.

It allows:

```text
Join
  -> Slot Joined
  -> Actor remains unresolved
```

A later consumer may issue the delivered public explicit Actor-selection command:

```text
PlayerSessionSelectActorCommandTrigger
  -> exact Player Slot
  -> exact ActorProfile
  -> revision-aware Session validation/commit
```

This is the canonical initial configuration for a Character Selection flow in which game-owned UI presents the available Actor choices after Join.

A `PlayerSessionDefaultActorSelectionCommandTrigger` request under `LeaveUnresolved` rejects with `RejectedDefaultResolutionDisabled`; it does not override the Profile intent.

Actor selection remains Session mutable logical intent, not physical hot-swap authority.

## Public Actor-selection continuation

The delivered public Actor-selection family is:

```text
PlayerSessionSelectActorCommandTrigger
PlayerSessionDefaultActorSelectionCommandTrigger
PlayerSessionReplaceActorSelectionCommandTrigger
PlayerSessionClearActorSelectionCommandTrigger
```

These operate on the already-created Session. They never mutate or reapply the Profile.

Typical Character Selection continuation:

```text
PlayerSessionProfile
  ActorResolution = LeaveUnresolved
        ↓
Join
        ↓
Joined Slot + unresolved Actor
        ↓
game-owned UI chooses ActorProfile
        ↓
Select Actor command
        ↓
Session commits selection
        ↓
existing Actor preparation / provisioning / Activity lifecycle
```

Replace/Clear remain logical pre-preparation operations. They do not authorize physical Actor hot-swap.

## Runtime authority

Profile resolves once into immutable effective configuration. The created Session owns mutable runtime state:

```text
Joining state
Slot occupancy
Session Player occurrence/revision
Actor selection
Actor selection revision
admitted physical Player ownership/state
physical preparation evidence
Leave state/result
```

Activity and Route changes do not silently reapply `PlayerSessionProfile`.

A Joined Player may exist without current Activity representation. This does not cause the Session Profile to re-run and does not invalidate retained Session physical preparation.

## Leave consequence

IF-ADR-020 owns individual terminal release.

Because both provisioning modes converge on Session ownership after successful admission, Leave releases the admitted physical Player through the appropriate semantic release path for the current occurrence.

Provisioning origin remains diagnostic and may require different acquisition/release adapters, but it does not preserve external runtime lifetime ownership after successful adoption.

## Rejected behavior

- Separate provisioning Profile.
- Capacity as a second Session limit.
- Per-Slot Host Provisioning.
- Per-Player physical persistence option.
- Scene-Provided remaining Activity-owned after successful admission.
- Runtime Profile reapplication on Activity changes.
- Treating no-Activity contextual absence as a request to reacquire/recreate physical state.
- Silent fallback between provisioning modes.
- Silent Actor fallback when the configured default is absent or when `LeaveUnresolved` is authored.
- Treating `LeaveUnresolved` as invalid merely because no Actor is selected immediately after Join.
- Consumer physical hot-swap through logical Actor-selection commands.

## Certification

Historical 2026-08-15 evidence remains preserved for the Session-initialization boundary that existed at that time:

```text
Player serialized command identity       5/5 PASS
Player Session                           PASS
SceneProvided provisioning/lifetime      PASS
Manager Provisioned                      PASS
Manager Join Without Activity            PASS
Manager Session Termination              PASS
Public Surface                           PASS
Full Player mandatory contracts          25/25 PASS
```

The later public Actor-selection extension and current Player surface are certified by the 2026-08-26 integrated Full Player result:

```text
PLAYER CURRENT AGGREGATE COMPLETE
mandatoryContracts = 27
executedContracts = 27
passedContracts = 27
actor = PASS
publicSurface = PASS
```

The current result confirms that `ResolveConfiguredDefault` and `LeaveUnresolved` remain valid Session initial policies while explicit Actor-selection commands operate later through the same Session authority.

The historical `5/5` serialized-command and `25/25` Full Player results are not relabeled as tests of the later eight-command Actor-selection surface.
