# IF-ADR-048 — 1.1.0 Stable Readiness Master Plan

**Status:** Proposed — integrated review and approval pending

**Plan revision:** v1

**Authority:** IF-ADR-048 (Accepted), IF-ADR-047 (Accepted), IF-GOV-001

**Execution state:** No workpackage or IF-ADR-048 phase is closed by this plan

## Goal

Prepare one reviewed, dependency-ordered route from the accepted Stable
contract baseline to operational evidence and release certification for the
exact 1.1.0 candidate. Complete relevant Phase 2 investigations and required
architectural decisions before changing Runtime, tests, QA assets, consumers,
package metadata, versions or release configuration. Workpackages become
actionable only after the integrated approval gate below.

## Architecture and governing boundaries

- Accepted ADRs decide architecture; IF-GOV-001 governs maturity and
  compatibility; Public-API states the current public contract; IF-ADR-048
  governs readiness phases and exits. This plan does not replace them.
- IF-ADR-047 makes `1.1.0` the first official Stable baseline. Pre-1.1.0
  releases are historical and create no generic compatibility duty.
- Preserve accepted boundaries, including IF-ADR-036 readiness versus runtime
  availability, IF-ADR-044 bounded availability, IF-ADR-046 Join closure and
  its bounded evidence disposition. Reopen none without new concrete evidence.
- The accepted Stable inventory contains 107 Runtime types. Workpackage depth
  follows each guarantee; this plan neither promotes Experimental APIs nor
  expands Stable contracts by implication.
- Experimental-only gaps remain outside the Stable claim unless a concrete
  causal path to a Stable guarantee is demonstrated.
- Runtime modules remain the authority for their contracts. QAFramework
  supplies integrated evidence, not a parallel Runtime authority. Consumer
  validation must use the exact immutable candidate required by IF-ADR-048.

## Scope and non-goals

This plan consolidates the ten individually recorded workpackage analyses
into one proposed implementation and evidence route for IF-ADR-048 Phase 2 and
the later certification sequence. The cross-workpackage decisions below are
proposals for review, not accepted architecture or implementation authority.

It does not authorize implementation, Stable API expansion, Experimental API
promotion, generic migration from earlier versions, package/version changes,
consumer edits, workflow edits or publication. Prior tests and user-reported
results do not close a workpackage or phase.

## Execution model and gates

### Stage A — operational analysis and decision closure

The ten individual analyses are recorded. This stage reconciles their evidence
against the current source, resolves shared ownership and dependency questions,
and classifies findings as (1) Stable guarantee directly affected, (2) indirect
operational risk, (3) Experimental-only gap with no demonstrated Stable effect,
or (4) post-release improvement. Record evidence and route concrete questions
through the owning ADR process. The integrated proposals below remain pending.
Do not change code or tests during this stage.

### Gate A — explicit integrated-plan approval before implementation

No Runtime, test, QA asset, consumer, package metadata/version or workflow
change may begin until the complete route is reviewed and explicitly approved:

- relevant operational analyses are complete and tied to source revisions;
- every required architecture/contract decision has an Accepted owning ADR, or
  its authority records why no new decision is needed;
- each workpackage identifies module owner, contract boundary, dependencies,
  migration/consumer impact and implementation scope;
- each material Stable guarantee has named automated tests and integrated QA
  cases with setup, failure injection, expected state, cleanup and provenance;
- current evidence is distinguished from proposed tests, historical reports,
  unexecuted hypotheses and candidate-specific certification;
- ordering, shared fixtures, artifact identity and residual-risk owners are
  explicit; and
- architecture, API governance, QA and release owners approve the route as a
  whole. A decision affecting product risk or a Stable promise also needs its
  designated decision authority.

Plan approval is not implementation authorization. After Gate A, a separate
explicit task/release authorization is still required. The approved plan is
immutable; execution status/evidence live in the tracker. Material route
changes require a reviewed revision and renewed approval before dependent
work.

### Stage B — approved implementation and operational certification

Only after Gate A and separate execution authorization, implement the approved
cuts and tests in dependency order. Execute package tests and integrated QA on
the identified source revision. Preserve Stable semantics and accepted
Experimental boundaries; record failures and policy rejections accurately.

IF-ADR-048 Phase 2 exits only after Stable guarantees and material
failure/rollback/retry/release/cleanup paths execute on the identified source
revision, evidence is reviewed, and every directly affected Stable gap is
closed or explicitly risk-accepted by its responsible authority. Plan approval
does not satisfy this execution-evidence gate.

### Stage C — remaining IF-ADR-048 phases

After Phase 2 exits, follow IF-ADR-048: Phase 3 recertifies the reviewed source
candidate and completes the contract-to-test/consumer matrix; Phase 4 certifies
one immutable package installed in QAFramework, FirstGame and PEGA; Phase 5
records explicit GO/NO-GO for that exact artifact before publication. This
plan does not declare or authorize those phases.

## Workpackages

All ten workpackages have individual analyses recorded and remain **Open —
transversal review pending**. This is planning status, not a defect finding or
approval. Record evidence and decisions using the common format below; the
tracker is the only mutable status surface.

### OP-P0-PC — Persistent Content bootstrap and Session-scope release

- **Contracts / owner:** Stable Persistent Content authoring and Session
  lifetime; Framework Core / Scene Lifecycle and the host owning
  `ReleaseSessionScope`; governed by IF-ADR-045.
- **Risk:** Direct Stable impact if exact root ownership, release or source-scene
  restoration is violated. Full-host shutdown integration is an evidence gap,
  not a proven Runtime defect.
- **Evidence:** `SessionCompositionReleaseTests` covers release, failure
  aggregation and retry within a live owned scope; tracker records a
  user-reported 194 EditMode passes and QA-045 at 6/7. Neither proves full
  bootstrap followed by host destruction.
- **Known trace / distinction:** After successful bootstrap,
  `FrameworkRuntimeHost` owns the Session scope and `OnDestroy` attempts
  `ReleaseSessionScope`. A rejected release leaves the owner and participant
  list alive only until Unity completes destruction; there is no supported
  controlled shutdown/retry operation while retaining the host. A second
  callback (`OnDestroy` after `OnApplicationQuit`) may incidentally retry, but
  it is not a bounded shutdown protocol and cannot preserve authority after
  final destruction. Controlled in-process shutdown differs from definitive
  process termination: a live host can retain roots and retry, while no
  runtime can retry after the process ends. IF-ADR-045 requires release at
  host shutdown but does not define persistent controlled rejection or the
  terminal best-effort boundary. This does not justify a global recovery
  registry.
- **Gaps / decisions:** Certify complete bootstrap-to-shutdown integration;
  define disposition of release rejection during controlled destruction and
  the bounded guarantee at terminal process exit.
- **Dependencies:** Bootstrap, root transfer, Session participant ordering,
  source-scene restore, actual host destruction and QA-045 fixture.
- **Investigation:** Enumerate destruction/replacement/quit paths; trace owner
  and callbacks; inject participant success, recoverable/persistent rejection
  and mixed outcomes; prove retry authority for each path; verify repeated
  cleanup and root identity. Normal-context retry is not proof of post-
  `OnDestroy` retry.
- **Disposition:** Decide controlled-shutdown behavior before implementation.
  Prefer existing host ownership if evidence supports it; no registry absent a
  demonstrated need.
- **Exit:** Integrated test reaches full bootstrap and verifies exact
  Session/root release, participant order, source-scene result, rejected
  release state, retry while a valid owner survives, persistent rejection and
  repeated cleanup. Terminal process exit is specified separately; QA-045
  records result and cleanup.

#### OP-P0-PC analysis record — 2026-10-13 (proposal; approval pending)

This record is source analysis, not a Runtime change or certification. It
refines the earlier hypothesis that a live owner was already a usable retry
authority: the owner retains data after a failed release, but current callsites
do not provide a controlled retry path that keeps it alive.

##### 1. Bootstrap and shutdown evidence

- [`FrameworkRuntimeHost.TryCreate` / `Initialize` / `StartAsync`](../../../Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs)
  create the host as `DontDestroyOnLoad`, create its Session scope root and
  `SceneLifecycleRuntime`, load Persistent Content, call
  `ComposeSessionScope(this, PersistedRoots)`, then transfer roots to
  application lifetime. After that commit, startup still attaches Player
  preparation/Input/Camera/scoped-access modules and starts GameFlow; several
  of those steps can return `Started=false` while the Session scope remains
  composed.
- [`OnApplicationQuit`, `OnDestroy` and `ReleasePersistentContentScope`](../../../Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs)
  currently attempt Session release before the rest of host teardown. The
  helper returns `void`; on rejection it logs and leaves
  `_persistentContentScopeComposed` true, but the caller continues. `OnDestroy`
  then disposes GameFlow readiness, readiness binding, Camera membership and
  integrations/assignments/output materialization, restores captured
  TimeScale and Unity destroys the host. It does not wait for release success.
  `OnApplicationQuit` first attempts release, then disposes membership and
  Assignment; a later `OnDestroy` may therefore issue one immediate retry.
- [`ImmersiveFrameworkBootstrap.BootAfterSceneLoad`](../../../Runtime/Bootstrap/ImmersiveFrameworkBootstrap.cs)
  destroys the host after local Player Provisioning initialization fails,
  after `StartAsync` succeeded. If `StartAsync` returns failure, it only logs
  and returns; it does not stop the host. Its catch also only logs exceptions.
  [`TryCreateWithProjectFrameRate`](../../../Runtime/ApplicationLifecycle/FrameworkRuntimeHost.FrameRate.cs)
  destroys on Progression Save composition failure before `StartAsync` and
  before Persistent Content Session composition. `TryCreate`'s Player
  participation attach failure also destroys before Persistent Content is
  loaded. Those two pre-composition paths do not need Persistent Content
  release, but must remain distinct in the shutdown matrix.
- After Persistent Content Session composition but before bootstrap completes,
  a failed application-lifetime commit attempts Session release and lets
  `GlobalUiSceneRuntime` restore roots to the still-loaded source scene. If
  that release fails, the host records the scope as composed and returns
  failure. Failures after the successful ownership commit (Player module
  attach/integration or startup GameFlow failure) return failure without
  releasing the Session scope. The local-provisioning failure path is the
  exception: it calls `Destroy(host.gameObject)` and relies on `OnDestroy`.
- Session request triggers can be bound during Session composition before the
  host's `GameFlowStarted` state is set. `FrameworkRuntimeHost.RequestRouteAsync`
  and `RequestActivityAsync` reject when `_gameFlowRuntime` is null, but their
  observed readiness condition is not a host lifecycle phase; once GameFlow
  exists, the methods delegate without an explicit “startup committed / not
  stopping” gate. This is a fail-closed requirement to test, not proof that a
  particular gameplay request was accepted during a failing boot.

##### 2. Session participant and retry evidence

`FrameworkRuntimeHost.Initialize` registers these fixed participants in
`SceneLifecycleRuntime`: Pause product binding, Pause surface, Reset request,
Session Camera Assignment command, Route/Activity request, Route/Activity
CycleReset, Activity Restart and Scene Lifecycle Events. The only current
`MonoBehaviour` implementation discovered dynamically beneath composition
roots is `FrameworkBgmDirectorSceneLifecycleParticipant` (Audio). This is the
complete current `ISceneLifecycleParticipant` implementation inventory found
by source search.

The known release implementations are retry-safe for the already-released
case: Pause/Reset/GameFlow/Camera binders unbind only their exact host port and
accept an already-null binding; Pause surface removes a scope contribution
idempotently; Audio consumers accept an already-null director, and its runtime
retains its per-scope consumer ledger when a detach fails; Scene Lifecycle
Events is a no-op for Session scopes. This is source-level evidence for current
participants, not proof that all paths work in Unity. `ISceneLifecycleParticipant`
does not itself express a retry contract, and no integrated test exercises a
mixed successful release followed by retry.

There is one concrete bookkeeping defect in the current failed-composition
path: `ComposeSessionScope` appends discovered Session participants to
`_sessionParticipants` only after *all* `OnSceneAvailable` callbacks succeed.
`NotifyAvailable` attempts reverse rollback and includes rollback failures in
its diagnostic, but does not return the partially acquired participant set.
The host then calls `ReleaseSessionScope`; its snapshot contains the fixed
participants but omits dynamic participants from the failed composition. It
can therefore report cleanup success while a dynamic feature binding still
needs release/retry. This contradicts the intended local compensation and
retained Session-participant authority in IF-ADR-040. Relevant code:
[`SceneLifecycleRuntime.ComposeSessionScope` / `NotifyAvailable` / `ReleaseSessionScope`](../../../Runtime/SceneLifecycle/SceneLifecycleRuntime.cs),
[`ISceneLifecycleParticipant`](../../../Runtime/SceneLifecycle/ISceneLifecycleParticipant.cs),
and [`FrameworkBgmDirectorInjectionRuntime`](../../../Runtime/Audio/FrameworkBgmDirectorInjectionRuntime.cs).

The existing [`SessionCompositionReleaseTests`](../../../Tests/Editor/PersistentContent/SessionCompositionReleaseTests.cs)
proves aggregate rejection and retry with the same owner/root, but both fake
participants reject their first release. It does not prove that participant A
can succeed on attempt one, participant B can reject, and A can safely receive
the retry; it also does not test a discovered participant whose composition
rollback fails and is then retried by Session cleanup. Audio composition tests
prove successful detach/repeated release, not transient detach failure and
retry.

Evidence classification for this workpackage:

| Classification | Finding | Meaning for the decision |
|---|---|---|
| **Statically demonstrated defect** | A failed Session `Available` can have its dynamic participants omitted from the host's subsequent release ledger even when local rollback reports failure. | Correct the SceneLifecycle ledger under IF-ADR-040; package-level failure injection must confirm behavior. This is a source-proven failure path, not yet an executed reproduction. |
| **Integration risk** | Bootstrap can return or destroy after Session scope composition without a uniform host stop gate; GameFlow requests are checked for a null runtime but not a committed/stopping host phase. | Prove fail-closed behavior across late Player, Camera and GameFlow initialization failures; do not claim a request was actually accepted on the current path absent reproduction. |
| **Evidence gap** | No full-host test proves release after complete bootstrap, mixed participant success/rejection/retry, late post-composition failure cleanup, or shutdown ordering. QA-045 checkpoint 7 is explicitly package-level and remains `6/7 BLOCKED`. | Add package-level tests/fixture; do not reinterpret the QA verdict or treat source calls as integration certification. |
| **Terminal limit** | `OnDestroy` cannot veto its own destruction, and process termination leaves no live Framework owner for retry. | Define callbacks as best effort only; do not build a post-exit recovery guarantee or global registry. |

##### 3. Proposed operational contract and decision

Adopt the following proposal, subject to approval before implementation:

1. A controlled in-process stop is an explicit internal operation performed
   while `FrameworkRuntimeHost` and the exact Session owner still exist. It
   first marks the host non-operational for new gameplay/framework commands,
   while keeping the ports and modules alive long enough for Session
   participants to unbind.
2. Release the exact Session scope through the existing `SceneLifecycleRuntime`,
   in its deterministic participant order, while the exact Persistent Content
   roots remain alive and available to the participants. Do not destroy roots
   as a substitute for composition release. This proposal does not add a root
   destruction step or change their accepted application lifetime/ownership.
3. Only after aggregate release succeeds, dispose remaining host-owned
   subscriptions, Camera integrations/assignments/materialization, restore
   captured Pause TimeScale and permit `Destroy(host.gameObject)`. Repeated
   successful stop is a no-op.
4. On recoverable or persistent rejection, return a failed shutdown result,
   preserve the exact host, roots and participant retry ledger, keep gameplay
   commands closed, log participant/scope diagnostics, and do not destroy the
   host. The existing owner is the only retry authority; a caller may retry
   that same operation while it remains alive. Do not introduce a public Stop
   API, registry or second lifecycle owner. No automatic unbounded retry is
   proposed.
5. Every Session participant already attempted during a failed composition
   remains in the Session cleanup ledger until an aggregate release succeeds,
   including the participant that rejected `Available` and any participant
   whose rollback failed. A failed release retains the complete attempted set;
   retry re-invokes all participants, requiring same-scope release to tolerate
   already-detached participants. A completed aggregate release clears it.
6. Every failure or exception after Session composition but before the host is
   fully started enters the same non-operational controlled cleanup path. If
   cleanup rejects, retain an inert live owner for explicit retry; never leave a
   partially initialized host accepting gameplay, and never report cleanup
   success when a dynamic participant was omitted.
7. `OnApplicationQuit` and `OnDestroy` remain final best-effort safety paths,
   not controlled retry protocols. They attempt exact Session release while
   Unity still permits callbacks and report rejection. `OnDestroy` cannot
   veto its own destruction; no callback retry is promised after the process
   terminates. A second Unity callback may retry while the host is still live,
   but it is incidental only. Terminal process exit is outside post-exit
   recovery guarantees.

The proposal treats partial Session-composition retention and the absence of
pre-destroy result handling as Runtime work, not test-only gaps. It does not
presume any other teardown defect. Root scene restoration on failed
application-lifetime commit remains owned by `GlobalUiSceneRuntime` and should
be asserted, not redesigned.

##### 4. Rejected alternatives

- **Rely on `OnDestroy` as controlled shutdown:** rejected because the callback
  cannot prevent object destruction and the current `void` helper only logs;
  it loses the host's retry authority on rejection.
- **Destroy even after recoverable rejection:** rejected because subsequent
  cleanup cannot use the same Session owner and participants may retain
  bindings to persistent roots.
- **Add a static/global recovery registry or second host:** rejected under
  IF-ADR-001; the live `FrameworkRuntimeHost` and its Session ledger already
  own the retry data. Registry-based retry after process termination is
  impossible and would introduce unneeded authority.
- **Add a public shutdown API or make QA-045 checkpoint 7 pass:** rejected
  because no consumer-supported shutdown request/result exists and the
  release protocol is internal. Keep QA-045 terminal `6/7 BLOCKED`; add
  package-level integration evidence without changing its verdict.
- **Retry only participants that returned failure:** rejected as the first
  implementation because a callback may partially detach before rejecting;
  retry the entire retained exact participant set and require idempotence.
- **Destroy Persistent Content roots when release fails:** rejected because
  release and physical root lifetime are separate, and IF-ADR-045 does not
  authorize changing the accepted application-lifetime root behavior.

##### 5. Normative decisions, dependencies and likely files

Normative ownership must follow the concern, rather than placing all shutdown
rules in the Persistent Content decision:

| Decision | Normative owner proposed | Required treatment | Approval owner |
|---|---|---|---|
| Host startup/stop phases, admission closed while stopping or incompletely started, release-before-destroy, shutdown order, and terminal callback limits | **IF-ADR-001 — Core Lifecycle and Runtime Authority** | Minimum amendment to define controlled in-process shutdown and fail-closed host behavior while retaining one explicit host authority. Keep the operation internal; do not create a registry or public Stop API. | Framework Architecture decision authority; Framework Core owner confirms implementability. |
| Retaining every attempted Session participant across failed `Available`, failed local compensation, and failed aggregate release; retrying the full exact set until aggregate release succeeds | **IF-ADR-040 — Scene Composition Binding Model** | Minimum amendment to its Session participant ledger and retry/idempotence rules. This is the demonstrated composition bookkeeping defect. | Scene Composition / Framework Architecture decision authority; Framework Core owner. |
| Persistent Content's authored scene identity, root transfer, source-scene restoration, Session scope and application lifetime | **IF-ADR-045 and IF-ADR-008 remain unchanged** | Use these as constraints. IF-ADR-045 explicitly says it changes reference representation, not ownership/lifetime/composition; IF-ADR-008 governs authoring/source-scene composition, not host shutdown. No emendation is needed unless a proposal later changes exact root ownership, source restoration, or Persistent Content lifetime. | Reopen only with evidence of a change to their owned decisions; approval by their ADR owners. |
| New standalone shutdown authority or broader public lifecycle contract | **No new ADR proposed now** | IF-ADR-001 is the existing Core lifecycle owner. A separate ADR is warranted only if architectural review finds the proposed behavior falls outside its scope or seeks an independent/global authority; stop implementation until that decision is accepted. | Framework Architecture decision authority; API Governance also reviews only if a Stable public contract is affected. |

The five proposed directives are therefore allocated as follows:

1. **Retain evidence until confirmed release:** the exact attempted participant
   set, roots/scope identity, and failure diagnostics remain owned by
   `SceneLifecycleRuntime` under IF-ADR-040 until aggregate release succeeds.
2. **Controlled shutdown is conditional on release:** the host's internal stop
   operation under IF-ADR-001 closes new operational admission, then releases
   Session composition before disposing remaining dependencies or allowing host
   destruction. Do not change Persistent Content root lifetime.
3. **Failure leaves an inert, recoverable host:** while Unity is still running,
   the same live host retains its owner, roots, ledger, and dependencies needed
   for an explicit retry. It accepts no new gameplay/framework work.
4. **Retry is safe and idempotent:** retry the complete retained participant
   set against the same scope/roots; participants that already detached must
   accept same-authority repeated release. Clear ownership/evidence only after
   aggregate success.
5. **Terminal shutdown is best-effort:** `OnApplicationQuit` / `OnDestroy`
   attempt release and report aggregate failures, but cannot veto destruction
   or promise post-process retry. No persistent recovery registry is proposed.

Required diagnostics should identify operation (`Available`, rollback,
controlled stop, terminal release), exact Session scope/owner and root set,
participant identity, primary failure and each compensation/release failure,
host operational phase, and whether retry authority remains. A successful
aggregate result must mean every retained participant accepted release; a
failure must not erase the ledger or be logged as cleanup success. The host
owns startup/stop state and command admission; SceneLifecycle owns participant
ledger and aggregation; feature participants own only their bindings and
feature-local idempotence; GlobalUiSceneRuntime remains owner of root transfer
and source-scene restoration.

Cross-workpackage constraints (not new authorities):

- **Join / Provisioning (OP-P1-JOIN-AVAIL and OP-X-PROVISIONING):** failure
  after admission or provisioning must route to host-owned cleanup; Join,
  Session admission and provisioning must not create a competing Session
  release owner or recovery registry. Keep IF-ADR-046's bounded Join contract.
- **GameFlow:** host startup/stopping phase gates requests; GameFlow remains
  owner of Route/Activity transition truth and must not own host shutdown or
  retry. Reject work while host admission is closed.
- **Camera:** release Session Camera bindings while their authority and ports
  are alive; Camera owns its own binding idempotence, while the host controls
  teardown order. Do not add Camera-specific global recovery.
- **Audio:** BGM's feature participant keeps its consumer ledger and local
  idempotence; SceneLifecycle retains/retries the participant. Do not add a
  second Audio-to-host or global retry mechanism.

Candidate implementation workpackages (planning only; none are authorized):

| Candidate | Scope and likely files | Dependencies | Objective acceptance |
|---|---|---|---|
| **OP-P0-PC-R1 — Session composition ledger** | `Runtime/SceneLifecycle/SceneLifecycleRuntime.cs`; focused tests in `Tests/Editor/PersistentContent/SessionCompositionReleaseTests.cs`. | Accepted IF-ADR-040 amendment. | Every participant whose Session `Available` was attempted is retained before callback invocation; failed rollback/release preserves it; retry visits all in deterministic order; only aggregate success clears ledger; diagnostics preserve original and cleanup failures. |
| **OP-P0-PC-R2 — Host controlled stop and fail-closed bootstrap** | `Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs`, `Runtime/ApplicationLifecycle/FrameworkRuntimeHost.FrameRate.cs` if applicable, `Runtime/Bootstrap/ImmersiveFrameworkBootstrap.cs`; inspect `Runtime/GlobalUi/GlobalUiSceneRuntime.cs` but change it only if an asserted ownership/restoration failure is demonstrated. | Accepted IF-ADR-001 amendment and R1 ledger semantics. | Full bootstrap can stop through a result-bearing internal operation; every post-composition bootstrap failure reaches it; host rejects work before Started/during stop; failed release leaves exact host/roots inert and retryable; host is destroyable only after confirmed controlled release. Root transfer/restoration remains unchanged. |
| **OP-P0-PC-R3 — Participant and full-host certification** | `Tests/Editor/PersistentContent/SessionCompositionReleaseTests.cs`, `Tests/Editor/Audio/FrameworkBgmDirectorCompositionTests.cs`, and one focused Framework package-level host fixture under `Tests/Editor/PersistentContent/` (PlayMode only if EditMode cannot exercise full bootstrap). | R1 and R2; no new public API. | Mixed success/rejection/retry, failed `Available` plus failed rollback, persistent rejection then recovered retry, late post-composition bootstrap failure, repeated successful stop, terminal callback best-effort, roots/owner identity, no residual bindings, and complete fixture cleanup pass. QA-045 checkpoint 7 remains unchanged. |

