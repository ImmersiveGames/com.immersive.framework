# Immersive Framework — Activity Readiness / WaitCovered Final Audit

**Status:** Consolidated final audit  
**Scope:** Activity Readiness, Player Participation, Manager-Provisioned Player, Late Join, Actor preparation/materialization, WaitCovered, Loading, capability gates, QA coverage and FIRSTGAME composition.  
**Inputs consolidated:**

```text
AUDIT-01-ACTIVITY-READINESS.md
AUDIT-02-PLAYER-LATE-JOIN.md
AUDIT-03-WAITCOVERED-LOADING-GATES.md
AUDIT-04-QA-COVERAGE.md
AUDIT-05-FIRSTGAME-INTEGRATION.md
AUDIT-06-ROOT-CAUSE.md
```

This report does not re-audit the repositories. It consolidates the six focused audits, resolves their conclusions and recommends one first cut.

---

# 1. Executive Summary

The observed problem is:

```text
Activity requires LogicalActorsPrepared
+ Entry Policy = WaitCovered
+ Player is not initially Joined/Prepared

→ Activity remains NotReady
→ Loading / cover remain active
→ reveal never happens
```

The consolidated evidence does **not** support the conclusion that Activity Readiness, Loading, WaitCovered or late-join reconciliation are fundamentally broken.

The current architecture behaves as follows:

```text
Explicit Player Slot exists
→ Player is not Joined
→ Player readiness contribution = Required / Preparing / WaitingForJoin
→ Activity aggregate = NotReady

WaitCovered observes the same Activity occurrence
→ retains Loading
→ retains Transition cover
→ retains capability gate
until Activity aggregate = Ready
```

This is correct behavior.

The Player path also works when a Join is actually emitted:

```text
RequestJoin
→ Slot Joined
→ Host/assignment evidence committed
→ Session revision changes
→ active Activity is reconciled
→ Actor is selected/prepared/materialized as required
→ Player readiness contribution completes
→ same Activity occurrence reaches Ready
```

The FIRSTGAME evidence already shows this progression with the currently observed `WaitVisible` configuration.

The root cause of the `WaitCovered` variant is therefore not a readiness deadlock inside the framework. It is a **control-plane composition problem** when the only human control capable of issuing `RequestJoin` belongs to the presentation that `WaitCovered` keeps covered until that same Join-dependent readiness completes.

Canonical problematic shape:

```text
LogicalActorsPrepared is Required
        ↓
Player is WaitingForJoin
        ↓
WaitCovered retains destination presentation
        ↓
Join control is inside that retained presentation
        ↓
user cannot emit RequestJoin
        ↓
no participation revision
        ↓
no reconcile delta
        ↓
Player remains WaitingForJoin
        ↓
Activity remains NotReady
        ↓
WaitCovered remains covered
        └──────────────────────────────↺
```

This is classified primarily as:

```text
K. incorrect FIRSTGAME composition
```

with the more precise description:

```text
presentation/control-plane dependency cycle
```

It is **not** currently classified as a defect in:

```text
Activity readiness aggregation
Player participation
Actor preparation/materialization
late-join reconciliation
WaitCovered runtime semantics
Loading progress authority
```

One important limitation remains: the local evidence does not directly expose the exact current Canvas/raycast hierarchy for the `WaitCovered` variant. Therefore:

```text
Confirmed:
  Join is required for this readiness path.
  WaitCovered retains presentation until Ready.
  public RequestJoin can operate while gameplay is gated.
  late Join can reconcile the active occurrence to Ready.
  the current FIRSTGAME Join emitter is associated with the Manager-Provisioned Player menu.

High-confidence hypothesis requiring direct product proof:
  the Join emitter becomes inaccessible when that menu is placed behind the WaitCovered cover.
```

The recommended first cut is therefore a **FIRSTGAME UX/product integration cut** that establishes an explicit control plane for Join during `WaitCovered`, without changing the readiness or Loading contracts.

---

# 2. Problema observado

The failure condition can be expressed precisely as:

```text
Activity Player Participation
  Projection = Explicit Slot

Player Slot
  exists
  not Joined initially

Requirement Level
  LogicalActorsPrepared

Activity Entry Readiness Policy
  WaitCovered
```

The Activity enters with a valid occurrence, but its Player readiness contribution cannot complete because the required Player has not joined.

Expected readiness state:

```text
ActivityReadinessStatus = NotReady

Required:
  pending > 0
  failed = 0

Reason:
  WaitingForJoin
```

`WaitCovered` then keeps the covered entry operation open.

The visible symptom is:

```text
Loading remains visible
cover remains active
gameplay remains gated
Activity does not reveal
```

That symptom can look like a Loading deadlock, but Loading is downstream of the unresolved readiness.

