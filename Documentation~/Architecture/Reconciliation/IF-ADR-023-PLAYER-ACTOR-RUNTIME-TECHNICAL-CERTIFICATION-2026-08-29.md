# IF-ADR-023 — Player Actor Runtime Technical Certification — 2026-08-29

Status: **TECHNICAL QA CERTIFIED**

> Post-certification reconciliation: [IF-ADR-023A — Player Actor Occurrence Identity Boundary — 2026-08-31](./IF-ADR-023A-PLAYER-ACTOR-OCCURRENCE-IDENTITY-BOUNDARY-2026-08-31.md) records the runtime occurrence-identity boundary correction and subsequent FIRSTGAME Scene-Provided `LogicalActorsPrepared` / `GameplayReady` Play Mode proof. This certification remains historical evidence; IF-ADR-023A is the current clarification for `PlayerActorDeclaration.ActorId` authoring/runtime semantics.

## Scope

This record certifies the package implementation of IF-ADR-023 at the Framework/QA boundary and reconciles the FIRSTGAME Player evidence available on 2026-08-29.

The later IF-ADR-023A reconciliation does not invalidate the composition certified here. It corrects a post-certification runtime ordering defect in occurrence identity establishment while preserving this Player Actor composition authority.

## Implemented composition

```text
LocalPlayerHostAuthoring
├── PlayerInput
└── ActorMount
    └── PlayerActorRuntimeHost
        ├── PlayerActorDeclaration
        └── PresentationMount
            └── ActorProfile.PresentationPrefab
```

Current ownership:

```text
Local Player Host composition
  owns reusable Player Actor Runtime Host infrastructure

ActorProfile
  owns Actor-specific PresentationPrefab

gameplay composition
  remains explicitly gameplay-owned
```

Removed structural authority:

```text
ActorProfile.LogicalActorHostPrefab
logicalActorHostPrefab
LogicalActorHost
SceneLogicalPlayerActorEvidence
HasLogicalActor
```

`LogicalActorsPrepared` remains a valid semantic lifecycle/readiness term.

## Transaction boundaries confirmed

```text
Session Join
!= Actor Selection
!= Activity Actor Preparation
!= Physical Materialization
```

Manager-Provisioned Join first establishes the technical/session Player occurrence. Immediate Join does not require contextual Activity assignment; `AssignmentOrigin=None` is valid before Activity reprojection/preparation.

### Player Actor occurrence identity clarification

The reusable authored `PlayerActorDeclaration` does not carry a persistent physical occurrence identity. Its authored template `ActorId` is empty; physical Player Actor preparation establishes the runtime occurrence identity before typed `ActorId` consumers may use it.

This boundary is formally reconciled by IF-ADR-023A. Ordinary persistent `ActorDeclaration` identity rules are unchanged.

## Manager-Provisioned chain

```text
PlayerSessionProfile
→ Manager-Provisioned Join
→ Local Player Host / PlayerInput
→ Slot Joined
→ Actor selection
→ Activity preparation requirement
→ PlayerActorRuntimeHost under ActorMount
→ ActorProfile.PresentationPrefab under PresentationMount
→ establish PlayerActorDeclaration runtime occurrence identity
→ PlayerActorDeclaration/runtime evidence
→ contextual Activity evidence
```

## Scene-Provided chain

```text
Scene-authored Local Player Host
→ authored/adopted PlayerActorRuntimeHost
→ authored/adopted Presentation
→ exact Profile + Presentation evidence
→ validate deterministic composition
→ establish runtime Player Actor occurrence identity during physical adoption
→ retain successful physical preparation/adoption evidence
→ Session-owned admitted Player occurrence
```

## Scoped consumer access reconciliation

Route and Activity are lifecycle scopes, not scene-location classifications.

```text
Route-scoped consumer
  binds to Route lifecycle access

Activity-scoped consumer
  binds to Activity lifecycle access

GameObject scene ownership
  does not redefine authored scope
```

## Teardown hardening

Exit Play Mode exposed this ordering:

```text
scene consumer destroyed
→ persistent runtime owner still retains managed wrapper
→ owner-side ReleaseScopedAccess
→ diagnostic read of destroyed Unity object
→ MissingReferenceException
```

Current rule:

```text
consumer OnDestroy
  releases its local scoped binding

later owner-side release
  treats Unity fake-null consumer as already released
  does not dereference destroyed Unity properties
```

This changes teardown robustness, not Session/access authority.

## Executed QA evidence

Manager-Provisioned setup:

```text
[QA_PLAYER_SETUP]
status='Applied'
fixture='ManagerProvisioned'
supportedSlots='2'
maxPlayers='2'
```

Pause/Input/Gate static composition:

```text
[P0_PAUSE_INPUT_GATE_COMPOSITION]
status='Passed'
verdict='StaticContractComplete'
cases='8/8'
```

Functional Player run:

```text
[QA_PLAYER_FULL]
status='Passed'
verdict='PLAYER QA CERTIFIED'
cases='14/14'
completed='access,join,observation,actor-default,actor-replace,actor-lifecycle,joining-control,second-player,commands,leave,rejoin,negatives,spatial,relocation'
```

The dedicated `actor-lifecycle` Activity transition reached `Active + Ready` with `blockingIssues=0` and proved Actor preparation/materialization under the composition certified on 2026-08-29.

Post-certification Scene-Provided evidence is recorded separately in IF-ADR-023A rather than being retroactively merged into this dated QA result.

## What 14/14 proves

```text
access
  Route + Activity scoped-access semantics

join
  P1 Manager-Provisioned technical/session Host evidence

observation
  scoped Session observation

actor-default
  default Actor selection

actor-replace
  explicit replace + restore before preparation

actor-lifecycle
  Activity-owned Actor preparation/materialization

joining-control
  exact JoiningClosed rejection + reopen

second-player
  P2 technical provisioning

commands
  eight explicit public command components

leave
  P2 Session Leave

rejoin
  P2 reprovisioning

negatives
  stale occurrence/revision rejection

spatial
  explicit Route spatial-entry authoring

relocation
  explicit Activity relocation authoring
```

## Coverage boundaries

P2 Join/Rejoin explicitly shares the Editor keyboard. This avoids depending on a second unpaired Input System device and proves technical provisioning only. It does not certify production Local Multiplayer Slot/device/InputUser/control-scheme policy.

`spatial` and `relocation` prove explicit authoring bindings. The lifecycle runtime path is exercised by `actor-lifecycle`; this 14-case suite does not add a separate direct world-coordinate assertion.

Historical Full Player `25/25`, aggregate `27/27` and focused regressions remain dated evidence for their own matrices.

## FIRSTGAME evidence available at certification time

FG-ADR-002 Revision 4 records:

```text
Getting Started / Minimal Game   Scene Player / PROVEN
Player Provisioning              Manager-Provisioned / PLAY MODE PROVEN
Character Selection              LeaveUnresolved / PLAY MODE PROVEN
Local Multiplayer                PLANNED / BLOCKED
```

The remaining Local Multiplayer blocker is the public Slot/device/input ownership and observation contract, not ADR-023 Actor composition.

Later Scene-Provided occurrence identity and readiness evidence belongs to IF-ADR-023A.

## Verdict

```text
IF-ADR-023 architecture                 ACCEPTED
Package runtime composition             IMPLEMENTED
ActorProfile Presentation authority     IMPLEMENTED
Manager-Provisioned migration           IMPLEMENTED
Scene-Provided migration                IMPLEMENTED
Scoped access semantics                 RECONCILED
Scoped access teardown                  HARDENED
Manager functional Player QA            CERTIFIED 14/14
Pause/Input/Gate composition            CERTIFIED 8/8
FIRSTGAME Player Provisioning            PROVEN
FIRSTGAME Character Selection            PROVEN
Production Local Multiplayer device model NOT CERTIFIED
```

Current occurrence-identity semantics and post-certification Scene-Provided readiness evidence: **see IF-ADR-023A**.