Implementation owner is Framework Core for host/bootstrap and
SceneLifecycleRuntime; feature owners fix only a participant when its focused
test demonstrates non-idempotence. QA owner approves fixture provenance,
failure injection and cleanup. API Governance review is needed only if the
work unexpectedly changes a public Stable guarantee; no public surface change
is proposed here.

Dependencies: IF-ADR-001's single host authority; IF-ADR-040 participant
binding/compensation; IF-ADR-045 / IF-ADR-008 root and authored-scene
constraints; Bootstrap post-create error paths; package-level tests; and
QA-045's explicit package-level boundary. Do not modify QAFramework or change
QA-045's verdict as part of these candidate workpackages.

##### 6. Minimum package-level certification and acceptance

Use focused Framework EditMode/PlayMode package tests, not QA-045's public
terminal, with a real `FrameworkRuntimeHost`, actual Persistent Content roots,
one minimal startup Route/Activity, and a test-only Session participant for
failure injection. Prefer `TryCreateWithProjectFrameRate` + `StartAsync` and an
internal result-bearing controlled-stop entry point; do not use reflection or
add public APIs. If the complete host fixture cannot run in EditMode, use the
smallest package PlayMode test scene and keep the unit-level participant tests.

Required cases and objective verdicts:

| Case | PASS | FAIL |
|---|---|---|
| Full bootstrap then controlled stop | Startup returns Started; the exact Session owner/roots are released once in reverse order; same root identities remain alive through release; remaining host resources are disposed only after release; repeated stop is a no-op. | Owner/root mismatch, wrong order, root destruction as fake release, duplicate disposal, or release not invoked. |
| Mixed release then retry | Participant A succeeds; B rejects once; aggregate stop fails and retains owner/ledger; retry re-enters A safely and releases B; final aggregate succeeds and clears the ledger. | A cannot tolerate repeat release, any participant is skipped, host is destroyed, or result falsely succeeds. |
| Failed Session composition rollback | Earlier participant binds, a later participant rejects, rollback is injected to reject; host cleanup retries every attempted dynamic participant; diagnostics preserve both original and cleanup failures. | Attempted participant is absent from retry ledger, cleanup reports success while a binding remains, or diagnostics hide rollback failure. |
| Persistent rejection | Repeated controlled-stop attempt fails; host and exact roots remain alive and non-operational with retry ledger retained; fixture later removes injection, retries successfully, then destroys cleanly. | Any new gameplay command is accepted; host is destroyed on failed controlled release; fixture leaves roots/participants behind. |
| Failure after PC composition, before full bootstrap | Inject a deterministic late Player/Camera integration or startup failure; boot returns failure, requests are rejected, Session cleanup succeeds or leaves the host inert with retained retry authority; source/root outcome matches whether ownership commit happened. | Host remains operational, Session participants leak, status/result claims Started, or source roots are restored/destroyed incorrectly. |
| Terminal callbacks | Verify source path attempts release with exact owner and logs aggregate rejection; do not assert retry after process termination or claim terminal recovery. | Claim post-exit retry, or infer QA-045 checkpoint 7 PASS from this package test. |

Fixture cleanup must disable/recover injected rejection, complete Session release
using the retained live owner, assert zero retained Session participants and
all expected bindings detached, destroy the Framework host only after success,
then unload/destroy created scenes/assets and restore the Editor active scene.
The test report remains package-level; QA-045 remains `6/7 BLOCKED` until its
own public evidence boundary changes by an approved decision.

Residual risk: definitive process termination cannot guarantee participant
callbacks finish or later retry; the responsible authority is Unity/process
shutdown, not Framework recovery. A persistent release rejection during a
controlled stop intentionally leaves an inert host alive until an explicit
retry or terminal process exit. Architecture/API Governance approves the
contract amendments; Framework Core owns Runtime implementation; QA owner
accepts package fixture/cleanup evidence; release authority reviews the
source-revision evidence for Phase 2.

**Current disposition:** Analysis and decision proposal are recorded. The
proposed IF-ADR-001 and IF-ADR-040 amendments, host operational gate and
candidate workpackages require explicit architectural approval before
implementation. IF-ADR-008 and IF-ADR-045 are constraints, not current amendment
targets. No implementation or certification is claimed.
OP-P0-PC remains **Open — not implemented and not certified**. Phase 2 remains
open; do not change QA-045's `6/7` result.

### OP-P1-JOIN-AVAIL — Stable Join and occurrence-scoped availability

- **Status:** Open — joint analysis recorded below; no implementation or
  certification is claimed.
- **Contracts / owner:** IF-ADR-046's bounded Manager-Provisioned Join closure
  is Stable under the Join/Session Runtime owner. IF-ADR-044's block values,
  UnityEvent trigger and block/release behavior are Stable under Player Gameplay
  Runtime. `IPlayerSessionScopedAccess` and provisioning configuration remain
  Experimental; host modules, bridge, backend and join lease are Internal.
- **Stable Join behavior:** `PlayerSessionJoinTrigger.RequestJoin` validates
  request/device, then uses an internal port bound to one exact Route/Activity
  occurrence. The lease revalidates host/module, owner, occurrence and current
  Manager readiness before calling the canonical bridge and Host-preparation
  registration. Stable outcomes distinguish rejection, confirmed admission and
  unknown rollback; `FailedRollback` is never treated as success. The closure
  does not promise Actor readiness/gameplay admission, Session/Slot/Host
  snapshots or observable physical destruction. See [IF-ADR-046](../ADRs/IF-ADR-046-Stable-Manager-Provisioned-Player-Join-Boundary.md)
  and its accepted amendments.
- **Stable result closure:** the current codes are `Joined`,
  `RejectedInvalidRequest`, `RejectedBusy`, `RejectedUnavailable`,
  `RejectedConfiguration`, `RejectedJoiningClosed`,
  `RejectedNoAvailableSlot`, `RejectedDeviceAlreadyOwned`,
  `RejectedSessionChanged`, `FailedAdmission`, `FailedRollback` and
  `FailedUnknown`. Admission state is `Admitted`, `NotAdmitted` or `Unknown`;
  `Unknown` is not success. The Stable closure also includes the whole
  `LocalPlayerProvisioningConsumerScope` enum and its current values, the
  request members/device-reference semantics and trigger `Scope`, `LastOutcome`,
  `RequestJoin`, `Invoke` and `InvokeFromDevice`. The Interface's broader
  configuration, Actor and Session evidence is not included.
- **Stable Availability behavior:** one exact current joined Player occurrence
  owns independent tokens; first acquire and last release project through the
  existing physical input Gate adapter. Normal explicit release preserves the
  token when projection fails, allowing the same owner to retry. Leave clears
  occurrence blocks before terminal Session commit. Gate composition does not
  change `GameplayReady`, Join, Actor, device or Camera state. See
  [IF-ADR-044](../ADRs/IF-ADR-044-Consumer-Controlled-Player-Runtime-Gameplay-Availability.md)
  and IF-ADR-036/005 for Gate and physical-input boundaries.
- **Availability outcomes:** Stable status codes are `SucceededBlocked`,
  `SucceededReleased`, `RejectedRuntimeUnavailable`,
  `RejectedInvalidRequest`, `RejectedPlayerNotJoined`,
  `RejectedForeignOrStaleToken` and `FailedInputProjection`; only the two
  `Succeeded*` codes represent completed block operations. Failure/release
  truth must not be inferred from token validity alone.
- **Evidence classification:**

  | Class | Finding | Disposition |
  |---|---|---|
  | **Statically demonstrated Stable cleanup defect** | `PlayerSessionScopedAccessRuntimeHostModule.RefreshBindings` calls `ActiveScopedBinding.Dispose()` before `Recipient.Release()`. `ActiveScopedBinding.Dispose()` disposes `IPlayerSessionScopedAccess`; the subsequent `PlayerGameplayAvailabilityBlockTrigger.OnScopedAccessReleasing` calls `RequestReleaseRuntimeGameplay` through that disposed access, whose `TryGetContext` rejects before token release. The trigger then clears `_ownedToken` regardless of result. Even if call order is corrected, `TrySetConsumerAvailabilityBlocked(false)` failure preserves the runtime token while this callback currently discards the consumer copy. A scope teardown can therefore strand an occurrence block while the Player remains joined. | Treat the order as a deterministic source-level defect; verify the exact outcome in EditMode/Play Mode before declaring a fix certified. Release must run while the access is current, and a failed release must retain a retry authority that survives the disappearing consumer/scope or prevent that boundary from completing. Do not merely retain a token on an object that is about to be destroyed. |
  | **Statically demonstrated shutdown evidence loss** | `ClearConsumerAvailabilityProjections` ignores each projection result and clears its tracked slots/adapters; `ReleaseAllConsumerGameplayBlocksForShutdown` then clears the token ledger. The call is from `PlayerGameplayRuntimeHostModule.OnDestroy`, so the remaining failure is terminal today; it becomes directly relevant to a controlled recoverable stop only if shutdown treats this void cleanup as success. | Reconcile with OP-P0 before implementing its stop order. Preserve a result/owner while the host is recoverable; terminal callbacks remain best effort only if that boundary is explicitly accepted. |
  | **Integration risk — bootstrap/commands** | `FrameworkRuntimeHost.StartAsync` builds Session-scoped access before `ImmersiveFrameworkBootstrap` attaches the provisioning module, and bootstrap returns on failed Start or directly destroys the host when provisioning attach fails. Scoped `OpenJoining`/`CloseJoining` can fall back to `PlayerParticipationRuntimeContext` when the provisioning module is absent/not ready. The Join lease rejects a missing/not-ready Manager, but no host `Started`/`Stopping` phase is checked. | Prove exact event reachability; require a shared host operational gate before Join/Session/Actor mutations during incomplete start or controlled stop. Keep owned release/cleanup operations available as needed. Do not claim a Stable Join was accepted during failure without an execution trace. |
  | **Stable Join evidence boundary** | `IF-ADR-046` accepted bounded closure, composed physical compensation evidence and partial execution provenance. Bridge tests exercise logical rollback and exact compensation request; Unity backend destroys by the exact object but returns `void`; accepted closure does not certify physical absence after a failed destroy. | Preserve the accepted boundary and residual risk. Do not reopen the ADR or demand a different Stable contract absent new evidence/requirements; candidate-linked release certification is still needed for 1.1.0. |
  | **Evidence gap / conflicting provenance** | Package tests cover trigger projection/binding, Join outcome mapping and bridge rollback; QAFramework has current Join and IF-ADR-044 scenarios. IF-ADR-044 records a manual consumer observation that a held block releases during scoped teardown, but the current source order disposes access before invoking the release callback; no named package test injects physical release failure during teardown or proves the combined trigger/host retry path. No test exercises Session `OpenJoining` mutation during failed/incomplete bootstrap or a recoverable inert host. | Preserve the accepted historical report but revalidate teardown on a candidate linked to source SHA. Add the smallest package failure tests and reuse existing QA compositions; do not relabel old evidence or infer it tested failure/retry. Unity executions remain separate evidence. |
- **Public surface boundary:** `PlayerSessionJoinTrigger` is deliberately
  standalone and exposes a narrow Stable Join port. In contrast,
  `PlayerGameplayAvailabilityBlockTrigger` derives from the public
  `[Internal]` [`PlayerSessionScopedAccessConsumer`](../../../Runtime/PlayerParticipation/Authoring/PlayerSessionScopedAccessConsumer.cs),
  inheriting public `TryGetAccess` to the whole Experimental
  [`IPlayerSessionScopedAccess`](../../../Runtime/PlayerParticipation/Contracts/IPlayerSessionScopedAccess.cs).
  [`Public-API`](../../API/Public-API.md) does not promote that interface, but
  the callable inherited surface is visible from the Stable trigger. Treat
  this as a concrete API boundary review item: decide whether inherited access
  is intentionally unsupported/internal or whether the Stable authoring
  component should avoid exposing it. Do not promote `IPlayerSessionScopedAccess`
  by association.
- **Known consumer evidence:** Framework package tests include
  `PlayerSessionJoinTriggerTests`, `PlayerSessionJoinBindingTests`,
  `LocalPlayerJoinOutcomeProjectionTests` and
  `LocalPlayerProvisioningBridgeRollbackTests`. QAFramework contains Join
  scenarios under `Assets/QA-IF-ADR-PLAYER-SESSION` and usage in
  `Assets/QA-IF-ADR-044`; `planet-devourer` includes the imported
  `Assets/_Sample/PlayerSamples/LocalMultiplayer` scene/scripts. QAFramework
  resolves Framework through a local `file:` dependency; `planet-devourer`
  resolves registry `1.1.0-preview.8`. These are not equivalent immutable
  candidate provenance. IF-ADR-044 reports 8/8 and IF-ADR-046's accepted
  execution reports remain bounded by their documented provenance.
- **Source/test map:** Stable boundary and exact lease are in
  [`PlayerSessionJoinTrigger`](../../../Runtime/PlayerParticipation/Authoring/PlayerSessionJoinTrigger.cs),
  [`PlayerSessionJoinBindingLease`](../../../Runtime/PlayerParticipation/Runtime/PlayerSessionJoinBindingLease.cs)
  and [`PlayerSessionJoinPort`](../../../Runtime/PlayerParticipation/Runtime/PlayerSessionJoinPort.cs);
  canonical Manager/bridge mapping is in
  [`LocalPlayerProvisioningRuntimeHostModule`](../../../Runtime/PlayerParticipation/Runtime/LocalPlayerProvisioningRuntimeHostModule.cs)
  and [`LocalPlayerProvisioningBridge`](../../../Runtime/PlayerParticipation/Runtime/LocalPlayerProvisioningBridge.cs).
  Availability release and clear paths are in
  [`PlayerGameplayAvailabilityBlockTrigger`](../../../Runtime/PlayerParticipation/Authoring/PlayerGameplayAvailabilityBlockTrigger.cs),
  [`PlayerSessionScopedAccessRuntimeHostModule`](../../../Runtime/PlayerParticipation/Runtime/PlayerSessionScopedAccessRuntimeHostModule.cs),
  [`PlayerGameplayRuntimeHostModule.ConsumerAvailability`](../../../Runtime/PlayerParticipation/Runtime/PlayerGameplayRuntimeHostModule.ConsumerAvailability.cs)
  and [`PlayerGameplayInputBindingRuntimeContext`](../../../Runtime/PlayerParticipation/Runtime/PlayerGameplayInputBindingRuntimeContext.cs).
  Deterministic coverage currently lives in
  [`PlayerSessionJoinTriggerTests`](../../../Tests/Editor/PlayerParticipation/PlayerSessionJoinTriggerTests.cs),
  [`PlayerSessionJoinBindingTests`](../../../Tests/Editor/PlayerParticipation/PlayerSessionJoinBindingTests.cs),
  [`LocalPlayerJoinOutcomeProjectionTests`](../../../Tests/Editor/PlayerParticipation/LocalPlayerJoinOutcomeProjectionTests.cs)
  and [`LocalPlayerProvisioningBridgeRollbackTests`](../../../Tests/Editor/PlayerParticipation/LocalPlayerProvisioningBridgeRollbackTests.cs).
- **Normative decision proposal:** no change to IF-ADR-046's outcome shape or
  admitted scope is proposed. Amend IF-ADR-001 only for the host operational
  gate that OP-P0 needs; amend IF-ADR-044 to define token/release behavior when
  scope teardown or controlled shutdown cannot confirm physical projection.
  IF-ADR-005 owns physical transaction/recovery guarantees and needs amendment
  only if the chosen rule changes them. IF-ADR-020/019 remain authoritative
  for exact Leave and Session physical lifetime; amend only if controlled host
  stop is intended to terminate/recreate a Session Player occurrence.
- **Dependencies:** OP-P0 controlled stop/retained retry authority; OP-X-
  PROVISIONING's Manager readiness and physical ownership; OP-P1-LEAVE-ACTOR
  occurrence semantics; Camera membership release; Pause/Input physical gate
  transaction and IF-ADR-036's readiness/availability distinction; GameFlow's
  Route/Activity scopes and transition Gate. Do not add a parallel Join,
  availability, Session or input writer.
- **Candidate implementation/certification sequence:** (1) obtain transverse
  approval for IF-ADR-001/044 scope and the OP-P0 command-gate allowlist; (2)
  test the exact availability release-failure path and choose a retained owner
  that survives scope teardown; (3) add deterministic tests for multi-token,
  foreign/stale token, last-token physical rejection, retry, Leave and cleanup;
  (4) add a host-phase test proving Join/OpenJoining/Actor mutation reject while
  not operational, while only approved cleanup releases remain possible; (5)
  rerun the existing Join/Leave and IF-ADR-044 QA on one immutable candidate
  and consumer package; do not duplicate their successful-path scenarios.
- **Required positive, negative, rollback/retry and cleanup evidence:** Join
  success commits one exact Session occurrence and one Host; closed Joining,
  invalid request/device, stale owner, unavailable Manager and occupied Slot
  map to their exact Stable outcomes without side effects; callback/commit
  rollback preserves truthful `NotAdmitted`/`Unknown` certainty and creates no
  duplicate on retry. Availability acquire/release composes independent
  blockers, preserves unrelated blockers, rejects foreign/stale tokens,
  keeps the token when physical release fails, and proves eventual retry or
  exact Leave cleanup. Scope teardown and host controlled-stop failure must
  preserve/recover ownership without claiming success prematurely. Cleanup
  asserts exact occurrence, logical token count, physical Action Map/gate
  posture, no duplicate Host/Slot, and baseline restoration. Do not assert
  post-process retry or physical destruction guarantees outside IF-ADR-046.
- **Exit criteria:** every Stable result is truthful against Session/Host
  commit; each token belongs to and releases only its exact occurrence; failed
  physical projection retains a live retry authority until successful cleanup;
  host phase prevents new mutations during incomplete start/controlled stop;
  allowed combinations preserve IF-ADR-005/036/044; exact package/candidate
  and consumer provenance is recorded. No Stable scope is expanded by these
  tests.

### OP-P1-LEAVE-ACTOR — Leave, Actor Selection and lifecycle ownership

- **Record status:** Analysis recorded against Framework source `617547a337e52022999c71c30f2e493a3dacbd26` (2026-10-13). This is a proposed workpackage record for transversal review; it does not close the workpackage, authorize implementation, or change any Accepted ADR.
- **Contracts / maturity / owner:** `PlayerSessionLeaveCommandTrigger`, remaining Session command triggers, `IPlayerSessionScopedAccess`, Actor Selection requests/results/policy, Actor preparation and Activity Player/Actor lifecycle are Experimental in Public-API/GOV-001. `PlayerSessionObserver` and the public selection/Leave controls are request/observation surfaces, not physical Actor or Leave authority. IF-ADR-020 owns Leave's exact-occurrence staged release; IF-ADR-003 owns Session Player/Actor selection policy; IF-ADR-021 owns Activity initial placement/relocation semantics and explicitly excludes Session selection, Leave and provisioning. Stable IF-ADR-046 Join and Stable IF-ADR-044 Gameplay Availability remain bounded contracts; neither promotes the broader scoped-access surface.
- **Stable risk classification:** Experimental-only by default. An indirect Stable risk exists only if a concrete path proves that Leave, selection or their cleanup falsifies IF-ADR-046 Join/Host commit or IF-ADR-044 block/release guarantees. No such falsification is established by this static trace. Do not reopen IF-ADR-046 or promote Leave/Actor Selection as part of this workpackage.
- **Evidence provenance:** Static source review only. `ActivityPlayerActorLeaveLedgerTests` covers contextual ledger retirement/filtering, not end-to-end Leave. IF-ADR-020 records historical focused ADR020-H `26/26` and 2026-08-15 Full Player QA evidence for its stated paths; these reports are not current-revision certification. No dedicated current Leave/Actor Selection runtime test matrix was found in `Tests/Editor/PlayerParticipation`. No Unity execution was performed.

#### Current source trace and owners

Public command path:

```text
PlayerSessionLeaveCommandTrigger.Invoke
  -> resolve explicit PlayerSlotProfile / exact scoped observation
  -> choose expected Slot revision (or preserve LastLeaveRequest revision while Leaving)
  -> IPlayerSessionScopedAccess.RequestLeave
  -> PlayerSessionScopedAccessRuntimeHostModule
  -> LocalPlayerProvisioningRuntimeHostModule.TryLeave
  -> SessionPlayerLeaveRuntimeHostModule.TryLeave
```

`SessionPlayerLeaveRuntimeHostModule` is the host-scoped orchestration owner; the underlying domains keep their own authority. The exact order is:

1. Validate the request and current Session snapshot; require the configured Slot to be Joined (or the same known Leaving occurrence) and require its current revision to match. Resolve the explicit provisioning mode and ready adapter.
2. `PlayerParticipationRuntimeContext.TryBeginSessionPlayerLeave` changes Joined to Leaving, increments Slot/context revisions, clears reservation state and creates a `SessionPlayerLeaveToken` containing Session context ID, sequence, Slot ID, expected occurrence revision and staged Slot revision. Slot is not yet Available.
3. `ActivityPlayerActorLifecycleParticipant` captures progress keyed by the Leave token, removes the Slot from Activity/readiness projections, and retires exact Gameplay Admission, Input binding, occupancy and contextual projection. It intentionally does not destroy the Session physical Actor. Missing Activity representation is a valid path when no current Activity evidence remains.
4. For Scene-Provided mode, capture the exact Session physical Host, retire Scene Local Player context, then later release Scene admission/Host/assignment/context authority. For both modes, release the prepared Session physical Actor using its preparation token/evidence.
5. For Manager-Provisioned mode, release Host evidence while retaining its receipt, pass that receipt to the provisioning bridge, release Host admission, then call the exact admitted-player backend release for `PlayerInput`. Scene-Provided mode uses its corresponding admission authority and exact retained Host.
6. Terminal Session cleanup verifies the same Leave token and successful Activity/provisioning receipts, clears the Session Actor selection, confirms no residual assignment, and commits the exact Slot to Available while clearing the Player occurrence. Only then does the public result report terminal Leave success.

| State or receipt | Current owner | Retention / release boundary |
|---|---|---|
| Joined/Leaving Slot, Actor selection and active Leave token | `PlayerParticipationRuntimeContext` | Active token retained in `_activeSessionPlayerLeaves` through retries; removed only at terminal Slot commit. |
| Orchestration results, provisioning mode, exact Scene Host reference, release receipts | `SessionPlayerLeaveRuntimeHostModule` | `_latestBySlot` stores the current record before irreversible stages and retains it through failure; a successful record supports duplicate confirmation. It is host-component scoped. |
| Activity/readiness/context release progress | `ActivityPlayerActorLifecycleParticipant` | Dictionary keyed by exact Leave token retains each stage and receipts through retries and after success. |
| Gameplay Admission/Input/occupancy tokens and stage flags | `PlayerGameplayRuntimeHostModule` | Dictionary keyed by exact Leave token preserves partial progress; release retries do not recreate already released stages. |
| Terminal assignment/selection/commit progress and completion result | `PlayerParticipationRuntimeContext` terminal partial | Keyed by Leave token; verifies exact provisioning mode and commit evidence and retains completed result. |
| Manager physical Host evidence, admitted `PlayerInput`, completed release receipt | Preparation module + `LocalPlayerProvisioningBridge` | Preparation release result retains previous Host evidence/reference; bridge removes admitted-player tracking only after backend call returns without throwing and records completed receipt by token. |
| Scene-Provided Host/admission/assignment/context receipts | `SceneLocalPlayerAdmissionRuntimeHostModule` | Its release record is correlated by Leave token and tracks each completed stage. |

#### Identity, occurrence, revision and retry invariants

- `PlayerSlotId` identifies the configured Slot; it is not a Player occurrence, Actor identity or Leave token.
- The current Player occurrence is additionally guarded by the Session context and occurrence/revision. A Leave token is unique to one staged Leave. Reusing a Slot after terminal Leave creates a different occurrence; an old request must be rejected as stale and cannot remove the new Player.
- `PlayerActorSelectionRequest.ExpectedSelectionRevision` is independent from the Leave token and preparation token. Successful selection mutation increments both `SelectionRevision` and Slot revision; a stale expected selection revision rejects without mutation.
- `PlayerActorPreparationToken` correlates Actor preparation/materialization to Session context, Slot/Player occurrence, selected Actor profile and selection revision. Activity lifecycle preparation is a separate contextual representation/token. Selecting an Actor does not itself materialize it, admit gameplay, or join an Activity.
- Same-token retry reuses retained stage receipts. Activity, gameplay and terminal progress are monotonic and do not compensate by recreating released resources. A later different occurrence may replace the orchestration record indexed by Slot, but old domain receipts are token-keyed and old requests remain stale.
- Duplicate Leave while the Slot is Leaving reuses active Session Leave correlation only when the expected occurrence matches. Duplicate successful Leave is confirmed from the completed orchestration/terminal receipt. The command trigger retains its last valid occurrence revision for retry while Leaving. After Slot reuse, that revision/token cannot target the replacement occurrence.
- Partial success is intentionally truthful: Activity/context or physical Actor may already be released when a later Host/backend/terminal step rejects. The Slot remains Leaving and the same host-scoped token record owns retry; no generic rollback or Actor recreation is promised.

