# Immersive Framework — IF-PLAYER-SURFACE-01E2
## QA Public Contract Coverage Audit

> Historical QA audit. Any Profile or Capacity coverage in this record applies
> to the superseded model and is not certification of the accepted IF-ADR-016
> configuration model.

**Date:** 2026-08-07  
**Status:** A0 CLOSED  
**Type:** Technical audit / QA contract reachability  
**Repository audited:** `rinnocenti/QAFramework`  
**Frozen QA baseline:** `c99df1e77a8408e6b48124a5d371f09e9af52019` — `IF-TXN-03A`  
**Repository policy:** read-only; no Git changes performed.

---

# 0. 2026-08-09 post-audit certification reconciliation

This document remains the historical A0 audit of the 2026-08-07 baseline. Its gap classifications must not be read as the current shipped state. The subsequent Player Surface cuts closed the audited consumer-reachability gaps.

```text
P1 scoped consumer access                         SHIPPED
P2 immutable consumer observation                 SHIPPED
P3 PlayerProvisioningCommandTrigger               SHIPPED
P4 PlayerProvisioningStatusBinding                SHIPPED

QA-PLAYER-SURFACE-01                              PASS 29/29
QA-PLAYER-SURFACE-02                              PASS 36/36
PLAYER SURFACE QA CERTIFIED                       YES
```

The public certification now directly proves the important gaps identified by A0:

- public scoped cross-scene provisioning access;
- public scoped immutable observation;
- Open/Close Joining, Set Dynamic Capacity and RequestJoin;
- public default Actor selection through its separate public boundary;
- Slot/Host/Actor lifecycle observation;
- logical preparation, physical materialization and gameplay admission as downstream runtime evidence;
- Manager-Provisioned `WaitingForJoin` + `WaitCovered` pending-then-terminal behavior;
- Activity exit preserving Session-owned join/Host;
- reentry with newer occurrence and no duplicate Slot/Actor;
- invalid/no-change/capacity rejection semantics;
- missing/wrong/destroyed/stale scope behavior;
- stale Actor selection revision and repeated selection behavior.

Deep reservation mutation, assignment token mutation, internal reconcile, preparation/materialization commands and runtime-host module access remain internal QA concerns. They were **not** exposed merely to make Q1/Q2 public.

# 1. Objective

Determine which Manager-Provisioned Player behaviors are already certified through package surfaces available to a real consumer, which are only certified through QA-private/internal access, and which consumer-facing cases still lack direct certification.

This audit deliberately separates two questions:

```text
Does the framework runtime technically work?
```

from:

```text
Can a game prove/use the behavior through the official public consumer surface?
```

The first is already strongly covered by QA. The second is incomplete.

---

# 2. Classification model

| Classification | Meaning |
|---|---|
| **PUBLIC-ONLY** | Arrangement, action and assertion can be performed through public package contracts without internal modules, reflection or QA-only authority access. |
| **PARTIAL / PUBLIC EVIDENCE** | Real public command or public observation is exercised, but the scenario still depends on `InternalEditor`, privileged fixture setup, host resolution or internal runtime arrangement. |
| **INTERNAL TECHNICAL** | The behavior is strongly proven, but the test directly accesses internal contexts/modules, reflection, synthetic bridges, QA-only setup or authority operations unavailable to consumers. |
| **NOT DIRECTLY CERTIFIED** | No direct proof for the exact consumer scenario was identified in the audited baseline. This does not mean the underlying runtime behavior is absent. |

`InternalEditor` is not treated as a defect by itself. Internal QA is appropriate for authority invariants. It simply cannot be counted as proof that a real game can reach the same behavior through the official consumer API.

---

# 3. Principal evidence audited

