# Player QA Suite Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Use QAFramework `qa-certification-design` for each scenario before `qa-certification-implementation`; all Unity execution is a manual user gate.

**Goal:** Rebuild complete, traceable coverage of current Player contracts in QAFramework using QA-001, without restoring legacy executors or changing Framework runtime.

**Architecture:** Keep deterministic contracts in package NUnit tests, real lifecycle/adversarial proofs in independent QAFramework scenarios, and integrated consumer happy paths in FirstGame. Start with two vertical slices and reassess shared infrastructure before expanding; each scenario owns its baseline, evidence, terminal outcome, cleanup and dependencies.

**Tech Stack:** QAFramework Unity `6000.5.0f1`; Framework `1.1.0-preview.7` from local path `C:\Projetos\ImmersivePackages\com.immersive.framework` (manifest/lock has no immutable hash); Unity Test Framework/NUnit; synthetic Input System devices where required.

**Spec:** `Documentation~/Architecture/Reconciliation/IF-PLAYER-FULL-RECERTIFICATION-2026-10-11.md`, IF-GOV-001, Player ADRs, QAFramework `Documentation~/ADR-001-QA-Framework-Certification-Architecture.md` and current operation docs.

## Global Constraints

- Baseline observed: QAFramework HEAD `76081302f5fa245ecc3b374a192911b210299b50`; Framework HEAD `49848313131daffd1b7f8c82c709a7fad09ca459`. Capture fresh SHAs and dirty state per run; Framework is a mutable `file:` dependency.
- Do not restore old Player executors, create a parallel QA model, modify Player runtime, promote API maturity, or alter FirstGame/PEGA.
- Runtime QA uses public APIs/authoring only: no private reflection, hidden discovery, internal host access, private mutation or test-only runtime backdoor.
- Agent must not run Unity, import/compile, Play Mode, smokes or batchmode. User executes manual Unity gates.
- Use QA-001 outcomes `PASS`, `FAIL`, `BLOCKED`; `SKIP` is not a runtime verdict. Missing terminal evidence is never PASS. Publish terminal only after cleanup.
- Do not duplicate deterministic package NUnit or independent IF-ADR-044 certification.
- Preserve `.meta`/GUIDs. Setup is distinct from execution, scoped, idempotent and validated after persistence.

## Review Focus

- Stale revision/token is rejected by the exact public result while current ownership remains unchanged (Session/Actor).
- Failed materialization/adoption leaves no partial ownership, duplicate occurrence or Input pairing (Provisioning).
- P1/P2 retain isolated Device, Slot, PlayerInput, occurrence and Reader ownership simultaneously (Input).
- Zero/one/two Players satisfy declared Required/Optional readiness and Route/Activity spatial behavior (Game Flow).
- Interrupted execution or cleanup failure cannot be reported PASS or poison the next run (every slice).

---

## 1. Current QA Model (source-first audit)

QA-001 separates three layers:

| Layer | Responsibility | Player examples |
|---|---|---|
| Framework package/NUnit | Isolated deterministic logic, values, validation, serialization, structural Editor tests and fakes | Profile serialization, policies, revision/value contracts, validators that need no runtime |
| QAFramework | Real Framework lifecycle and Unity composition, public boundaries, adversarial/negative runtime | Join/Leave, provisioning, occurrence adoption/replacement, Input pairing/Reader, readiness and transitions |
| FirstGame | Integrated consumer happy path | Existing Manager-Provisioned/local-multiplayer samples, read-only in this plan |

Current module sources/assets are under `Assets/QA-IF-ADR-042`…`045`, shared terminal code under `Assets/_Project/Scripts/QaCertificationTerminal.cs`. Runtime scenarios are generally `MonoBehaviour`s in Persistent Content scenes, started from `Start()`/coroutines. Manual flow: module `Immersive Framework → QA → IF-ADR-0XX → Configure…`; select its GameApplication in `ImmersiveFrameworkSettings`; open persisted Persistent Content scene; start fresh Play Mode; inspect causal logs and terminal line.