#### Actor Selection operations

`PlayerParticipationRuntimeContext.ApplyActorSelection` owns the logical selection state. `Select` rejects a different profile when one is already selected and directs callers to `Replace`; selecting the same profile is successful with no mutation. `Replace` requires an existing selection; replacing with the same profile is successful with no revision change. `Clear` requires a null profile, succeeds idempotently when already clear, and otherwise increments selection/Slot/context revisions. `SelectDefault` resolves the configured Slot default only when the immutable Session policy allows default resolution. `AllowDuplicates` accepts repeated `ActorProfileId`; `UniqueAcrossJoinedSlots` rejects a duplicate selected by another Joined Slot. Invalid request/profile, missing policy/default, non-Joined Slot, stale revision and duplicate policy rejection all return before commit and preserve the previous profile/revisions.

Selection intent is Session-scoped. `PlayerActorPreparationRuntimeContext` consumes the selected profile and its revision to prepare an Actor Runtime instance, and Activity lifecycle can further contextualize that prepared Actor, admit gameplay and attach input/occupancy. Conversely, Activity lifecycle may select a default as part of Activity entry. Those are distinct operations and owners; Activity participation/readiness is not implied by logical selection, and Session selection does not imply physical materialization or gameplay availability.

#### Findings: defect, risk and evidence gap

| Classification | Finding | Consequence |
|---|---|---|
| **No pre-confirmation owner loss demonstrated in the reviewed normal Leave path** | The host orchestration record is inserted after Begin and before Activity release; lower ledgers capture exact token/receipts before their irreversible operations. Manager Host evidence is passed back into the host record before the bridge release result is evaluated. | A failed stage leaves retry authority while the host/modules remain alive. This is source evidence, not an executed fault-injection proof. |
| **Structural shutdown risk** | All active Leave orchestration/retry owners and their dependencies are components/contexts under `FrameworkRuntimeHost`; a persistent rejection followed by host destruction ends their in-memory retry authority. Current `OnDestroy`/terminal teardown is not a controlled, retryable Leave protocol. | Jointly decide host stop semantics before implementation. Do not claim retry after host destruction or process exit. |
| **Structural receipt-retention risk** | Activity, Gameplay, terminal and bridge completion dictionaries are token-keyed and completed receipts are not removed in the inspected Leave methods. | Lifetime growth is possible across repeated Join/Leave in a long-lived Session. Bound cleanup only after proving no duplicate/retry observer needs the receipt; not a demonstrated correctness defect for 1.1.0 and likely a post-release improvement unless resource impact is shown. |
| **Physical-settle limit** | Manager release reports success when the backend call returns without throwing; Unity destruction may settle later. The method does not observe eventual GameObject destruction. | Keep logical release result distinct from post-settle physical observation as IF-ADR-020 requires. A synchronous physical-destruction promise is not established here. |
| **Evidence gap** | No current package test covers complete public command -> Activity -> Actor -> Host/Input -> terminal Slot across recoverable/persistent failures, nor the proposed stopping gate. | Add named package tests and integrated QA cases only after Gate A and separate implementation authorization. |
| **Transversal authority question** | Leave releases consumer Gameplay Availability blocks for the exact occurrence before releasing Gameplay Input; those token owners may later observe their token as stale because the Player occurrence ended. | Decide and prove cleanup semantics jointly with IF-ADR-044. Do not let a host gate reject cleanup of the exact Leave/Availability/Input token while its host remains recoverable. This is not itself proof that IF-ADR-044 is violated. |

#### Proposed architectural disposition for review

- Preserve IF-ADR-020's staged exact-occurrence Leave semantics and existing Session/Activity/Provisioning authorities. Do not redefine Leave as Activity exit, Joining close, Actor deactivation, scene unload or Session termination.
- A future host operational gate should reject new gameplay intent changes during stopping (Join/open Joining, Actor selection/replacement, Gameplay Availability acquire and GameFlow requests), while retaining an explicit cleanup allowlist. Exact-occurrence Leave is cleanup: permit a Leave of a still-current Joined occurrence and retries for its same active Leaving token while the host is alive and recoverable. Do not reject Leave merely because gameplay admission has closed. Whether shutdown automatically requests Leave for every Joined Player, or only permits explicit/host-issued Leave cleanup, remains a transversal policy decision.
- Keep block release, Leave stage retries and physical binding teardown callable under the same stopping cleanup authority. Reject new Availability block acquisition after stopping begins. Maintain token ownership and occurrence validation; no broad bypass for stale or foreign tokens.
- On controlled shutdown rejection, proposed behavior is to retain the same host and all exact owners/receipts, keep gameplay mutations closed, and permit same-token/occurrence cleanup retry. Terminal Unity/process exit remains best-effort and cannot promise post-destruction retry. Coordinate with OP-P0-PC / IF-ADR-001 and OP-P1-JOIN-AVAIL / OP-X-PROVISIONING before treating this as a route decision.
- Candidate Runtime changes, only if integrated fault injection confirms a gap: (a) host operational phase/operation allowlist enforced at scoped command ingress and internal cleanup paths; (b) an internal host-owned controlled-stop driver that reuses the canonical Leave coordinator and preserves the host on rejection; (c) receipt retirement at exact Session teardown after no retry/duplicate confirmation is possible. Do not add another Session/Actor authority, global retry registry, parallel Leave service, public Stop API or Stable API promotion.

**Alternatives rejected for this proposal:** a single `stopping` boolean that blocks release and cleanup along with gameplay mutations; destroying the host after a recoverable Leave rejection; treating `OnDestroy` as a controlled retry protocol; reconstructing physical Actor/Host by hierarchy lookup after evidence loss; clearing Slot/selection before required release receipts; compensating a partial Leave by silently rejoining/recreating the Actor; global recovery registry; treating Actor selection as Actor materialization or Activity participation; and promoting Experimental Leave/selection based on passing tests.

#### Candidate verification matrix (proposed; not executed)

| Case | Setup / injected condition | Required assertion / cleanup |
|---|---|---|
| Positive, Manager-Provisioned | Joined occurrence with selection, prepared Actor and active Activity Gameplay chain | Public command yields one matching Leave token; Activity/readiness/input/occupancy retire, Actor and exact Host/PlayerInput release, then Slot becomes Available; physical settle checked separately. |
| Positive, Scene-Provided | Adopted physical Player with and without a current Activity representation | Exact retained Host/Scene admission releases; absent Activity skips only contextual steps; terminal Slot and occurrence identity are correct. |
| Selection policy | Select/default/replace/clear under both duplicate policies | Expected profile and revision transitions; same-value/idempotent operations do not mutate; duplicate conflict names exact other Joined Slot. |
| Negative, no mutation | Invalid profile/request, missing default/policy, non-Joined Slot, stale selection revision, duplicate unique Actor, Select-over-existing, Replace-when-clear, invalid Clear payload | Rejection status is exact; prior selected profile, SelectionRevision and Session/Slot revisions remain unchanged. |
| Negative, stale identity | Old Leave token/request after Slot rejoin; preparation token for different profile/revision/occurrence; mismatched Host/Input evidence | Reject before affecting replacement occurrence/resources; exact new owner remains intact. |
| Rollback/retry, Activity | Fail admission release, Input release, occupancy release, contextual projection release, Actor release at each boundary, then recover | Slot remains Leaving; same token and stage flags/receipts retained; already released stages are not recreated; retry reaches terminal commit. |
| Rollback/retry, physical provisioning | Manager Host evidence succeeds then Host admission or physical backend rejects/throws; Scene Host/admission/assignment/context release rejects | Preserve exact Host/Input/release receipt and bridge record; no false physical success or Slot vacancy; retry same occurrence; distinguish irrecoverable partial destruction from recoverable rejection. |
| Persistent rejection / shutdown | Keep one required release failing across attempts while host enters controlled stopping | Host is retained; gameplay mutation/new acquire is rejected; exact Leave and release retry remains allowed; on terminal exit report best-effort with no post-destroy guarantee. |
| Duplicate Leave | Repeat while Joining is closed, while same occurrence is Leaving, after completion, and after later Join reuses Slot | Same active token resumes; completed duplicate confirms prior terminal result; stale duplicate cannot remove later occurrence. |
| Availability / Input / Pause | Active consumer Availability block and physical Input Gate; Pause binding present; fail physical unblocking/release | Leave cleanup does not lose token/receipt before physical readback; stale token after terminal occurrence is reported as stale; Pause/Input owner cleanup order and physical posture are explicit. |
| Camera / GameFlow integration | Activity Camera assignment/Actor subject, Activity transition and pending GameFlow request around Leave/stop | Camera occurrence is removed or retained exactly per its own owner; transition/observer output cannot imply Leave success; shutdown rejects new flow while allowing prerequisite cleanup. |
| Cleanup/idempotency | Repeat cleanup, release modules/scopes, then create a new Session/Join occurrence | No old token, Actor, Host, camera/input projection or completion receipt can mutate the new occurrence; every fixture restores its baseline and reports secondary cleanup failures separately. |

#### Dependencies and likely files

- **Runtime owner / likely touchpoints if approved:** `Runtime/PlayerParticipation/Authoring/PlayerSessionLeaveCommandTrigger.cs`; `Runtime/PlayerParticipation/Runtime/PlayerSessionScopedAccessRuntimeHostModule.cs`; `SessionPlayerLeaveRuntimeHostModule.cs`; `PlayerParticipationRuntimeContext.SessionPlayerLeave.cs` and `.SessionPlayerLeaveTerminal.cs`; `ActivityPlayerActorLifecycleParticipant.SessionPlayerLeave.cs`; `PlayerGameplayRuntimeHostModule.SessionPlayerLeave.cs` and `.ConsumerAvailability.cs`; `PlayerActorPreparationRuntimeHostModule.SessionPlayerLeave.cs` and `PlayerActorPreparationRuntimeContext.cs`; `LocalPlayerProvisioningRuntimeHostModule.SessionPlayerLeave.cs`, `LocalPlayerProvisioningBridge.SessionPlayerLeave.cs`, `SceneLocalPlayerAdmissionRuntimeHostModule.SessionPlayerLeave.cs`; `FrameworkRuntimeHost` lifecycle/gate owner. Touch only the actual owning module justified by a reproduced gap.
- **Cross-owner decisions:** OP-P0-PC / IF-ADR-001 controlled host start/stop and failure retention; OP-P1-JOIN-AVAIL / IF-ADR-044 exact Gameplay Availability acquire/release and Stable result implications; OP-X-PROVISIONING Host/backend ownership and physical release; Camera Session Assignment/Actor Subject owner; Pause/Input physical gate/receipt lifetime; GameFlow shutdown request ordering. IF-ADR-020 remains the Leave semantic owner. IF-ADR-021 remains the Activity placement owner; reopen either only if a proposed change crosses its accepted boundary.
- **Tests / QA likely:** add deterministic package coverage in `Tests/Editor/PlayerParticipation` for Leave orchestration, token ledgers, selection policy and failure injection; review `ActivityPlayerActorLeaveLedgerTests.cs`; add integrated scenarios only in QAFramework's Player Participation lifecycle fixtures after the public/source contract and exact package revision are agreed. Consumer validation in `planet-devourer` is separate and must use the immutable candidate. No QA assets/tests or consumer files are changed by this analysis.

#### Workpackage exit criteria

1. Reviewed source trace covers command ingress through Activity, prepared Actor, exact physical Host/PlayerInput, terminal selection cleanup and Slot/Session occurrence commit for both provisioning modes and no-Activity path.
2. Every retained token/receipt has a named owner, failure-stage retry behavior and cleanup lifetime; transversal review confirms no owner is lost before required release confirmation, or accepts a precisely bounded terminal failure.
3. Host stopping gate defines allowed/rejected operations, including Leave, Availability release/acquire, physical Input/Pause cleanup, Camera teardown and GameFlow; IF-ADR-001 / IF-ADR-044 / provisioning owners agree on exact occurrence and retry semantics.
4. Actor selection revision, duplicate policy, clear/replace/default semantics and distinction from Actor materialization/Activity participation are mapped to current Public-API/GOV-001 maturity with no implied promotion.
5. Test matrix names positive, negative/no-mutation, partial rollback/retry, persistent rejection, duplicate/stale occurrence, physical-settle and cleanup evidence with fixture owners and provenance. Executed evidence remains pending until a separately authorized implementation/certification stage.
6. Any demonstrated architecture change is routed to its owning ADR and accepted before implementation; unresolved concerns are listed with decision owner. Gate A and separate execution authorization remain mandatory.

### OP-P1-CAMERA — Stable Camera Assignment and Output transactions

- **Registro / status:** análise de código em `master` no snapshot local
  `617547a337e52022999c71c30f2e493a3dacbd26`; OP-P1-CAMERA continua Proposed,
  sem implementação ou certificação. Evidência abaixo é leitura estática; esta
  análise não executou NUnit, Unity ou QA.
- **Contrato e maturidade:** IF-ADR-039 estabiliza somente
  `ISessionCameraAssignmentCommandPort` / `ISessionCameraAssignmentCommandConsumer`,
  composição exata e comandos Activate/Replace/Clear com Assets Experimentais.
  `CameraOutputAuthoring`, `CameraRigComposer` e valores de suporte têm Stable
  limitado ao Output/rig por Session (1..N). `SessionCameraAssignmentAsset`,
  `CameraOutputDefinition`, `ActorCameraSubjectAuthoring` e o Trigger são
  Experimentais (Public-API.md). IF-ADR-043 aceita evidência focada para
  participação física `IndividualPerPlayer`; não promove Assignment, Subject,
  split-screen em geral ou todo IF-ADR-038. IF-ADR-038 é a autoridade normativa
  primária do modelo e ainda marca validação Unity pendente. Não ampliar esses
  limites por inferência a partir de comandos Stable ou QA focado.
- **Owners e fluxo real:** o comando público entra por
  `ISessionCameraAssignmentCommandPort` entregue ao consumer exato da
  composição; `SessionCameraAssignmentCommandTrigger` é adaptador opcional
  Experimental. O port delega ao `FrameworkRuntimeHost`/Session e a
  `SessionCameraAssignmentRuntime`. Session/`SessionCameraAssignmentRuntime` owns
  Assignment e Occurrence; `CameraOutputSession`/`CameraOutputState` own
  reserva, ocorrência retida/apresentada e cobertura Fallback por Output;
  `CameraOutputAuthoring`/applicator aplicam Unity Camera/Brain/rig; rig é
  materializado por ocorrência, sem estado mutável compartilhado. Assignment
  contém política, mapping explícito e identidade; ocorrência Session/Shared é
  Assignment+Output e Individual é Assignment+PlayerOccurrence exato+Output.
  `SessionCameraMembershipRuntime` projeta Player/Actor corrente na Assignment;
  `PlayerCameraOutputIntegrationRuntime` apenas publica/limpa a Unity Camera no
  `PlayerInput` correlacionado com evidência do Host exato. Activity/Route
  afetam elegibilidade de participação/target, mas não selecionam nem possuem
  Camera. O Actor Subject vem da ocorrência Actor preparada corrente e de sua
  evidência explícita, não de busca por hierarquia.
- **Activate:** valida/build de Asset e colisão/ownership de Outputs antes de
  materializar; cria ocorrências em staging, prepara rigs e cobertura, aplica
  candidato em Outputs e registra owner apenas em sucesso. Para membership
  explícita, o Host cria membership runtime a partir do Player Session e Actor
  source; se essa composição ou refresh do topology falha, tenta Clear do
  candidato. A compensação chama Clear e descarta seu diagnóstico, portanto
  falha da compensação pode não ser observável ao chamador. O refresh ocorre
  depois do Activate comitado, então retorno `false` pode coexistir com
  Assignment ativa se a compensação falhar. Output/PlayerInput topology é uma
  projeção derivada, não a autoridade da Assignment.
- **Replace:** resolve membros/Subjects correntes por Slot e PlayerOccurrence,
  materializa candidatos sob staging inativo, captura snapshots exatos de cada
  Output, valida commit, ativa candidatos, aplica todos os Outputs e então
  comita estado lógico. Falhas pré-commit tentam restaurar os snapshots e
  destroem candidatos; os testes cobrem candidato inválido e falha no segundo
  Output preservando Assignment/câmera/estado anteriores. Após commit, substitui
  registros atuais e remove owners antigos antes de desalocar recursos antigos.
  Erros de `RelinquishSubjectFallbackCoverageAfterReplacement` ou
  `DestroyObject` viram diagnóstico com retorno `true`; a antiga ocorrência já
  não está no ledger e não há fila de retry. O contrato precisa decidir se
  `true` significa apenas commit lógico ou também release físico concluído.
  No Host, `replaced && TryRefreshPlayerCameraOutputTopology(...)` também pode
  retornar `false` depois do Replace já comitado se a projeção/binding falhar;
  o chamador não distingue esses estados.
- **Clear / Leave:** Clear aplica Fallback e comita todos os Outputs antes de
  remover ocorrência, Assignment/Output owner e membership do ledger; só depois
  chama relinquish/destroy. Exceção pós-commit retorna `true` com diagnóstico
  e sem owner persistente para retry. `RemovePlayerOccurrence` exige
  `PlayerOccurrenceId` exato, remove membership de Shared/Session e, para
  Individual, libera Subject fallback, pede remoção exata ao Output, destrói
  Root e por fim remove ocorrência do ledger. Rejeição do Output mantém Root e
  ledger para retry, mas a cobertura Subject já foi liberada; a repetição deve
  provar idempotência/reconciliação. Exceção em destroy ocorre antes de remover
  ledger e pode ser tentada de novo. A integração Player→Camera limpa a
  associação exata antes de retirar o binding local, porém `Dispose` marca-se
  disposed primeiro; exceção em `ClearCamera` interrompe a limpeza e não admite
  nova tentativa nesse objeto. No Host, Clear pode retornar `false` após o
  Assignment já removido se o refresh topology falhar.
- **Shutdown / bootstrap / ordem:** o IF-ADR-038 requer bloquear mudança de
  Assignment/membership; encerrar Individual, depois Shared/Session, limpar
  publicações de Subject e liberar Outputs/Fallback por último. O
  `FrameworkRuntimeHost.OnDestroy` atual solta escopo persistente e GameFlow,
  dispose de membership, integração Player Camera, Assignment Runtime e por
  fim materialização/topology de Outputs. A ordem relativa entre Camera binding
  e Assignment/Output é compatível com liberar `PlayerInput.camera` antes de
  destruir Outputs, mas é best-effort: OnDestroy não tem gate/retry/resultado
  agregado e os Dispose intermediários descartam falhas. O Assignment
  `Dispose` marca `_disposed` antes de fallback/release; ignora o bool de
  `ResetSessionAssignmentToFallback`, depois destrói Roots. O Output topology
  chama teardown síncrono, mas também não expõe resultado agregado. Startup
  falho tenta rollback de Outputs/ocorrências; a retenção/retry após falha
  parcial de bootstrap não está estabelecida por contrato. Ordem candidata:
  bloquear gameplay mutations/acquire; permitir exclusivamente Leave e release
  exatos/repetíveis; concluir Actor/Activity/Availability/Input cleanup do
  Leave; remover Camera membership e binding PlayerInput; encerrar ocorrências
  Session Camera; liberar Outputs/Fallback; destruir módulos e Host por último.
  Essa ordem precisa ser conciliada com OP-P0-PC, OP-P1-LEAVE-ACTOR,
  OP-P1-JOIN-AVAIL e OP-X-PROVISIONING.
- **Seleção lógica vs materialização/participação:** seleção/default/revision e
  duplicate policy pertencem ao Player Session selection model (OP-P1-LEAVE-ACTOR),
  preparação do Actor pertence ao Actor preparation/provisioning owner, Camera
  Assignment projeta o Subject daquela ocorrência somente após join elegível,
  e Activity participation é projeção independente. Escolher Actor não cria
  automaticamente uma Camera; saída/replace de Actor deve atualizar Subject
  pela ocorrência exata sem trocar identidade de Player, Assignment ou Output.
- **Achados separados:** (a) **Comportamento demonstrado pelo código:** Clear e
  Replace removem os owners antigos antes da limpeza posterior; exceção após
  commit é reportada como sucesso (`true`) e não retém item de cleanup. Dispose
  marca-se inativo antes de operações de release e ignora retorno de Fallback;
  integração remove binding depois de executar `ClearCamera`, mas também marca
  disposed antes. Host Clear/Replace podem retornar `false` depois de mutation
  comitada por falha do refresh topology; Activate tenta compensação sem
  propagar o resultado de Clear e também pode retornar `false` com Assignment
  residual se a compensação falhar. (b) **Risco estrutural sem gatilho reproduzido:** falha Unity
  ao limpar associação, Subject coverage, fallback ou destruir objeto pode
  deixar recurso físico residual sem recibo/tentativa via mesmo owner; falha do
  refresh do topology depois de Activate/Replace/Clear pode deixar resultado
  parcial entre Assignment, binding e output; falha durante bootstrap pode
  interromper rollback. Não afirmar que isso ocorre em execução. (c) **Lacuna de
  evidência:** suites atuais não mostram fault injection de teardown pós-commit,
  repetição após falha persistente, destroy exception, OnDestroy best-effort ou
  failure matrix Unity. NUnit/QA históricos não foram reexecutados e não são
  ligados aqui a um SHA de execução.
- **Transações e decisões candidatas:** preservar prepare→apply→commit para
  Activate/Replace e rollback por snapshots pré-commit; conservar ocorrência e
  Output anterior até commit do novo estado, inclusive em todos os Outputs.
  Após commit, manter recibo/owner interno de ocorrência retirada até limpeza
  confirmada; `Dispose`/shutdown deve devolver ou registrar falha agregada e
  permitir retry enquanto o Host ainda existe. Separar status “committed” de
  “cleanup complete” internamente sem quebrar agora o port Stable. Shutdown
  gate deve rejeitar Activate/Replace/Clear (mutação de gameplay) e aceitar
  apenas cleanup correlacionado a identidade/occurrence já ativa, idempotente,
  inclusive Leave durante stopping. Avaliar política de retry/failure terminal
  na revisão transversal. Alternativas rejeitadas: descartar restos como sucesso
  silencioso; reverter assignment já comitada quando teardown antigo falhar;
  destruir Host/Outputs antes de liberar binding e ocorrências; impedir todo
  Leave durante shutdown; usar Slot/ordem/hierarquia como substituto de
  PlayerOccurrence ou Actor occurrence exatos.
- **Emendas normativas candidatas:** não alterar IF-ADR-039 nem ampliar sua
  superfície Stable. Na revisão, decidir se IF-ADR-038 precisa de adendo
  operacional para receipt/retry pós-commit e resultado de shutdown, preservando
  sua semântica já aceita; decisão de allowlist/gate e retenção do Host pertence
  a IF-ADR-001/OP-P0-PC, não ao Camera ADR. IF-ADR-043 só deve receber ajuste se
  o limite da certificação física precisar ser esclarecido; a evidência atual
  não justifica ampliar escopo. Não reabrir IF-ADR-046.
- **Runtime candidato, condicionado à confirmação transversal:** ledger interno
  de cleanup pendente para ocorrências retiradas após commit; release/dispose
  com resultado agregável e retry até confirmação; não definir `_disposed`
  antes de conservar owner/recibo de falhas; tratar erro de `ClearCamera` e de
  fallback/Output reset sem perder referência; tornar compensação de bootstrap
  observável. Não mexer em Runtime nesta fase; só aprovar mudanças após teste
  determinístico reproduzir cada gap e selecionar o módulo owner.