The key diagnostic question is:

```text
Can the operation that satisfies readiness — RequestJoin —
still be produced while WaitCovered is retaining the destination?
```

The final audit concludes that this is the critical product-composition boundary.

---

# 3. Estado atual do sistema

## 3.1 Activity Readiness

Activity readiness is occurrence-scoped.

Each occurrence is correlated by:

```text
Activity reference
+
transition sequence
```

The runtime captures its participant set and aggregates:

```text
technical baseline
+
authorable/runtime readiness participants
```

Required participants block `Ready`.

Optional participants remain diagnostic and do not block `Ready`.

## 3.2 Player contribution

Player participation is projected into Activity readiness through a package-owned Required participant when the Activity requires Player state.

For an Explicit Slot that exists but is not yet Joined:

```text
Player contribution = Preparing
Reason = WaitingForJoin
```

This is not equivalent to:

```text
zero participants
```

and is not equivalent to:

```text
failure
```

## 3.3 LogicalActorsPrepared

`LogicalActorsPrepared` is a Player Participation requirement level, not an Activity readiness status.

Its effective path is:

```text
Slot Joined
→ Actor selected
→ logical Actor prepared
→ physical Actor materialized
→ Player readiness contribution Completed
```

When that Required contribution completes, the Activity aggregate can become `Ready` if the technical baseline is also valid.

## 3.4 Late Join

The current runtime supports late completion for a Slot already included in the frozen Activity projection.

Canonical supported path:

```text
Activity active
→ Explicit Slot already projected
→ Slot not Joined
→ WaitingForJoin
→ RequestJoin
→ stable Session mutation
→ reconcile active Activity
→ Actor lifecycle progresses
→ same readiness occurrence updates
```

The frozen occurrence does not dynamically add unrelated Slots that were not part of its captured projection.

## 3.5 WaitCovered

`WaitCovered` is presentation/orchestration policy.

It does not decide readiness.

It:

```text
captures the target readiness occurrence
retains Loading / cover / capability gate
waits for the occurrence terminal
releases normal presentation only after Ready
```

There is no automatic timeout that turns `Preparing` into success.

## 3.6 Loading

Loading is a projection of operation/readiness progress.

It does not own:

```text
Activity authority
Player participation
readiness completion
Actor lifecycle
```

Successful terminal Loading completion must not precede aggregate `Ready`.

## 3.7 FIRSTGAME current evidence

The currently observed Manager-Provisioned Player flow uses `WaitVisible`, not a reproduced current `WaitCovered` path.

In that observed flow:

```text
Activity starts NotReady
→ OpenJoining succeeds
→ RequestJoin succeeds
→ later the same route operation completes
→ Manager Player Activity is Ready
```

Therefore the current consumer evidence proves that the Player late-join path can progress to Ready when Join is actually emitted.

---

# 4. Autoridades envolvidas

The audited architecture has a coherent ownership split.

| Concern | Authority |
|---|---|
| Activity occurrence and aggregate readiness | `ActivityFlowRuntime` |
| Slot allocation, Joined state, selection revisions | `PlayerParticipationRuntimeContext` |
| Activity-scoped Player/Actor lifecycle | `ActivityPlayerActorLifecycleParticipant` |
| Actor preparation/materialization | Player Actor preparation runtime/context and materialization adapter |
| Stable host-side reconciliation | Player reconciliation runtime/host module |
| Entry ordering / WaitCovered / reveal | `GameFlowRuntime` |
| Loading presentation | Loading reporter/surface |
| Transition cover | Transition surface/orchestration |
| Capability blockers | Gate policies/runtime |
| Join command source in FIRSTGAME | Manager-Provisioned Player command UI/channel/receiver |
| Game-specific UX composition | FIRSTGAME |

No evidence indicates a second FIRSTGAME authority for:

```text
readiness
Actor preparation
reconcile
Loading progress
gate release
```

The local command channel is transport/glue, not an authority replacement.

---

# 5. Fluxo causal atual

## 5.1 Entry

```text
Activity request
→ transition begins
→ target scenes/content materialize
→ Activity readiness occurrence N is created
→ Player projection is captured
```

## 5.2 Player not Joined

For an Explicit Slot:

```text
projected Slot exists
→ Slot not Joined
→ Player readiness contribution starts Preparing
→ reason = WaitingForJoin
```

## 5.3 Aggregate

```text
Required pending > 0
→ Activity aggregate = NotReady
```

No Required failure is necessary.

## 5.4 WaitCovered

```text
WaitCovered captures occurrence N
→ waits for N
→ Loading/cover/gate remain retained
```

## 5.5 Progress event

The event that can change the Player state is:

```text
RequestJoin
```

Successful Join creates stable Session evidence.