| QA file | Role in current certification | A0 classification |
|---|---|---|
| `Player/Editor/QaManagerProvisionedLifecyclePublicContractRegression.cs` | Edit Mode contract semantics, immutability, normalization, unavailable state | **PUBLIC-ONLY** |
| `Player/Editor/QaP3FSessionSlotRuntimeSmoke.cs` | Session Slot state machine, capacity, reservations, invalid configurations | **INTERNAL TECHNICAL** |
| `Player/Assignment/InternalEditor/QaP3G3ProvisioningBridgeSyntheticSmoke.cs` | assignment token/owner/origin, Host binding/correlation, rollback and callback cases | **INTERNAL TECHNICAL** |
| `GameFlow/InternalEditor/QaManagerProvisionedLifecycleWaitingProjectionRegression.cs` | real `WaitingForJoin`/Released lifecycle projection exposed by public authoring | **PARTIAL / PUBLIC EVIDENCE** |
| `GameFlow/InternalEditor/QaM07ActivitySessionLifecycleProjectionRegression.cs` | real public Open/Close/Join plus Activity subset projection, revisions, occurrences and release | **PARTIAL / PUBLIC EVIDENCE** |
| `GameFlow/InternalEditor/QaPlayerActorSelectionRuntimeBindingRegression.cs` | public Join + public default Actor selection and binding behavior | **PARTIAL / PUBLIC EVIDENCE** |
| `GameFlow/InternalEditor/QaM07InternalReconcileRegression.cs` | occurrence-scoped reconcile, rollback, replacement, reentry, Actor renewal | **INTERNAL TECHNICAL** |
| `Player/Editor/QaPlayerGameplayAdmissionRegression.cs` + `Player/InternalEditor/QaPlayerGameplayAdmissionFixture.cs` | gameplay admission lifecycle and negative cases | **MIXED, predominately INTERNAL TECHNICAL** |
| `GameFlow/InternalEditor/QaParticipantAwareReadinessLoadingProgressRegression.cs` | participant-aware loading/readiness progress using `WaitCovered` | **INTERNAL TECHNICAL** |
| `Player/Editor/QaPlayerParticipationAuthoringRegression.cs` | Slot/Application/Activity authoring configuration and validators | **EDITOR/INTERNAL VALIDATION**, not runtime consumer proof |

---

# 4. Key findings

## 4.1 A genuine public-only lifecycle contract test already exists

`QaManagerProvisionedLifecyclePublicContractRegression` explicitly declares itself an Edit Mode **public-only regression** and does not simulate ActivityFlow.

It proves the public contract shape for:

```text
Unavailable lifecycle
gate-evidence normalization
pending Player readiness contribution
Slot collection immutability
Failed terminal contribution
Released terminal contribution
null Slot rejection
unbound LocalPlayerProvisioningAuthoring -> explicit Unavailable
```

This is valuable, but it proves **contract semantics**, not full runtime reachability.

Therefore:

```text
public lifecycle contract exists        YES
public-only runtime consumer journey    NO
```

---

# 5. Coverage matrix