- **Matriz de testes futura (não executada):**

  | Cenário | Evidência objetiva requerida |
  |---|---|
  | Activate Session/Shared/Individual válida; zero Player com Fallback; membership e Subject corrente | Assignment/Occurrence/Output exatos; rig isolado; PlayerInput binding só com Host correspondente; cobertura física após settle |
  | Candidato inválido, Output ocupado, mapping/topology inválido | rejeição sem mutation: mesma ocorrência, snapshot, Camera visível e fallback/binding anteriores |
  | Replace 1→N e N→1; falha em cada Output, staging, apply e commit | rollback completo pré-commit ou novo estado integral; nenhum Output misto; owner anterior preservado até commit |
  | Falha após commit ao liberar Subject coverage/destroy da ocorrência anterior | resultado distingue commit de cleanup; recibo antigo permanece; retry remove exatamente o resíduo sem afetar novo Assignment |
  | Clear/Individual Leave com Output reject, fallback reject, `ClearCamera`/destroy failure | Leave mantém token/Occurrence/owner retryável; Assignment e outros Players intactos; retry converge |
  | Leave duplicado, stale occurrence, rejoin do mesmo Slot, Actor replace e Subject invalidation | idempotência para token ativo/concluído; identidade antiga rejeitada; nova ocorrência não afetada |
  | Shutdown controlado: comando novo, Leave pendente, cleanup permitido, rejeição persistente | gate rejeita gameplay mutation/acquire e permite release exato; Host retido até retry ou resultado terminal explícito |
  | Shutdown/destruição após membership, binding, Assignment, Output reset em cada etapa | ordem observada: Leave/Activity/Actor/Input, PlayerInput.camera, ocorrências, Outputs/Fallback, módulos/Host; cada falha preserva recibo até report/retry |
  | Bootstrap parcial e rollback falho; repetição do dispose; cleanup baseline | sem falso sucesso; recursos/owners pendentes enumerados e retryáveis; baseline restaurado; diagnósticos primário e secundário preservados |

- **Cobertura existente e proveniência:** NUnit encontrados em
  `Tests/Editor/Camera/Assignments/SessionCameraAssignmentRuntimeTests.cs`,
  `SessionCameraAssignmentCompositionTests.cs`, `CameraOutputStateTests.cs`,
  `PlayerCameraOutputIntegrationTests.cs`, `CameraOutputAuthorityBoundaryTests.cs`
  e testes de authoring/identidade. Runtime tests cobrem startup/materialização,
  clear/replace normal, fallback temporário, membro exato Leave/Rejoin,
  candidate inválido, falha de segundo Output e ordem de desativação. Não
  localizei injeção de falha em cleanup pós-commit ou Dispose. QAFramework
  histórico inclui QA-NEW-004 9/9 e IF-ADR-042/043 7/7 cada; documentação
  registra Camera Editor 78/78 e outras contagens históricas. Isso é evidência
  reportada/registrada para seus recortes, não execução neste turno, não
  certifica Stable command port contra todas as falhas e não prova o snapshot
  atual. `SessionCameraShutdownOrderProbe` testa desativação/ordem da ocorrência,
  não o gate transacional nem o agregado de shutdown do Host.
- **Dependências / arquivos prováveis:** OP-P0-PC/IF-ADR-001 (shutdown gate,
  Host retention), OP-P1-LEAVE-ACTOR/IF-ADR-020 (Leave e ocorrência),
  OP-P1-JOIN-AVAIL/IF-ADR-044 (cleanup availability), OP-X-PROVISIONING
  (Host físico), OP-P1-PAUSE-INPUT-GATES (PlayerInput/Pause/Input), GameFlow e
  Activity/Route readiness. Se autorizadas depois, inspecionar
  `Runtime/Camera/Assignments/SessionCameraAssignmentRuntime.cs`,
  `Runtime/Camera/Output/CameraOutputSession.cs` e `CameraOutputState.cs`,
  `Runtime/Camera/Integration/PlayerCameraOutputIntegrationRuntime.cs`,
  `Runtime/Camera/Lifecycle/CameraSessionOutputMaterializationRuntime.cs`,
  `Runtime/Camera/Output/CameraOutputSessionTopology.cs`,
  `Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs`; testes correspondentes
  em `Tests/Editor/Camera/Assignments` e cenário integrado só no QAFramework
  após fixar candidate SHA. Nenhum arquivo Runtime/teste/QA foi alterado.
- **Alternativas fora do corte Stable:** expandir Assignment/Subject assets,
  split-screen ou Camera Trigger para Stable; transferir a seleção de Camera a
  Activity/GameFlow; inventar fallback ou inferir Subject por hierarchy; incluir
  layout geométrico no Output contract. Só discutir em workpackage próprio,
  caso dependência Stable objetiva exija.
- **Critérios de encerramento:** revisão transversal confirma owners e ordem
  Leave→Camera membership/binding→Input/Host→Session Output conforme seus
  contratos; resultado de commit vs cleanup e retry terminal ficam definidos
  sem alterar estabilidade pública; decisões de adendo e Runtime têm owner
  normativo; matriz cobre os cenários acima com falha determinística e evidência
  causal; QA integrado identifica package/scene/candidate SHA e baseline
  restaurado; execução Unity/QA separada certifica somente a superfície
  delimitada; tracker e API maturity são atualizados após autorização própria.
  OP-P1-CAMERA e Fase 2 permanecem abertos até esses passos e Gate A.

### OP-P1-PAUSE-INPUT-GATES — Pause, physical Input and Gate composition

- **Registro / status:** análise estática do código atual em `master`, snapshot
  `617547a337e52022999c71c30f2e493a3dacbd26`. OP-P1-PAUSE-INPUT-GATES
  permanece Proposed; não houve execução de NUnit, Unity ou QA neste turno.
- **Contratos e autoridades:** `PauseRuntime` possui apenas estado lógico
  Running/Paused e snapshot; `PauseGateBlockerPolicy` projeta blockers passivos
  InputAcceptance e InteractionAcceptance enquanto Paused. Gate runtime compõe
  blockers por owner/domain. `UnityPlayerInputGateAdapter` observa Gate e é
  autoridade física limitada a bloquear/restaurar o Gameplay Action Map;
  `UnityPlayerInputStateWriter` é o único writer package-owned de posture/map
  state. `PlayerInputActionMapReference` resolve Action Map pelo GUID no
  `PlayerInput.actions` exato, sem fallback por nome. Pause product é o owner da
  transação Pause + write receipt; input-binding runtime de Gameplay é outro
  owner e deve respeitar a supressão Pause ao reparar seu mapa. `PlayerInput`
  continua owner das instâncias/actions, não do policy Gate. Pause não adiciona
  blocker `GameplayAction` à sua policy, e este registro não propõe fazê-lo.
- **Owners e sequência real:** `PauseRequestTrigger` chama o port de request;
  `PauseProductBindingRuntimeContext` serializa uma operação com
  `_requestInFlight`/`PauseProductTransaction`; `FrameworkRuntimeHost.RequestPause`
  aplica as regras de admissão e delega ao `PauseRuntime`, depois sincroniza
  TimeScale/presentation. Com binding ativa, o product context aplica a posture
  física ao `PlayerInput` exato através do Gate Adapter/canonical writer. Pause
  escreve o conjunto Global-only, preserva recibo do mapa antes/depois e liga
  `IsContractedGameplayRestoreSuppressed`; Resume restaura o conjunto registrado
  e só então remove a supressão e o recibo. Sem binding ativa, o caminho
  application-only não escreve posture física.
- **Binding / recibos e invariantes:** `PauseSessionPlayerInputBindingRuntimeHostModule`
  correlaciona Slot→Host corrente, seleciona o `PlayerPauseInput` co-localizado
  e guarda Slot, Host, Binding e `PauseProductBindingToken`. Product context
  captura `PausePlayerInputPostureReceipt` antes de binding (PlayerInput
  EntityId, mapa corrente, conjunto enabled, default map e inputIsActive), mais
  `UnityPlayerInputActionMapSetWriteReceipt` para o Pause posture. Tokens são
  geração + PlayerInput EntityId; release rejeita token foreign/stale. Player
  binding mantém port/token em falha recoverable. Os recibos só podem ser
  consumidos após readback/restore exato ou encerramento terminal explícito do
  PlayerInput alvo destruído. Binding e pausa são single-player no Stable
  contrato; não inferir suporte multiplayer.
- **Pause/Resume e compensação:** admissão consulta Transition Gate e Activity
  ativa. Pause aplica primeiro estado lógico (incluindo Gate/TimeScale/surface),
  depois escreve mapas físicos; falha física tenta retornar Pause snapshot ao
  anterior. Resume aplica Running lógico, tenta restaurar o conjunto exato;
  falha de restore reaplica o conjunto Pause, restaura snapshot Paused e mantém
  suppression/receipt. Falha de commit também tenta compensar o físico e o
  lógico; no caminho de falha de Resume liga suppression novamente. Resultado
  de request diferencia Applied/Rejected/Failed. Rejeição de admissão não deve
  fazer write físico. Lifecycle Activity exit usa caminho interno que bypassa
  admissão: restaura físico antes de Running lógico e bloqueia commit/saída da
  Activity se a restauração falhar; se o PlayerInput exato já foi destruído,
  encerra como TargetDestroyed sem declarar postura restaurada. Sem mudar
  IF-ADR-005, preservar essa diferença entre comando externo e cleanup interno.
- **Gate físico e repair loop:** adapter aplica Gate lógico ao gameplay map e
  verifica posture física; retry automático de Update é limitado a duas
  tentativas e pode ser reiniciado por Apply/Restore explícito. Após limite,
  mostra RetryLimitReached; não promete supressão física diante de falha
  persistente. O repair do Gameplay Input consulta
  `IsContractedGameplayRestoreSuppressed` e não religa o mapa enquanto Pause
  possui a posture. Gate/Pause/Availability são sinais/autorizadores lógicos;
  somente adapter/writer aplica mapas. `GameplayReady` estrutural não muda
  quando Gate/Pause/Availability mudam.
- **Activity exit, Leave, replacement e shutdown:** Activity transaction chama
  precondição Pause antes de commit; depois do commit, libera Activity-scoped
  Pause binding antes do teardown dos Players. Rejeição da precondição bloqueia
  commit; falha posterior de release de binding ocorre após Activity authority
  já comitada e impede o restante do teardown, mantendo evidence no
  `PauseActivityBindingRuntimeContext`. Session Pause binding acompanha evento
  de mudança/liberação do Host; em Leave, a liberação física deve terminar antes
  de destruir o PlayerInput ou tornar o Host irrecuperável. `PlayerGameplayAvailabilityBlockTrigger`
  mantém token por ocorrência, mas não possui a postura física; Camera mantém
  `PlayerInput.camera` por binding próprio/exato. Ao Leave/replacement, Availability
  release, Pause binding posture e Camera association são operações diferentes
  no mesmo PlayerInput; nenhuma pode apagar o recibo da outra. Camera deve limpar
  somente sua associação ainda igual à Camera que possui. Durante stopping,
  bloquear novas requests gameplay/Pause, novos bindings, acquire Availability,
  transições e mutações não-cleanup; permitir Activity/Leave teardown, release
  de tokens/bindings já existentes, restauração de posture e retry exato enquanto
  Host/PlayerInput existirem. Comandos comuns Pause/Resume permanecem sujeitos
  ao gate; cleanup interno bypass autorizado continua disponível. Hoje
  `FrameworkRuntimeHost.RequestPause` verifica Transition Gate e Activity ativa,
  mas não verifica estado explícito Started/Stopping do Host; o acesso público
  típico está scoped, então aceitação durante bootstrap incompleto/stopping ainda
  requer cenário integrado. Host/Application destruição terminal é best-effort,
  não garante retry após perder o alvo.
- **Availability trigger / tokens:** `PlayerGameplayRuntimeHostModule` registra
  tokens por PlayerOccurrence e mantém o ledger se a projeção de unblock falha;
  Leave também preserva ledger até `TrySetConsumerAvailabilityBlocked(false)`
  confirmar. Porém `PlayerGameplayAvailabilityBlockTrigger.OnScopedAccessReleasing`
  limpa `_ownedToken` incondicionalmente após chamar RequestRelease, mesmo se o
  resultado for `FailedInputProjection`/`RejectedRuntimeUnavailable`; token local
  do consumer se perde antes de confirmação. O runtime ainda pode reter token
  lógico, mas deixa de existir owner no trigger para retry explícito. Isso conflita
  com o contrato de um token por consumer de IF-ADR-044 e é achado de implementação,
  não motivo para confundir block lógico com estado do Action Map.
- **Achados separados:** (a) **Defeitos/risco de owner demonstrados estaticamente:**
  trigger de Availability descarta token local em falha do release scoped;
  `PauseSessionPlayerInputBindingRuntimeHostModule.OnDestroy` chama ReleaseActive,
  mas depois zera port/player/runtime refs mesmo quando release falhou e token
  continuou pendente; como o módulo está no GameObject do FrameworkRuntimeHost,
  destroy elimina o próprio retry owner. `PlayerPauseInput.OnDestroy` também
  tenta ReleaseLocal; falha mantém estado só no componente que está sendo
  destruído. `PlayerGameplayInputBindingRuntimeContext.ClearConsumerAvailabilityProjections`
  também ignora falhas ao remover cada projection e limpa seus Sets/Adapter map;
  `PlayerGameplayRuntimeHostModule` em seguida limpa o ledger de tokens durante
  shutdown. No writer, `TryApplyActionMapSet` cria rollback receipt local; se a
  escrita desejada e rollback falham, retorna `false` com receipt de saída
  default, deixando somente qualquer binding-level posture receipt mais ampla
  como possível recuperação. Esses paths são condicionais a rejeição recoverable,
  não foram reproduzidos neste turno. (b) **Compensação demonstrável, resultado não
  confirmado:** `RequestPause` ignora bool/diagnóstico ao compensar Pause snapshot
  após erro físico ou de commit; assim, falha secundária pode divergir estado
  lógico de Pause do físico, embora resultado externo seja Failed. ReleaseBinding
  e Activity exit retêm token/receipt/supressão quando a falha é recoverable.
  (c) **Risco estrutural:** Activity-scoped binding release pode falhar depois
  de transição comitada; ordem de `OnDestroy` do Host, Pause module, PlayerPauseInput,
  Gate Adapter e Camera integration não é uma transação result-bearing comum.
  Adapter OnDisable/OnDestroy executa RestoreIfNeeded; falha deixa estado local
  no Adapter e não agenda retry após Update/destruição. (d) **Lacunas:** não encontrei
  pacote teste que force falha simultânea de apply + compensação lógica, falha
  persistente de binding release no OnDestroy/Host stopping, falha do callback
  scoped Availability release, nem composição pós-commit Activity exit + Camera
  + Pause/Input + Leave.
- **Decisões arquiteturais candidatas:** separar decisão lógica/admissão, posture
  física e composição de disponibilidade; manter writer físico único, recibos
  exatos e propriedade local dos tokens. Na parada controlada, host gate deve
  fechar gameplay commands/acquire e preservar cleanup/retry autorizado por
  token/identity existente. Não converter Gate em lifecycle manager, não adicionar
  receipt registry transversal e não criar segunda autoridade de Availability.
  Alternativas rejeitadas: converter Pause em blocker `GameplayAction`; atrelar
  Gate a GameplayReady/Join/Actor readiness; permitir que Trigger escreva mapa
  diretamente; zerar recibos no primeiro erro; aceitar simultaneamente Pause e
  Transition Gate sem policy/evidência. Preservar IF-ADR-036: Transition Gate
  pode coexistir com GameplayReady=true e RuntimeGameplayAvailable=false; Pause
  admitido durante Gate continua rejeitado. IF-ADR-044 block não muda readiness,
  Actor, Camera, device ou participação.
- **Mudanças mínimas candidatas de Runtime:** (1) trigger Availability só limpar
  token após `SucceededReleased` ou decisão terminal stale/occurrence destruída
  autorizada pelo contrato, mantendo token/access de retry em falha; (2) Pause
  Session module e binding component não descartarem owner/token em shutdown até
  release confirmado ou target destruído terminal, com teardown controlado
  resultado-bearing pelo owner de Host (OP-P0-PC); (3) propagar/registrar resultado
  de compensação lógica e manter estado/supressão/receipt explícitos quando
  compensação falha; (4) preservar receipts locais de apply mesmo quando
  rollback de map-set falha e agregar resultado de availability projection
  durante shutdown, sem apagar owner até resultado terminal. Aprovar somente os
  cortes reproduzidos e locais ao owner; não ampliar API Stable nem alterar
  `IF-ADR-005` por presunção.
- **Emenda normativa:** nenhuma necessária para o contrato Pause/Gate/Availability
  se a implementação voltar a cumprir ownership/retry atual. A implementação
  do trigger deve cumprir o contrato já aceito IF-ADR-044; não alterar essa
  decisão nesta análise. Governança do Host gate/controlled stop pertence ao
  IF-ADR-001/OP-P0-PC. Reabrir ADR aceito apenas se revisão transversal provar
  mudança semântica, não para acomodar defeito de implementação.
- **Dependências:** OP-P0-PC/IF-ADR-001 para bootstrap/stopping e retenção do
  Host; OP-P1-JOIN-AVAIL/IF-ADR-044 para tokens de Availability e release;
  OP-X-PROVISIONING e OP-P1-LEAVE-ACTOR para Host/PlayerInput lifetime, Leave e
  retries; OP-P1-CAMERA para `PlayerInput.camera` e ordem de clear; Activity/Route
  transition / GameFlow para Transition Gate, Activity commit e lifecycle bypass;
  Persistent Content / IF-ADR-040 para a ordem de participant release e falha
  result-bearing. IF-ADR-005, IF-ADR-036 e IF-ADR-044 continuam autoridades
  aceitas; combinar evidências separadas não certifica composição.
- **Arquivos prováveis se posteriormente autorizados:**
  `Runtime/Pause/PauseProductBindingRuntimeContext.cs`,
  `PauseActivityBindingRuntimeContext.cs`,
  `PauseActivityBindingRuntimeHostModule.cs`,
  `PauseSessionPlayerInputBindingRuntimeHostModule.cs`, `PlayerPauseInput.cs`,
  `Runtime/UnityInput/UnityPlayerInputGateAdapter.cs`,
  `UnityPlayerInputStateWriter.cs`,
  `Runtime/PlayerParticipation/Authoring/PlayerGameplayAvailabilityBlockTrigger.cs`,
  `Runtime/PlayerParticipation/Runtime/PlayerGameplayRuntimeHostModule.ConsumerAvailability.cs`,
  `Runtime/PlayerParticipation/Runtime/PlayerGameplayInputBindingRuntimeContext.cs`,
  `FrameworkRuntimeHost.PauseActivityLifecycle.cs`,
  `ActivityFlowRuntime.Transaction.cs` e somente os respectivos owner modules.
  QA integrado provável em QAFramework; não tocar neste plano.
- **Matriz futura de testes (não executada):**

  | Cenário | Evidência objetiva requerida |
  |---|---|
  | Bind com posture inicial variada e Pause/Resume | PlayerInput EntityId e GUID maps exatos; readback current/enabled/default/input-active restaurado; receipts consumidos só após confirmação |
  | Transition Gate ativo / sem Activity / host ainda Starting | Pause request rejeitado sem state/map/timeScale/surface mutation; GameplayReady permanece estrutural e intocado |
  | Apply físico falha e compensação lógica/física sucede | Resultado Failed/Rejected coerente; snapshot anterior e mapas confirmados; sem receipt órfão |
  | Apply e compensação falham; Resume restore falha | divergência identificável sem falso sucesso; suppression e receipt retidos; retry recupera postura correta |
  | Commit Pause/Resume falha depois da escrita física | rollback para posture e Pause anterior ou recuperação explícita; readback e diagnóstico das duas falhas |
  | Activity exit Paused com sucesso ou restore reject persistente | restore físico precede Running/commit; reject impede commit; token de binding permanece; retry converge |
  | Activity Pause binding release falha depois do commit | Activity result identifica authority já comitada e cleanup pendente; owner/host/binding retidos; Player exit subsequente bloqueado até retry |
  | Leave/release Availability/Pause/Camera com falha isolada e combinada | cada owner mantém seu token/receipt; nenhum Gameplay map, Camera association ou PlayerInput é alterado pelo owner alheio; retry exato antes da destruição |
  | Trigger Availability scope release rejeitado | token continua recuperável no owner ou falha terminal explícita; não deixar blocker/projection órfãos |
  | Troca de binding/Host, adapter disable/destroy, action map renomeado | binding antigo restaurado/rejeitado precisamente; map GUID no target correto; nenhuma restauração aplica em PlayerInput substituto |
  | stopping: comandos normais, cleanup, retry e target destruído | novas requests/acquire/transitions bloqueadas; release/retry autorizados; TargetDestroyed terminal não declarado Restored |
  | Gate/Availability/Pause recomposition e repair loop | duas tentativas automáticas/explicit retry observadas; gate readback confere; Pause suppression impede repair do Gameplay map sem criar blocker GameplayAction |
  | Gate/Availability/Pause muda enquanto GameplayReady é true | GameplayReady e admission identity estáveis; só RuntimeGameplayAvailable varia; nenhum Join/Actor reprovision |
  | Repeated cleanup e baseline | Release idempotente apenas com token/receipt exatos; baseline de mapas, input active, timeScale/surface e Camera restaurado; falhas secundárias reportadas |

- **Cobertura atual e proveniência:** `Tests/Editor/Pause/PauseProductTransactionTests.cs`
  contém 18 NUnit cases incluindo Pause/Resume, falha física, commit failure,
  Activity exit retry, suppression contra gameplay-map repair, bounded adapter
  retries, binding release com receipt retido/retry e target destruído terminal.
  Cobertura prova doubles/reflection e cenários focados, não execução neste turno
  nem composição integrada de Host stop/OnDestroy + Availability trigger + Leave
  + Camera. Registro histórico: Pause EditMode 18/18 reportado; QA-IF-ADR-036
  Play Mode 8/8 user-reported BaselineRestored cobre readiness/availability sob
  Transition Gate, Pause rejeitado durante Gate e Pause/Resume depois da liberação;
  IF-ADR-044 QA 8/8 BaselineRestored e manual FirstGame do trigger cobre P1/P2
  Block/Release e composição com Pause, sem testar a falha citada do callback de
  scoped release. IF-ADR-005 mantém Play Mode e PEGA consumer validation pendentes
  para o cut transacional atual. Nenhum desses resultados identifica/certifica
  o candidato futuro ou a composição completa desta matriz.
- **Critérios verificáveis para futura implementação/certificação:** revisar
  transversalmente semântica de Stop e quais cleanups seguem permitidos; aprovar
  ownership de retries e alterações mínimas com causa reproduzida; preservar
  boundaries Stable/Experimental e decisões IF-ADR-005/036/044; executar fault
  matrix determinística por owner, inclusive compensação falha e trigger release;
  demonstrar receipts e tokens retidos até confirmação/terminalidade; prova
  integrada precisa registrar package/scene/candidate SHA, mapa físico/readback,
  Pause/Gate/Availability/GameplayReady/RuntimeGameplayAvailable, Host/Camera
  identity e BaselineRestored. Evidência histórica fica citada no escopo original,
  não reutilizada como certificação do futuro candidato. OP-P1-PAUSE-INPUT-GATES
  e Fase 2 permanecem abertos; Gate A e autorização futura continuam necessários.

### OP-P2-APP-ROUTE-ACTIVITY-GAMEFLOW — Boot, authoring and transitions

- **Status:** Open — análise estática registrada; desenho aguardando revisão
  transversal. Não implementado, não certificado e sem encerramento operacional.
- **Contratos / maturidade:** Application, `GameApplicationAsset`, `RouteAsset`,
  `ActivityAsset`, GameFlow request/result e transições de autoridade que os
  consomem preservam os limites Stable vigentes em IF-GOV-001 e IF-ADR-047/048.
  Readiness, Loading/visual transition, Transition Gate mode, projections e
  detalhes experimentais de Activity continuam Experimentais conforme seus
  ADRs. Este registro não promove esses comportamentos nem modifica schema ou
  intenção serializada.
- **Owners observados:** `FrameworkRuntimeHost` compõe recursos e é owner do
  ciclo de vida do Host; `ImmersiveFrameworkBootstrap` resolve configuração e
  conduz o bootstrap; `GameFlowRuntime` é autoridade de admissão, Gate e
  resultado das requests; `RouteLifecycleRuntime` mantém Route/occurrence,
  cenas, conteúdo e bindings da Route; `ActivityFlowRuntime` mantém Activity,
  occurrence/readiness e transação de Activity; `SceneLifecycleRuntime` agrega
  participantes de Session; `GlobalUiSceneRuntime` mantém transferência e
  restauração de Persistent Content. `RuntimeContentRuntime` mantém roots e
  owners tipados. Esses módulos internos bastam como owners: não se propõe novo
  registry, coordinator ou estado público.

#### Evidência do código atual