## 5.6 Reconcile

```text
Session revision changes
→ current Activity reconcile target is validated
→ missing Player lifecycle work is applied
→ Actor selected/prepared/materialized
→ Required contribution completes
```

## 5.7 Ready

```text
Player Required completed
+ technical baseline valid
→ Activity aggregate = Ready
```

## 5.8 Release

```text
WaitCovered sees Ready
→ Loading terminal success
→ Loading hides
→ Transition After / reveal
→ capability gate releases
→ request succeeds
```

The architecture therefore has a valid complete path.

The problem exists when no usable path produces the `RequestJoin` event.

---

# 6. Player já disponível

When the Player is already Joined before Activity entry, the readiness dependency can be satisfied during the normal Activity lifecycle.

Expected path:

```text
Player already Joined
→ Activity captures the Slot
→ valid Host evidence exists
→ default Actor selection if needed
→ logical preparation
→ physical materialization
→ Required Player readiness completes
→ Activity Ready
→ WaitCovered can reveal normally
```

The QA evidence proves the technical Player/Actor capabilities strongly.

The FIRSTGAME evidence also demonstrates lifecycle reentry while the Session Player remains present.

However, the consolidated evidence does not expose every final Actor-materialization snapshot for the current FIRSTGAME reentry.

Classification:

```text
Package technical path:
  PROVEN

FIRSTGAME exact end-to-end evidence:
  PARTIALLY PROVEN
```

This is not the principal risk in the reported bug.

---

# 7. Late Join

Late Join is the most important discriminator in the diagnosis.

## 7.1 What was historically missing

Older M07 evidence showed:

```text
RequestJoin
→ Slot Joined
→ Actor selection
→ no active-Activity progression
```

That historical state must not be used as the current diagnosis.

## 7.2 Current evidence

The current FIRSTGAME evidence shows:

```text
RequestJoin
status = SucceededJoined
logicalActorPrepared = False
```

followed later by:

```text
Manager Player Activity
activityReadiness = Ready

Route request
status = Succeeded
```

This means:

```text
RequestJoin transaction ends
before logical Actor preparation is complete

then package-owned reconciliation
progresses the active Activity
```

Therefore:

```text
logicalActorPrepared=False
```

inside the Join result is an intermediate boundary, not proof that the Activity is stuck.

## 7.3 Canonical limitation

Late Join is supported when the Slot is already part of the frozen projection.

An Activity occurrence does not dynamically expand its participant set just because a new, previously unprojected Player later appears.

For the reported scenario, the relevant shape is the valid one:

```text
Explicit Slot already projected
→ late Joined
```

## 7.4 Conclusion

```text
Late-join reconciliation is not the root cause
of the reported WaitCovered lock
when RequestJoin is actually emitted.
```

---

# 8. Player nunca entra

If the Player never joins, the correct state is not success.

Expected behavior:

```text
Player contribution remains Preparing / WaitingForJoin
→ Activity remains NotReady
→ WaitCovered remains pending
→ no successful Loading terminal
→ no reveal
```

There is no accepted timeout that should silently convert this state into `Ready`.

This state can remain indefinitely until an explicit operation changes the situation, for example:

```text
Join
Activity clear
Activity replacement
Route replacement
owned cancellation/dispose
```

This is architecturally correct.

The product problem appears when the user has no accessible means of producing the expected Join event.

QA has partial evidence for:

```text
WaitingForJoin
exit while waiting
release/cleanup
Session preservation
```

but lacks a closed public-only “never joins” case proving the full WaitCovered non-success behavior before explicit unwind.

---

# 9. Failure e occurrence replacement

## 9.1 Required failure

A Required participant failure is terminal blocking evidence.

Expected:

```text
Required Failed
→ Activity NotReady
→ terminal readiness failure
→ no successful Loading 100%
→ no normal reveal
→ recovery capability remains controlled
```

The generic QA coverage for this contract is strong.

## 9.2 Required release

Premature release of a Required participant is also terminal for readiness.

It must not be treated as successful completion.

## 9.3 Cancellation

Owned cancellation is typed and must unwind without fabricated success.

## 9.4 Replacement

Activity occurrences are isolated.

When occurrence `N` is replaced by `N+1`:

```text
N becomes stale/invalidated
→ late completion from N cannot make N+1 Ready
→ old Loading/readiness evidence cannot release the new operation
```

Generic QA coverage for replacement and stale occurrence rejection is strong.

## 9.5 Player reconcile replacement gap

The generic occurrence contract is proven more strongly than the Player-specific case:

```text
reconcile N in progress
→ replacement N+1
→ stale Player completion from N rejected
```

That specific Player negative remains a QA gap.

---

# 10. Package