| Capability / invariant | Consumer certification | Existing technical proof | A0 disposition |
|---|---|---|---|
| Lifecycle snapshot contract shape | **PUBLIC-ONLY** | Strong | Keep existing test |
| Explicit unavailable lifecycle | **PUBLIC-ONLY** | Strong | Keep existing test |
| Snapshot normalization / immutable Slot copy | **PUBLIC-ONLY** | Strong | Keep existing test |
| `WaitingForJoin` real runtime projection | **PARTIAL** | Strong | Q1 should reproduce through public consumer setup |
| Player readiness contribution held while waiting | **PARTIAL** | Strong | Q1 |
| Released lifecycle after Activity exit | **PARTIAL** | Strong | Q1 |
| Activity occurrence exposed publicly | **PARTIAL** | Strong | Q1 |
| Session revision exposed publicly | **PARTIAL** | Strong | Q1 |
| Requested/applied revision correlation | **PARTIAL** | Strong | Q1 |
| Activity subset projection vs Session roster | **PARTIAL** | Strong | Q1 |
| Repeated public snapshot reads are non-mutating | **PARTIAL** | Strong | Q1 may retain one idempotency assertion |
| Open Joining | **PARTIAL** | Strong internal + real public call | Q1 |
| Close Joining | **PARTIAL** | Strong internal + real public call | Q1 |
| Set Dynamic Capacity | **INTERNAL TECHNICAL** | Strong | Add public consumer case after P1 |
| Join rejected while closed | **INTERNAL TECHNICAL** | Strong | Q2/public negative |
| Capacity rejection | **INTERNAL TECHNICAL** | Strong | Q2/public negative |
| `RequestJoin` happy path | **PARTIAL** | Strong | Q1 |
| Join result typed evidence | **PARTIAL** | Strong | Q1 |
| Slot assignment/commit evidence | **PARTIAL at public result level** | Strong internal correlation proof | Q1 should assert only public-safe evidence |
| assignment owner/origin/token invariants | **INTERNAL TECHNICAL** | Strong | Remain internal QA |
| `HostBindingIdentity` correlation | **INTERNAL TECHNICAL** | Strong | Remain internal QA; expose read-only detail only if product/debug requires |
| stable technical Host after Join | **PARTIAL** | Strong | Q1 |
| duplicate/foreign reservation prevention | **INTERNAL TECHNICAL** | Strong | Remain authority QA; public symptom may be Q2 |
| late/reentrant provisioning callback handling | **INTERNAL TECHNICAL** | Strong synthetic proof | Remain internal QA |
| default Actor selection public request | **PARTIAL** | Strong | Q1 |
| selection revision advances | **PARTIAL** | Strong | Q1 |
| repeated same default Actor selection/no-change | **PARTIAL** | Strong | Q1/Q2 as appropriate |
| stale Actor selection revision | **NOT DIRECTLY CERTIFIED as public consumer case** | Runtime support inferred from revision-aware API; exact public negative not identified | Q2 |
| logical Actor preparation | **PARTIAL as resulting public lifecycle evidence** | Strong internal | Q1 observes; internal QA mutates/tests authority |
| physical Actor materialization | **PARTIAL as resulting runtime/public lifecycle evidence** | Strong internal | Q1 observes only |
| excluded Slot must not materialize | **PARTIAL** | Strong | Keep current regression; Q1 can assert public observable effect |
| gameplay admission | **INTERNAL/MIXED** | Strong | Q1 must reach it without QA calling internal admission helpers |
| Active reconcile | **INTERNAL TECHNICAL** | Strong | Must remain internal; do not expose reconcile just for Q1 |
| preparation rollback | **INTERNAL TECHNICAL** | Strong | Remain internal QA |
| occurrence-scoped reconcile | **INTERNAL TECHNICAL** | Strong | Remain internal QA |
| exit preserves Session-owned Host/join | **PARTIAL** | Strong | Q1 |
| Activity Actor removed on exit | **PARTIAL** | Strong | Q1 |
| later Activity occurrence switches projection | **PARTIAL** | Strong | Q1 |
| reentry occurrence advances | **INTERNAL/PARTIAL observation** | Strong | Q1 minimal reentry + internal deep proof |
| reentry renews Actor without duplication | **INTERNAL TECHNICAL** | Strong | Q1 observes no duplicate; deep renewal remains internal |
| invalid Slot/Application authoring | **EDITOR/INTERNAL VALIDATION** | Strong | Not a runtime consumer blocker |
| Player readiness / loading progress | **INTERNAL TECHNICAL** | Strong | Existing readiness QA remains valid |
| generic `WaitCovered` participant progress | **INTERNAL TECHNICAL** | Strong | Existing readiness QA remains valid |
| **Manager-Provisioned `WaitingForJoin` + `WaitCovered` end-to-end** | **NOT DIRECTLY CERTIFIED public-only** | Related pieces are separately proven | Add explicit Q1/Q2 scenario |
| public scoped cross-scene access to provisioning authority | **NOT CERTIFIED** | QA resolves host/authoring through privileged setup/helper | Blocker for canonical Q1 |
| public scoped observation from a scene consumer | **NOT CERTIFIED as consumer access path** | Public snapshots themselves are proven | Blocker for canonical Q1 |
| unavailable/wrong-scope consumer access | **NOT CERTIFIED for the future scoped endpoint** | Current unbound authoring Unavailable is public-only | P1 + Q2 |
| stale/disposed consumer scope | **NOT CERTIFIED** | Internal lifetime tests cover adjacent behavior | P1 + Q2 |

---

# 6. Important distinction: public observation exists and works

The audit changes one assumption in the closure plan.

`ManagerProvisionedPlayerLifecycleSnapshot` is not merely a synthetic public type. Real Play Mode regressions already observe through `LocalPlayerProvisioningAuthoring`:

```text
WaitingForJoin
ActivityOccurrence
SessionRevision
AppliedSessionRevision
HostCount
Activity-specific Slot projection
Ready
Released
Player readiness contribution / GateHeld
```

The current QA also proves that:

```text
Session may contain two joined Slots
while the Activity projects only one;

a later Session revision for an excluded Slot
does not expand that Activity projection;

Activity exit empties contextual Actor projection
while preserving Session-owned joins/Hosts;

a later Activity occurrence can project another Slot.
```

Therefore **P2 must not create a second lifecycle state model**.

P2 should be reframed as:

```text
complete/enrich the existing public observation where consumer/debug
needs typed Slot–Host–Actor correlation that the compact lifecycle
projection currently loses.
```

---

# 7. Important distinction: commands exist, access is the unresolved product boundary

Real QA already calls these public methods successfully:

```text
LocalPlayerProvisioningAuthoring.OpenJoining(...)
LocalPlayerProvisioningAuthoring.RequestJoin(...)
LocalPlayerProvisioningAuthoring.CloseJoining(...)

LocalPlayerActorSelectionRequestAuthoring
    .RequestDefaultActorSelection(...)
```