- [`ImmersiveFrameworkBootstrap`](../../../Runtime/Bootstrap/ImmersiveFrameworkBootstrap.cs)
  valida Settings, cria Host, chama `StartAsync`, depois anexa Provisioning.
  Falha de `StartAsync` só é registrada e retorna; exceção no `async void` é
  registrada sem stop explícito. Falha ao anexar Provisioning destrói o GameObject
  do Host diretamente. Não há nesta sequência uma operação result-bearing de
  stop controlado que todos os caminhos de falha invoquem.
- [`FrameworkRuntimeHost.StartAsync`](../../../Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs)
  prepara Primary Scene, carrega Global UI, materializa Outputs, compõe
  Persistent Content em `SceneLifecycleRuntime`, comita a transferência de
  roots para lifetime da aplicação e ainda executa vários attaches e GameFlow
  startup que podem falhar depois desse commit. Em falha de composição/commit,
  tenta release local e atualiza `_persistentContentScopeComposed`; falhas
  posteriores retornam `Started=false` sem um teardown uniforme. A composição
  de Session já habilita bindings/triggers antes de GameFlow estar pronto.
- `RequestRouteAsync` e `RequestActivityAsync` no Host rejeitam quando
  `_gameFlowRuntime == null`; depois disso delegam ao GameFlow sem verificar
  fase `Started` ou `Stopping`. `FrameworkRuntimeHost` não mantém hoje um estado
  explícito de admission/stop. Evidência mostra ausência do gate; aceitação de
  uma request durante cada janela concreta de bootstrap/stopping ainda precisa
  ser reproduzida em fixture integrada.
- `GameFlowRuntime.StartAsync` aplica Transition Gate em alguns caminhos e
  `RequestRouteAsync`/`RequestActivityAsync` avaliam Gate e operação em voo.
  `finally` de operações cobertas chama `ReleaseTransitionGateIfStillActive` e
  limpa o marcador de request. Esse Gate é terminal da transição; não representa
  estado de vida do Host e seu release de memória não prova limpeza de todos os
  efeitos externos da transição.
- `ActivityFlowRuntime` prepara Scope, cenas, bindings, Resettable e requisitos
  antes do commit. O commit atualiza `_currentActivityState` e marca a
  transação; depois executa saída de participantes/conteúdo, release das cenas
  anteriores e readiness do destino. Falha pré-commit entra em
  `FailBeforeCommitAsync` e tenta rollback dos recursos do alvo. Falha pós-commit
  retorna destino autoritativo com estado `CommittedNotReady` ou
  `CommittedFinalizationFailed`; portanto não deve ser reportada como rollback
  para Activity anterior. O snapshot de transição é autoridade interna para
  distinguir esses resultados.
- `RouteLifecycleRuntime.StartRouteAsync` tem sequência diferente: limpa a
  Activity anterior e libera cenas/content da Route anterior antes de concluir
  a composição da Route alvo. A Route alvo passa a `_currentRouteState` e
  publica transição apenas no fim, mas failures intermediárias retornam falha
  após efeitos parciais possíveis sobre Activity/cenas/bindings antigas. Isso
  é uma propriedade transacional a decidir e cobrir; os ADRs Stable não declaram
  atomicidade integral da Route nem restauração automática do estado anterior.
- `OnApplicationQuit` e `OnDestroy` chamam `ReleasePersistentContentScope`,
  cujo retorno é `void`; se Session release falha, ele registra erro e mantém
  `_persistentContentScopeComposed`, mas o chamador continua descartando
  GameFlow/readiness/Camera/materializadores e o Unity pode destruir o Host.
  `OnApplicationQuit` também descarta Camera membership/assignment. Em
  destruição terminal não há garantia de retry posterior; em shutdown controlado
  essa perda de dependências/recibos antes do release confirmado é incompatível
  com o retry pretendido em OP-P0-PC. A integração completa de shutdown ainda
  não foi certificada.
- O código já possui owners locais e receipts para vários recursos; não foi
  demonstrada necessidade de um owner global adicional. O problema central é
  admissão uniforme e retenção dos owners existentes até seus releases
  confirmarem, em coordenação com OP-P0-PC e workpackages de Player/Camera/Input.

**Cobertura existente e proveniência:** na árvore atual, a suite diretamente
nomeada encontrada para Route/Activity é
[`RouteActivityTransitionObserverTests`](../../../Tests/Editor/RouteLifecycle/RouteActivityTransitionObserverTests.cs): cobre dispatch de observers e ordem de notificação após
Activity replace/clear enquanto a composição da Route permanece carregada; não
é teste de autoridade/rollback integral de Route ou Host. `FlowTriggerCompositionTests`
é citado no tracker como cobertura de boundary/composição, não de transação.
O tracker registra reports históricos de 310/310 EditMode e QA-IF-ADR-036
Play Mode 8/8; QA-NEW-002 cobriu controlled Activity readiness/rollback e
owner release, e QA-NEW-003 Route A→B→A com sobrevivência de owner. Esses
registros têm escopo útil, mas não estabelecem evidência imutável deste SHA nem
cobrem o shutdown proposto. Nenhum teste/QA foi executado nesta análise.

#### Invariantes e proposta para revisão transversal

1. Host, Session scope e roots exatos permanecem vivos enquanto bootstrap
   incompleto ou stop controlado tiver release pendente. Uma falha mantém Host
   inerte para novos trabalhos, porém retryável pelo mesmo owner; só release
   agregado confirmado permite descartar dependências e finalizar Host.
2. Requests de Route/Activity e demais mutações de gameplay só são admitidas
   após bootstrap pronto e enquanto Host operacional. Cleanup explícito e retry
   autorizado continuam permitidos durante stopping: Activity/Route exit,
   Player Leave, releases exatos de Availability, Camera, Pause/Input e
   participantes; comandos que criem nova participação, binding, assignment ou
   conteúdo ficam fechados. A lista e owner de cada exceção dependem da revisão
   transversal. Não usar um booleano que bloqueie callbacks necessários à
   própria liberação.
3. Cada release mantém identidade de Session/Route/Activity scope, occurrence,
   owner/token e receipt até sucesso. A sequência deve parar ou preservar
   evidência recuperável em rejeição, sem transformar falha de cleanup em
   sucesso de transição. Repetição deve ser idempotente para owners já liberados.
4. Pre-commit Activity failure conserva Activity anterior como autoridade e
   reporta toda compensação pendente. Pós-commit failure preserva destino como
   autoridade e reporta readiness/finalização incompleta. Para Route, a revisão
   deve decidir se garante rollback do estado antigo ou especifica resultado
   parcial recuperável; não inferir atomicidade do resultado atual.
5. Transition Gate sempre alcança resultado terminal e diagnóstico no GameFlow;
   shutdown não pode depender só do `finally` do Gate para provar release de
   Activity, Player, Input/Camera, Scene ou Persistent Content. Preservar
   IF-ADR-036: `GameplayReady` estrutural permanece independente de Pause/Gate,
   `RuntimeGameplayAvailable` segue operacional e Pause durante Transition Gate
   continua rejeitado. IF-ADR-046/047 permanecem vigentes; não reabrir
   IF-ADR-036, 046 ou 047 sem evidência nova.
6. `GameApplicationAsset`, `RouteAsset` e `ActivityAsset` mantêm nomes/campos,
   valores serializados, referências exatas e intenção documentada. Revisar
   round-trip, valores inválidos/enum desconhecido e Unity import/build como
   evidência; nenhum ajuste de schema é proposto nesta análise.

**Fases operacionais candidatas (internas ao Host; não API pública):** criação /
composição inicial; bootstrap em progresso; operacional após todos os módulos
obrigatórios e GameFlow startup; stopping após fechar admissão de novas
operações; inerte/retryável quando cleanup controlado falha; liberado após
release agregado. Antes de criar um enum, revisar se um estado interno do Host
mais guards em owners existentes expressa as fases sem duplicar autoridade.
Não chamar um estado de `Started` com base apenas em `GameFlowStarted` se
Provisioning obrigatório ainda não foi anexado.

**Ordem de teardown proposta, sujeita à revisão transversal:** (a) fechar novas
requests/acquires e impedir nova transição; (b) terminar ou registrar de forma
consistente transição em voo e terminalizar efeitos/Gate; (c) liberar Activity
e Route bindings/content, sem unload dos alvos que os receivers ainda usam;
(d) para cada Player occurrence, liberar Camera membership/Assignment e
`PlayerInput.camera` com ports vivos, restaurar Pause/Input físico e limpar
Availability conforme owners; executar Leave da ocorrência e liberar Actor,
Host/binding físico de acordo com IF-ADR-019/020 e OP-P1-LEAVE-ACTOR;
(e) liberar bindings restantes de Pause/Input e participantes de Session
enquanto Session/Camera/Input adapters ainda existem; (f) liberar Scene
Lifecycle Session ledger e Persistent Content/roots através de
`GlobalUiSceneRuntime`; (g) apenas após sucesso agregado, descartar runtimes,
ports e Host. Dependências exatas entre Pause restore, Leave, Camera clear,
Availability release, Activity exit, Scene participant e Persistent Content
permanecem para decisão conjunta OP-P0-PC × Join/Availability × Provisioning ×
Leave/Actor × Camera × Pause/Input. Quit/destroy real permanece best-effort,
sem promessa de retry pós-processo.

**Alternativas rejeitadas nesta proposta:** (1) destruir Host imediatamente ao
primeiro erro de bootstrap ou no início de stop — perde retry authority e pode
abandonar scopes; (2) gate único que rejeita também Leave/release — impede
cleanup próprio; (3) migrar shutdown/Route/Activity truth para um Recovery
Registry global — duplica ownership; (4) tratar Route como atomicamente
restaurável sem evidência/contrato — falseia o resultado; (5) promover readiness,
Transition Gate, loading ou Actor/Player comportamentos Experimentais a
Stable; (6) alterar intenção/schema dos Assets para resolver teardown.

#### Separação dos achados

- **Defeitos comprovados estaticamente:** `ReleasePersistentContentScope` é
  result-less e os callbacks de quit/destroy prosseguem com descarte de módulos
  depois de release rejeitado; `BootAfterSceneLoad` retorna após `StartAsync`
  falho sem stop explícito e destrói diretamente no attach de Provisioning
  falho; Host route/activity ingress não consulta fase de start/stop além da
  nulidade do GameFlow. São falhas de protocolo/coordenação observadas no
  código, não prova de impacto Stable em todos os cenários.
- **Riscos estruturais ainda sem reprodução integrada:** falha Route depois de
  Activity exit/content release pode deixar Route anterior selecionada com
  Activity/bindings/cenas parciais; corrida entre callback de shutdown e
  transição async pode descartar dependências; triggers podem estar vinculados
  durante bootstrap; efeitos externos podem falhar mesmo com Gate terminal.
- **Lacunas de evidência:** não há fixture nomeada que percorra full Host
  bootstrap → comandos → transição → shutdown/retry; não está demonstrada
  matriz Route pre/post-commit, falhas durante cleanup, terminal Gate com
  blockers, pending async/readiness cancellation, Session/Player/Camera/Input
  cleanup integrado ou schema round-trip no candidate SHA. Resultados QA-NEW-002,
  QA-IF-ADR-036 e contagens históricas não certificam este candidato.
- **Limitações legítimas:** comportamentos Experimentais de readiness, visual
  transition, projections e Actor lifecycle não viram requisito Stable sem
  causalidade demonstrada. Destruição real do processo Unity é best-effort e
  não pode garantir retry depois do encerramento.

#### Mudanças candidatas, testes e critérios

- **Normativa mínima candidata:** emenda a IF-ADR-001 para definir admission
  interna após bootstrap completo, stop controlado result-bearing, retenção do
  Host/owners em falha e distinção entre comandos novos e cleanup/retry
  permitidos. A alocação com a proposta OP-P0-PC deve ser consolidada numa só
  emenda, não duplicada. IF-ADR-040 é dependência da proposta de ledger de
  participantes em OP-P0-PC. Nenhuma emenda a IF-ADR-036/046/047 é proposta.
  Só propor emenda Route/Activity a ADR proprietário se a revisão aprovar nova
  garantia além dos resultados atuais; primeiro verificar IF-ADR-006/007/011 e
  a superfície Stable de IF-ADR-048.
- **Runtime candidato, condicional a evidência/testes:** operação interna de
  stop no `FrameworkRuntimeHost`, retorno agregado e guards de admission nos
  ingressos existentes; bootstrap encaminha toda falha pós-criação para stop;
  teardown retém o mesmo owner/receipts se release falhar. Alterar Route ou
  Activity transaction somente se matriz demonstrar perda de autoridade,
  resultado falso ou impossibilidade de retry. Não adicionar API pública,
  máquina pública, registry de recuperação nem alterar serialized assets.
- **Arquivos prováveis:** `Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs`
  e partials que efetivamente fazem admission/teardown;
  `Runtime/Bootstrap/ImmersiveFrameworkBootstrap.cs`;
  `Runtime/GameFlow/GameFlowRuntime.cs` e `GameFlowRuntime.TransitionFailureAuthority.cs`;
  `Runtime/RouteLifecycle/RouteLifecycleRuntime.cs` e
  `RouteLifecycleRuntime.ActivityActivation.cs`;
  `Runtime/ActivityFlow/ActivityFlowRuntime.Transaction.cs`,
  `ActivityTransitionRuntimeTransaction.cs`;
  `Runtime/SceneLifecycle/SceneLifecycleRuntime.cs` (coordenado com OP-P0);
  Authoring assets/validator somente se schema/invalidez for reproduzida;
  testes Editor atuais ou novas fixtures focadas em Application/Host e QAFramework
  apenas para composição Play Mode e restauração de baseline.
- **Matriz de futura evidência** (nenhum teste executado nesta análise):

| Grupo | Casos mínimos | Resultado observável exigido |
|---|---|---|
| Bootstrap positivo/negativo | Config válida; falha em validação, scene load, Global UI, Output, Session composition, ownership commit, cada attach posterior e GameFlow startup | Estado operacional só após composição obrigatória; erro aponta fase/owner; roots/Session scope e bindings correspondem ao commit; falha pós-composição chega ao stop. |
| Admission | Route/Activity/Join/Availability/Camera/Pause antes de pronto, durante stopping e após stop falho | Novas mutações rejeitadas; limpeza/retry da ocorrência exata aceita; nenhum bypass via trigger/scoped access. |
| Activity transação | Falha antes do commit em cada prepare/scene/binding; falha pós-commit em Exit, content participant, scene release, readiness; retry | Autoridade anterior preservada pre-commit; destino autoritativo pós-commit; resultado/snapshot truthful; receipts pendentes retidos e retry idempotente. |
| Route transação | Falha em Activity clear, Route content release, scene unload/load, resettable, spatial entry e startup Activity antes/depois de commit; supersession/cancelamento | Revisão decide e testa o resultado de autoridade parcial/rollback; não há falsa indicação de Route/Activity ativa nem perda silenciosa de escopos. |
| Gate/transição | falha/throw em fases Before/After, rejeição gate, Gate terminal em startup/Route/Activity e stopping durante request async | Gate não fica ativo em memória; effects/release failures visíveis separadamente; sem concorrência que descarte owner; regras IF-ADR-036 preservadas. |
| Teardown integrado | Activity exit → Player Leave → Camera → Pause/Input → Availability → Scene participants → Persistent roots → Host; rejeição persistente e recuperação; cleanup repetido; quit/destroy terminal | Dependências vivas até seus releases confirmarem; identidade/occurrence/token/receipt exatos; falha deixa Host inerte e retryável durante execução; sucesso agregado libera tudo; quit terminal reporta best-effort sem declarar retry. |
| Schema/QA | round-trip de GameApplication/Route/Activity, enums/refs inválidos e defaults; composições QA declaradas | Nenhuma perda de intenção ou promoção experimental; evidência ligada ao SHA candidato; QA restaura baseline e limpa todos os objetos/scenes/bindings. |

- **Dependências e critérios de aprovação do desenho:** revisar em conjunto
  OP-P0-PC (IF-ADR-001/040 e owner ledger), OP-P1-JOIN-AVAIL e
  OP-X-PROVISIONING (admission, Session/Host), OP-P1-LEAVE-ACTOR (ocorrências,
  Host físico e terminalização), OP-P1-CAMERA (Output e binding lifetime),
  OP-P1-PAUSE-INPUT-GATES (restauração física e cleanup autorizado), Scene
  Lifecycle, GameFlow/Observer boundary e QA. Aprovar somente quando: (i) fase
  operacional e gate forem únicos e internos; (ii) cada comando tiver decisão
  Starting/Operational/Stopping; (iii) matriz de teardown identificar ordem e
  dependências com owner; (iv) pre/post-commit Route/Activity disserem a verdade
  de autoridade; (v) cada falha conservar owner/receipt/retry possível; (vi)
  schema/intenção Stable e maturidades aceitas permanecerem intactos.
- **Critérios objetivos para futura certificação:** implementar apenas após a
  revisão aprovar desenho e emendas; executar testes focados e QA integrado na
  revisão candidata imutável; provar bootstrap completo e cada falha nomeada,
  Gate terminal, rejection/retry de cleanup, restauração de baseline sem objetos,
  roots, Scenes, binding ou tokens órfãos; verificar schema serializado e
  resultados contra autoridade atual; registrar SHA, comandos/evidência e
  limites. Até lá OP-P2 permanece aberto, Fase 2 em progresso e release NO-GO.

### OP-P2-SAVE — Progression Save Stable contract

- **Status / contract boundary:** Analysis recorded; implementation and
  certification remain open. The Stable contract is the 13-type set listed in
  IF-ADR-018 §6 and Public-API: `IProgressionSaveStore` plus
  `ProgressionSaveBackendId`, `ProgressionSaveRecordId`,
  `ProgressionSaveSlotId`, `ProgressionSavePayload`,
  `ProgressionSavePayloadFormat`, `ProgressionSaveSlotRecord`,
  `ProgressionSaveReadStatus`, `ProgressionSaveReadResult`,
  `ProgressionSaveWriteStatus`, `ProgressionSaveWriteResult`,
  `ProgressionSaveDeleteStatus` and `ProgressionSaveDeleteResult`.
  `ProgressionSaveRuntime`, request/moment types, profile/provider/composition,
  catalog/manifest and `JsonProgressionSaveStore` remain Experimental or
  internal as declared by their own attributes and API records. Stable Save is
  a backend port, not a gameplay-save product workflow.
- **Authorities / owners:** `IProgressionSaveStore` owns the synchronous
  `BackendId`, `ReadSlot`, `WriteSlot` and `DeleteSlot` boundary. An adapter owns
  mapping to its storage and must preserve typed identity, bytes/format and
  result meaning. `ProgressionSaveRuntime` is an Experimental explicit request
  adapter over the injected store. ADR018-C's Experimental profile/provider
  composition resolves the selected store and runtime at Game Application
  scope; `FrameworkRuntimeHost` retains its composition result. No current
  Stable Session, Actor, Activity or GameFlow authority owns or triggers Save.
- **Actual Stable semantics:** Backend, slot and record IDs are distinct typed
  identities in the ProgressionSave domain; slot is a logical address, record
  identifies stored content, and backend identifies an adapter, never a path
  or implementation type. Payload format is `Empty`, `Binary`, `Text` or
  `Structured`; bytes and media type are opaque to the backend-neutral
  contract. `Empty` carries no bytes; other formats require bytes. Text helper
  encodes UTF-8. Slot records require valid slot/record IDs, a valid payload,
  valid UTC ticks and `UpdatedUtcTicks >= CreatedUtcTicks`; descriptive text is
  normalized. Read distinguishes `Found`, `Missing`, `Rejected`, `Corrupt`,
  `BackendUnavailable` and `Failed`; `Found` must carry a valid record for the
  requested slot. Write distinguishes `Written`, `Rejected`,
  `BackendUnavailable` and `Failed`; only `Written` carries a record ID.
  Delete distinguishes `Deleted`, `Missing`, `Rejected`,
  `BackendUnavailable` and `Failed`. Delete of an absent slot is completed;
  read of an absent slot is a completed `Missing`. Invalid/unknown statuses,
  invalid IDs, mismatched record slot and malformed payload construction are
  rejected by value constructors rather than represented as storage results.
- **Guarantees not made by Stable:** The port is synchronous and defines no
  async operation, cancellation, operation receipt, retry policy, idempotency
  key, write atomicity, cross-process serialization, compare-and-swap,
  concurrency order or resource disposal protocol. `Failed` is distinct from
  `Rejected`, but does not expose a separate “commit outcome unknown/partial”
  status. A backend must return truthful typed outcomes and preserve data; the
  interface alone does not promise that retrying a failed write is safe.
  Repeating delete against an already absent slot can return `Missing`, but
  this does not establish ordering guarantees against concurrent writes.
- **Backend-specific boundary:** IF-ADR-018 §7–9 assigns local JSON durability
  to the Experimental built-in backend: explicit commit intent, staging,
  validation, recovery before operations, diagnostics and no silent
  substitution. JSON's transaction replay/idempotence and single-process I/O
  serialization are not general adapter guarantees. Cloud sync, encryption,
  anti-tamper, compression, platform-native storage and conflict resolution
  remain optional backend capabilities. Catalog/manifest is optional and
  outside the Stable port. No Snapshot, gameplay capture, autosave, world
  serialization, Save scheduling or lifecycle trigger is added here.
- **Composition and shutdown:** Host creation resolves the selected
  Experimental composition in `FrameworkRuntimeHost.FrameRate.TryCreateWithProjectFrameRate`
  before `StartAsync`; a selected-backend failure aborts creation explicitly,
  with no fallback. `ProgressionSaveRuntime.Request` and store calls are
  synchronous, and the Host exposes only an internal runtime accessor; no
  Stable operation queue or in-flight ledger exists. Host shutdown currently
  does not dispose the stored Save composition, and neither store nor runtime
  has a disposal contract. This is not a demonstrated loss for the current
  synchronous JSON path, which opens operations internally, but it is an
  ownership gap for a custom provider that retains SDK/session resources. No
  current code establishes Save draining or rejection once the Host starts
  stopping. The cross-workpackage candidate is: the Host may reject new
  Host-scoped Save commands after stopping begins, let an already executing
  synchronous call return, then release the Experimental composition using a
  provider-specific lifetime contract if a real backend requires it. Do not
  add shutdown semantics or disposal to the Stable store absent a concrete
  backend need. Direct custom use of public `Resolve` remains caller-owned.
- **Defect proven by source:** `ProgressionSavePayload` copies incoming bytes
  in its constructor, but `Bytes` returns the private array as
  `IReadOnlyList<byte>`. A caller can cast the returned object to `byte[]` (or
  mutable list interface) and mutate a value documented and governed as
  immutable. This can change payload equality/hash and data after construction
  or after a record/result captured it. Candidate Runtime correction is to
  expose a non-mutable view or defensive copy while preserving the Stable
  member signature; test source-array isolation, getter mutation resistance,
  stable equality/hash and record/result round trip. This aligns with existing
  immutable contract text and does not require changing IF-GOV-001.
- **Governance divergence:** IF-ADR-018 §6, `Public-API.md` and all 13 current
  type attributes identify the core port/value set as Stable. IF-ADR-018 §11
  still says “ProgressionSaveRuntime and contracts EXPERIMENTAL pending
  stabilization”, an unqualified statement that contradicts the accepted
  Stable subset; the adapter guide labels itself Experimental but explicitly
  says the Stable core is certified. Propose a narrow future editorial
  amendment to §11 clarifying that only runtime/request/composition and
  concrete backend surfaces are Experimental and referring to §6 for the
  Stable 13-type set. Do not change the Accepted ADR in this analysis.
- **Structural risks:** Store result structs validate shape but cannot prove
  backend truthfulness or durability. A custom adapter could return `Written`
  before durable commit, collapse `Corrupt` into `Missing`, or expose an
  ambiguous `Failed` after remote commit. Current Stable API has no safe
  resolution protocol for that ambiguity. Synchronous vendor calls can also
  block shutdown. These are adapter conformance/lifetime risks, not proven
  defects in the Stable result constructors.
- **Evidence gaps:** Framework `Tests` has no named Progression Save test
  suite. Current static QAFramework search found no Save runtime QA; inspected
  Application fixtures leave Save disabled. FirstGame contains a future
  Persistence sample reference in planning documentation, not an active Save
  consumer. IF-ADR-018's historical B5 18/18 proves its recorded JSON
  interruption/recovery candidate only; ADR018-A contract suites are also
  historical. Neither is linked here to the current source SHA or a current
  candidate and neither certifies the present payload getter. No tests were
  executed for this assessment.
