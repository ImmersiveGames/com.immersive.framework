# IF-ADR-023 — Player Actor Runtime Host and Presentation Authority

Status: **Accepted / Implemented / Technical QA Certified**  
Accepted: **2026-08-28**  
Implemented / reconciled: **2026-08-29**  
Last updated: **2026-08-29**  
Type: architecture / Player Actor composition / runtime host / presentation authority  
Related decisions: IF-ADR-003, IF-ADR-007, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021  
Technical certification: [IF-ADR-023 Player Actor Runtime Technical Certification — 2026-08-29](../Reconciliation/IF-ADR-023-PLAYER-ACTOR-RUNTIME-TECHNICAL-CERTIFICATION-2026-08-29.md)

## Context

The stable Local Player Host and Actor selection are separate responsibilities.

```text
LocalPlayerHostAuthoring
├── PlayerInput
└── ActorMount
```

Scene-Provided supplies/adopts that technical Host from consumer composition. Manager-Provisioned creates the same technical Host through Session provisioning authority. After successful admission both modes converge on the same Session-owned Player occurrence.

The previous composition placed too much authority in `ActorProfile`:

```text
ActorProfile
└── LogicalActorHostPrefab
```

That monolithic prefab mixed framework Actor infrastructure, gameplay-owned composition and Actor-specific presentation. The structure also encouraged separate framework/runtime Actor Hosts merely to represent different character visuals.

## Decision

### 1. Preserve the Local Player Host boundary

`LocalPlayerHostAuthoring` remains the stable technical Host for one local Player.

It owns the Player/Input boundary and the `ActorMount`. It is not an Actor, does not execute gameplay and does not become Actor selection authority.

Scene-Provided and Manager-Provisioned continue to differ only in how the Local Player Host is acquired before successful admission.

### 2. PlayerSlotProfile remains Slot authority

`PlayerSlotProfile` remains Slot configuration and identity authority. It may reference a configured `DefaultActorProfile`, but it does not provide:

```text
Local Player Host prefab
Player Actor Runtime Host prefab
Presentation prefab
```

### 3. Player Actor Runtime Host is Actor-independent

The implemented composition is:

```text
LocalPlayerHostAuthoring
├── PlayerInput
└── ActorMount
    └── PlayerActorRuntimeHost
        ├── PlayerActorDeclaration
        └── PresentationMount
            └── ActorProfile.PresentationPrefab
```

Ownership is explicit:

```text
Local Player Host composition
  -> supplies reusable Player Actor Runtime Host infrastructure

ActorProfile
  -> supplies Actor-specific PresentationPrefab

gameplay-owned composition
  -> remains gameplay-owned unless another accepted contract says otherwise
```

There is no longer one framework/runtime Host per selectable Actor merely because presentation differs.

### 4. Actor runtime materialization remains separate from Join

Canonical transaction order is:

```text
Join
→ Slot Joined
→ Actor selection
→ Activity Actor preparation when required
→ PlayerActorRuntimeHost materialization/adoption
→ selected Presentation materialization/adoption
→ preparation evidence
→ GameplayReady when remaining Activity requirements are satisfied
```

Therefore:

```text
Session Join
!= Actor Selection
!= Activity Actor Preparation
!= Physical Materialization
```

Manager-Provisioned Join may expose complete technical/session Host evidence while contextual Activity assignment is still absent. `AssignmentOrigin=None` is valid at that boundary.

### 5. ActorProfile is presentation authority

Current minimum responsibility:

```text
ActorProfile
├── ActorProfileId
├── DisplayName
├── Description
├── Icon
├── ActorKind
├── ActorRole
└── PresentationPrefab
```

`PresentationPrefab` is intentionally broader than a model. It may contain presentation-owned visuals, Animator, VFX anchors, audio emitters and presentation behaviours.

It must not become authority for Session state, Slot state, Player lifetime, admission, authoritative gameplay state or framework Player authority.

### 6. No ActorPresentationProfile is introduced

The accepted public authoring surface remains:

```text
ActorProfile.PresentationPrefab : GameObject
```

A separate presentation asset requires a future concrete reuse/configuration need.

### 7. Gameplay composition is distinct from Presentation

Locomotion, character controllers, gameplay input consumers and gameplay camera authoring are not Presentation merely because they are Actor-specific.

Likewise, sample/gameplay components do not become mandatory framework runtime infrastructure because they happen to live beside `PlayerActorDeclaration`.

### 8. Scene-Provided validates/adopts Runtime Host + Presentation

Current Scene-Provided authority is:

```text
Scene-Provided Local Player
├── exact Local Player Host
├── exact PlayerActorRuntimeHost / declaration evidence
└── selected ActorProfile
    └── matching Presentation under PresentationMount
```

The consumer scene may author the candidate composition. The Framework validates/adopts it deterministically and rejects mismatched or ambiguous evidence rather than silently repairing/replacing content.

The old `ActorProfile.LogicalActorHostPrefab` evidence model is removed.

### 9. Manager-Provisioned provisioning remains unchanged up to Actor preparation