Setup is separate from runtime execution. 042/043/044 persist settings/scenes and may change `EditorBuildSettings`; 044 reuses 042 material and adds Activity/Profile/Reader composition. 045 provides scoped generated-content cleanup (`Prepare`, run migration cases, remove exact owned paths, report unknown leftovers) and does not edit build lists. Reuse these ownership patterns only where appropriate; 045 is not a Player example.

`QaCertificationTerminal.cs` defines internal `Pass/Fail/Blocked`, cleanup disposition (`BaselineRestored`/`FreshBootRequired`), result and recorder. 042–044 use it or related publishing in predefined assemblies. 045 has Runtime/Editor asmdefs and publishes its own result because an asmdef cannot reference internal `Assembly-CSharp` code. 042–044 do not have asmdefs. This divergence is real; QA-001 intentionally leaves shared runner, scenario/environment/result types, registry, folders, asmdefs, menu, timeout and boot grouping open until two vertical slices prove shared needs.

### Source/documentation divergences

- `immersive-framework-qa-smokes/references/canonical-patterns.md` points to removed `Assets/ImmersiveFrameworkQA` and historical PlayerV2/editor regressions. Current QA-001, local QA skills and 042–045 source are authoritative; record the stale reference for a separate skill-owner update.
- Earlier recertification docs incorrectly said QAFramework lacked `ProjectSettings/ProjectVersion.txt`. It exists and declares Unity `6000.5.0f1`; manifest/lock resolve Framework `.7` by local `file:` path without hash. Generated `ImmersiveFrameworkQA.Player*.csproj` are ignored artifacts, not tracked infrastructure.
- The historical `QaPlayerFullCertificationOrchestrator`/27-case aggregate is absent from current Framework and QA source. It is catalog/history, not a required architecture to restore.
- 045 covers Persistent Content migration, not Player. Its shutdown case is BLOCKED because no supported public shutdown request was found; do not add private shutdown access to QA.

## 2. Player Scenario Matrix

Readiness is design-only. `READY FOR IMPLEMENTATION` requires confirmed public action, observation and valid baseline in exact `.7`. `BLOCKED` means public observability, supported failure injection or baseline is not yet proven. Inspect exact NUnit test classes before adding package tests.