- **Decisions proposed for transversal review:** (1) accept the payload
  immutability correction as required Stable remediation; (2) retain the
  Stable port synchronous and backend-neutral, without adding global atomicity,
  retry or disposal claims; (3) state that `Written`/`Deleted` must mean the
  adapter's claimed operation completed and `Failed` must not conceal known
  partial state, while leaving commit ambiguity/recovery policy to each
  backend unless a consumer requires a Stable protocol; (4) resolve Host
  stopping behavior and custom-provider lifetime in OP-P0/Application; (5)
  approve or reject the narrow IF-ADR-018 §11 emendation; (6) decide under
  IF-GOV-001 whether missing current-SHA conformance evidence blocks 1.1.0
  certification. Rejected alternatives: promote the whole Save feature because
  its port is Stable; add Session/Actor/GameFlow capture or autosave; require
  cloud/JSON implementation details of every adapter; claim B5 as current
  candidate certification; or broaden Stable with async/disposal/transaction
  APIs without demonstrated need.
- **Candidate changes (future only):** minimal Runtime fix to payload exposure;
  a focused NUnit payload immutability/value suite and a backend-neutral
  `IProgressionSaveStore` contract suite. Retain separate JSON fault-injection
  tests for commit/recovery and fail-closed behavior; do not fold those into
  Stable conformance. If Host disposal is required for an actual custom
  provider, define its Experimental composition/provider lifetime and shutdown
  owner in the Application workpackage before adding code. No other Runtime
  change is justified by this static inspection.
- **Test matrix for a future candidate:**

  | Class | Required proof |
  |---|---|
  | Positive | `ReadSlot` Found round-trips exact slot/record IDs, format, media type, bytes and record metadata; Missing is distinct; `WriteSlot` Written returns same slot/record identity; `DeleteSlot` Deleted then Missing. |
  | Negative | Invalid ID/status/payload/ticks rejected at value boundary; wrong-slot record cannot be reported Found; each Rejected, Corrupt, BackendUnavailable and Failed path retains requested identity and does not become success/Missing. |
  | Rollback / retry | Inject failures before and after adapter commit; verify truthful result and observed persisted state; retry only according to adapter's declared policy. JSON candidate separately proves staging discard, committed intent replay, fail-closed corruption, no residue, and repeated recovery. Do not infer general idempotency from JSON replay. |
  | Cleanup / lifecycle | Disabled Save composes no runtime; invalid selected provider aborts bootstrap without fallback; successful composition is application-scoped; stopping rejects new Host-scoped requests if approved; synchronous in-flight call reaches a terminal result before composition cleanup; resourceful provider release/retry only if its explicit Experimental lifetime contract requires it. |
  | Value integrity | Constructor input mutation, `Bytes` exposure, `ToByteArray`, equality/hash, record/result copies, enum boundaries, identity domain/equality and text normalization. |

- **Dependencies:** IF-ADR-018 and IF-GOV-001; IF-ADR-047/048 release gates;
  typed identity contract/workpackage; OP-P0 Host bootstrap/shutdown and
  OP-P2 Application/GameFlow for composition lifetime. Join/Availability,
  Provisioning, Leave/Actor, Camera, Pause/Input/Gates and Persistent Content
  have no direct Save contract dependency; their teardown ordering matters only
  if future application shutdown invokes Save or disposes its provider.
- **Files / owners likely in a future authorized cut:**
  `Runtime/ProgressionSave/ProgressionSavePayload.cs` (Framework Runtime);
  focused `Tests/Editor/ProgressionSave/*` (Framework NUnit);
  QAFramework Save bootstrap/backend fixture only if composition/lifecycle needs
  an integrated Unity proof; current plan master remains the only file changed
  by this analysis. IF-ADR-018 §11 is a governance-owned decision file and is
  not edited here.
- **Objective future approval / certification criteria:** reconcile all 13
  Stable attributes against §6, Public-API and IF-GOV-001; establish the
  accepted normative reading of §11; prevent external mutation of payload
  bytes; pass current-revision deterministic conformance tests for every Stable
  method/status and exact identity/payload preservation; demonstrate honest
  failure behavior without claiming unpromised atomicity/idempotency; exercise
  JSON recovery separately on the candidate if the built-in backend is in
  release scope; link raw test/QA evidence to immutable candidate SHA; resolve
  Host/provider shutdown ownership; record unsupported capabilities and keep
  Session/gameplay/Snapshot/autosave outside the Stable claim. Until those
  gates and transversal decisions are satisfied, OP-P2-SAVE remains open,
  this plan remains Proposed, Fase 2 remains open and release remains NO-GO.

### OP-P2-IDENTITY-METADATA — Typed identities and governance metadata

- **Status / scope:** Analysis recorded; no API expansion or certification.
  This workpackage covers the Stable contracts `IFrameworkIdentity`,
  `FrameworkIdentityDomain`, `FrameworkIdentityKey`,
  `FrameworkIdentityValue`, `RouteId`, `ActivityId`, `PlayerSlotId`,
  `FrameworkApiStatus` and `FrameworkApiStatusAttribute`. The operational
  semantics of Session/Actor selection, Camera Assignment, occurrences and
  lifecycle owners remain with their feature contracts; this review does not
  promote them.
- **Authorities:** Concrete typed wrappers own their fixed identity domain and
  value validation. `FrameworkIdentityKey` is the generic, domain-qualified
  bridge and owns equality across erased identities. Exact Route/Activity
  definition authority remains the `RouteAsset`/`ActivityAsset` reference;
  stable IDs project those definitions across persistence/diagnostic
  boundaries, while occurrence and cleanup authority require the accepted
  definition/token model in IF-ADR-014. Player participation uses
  `PlayerSlotId` for the logical seat; Camera mappings consume that typed ID
  without replacing Session/Player occurrence identity. `FrameworkApiStatus`
  defines the canonical vocabulary; the attribute is source-level metadata,
  not a runtime authority or feature-promotion mechanism. IF-GOV-001 owns
  maturity interpretation; owning feature ADRs delimit contract scope.
- **Behavior confirmed in code:** `FrameworkIdentityValue` rejects null,
  empty and whitespace-only input, trims surrounding whitespace via
  `NormalizeText`, retains case, and compares/hashes with ordinal string
  semantics. Its default value is invalid and reads as empty text. RouteId and
  ActivityId additionally require lowercase ASCII alphanumeric segments
  separated by single dots/hyphens; they preserve that canonical text and
  throw on invalid construction. PlayerSlotId rejects an invalid/empty value,
  has fixed PlayerSlot domain and canonical Player1–Player4 convenience
  values; authored `PlayerSlotProfile` trims its serialized string before
  constructing the typed value. It intentionally does not use Route/Activity's
  restricted grammar. All four identity values/wrappers are readonly value
  types with ordinal value equality/hash; RouteId and ActivityId have distinct
  CLR types and fixed Route/Activity domains.
- **Key, domain and invalid-value invariants:** `FrameworkIdentityKey`
  construction rejects `Unspecified`, undefined enum values and invalid
  values; equality/hash use exactly `(Domain, Value)`. Thus equal text in Route,
  Activity and PlayerSlot domains remains distinct. Default key/value/wrapper
  values are invalid and must not authorize lookup, lifecycle release or
  binding. `FrameworkIdentityKey.From(IFrameworkIdentity)` rejects null and
  then revalidates domain/value. No parser exists for `StableText`; it is a
  representation/diagnostic, not an identity input format. Do not recreate
  identities by parsing it. `IFrameworkIdentity` itself is intentionally
  minimal and third-party implementations can report their own domain; key
  construction guarantees enum/value validity but cannot prove that a custom
  implementation's declared domain matches its nominal type.
- **Serialization/reference behavior:** `GameApplicationAsset` has no
  dedicated ApplicationId Stable identity; its serialized Route/Profile
  references are Unity object references and `ApplicationName` is presentation
  and diagnostics text. `RouteAsset.routeId` and `ActivityAsset.activityId`
  are serialized strings and typed accessors validate without regenerating or
  rewriting them. IF-ADR-014 states that equal RouteId/ActivityId values do
  not merge two distinct authored assets: reference identity is definition
  authority and token/occurrence remains operational authority. `PlayerSlotProfile`
  serializes the seat identity text; Camera Session mapping stores/uses
  `PlayerSlotId` as its logical key, with actual Output/Assignment reference
  authority governed by the Camera workpackage. No new generic serialized
  identity field or pre-1.1.0 compatibility path is justified.
- **Maturity metadata confirmed:** `FrameworkApiStatus` is a Stable enum with
  the six canonical values (`Stable=0`, `Experimental=1`, `Internal=2`,
  `Deferred=3`, `DevelopmentTooling=4`, `Removed=5`). The attribute is Stable,
  non-inherited, single-use and allowed on class/struct/interface/enum/delegate;
  its read-only `Status` and normalized `Note` do not affect execution.
  Public-API lists the nine audited contracts as Stable; current source
  attributes agree, and IF-GOV-001 defines the same maturity/compatibility
  meanings. Stable metadata does not make a dependent workflow Stable.
- **Defect assessment:** No proved equality/hash/domain or serialization
  corruption was found in the audited identity wrappers. The constructor
  invariants prevent a constructed key with an invalid/unknown domain. No
  current malformed status attribute was found in the audited surfaces. One
  source-level acceptance gap remains: `FrameworkApiStatusAttribute` does not
  validate its `status` argument, so a caller can apply a cast undefined enum
  value and create malformed governance metadata. No current declaration does
  so; record as a low-priority contract-hardening decision, not as a current
  bad metadata instance. No missing ApplicationId is a Stable defect: the
  accepted authoring contract uses the Game Application asset and its explicit
  references, and does not declare such an ID.
- **Structural risks / questions:**
  1. `FrameworkIdentityKey` is intentionally coarse `(Domain, Value)`. Distinct
     nominal identities assigned the same coarse domain can compare equal
     after erasure; for example Progression Save backend/slot/record IDs share
     `ProgressionSave`. This is not a current defect where consumers retain
     their nominal types, but callers must not use the generic key to replace
     those types. Preserve this distinction in OP-P2-SAVE conformance and do
     not add subdomains without a demonstrated collision in an authorized
     consumer.
  2. RouteId/ActivityId `StableText` yields the raw canonical value, while
     PlayerSlotId and FrameworkIdentityKey `StableText` include a domain
     prefix. Equality/conversion remain correct, and no identity parser exists,
     but the shared property name has differing textual shape. Do not
     normalize public representations speculatively; test and document that
     they are not interchangeable parse formats if the transversal review
     finds consumer ambiguity.
  3. `RuntimeContentOwner.Session` is Experimental but
     `FrameworkRuntimeHost` constructs it with `application.ApplicationName`
     as both Session identity text and owner name. The Stable GameApplication
     asset exposes no functional ApplicationId and names are mutable/non-unique.
     This is a confirmed cross-layer use, not proof of a Stable identity
     violation or current collision (the Host owns one application scope).
     OP-P0/Application review must decide whether Session-scope owner identity
     is merely application-lifetime-local or needs another existing exact
     authority; do not add an ApplicationId in this workpackage.
  4. Invalid defaults are consistently marked by `IsValid == false`, but
     string projection behavior is not uniform: RouteId/ActivityId guard
     `StableText` and return empty, while key-based wrappers can throw while
     constructing a key from default invalid value. Callers should validate
     before formatting; the Stable contract does not currently define one
     universal invalid-value formatting result. Verify this is harmless at
     call sites before any behavior change.
- **Cross-cutting immutability:** Identity/key/status structs and metadata
  properties inspected here are readonly and retain immutable string/enum
  values. This supports the Stable value invariant. The independently proven
  mutable backing-array exposure in `ProgressionSavePayload.Bytes` is already
  recorded under OP-P2-SAVE; keep its single remediation and tests there rather
  than duplicate a second identity-package fix. Include payload immutability
  in the overall Stable value conformance gate.
- **Coverage and evidence:** Current tests exercise identity indirectly in
  `StableObjectBindingTests`, `PlayerSessionJoinBindingTests`, Camera
  Assignment/Output integration and `PauseProductTransactionTests` (which
  protects the removed InputMode domain). `LocalPlayerJoinOutcomeProjectionTests`
  checks one Stable API attribute. Static search found no focused NUnit suite
  directly covering FrameworkIdentityValue/Key equality/hash, all wrapper
  constructors/invalid defaults, domain collision behavior, serialized
  Route/Activity/Player identity round trips, or complete attribute vocabulary
  and status inventory. Existing 107-type inventory evidence is recorded in
  IF-TRACK but is not a substitute for candidate-SHA test evidence. No tests
  were executed for this analysis.
- **Decisions proposed:** Preserve the existing seven identity contracts and
  six maturity values without new abstractions or baseline-backcompat rails.
  Require `Domain + Value` for erased comparisons and preserve exact nominal
  wrappers at public feature boundaries. Treat two same-ID Route/Activity
  assets as different definitions; never use stable ID as lifecycle/token
  authority. Decide whether invalid enum status should fail at attribute
  construction or be rejected by the authoritative maturity inventory check;
  choose one owner and avoid duplicated validation systems. Decide whether
  StableText raw-vs-domain-qualified formatting and invalid-default behavior
  are intentional API shape or need explicit documentation. Resolve the
  ApplicationName → Session owner derivation with OP-P0/Application. Rejected:
  introduce ApplicationId, per-type new identity domains, general parser,
  string compatibility aliases or migration for pre-baseline assets without
  concrete Stable breakage evidence.
- **Candidate Runtime changes:** None is justified from the audited core
  identity implementation alone. If the transversal review accepts fail-fast
  metadata validation, a minimal `FrameworkApiStatusAttribute` argument check
  may be proposed as a separate authorized cut. The already registered
  ProgressionSave payload correction remains owned by OP-P2-SAVE. No changes
  were made to Runtime, tests or QA.
- **Future NUnit matrix:**

  | Class | Required proof |
  |---|---|
  | Value/equality | null/empty/whitespace rejection; trim/case behavior; ordinal equal/hash consistency; default invalid values; Route/Activity grammar edges; PlayerSlot authored normalization and predefined IDs. |
  | Domain/conversion | same text across Route/Activity/PlayerSlot yields distinct keys; each wrapper converts to exact `(fixed domain, original value)`; null/custom invalid `IFrameworkIdentity` rejection; unknown/Unspecified domain rejection; generic-key collisions are tested for nominal types intentionally sharing one domain and are not used as interchangeable keys. |
  | Serialization | persist/reload GameApplication object references, RouteId/ActivityId strings and PlayerSlotProfile identity; verify exact configured intent is retained, invalid IDs fail validation without silent regeneration, and two assets with a colliding stable ID remain different exact definitions/tokens. Include PlayerSlot round-trip through Camera Slot→Output mapping without converting to a string identity. |
  | Metadata | all six enum values and meanings; allowed attribute target/single-use/non-inherited behavior; every in-scope Stable type agrees across attribute, Public-API and owning ADR; malformed enum status is rejected by the selected governance owner; metadata does not change runtime authority. |
  | Cross-layer | GameApplicationName remains presentation except for the existing Experimental Session owner path; if that path is revised in a future approved cut, prove separate application scopes do not collide and cleanup retains exact owner authority. |

- **Dependencies:** IF-ADR-014 for authored definition/stable ID/occurrence
  separation; IF-GOV-001, IF-ADR-047/048 and Public-API for maturity and
  baseline compatibility; OP-P2-SAVE for same-domain nominal IDs and the
  existing payload immutability finding; OP-P0 and OP-P2-APP for the
  ApplicationName/Session owner relationship; Join/Availability and
  Leave/Actor for PlayerSlot authority; Camera for typed Slot mapping and
  separate Camera identity maturity; Persistent Content for Session/Route/
  Activity owner identity and tokens. These dependencies do not reopen accepted
  decisions or promote Experimental behavior.
- **Objective closure criteria:** establish a source-linked inventory for all
  nine audited declarations and their owning contracts; current-SHA NUnit
  evidence proves value, domain, equality/hash, invalid/default, conversion and
  serialized-reference invariants; camera/player integrations preserve typed
  PlayerSlot identity; duplicate authored Route/Activity IDs never merge exact
  definitions; status values and attributes reconcile with IF-GOV-001 and
  Public-API, including a decided malformed-status policy; callers do not
  treat StableText as a parser or use invalid defaults as authority; resolve
  ApplicationName-derived Session identity in cross-workpackage review; link
  raw validation evidence to the candidate SHA. Until then this workpackage is
  analyzed but remains open pending transversal review, the plan remains
  Proposed, Fase 2 remains open and release remains NO-GO.

### OP-X-OBSERVER-COMMAND — Observer projections and command boundaries

- **Status:** Open — divergência de surface comprovada e impactos classificados;
  decisão de API e revisão transversal pendentes. Sem implementação ou
  certificação.
- **Maturidade/autoridade:** `PlayerSessionObserver`,
  `IPlayerSessionScopedAccess`, command triggers restantes e Provisioning são
  Experimentais conforme IF-GOV-001/Public-API. Observer é projection/read-only;
  command triggers são ingressos explícitos; Session/Player, Gameplay runtime
  e Provisioning runtime mantêm a autoridade canônica de cada operação.
  `PlayerSessionJoinTrigger` é o ingresso Stable e delimitado por IF-ADR-046;
  Availability tokens/result e `PlayerGameplayAvailabilityBlockTrigger` têm o
  limite Stable de IF-ADR-044. Nada neste workpackage promove a interface
  Experimental inteira, Observer, Leave, Joining ou Actor APIs.

#### Comportamento atual e divergência comprovada

- [`PlayerSessionObserver`](../../../Runtime/PlayerParticipation/Authoring/PlayerSessionObserver.cs)
  documenta-se como observer read-only e fornece `TryGetObservation`,
  `TryGetSnapshot`, `Changed`, eventos Unity e summaries de presentation. Não é
  owner de Session nem uma segunda fonte de verdade.
- Contudo, Observer herda de
  [`PlayerSessionScopedAccessConsumer`](../../../Runtime/PlayerParticipation/Authoring/PlayerSessionScopedAccessConsumer.cs),
  cuja classe está marcada `FrameworkApiStatus.Internal`, mas possui o método
  C# `public TryGetAccess(out IPlayerSessionScopedAccess, ...)`. A herança torna
  esse accessor chamável em um `PlayerSessionObserver` público; ele retorna a
  referência viva e mutável à
  [`IPlayerSessionScopedAccess`](../../../Runtime/PlayerParticipation/Contracts/IPlayerSessionScopedAccess.cs).
  Essa interface inclui leitura/eventos e mutações: Block/Release Availability,
  Open/Close Joining, Leave, Actor select/default/replace/clear e prepared Actor
  replacement. Isso diverge do contrato read-only descrito pelo Observer. A
  maturidade Internal do tipo base não remove, por si só, a acessibilidade
  pública herdada; a API inventory atual também não lista `TryGetAccess` como
  superfície de Observer. A mesma herança passa por
  `PlayerSessionCommandTriggerBase`; portanto os command triggers e o
  `PlayerGameplayAvailabilityBlockTrigger` Stable também expõem a capability
  broad herdada. `PlayerSessionJoinTrigger` é a exceção, pois usa `MonoBehaviour`
  e um port separado.
- `PlayerSessionScopedAccessRuntimeHostModule.PlayerSessionScopedAccess` é a
  implementação concreta; captura Host, contexto canônico de Session, scope,
  `RuntimeContentOwner`/occurrence e consumer. Cada operação valida
  `TryGetContext`/`IsCurrent` e delega a owners existentes. O accessor não cria
  autoridade global nem oferece `RequestJoin`, mas entrega uma capability para
  comandos do scope enquanto a binding for corrente. O teste de currentness
  verifica disposed/host/context/scope; não há consulta a uma fase
  operacional/stopping futura no access atual.
- Os command triggers de Open/Close Joining, Leave, Actor Selection e Stable
  Availability também usam `TryGetAccess` internamente, mas expõem comandos
  específicos, resultados e configuração próprios. `PlayerSessionJoinTrigger`
  usa um binding diferente (`IPlayerSessionJoinPort`) e sua operação não existe
  em `IPlayerSessionScopedAccess`. Não foi encontrada uma segunda implementação
  de Join por Observer/scoped access.
- `LocalPlayerProvisioningAuthoring` ainda expõe publicamente Experimental
  OpenJoining, CloseJoining e RequestLeave, enquanto command triggers e
  `IPlayerSessionScopedAccess` oferecem entradas para as mesmas autoridades.
  Isso é duplicação de ingressos, não duplicação de Session authority.
  No scan bounded de Framework Runtime/Tests, QAFramework e sample consumidor,
  não apareceu chamada direta aos três métodos do componente; os consumidores
  encontrados usam triggers ou scoped access. A ausência nessa busca não prova
  ausência de dependência externa.

#### Consumidores encontrados e requisitos

- **Sample FirstGame / `LocalMultiplayerJoinTutorialController`:** obtém o
  acesso pelo Observer para `Changed`, `Snapshot`/`TryGetObservation` e
  reconciliação de leitura. Open/Close Joining e Join são executados por
  `PlayerSessionOpenJoiningCommandTrigger`,
  `PlayerSessionCloseJoiningCommandTrigger` e Stable
  `PlayerSessionJoinTrigger`, respectivamente. Este consumidor não requer
  mutações pelo Observer; suas leituras já estão cobertas pela superfície
  read-only pública do componente.
- **QA-IF-ADR-044 `QaIfAdr044ConsumerGameplayAvailabilityScenario`:** captura
  `IPlayerSessionScopedAccess` por `PlayerSessionObserver` para Open Joining,
  dois Availability blocks independentes, releases por token, Leave enquanto
  outro token está ativo e rejeição de token stale; cleanup libera tokens e
  restaura Session. O report 8/8 é evidência histórica do runtime block/release,
  não de um Observer read-only ou de uma Stable-trigger-only closure. A QA
  também declara cobertura de composição do Stable trigger em FirstGame.
- **QA-IF-ADR-046 `QaIfAdr046ScopedBindingScenario` / `QaIfAdr046JoinScenario`:**
  usa o Observer accessor para observar lifecycle de binding e para Open/Close
  Joining e Leave; requisições de Join continuam passando pelo
  `PlayerSessionJoinTrigger` Stable. Portanto o uso mutável do accessor não é
  uma segunda Join surface.
- **QA-IF-ADR-042/043, QA-IF-ADR-036 e QA-IF-ADR-PLAYER-SESSION:** cenários
  adquirem access pelo Observer para operações necessárias à fixture/cleanup,
  incluindo Joining, Leave, prepared Actor replacement e leitura de evidência.
  São consumidores QA concretos que precisam migrar para command triggers
  explícitos ou permanecer num seam de teste aprovado se o accessor deixar de
  ser público. `RequestReplacePreparedActor` não tem trigger público equivalente
  confirmado nesta análise; não remover essa capability antes de decidir seu
  owner/uso Experimental.
- **Authoring de produto:** os componentes trigger já são as superfícies de
  comando mais estreitas; Availability trigger mantém o token do consumer,
  e Join trigger expõe só Join/outcome. Provisioning direct methods continuam
  como alternativa Experimental, sem chamadas ativas encontradas no scan.

#### Impacto nas closures Stable e gate operacional

- **Stable Join / IF-ADR-046:** Observer/scoped access não pode solicitar Join;
  Join permanece ligado ao occurrence-bound `PlayerSessionJoinTrigger` e ao
  backend canônico já analisado. Open/Close Joining, Leave e Actor operations
  através da interface podem afetar precondições/resultados do Join, mas são
  comandos Experimentais distintos; o Join Stable deve continuar reportando o
  resultado canônico sem expor evidência de Session/Slot/Host. Não há evidência
  de quebra direta da closure de Join nem motivo para reabrir IF-ADR-046.
- **Stable Gameplay Availability / IF-ADR-044:** o componente Stable chama os
  mesmos métodos block/release do owner/runtime por sua binding Experimental,
  e mantém seu token próprio para UnityEvent/cleanup. Porém o accessor público
  herdado pelo próprio componente Stable permite que um consumidor obtenha a
  interface broad e invoque Availability múltiplas vezes (e outros comandos)
  sem usar o token que o trigger mantém. É uma exposição C# demonstrável que
  diverge do inventário Stable, que delimita a trigger a um block owner-scoped;
  não é nova autoridade de Runtime nem defeito provado na implementação dos
  tokens. Para preservar a closure Stable publicada na 1.1.0, a decisão
  recomendada é retirar `TryGetAccess` da superfície pública efetiva antes da
  certificação e manter o helper disponível somente aos componentes derivados
  (ex. `protected`). Alternativa de manter acesso público exige governança
  explícita que classifique a capability herdada e reveja a promessa “one
  consumer-owned block”; não pode ser tratada como detalhe Internal sem
  evidência de que consumidores não a podem chamar. Evidência direta de QA por
  `IPlayerSessionScopedAccess` continuará validando a operação Runtime, não a
  ownership boundary da trigger Stable. Não reabrir IF-ADR-044: qualquer ajuste
  fica na acessibilidade da implementação/public API inventory se o ADR owner
  confirmar que o contrato aceito não muda.