## 10.1 What the package already gets right

The package currently separates:

```text
Activity authority
readiness authority
Player Session authority
Actor lifecycle
entry orchestration
presentation
capability gating
```

It also supports the essential sequence:

```text
WaitingForJoin
→ public RequestJoin
→ stable Session change
→ reconcile
→ LogicalActorsPrepared
→ Activity Ready
```

## 10.2 What should not be changed to solve this bug

Do not “fix” the reported symptom by:

```text
making Player readiness Optional silently
treating missing Join as NoParticipants
forcing Activity Ready
letting Loading complete while Activity remains NotReady
revealing WaitCovered before Ready
adding timeout-to-success
re-requesting the same Activity as reconcile
exposing raw Actor preparation APIs to FIRSTGAME
adding a manual Reconcile button
```

All of these would weaken or bypass valid contracts.

## 10.3 Package defect classification

No package defect is currently proven as the root cause.

A targeted QA can still reveal a package problem if:

```text
public RequestJoin is definitely emitted and accepted
while WaitCovered is active

but

the same Activity occurrence does not reconcile to Ready
```

The existing evidence does not show that failure.

---

# 11. QAFramework

## 11.1 Strongly proven areas

The QA evidence strongly covers:

```text
generic Activity Readiness
WaitVisible
WaitCovered
Required failure
Required release
Loading no-success-on-failure
cancellation
occurrence replacement
late old occurrence rejection
generic readiness isolation
technical Player Join
technical Actor preparation/materialization
technical gameplay admission/release
```

## 11.2 Partially proven areas

```text
late Join + internal reconcile
revision coalescing
one Actor per Slot
Player reentry
Player stale occurrence
positive multi-Required Loading progression
Optional denominator semantics
```

The M07 internal reconcile regression reached the important central assertions but the observed run did not finish as a fully green baseline.

## 11.3 Not proven

The principal missing vertical is:

```text
public-only
WaitCovered
+ Explicit Slot not Joined
+ LogicalActorsPrepared
+ public RequestJoin while entry gate is retained
+ automatic same-occurrence reconcile
+ Actor preparation/materialization
+ Activity Ready
+ Loading/reveal/gate release
```

This is the QA case most directly relevant to the reported bug.

## 11.4 Important distinction

QA already proves `WaitCovered` generically.

Therefore the next useful test is not another isolated WaitCovered smoke.

It must connect:

```text
Player control plane
+
WaitCovered
+
same-occurrence readiness progression
```

---

# 12. FIRSTGAME

## 12.1 Current Manager-Provisioned composition

The relevant FIRSTGAME model contains:

```text
Manager Player Route
Manager Player Activity
Manager-Provisioned Player menu
persistent provisioning authoring/receiver
Player Slot
Player Host prefab
persistent Transition/Loading surfaces
```

The Join command path is:

```text
ManagerProvisionedPlayerCommandEmitter
→ ManagerProvisionedPlayerCommandChannel
→ ManagerProvisionedPlayerCommandReceiver
→ LocalPlayerProvisioningAuthoring
```

## 12.2 No local framework workaround found

No evidence was found of FIRSTGAME using:

```text
reflection
manual RuntimeScopeContext
direct TryPrepareSelectedActor
direct reconcile
external Slot mutation
manual Actor spawn as repair
direct Loading progress updates
manual gate release
```

## 12.3 Current observed policy

The recent runtime evidence for the M07 flow shows:

```text
WaitVisible
```

This matters because `WaitVisible` naturally allows a Join UI that belongs to the destination to become available while gameplay remains gated.

## 12.4 Current observed success

The FIRSTGAME evidence shows:

```text
Activity NotReady
→ OpenJoining
→ RequestJoin
→ later Activity Ready
→ Route request success
```

Therefore the real consumer has already demonstrated the package-owned late-join progression.

## 12.5 WaitCovered variant

The reported `WaitCovered` problem represents a different composition.

If the Join emitter remains inside the destination presentation and that presentation is covered, the product has no human path to produce the event required for readiness.

The exact raycast/Canvas relationship of the current `WaitCovered` variant is not present in the available evidence.

Classification:

```text
Control-plane dependency cycle:
  high-confidence finding

Exact visual/raycast mechanism:
  hypothesis pending direct FIRSTGAME proof
```

---

# 13. Findings consolidados

## Confirmed findings

### IF-READY-AUD-001 — Activity readiness aggregation is behaving correctly

`LogicalActorsPrepared` contributes through Player readiness and a pending Required participant correctly prevents aggregate `Ready`.

**Status:** CONFIRMED

---

### IF-READY-AUD-002 — Explicit unjoined Slot is not the same as zero participants

An Explicit Slot already included in the Activity projection can legitimately remain:

```text
Preparing / WaitingForJoin
```

without becoming a failure.

**Status:** CONFIRMED

---

### IF-READY-AUD-003 — `LogicalActorsPrepared` is a Player requirement, not an Activity status

Activity readiness remains generic; Player Participation supplies the Required contribution.

**Status:** CONFIRMED

---

### IF-READY-AUD-004 — WaitCovered is not a readiness authority

`WaitCovered` observes the captured occurrence and retains presentation/capabilities until terminal readiness.

**Status:** CONFIRMED

---

### IF-READY-AUD-005 — Loading retention is a symptom of unresolved readiness

Loading does not prevent readiness from completing. It remains active because the captured Activity occurrence is not yet `Ready`.

**Status:** CONFIRMED

---

### IF-READY-AUD-006 — Public RequestJoin is not directly blocked by the Gameplay Gate

The audited package path does not gate `RequestJoin` through normal Player gameplay capability checks.

**Status:** CONFIRMED

---

### IF-READY-AUD-007 — Late Join can reconcile the active Activity occurrence

Current FIRSTGAME evidence shows a successful Join followed later by Activity `Ready` and request success.

**Status:** CONFIRMED

---

### IF-READY-AUD-008 — `logicalActorPrepared=False` in the Join result is not a terminal failure

The Join transaction can finish before Actor preparation; subsequent package-owned reconcile can satisfy the Activity requirement.

**Status:** CONFIRMED

---

### IF-READY-AUD-009 — The observed current M07 consumer flow uses WaitVisible

The recent FIRSTGAME evidence identifies `WaitVisible`, which explains why a destination-owned Join menu can be used while readiness is pending.

**Status:** CONFIRMED

---

### IF-READY-AUD-010 — Generic WaitCovered failure/replacement semantics have strong QA coverage

Required failure/release, cancellation, stale occurrence and replacement are substantially proven in QA.

**Status:** CONFIRMED

---

### IF-READY-AUD-011 — The public-only Player + WaitCovered vertical is not closed in QA

There is no fully proven consumer-equivalent regression for the exact combination implicated by the bug.

**Status:** CONFIRMED

---

### IF-READY-AUD-012 — FIRSTGAME does not currently appear to replace framework runtime authority

Its Manager-Provisioned command channel transports public commands but does not own readiness, Actor preparation, reconcile, Loading or gate state.

**Status:** CONFIRMED

---

### IF-READY-AUD-013 — Never-Join should remain Preparing, not become silent success

No automatic timeout-to-Ready is part of the accepted contract.

**Status:** CONFIRMED

---

### IF-READY-AUD-014 — Occurrence isolation is a strong part of the current design

Old readiness evidence is guarded by Activity reference, occurrence sequence and invalidation/release behavior.

**Status:** CONFIRMED

---

### IF-READY-AUD-015 — The root cause is upstream of reconcile when Join cannot be emitted

If no `RequestJoin` is produced, there is no participation revision to advance the Player readiness contribution.

**Status:** CONFIRMED as causal structure

---

### IF-READY-AUD-016 — A control operation required to achieve readiness must not depend on gameplay becoming Ready

Join is a control-plane operation for this flow, not normal gameplay input.

**Status:** CONFIRMED architectural conclusion

---

## Hypotheses requiring direct product proof

### IF-READY-AUD-017 — The current WaitCovered cover makes the existing Join emitter inaccessible

The available evidence strongly suggests this as the concrete FIRSTGAME failure mode because the emitter belongs to the Manager-Provisioned Player menu, but the exact current Canvas/raycast/sorting configuration was not available.

**Status:** HYPOTHESIS — HIGH CONFIDENCE

Required proof:

```text
WaitCovered active
→ inspect Join control visibility/interactivity
→ verify whether input/raycast reaches the emitter
```

---

### IF-READY-AUD-018 — Moving the same Join control to an always-available control plane will resolve the observed lock without runtime changes

This follows from the proven package path, but must be demonstrated in FIRSTGAME.

**Status:** HYPOTHESIS — HIGH CONFIDENCE

---

# 14. Root Cause

## Primary classification

```text
K. incorrect FIRSTGAME composition
```

More precise form:

```text
presentation/control-plane dependency cycle
```

## Causal statement

The bug is produced when all three are true:

```text
1. Activity Ready depends on a Player action:
   RequestJoin → LogicalActorsPrepared

2. Entry uses WaitCovered:
   destination presentation is retained until Activity Ready

3. The only human Join control belongs to that retained presentation
```

That creates:

```text
Join required for Ready
        ↓
Join control unavailable until reveal
        ↓
reveal requires Ready
        └──────────────↺
```

## Why this is not a readiness bug

Readiness is correctly saying:

```text
Required Player contribution is incomplete.
```

## Why this is not a WaitCovered bug

WaitCovered is correctly saying:

```text
Do not reveal the destination before the captured Activity is Ready.
```

## Why this is not a Loading bug

Loading is correctly saying:

```text
Do not report terminal success while the covered Activity is still Preparing.
```

## Why this is not currently a reconcile bug

When `RequestJoin` is actually emitted, current evidence shows progression to `Ready`.

---

# 15. Comportamento correto versus defeito

| Behavior | Classification |
|---|---|
| `LogicalActorsPrepared` blocks Ready while Player is not prepared | Correct |
| Explicit unjoined Slot becomes `Preparing / WaitingForJoin` | Correct |
| WaitCovered keeps cover while Activity is NotReady | Correct |
| Loading does not reach successful terminal before Ready | Correct |
| Gameplay input remains gated while Activity prepares | Correct |
| Public Join can be requested independently of Player gameplay | Correct |
| Join triggers later active-Activity reconcile | Correct in current evidence |
| Same occurrence can reach Ready after late Join | Correct in current evidence |
| Player never joins and Activity remains Preparing | Correct |
| Timeout silently converts waiting to Ready | Incorrect |
| Loading ignores Required Player readiness | Incorrect |
| WaitCovered reveals early only to expose Join UI | Incorrect as a semantic “fix” |
| FIRSTGAME requires Join but hides the only Join control behind WaitCovered | Defective composition |
| FIRSTGAME manually prepares Actor to escape the wait | Incorrect workaround |
| FIRSTGAME re-requests the same Activity to force reconcile | Incorrect workaround |
| A persistent/control-plane Join action remains usable during WaitCovered | Correct composition |
| Join happens before entering the covered Activity | Correct composition |
| Use WaitVisible because Join is intentionally part of the visible Activity experience | Correct composition |

---

# 16. Gaps de QA

The remaining QA gaps should be kept narrow.

## QA-GAP-01 — Public Join under WaitCovered

Need a public-only regression:

```text
WaitCovered Activity
Explicit Slot not Joined
LogicalActorsPrepared
→ confirm WaitingForJoin
→ confirm entry gate retained
→ call public RequestJoin
→ confirm stable Player commit
→ confirm automatic reconcile
→ confirm Actor preparation/materialization
→ confirm same occurrence Ready
→ confirm Loading terminal
→ confirm reveal
→ confirm gate release
```

This is the most important missing technical proof.

## QA-GAP-02 — Never Join

Need:

```text
WaitingForJoin
→ no Join
→ remains Preparing
→ no successful Loading terminal
→ no reveal
→ explicit owned clear/replacement
→ clean unwind
```

No timer is required.

## QA-GAP-03 — Player reconcile stale occurrence

Need:

```text
occurrence N waiting
→ Join/reconcile begins
→ N is replaced by N+1
→ stale N completion rejected
→ no Actor/readiness/gate contamination of N+1
```

## QA-GAP-04 — Positive Required/Optional Loading evidence

The positive participant-aware Loading runner should have a confirmed green execution for:

```text
multiple Required
Optional pending/failed
monotonic progress
100% only on Ready
```

## QA-GAP-05 — Internal M07 reconcile baseline

The existing internal reconcile regression should finish fully green before being treated as a closed regression family.

---

# 17. Riscos arquiteturais/produto

## Risk 1 — Fixing the presentation by weakening readiness

Severity:

```text
Critical
```

Examples:

```text
ignore missing Player
mark Player Optional silently
force Ready
Loading completes independently
timeout-to-success
```

This would hide invalid gameplay state and damage the framework contract.

## Risk 2 — Treating WaitCovered and WaitVisible as interchangeable

Severity:

```text
High
```

They communicate different product intent.

```text
WaitVisible
  preparation can be visible/interacted with

WaitCovered
  destination should remain hidden until prepared
```

A Join UX inside the destination naturally fits `WaitVisible`.

A covered flow needs an external control plane or pre-entry Join.

## Risk 3 — Conflating visual cover with capability gate

Severity:

```text
High
```

A command can be legally callable by the runtime while still being impossible for the user to emit because its UI is visually covered or not receiving raycasts.

Diagnostics and tests should distinguish:

```text
API admission
capability admission
visual visibility
UI interactivity
command emission
```

## Risk 4 — Reintroducing manual Actor/reconcile operations in FIRSTGAME

Severity:

```text
High
```

That would turn the consumer into a runtime repair layer and hide regressions in the package.

## Risk 5 — Treating historical M07 limitations as current truth

Severity:

```text
Medium-High
```

Older evidence described missing late-join reconcile. Current FIRSTGAME evidence shows progression after Join.

Future audits must continue to privilege current runtime evidence over historical plans.