| ID | Contract / scenario | Layer and baseline/action | Evidence, terminal, cleanup | Existing coverage / readiness |
|---|---|---|---|---|
| S1 | Session creation/config: slots, order, joining initial state, defaults and serialization (003/016) | NUnit for deterministic contracts; QA for real startup. Valid GameApplication + Session profile | Effective Session snapshot correlates profile, slots and revision; restore or mark fresh boot | Inspect profile tests. QA ready once baseline proven |
| S2 | Joining/selection: 0→P1→P1+P2, joining open/closed, default and `LeaveUnresolved` (003/015/015B/016) | QA. Invoke public Join trigger/capability and observe scoped Session | Typed Join result, slot, selection/revision and exact rejection; cleanup/fresh boot | 042/043 only camera participation. Ready for first slice |
| S3 | Stale selection/Leave revisions and requests (015B/019/020/025) | NUnit for pure policy; QA for lifecycle. Capture token/revision, advance owner, submit stale request | Exact typed rejection plus unchanged current owner; fresh boot if destructive | Historical aggregate only; ready when public snapshot proves invariant |
| P1 | Scene-Provided composition, admission/adoption, Leave/release (003/015/019/020) | Editor structural test for authoring; QA lifecycle with canonical persisted candidate | Exact Host/Actor occurrence admission, release result, no partial adoption; scenario owns cleanup | 042 is camera SharedGroup, not full Player admission; confirm `.7` validator and result |
| P2 | Manager-Provisioned before/during Activity: reserve→prepare→materialize→adopt (015/016/019/020/023A) | QA. Profiles + public Join and observer/readiness | Stage results correlate Slot/Host/Actor/PlayerInput; reverse rollback, no duplicate or leak | 043 camera and 044 input are not full provisioning. Ready after fixture proven |
| P3 | Provisioning failure/retry and rollback (015/016/020) | NUnit deterministic policy; QA real materialization if supported failure seam | Exact failing stage, no retained reservation/Actor/Input, valid retry; cleanup owned resources | BLOCKED if `.7` has no public failure injection; no test-only API |
| A1 | Actor identity/occurrence/spatial authority after 023A/038 | QA with Manager-Provisioned Actor | Public snapshot/result ties Slot to current occurrence and spatial owner; clean or fresh boot | Existing `ActivityPlayerActorLeaveLedgerTests` (3) and optional visual/Subject Editor tests are static/structural only. Confirm public observability |
| A2 | Prepared replacement, Leave/Rejoin, new occurrence and stale old references (024/038) | QA. Current occurrence + supported replacement/reentry | Typed result/revision, new current occurrence, old reference rejected; reverse cleanup | Historical 024 predates 038. Ready only if public current identity is observable |
| A3 | Actor preparation/adoption failure, rollback and no orphan (023A/038) | QA if failure can be induced by supported seam; otherwise package tests | Exact failed stage and rollback; no current ownership/physical occurrence | BLOCKED pending supported failure path |
| I1 | Device/Slot/PlayerInput/Actor ownership for simultaneous P1/P2 (025/038) | QA with synthetic Input devices and two joined Players | Per-device and PlayerInput correlation, isolated observations, no cross-signal; remove devices/Leave reverse order | 043 camera is not general Input proof. Confirm device-aware Join result |
| I2 | `PlayerGameplayInputReader` bind/rebind to current Activity occurrence (025/038) | QA; Actor declaration uses exact PlayerInput and Action Map | Binding token/revision, readiness/availability, isolated P1/P2 reads, stale binding rejection; Activity releases binding | Reader public read/snapshot exists; internal TryBind is forbidden to QA |
| I3 | Input Gate + Leave/Rejoin and non-interference (025/044) | QA integration, citing independent 044 certification | Public block token/availability and Reader state; one Player's gate does not affect other; clear tokens and cleanup | Do not repeat independent IF-ADR-044 8-case suite |
| R1 | Participation/readiness: 0/1/2 Players; Required/Optional (003/012/019/021/021A) | QA with real Route/Activity profiles | Per-slot public readiness and exact states; restore topology | Route 18/18, Activity 23/23 are historical only; confirm snapshot API |
| R2 | Route Spatial Entry, late Join/Leave, recovery/no duplicate placement (012/021/021A/038) | QA with explicit entry and current Actor | Spatial result/revision, readiness, cleanup on Route exit | Historical only; depends A1/R1 |
| R3 | Activity Relocation/transition, content unload/rebind/readiness (003/019/021/038) | QA with real Route/Activity transition | Transition result, current occurrence/readiness, old content released once | Depends R1/R2/I2; split at independent cleanup boundaries |
| X1 | IF-ADR-044 integration boundary | Keep existing QA-IF-ADR-044 independent | Player scenario asserts only required public Reader/gate interaction and cites separate 8-case result | No duplicate certification or aggregate runner |

## 3. Proposed Files, Assemblies and Fixtures

All implementation targets belong to `C:\Projetos\QAFramework`; this plan/report remains inside Framework documentation. Preserve existing module conventions and decide exact names after accepted scenario brief and `.7` validator inspection.

**Slice 1 (Session):** module README, one `QaPlayerSessionJoinScenario` component, one focused Editor configurator only if persisted authoring requires it, and minimal GameApplication/Route/Persistent Content/Activity/Profile assets with `.meta`. Prove Session start, public Join/default-or-unresolved behavior, one observation and cleanup. No generic runner or registry.

**Slice 2 (difficult lifecycle):** separate scenario/setup for either late Manager-Provisioned Join during Activity + Leave/Rejoin/stale occurrence, or Actor replacement/reentry under 038. Choose the one with fully public observation; split if failure/cleanup boundaries differ. Only after both manual runs assess common infrastructure.

**Expansion candidate, only after reassessment:** `Assets/QA-IF-PLAYER/{Session,Provisioning,Actor,Input,GameFlow}/{Scenes,Settings,Scripts,Editor}` plus focused README(s). Keep Host prefab separate from logical Actor evidence. Input fixtures own synthetic devices/action asset/references. Reuse `_Project` recorder only if same assembly can access it; otherwise local terminal publishers until an explicit QA architecture decision. Start in default assemblies; add per-module Runtime/Editor asmdefs only when proven useful. No broad base class, suite, environment, registry or orchestrator.