The problem is that the tests obtain the corresponding objects through QA-specific/runtime-host setup.

This means the missing contract is not:

```text
"invent Join"
```

It is primarily:

```text
scene / Route / Activity consumer
        ↓
typed scoped public access
        ↓
existing provisioning command endpoints
```

Dynamic capacity remains the clearest operation whose state-machine semantics are strongly covered internally but for which A0 did not identify a direct real public consumer proof.

---

# 8. WaitCovered finding

The QA baseline contains an explicit participant-aware loading regression using:

```text
ActivityEntryReadinessPolicy.WaitCovered
```

It proves readiness-progress/loading behavior with required and optional participants.

Separately, the Manager-Provisioned lifecycle regression proves:

```text
WaitingForJoin
Player readiness contribution = Preparing
gate contribution held
Activity exit -> Released
Session preserved
```

However, A0 did **not** identify one direct public-only regression combining:

```text
Manager-Provisioned Player
+ no joined Player
+ Activity requiring Player participation
+ WaitCovered
+ loading remains correctly pending
+ public Join
+ Player readiness completes
+ loading reaches terminal completion
+ gate releases
```

This combined scenario should be added explicitly. It is especially important because it validates the semantic boundary between Player readiness and the loading/readiness presentation pipeline, rather than merely proving each subsystem separately.

---

# 9. What QA should continue testing internally

The following capabilities should **not** be made public merely to convert existing QA to public-only:

```text
reservation mutation
TryMarkJoined
Actor preparation authority mutation
physical materialization commands
gameplay admission authority mutation
internal reconcile
rollback internals
runtime-host module lookup
assignment token mutation
Host registry mutation
late callback injection
revision coalescing internals
```

These are authority-level behaviors. Internal QA is the correct place to prove them.

The public consumer should request intent and observe outcomes.

---

# 10. Exact Q1 blocker map

## Blocker B1 — canonical scoped access

QA currently resolves the official host/provisioning authoring through QA-specific host-resolution/setup code.

Q1 needs the same path available to a game without:

```text
FrameworkRuntimeHost internals
scene scan
reflection
QA helper
serialized cross-scene authority reference
```

**Owner:** P1 — `IF-PLAYER-SURFACE-03`.

## Blocker B2 — public observation completeness

Current lifecycle observation is already useful and real, but the compact per-Slot surface does not expose all typed correlation needed for advanced diagnostics/certification.

Q1 only requires the minimum public evidence necessary to prove:

```text
Slot identity
joined state
selected Actor
logical preparation
physical materialization
gameplay admission
Activity occurrence
Session/applied revision
```

Rich assignment/Host/Actor correlation can stay Advanced/Debug.

**Owner:** P2 — `IF-PLAYER-SURFACE-04`.

## Blocker B3 — gameplay-ready progression without QA authority calls

A public-only happy-path regression must not call:

```text
PrepareSelectedActor
EnsureGameplayReady
internal reconcile
internal preparation module
internal gameplay module
```

The normal runtime lifecycle must progress from public intent to terminal public observation.

If it cannot, that is a package runtime integration gap and should be fixed in the smallest package cut; it is not justification for exposing those authority operations.

**Owner:** package runtime only if Q1 evidence proves a real gap.

## Blocker B4 — Player-specific WaitCovered regression

Generic WaitCovered readiness is covered, and Player WaitingForJoin is covered, but the combined public consumer path is not directly certified.

**Owner:** Q1/Q2 after P1.

---

# 11. Proposed Q1 contract

`QA-PLAYER-SURFACE-01` should be a new additional regression, not a rewrite of internal QA.

Allowed package surface:

```text
GameApplicationAsset
RouteAsset
ActivityAsset
PlayerSlotProfile
ActorProfile
canonical scoped Player consumer access
public Player commands
public immutable Player observation
public Activity/readiness/gate observation
```

Forbidden:

```text
System.Reflection for package internals
InternalsVisibleTo as consumer route
QaM07InternalReconcileSetup as operational dependency
direct RuntimeHost module lookup
direct prepare/materialize/admit/reconcile
external Slot mutation
log parsing as truth
FindObjectOfType / FindObjectsByType to discover framework authority
```

Minimum Q1 flow:

```text
start canonical runtime
→ enter Activity requiring a configured Player Slot
→ observe WaitingForJoin
→ confirm WaitCovered/loading remains non-terminal when applicable
→ Open Joining
→ Request Join
→ observe stable Slot + Host result
→ request/observe accepted default Actor selection where the product flow requires it
→ allow normal runtime lifecycle to prepare/materialize/admit
→ observe Player readiness Ready
→ observe gate/loading terminal release
→ exit Activity
→ observe contextual release
→ confirm Session-owned Host/join persists
→ reenter
→ confirm newer occurrence and no duplicate Slot/Actor
→ Close Joining
```