## Risk 6 — Assuming the root-cause UI mechanism without proving it

Severity:

```text
Medium
```

The dependency cycle is strongly supported, but the exact current cover/raycast relationship was not present in the evidence.

The first product cut should make this mechanism explicit and observable instead of leaving it inferred.

## Risk 7 — Making Join control globally available without clear scope/lifetime

Severity:

```text
Medium
```

A persistent control plane must still be:

```text
explicitly scoped
bound/unbound deterministically
diagnostic
not a global service locator
not gameplay authority
```

---

# 18. Recommended First Cut

## Cut

```text
FIRSTGAME-M07-WAITCOVERED-CONTROL-PLANE-01
```

## Objetivo

Make the `WaitCovered + LogicalActorsPrepared + late Join` composition valid by ensuring that the Player Join operation remains explicitly accessible while the target Activity is covered.

The cut should prove that the existing package runtime can complete:

```text
WaitingForJoin
→ RequestJoin
→ automatic reconcile
→ LogicalActorsPrepared
→ Activity Ready
→ Loading/reveal
```

without changing Activity Readiness, Loading or Player reconciliation contracts.

## Tipo

```text
UX/product + real integration
```

This is intentionally a FIRSTGAME-first cut because the root cause is a consumer composition problem, not a proven missing runtime contract.

## Escopo

Use one Manager-Provisioned Player scenario configured intentionally as:

```text
Activity Entry Policy
  WaitCovered

Player Participation
  Explicit Slot

Requirement
  LogicalActorsPrepared
```

Provide a **control-plane Join surface** that remains usable while the Activity destination is covered.

The surface may reuse the existing:

```text
ManagerProvisionedPlayerCommandEmitter
ManagerProvisionedPlayerCommandChannel
ManagerProvisionedPlayerCommandReceiver
```

The key change is placement/lifetime/presentation:

```text
Join control is available before Activity Ready
and does not depend on target gameplay reveal.
```

Record evidence for:

```text
Activity occurrence
WaitingForJoin
cover held
gate held
Join command emitted
RequestJoin result
Session revision/Joined state
automatic reconcile
Actor prepared/materialized
same occurrence Ready
Loading terminal
cover release
gate release
request success
```

Also retain a second execution where Join is not issued long enough to confirm:

```text
WaitingForJoin remains Preparing
no fake Ready
no fake Loading terminal
```

then unwind through an explicit owned operation.

## Fora de escopo

```text
changing ActivityReadiness aggregation
changing Required/Optional semantics
changing LogicalActorsPrepared
changing WaitCovered semantics
changing Loading percentage rules
adding timeout
adding automatic Player creation
manual Prepare Actor
manual Reconcile
same-Activity repair request
Session Leave
M08 participation policy expansion
new singleton/service locator
new global input manager
package Composer/Recipe work
```

## Projeto responsável

Primary:

```text
planet-devourer / FIRSTGAME
```

QA follow-up:

```text
QAFramework
```

only after the product flow has been demonstrated and the exact required package contract is clear.

No package runtime change is recommended in this first cut unless the FIRSTGAME proof demonstrates that `RequestJoin` is emitted successfully but the current Activity still fails to reconcile.

## Arquivos provavelmente afetados

Exact current paths must be confirmed before implementation.

Likely FIRSTGAME areas:

```text
Demo 02 / Manager-Provisioned Player Activity asset
  configure the intended WaitCovered proof

Demo02_PersistentContent
or its existing persistent/control-plane UI prefab
  host the Join control outside the covered gameplay destination

Manager-Provisioned Player menu/UI composition
  remove the assumption that Join must come only from covered Route content

existing Manager-Provisioned command emitter binding
  reuse rather than create a second command path

Demo 02 README / model documentation
  explain WaitVisible vs WaitCovered Join composition
```

Expected existing scripts to remain transport only:

```text
ManagerProvisionedPlayerCommandEmitter.cs
ManagerProvisionedPlayerCommandChannel.cs
ManagerProvisionedPlayerCommandReceiver.cs
```

No change should be made to them unless current binding/lifetime prevents a persistent control-plane use.

## Superfície de produto afetada

```text
Manager-Provisioned Player Join UX
Activity entry policy demonstration
persistent/control-plane UI composition
Advanced/Debug evidence for waiting/reveal
```

The designer-facing lesson must become explicit:

```text
WaitVisible
  Join may live inside the visible Activity/Route experience.

WaitCovered
  any action required to achieve readiness must remain outside
  the covered gameplay presentation or occur before entry.
```

## Fluxo esperado

### Success path