- **Future host gate:** se o gate for aplicado somente no código do trigger,
  qualquer consumer que reteve `IPlayerSessionScopedAccess` (ou chama os
  métodos de Provisioning) pode contorná-lo. A leitura/access snapshot não é
  autorização de comando. O gate Starting/Stopping deve ser checado pelos
  owners/portas canônicos usados por todas as entradas, com allowlist explícita
  para releases, Leave e cleanup/retry exatos. Capability já retida deve passar
  pela mesma checagem em cada mutação; fechar/invalidar todas as bindings cedo
  impediria cleanup legítimo. Esta dependência pertence a OP-P0-PC/IF-ADR-001 e
  aos owners Join/Availability/Leave/Provisioning, não requer um novo Observer
  gate/registry.

#### Decisões candidatas, alternativas e necessidade pré-1.1.0

- **Correção de boundary candidata:** manter Observer genuinamente read-only e
  fazer com que nenhum componente de comando/Observation (incluindo a trigger
  Stable de Availability) exponha o `IPlayerSessionScopedAccess` broad por
  herança. Menor corte candidato: tornar `TryGetAccess` `protected` ou usar seam
  equivalente disponível somente à implementação derivada; as APIs específicas
  do trigger continuam públicas e delegam aos mesmos owners. Isso não altera
  autoridade, estado Session nem formato Stable. A mudança afeta consumers que
  hoje chamam o método diretamente, portanto requer versão/callsite inventory e
  migração coordenada, embora o base esteja marcado Internal.
- **Migração dos consumers:** FirstGame já usa comandos estreitos para escrita
  e pode permanecer apenas com `TryGetObservation`/`Changed`. QA deve substituir
  escrita por triggers onde houver equivalente; operações sem trigger (ex. o
  prepared Actor replacement citado) exigem seam de teste/decisão de produto
  antes de ocultar o accessor. Não manter broad access no Observer só para
  simplificar setup QA.
- **Entradas de Provisioning:** consolidar inventário de consumers e escolher
  uma rota canônica documentada por operação. Não remover Open/Close/Leave do
  componente apenas com base no scan limitado; se mantidos, devem delegar aos
  mesmos owners e obedecer ao mesmo gate. Não duplicar Session authority nem
  promover Provisioning para Stable.
- **O que não fazer:** (1) marcar todo `IPlayerSessionScopedAccess` como Stable
  por ser dependência interna de Stable Availability; (2) fundir Join com
  scoped access, adicionando uma segunda Join implementação; (3) transformar
  Observer em façade de escrita para justificar sua herança; (4) criar um
  segundo registry/command router; (5) interpretar cada uso QA Experimental
  como contrato de produto ou reprojetar todas as superfícies Experimentais.
- **Necessidade para Stable 1.1.0:** manter toda a interface, Observer,
  Joining/Leave/Actor selection e Provisioning Experimentais continua correto.
  O leak efetivo pelo `PlayerGameplayAvailabilityBlockTrigger` é diferente de
  redesenhar essas APIs: para sustentar a closure estreita de IF-ADR-044, deve
  ser resolvido ou formalmente avaliado/aceito pela API Governance antes da
  certificação 1.1.0. Recomendação técnica: helper não público, APIs específicas
  preservadas e QA migrada; se governança aceitar a acessibilidade herdada
  como fora do contrato, registrar a decisão e não contá-la como Stable surface.
  Para Observer, a remoção do accessor é alinhamento Experimental desejável e
  pode seguir a mesma correção; FirstGame já usa leituras públicas do Observer.
  O gate operacional central e cleanup/retry continuam dependências separadas
  de shutdown de OP-P0, necessárias à futura certificação integrada.
- **Normativa:** nenhuma emenda a IF-ADR-044/046 é proposta. O gate permanece
  candidato à emenda IF-ADR-001 em OP-P0. Se governança determinar que a
  read-only promise deve ser restrição normativa, registrar ajuste mínimo na
  API inventory/ADR dono da superfície Experimental; não alterar ADR Accepted
  nesta análise.

#### Cobertura futura e critérios de aprovação

**Cobertura atual:** package tests encontrados incluem
`PlayerSessionJoinTriggerTests`, `PlayerSessionJoinBindingTests`,
`LocalPlayerJoinContractTests` e `LocalPlayerJoinOutcomeProjectionTests`.
`PlayerSessionJoinBindingTests` verifica binding de recipient para a Stable
Availability trigger, mas a busca atual não encontrou suite Editor nomeada
para o contrato read-only do Observer, invisibilidade do accessor herdado ou
ciclo de vida do token pela superfície Stable trigger. QA-IF-ADR-044 registra
8/8 runtime availability e a matriz QA acima usa `IPlayerSessionScopedAccess`
diretamente; isso não demonstra ocultação do accessor nem lifetime Stable da
trigger. Os resultados são históricos, sem reexecução nesta análise e sem
associação de execução a um SHA imutável do candidato.

| Grupo | Casos que a futura implementação/certificação deve cobrir | Evidência objetiva |
|---|---|---|
| Surface/authority | Observer fornece leitura/evento do snapshot; tentativa de obter/mutar broad access pelo Observer segundo a decisão aprovada; command triggers delegam ao mesmo owner | API/reflection/compile contract confirma read-only boundary; nenhuma autoridade duplicada; evento/projection não é usado como commit. |
| Stable Join | Request pelo trigger, binding Route/Activity exato, trigger stale, Joined mas Host ainda settling, repeated Join e falha canônica | Somente trigger Stable aceita request; outcome continua bounded e fiel; mutações Experimentais não viram Join alternate path. |
| Stable Availability | Trigger adquire um token, tokens independentes coexistem, release exato, stale/foreign/duplicate, acesso/interface direct sob a maturidade aprovada | Stable trigger mantém ownership explícito e retry; resultado/runtime token concordam; QA via interface não é contado como certificação de lifetime da trigger. |
| Host Starting/Stopping | Chamar via trigger, Observer access já retido e Provisioning direct API antes de pronto/durante stop; permitir só cleanup/retry exato | Todos convergem ao mesmo guard owner; mutações novas rejeitadas; Leave/release permitido conforme allowlist; nenhum port bypass. |
| Teardown/QA consumer | Dispose de consumer/scope com block release rejeitado, Access stale/disposed, retry no Host vivo e restauração de baseline; operações QA sem trigger equivalente | Nenhum token/receipt perdido; consumers recebem resultado/diagnóstico; todas as cenas/bindings/tokens restaurados. |

- **Dependências:** OP-P0-PC/IF-ADR-001 para operação e shutdown gate;
  OP-P1-JOIN-AVAIL/IF-ADR-044 e IF-ADR-046 para fronteiras Stable;
  OP-X-PROVISIONING para ingressos duplicados e manager readiness;
  OP-P1-LEAVE-ACTOR para mutations/cleanup de Session; OP-P1-CAMERA e
  OP-P1-PAUSE-INPUT-GATES para cleanup em Activity e release; OP-P2 Application/
  GameFlow para admission e shutdown orchestration; QAFramework e sample para
  migração de consumers. Manter os limites Experimentais de Actor/readiness.
- **Critérios para aprovar desenho:** decidir public accessibility efetiva de
  `TryGetAccess` em Observer, command triggers e, especialmente, na trigger
  Stable de Availability sem confundir status do base com accessibility C#;
  classificar os consumers de leitura/escrita acima e definir caminho para
  prepared Actor replacement; escolher ingressos documentados para Provisioning;
  posicionar gate em owners canônicos; comprovar que Stable Join segue um único
  port e que Availability trigger continua dona do seu token. Sem isso, não
  iniciar corte de Runtime/API.
- **Critérios objetivos para futura certificação:** após aprovação, compilar
  Framework, testes e consumers migrados na revisão imutável; testar observer
  read-only contract, command delegation, Stable Join/Availability através das
  superfícies Stable, guards de todos os ingressos, rejected release/retry e
  cleanup com `BaselineRestored`; registrar provenance/SHA. Os 8/8 históricos
  de IF-ADR-044, QA-IF-ADR-046 e integrações são evidência delimitada, não
  certificação desta revisão nem autorização de implementação. Workpackage
  permanece aberto; Fase 2 não encerrada e release NO-GO.

### OP-X-PROVISIONING — Player Provisioning dependencies

- **Status:** Open — separate Experimental analysis; it does not merge into
  OP-P1-JOIN-AVAIL or promote its configuration/observation surfaces.
- **Authority/maturity:** `LocalPlayerProvisioningEndpointRegistration` resolves
  the explicit GameApplication-owned UIGlobal authoring reference. The
  `LocalPlayerProvisioningAuthoring` component and `PlayerSessionProfile`/
  `PlayerHostProvisioningMode` configuration are Experimental. The host-scoped
  `LocalPlayerProvisioningRuntimeHostModule`, `LocalPlayerProvisioningBridge`,
  Unity backend and callback ledger are Internal. On successful admission,
  Session—not provisioning authoring—owns the admitted physical Player under
  IF-ADR-019; explicit individual release belongs to IF-ADR-020.
- **Public controls versus authority:** `LocalPlayerProvisioningAuthoring`
  publicly exposes Manager/configuration snapshots plus `OpenJoining`,
  `CloseJoining` and `RequestLeave`, all under the component's Experimental
  status. The same Joining commands and Leave are also exposed through
  Experimental `IPlayerSessionScopedAccess` and command triggers; actor
  selection methods on the authoring component are internal while scoped
  selection/replacement remains Experimental. These are multiple ingress
  surfaces to the same Session/Player authorities, not independent state
  owners. Decide the supported consumer entry surfaces separately from the
  Stable Join trigger.
- **Observed flow:** `FrameworkRuntimeHost.StartAsync` creates Session and
  Player preparation/composition, then returns a started result. Only
  `ImmersiveFrameworkBootstrap.BootAfterSceneLoad` subsequently attaches the
  provisioning module. That module validates one explicit Manager/prefab,
  materializes a missing Manager prefab reference, disables Unity joining,
  creates the bridge and binds the authoring component. Stable Join uses the
  occurrence-bound port to call the same bridge and maps its final canonical
  result; no second Stable Join request implementation was found.
- **Source map:** explicit registration and root resolution are in
  [`LocalPlayerProvisioningEndpointRegistration`](../../../Runtime/PlayerParticipation/Authoring/LocalPlayerProvisioningEndpointRegistration.cs),
  [`FrameworkRuntimeHost.TryResolveLocalPlayerProvisioningAuthoring`](../../../Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs)
  and [`GlobalUiSceneRuntime`](../../../Runtime/GlobalUi/GlobalUiSceneRuntime.cs).
  Startup and direct destruction are in
  [`ImmersiveFrameworkBootstrap`](../../../Runtime/Bootstrap/ImmersiveFrameworkBootstrap.cs);
  initialization/OnDestroy are in
  [`LocalPlayerProvisioningRuntimeHostModule`](../../../Runtime/PlayerParticipation/Runtime/LocalPlayerProvisioningRuntimeHostModule.cs);
  physical release is in
  [`UnityLocalPlayerProvisioningBackend`](../../../Runtime/PlayerParticipation/Runtime/UnityLocalPlayerProvisioningBackend.cs).
- **Failure/lifecycle paths:** `StartAsync` has failures after Persistent
  Content ownership commit and after GameFlow/Player/Camera components begin
  composition. On failure bootstrap logs and returns while retaining the host.
  If Start succeeds but provisioning attachment fails, bootstrap directly
  destroys the host. `TryInitialize` cleans a bridge if authoring bind throws;
  the module's `OnDestroy` disables the Manager, disposes the bridge and
  unbinds authoring. Bridge disposal cancels a pending reservation and requests
  physical release of admitted Manager Players, but `IAdmittedLocalPlayerReleaseBackend.ReleaseAdmittedPlayer`
  and Unity `Destroy` return no result; the admitted-player ledger is cleared
  before those calls. This is not a retryable confirmed-release protocol.
- **Integration risk, not established Join defect:** stable Join lease requires
  an active host and ready Manager, so an absent/unready Manager maps to
  `RejectedUnavailable`. However the Manager is attached after GameFlow
  startup, while scoped access may already exist. Its Experimental
  `OpenJoining`/`CloseJoining` fall back to logical Session mutation when the
  Manager is absent/not ready. Thus an incomplete bootstrap can logically open
  or close Joining without coordinating `PlayerInputManager`; no evidence
  proves a Stable Join succeeds on that path. `TryGetSelectionModule` similarly
  checks a current scoped access and initialized Player Participation module,
  not a host started/stopping phase; public Leave and Actor command triggers
  use the same scoped context. A future OP-P0 inert host would also leave the
  current Join lease/Manager and Session access checks satisfied unless the
  shared host stopping phase is explicitly consulted. Treat actual late-failure
  command acceptance as an integration risk until reproduced, but gate all
  Session/Actor mutations in the approved stop contract.
- **Additional source-level question, not yet a Runtime defect:** profile
  `InitialJoiningOpen` may be true, but provisioning initialization always
  calls `PlayerInputManager.DisableJoining`; later `TryOpenJoining` returns
  `IgnoredNoChange` when the logical Session bit is already true, before
  calling `EnableJoining`. The explicit bridge calls `PlayerInputManager.JoinPlayer`
  directly, so source alone does not prove the technical enable flag is needed
  for this manual flow. Decide whether this flag is intentionally a separate
  Unity automatic-join gate or must mirror logical Joining; certify the chosen
  meaning before changing it.
- **Ingress duplication:** `LocalPlayerProvisioningAuthoring.OpenJoining` /
  `CloseJoining` and `PlayerSessionOpenJoiningCommandTrigger` /
  `PlayerSessionCloseJoiningCommandTrigger` are distinct public Experimental
  call surfaces into the same Session/Manager authority. The known Framework
  call sites use the scoped command triggers (plus QA scripts calling scoped
  access); no direct call to the authoring methods was found in the scanned
  Framework, QAFramework or `planet-devourer` sources. This is duplicate
  ingress, not duplicate Session authority. Decide whether direct authoring
  methods are a supported explicit-reference product surface or unnecessary
  preparation; do not remove them solely from absence in this bounded scan.
- **Consumer/evidence baseline:** QAFramework resolves the package from the
  local Framework checkout and has Player Join/Leave/Rejoin and IF-ADR-044
  scenarios. `planet-devourer` resolves published `1.1.0-preview.8` from
  OpenUPM; its imported `Assets/_Sample/PlayerSamples/LocalMultiplayer`
  composition references Join, Open Joining and Availability triggers. This is
  consumer/sample usage evidence, not proof against the current source SHA.
  Existing package tests cover bridge callback/throw rollback and result
  projection, not all Unity physical release failures or failed-host cleanup.
- **Normative owners proposed:** IF-ADR-003/016 remain owners for Session
  configuration, initial Joining intent and provisioning mode; IF-ADR-001 owns
  host boot/operational/stopping gate (pending transversal review from OP-P0);
  IF-ADR-019/020 own admitted Player lifetime and Leave; IF-ADR-046 owns only
  the bounded Stable Join projection. Amend IF-ADR-016 only if initial logical
  Joining versus Unity Manager enablement is decided to be one synchronized
  contract. Amend IF-ADR-019/020 only if controlled host stop changes Session
  termination/Player release semantics. IF-ADR-046 needs no shape or scope
  amendment now; `RejectedUnavailable` already covers unavailable host/Manager.
- **Decision alternatives:** retain one bridge/backend and exact explicit
  registration; reject global `PlayerInputManager.instance`, second Join
  adapter, registry or provisioning-owned Session authority. Do not make
  provisioning authoring Stable merely because the narrow Join trigger is
  Stable. If authoring ingress remains, it still must delegate to the same
  canonical Session/Manager operation.
- **Candidate implementation/certification sequence:** after IF-ADR-001/040
  and OP-P0 stop semantics are reviewed, decide the Manager/logical Joining
  relationship and public ingress boundary; then add deterministic tests for
  initialization failure after each acquired resource, physical Manager gate
  state, callback correlation, bridge disposal with pending/admitted Players,
  and exact ownership through retry/Leave. Reuse QAFramework Join/Leave and
  consumer LocalMultiplayer scenarios for integrated composition. Do not add a
  new QA runtime or repeat the accepted IF-ADR-046 evidence as if it covered
  shutdown.
- **Required positive, negative, rollback/retry and cleanup evidence:** valid
  Manager setup binds once and manual Join commits the same Session/Host
  identity; missing/mismatched registration, bad prefab, disabled Manager,
  stale occurrence and closed Joining fail at the right boundary; callback
  mismatch/null/throw and Session commit/Host preparation failure preserve the
  mapped result, reservation/Slot truth and exact compensation request. Repeated
  Join after recoverable rollback must not duplicate Host/Slot/device ownership.
  Host-start failure before/after Session composition and provisioning attach
  failure must retain/release exact owners per the approved OP-P0 policy. Any
  physical destruction assertion distinguishes logical result from Unity
  post-settle state and respects IF-ADR-046's accepted residual.
- **Exit:** exactly one canonical Stable Join implementation; Experimental
  authoring/provisioning roles and consumers inventoried; Joining logical vs
  Unity gate semantics decided; host operational state gates all relevant
  mutations; admitted Player release aligns with IF-ADR-019/020 and OP-P0;
  failures/retries neither falsely report Stable success nor silently lose
  recovery ownership. Package and consumer results are linked to an immutable
  candidate before Phase 2 closure.

### Joint review required — Join/Availability × Provisioning × host shutdown

Keep the detailed dispositions above separate. Before implementation, transverse
review must settle this shared contract matrix so a local workpackage does not
invent another authority:

| Operation while host is starting/stopping/inert | Proposed disposition to review | Owner/dependency |
|---|---|---|
| Stable Join / Experimental Open Joining | Reject new admission and logical open when host is not operational; Join uses existing `RejectedUnavailable`. Close Joining may be an allowed cleanup command or host-owned shutdown step; choose one canonical route. | IF-ADR-001 + IF-ADR-046; provisioning readiness from OP-X. |
| Actor selection/replacement | Reject new intent changes while stopping; do not conflate with Session Join or Leave. | IF-ADR-001 + IF-ADR-021/Actor-selection owner + OP-P1-LEAVE-ACTOR. |
| Availability acquire | Reject new blocks once stop begins. | IF-ADR-001 + IF-ADR-044. |
| Availability release/Leave cleanup | Keep only the exact cleanup operation available while its authority is alive; do not close the command gate so early that Session-scope teardown cannot release owned tokens/physical gates. A failed physical release keeps the host/ledger recoverable during controlled operation. | IF-ADR-044 + IF-ADR-005 + OP-P0. |
| Camera/Input/Audio teardown | Release dependent Session bindings before their ports/modules; use their existing feature owners and exact occurrence identity. No cross-module recovery registry. | OP-P0 + OP-P1-CAMERA + OP-P1-PAUSE-INPUT-GATES + Audio participant under IF-ADR-040. |
| Session Player physical lifetime | Decide whether controlled host shutdown means explicit Leave of every joined occurrence or terminal Session termination; do not let GameObject hierarchy destruction silently substitute for IF-ADR-020. | IF-ADR-019/020 + OP-P0 + OP-X. |

The proposed gate therefore needs an operation allowlist/phase contract, not a
single boolean that blocks both new gameplay mutations and release callbacks.
This is a review dependency, not an accepted design or implementation order.

## Transversal review — integrated design and implementation route

The ten individual workpackage analyses are recorded, but none is accepted as
an implementation authorization or certification. The following synthesis is
the proposed decision set for Gate A. Code references below identify static
behavior. The cross-cutting source recheck used Runtime at
`617547a337e52022999c71c30f2e493a3dacbd26`; Runtime files are unchanged in
the current worktree. No failure was reproduced and no test or QA was run for
this plan.

### Consolidated evidence and finding classes

| Finding | Evidence class | Stable disposition proposed |
|---|---|---|
| `SceneLifecycleRuntime.ComposeSessionScope` adds participants to `_sessionParticipants` only after every `OnSceneAvailable` succeeds. A partial Available followed by failed rollback can therefore leave an acquired participant outside the later release snapshot. | Source-demonstrated ownership gap; no current reproduction. `Runtime/SceneLifecycle/SceneLifecycleRuntime.cs`, `ComposeSessionScope`, `NotifyAvailable`. | Required correction and injected-failure/retry tests before Stable candidate certification; the ledger must include every participant that may have acquired resources. |
| `PlayerSessionScopedAccessRuntimeHostModule.RefreshBindings` disposes an active binding before calling the recipient release callback. `PlayerGameplayAvailabilityBlockTrigger.OnScopedAccessReleasing` then tries release through that access and clears `_ownedToken` regardless of release result. | Source-demonstrated release-order/receipt-loss path; no current reproduction. `Runtime/PlayerParticipation/Runtime/PlayerSessionScopedAccessRuntimeHostModule.cs`, `PlayerGameplayAvailabilityBlockTrigger.cs`. | Required Stable Availability correction. Preserve exact token and usable release authority until release succeeds or its exact owner is terminally gone; keep IF-ADR-044 result/token contract unchanged. |
| Availability projection and Pause/Input teardown paths clear local ownership after failed release (`ClearConsumerAvailabilityProjections`, host-module and component teardown). The physical writer can also fail both desired posture and rollback writes without returning a successful new posture receipt. | Source-demonstrated failure branches; no current reproduction. `Runtime/PlayerParticipation/Runtime/PlayerGameplayRuntimeHostModule.ConsumerAvailability.cs`, `PlayerGameplayInputBindingRuntimeContext.cs`, `PauseSessionPlayerInputBindingRuntimeHostModule.cs`, `PlayerPauseInput.cs`, `UnityPlayerInputStateWriter.cs`. | Required only where these branches can affect a live Stable owner before terminal Unity destruction. Retain retry evidence in the existing feature owner; do not add a shared recovery registry or GameplayAction blocker. Test exact residual physical/logical state. |
| `FrameworkRuntimeHost.ReleasePersistentContentScope` logs rejection and returns `void`; `OnDestroy` continues disposing GameFlow, Camera, readiness and Pause resources, while `OnApplicationQuit` also tears down Camera after rejection. | Source-demonstrated best-effort terminal teardown; controlled-stop behavior is absent. `Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs`, `ReleasePersistentContentScope`, `OnDestroy`, `OnApplicationQuit`. | Required internal controlled-stop path before candidate certification. A rejected release keeps this same host inert and retryable with its roots and owners alive. Unity terminal callbacks stay best-effort and are not claimed as retryable. |
| `ImmersiveFrameworkBootstrap.BootAfterSceneLoad` logs/returns on failed `StartAsync` and destroys the Host after provisioning attach failure; it does not use one controlled rollback path for already-composed scopes. | Source-demonstrated divergent failure disposition; no current reproduction. `Runtime/Bootstrap/ImmersiveFrameworkBootstrap.cs`. | Required fail-closed bootstrap handling for resources acquired by the attempt. Rollback uses only their existing owners and must finish before Host destruction; failed rollback retains an inert retryable Host where Unity lifetime permits. |
| `ProgressionSavePayload.Bytes` returns the backing byte array typed as `IReadOnlyList<byte>`, allowing a cast and mutation after construction. | Source-demonstrated Stable value mutability. `Runtime/ProgressionSave/ProgressionSavePayload.cs`. | Required minimal immutability correction and regression test; preserve payload bytes, value equality, factories and all 13 Stable Save types. |
| Stable `PlayerGameplayAvailabilityBlockTrigger` inherits public `TryGetAccess` from the Experimental scoped-access consumer base; the returned interface allows mutation. | Source-demonstrated Stable surface exposure, despite the base capability being Experimental. `Runtime/PlayerParticipation/Authoring/PlayerSessionScopedAccessConsumer.cs`, `PlayerGameplayAvailabilityBlockTrigger.cs`. | Gate A must choose the narrow public boundary: make access retrieval non-public to consumers and migrate any actual supported caller, or explicitly accept/document the inherited capability. No redesign of all Experimental Observer/Command contracts. |
| Host has no single admission phase guarding Route/Activity and Session mutation entry points. Startup and stopping windows are not uniformly closed by `FrameworkRuntimeHost`; no tests currently prove acceptance during those windows. | Structural risk/verification gap, not a reproduced Stable defect. `FrameworkRuntimeHost.StartAsync`, `RequestRouteAsync`, `RequestActivityAsync`, command triggers and bootstrap. | Adopt an internal, owner-local phase guard at canonical mutation ingress and test every supported entry point. Do not add a public Host state API or a second router. |
| Route transitions may leave old Route/Activity resources released after a later target failure. No accepted Stable contract promises a fully atomic Route swap; current results are stateful and must remain truthful. | Contract limitation and recovery question, not a demonstrated defect. `GameFlowRuntime`, Route/Activity lifecycle owners. | Preserve explicit partial outcomes and retry/cleanup authority. Add Runtime recovery only if fault-injection proves a Stable guarantee is false; do not promise blanket Route rollback. |
| Camera candidate validation/commit preserves prior logical Assignment on pre-commit rejection; output materialization can fail after commit. Stable Camera closure is bounded by IF-ADR-039/043; IF-ADR-038 topologies remain Experimental. | Current implementation behavior from the Camera analysis; shutdown ordering is not integrated-certified. | Preserve post-commit destination authority and its retry receipts. Keep Camera and PlayerInput owners alive through exact Player cleanup. Do not expand IF-ADR-038. |
| Leave has occurrence-scoped stage receipts in its live owner; Actor Selection and Leave remain Experimental. Destruction of their host component removes in-memory retry. | Static lifecycle trace; no normal-path pre-confirmation loss demonstrated. | Reuse canonical Leave for shutdown cleanup without promoting the APIs. Keep occurrence, token and stage receipts through failed cleanup. |
| Identity wrappers enforce domain-aware equality; no Stable wrapper defect was demonstrated. Status attributes agree with the current Stable inventory except for an unvalidated undefined enum value that has no current malformed use. | Source review; existing tests are indirect and not a candidate certification. | No new Stable abstraction. Add baseline conformance tests. Treat `ApplicationName` used in `RuntimeContentOwner.Session` as an internal, collision-prone session key risk to decide; do not turn display metadata into a new public identity without approval. |
| Save provider creation is synchronous and has no Stable async/cancel/dispose protocol. No generic provider-resource disposal obligation was found in the Stable interface. | Contract boundary; resourceful providers are Experimental integrations. | Do not add Stable provider lifecycle or async guarantees. Certify the current in-memory/synchronous contract; accept resource-owning provider behavior as integration-owned unless a first-party Stable provider demonstrates a required disposal contract. |