```text
LocalPlayerProvisioningAuthoring
→ PlayerInputManager
→ Local Player Host
→ Slot admission
```

The ADR-023 change begins at Actor preparation:

```text
selected ActorProfile
→ reusable PlayerActorRuntimeHost
→ selected ActorProfile.PresentationPrefab
```

### 10. Session physical lifetime remains unchanged

IF-ADR-019 remains authoritative for Session-owned admitted physical Player lifetime. Ordinary Activity transitions do not implicitly recreate the Session Player occurrence.

IF-ADR-020 remains authoritative for explicit Leave/resource release.

IF-ADR-021 remains authoritative for Route Spatial Entry and optional Activity Explicit Relocation.

### 11. Readiness terminology remains semantic

`LogicalActorsPrepared` remains a valid current readiness/requirement term.

Its name describes the semantic prepared-Actor condition; it does **not** imply that the removed `LogicalActorHost` structural architecture is still current.

No cosmetic readiness rename is part of this ADR.

### 12. Scoped consumer access remains lifecycle-scoped

IF-ADR-015 remains authoritative for scoped Player consumer access.

Current rule:

```text
Route scope     = Route lifecycle ownership
Activity scope  = Activity lifecycle ownership
scene location  != scope authority
```

An Activity-scoped consumer may be discovered from Route content and bind while the Activity lifecycle scope is active.

## Removed structural API / evidence

The following are no longer current Player Actor composition authorities:

```text
ActorProfile.LogicalActorHostPrefab
logicalActorHostPrefab
LogicalActorHost
SceneLogicalPlayerActorEvidence
HasLogicalActor
```

No silent compatibility fallback reinterprets legacy serialized values as `PresentationPrefab`.

## Implemented provisioning chains

### Manager-Provisioned

```text
PlayerSessionProfile
→ Manager-Provisioned Join
→ Local Player Host under Session authority
→ Actor selection
→ Activity preparation requirement
→ PlayerActorRuntimeHost under ActorMount
→ ActorProfile.PresentationPrefab under PresentationMount
→ PlayerActorDeclaration/runtime evidence
→ contextual Activity evidence
```

### Scene-Provided

```text
Scene-authored Local Player Host
→ authored/adopted PlayerActorRuntimeHost
→ authored/adopted Presentation
→ exact Profile + Presentation evidence
→ validate/adopt deterministic composition
→ Session-owned admitted Player occurrence
```

## Rejected alternatives

- `PlayerSlotProfile` as Player Actor Runtime Host provider.
- `ActorProfile` continuing to provide the complete runtime Actor hierarchy.
- one framework Actor Host per visual character variant.
- pre-instantiating a prepared Actor shell merely because a Local Player Host exists.
- folding gameplay code into Presentation by default.
- promoting sample gameplay components to mandatory framework infrastructure.
- introducing `ActorPresentationProfile` without a concrete requirement.
- silent fallback from `PresentationPrefab` to legacy Logical Actor Host fields.
- maintaining two competing prefab authorities.

## Certification

Current technical evidence includes:

```text
[P0_PAUSE_INPUT_GATE_COMPOSITION]
status='Passed'
verdict='StaticContractComplete'
cases='8/8'

[QA_PLAYER_FULL]
status='Passed'
verdict='PLAYER QA CERTIFIED'
cases='14/14'
completed='access,join,observation,actor-default,actor-replace,actor-lifecycle,joining-control,second-player,commands,leave,rejoin,negatives,spatial,relocation'
```

The dedicated `actor-lifecycle` case requests the Relocate Activity and crosses the Actor selection → preparation/materialization boundary instead of treating Join as materialization.

The same QA run also reconciles the current Route/Activity scoped-access semantics. A subsequent exit-Play defect in destroyed-consumer teardown was corrected in the Framework consumer boundary and confirmed clean on rerun.

Historical Full Player `25/25`, current aggregate `27/27` and focused Player regressions remain dated evidence for the matrices they executed. They are not mechanically relabeled as the 14-case consolidated functional run.

## FIRSTGAME disposition

FG-ADR-002 Revision 4 records the current Player sample state:

```text
Getting Started / Minimal Game   Scene Player / PROVEN
Player Provisioning              Manager-Provisioned / PLAY MODE PROVEN
Character Selection              LeaveUnresolved / PLAY MODE PROVEN
Local Multiplayer                PLANNED / BLOCKED by public Slot/device/input contract
```

That sample evidence is consumer-owned and does not create a second runtime architecture.

## Final disposition

```text
Architecture decision             ACCEPTED
Runtime composition                IMPLEMENTED
ActorProfile Presentation authority IMPLEMENTED
Scene-Provided migration           IMPLEMENTED
Manager-Provisioned migration       IMPLEMENTED
Scoped access semantics             RECONCILED
Scoped access teardown              HARDENED
Manager functional Player QA        CERTIFIED 14/14
Pause/Input/Gate composition         CERTIFIED 8/8
FIRSTGAME Player Provisioning        PROVEN
FIRSTGAME Character Selection        PROVEN
Local Multiplayer device contract    FUTURE / separate scope
```