A technical Camera Output may be a mandatory `.7` boot prerequisite even when Player QA does not test Camera. Audit the exact gate first; if required, author a legitimate minimal QA output and explicitly exclude camera behavior from Player assertions and maturity claims. Never copy all of 042/043.

## 4. Incremental Implementation Plan

### Phase 0 — per-slice preflight

- [ ] Capture QA and Framework branch/HEAD/dirty state; resolve Unity version, manifest/lock, package.json and local path provenance.
- [ ] Trace public Player authoring validators and all unconditional/conditional GameApplication boot gates; record owner, condition, prerequisite, result and maturity.
- [ ] Inventory exact package NUnit classes/tests for each candidate assertion; do not duplicate them.
- [ ] Write/accept scenario design brief: known baseline, public action, observation, expected diagnostic, terminal, failure path, cleanup and manual launch. Stop BLOCKED where proof would require internals.

### Phase 1 — Session vertical slice

- [ ] Add only the Player module's README, one Session scenario and its necessary setup/assets.
- [ ] Keep Editor setup separate and idempotent; validate persisted result and preserve unrelated scene/build settings.
- [ ] Scenario captures Session/slot/revision baseline, invokes public Join, proves default or `LeaveUnresolved`, observes typed state and restores baseline or declares fresh boot.
- [ ] Publish one stable terminal result after cleanup. Exact typed result proves expected negatives; generic error logs do not.
- [ ] Static review `.meta`, serialized references, paths, case count/order and cleanup; agent does not execute Unity.

### Phase 2 — user validation gate

- [ ] Open QAFramework in Unity `6000.5.0f1`; record resolved Framework `.7` HEAD and dirty status.
- [ ] Run focused Configure once; inspect assets and all applicable GameApplication/Route/Activity/Profile/scene validators; run Configure a second time to prove idempotence.
- [ ] Select the QA GameApplication in `ImmersiveFrameworkSettings`, open persisted Persistent Content and start fresh Play Mode.
- [ ] Capture complete boot→Join→selection→terminal→cleanup logs. Review the full causal evidence before implementing Slice 2.

### Phase 3 — difficult slice and reassessment

- [ ] Implement one lifecycle: late Join/Leave/Rejoin/stale occurrence, or prepared replacement/reentry under IF-ADR-038. Split if boundaries differ.
- [ ] Prove old ownership is revoked before new acceptance; prove rollback/cleanup. Never force shutdown through internals; mark fresh boot or BLOCKED.
- [ ] Repeat manual validation and setup idempotence.
- [ ] Reassess QA-001 with two slices: recorder sharing, assemblies, setup, timeout/reload persistence and environment boundaries. Amend QA architecture before creating shared machinery; otherwise retain independent scenarios.

### Phase 4 — expand coverage

- [ ] Add independent Scene-Provided admission/Leave and Manager-Provisioned prepare/materialize/adopt/fail/retry coverage.
- [ ] Add Actor identity/authority, replacement, reentry, stale-reference and rollback scenarios for 023A/038.
- [ ] Add synthetic two-device P1/P2 ownership, Reader binding/rebinding, gate isolation, Leave/Rejoin and cross-player interference. Cite 044; do not duplicate it.
- [ ] Add Activity/Route zero/P1/P1+P2 Required/Optional, late Join/Leave, Spatial Entry, Relocation, transitions and cleanup. Split independent causal lifecycles.
- [ ] After each scenario perform static review and request manual Unity evidence; scenarios must not rely on execution order.

### Phase 5 — versioned recertification and maturity

- [ ] Bind each run to QA HEAD, Framework package HEAD/dirty state, package version, manifest/lock, Unity version, scenario/setup revision, fixture identity and timestamp.
- [ ] User executes independent scenarios and supplies complete logs plus terminal result. Store concise hashed evidence reports using QAFramework's current convention; do not treat historical SHA as current PASS.
- [ ] Re-run after any Framework fix on the exact candidate SHA. Record PASS/FAIL/BLOCKED per scenario after cleanup; no opaque aggregate percentage.
- [ ] Only after executed evidence, reassess each API separately under IF-GOV-001. Keep IF-ADR-044 bounded Stable independent of broader scoped access/Reader; no maturity or release changes in this plan.