Historical reports (including B5, Join/Availability, Pause, Camera and GameFlow
QA results) remain historical. They are not execution evidence for the candidate
revision. No failure listed above is described as reproduced.

### Proposed architectural decisions and ADR ownership

| Decision for review | Owning authority / amendment proposal | Preserved boundary and rejected alternative |
|---|---|---|
| Give `FrameworkRuntimeHost` internal phases: composing/bootstrap, operational, stopping, inert/retryable, and released. Gate new mutations at their canonical owners. The phases are internal implementation state, not a new public API. | IF-ADR-001: propose a minimal normative amendment describing readiness admission, controlled stop, retained owner on rejected release, and the limit of terminal Unity callbacks. | Keep existing modules as authority. Reject a public lifecycle state machine, global registry, second command router, or one Boolean that blocks release along with new work. |
| During bootstrap, admit only the bootstrap operation and rollback/release for resources it already acquired. During stopping, reject new product intent and allow only explicit host-owned transition/leave/release retries for exact live identities and receipts. | IF-ADR-001; IF-ADR-046 and IF-ADR-044 remain the constraints for Join and availability. No amendments proposed to 046/044. | Preserve IF-ADR-036: structural `GameplayReady` remains independent of Gate/Pause; `RuntimeGameplayAvailable` stays operational; Pause remains rejected during active Transition Gate. No GameplayAction blocker. |
| Stop order: close admission; settle the in-flight GameFlow operation; close Joining; run canonical Leave once per still-joined Session occurrence while Camera, Pause/Input, Availability and PlayerInput owners remain alive; exit remaining Activity then Route through GameFlow; release Session Scene Lifecycle participants and retries; release Persistent Content roots last; dispose/destroy Host only after aggregate success. | IF-ADR-001 for orchestration boundary. IF-ADR-019/020 remain Leave/Session occurrence authorities. IF-ADR-040 owns participant release. Amend 019/020 only if review changes their existing semantics. | Do not implement a parallel Host-level Player teardown or manually duplicate Leave stages. Do not destroy Session/Player hierarchy as a substitute for Leave. Exact ordering inside canonical Leave is an integration-test decision: Camera clear before its subject/Output owner disappears; Pause/Input restoration while PlayerInput is live; exact Availability token release before scoped access is disposed. |
| Any failed cleanup leaves the same Host inert, with dependencies, participants, occurrences, tokens and receipts needed for retry. Retry replays only idempotent releases or skips a stage only with its confirmed receipt. | IF-ADR-001 for host retry; IF-ADR-040 for participant-set/rollback ledger. Propose IF-ADR-040 amendment clarifying that partial Available participants are retained and included in rollback/release snapshots. | Reject discard-on-failure, “log and continue destroying,” or a new cross-module recovery ledger. A participant that succeeded release may receive a repeat during aggregate retry and must tolerate it under the existing lifecycle contract or report a confirmed receipt. |
| Route/Activity failure results remain truthful around commit. Pre-commit failures compensate to prior authority where current contract allows; post-commit failure leaves destination authoritative and retryable. Route-level atomicity is not promised. | Existing Route/Activity owning ADRs; no amendment now. If tests show a violated Stable promise or review elects a new all-or-nothing guarantee, amend the specific Route/Activity ADR before implementation. | Reject blanket rollback that revives already-released old scopes or hides partial state. |
| Camera pre-commit rejection preserves prior Assignment; post-commit output failure preserves destination Assignment plus output retry ownership. Shutdown releases Camera bindings before destroying PlayerInput/Actor subjects. | IF-ADR-039/043 Stable scope; no amendment proposed unless integrated evidence proves their text insufficient. IF-ADR-038 stays Experimental. | Reject undoing the logical commit after physical output failure or broadening experimental topology guarantees. |
| Stable Availability keeps current token/results; cleanup release is permitted during stopping only for the exact token. Observer/Command remains Experimental except that inherited mutable access on a Stable trigger must be closed or explicitly accepted. Provisioning remains an Experimental ingress that must obey the same owner gate. | IF-ADR-044 unchanged. IF-GOV-001/Public-API boundary decision; no separate Observer ADR amendment proposed unless the review finds the declared status insufficient. | Reject promoting Observer, Provisioning, Leave or Actor Selection. Keep a single Session command owner; ingress aliases may not bypass its Host phase gate. |
| `ProgressionSavePayload` is immutable to callers; no atomicity, idempotency, async completion or provider disposal is added absent current contract text. | IF-ADR-018: propose a narrow editorial amendment to §11 so it does not contradict the Stable core inventory in §6/Public-API. The payload repair itself needs no contract expansion. | Reject snapshot/autosave/world serialization/cloud/encryption and generic provider lifecycle additions from this baseline. |
| Identity wrappers and serialized authoring identity remain as accepted. Decide whether internal Session ownership needs an opaque per-Host identity instead of `ApplicationName`; do not expose it as a new Stable type absent demonstrated need. | IF-ADR-001 for Session/Host lifetime; IF-ADR-018 only for Save application context if its editorial scope requires it; IF-GOV-001 for API maturity. No amendment to identity contracts proposed. | Reject converting presentation `ApplicationName` into a new public Session identity or adding pre-baseline compatibility rails. Preserve GameApplication/Route/Activity serialized schemas. |

Decisions that remain for the transverse reviewers: (a) accept the internal Host
phase/admission and inert retry contract and approve the minimal IF-ADR-001
amendment; (b) require exact Leave of each joined occurrence during controlled
shutdown, as proposed; (c) approve the IF-ADR-040 ledger clarification; (d)
resolve the inherited `TryGetAccess` exposure on the Stable Availability
trigger; (e) choose whether Session owner identity may continue using
`ApplicationName`; (f) approve the narrow IF-ADR-018 §11 correction; and (g)
decide whether any first-party Save provider actually requires an owned disposal
contract. The plan does not resolve these by implication.

The proposed admission table makes the cleanup boundary explicit:

| Host phase | Reject | Allow through existing owner |
|---|---|---|
| Bootstrap incomplete | External Route/Activity requests; Join and Open Joining; Actor select/replace/clear/default; Availability acquire; Camera activate/replace; user Pause/Resume; Provisioning admission. | Bootstrap's own ordered composition/start calls and rollback/release for the exact resources already acquired by that attempt. |
| Stopping / inert after rejection | All new gameplay/session intent, new Player admission, new Selection or Camera Assignment, new Availability blocks, and user Pause/Resume or Route/Activity transitions. Provisioning cannot reopen Joining or admit a Player. | Host-owned close Joining; settle an already-running GameFlow request to its defined terminal result; canonical Leave/retry for the exact joined occurrence; exact Availability token release; exact Camera Assignment/output clear; Pause/Input restoration and release; shutdown-owned Activity/Route exit; Scene Lifecycle and Persistent Content release/retry for the same owner and retained participant set. |
| Released | All operations. | None; callbacks are terminal/best-effort only. |

The allowlist is implemented as call-site policy at the existing authority,
not as a parallel command service. If an in-flight operation cannot reach a
terminal result, the Host remains retained and does not proceed to destructive
teardown; reviewers must approve the timeout/escalation policy before any
implementation adds one.

### Executable implementation workpackages (proposed order)

These packages are candidates for Gate A approval, not active work. Runtime
changes are limited to the listed proven Stable failures or to the Host design
accepted above. Any additional behavior change requires a reviewed plan update.
Source-analysis mapping: I-01 covers OP-P0-PC; I-02 covers OP-P1-JOIN-AVAIL,
OP-P1-PAUSE-INPUT-GATES and the Stable-facing boundary in OP-X-OBSERVER-COMMAND;
I-03 covers OP-P0-PC, OP-P2-APP-ROUTE-ACTIVITY-GAMEFLOW and OP-X-PROVISIONING;
I-04 covers OP-P1-LEAVE-ACTOR and OP-P1-CAMERA with I-02/I-03 dependencies;
I-05 covers OP-P2-APP-ROUTE-ACTIVITY-GAMEFLOW; I-06 covers OP-P2-SAVE;
I-07 covers OP-P2-IDENTITY-METADATA; I-08 covers OP-X-OBSERVER-COMMAND and
OP-X-PROVISIONING. No Experimental-only finding is promoted by this mapping.

| Order / package | Problem, outcome and likely files/subsystems | Contracts, consumers and tests | PASS / recovery / rollback / dependencies |
|---|---|---|---|
| **I-01 — Scene Lifecycle participant ledger** (P0) | Retain every participant that may have acquired Session resources even when a later `OnSceneAvailable` or rollback fails. Make compose failure and release retry use one exact owner/snapshot. Likely `Runtime/SceneLifecycle/SceneLifecycleRuntime.cs` and `Tests/Runtime/SceneLifecycle/*`. | Preserve `ISceneLifecycleParticipant`, scope identity and IF-ADR-040 semantics; no consumer API change. NUnit: failure at participant N after prior successes; rollback success/failure independently; release partial success then retry; duplicate release; exact Session owner mismatch; cleanup ordering. QA scenario: Session composition partially acquires participant state, then host releases and retries without orphaning any participant. | PASS only when no acquired participant is absent from the ledger and aggregate success is reported only after all required releases succeed. Failed rollback retains all receipts and permits retry. Revert this cut if it changes successful ordering or scope semantics beyond approved ADR text. Depends on IF-ADR-040 disposition; precedes Host stop. |
| **I-02 — Stable scoped release and physical receipts** (P0) | Fix availability release-before-binding-dispose ordering and retain token/projection/Pause/Input restoration evidence after rejection. Close or explicitly disposition public inherited `TryGetAccess`. Likely `PlayerSessionScopedAccessRuntimeHostModule`, `PlayerGameplayAvailabilityBlockTrigger`, `PlayerGameplayRuntimeHostModule.ConsumerAvailability`, `PlayerGameplayInputBindingRuntimeContext`, `PauseSessionPlayerInputBindingRuntimeHostModule`, `PlayerPauseInput`, `UnityPlayerInputStateWriter`; associated NUnit and API/consumer references. | Preserve IF-ADR-005/036/044 outcomes and token identity; no GameplayAction blocker. Audit FirstGame, QAFramework and all package callsites before visibility changes. NUnit: exact release success/rejection/retry; stale/foreign token; recipient access live until callback; projection failure; Pause apply + rollback rejection; later restore retry; no false logical/physical success. QA scenarios: Availability token rejection/retry and Pause/Input physical baseline restoration after release failure. | PASS requires owner/receipt retained until confirmed restoration/release or exact terminal subject destruction, and no new Stable capability leak unless accepted. Roll back a visibility change if a supported in-scope consumer is unaccounted; no pre-1.1 compatibility obligation is inferred. Depends on transverse API decision; precedes integrated Leave/Host. |
| **I-03 — Host admission, bootstrap failure and controlled stop** (P0) | Add internal Host phase gates at canonical Route/Activity/Join/Selection/Availability/Camera/Pause/Provisioning mutation owners; fail closed before operational and while stopping; implement retained, retryable controlled stop and route bootstrap failures through rollback. Likely `FrameworkRuntimeHost.cs`, `ImmersiveFrameworkBootstrap.cs`, gate-bearing Runtime modules/triggers and Host tests. | Keep public APIs, serialized assets and result semantics; Join still uses accepted `RejectedUnavailable`; cleanup exact release commands remain callable. NUnit: each ingress before ready/stopping; bootstrap failures pre/post PC composition; provisioning attach failure; in-flight transition settlement; stop success; each release stage rejects then retry on same Host; terminal callbacks best-effort. QA scenario: host bootstrap/stop across the allowlist with injected failures and retry on the same live Host. | PASS only when no new operation mutates state outside Operational, cleanup allowlist remains usable, and failed controlled release leaves same Host/dependencies retryable. No destruction after failed controlled stop. Roll back if guards duplicate or bypass canonical owners. Depends on I-01/I-02 and IF-ADR-001 approval. |
| **I-04 — Canonical player/camera/activity teardown integration** (P0) | Integrate controlled stop with existing per-occurrence Leave, Camera Assignment/Output, Pause/Input and Activity/Route owners; do not create Host-level parallel cleanup. Likely `SessionPlayerLeaveRuntimeHostModule`, Camera assignment/materialization/membership modules, Pause/Input binding modules, Activity/Route lifecycle, and integrated NUnit/QA fixtures. | Keep Leave/Actor Selection Experimental and Camera scope at IF-ADR-039/043; preserve Actor/Slot/Occurrence/token identity. NUnit: joined Player stop; each Leave stage rejection/retry; Camera clear/output failure pre/post commit; PlayerInput camera restoration; Pause restore; Availability release; multiple Players and duplicate Leave; Activity/Route exit after Players; no stale owners; Host remains retryable. QA scenario: two joined occurrences with Camera and Pause ownership, one failing cleanup stage, then retry and verify final baseline. | PASS requires Camera and physical Input cleanup while subjects are live, exact Leave completion before Host destruction, and no lost receipts after partial cleanup. Failed attempt leaves Host inert and retryable. Depends on I-02/I-03; ordering within existing Leave owner must be approved from tests, not duplicated. |
| **I-05 — GameFlow partial-failure truth and recovery** (P1; conditional Runtime) | Fault-inject Start/Route/Activity failures before and after commit; verify Gate, readiness, Route/Activity bindings, Scene Content and Host ownership stay truthful. Likely `GameFlowRuntime`, `RouteLifecycleRuntime`, `ActivityFlowRuntime`, `SceneLifecycleRuntime`, readiness binding and GameFlow NUnit/QA. Runtime change only for a reproduced Stable contract violation. | Preserve IF-ADR-036/047 and serialized `GameApplicationAsset`, `RouteAsset`, `ActivityAsset`; no all-or-nothing Route promise. NUnit: target composition fail, old scope release fail, post-commit readiness failure, Transition Gate terminality, cleanup retry and baseline restoration. QA scenario: route/activity pre-commit and post-commit failures with owner/result/baseline assertions. | PASS when every result identifies the committed authority and every residual owner has an available cleanup/retry path. If partial outcomes satisfy current contract, close with tests/evidence only. Runtime patch rollback if it changes commit point or revives released authority. Depends on I-03/I-04 and owning ADR review if promise expands. |
| **I-06 — Progression Save payload integrity and conformance** (P0) | Prevent mutation through `Bytes` while preserving byte content/equality. Likely `Runtime/ProgressionSave/ProgressionSavePayload.cs` and Save NUnit fixture. | Preserve all 13 Stable contracts and `IProgressionSaveStore`; audit current consumers. NUnit: input-array copy, exposed-view mutation/cast attempt, equality/hash, Empty/Text/Binary round trips, result/code and payload preservation across read/write/delete, backend rejection/failure distinctions. QA scenario: current Stable Store contract smoke with exact package identity and successful/rejected operation outcomes. | PASS when caller mutation cannot alter a constructed payload or subsequent store result. No async/atomic/idempotency claims. Revert only the implementation mechanism if API/value semantics change. Independent after Gate A; may execute alongside I-01. |
| **I-07 — Identity and metadata baseline conformance** (P1) | Add direct tests for domain/equality/hash/invalid/default/parse/conversion and Stable attribute declarations; investigate `ApplicationName` Session ownership and undefined `FrameworkApiStatus` values. Likely `Runtime/Identity/*`, `Runtime/ApiStatus/*`, identity tests and inventory scripts. Runtime change only for a demonstrated inconsistency or explicitly accepted identity decision. | Preserve current seven identity contracts, asset schemas and Stable inventory. NUnit: cross-domain collision rejection, same-domain value rules, serialized Application/Route/Activity references, statuses and metadata against Public-API/IF-GOV-001. QA scenario: package/consumer authoring references resolve without changing serialized schemas. | PASS when implementation, attributes and governance inventory agree and invalid values fail according to documented contract. Treat formatting differences as non-defects unless tests reveal semantic ambiguity. Roll back unapproved public identity changes. Depends on identity decision; no Stable API expansion. |
| **I-08 — Observer/Provisioning ingress and closure audit** (P1; Experimental boundary) | Confirm every ingress reaches the same gated Session owner and scope read/write requirements. Migrate only consumers required by I-02/I-03; otherwise preserve Experimental surfaces. Likely Observer/Command/Provisioning authoring and runtime modules, Public-API inventory and QA consumers. | No Stable promotion and no new Session authority. NUnit: Observer read projection; mutation unavailable to read-only consumers; Provisioning direct/automatic ingress while Starting/Stopping; Join closure unchanged; scoped release remains allowed. QA scenario: all supported Stable and Experimental ingress paths obey the same Host gate without duplicate ownership. | PASS when no ingress bypasses the Host gate and no duplicated owner exists; Experimental-only behavior may be deferred when no Stable causal path exists. Roll back broad migrations not needed for a Stable contract. Depends on I-02/I-03. |

### Cross-package validation, certification and GO/NO-GO

1. **Before implementation (Gate A):** approve this plan revision, the exact
   package order, the IF-ADR-001/040/018 dispositions, Observer exposure,
   shutdown Leave policy, Session identity decision and risk owners. Record
   named test/QA cases and consumer callsite audit. This plan remains Proposed
   until that record is explicit.
2. **Per implementation package:** add deterministic NUnit fault injection
   first; implement only the approved owner-local change; run focused tests and
   package suite; inspect diff for public/API/schema/version/workflow scope;
   update candidate evidence. Unity compile/import, Play Mode and QAFramework
   execution are separate required validation by the authorized executor.
3. **Integrated lifecycle candidate:** test bootstrap pre/post Session
   composition; exact Stop allowlist; multi-Player canonical Leave; Camera and
   Pause/Input teardown; availability token release; partial participant
   rollback; Scene Lifecycle and Persistent Content final release; failure at
   every stage followed by retry on the same Host; baseline restoration after
   Route/Activity failures. Verify no command mutates during Starting/Stopping.
4. **Freeze/certify:** after all approved cuts, freeze an exact source revision
   and candidate package identity. Require zero unexplained NUnit failures,
   candidate-specific QAFramework evidence for all Stable contracts and
   failure/cleanup paths, reviewed Public-API/Stable inventory, consumer
   validation in QAFramework, FirstGame and PEGA, and resolved/accepted risks.
   Historical results do not count for this candidate.
5. **Publication gate:** only after IF-ADR-048 Phases 2–4 evidence and explicit
   Phase 5 GO may release metadata, version, tag or publication be authorized.
   None is authorized here.

**Required Stable corrections proposed:** I-01 participant ledger retention;
I-02 availability/scoped release ownership and any confirmed live Pause/Input
receipt loss; I-03 fail-closed Host admission plus controlled retryable stop;
I-04 canonical cleanup integration; I-06 immutable Save payload. I-05 is a
Runtime correction only if fault injection establishes a Stable violation;
I-07 is primarily certification unless a contract inconsistency is proven;
I-08 changes only the ingress needed to protect Stable closures.

**Certification without Runtime change proposed:** Stable API/maturity inventory
and identity/status conformance; bounded Join and Availability result matrices;
Camera 039/043 success/reject/post-commit behavior; Pause/Gate and IF-ADR-036
structural-versus-operational cases; Session/Route/Activity serialized schema
preservation; Save read/write/delete result and error-code matrix; consumer
package identity and exact-candidate installation. Each requires current
candidate evidence, not historical test counts.

**Risk acceptance required:** in-memory retry ends at process/Unity destruction;
Route transitions may have truthful partial outcomes; Experimental Provisioning,
Observer, Actor Selection, Leave and IF-ADR-038 Camera topologies remain outside
Stable promises except where they ingress into a Stable owner; Save has no
atomicity/idempotency/async/provider-disposal promise; `ApplicationName` may
collide if it remains an internal Session owner key. Each is either bounded by
the accepted contract or explicitly assigned to an accepting authority.

**Deferred beyond 1.1.0:** all-or-nothing Route replacement absent a Stable
requirement; public Host lifecycle API; generic cleanup registry; automatic
Save/autosave, snapshots, world serialization, cloud/encrypted backends and
provider lifecycle; new Session identity API; broad Observer/Provisioning
redesign; promotion or certification of experimental Actor/Leave/Camera
topologies; compatibility shims for pre-1.1 prototypes.

**Planning GO/NO-GO:** this is a complete integrated proposal for review, not
Gate A approval. Planning remains Proposed and implementation remains NO-GO
until the pending decisions above are recorded and the integrated plan is
explicitly approved. Fase 2 remains open; no workpackage is implemented or
certified by this record.

## Standard workpackage record

Use this record for each analysis, decision or execution result. Keep live
status/evidence in IF-TRACK-Framework; an approved plan has no mutable
completion checkboxes.

```text
Workpackage ID / title:
Source revision and audit date:
Owning subsystem / accountable owner:
Stable contracts and Experimental dependencies:
Runtime authority and known consumer callsites:
Finding class: Stable-direct | indirect | Experimental-only | post-release
Observed behavior (callsite/file/symbol):
Expected behavior and governing ADR/Public-API clause:
Evidence provenance: executed | static | user-reported | historical | proposed
Lifecycle: entry -> commit -> rollback -> retry -> release -> cleanup
Concrete gap or “none demonstrated”:
Decision needed / authority / disposition:
Dependencies and migration impact:
Implementation scope (only after analysis and approval gate):
Automated tests: fixture, setup, injected failure, assertions, cleanup:
Integrated QA/consumer matrix: project, package identity, scenario, cleanup:
PASS / FAIL / blocked criteria:
Residual risk and accepting authority:
Review result: pending | accepted | rejected; reviewer/date:
Tracker evidence reference:
```

Do not record a hypothesis as a defect, static scan as execution, aggregate
pass counts as artifact certification, or Experimental results as Stable
evidence without a demonstrated causal path.

## Integrated-plan approval record

Gate A must be recorded as one versioned decision linked from the tracker. It
lists the exact plan revision and baseline, accepted decision references,
included workpackage IDs, implementation order, named tests/QA deliverables,
consumer migration disposition, reviewers, unresolved risks and explicit
approval or rejection. Silence or phase progression is not approval.

## Completion criteria and release boundary

- **Analysis complete:** each workpackage has source-backed owner/authority and
  lifecycle traces, evidence classification, gap disposition and decision
  owner. This alone does not close Phase 2.
- **Plan approved:** Gate A receives explicit integrated approval. This alone
  does not authorize changes.
- **Operational certification complete:** approved changes and named tests/QA
  execute on an identified source revision; failures, cleanup and residual
  risk are reviewed. Only then may Phase 2 be recommended for explicit closure
  under IF-ADR-048.
- **Stable release:** still requires IF-ADR-048 Phases 3–5, exact immutable
  1.1.0 artifact installed and identified in QAFramework, FirstGame and PEGA,
  and explicit final GO. This plan authorizes no version, tag or publication.