```text
T0  Request Manager Player Activity

T1  WaitCovered applies cover
    gameplay capabilities gated

T2  Explicit Player contribution starts
    Preparing / WaitingForJoin

T3  Join control remains visible/interactive
    because it belongs to the control plane

T4  User emits RequestJoin

T5  Slot/Host/assignment commit succeeds

T6  Package observes stable Player revision

T7  Active Activity occurrence is reconciled

T8  Actor is selected/prepared/materialized

T9  Required Player contribution completes

T10 Activity aggregate becomes Ready

T11 Loading publishes terminal success

T12 Loading hides

T13 cover releases / Activity reveals

T14 gameplay gate releases

T15 original request returns success
```

### No-Join proof

```text
Activity enters WaitingForJoin
→ do not emit Join
→ remain Preparing
→ no successful terminal/reveal
→ explicitly clear/replace
→ clean unwind
```

## QA necessário

After the FIRSTGAME flow proves the intended product shape, add one public-only QA regression mirroring the same contract:

```text
WaitCovered
+ Explicit Slot
+ LogicalActorsPrepared
+ public RequestJoin while gate retained
+ automatic reconcile
+ same occurrence Ready
+ reveal/gate release
```

Forbidden QA shortcuts:

```text
reflection as the main path
manual RuntimeScopeContext
direct TryPrepareSelectedActor
direct reconcile
external Slot mutation
manual Actor spawn
log parsing as primary assertion
timer-based success
```

The QA should also include:

```text
never Join
stale occurrence replacement during Player progression
```

as follow-up cases within the same family, not as a parallel QA architecture.

## FIRSTGAME necessário

Required in this first cut.

FIRSTGAME must demonstrate:

```text
the exact WaitCovered composition
the Join control location
Join availability while cover is held
automatic progression after Join
no local framework workaround
clear visual distinction between control plane and gameplay
```

The normal user flow should not expose:

```text
Prepare Actor
Reconcile
Readiness Complete
Release Gate
```

as technical buttons.

## Critérios de aceite técnico

```text
Activity uses WaitCovered intentionally.
Player Slot is Explicit and initially not Joined.
LogicalActorsPrepared is Required.
Activity enters Preparing / WaitingForJoin without failure.
Cover and capability gate are retained.
Join command is emitted through the existing public provisioning surface.
No internal Player/Actor API is called by FIRSTGAME.
Join commit changes Player Session state.
Same Activity occurrence progresses automatically.
Exactly one Actor is prepared/materialized for the Slot.
Activity becomes Ready.
Loading does not report successful terminal before Ready.
Reveal occurs after Ready.
Gameplay gate releases after the accepted terminal ordering.
No Join path exists through a hidden manual repair.
No timeout/fallback converts waiting to success.
No duplicate Loading/Transition authority is introduced.
No singleton/service locator is introduced.
Clear/replacement while still waiting unwinds cleanly.
```

## Critérios de aceite de produto

```text
The user can understand why the Activity is waiting.
The user can still perform Join while gameplay is covered.
The Join control is visibly separate from gameplay capability.
The WaitCovered behavior is understandable without reading raw logs.
The flow does not require a technical Prepare/Reconcile button.
The same demo explains when WaitVisible is the better policy.
The designer can identify where the Join control lives and why.
The model can be reused without copying framework runtime logic.
```

## Ganho arquitetural

Clarifies the boundary:

```text
control plane
  operations required to establish runtime readiness

gameplay plane
  capabilities that must remain gated until Ready
```

This preserves:

```text
Activity readiness authority
Player runtime authority
WaitCovered semantics
Loading semantics
```

instead of weakening them to accommodate one consumer composition.

## Ganho de usabilidade

Makes `WaitCovered + Manager-Provisioned Player` authorable as a real product flow.

The user sees a coherent rule:

```text
If the Activity must stay hidden until the Player is prepared,
the action that creates/prepares that Player must be available
outside the hidden gameplay presentation.
```

It also makes the difference between `WaitVisible` and `WaitCovered` teachable rather than incidental.

## Commit message sugerida

```text
feat(firstgame): keep player join available during wait-covered readiness
```

---

# Final Conclusion

The six audits converge on one result:

```text
The framework is not stuck because Player is "a readiness" that Loading cannot finish.

The Activity is correctly waiting for a Required Player condition.
WaitCovered is correctly retaining presentation until that condition is satisfied.
The current late-join runtime can satisfy the condition after RequestJoin.

The failure appears when the consumer removes access to the operation
that must produce the readiness change.
```

The first correction should therefore be made at the **FIRSTGAME control-plane composition**, not by relaxing Activity Readiness or Loading.

Only if a public `RequestJoin` is demonstrably emitted and accepted under `WaitCovered`, but the same Activity occurrence still fails to become `Ready`, should the root cause be reopened as a package Player/reconciliation defect.