## 5. Manual Execution and Acceptance

For each scenario: open `C:\Projetos\QAFramework` in Unity `6000.5.0f1`; verify manifest/lock and Framework `.7`, record package `git rev-parse HEAD` and dirty status; run documented Configure/Prepare and inspect assets/validators; repeat setup where applicable to prove idempotence; select GameApplication in `ImmersiveFrameworkSettings`; open its persisted Persistent Content scene; start fresh Play Mode; capture full Console/Editor.log through terminal and cleanup; verify exact typed public outcomes, baseline restoration or fresh-boot requirement; run scoped cleanup and confirm scene/build settings/unrelated assets unchanged. Capture log/report/fixture SHA-256 as applicable. Authoring validation, setup idempotence and runtime PASS are separate results.

Acceptance requires every Player contract mapped to package NUnit, independent QA scenario or justified non-external contract; no duplicated NUnit/044 suite; exact negative result and unchanged baseline; independent scenario baseline/terminal/cleanup; validators and setup idempotence recorded separately; exact SHAs/versions; no Player runtime, maturity, FrameworkValidation, FirstGame or PEGA changes.

## 6. Risks and Open Decisions

1. **Session shutdown:** 045 found no public shutdown request. Confirm exact `.7` contract; use fresh boot or package/internal proof, never private QA access.
2. **Occurrence observability:** confirm public snapshots/results prove current physical Actor occurrence and stale-reference rejection; otherwise record public observability gap and stop that scenario.
3. **Failure injection:** if no public seam exists, keep deterministic rollback in package tests and mark real QA failure path BLOCKED; do not add test-only Framework API.
4. **Camera bootstrap:** inspect actual `.7` call path and validator; include minimal legitimate technical output only if boot requires it, without certifying Camera.
5. **Recorder/assemblies:** 045 asmdef cannot consume internal `_Project` helper. Decide after two validated slices; avoid accidental framework dependency.
6. **Setup ownership:** 042–044 persisted assets/build-list changes; 045 scoped generated assets/no build-list mutation. Select per scenario and preserve exact cleanup ownership.
7. **Maturity:** Manager-Provisioned, Session access, Actor occurrence and Reader are Experimental in `.7`; QA evidence alone does not promote them.
8. **Mutable dependency:** local `file:` lock has no hash; record Framework HEAD and dirty state every run.
9. **Stale canonical-pattern skill reference:** propose separate correction to skill owner; do not edit shared skill here.
10. Historical 27-case aggregate is not a required design; reconstruct from current ADRs without a monolithic runner.

## 7. Contract and Source Index

QA: QAFramework `Documentation~/ADR-001-QA-Framework-Certification-Architecture.md`, local `qa-certification-design`, `qa-certification-implementation`, `qa-certification-review`; modules `Assets/QA-IF-ADR-042` through `045`; terminal `Assets/_Project/Scripts/QaCertificationTerminal.cs`.

Player authority: IF-GOV-001; IF-ADR-003, 012, 015/015B, 016, 019, 020, 021/021A, 023A, 024, 025, 038, 044. Installed public source to inspect: `Runtime/PlayerParticipation/Authoring/PlayerSessionJoinCommandTrigger.cs`, `Contracts/IPlayerSessionScopedAccess.cs`, `Authoring/PlayerGameplayInputReader.cs`, and public Session/Actor/Activity/Route snapshots, authoring assets and validators.

Existing tests to inventory before adding cases: `Tests/Editor/PlayerParticipation/ActivityPlayerActorLeaveLedgerTests.cs`; `Tests/Editor/Camera/Assignments/ActorOccurrenceOptionalVisualContentTests.cs`, `ActorCameraSubjectAuthoringTests.cs`, and `PlayerCameraOutputIntegrationTests.cs`. Static inspection is not test execution.