Q1 should assert public evidence only. Deep internal invariants remain covered by existing specialized tests.

---

# 12. Proposed Q2 public-negative coverage

After Q1 is green, public negative certification should include:

```text
joining closed
capacity exhausted
invalid capacity request
repeated/no-change operation
stale Actor selection revision
consumer authority unavailable
wrong scope
disposed/replaced scope
stale Activity occurrence
late result from old scope/occurrence
exit while WaitingForJoin
exit during Actor progression
reentry
repeated binding/subscription
```

Not every internal synthetic failure needs a duplicate public test. Q2 should cover failures whose externally observable semantics matter to a consumer.

---

# 13. Consequence for the closure plan

The A0 evidence refines P1/P2 as follows.

## P1 remains mandatory

**Scoped consumer access is the main missing public boundary.**

It should reuse existing runtime-host/registration infrastructure rather than introducing a registry or new Player authority.

## P2 becomes narrower

Do not build a new lifecycle snapshot from scratch.

Use the already-working public lifecycle projection and enrich/compose only the typed evidence required by:

```text
consumer status
Advanced/Debug
public QA certification
```

## P3/P4 remain product cuts

The current QA calls public methods directly in code. FIRSTGAME needs designer-facing command authoring and status presentation. Those are product surfaces, not prerequisites for proving the underlying runtime command contract, but should be implemented before final FIRSTGAME integration.

---

# 14. A0 acceptance result

| Acceptance question | Result |
|---|---|
| Did the audit identify a real public lifecycle contract test? | **YES** |
| Did it distinguish contract-shape proof from runtime consumer reachability? | **YES** |
| Are core Player authority invariants technically covered? | **YES, strongly** |
| Are public Open/Close/Join operations exercised in real runtime? | **YES, but in internally arranged QA scenarios** |
| Is public default Actor selection exercised? | **YES, but in an internally arranged QA scenario** |
| Is real lifecycle observation exposed through public authoring? | **YES** |
| Is cross-scene/scoped consumer discovery certified publicly? | **NO** |
| Is dynamic capacity certified through a real public consumer path? | **NO direct proof identified** |
| Is prepare/materialize/admit reachable without privileged QA operations in a canonical public-only test? | **NOT YET CERTIFIED** |
| Is Manager-Provisioned `WaitingForJoin + WaitCovered` certified end-to-end public-only? | **NO direct combined proof identified** |
| Is internal reconcile appropriately kept internal? | **YES** |
| Is Q1 still necessary? | **YES** |
| Does A0 justify a new Player authority/state store? | **NO** |

---

# 15. A0 verdict

```text
RUNTIME TECHNICAL CERTIFICATION
    STRONG

PUBLIC CONTRACT SHAPE
    STRONG for lifecycle snapshot semantics

PUBLIC COMMAND EXISTENCE
    STRONG

PUBLIC COMMAND RUNTIME EXERCISE
    PARTIAL

PUBLIC REAL-TIME LIFECYCLE OBSERVATION
    PARTIAL but substantially proven

PUBLIC SCOPED CONSUMER ACCESS
    MISSING / NOT CERTIFIED

PUBLIC-ONLY END-TO-END PLAYER JOURNEY
    MISSING

PUBLIC-ONLY MANAGER-PROVISIONED + WAITCOVERED JOURNEY
    MISSING
```

The QAFramework is therefore **not weak**. It is strong at proving framework authorities and regressions.

The gap is narrower:

> QA currently proves the framework from the inside and proves several public surfaces from an internally arranged environment, but it does not yet prove that an ordinary game consumer can traverse the complete Manager-Provisioned Player lifecycle exclusively through the official scoped public surface.

That is exactly the contract that P1/P2 and Q1 must close.

---

# 16. Next action

A0 is closed.

The next cut should be:

```text
P0 — IF-PLAYER-SURFACE-02
Canonical consumer boundary freeze
```

P0 should use this audit to freeze responsibilities before P1 code is written.

The first implementation cut remains:

```text
P1 — IF-PLAYER-SURFACE-03
Scoped Player provisioning consumer access
```

but P1 should now be designed with the knowledge that:

```text
Open/Close/Join already exist;
default Actor selection already exists;
real lifecycle observation already exists;

the missing product contract is chiefly scoped reachability,
plus targeted enrichment of observation rather than a replacement state model.
```
