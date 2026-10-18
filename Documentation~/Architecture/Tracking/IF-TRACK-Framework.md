# IF-TRACK — Immersive Framework

Status: **Complete — Stable 1.1.0 published and verified on 2026-10-14; IF-ADR-046 Player Join closure APPROVED Stable**

Last updated: **2026-10-17**

## 1.1.0 first Stable release readiness

IF-ADR-047 establishes `1.1.0` as the first official Stable baseline. Phases
1–5 are closed. The Project Owner gave explicit Phase 5 GO on 2026-10-14 for
Stable `v1.1.0`, derived from the Preview 11 candidate. The tagged workflow
run [#20, attempt 2](https://github.com/ImmersiveGames/com.immersive.framework/actions/runs/38625559085)
completed successfully, including candidate build and publication. The stable
GitHub Release [v1.1.0](https://github.com/ImmersiveGames/com.immersive.framework/releases/tag/v1.1.0)
is non-prerelease and contains `com.immersive.framework-1.1.0.tgz` with
SHA-256 `ce86cff5eb8e4da3964ef8eab124042794cba9ecebc44c464881d4967013dd2e`.
The workflow published `com.immersive.framework@1.1.0` to OpenUPM, which
reported it as signed; its registry tarball byte-for-byte matched the GitHub
Release asset. The stable tag points to `c96227afccdb510b9d6bf5603d58f2ea8d126292`;
`Runtime/` and `Tests/` are unchanged from the tested Preview 11 tag.
Immutable prereleases `.9` and `.10` remain historical failed attempts; `.11`
was the successful Release Candidate. Previously published 1.0.2 and preview
artifacts remain historical; migration from 1.0.2 is not a blocker. This
disposition does not waive an explicit gate for an individual Stable contract
or serialized-data guarantee. [IF-ADR-048](../ADRs/IF-ADR-048-Framework-1.1.0-Stable-Readiness-Roadmap.md)
is the accepted readiness roadmap; its approval did not itself authorize
publication. The Project Owner's composed evidence acceptance closed Phases
2–4, and the later explicit Phase 5 GO plus successful publication closes the
release mission.

The integrated execution route is
[IF-PLAN-ADR-048-STABLE-READINESS.v1](../Plans/IF-PLAN-ADR-048-STABLE-READINESS.v1.md).
The [Gate A v2 approval record](../Plans/IF-GATE-A-ADR-048-STABLE-READINESS-REVIEW.v2.md)
records GO for exact plan blob `628f64b2878cdad9fbf8d89392e8e39725b6fe3a`
and the functional decisions, risk acceptances and evidence route. The
preparation record v1 is preserved as historical review. The mission's
conditional implementation authorization became effective after Gate A. Gate A
closed no workpackage or readiness phase. Phases 2–4 closed on composed
operational and candidate evidence; Phase 5 publication is now closed with the
execution evidence recorded below.

| Gate | GO criteria | Current evidence / disposition |
|---|---|---|
| Runtime integrity | Candidate imports and compiles in the supported Unity `6000.5` line; all release-blocking defects in Stable behavior are resolved; lifecycle rollback/retry/cleanup paths covered at their contract scope. | The latest user-reported QAFramework EditMode run is 365/365, including the controlled-stop integration suite at 7/7; complementary QA scenario results are recorded below. This closes Phase 2 operational integrity by composed evidence, but does not replace exact-candidate import/compile in Phases 3–4. |
| Contracts and architecture | `FrameworkApiStatusAttribute`, Public-API, accepted ADRs and observed implementation agree for every Stable contract included in 1.1.0; no Experimental/Internal promotion inferred from package version; breaking changes have explicit accepted decisions and consumer disposition. | **Phase 1 closed by explicit architectural review (2026-10-12):** approved inventory of 107 Stable Runtime types reconciled with Public-API and owning ADRs. Evidence is documentary/static only: symbol/status inventory, exact Public-API symbol coverage, relative-link checks and `git diff --check`. It does not certify Runtime behavior, a candidate package or publication. |
| QA coverage | Focused automated and integrated evidence covers each release Stable guarantee and its material failure/cleanup paths; evidence names exact candidate/package resolution where the claim depends on it. All failures are resolved or explicitly accepted by the responsible authority under the owning ADR. | **Phase 2 operational evidence accepted compositionally (2026-10-14):** QAFramework EditMode 365/365, including Framework Host controlled-stop 7/7 and the package assemblies shown in the Test Runner, combined with the named QA scenario results below. This is user-reported working-source evidence, not immutable RC certification. |
| Reproducible distribution | Immutable `v1.1.0` tag resolves to the reviewed source commit; tag, `package.json`, dependency graph and release archive identify the same candidate; signed-release workflow completes archive checks and publication; artifact identity and consumer import/compile are recorded. | **Closed:** Preview 11 candidate build and composed consumer import/compile evidence closed Phase 4. Stable tag `v1.1.0` resolves to `c96227afccdb510b9d6bf5603d58f2ea8d126292`; run #20 attempt 2 built and published successfully. Stable GitHub Release includes the `.tgz` with SHA-256 `ce86cff5eb8e4da3964ef8eab124042794cba9ecebc44c464881d4967013dd2e`; OpenUPM verified the same tarball bytes. `Runtime/` and `Tests/` match Preview 11. |

**GO only when all four gates pass** and no unresolved Stable-contract, package
resolution, import/compile or artifact-identity blocker remains. **NO-GO** for
any unexplained failure or mismatch in those areas. A known, bounded risk in an
Experimental contract may be documented and accepted only when it does not
undermine a Stable guarantee or a specific accepted release gate.

### Release-candidate sequence

1. Freeze the candidate source; review the per-contract Stable inventory and
   `package.json` dependency graph against accepted ADRs and Public-API.
2. Run the focused package and QAFramework lanes required by those Stable
   guarantees in the supported Unity version; record exact source revision,
   resolved package source/version, result and available raw evidence.
3. Run consumer import/compile and targeted smoke validation against the same
   immutable candidate. Keep unrelated or unsupported Experimental scenarios
   out of the release claim unless they affect Stable behavior.
4. Set package metadata and annotated release tag to the approved `1.1.0`
   source; verify signed-release workflow archive contents, hash/provenance,
   GitHub Release/OpenUPM artifact identity and installed package resolution.
5. Record gate disposition and residual risks here before declaring GO. Publish
   only after the gates pass; do not infer publication or validation from the
   existence of a local source SHA or old preview evidence.

The sequence above remains the current tracker checklist, reconciled with the
five accepted IF-ADR-048 phases. The tracker records execution evidence and
phase status; acceptance of the roadmap did not complete any phase.

The GitHub workflow's presence/attestation-file check is not itself proof of
independent cryptographic signature verification; record only what the run and
consumer installation actually establish.

## IF-ADR-048 phase status

| Phase | Status | Evidence / boundary |
|---|---|---|
| 1 — Architectural and contract closure | **Closed — documentary review approved 2026-10-12** | The 107-type Stable inventory is reconciled across `FrameworkApiStatusAttribute`, Public-API, IF-GOV-001 and owning ADRs. Static checks found all 107 symbols represented in Public-API, all relative Markdown targets in the reviewed documents present, and `git diff --check` clean. This approval covers contract scope and documentation only; it does not certify the Runtime, package import, candidate artifact or release. |
| 2 — Operational integrity | **Closed — composite evidence accepted by the Project Owner, 2026-10-14** | The latest QAFramework Test Runner result is 365/365, with Persistent Content 28/28 and `FrameworkRuntimeHostControlledStopTests` 7/7. These integrated tests are accepted together with the component Editor suites and the previously reported QA scenario terminals; a separate bespoke test for behavior already covered across those proofs is not required. Bounded residuals and evidence scope are recorded in the latest Phase 2 entry. |
| 3 — Transversal recertification | **Closed — candidate source validated, 2026-10-14** | Frozen source `e98b530a961bd4d5c9795398a2ff42096a4ea95d`; the user supplied QAFramework EditMode 365/365 and candidate-local `file:` resolution evidence plus the listed QA terminals. The test screenshot/logs are user-reported and do not include raw XML or a runner fingerprint. The complete test/runtime source matches the reviewed candidate; no code was changed after the freeze. |
| 4 — Immutable Release Candidate | **Closed — candidate built and consumer import/compile accepted compositionally, 2026-10-14** | Immutable tag `v1.1.0-preview.11` resolves to `a0d4f809fea5c0d7781e2161024eb3c0bcc532fd`. Signed candidate workflow run [#19](https://github.com/ImmersiveGames/com.immersive.framework/actions/runs/38617237048) completed successfully; pack/sign, candidate and provenance verification, and upload passed. Uploaded artifact `signed-upm-candidate-v1.1.0-preview.11-38617237048-attempt-1` has GitHub artifact SHA-256 `1f6833f1f95a39de3721fb7fef86e9f0f3d007b60532bb13db1edc81f345cff7`; the package `.tgz` SHA-256 recorded by its provenance is `ab9785e97c3076afa1e2c8565df5b172b5e4c3e55ffe862e9462e6722e0d42e5`. The Project Owner reports that both Git-based consumers temporarily installed this Preview 11 tarball and imported/compiled without errors; after validation both were restored to their normal Git package references. QAFramework's 365/365 Editor result covers the same candidate source commit; `Runtime/` and `Tests/` match the immutable candidate. No Unity run was performed by this session. The consumer import result is owner-reported, not independently captured from the transient package locks. Promotion was skipped as required for a prerelease. The workflow's candidate/provenance check is run evidence, not independent cryptographic signature verification. |
| 5 — Explicit publication decision | **Closed — Stable `v1.1.0` published and verified, 2026-10-14** | Project Owner authorized Stable `v1.1.0`. Run [#20, attempt 2](https://github.com/ImmersiveGames/com.immersive.framework/actions/runs/38625559085) succeeded: candidate build, Release asset identity/hash checks, GitHub Release creation, OpenUPM publication and same-tarball-byte verification. Tag commit `c96227afccdb510b9d6bf5603d58f2ea8d126292`; asset SHA-256 `ce86cff5eb8e4da3964ef8eab124042794cba9ecebc44c464881d4967013dd2e`. |

**Gate A — integrated plan approval:** **GO — 2026-10-13**, for exact plan blob `628f64b2878cdad9fbf8d89392e8e39725b6fe3a`; see the versioned approval record. Implementation/certification was authorized conditionally. **Phase 5 Stable publication GO:** the Project Owner explicitly approved `v1.1.0` on 2026-10-14, derived from Preview 11. That decision authorized the tag-triggered Stable release workflow; the subsequent build and publication were verified as recorded below.

### Phase 5 execution — Stable publication completed (2026-10-14)

The stable tag `v1.1.0` resolves to reviewed source commit
`c96227afccdb510b9d6bf5603d58f2ea8d126292`. GitHub Actions run #20 attempt 2
completed with `success`; both `Build immutable signed candidate` and
`Approve and promote the certified artifact` succeeded. The promoted GitHub
Release is non-prerelease and contains `com.immersive.framework-1.1.0.tgz`
(1,832,824 bytes; SHA-256
`ce86cff5eb8e4da3964ef8eab124042794cba9ecebc44c464881d4967013dd2e`).
OpenUPM reported `com.immersive.framework@1.1.0` as published and signed; the
workflow's follow-up registry fetch confirmed it served the same tarball bytes.
The stable tag has no `Runtime/` or
`Tests/` changes relative to Preview 11, whose consumer import/compile and
composed QA evidence are recorded in Phase 4. No new Unity run was needed for
the metadata-only stable release. The workflow's pack/sign and provenance
steps are recorded as pipeline evidence; independent cryptographic signature
verification is not claimed.

Phase 5 is **Closed**. All five IF-ADR-048 phases are closed and the Stable
1.1.0 release mission is **Complete**.

### Implementation-only continuation — I-08 Activity Observer ownership (2026-10-14)

The Project Owner supplied a later IF-ADR-042 terminal reporting **BLOCKED
0/6** before the first checkpoint. Its diagnostics showed an available Route
Player Session observation, one registered Output, one occurrence, and an
available Activity Observer, but did not expose the specific startup-readiness
predicate that remained false. This is a blocked scenario result, not a Shared
Group contract failure or a PASS.

The QAFramework scenario now resolves its Activity Observer only by enumerating
the loaded scenes explicitly declared by the startup Activity content profile;
it no longer searches every loaded Unity object. Observer cardinality, scope,
enabled state and declared-scene load state are included in the causal
diagnostic. The IF-ADR-042 README now matches the six implemented checkpoints
and no longer claims that this consumer scenario performs prepared-Actor
replacement, which I-08 excludes from public consumer QA. Package-level
Actor-preparation coverage remains pending.

No Unity import, Test Runner or Play Mode execution was performed for this
implementation-only continuation, at the Project Owner's direction to defer
tests. The source/doc changes alone did not close I-08 or any Phase 2 group;
the subsequently supplied PASS 6/6 terminal is recorded in the direct Play
Mode follow-up below. Phases 3–5 remain pending and publication remains
**NO-GO**.

### Wave 1 — implementation and package EditMode evidence (2026-10-13)

I-01, I-06 and I-07 implementation changes are present in this working source
cut. QAFramework `main` at `669b0e93ef63eadb6babd4a76498364f3319ef44`
resolved `com.immersive.framework` from the local `file:` package at
`1.1.0-preview.8`; Unity Editor was `6000.5.0f1`. The complete QAFramework
EditMode run passed **339/339** (0 failed, 0 skipped; 19.2355078 seconds):
`SessionCompositionReleaseTests` 3/3, `ProgressionSavePayloadTests` 8/8,
`FrameworkIdentityConformanceTests` 14/14 and
`FrameworkApiStatusMetadataTests` 5/5. The focused I-01 ledger test passed
1/1. Raw results: `QAFramework/Temp/IF-ADR-048-I01-contract.xml` and
`QAFramework/Temp/IF-ADR-048-wave1-final.xml`; runner logs use matching `.log`
names.

The I-01 test expectation was aligned with the approved contract after a
focused run showed that it incorrectly expected successful local rollback to
clear the Session ledger. The exact approved plan and IF-ADR-040 Amendment 001
retain every attempted participant until a successful aggregate Session
release; that release may revisit participants already released by rollback.
The Runtime was not changed for this expectation correction. The aggregate
release test now proves this retention and that a duplicate successful
release is idempotent.

Working source snapshot SHA-256 (sorted, non-ignored `Runtime/`, `Tests/` and
`package.json` file manifest):
`176de626ced99bfed0c0fc44c7bd428bac0d84885581fc56e6b84c1482a46b7a`.
This identifies the package source/test snapshot, not a commit or immutable
release candidate. Wave 1 package EditMode evidence passes; the full Host
controlled-stop/retry integration remains pending I-03. QAFramework was not
modified by this run, and no Phase 2 group is closed by this checkpoint.

### Wave 2 — I-02 Stable scoped release and physical receipts (2026-10-13)

I-02 package changes preserve scoped ownership until release callbacks and
physical Input projections are confirmed. `PlayerSessionScopedBindingRecipient`
remains retryable after a rejected release; `ActiveScopedBinding` now releases
the recipient before disposing its binding and lease, retaining both on
failure. Availability consumers retain their block token and report access as
unavailable while cleanup is pending. The runtime host retains its occurrence
token ledger and the Input context retains failed slot/adapter projection
receipts until physical restoration succeeds. The Input Gate adapter now
rejects an unavailable Gameplay Action Map instead of inferring successful
restoration from the inability to inspect that map. Pause/Input release
continues to retain its existing restoration receipt for retry.

QAFramework `main` at `669b0e93ef63eadb6babd4a76498364f3319ef44` resolved the
working package at `1.1.0-preview.8` with Unity Editor `6000.5.0f1`. Focused
EditMode evidence: `PauseProductTransactionTests` 19/19 and
`PlayerSessionJoinBindingTests` 11/11. The complete EditMode suite passed
**342/342** (0 failed, 0 skipped; 26.0337426 seconds). Raw results and runner
logs: `QAFramework/Temp/IF-ADR-048-I02-pause-green.xml` and `.log`,
`QAFramework/Temp/IF-ADR-048-I02-player-participation.xml` and `.log`, and
`QAFramework/Temp/IF-ADR-048-wave2-I02-final.xml` and `.log`. The targeted
physical restoration failure test proves the host retains both the occurrence
token ledger and projection receipt on failure, then clears them only after a
successful retry. QAFramework's Unity-mutated Standalone define was restored;
its worktree is clean. No QAFramework consumer migration is included in I-02.

Working source snapshot SHA-256 (sorted, non-ignored `Runtime/`, `Tests/` and
`package.json` per-file SHA-256/path manifest; 2,028 files):
`fc918370f608e6f14eae165911043eb19862c677958566e7b9d8f3967d8b42af`.
This identifies the working package source/test snapshot at the I-02 checkpoint,
not a commit or immutable release candidate. I-02 package EditMode evidence
passes; the I-08 migration and its current execution evidence are recorded
below. No Phase 2 group is closed by this checkpoint.

### Wave 2 — I-08 Observer/Command QA migration (2026-10-13)

The inherited `PlayerSessionScopedAccessConsumer.TryGetAccess` helper is now
protected, and the inspected QA consumers use the existing Join/Open/Close/
Leave/Availability triggers plus Observer reads. Raw stale/foreign/multiple
token probes remain with the Availability owner NUnit tests; no new trigger was
added for low-level token operations or prepared-Actor replacement. The QA
fixtures were regenerated successfully by the existing setup methods, including
the two independent IF-ADR-044 availability triggers. Temporary setup runner
files were removed after generation.

The complete QAFramework EditMode suite passed **345/345** (0 failed, 0
skipped; Unity Editor `6000.5.0f1`). New `PlayerSessionAccessGovernanceTests`
cover derived-only helper access, stale Leave revision rejection after same-slot
rejoin, and foreign/stale/multiple Availability token ownership and release.
Raw result and runner log: `QAFramework/Temp/Immersive-Framework-I08-EditMode-final.xml`
and `.log`. Setup runner output is
`QAFramework/Temp/Immersive-Framework-I08-QA-Setup-staging.log`; all nine
fixture setup markers reported `complete=true`.

At the 2026-10-13 checkpoint, I-08 runtime QA remained BLOCKED because its
migrated Play Mode scenarios had not been executed. The QAFramework project has
no automated Play Mode test lane for these scenario MonoBehaviours; fixture
setup and EditMode tests do not substitute for their runtime terminals.

#### I-08 direct Play Mode follow-up — 2026-10-14

Unity `6000.5.0f1` was started directly from the installed Editor against
QAFramework, using fresh Play Mode sessions and the exact scenario scenes. The
Framework source was resolved from the local package at HEAD
`861620ca00b662a31d3542ff79b5c1f632d843ee` with a dirty working tree; QAFramework
was at HEAD `669b0e93ef63eadb6babd4a76498364f3319ef44`, also dirty. These runs
are not immutable-candidate evidence.

- IF-ADR-036 completed **PASS 8/8**, with `cleanup='BaselineRestored'`:

  ```text
  [IF-ADR-036] status='Passed' verdict='PASS' cases='8/8' requestBAccepted='True' requestBCompleted='True' bOccurrence='2' cleanup='BaselineRestored' firstDivergence='' cleanupIssue=''.
  ```

- IF-ADR-042 completed **PASS 6/6** with `BaselineRestored` in a direct Play
  Mode run after the earlier user-supplied `Blocked 0/6` result. The terminal
  line was observed in this task. The Project Owner has now supplied its full
  terminal output in the conversation; it reports `targetMembers='0'`, blank
  `execution` and `cleanupIssue`, plus the assignment/output/occurrence
  identifiers shown below. The supplied line supplements the session-level
  evidence; no raw Editor log/XML, execution timestamp, or immutable package
  identity accompanied it, so it does not establish candidate certification.

  ```text
  [IF-ADR-042-SHARED-GROUP] status='Passed' verdict='PASS' cases='6/6' next='' completed='6' missing='' assignment='c1de6b74d45a4b04bff462646fbbb5c3' output='04200000000000000000000000000001' occurrence='c1de6b74d45a4b04bff462646fbbb5c3/04200000000000000000000000000001' targetMembers='0' cleanup='BaselineRestored' execution='' cleanupIssue=''.
  ```

  A fresh repeat on 2026-10-14 did not enter Play Mode:
  the directly launched Editor could not connect to `LicenseClient-renat`,
  asserted during licensing/WMI initialization, and exited before either the
  temporary runner or scenario emitted a marker. After the Project Owner said
  the Hub was activated, a second attempt with explicit `-hubIPC
  -licensingIpc LicenseClient-renat` arguments produced the same refusal and
  exited before a scenario marker. These are environment blocks, not scenario
  results. The settings asset remained byte-for-byte at its pre-run SHA-256
  `80263DC37583C0D0039308106EC1A824C64D8428A4BCDDD03006EF1CC8DBAF76`.
- IF-ADR-043 initially booted but emitted **BLOCKED 0/7** because the two
  Route Leave triggers had no serialized Player Slot references. The setup had
  loaded P1/P2 wrappers before creating Persistent Content with
  `NewScene(Single)` and then reused those invalidated wrappers. Reacquiring the
  profiles after that scene transition fixed serialization; setup validation now
  requires exactly two enabled Route Leave triggers bound to the exact Slots.
  The corrected fresh Play Mode run completed **PASS 7/7** with
  `BaselineRestored` and no frame without a participating camera:

  ```text
  [IF-ADR-043] status='Passed' verdict='PASS' cases='7/7' next='<none>' completed='7' missing='<none>' assignment='0303ca8e5a7b44758de8f7632f8f051b' framesObserved='10' framesWithoutCamera='0' firstFrameWithoutCamera='-1' execution='' unwind='public-leave-requests' cleanup='BaselineRestored' cleanupIssue=''.
  ```

- IF-ADR-044 initially exposed stale P1/P2 asset wrappers after setup changed
  scenes. Reacquiring both Profiles after each `NewScene(Single)` fixed the
  serialized Route Leave Slot references. The scenario also now authors two
  distinct Activity Availability Block triggers for P1, matching the plan's
  independent-consumer case. The corrected setup and fresh runtime completed
  **PASS 8/8**, `cleanup='BaselineRestored'`.
- IF-ADR-046 I4 minimal completed **PASS 1/1**,
  `cleanup='BaselineRestored'`. I4.2 OBSERVABLE reported **PASS 8/8**,
  `cleanup='BaselineRestored'`, for its eight named lifecycle/current-binding
  cases. Its supplementary `stalePublicCalls='3/3'` is the same three calls
  reported by the separate I4.2 LEASE lane, not independent lease evidence.
  Those calls returned `RejectedUnavailable` with no observed effects because
  Unity's destroyed-object guard ran before lease validation. The Lease lane
  remains **BLOCKED 0/3**, as already documented in the I4.2 specification and
  Player Join certification README. The current QAFramework source correction
  removes the three retired-trigger probes from the integrated occurrence
  fixture and stops emitting the duplicate Play Mode LEASE terminal. Its eight
  Observable cases now cover current Join/Leave and lifecycle/reentry assertions.
  The separate Framework EditMode `PlayerSessionJoinBindingTests` result (11/11
  in the Wave 2 record) is unchanged. Preserve the old I4.2 LEASE `BLOCKED 0/3`
  result as historical; this correction does not relabel it PASS. The Project
  Owner supplied a fresh terminal from the revised fixture, reporting PASS 8/8
  and `BaselineRestored`. This user-reported run has no attached Unity version,
  raw log or source SHA, so it validates the reported scenario behavior but is
  not candidate-specific evidence:

  ```text
  [QA-IF-ADR-046-I4.2-OBSERVABLE] scenario='IF-ADR-046-I4.2-SCOPED-BINDING-OBSERVABLE' status='Passed' verdict='PASS' cases='8/8' caseResults='activity-a1-current=PASS | route-a1-current=PASS | activity-a-reentry-new-occurrence=PASS | activity-a2-current=PASS | route-a-reentry-new-occurrence=PASS | route-a2-current=PASS | owner-route-change-recreates-activity-occurrence=PASS | activity-a-after-owner-route-change-current=PASS' activityOccurrences='activity-a1-to-a2=1->3 sceneHandle=568105589213751250->568105589213751200 | owner-route-activity-a3-to-a4=7->11' routeEntry='routeAssetSame='True' sceneHandle='568105589213751328->568105589213751174' unloadObserved='True' unloadedHandle='568105589213751328' reloadObserved='True' reloadedHandle='568105589213751174' routeSceneReloaded='True' initialLifecycleEntered='True' currentLifecycleActive='True' currentLifecycleContextAvailable='True' currentLifecycleEntered='True' currentLifecycleAssetSame='True' currentLifecycleContributionSame='True' lifecycleInstanceChanged='True' routeJoinInstanceChanged='True' activityObserverChanged='True' activityOccurrence='3->7' activityOccurrenceAdvanced='True' routeOccurrenceOrdinal='not-exposed-by-public-contract'' currentJoin='activity-a1-current:Joined/Admitted | route-a1-current:Joined/Admitted | activity-a2-current:Joined/Admitted | route-a2-current:Joined/Admitted | activity-a-after-owner-route-change-current:Joined/Admitted' cleanup='BaselineRestored' cleanupIssue='' firstCause=''
  ```
- Player Session Join completed **PASS 8/8** with
  `cleanup='BaselineRestored'`; invalid Join preserved revision, valid Join
  selected/prepared/materialized its Actor without admitting gameplay, and
  Leave released provisioning and committed the terminal result.
- Player Leave/Rejoin completed **PASS** with two distinct occurrences for the
  same Slot; both Join and Leave cycles succeeded and cleanup was
  `BaselineRestored`. Player Multiplayer Ownership completed **PASS**: P1/P2
  joined independently, P2 survived P1 Leave and rejoin, both final Leaves
  succeeded, and cleanup was `BaselineRestored`.
- These successful runs were observed in this task, but raw Editor logs/XML and
  immutable source identities were not retained. The user separately supplied
  the QAFramework EditMode Test Runner result **357/357 passed, 0 failed**; its
  assembly list includes Framework EditMode tests. That screenshot has no raw
  XML/package source hash and does not replace the scenario Play Mode terminals
  or immutable-candidate provenance.

#### Project Owner supplied terminal batch — 2026-10-14

The Project Owner subsequently supplied complete terminal lines for IF-ADR-036,
IF-ADR-043, IF-ADR-044, IF-ADR-046 I4 minimal, and the Player Session Join,
Leave/Rejoin, and Multiplayer Ownership scenarios. All report PASS and
`BaselineRestored`; IF-ADR-043 reports `framesWithoutCamera='0'`. The two
identical Leave/Rejoin lines in the message are one terminal result, not two
independent runs. IF-ADR-042's separately supplied PASS 6/6 remains recorded
above. These are user-supplied terminal records; the batch did not include the
Unity version, resolved Framework source identity, raw Editor log/XML, or
preceding causal diagnostics.

The newly supplied terminal details not already reproduced above are:

```text
[IF-ADR-044] status='Passed' verdict='PASS' cases='8/8' next='none' completed='8' missing='none' execution='' unwind='public-leave-requests' cleanup='BaselineRestored' cleanupIssue=''.
[QA-IF-ADR-046-I4] scenario='IF-ADR-046-I4-MINIMAL' scope='Activity' owner='QA IF-ADR-046 Activity A' cases='1/1' verdict='PASS' firstCause='' outcome='Joined' actorPrepared='False' gameplayAdmitted='False' sessionFinal='revision:7,available:1,reserved:0,joined:0,leaving:0,joiningOpen:False' sessionRestored='True' playerInputBaseline='0' playerInputFinal='0' keyboardRemoved='True' cleanup='BaselineRestored' cleanupIssue=''
[QA-PLAYER-LEAVE-REJOIN-OCCURRENCE][terminal] verdict='PASS' slot='PlayerSlot:player.1' actorA='Session:Session:568105589213753644:player-actor-content:3b9d3d2de5be429a8fa7250841ab7550:Session:568105589213753644:player.1:1' actorB='Session:Session:568105589213753644:player-actor-content:3b9d3d2de5be429a8fa7250841ab7550:Session:568105589213753644:player.1:2' joinA='Joined' leaveA='SucceededLeft' joinB='Joined' leaveB='SucceededLeft' cleanup='BaselineRestored' firstCause='' cleanupIssue=''
[QA-PLAYER-MULTIPLAYER-OWNERSHIP][terminal] verdict='PASS' stage='cleanup-leave' joinA='Joined' joinB='Joined' leaveA='SucceededLeft' rejoinA='Joined' leaveFinal='not-run,SucceededLeft,SucceededLeft' simultaneousIsolation='True' p2AfterLeave='True' p2AfterRejoin='True' cleanup='BaselineRestored' firstCause='' cleanupIssue=''
[QA-PLAYER-SESSION-JOIN] status='Passed' verdict='PASS' cases='8/8' next='' completed='8' missing='' invalidJoinStatus='RejectedInvalidRequest' invalidRevisionPreserved='True' joinStatus='Joined' joinSlotMatched='True' actorSelected='True' actorPrepared='True' actorMaterialized='True' gameplayAdmitted='False' leaveStatus='SucceededLeft' leaveProvisioningReleased='True' leaveTerminalCommitted='True' finalSession='initialized:True,joined:0,available:1,reserved:0,leaving:0,joiningOpen:False' cleanup='BaselineRestored' execution='' cleanupIssue=''.
```

The Project Owner next supplied the IF-ADR-046 Occurrence Binding log. Its boot
diagnostic identifies `QA IF-ADR-046 I4 Occurrence Binding`, startup Route
`I4_Binding_Route_A`, and Activity `QA IF-ADR-046 I4 Binding Activity A`,
confirming that the correct `GameApplication_I4_ScopedBinding` fixture ran.
The OBSERVABLE lane passed 8/8 and restored baseline. The separate LEASE lane
was `BLOCKED 0/3` because destroyed-Unity-object guards returned
`RejectedUnavailable` before lease validation; cleanup was also
`BaselineRestored`. The `stalePublicCalls='3/3'` telemetry printed on the
OBSERVABLE terminal refers to those same three calls; it confirms no observed
side effects, but does not independently prove stale-lease rejection. The
attachment contains the full log; its terminal lines are preserved as the
historical output from before the reporting correction:

```text
[QA-IF-ADR-046-I4.2-OBSERVABLE] scenario='IF-ADR-046-I4.2-SCOPED-BINDING-OBSERVABLE' status='Passed' verdict='PASS' cases='8/8' caseResults='activity-a1-current=PASS | route-a1-current=PASS | activity-a1-b-a2=PASS | activity-a2-current=PASS | route-a1-b-a2=PASS | route-a2-current=PASS | owner-route-change-invalidates-old-activity=PASS | activity-a-after-owner-route-change-current=PASS' activityOccurrences='activity-a1-to-a2=1->3 sceneHandle=568105589213752216->568105589213752166 | owner-route-activity-a3-to-a4=7->11' routeEntry='routeAssetSame='True' sceneHandle='568105589213752294->568105589213752140' unloadObserved='True' unloadedHandle='568105589213752294' reloadObserved='True' reloadedHandle='568105589213752140' routeSceneReloaded='True' initialLifecycleEntered='True' currentLifecycleActive='True' currentLifecycleContextAvailable='True' currentLifecycleEntered='True' currentLifecycleAssetSame='True' currentLifecycleContributionSame='True' lifecycleInstanceChanged='True' routeJoinInstanceChanged='True' activityObserverChanged='True' activityOccurrence='3->7' activityOccurrenceAdvanced='True' routeOccurrenceOrdinal='not-exposed-by-public-contract'' currentJoin='activity-a1-current:Joined/Admitted | route-a1-current:Joined/Admitted | activity-a2-current:Joined/Admitted | route-a2-current:Joined/Admitted | activity-a-after-owner-route-change-current:Joined/Admitted' stalePublicCalls='3/3' staleNoEffects='observed' staleEvidence='activity-a1-after-a-b-a:RejectedUnavailable,revision=13->13,destroyedUnityObject=True | route-a1-after-a-b-a:RejectedUnavailable,revision=19->19,destroyedUnityObject=True | activity-a-after-owner-route-change:RejectedUnavailable,revision=25->25,destroyedUnityObject=True' leaseValidation='separate-blocked-result' cleanup='BaselineRestored' cleanupIssue='' firstCause=''
[QA-IF-ADR-046-I4.2-LEASE] scenario='IF-ADR-046-I4.2-SCOPED-BINDING-LEASE' status='Blocked' verdict='BLOCKED' cases='0/3' blockedChecks='3/3' details='activity-a1-after-a-b-a: destroyed-object guard returned RejectedUnavailable before lease validation | route-a1-after-a-b-a: destroyed-object guard returned RejectedUnavailable before lease validation | activity-a-after-owner-route-change: destroyed-object guard returned RejectedUnavailable before lease validation' cleanup='BaselineRestored'
```

The required I-08 consumer scenario terminal set is now present in the
Project Owner's supplied results. The eight named I4.2 OBSERVABLE cases remain
reported PASS; the three repeated stale calls are not an independent pass and
do not establish lease validation. The I4.2 LEASE lane remains BLOCKED 0/3.
I-08 still requires the execution source identity to be tied to these
user-supplied logs.

The earlier agent-launched Unity attempt ended before Play Mode because of
Licensing Client IPC; this is an environment block, not an IF-ADR-042 failure.
The settings asset was unchanged during that earlier attempt. A prior full
worktree `git diff --check` reported whitespace in Unity `.meta`/scene YAML and
generated `obj` metadata; the tracker-only check passes. I-08 migration and
API Governance EditMode evidence are present (including the user-reported
357/357 QAFramework suite). The Project Owner supplied terminal lines are now
retained above, including I4.2 OBSERVABLE PASS 8/8. Raw Editor logs/XML, the
Unity version and exact executed Framework source identity are still missing.
I-08, dependent workpackage certification and Phase 2 remain open. Release
remains **NO-GO**.

### Wave 3/4 — I-03/I-04 implementation checkpoint (2026-10-13)

The controlled Host stop now routes joined/leaving Slots through canonical
Session Player Leave, clears an active Activity through GameFlow after Player
release, clears Session Camera Assignments, restores PlayerInput camera and
Output participation, and tears down the Output topology before releasing
Persistent Content. A pre-commit rejection keeps the same Host and owners
available for retry. Activity projection now follows `DestinationAuthoritative`
after committed clear failures, matching Route and Activity request projection.

Camera Assignment Clear/Replace now transfers committed Occurrences into an
exact pending-cleanup ledger before releasing Subject fallback ownership or
destroying roots. Controlled Host stop waits for that ledger to drain. Output
Session, topology and materialization teardown now return failures and retain
their live owners for retry; Player Camera integration retains exact
PlayerInput/Output receipts until physical cleanup is confirmed. The Editor
regression `ControlledStopClear_ReleasesEveryActiveAssignmentBeforeOwnerDisposal`
was added, but has not executed on this source cut.

I-04 now has a canonical shutdown-owned Route exit on the existing Route
Lifecycle owner. The Project Owner's 2026-10-13 ruling preserves the Primary
Scene loaded and active, releases Framework composition scopes, and unloads
only owned additive Route scenes; the decision is recorded in IF-ADR-001
Amendment 001-A and the Gate A v2 record. Route content callbacks, spatial
entry, Resettable/Stable Object Binding owners, additive release, scene-scope
release and Route root removal retain stage receipts for retry. Route authority
and `RouteExitedEvent` clear only after blocking stages complete. The Host
regression now asserts that a later Persistent Content rejection retains no
Route authority and preserves the exact active Primary Scene. Unity execution
remains pending.

The current Route-exit source cut statically compiled all 920 core Runtime C#
sources against 305 existing references with exit 0; `Runtime/Audio` was
excluded per its separate asmdef. The `PersistentContent.TestComponents` probe
and `FrameworkRuntimeHostControlledStopTests` Editor fixture also statically
compiled with their declared assembly identities. This is compiler evidence
only: NUnit, Unity Editor/Play Mode and QA scenarios were not executed, so no
runtime PASS is claimed.

Pause cleanup is already integrated into canonical Activity exit. The controlled
stop Editor regression now pauses at a non-default `Time.timeScale`, forces a
later Persistent Content release rejection, and asserts that Pause is Running
and the captured scale is restored before the same-Host retry. This regression
has not executed because Unity Licensing IPC is unavailable.
I-05's Activity-clear projection correction is implemented from the explicit
post-commit result path, but its integrated fault-injection regression remains
unexecuted; I-05 is not closed.

Static C# compilation of the current core Runtime sources passed using the
available MSBuild compiler and Unity `6000.5.0f1` reference assemblies; the
separate `Runtime/Audio` assembly was excluded according to its asmdef boundary.
`git diff --check` returned exit 0 in Framework and QAFramework. The existing
generated Rider `.csproj` was stale (229 of 1,065 source paths were absent), so
it is not a valid project-level result; the temporary static project filtered
those paths and compiled the current Runtime inventory instead. Unity Editor,
EditMode, Play Mode and QA scenarios were not executed for this checkpoint.
The latest Unity batch attempt is BLOCKED by Licensing Client IPC refusal; do
not count earlier 351/351 Editor results as validation of these later changes.

I-03/I-04/I-05 remain open and no Phase 2 group is closed. I-04 still needs
Unity execution of its Route-exit and retry regression; integrated retry
fixtures, fresh package NUnit execution and QA/consumer evidence remain
outstanding. Release stays **NO-GO**.

### Follow-up status and provenance check (2026-10-13)

The previously referenced Wave 1/Wave 2/I-08 NUnit XML and runner-log files are
not present at their recorded `QAFramework/Temp/` paths in the current
workspace. The counts above remain historical execution records from that
checkpoint; this check did not reproduce them, and they are not candidate-
specific evidence. Recover the originals or rerun the required lanes in an
authorized Unity session before using them to close a current gate.

Current consumer resolution is not an immutable-candidate certification:
QAFramework resolves the local Framework checkout (`file:`) whose package
metadata is still `1.1.0-preview.8`; FirstGame resolves OpenUPM
`1.1.0-preview.8`. PEGA exists at the mission path, but this session cannot
read its Git state or package resolution because access is denied. No consumer
has been shown to import the same immutable 1.1.0 candidate bytes. Phase 4
therefore remains pending.

This follow-up is status/provenance only. It does not change the approved plan,
reclassify historical counts, close I-01 through I-08, close Phase 2, or alter
the release disposition. No Unity execution was attempted in this verification:
the workspace `AGENTS.md` prohibits Unity build, tests, Play Mode, smoke and
batchmode. Runtime evidence therefore remains BLOCKED here.

### Execution follow-up — test fixture corrections and environment block (2026-10-13)

The focused Unity GUI result at
`QAFramework/Temp/IF-ADR-048-I03-fullstartup-gui.xml` and `.log` is **0/1**:
`FullStartupAndControlledStop_RetainExactRootsAndSameHostForReleaseRetry`
reached `FrameworkRuntimeHost.TryCreate` outside Play Mode, where Unity rejected
`DontDestroyOnLoad`. The fixture had enabled `DisableDomainReload` and
`DisableSceneReload` while yielding the default `EnterPlayMode` instruction
(`expectDomainReload: true`). The controlled-stop fixture now sets
`expectDomainReload: false` and asserts Play Mode before creating the Host. Its
scene builder also moves each participant root into the exact scene it saves;
the previous source did not explicitly do so. These are fixture corrections,
not verified Runtime fixes. The route-scene release rejection reported by the
preceding full-suite run is not reproducible from retained XML in this
workspace.

A later full QAFramework EditMode attempt against Unity `6000.5.0f1` terminated
before a test result was produced: Unity logged Licensing IPC refusal,
`Licensing initialization failed after 115.39s`, WMI assertions and a native
crash. Process exit was `-536870911`; the requested
`QAFramework/Temp/IF-ADR-048-final-editmode.xml` was not created. It is not a
PASS or a test failure. The earlier 353/355 count exists only in the prior
session transcript; its raw XML/log is absent from `QAFramework/Temp/` and it
cannot close a current gate.

I-05's post-commit Activity-clear result already carries destination authority
in the current source. Two focused NUnit cases were added under
`Tests/Editor/RouteLifecycle/ActivityCommitAuthorityProjectionTests.cs` for
post-commit no-Activity projection with Route/Session preservation and for
pre-commit non-authority. They have not compiled or executed in Unity here;
the broader I-05 GameFlow fault-injection/QA path remains unproven. The Host
fixture corrections and I-05 tests are therefore pending Unity validation.

No I-01–I-08 workpackage or Phase 2 group is closed by this update. Wave 1 and
I-02 implementations remain associated only with the historical counts above;
their raw results are still unavailable. I-08 QA migration is present, but its
required runtime scenarios remain BLOCKED. I-03/I-04 controlled shutdown and
integrated teardown remain BLOCKED on Unity/NUnit/QA evidence; I-05 remains
open pending fault-injection evidence. Phases 3–5 and publication remain
pending/NO-GO; no immutable candidate or same-artifact consumer certification
exists.

### User-run focused Unity follow-up — controlled stop and I-05 fixtures (2026-10-13)

The Project Owner supplied focused Test Runner output. The I-05 post-commit
projection test stopped in fixture construction because its in-memory
`ActivityAsset` had no valid `ActivityId`; the assertion did not reach state
projection. The Host controlled-stop output also showed that the Route-release
rejection probe was authored in the Route scene, while IF-ADR-040 discovers
dynamic participants from Persistent Content roots and retains them for managed
Scene scopes. The injected rejection therefore did not run. Its teardown then
called `StopAsync` again after the Host had already reached `Stopped`, and the
runtime retried cleanup against a terminal admission phase.

The test fixture now assigns valid Route/Activity IDs, puts the Route-release
probe under Persistent Content, asserts that the generated Game Application
retains a resolvable Startup Route/Primary Scene, and expects the intentional
controlled-stop rejection log. `FrameworkRuntimeHost.StopAsync` now returns
success for an already stopped Host, preserving repeated-stop idempotence. These
changes have not been rerun in Unity. The separately supplied
`FullStartupAndControlledStop...` output reported a Startup Primary Scene
preparation error with an empty scene label; the new asset assertions should
localize whether that is fixture resolution or runtime behavior on rerun.

`git diff --check` returned exit 0 after these edits. No Unity compilation,
NUnit rerun, Play Mode scenario, or QA evidence is claimed. I-03/I-04/I-05
remain open and the release remains NO-GO until the focused tests and required
runtime evidence pass on this source cut.

### User-run focused rerun — participant callback counts and Route fixture (2026-10-13)

The Project Owner supplied Unity Test Runner output for **354/357 passed**.
Both `ActivityCommitAuthorityProjectionTests` passed. The controlled Route
release test now reached its injected rejection and retry; its remaining failure
was the assertion expecting one callback although all three Persistent Content
participants correctly received the managed Scene release. The startup-failure
test likewise expected two participant callbacks after the fixture added the
third Persistent Content participant. Assertions now count the complete
three-participant callback set and separately verify the `route-scene` probe's
own first-attempt/retry calls.

`FullStartupAndControlledStop...` failed during `SetUp` because the generated
Route asset did not retain its Primary Scene path. The fixture now explicitly
marks SerializedObject edits dirty, checks the path immediately after authoring,
and verifies the reloaded Route asset after save/import. These changes are
unverified. The screenshot also showed **354 passed, 3 failed** overall; no
aggregate PASS is claimed. I-03/I-04/I-05 and Phase 2 remain open; release stays
NO-GO pending another focused Unity run and the required integrated scenarios.

### User-run follow-up — Route serialization and release-scope fixture (2026-10-13)

The Project Owner's next Unity run confirmed that `RouteAsset.PrimaryScenePath`
was correct immediately after authoring but empty after the fixture's broad
`SaveAssets`/`Refresh` cycle. The attached `FullStartup...` log also showed the
`second` probe rejecting the managed Scene release before the test reached its
intended Persistent Content Session-release failure. These are fixture/setup
issues; no Runtime behavior change is indicated by this output.

The Host fixture now serializes the Route fields together, saves the Activity,
Route and Game Application assets individually, imports the Route and
Application explicitly, and checks the serialized Route file before import as
well as the path and Activity reference after import. The scene save result is
asserted. Release rejection is now scoped: `second` rejects Session release,
while `route-scene` rejects managed Scene release. The existing `LogAssert`
expectations therefore exercise their intended shutdown stage.

`git diff --check` returned exit 0; it reported only existing LF-to-CRLF
conversion warnings. Unity compilation and NUnit rerun remain pending, so these
fixture corrections are unverified. I-03/I-04/I-05 and Phase 2 remain open;
release remains NO-GO.

### User-run confirmation — QAFramework EditMode suite (2026-10-13)

The Project Owner supplied a Unity Test Runner screenshot showing the full
QAFramework EditMode suite at **357/357 passed, 0 failed**. The
`Immersive.Framework.PersistentContent.Editor.Tests.dll` (24 tests) and
`Immersive.Framework.RouteLifecycle.Editor.Tests.dll` (8 tests) groups are
green in the screenshot, including the previously failing controlled Route
release/retry and startup/shutdown fixture groups. The expanded
`ActivityCommitAuthorityProjectionTests` node also shows both cases green.
This supersedes the prior 354/357 report for these test groups and confirms
their EditMode rerun passed; it does not close the corresponding workpackages.

This is user-reported execution evidence from the screenshot only: no raw XML,
runner log, exact package source hash, or candidate identity is attached. It
confirms this EditMode run and does not certify Play Mode scenarios, immutable
candidate provenance, consumer validation, or Phase 2 exit. I-03/I-04/I-05
remain open pending their remaining integrated evidence; release remains
NO-GO. The next execution dependency is Wave 2 I-08 runtime QA: run the
migrated IF-ADR-036, IF-ADR-042, IF-ADR-043, IF-ADR-044, both IF-ADR-046
scenarios, and the Player Session Join, Leave-Rejoin, and Multiplayer
Ownership scenarios in fresh Play Mode sessions; retain each complete terminal
line and preceding causal diagnostics. I-08 remains BLOCKED until these
user-run terminals are reviewed.

### Batchmode Play Mode attempt — environment BLOCKED (2026-10-13)

At the Project Owner's request, Unity `6000.5.0f1` was invoked against
QAFramework with `-batchmode -runTests -testPlatform playmode`. The first
attempt was rejected because the project was open in another Unity instance.
After the Project Owner closed that instance, the retry entered Unity startup
but did not reach test execution: the Licensing Client IPC channel was refused,
initialization timed out after 60 seconds, WMI assertions followed, and the
Editor crashed with exit code `-536870911`. The runner log is
`QAFramework/Temp/IF-ADR-048-playmode-batch-retry.log`; no result XML was
created. This is **BLOCKED environment**, not a fixture FAIL or PASS.

The resolved Framework test assembly definitions are under `Tests/Editor`
with `includePlatforms: Editor`; QAFramework has no separate Play Mode NUnit
assembly for the certifications listed above. Those certifications are
scene-driven QA components, not scenarios invoked by the standard
`-runTests -testPlatform playmode` lane. No such scenario was exercised by
this attempt. After the attempt, the
working tree showed `ImmersiveFrameworkSettings.asset` selecting
`GameApplication_QaIfAdr042` instead of the `QaIfAdr036` value present in the
pre-attempt status snapshot. This user/project setting was left untouched; its
change is not attributed to a test result.

## Phase 2 — Operational Integrity: initial assessment

This is an investigation and certification plan, not a Runtime defect report.
Its contract baseline is the Stable surface accepted for 1.1.0 under IF-ADR-047;
it does not introduce a generic compatibility or migration requirement for
pre-1.1.0 packages. Preserve IF-ADR-036's structural/operational readiness
split, IF-ADR-044's bounded availability contract and IF-ADR-046's bounded Join
closure and accepted evidence disposition.
Risk classes are kept separate: **Stable guarantee directly affected** means
failure could violate a published Stable contract; **indirect operational
risk** means a dependency or integration can impair the experience without
changing that contract; **Experimental-only gap** is outside the Stable claim
unless evidence shows otherwise; **post-release improvement** is not required
to satisfy the current Stable boundary. Existing Unity results below are
historical or user-reported at their recorded provenance; no tests were run for
this assessment.

| Priority / Stable group | Critical paths to assess | Existing automated coverage and QA evidence | Stable guarantee directly affected | Indirect operational risk | Experimental-only gap | Post-release improvement | Verifiable Phase 2 exit evidence |
|---|---|---|---|---|---|---|---|
| **P0 — Persistent Content** (`PersistentContentComposition`; part of Application authoring) | Successful bootstrap transfers exact roots to application lifetime; failed Session composition restores source roots; release on host shutdown; partial release failure, retry and repeated cleanup; scene identity/path resolution in Player build. | `Tests/Editor/PersistentContent/SessionCompositionReleaseTests.cs` covers failure aggregation/retry and repeated release; `PersistentContentSceneReferenceTests.cs` covers path/name and migration cases. Tracker records user-reported 194 EditMode passes and QA-045 partial 6/7; `FrameworkRuntimeHost.OnDestroy → ReleaseSessionScope` after successful full bootstrap remains explicitly uncovered. | Schema/scene reference and application-lifetime retention/release behavior in IF-ADR-045. The host-shutdown integration is a **material evidence gap**, not a proven Runtime defect. | Bootstrap/teardown ordering can strand roots or source scenes even if isolated release tests pass. | Player Admission discovery and unrelated scene lookup behavior remain outside this contract; do not change IF-ADR-040. | Additional supported Player-build/platform matrix beyond the release's declared support. | A test reaches successful full host bootstrap, destroys/shuts down the host, proves exact Session scope release, root ownership and source-scene outcome; injects release failure and proves retained retry authority; repeats shutdown without double release/orphans. Validate path resolution on the supported Player target. |
| **P1 — Player Join and availability** (13 Stable Player Participation types) | Join request through canonical Session authority; pre-commit and post-commit failure compensation across Session/Host; bounded public outcome; release of occurrence-scoped availability token; Leave cleanup interaction; retry without duplicate Slot/Host/Actor ownership. | `PlayerSessionJoinContractTests`, `PlayerSessionJoinBindingTests`, `PlayerSessionJoinTriggerTests`, `LocalPlayerProvisioningBridgeRollbackTests`, `LocalPlayerJoinOutcomeProjectionTests`, and `ActivityPlayerActorLeaveLedgerTests`. Recorded user reports include 101/101 Player Participation, IF-ADR-044 8/8 with `BaselineRestored`, and IF-ADR-046’s accepted partial-provenance Join closure. | Bounded Stable Join request/outcome and exact token-specific availability block/release (IF-ADR-044/046). Preserve the accepted IF-ADR-046 evidence disposition; this assessment does not reopen it. | Experimental Session/Leave/Actor lifecycle failures can affect downstream cleanup while remaining outside the Stable Join signature. | Observer, general provisioning configuration, Leave, Actor selection/preparation and broad scoped access. Keep their current maturity and authority. | Broader Manager-Provisioned configuration/product workflow only if a concrete consumer requires it. | Targeted failure matrix proves the Stable result never reports false success, rollback evidence matches the same Session/Slot occurrence, cleanup releases only owned tokens, and retry does not duplicate admission. Record accepted IF-ADR-046 residuals separately from new candidate evidence. |
| **P1 — Camera assignment and Output** (12 Stable Camera types) | Output/rig composition failure; assignment activation; replacement candidate validation and commit; failed replacement preserving current occurrence; fallback coverage/reveal; dispose/session shutdown ordering; reentrant or repeated release. | `SessionCameraAssignmentRuntimeTests`, `SessionCameraAssignmentCompositionTests`, `CameraOutputStateTests`, `CameraOutputAuthorityBoundaryTests`, `CameraAssignmentIdentityTests`, and Player/Output integration tests. Tracker records QA-NEW-004 9/9 and IF-ADR-042/043 focused PASS evidence; it also records transactional Assignment failure paths and broader IF-ADR-038 recertification as still pending. | Stable per-Output authoring/composer and bounded Session Assignment command/consumer operations as scoped by Public-API and IF-ADR-038/039. | Camera integration can disrupt a working Output or Player camera even when the request API remains correctly bounded. | Split-screen, additional Outputs/Assignment topologies, Actor Subject and world-target surfaces beyond the accepted scope. | Additional camera layouts or presentation features without a current consumer requirement. | Inject candidate/activation/replacement/release failures and prove current Assignment/Occurrence preservation, no uncovered output gap, exact cleanup order and idempotent dispose for each Stable topology. Do not count broader Experimental topologies as required Stable coverage. |
| **P1 — Pause, Input and Gate posture** (37 Stable Gate/Pause/Input types) | Pause/Resume physical transaction; apply failure and rollback; restore failure retaining suppression/receipt; retry; binding replacement/release; adapter disable/destroy; gate recomposition and Action Map readback. | `Tests/Editor/Pause/PauseProductTransactionTests.cs` covers commit/rollback/retry/release cases; user-reported Pause EditMode 18/18. QA-IF-ADR-036 Play Mode 8/8 and IF-ADR-044 8/8 are recorded, with their specific boundaries. | Stable single-player Pause requests/results and physical PlayerInput gate posture/recovery. | A failed adapter restore can leave physical input unavailable; suppression must remain until restoration succeeds or the target ends. | Simultaneous active Pause + Transition Gate remains unsupported/uncertified; IF-ADR-036 records rejection of Pause during an in-progress transition. This is not a reason to weaken Stable Pause assertions. | Additional diagnostics/telemetry beyond status and retry evidence already required by the contract. | Fail each transaction boundary and verify logical posture, current map, enabled maps, input-active state, suppression/receipt ownership, retry, and cleanup. Confirm only supported Gate/Pause compositions are claimed; no admission/readiness changes from Gate-only changes. |
| **P2 — Application, Route/Activity authoring, GameFlow and serialized content** (6 Application/settings, including Persistent Content covered separately at P0; 9 Route/Activity authoring; 8 GameFlow) | Required boot validation and module construction; source scene load failure before/after commit; transition cancellation/supersession; post-commit failure reports actual authority; transition Gate terminal cleanup; serialized schema round-trip/unknown enum validation. | `RouteActivityTransitionObserverTests` covers observer projection, not full flow authority. `FlowTriggerCompositionTests` covers composition boundaries. QA-IF-ADR-036 and QA-NEW-002 provide reported transition compositions; tracker reports 310/310 source-cut EditMode and QA-IF-ADR-036 8/8, without immutable candidate linkage. | Stable asset schema/intention and request/event surface. Stable GameApplication configuration must fail through the documented required gates; request results must not misrepresent committed authority. | Bootstrap and scene-transition failure can prevent consumers from using Stable authoring/request contracts. | Activity readiness, Loading/Transition presentation, `TransitionGateMode` behavior and Player projection remain Experimental; preserve IF-ADR-036 semantics and do not make them Stable by this validation. | More authoring diagnostics or transition presentation conveniences outside existing guarantees. | Add/identify focused checks for serialized round-trip and invalid values; exercise request accepted/rejected/superseded and pre-/post-commit failure; verify exact committed definition, terminal Gate cleanup and no false success. Separate stable asset schema proof from Experimental behavior claims. |
| **P2 — Progression Save** (13 Stable storage contract/value types) | Read/write/delete results across success, missing record, invalid payload and backend failure; idempotent delete/retry; payload/record/slot identity and format preservation. | IF-ADR-018 records historical B5 18/18 evidence including interruption/recovery; current Framework `Tests` contains no named Progression Save suite. Static QAFramework search found no Save runtime QA; its current Application fixtures disable Save. FirstGame references a future Persistence sample in planning documentation but no active Framework Save consumer was found in the inspected runtime/assets. The historical result is not candidate-SHA evidence. Built-in JSON/profile/runtime integration remains Experimental. | Stable `IProgressionSaveStore` method/value contracts; an incorrect result mapping or payload identity could break consumers. | A consumer backend can lose or duplicate data without changing Framework runtime authority. | Built-in JSON backend, Save profile/authoring and gameplay snapshot/autosave. | Additional built-in backends and high-level save workflows. | Establish current automated contract coverage for the Stable interface: deterministic success/failure mapping, retry/idempotence, and payload/identity preservation. Keep adapter-specific persistence guarantees outside the Stable claim unless separately accepted. |
| **P2 — Maturity metadata and typed identities** (2 governance metadata + 7 identity types; validation policy types are covered with Application) | Unknown status/config values fail according to policy; identity domain and exact-definition equality remain distinct; collision handling/serialization does not silently merge or regenerate identity. | Static status/API inventory and existing authoring/Camera/Player identity tests. No independent release evidence was run in this assessment. | Stable category vocabulary/metadata surface and typed identity contracts. | Incorrect identity interpretation can corrupt Route/Activity/Slot ownership across otherwise valid lifecycles. | Internal diagnostics and Experimental Actor/Session selection semantics. | Broader property-based collision or authoring usability checks where they do not cover an existing Stable promise. | Verify public enum values and metadata meanings; exercise domain mismatch, duplicate stable IDs, exact asset reference authority and fail-closed unknown configuration without silent regeneration. |

Phase 2 remains **in progress**. Its exit requires named source-revision evidence for all Stable guarantees and material lifecycle paths; every unresolved gap that directly affects a Stable guarantee, regardless of priority label, must be closed or explicitly risk-accepted by the responsible authority under IF-GOV-001 and the owning ADR. A reported aggregate pass count alone is insufficient. Experimental-only gaps stay excluded unless a causal path to a Stable guarantee is demonstrated. No phase beyond Phase 1 is marked complete by this assessment.

## Current Framework source cut — Pause physical restoration integrity

The Framework source removes `Runtime/InputMode`, including its former Stable
contracts, and places the remaining transaction evidence inside Pause. This
cut also makes Gate block status depend on current Action Map state, bounds
automatic projection attempts, and retains Pause release ownership and exact
receipts while restoration is recoverable. The decision and consumer migration
are recorded in [IF-ADR-005](../ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md#2026-10-12-persistent-input-gate-failure-and-physical-release-amendment).
QAFramework resolves the local checkout and FirstGame resolves published
`1.1.0-preview.8`; neither has a direct InputMode source reference in the
inspected assets. The latest user-reported QA evidence for the current source
cut is recorded in the release-readiness gate above. This evidence has no raw
log or immutable candidate SHA and is not candidate-specific release
certification. Play Mode coverage outside the reported QA-IF-ADR-036 proof
remains separately scoped. PEGA resolves published `1.1.0-preview.8`; source
and GUID inspection for RESET-035-H found no removed Adapter references, but
consumer validation against this local candidate remains pending.

`PauseProductTransactionTests` coverage is reported as 18/18 for this source
cut. Activity-exit and Session Host recovery scenarios still require
Unity/QAFramework execution beyond the reported suites where applicable.

## Authority and status model

```text
Accepted ADRs    -> normative architecture
Governance       -> cross-cutting compatibility/product policy
Reconciliation   -> current technical alignment/certification
Tracker          -> current mutable delivery state
FIRSTGAME        -> Stage B real-consumer evidence
Archive          -> historical/non-authoritative execution history
```

A dated certification remains evidence for the boundary it executed. Later cuts add evidence; they do not retroactively relabel historical matrices.

## Public package distribution

The public OpenUPM graph is published:

| Package | Version | GitHub Release | OpenUPM |
|---|---:|---|---|
| `com.immersive.foundation` | `0.2.1` | [v0.2.1](https://github.com/ImmersiveGames/com.immersive.foundation/releases/tag/v0.2.1) | [package](https://openupm.com/packages/com.immersive.foundation/) |
| `com.immersive.logging` | `0.2.2` | [v0.2.2](https://github.com/ImmersiveGames/com.immersive.logging/releases/tag/v0.2.2) | [package](https://openupm.com/packages/com.immersive.logging/) |
| `com.immersive.pooling` | `0.2.1` | [v0.2.1](https://github.com/ImmersiveGames/com.immersive.pooling/releases/tag/v0.2.1) | [package](https://openupm.com/packages/com.immersive.pooling/) |
| `com.immersive.audio` | `0.2.2` | [v0.2.2](https://github.com/ImmersiveGames/com.immersive.audio/releases/tag/v0.2.2) | [package](https://openupm.com/packages/com.immersive.audio/) |
| `com.immersive.framework` | `1.0.2` | [v1.0.2](https://github.com/ImmersiveGames/com.immersive.framework/releases/tag/v1.0.2) | [package](https://openupm.com/packages/com.immersive.framework/) |

Preview **`1.1.0-preview.8`** is published to GitHub Releases and OpenUPM. Its
archive is attached to [the GitHub Release](https://github.com/ImmersiveGames/com.immersive.framework/releases/tag/v1.1.0-preview.8),
and OpenUPM resolves `.8` as the current preview. It depends on Foundation
`0.2.2`, Logging `0.2.3`, Cinemachine `3.1.7` and Input System `1.19.0`. The
`v1.1.0-preview.1` tag
stopped at signed-archive verification; no GitHub Release or OpenUPM publication
was created for it. Package import/compile and broader Camera/Player validation
remain pending.

### Preview `1.1.0-preview.8` publication record

- Source commit: `f01b0bf1c54ea2cbf8c47aac851a21355323ec83`; annotated tag
  `v1.1.0-preview.8` points to that commit.
- [Signed-release workflow run 38278260074](https://github.com/ImmersiveGames/com.immersive.framework/actions/runs/38278260074)
  completed successfully, including `upm pack`, archive content checks, GitHub
  Release upload and OpenUPM publication.
- OpenUPM resolves `1.1.0-preview.8` with repository revision
  `f01b0bf1c54ea2cbf8c47aac851a21355323ec83`. The GitHub Release asset and
  OpenUPM tarball were byte-identical: 1,714,369 bytes, SHA-256
  `6a8b268200251e536b431678acadeee05f0ae2181100a165814af8fef73efc1c`.
- Workflow verification confirmed the expected archive entries and presence
  of `package/.attestation.p7m`. The workflow does not perform an independent
  cryptographic signature-verification command; this record does not claim it
  did.
- Consumer installation validation remains **PENDING** until a Unity
  `6000.5.0f1` project confirms the exact resolved version, import/compilation
  without errors or signature warnings, and Framework initialization.

Registry dependency graph:

```text
com.immersive.framework 1.0.2
├── com.immersive.foundation 0.2.1
└── com.immersive.logging 0.2.2

com.immersive.audio 0.2.2
└── com.immersive.pooling 0.2.1
```

Distribution status: **1.1.0-preview.8 PUBLISHED / PENDING UNITY COMPILE/IMPORT VALIDATION**.

## Current Reset integration QA evidence

On 2026-10-06 the current RESET-035-B extensions were executed in Unity Play Mode. `QA-NEW-002` passed the controlled Activity readiness/rollback and owner-release path with `BaselineRestored`; `QA-NEW-003` passed Route A → B → new A with owner survival, exact lifecycle observations and `BaselineRestored`. Both ended with empty `firstDivergence` and `cleanupIssue`. This closes the integration execution gate represented by those two fixtures without relabeling older historical certifications.

## Current Reset source cut — RESET-035-H legacy Adapter removal

`UnityResetSubjectAdapter`, its independent ID/discovery/registration bridge,
and its Scene Lifecycle binding participant are removed. Resettable owner-aware
Route/Activity transactions remain the only subject registration authority;
ResetComposition, ResetTarget, ResetMembership, ResetRegistry, ResetExecutor
and existing capabilities remain. IF-ADR-035, Public-API, Reset Usage and the
Unreleased changelog record the deliberate breaking change and migration under
IF-GOV-001. Package version remains `1.1.0-preview.8`.

Static consumer inspection found no Adapter API reference or removed Adapter
component GUID in QAFramework, FirstGame or PEGA. QAFramework's one custom
`IUnityResettable` fixture was migrated off its obsolete `ResetParticipantId`
property. FirstGame's pre-existing settings/package-manager edits were
preserved. QAFramework resolves the local package checkout; FirstGame and PEGA
currently resolve registry `1.1.0-preview.8` and therefore do not compile the
candidate source cut.

The user reports Unity validation for this Adapter-removal cut: QAFramework
EditMode **306/306 PASS**, Reset EditMode **69/69 PASS**, `QA-NEW-002` Play Mode
**PASS**, and `QA-NEW-003` Play Mode **PASS**. This is manually reported
evidence; raw logs and an execution SHA were not supplied. FirstGame consumer
validation against an immutable candidate remains **PENDING**. PEGA has no
source/GUID reference in the inspected checkout, but its registry installation
likewise does not validate the new package source.

## Current Player state

The Player target architecture is defined by IF-ADR-038 for Actor composition,
physical/spatial authority and Camera Subject identity, plus IF-ADR-024 for
Manager-Provisioned prepared Actor replacement. Scene-Provided authoring validation, transient
resolution and runtime adoption remain implemented; derived evidence and Player
Apply / Rebuild are removed.

```text
Local Player Host
└── ActorMount
    └── PlayerActorRuntimeHost
        ├── PlayerActorDeclaration
        ├── Actor occurrence root / physical state
        ├── ActorCameraSubjectAuthoring / ObservationTransform
        └── optional subordinate visual content
```

Current transaction split:

```text
Join
!= Actor Selection
!= Activity Actor Preparation
!= Physical Materialization
!= Prepared Actor Replacement
```

Current prepared Actor replacement public boundary:

```text
IPlayerSessionScopedAccess.RequestReplacePreparedActor(...)
  Manager-Provisioned V1 only
  same Player Slot / Host / PlayerInput / Session / Activity occurrence
  Actor A Prepared + GameplayReady
    -> release A contextual gameplay
    -> release A occupancy by exact preparation ownership
    -> replace A -> B
    -> establish B gameplay
    -> reconcile readiness
```

Current Player Actor identity boundary:

```text
AUTHORED / UNPREPARED
  PlayerActorDeclaration.actorId = empty

→ physical preparation establishes runtime occurrence identity

IDENTITY ESTABLISHED / PREPARING
  typed PlayerActorDeclaration.ActorId is valid

→ commit

PREPARED / COMMITTED
  physical preparation evidence retained
```

`PlayerActorDeclaration.ActorId` is runtime occurrence identity. It is not a persistent prefab/template identity. Ordinary persistent `ActorDeclaration` identity rules remain separate.

Current Player evidence and authority:

```text
QA-001 Slice 1 — Session / Join                    user-reported PASS 8/8; provenance pending
QA-001 Slice 2 — Leave / Rejoin / Actor occurrence user-reported PASS; provenance pending
QA-001 Slice 3 — Local Multiplayer / Input ownership user-reported PASS; initial cleanup FAIL retained
Full Player aggregate 25/25 and 27/27             dated historical records; not current runners
Manager 14/14; ADR-024 16/16                     dated evidence for earlier boundaries
Route 18/18; Activity 23/23                       dated bounded evidence; full recertification open
```

The QA-001 results are scoped evidence supplied by the user; no raw logs or
immutable Framework SHA were attached. They do not close full Player
recertification or alter maturity. See
[IF-PLAYER-FULL-RECERTIFICATION-2026-10-11](../Reconciliation/IF-PLAYER-FULL-RECERTIFICATION-2026-10-11.md)
for the single current coverage/maturity matrix and remaining gates. Dated counts
remain historical evidence only for the boundaries they executed.

### Histórico supersedido — Player Stable readiness (2026-10-11)

Esta fotografia é histórica e foi supersedida pela decisão Stable delimitada
IF-ADR-046, registrada abaixo e na linha 046 do status atual.

Static audit baseline: Framework `.7` HEAD `039223a39abfbe3bcf1c6f37c2d7764ad60834a8`; QAFramework HEAD `f00676563b5adfdc4c66323234c60a0a733c8217`; FirstGame HEAD `98a0c23ea33f11e721afe80161fe0aba8cb7acc7`. QAFramework and FirstGame resolve the local Framework path without an immutable lock SHA. Slices 1–3 remain user-reported, partial-provenance evidence, not current SHA-bound runs.

Manager-Provisioned Join remains Experimental. Latest user-reported Unity Test Runner result is EditMode **292/292 PASS**, PlayerParticipation **101/101 PASS**, zero failures, with partial provenance and no verified execution-byte association. User-reported I4 evidence is accepted proportionally as a composite: I4 minimal **1/1 PASS**, I4.2 OBSERVABLE **8/8 PASS**, both `BaselineRestored`; the independent I4.2 LEASE lane remains **BLOCKED 3/3** because destroyed-object guards can reject before lease validation. P1 (exact identity tuple captured by the real binder) and P2 (no canonical Join delegation by a stale real port) are not directly demonstrated by tests and remain supported by static review; they are recorded as residual risks, not PASS claims. The composite closes the I4 integration gate; it does not promote Stable or certify the API's full compatibility closure. FirstGame I5 and the QA scenarios have since migrated to `PlayerSessionJoinTrigger`; their user-reported validation remains partial-provenance evidence. See the [canonical Player matrix](../Reconciliation/IF-PLAYER-FULL-RECERTIFICATION-2026-10-11.md), [IF-ADR-046](../ADRs/IF-ADR-046-Stable-Manager-Provisioned-Player-Join-Boundary.md), Amendments 001/002, approved plan v3, and the QAFramework I4 record. Do not promote `IPlayerSessionScopedAccess` or adjacent Actor/Input APIs by association.

### Histórico supersedido — IF-ADR-046 antes da decisão Stable (2026-10-12)

O estado abaixo é preservado como registro histórico e foi supersedido pela
decisão de promoção delimitada Stable em
[IF-ADR-046](../ADRs/IF-ADR-046-STABLE-PROMOTION-DECISION.md), pela Amendment
003 e pela linha 046 do status atual. Não representa a maturidade vigente de
`PlayerSessionJoinTrigger`.

I4 é aceito por evidência composta com ressalvas: EditMode 291/291 (PlayerParticipation 100/100), I4 mínimo 1/1 e I4.2 OBSERVABLE 8/8, os cenários integrados com `BaselineRestored`; resultados reportados pelo usuário com proveniência parcial. I4.2 LEASE permanece BLOCKED 3/3 e P1/P2 não são apresentados como testes diretos: captura exata pelo binder e não delegação do port real obsoleto são sustentadas por revisão estática e permanecem riscos residuais conhecidos. Gatilhos de reabertura estão na [matriz canônica Player](../Reconciliation/IF-PLAYER-FULL-RECERTIFICATION-2026-10-11.md).

I5 foi implementado e a retirada foi registrada no Framework em `ca4b5bf6aea335b4c1816f9860ca3d1b29b0a1fb`. O usuário reportou EditMode 292/292 PASS, PlayerParticipation 101/101, QAFramework IF-ADR-042/043/044 22/22 PASS com cleanup 3/3 `BaselineRestored`, e replay de Slice 1 pós-correção 8/8 `BaselineRestored`; toda essa evidência tem proveniência parcial e não está associada a SHA/log/hash da execução. No FirstGame, o usuário observou Join/Leave/Rejoin funcionais no Local Multiplayer; Character Selection não tem validação manual independente.

Busca estática não encontrou consumidores operacionais das superfícies retiradas em Framework, QAFramework ou FirstGame. PEGA não contém referências legadas nos arquivos de projeto pesquisados, mas resolve `1.1.0-preview.7`, que não contém I3; portanto sua compatibilidade com este candidato não foi validada. O checkout PEGA existe, porém `git status` falha por configuração/estado de worktree inconsistente e não é declarado limpo.

O novo `PlayerSessionJoinTrigger` permanece Experimental. I4.2 LEASE continua BLOCKED 3/3 como histórico; P1/P2 permanecem riscos residuais sustentados por revisão estática. Pela [Amendment 002](../ADRs/IF-ADR-046-AMENDMENT-002-COMPOSITE-PHYSICAL-COMPENSATION-EVIDENCE.md), o gate de compensação física está **ACEITO POR EVIDÊNCIA COMPOSTA**, não como PASS Play Mode integrado. Proveniência vinculada ao candidato, import/compilação e execução FirstGame no mesmo SHA, e a decisão normativa sobre alcançabilidade de falha no commit Session permanecem pendentes. A revisão documental da closure Input System, versão, changelog e migração está concluída para esta API. Consulte a matriz Player e o registro de certificação do QAFramework.

### IF-ADR-046 — decisão de maturidade APPROVED (2026-10-12)

O reexame estático encerrou o gate condicional de falha do commit Session como **INALCANÇÁVEL pela boundary pública válida** nas premissas síncronas Unity descritas na [matriz canônica](../Reconciliation/IF-PLAYER-FULL-RECERTIFICATION-2026-10-11.md#15-if-adr-046--fechamento-da-alcançabilidade-e-preparação-da-decisão-stable-2026-10-12). Nenhuma fixture adicional é exigida; não é uma afirmação absoluta sobre mutações internas forçadas ou execução fora dessas premissas.

I4 e compensação física mantêm seus aceites compostos e ressalvas. O usuário reportou EditMode 292/292, PlayerParticipation 101/101, QA-PLAYER-SESSION-JOIN Slice 1 8/8 `BaselineRestored`, IF-ADR-042/043/044 22/22 e QA-PLAYER-LEAVE-REJOIN-OCCURRENCE PASS. O ZIP fornecido foi examinado e arquivado; o log bruto FirstGame e hashes dos membros foram verificados. Nenhum artefato registra SHA Framework/QA ou timestamp das execuções. HEADs atuais são snapshots de revisão, não proveniência retroativa.

**APPROVED — Player Join Stable.** IF-GOV-001 §§2 e 7 não proíbem uma exceção de evidência limitada à IF-ADR-046 que declare proveniência parcial e aceite risco sem mudar o significado de Stable. A [Amendment 003](../ADRs/IF-ADR-046-AMENDMENT-003-PARTIAL-PROVENANCE-ACCEPTANCE.md) foi aceita pela autoridade responsável e aplica-se somente à closure `PlayerSessionJoinTrigger`. A [decisão](../ADRs/IF-ADR-046-STABLE-PROMOTION-DECISION.md) registra a promoção delimitada. Os artefatos não vinculam as execuções a SHAs ou timestamps; hashes conferidos garantem integridade dos arquivos recebidos. I4 e a compensação física composta permanecem aceitos e não são repetidos.
Foundational Player sample evidence from FIRSTGAME FG-ADR-002 Revision 4 and the 2026-08-31 Scene-Provided reconciliation run:

```text
Getting Started / Scene Player  PROVEN
  LogicalActorsPrepared         READY / PASS
  GameplayReady                 READY / PASS
Player Provisioning             PLAY MODE PROVEN
```

Current sample migration and Play Mode evidence:

```text
Character Selection             IMPLEMENTED / INTEGRATED / MANUAL PLAY MODE PASS
Local Multiplayer               IMPLEMENTED / INTEGRATED / MANUAL PLAY MODE PASS
Character Selection Split Screen IMPLEMENTED / INTEGRATED / MANUAL PLAY MODE PASS
```

`GameplayReady` proves the current contextual gameplay projection over retained prepared Session Players. It does not by itself certify game-owned locomotion, camera composition, concrete gameplay input consumers or Presentation completeness.

## Current ADR status

| ADR | Architecture / package | Technical QA | Current disposition |
|---|---|---|---|
| 001 | ACCEPTED / RECONCILED / IMPLEMENTED | core evidence preserved; Editor startup isolation proven | current |
| 002 | ACCEPTED / RECONCILED / IMPLEMENTED | feature-owned | current |
| 003 | ACCEPTED / RECONCILED / IMPLEMENTED; ADR-023 structural reconciliation current | historical Player aggregate 27/27 + Manager functional 14/14; QA-001 Slices 1–3 user-reported | architecture current; full Player recertification open |
| 005 | ACCEPTED / RECONCILED / IMPLEMENTED | Input Gate / Restart / Pause certified | current |
| 006 | ACCEPTED / RECONCILED / IMPLEMENTED | technical Transition/Loading certified | Game Flow consumer PASS |
| 007 | ACCEPTED / RECONCILED / IMPLEMENTED | readiness policies certified | Game Flow consumer PASS |
| 008 | ACCEPTED / RECONCILED / IMPLEMENTED | feature-owned | persistent composition consumer evidence present |
| 009 | ACCEPTED / RECONCILED / IMPLEMENTED / TECHNICAL QA CERTIFIED | Contribution 3/3 + Visibility 2/2 + lifecycle 16/16 | current post-split contract |
| 010 | ACCEPTED / IMPLEMENTED | feature-owned | current |
| 011 | ACCEPTED / RECONCILED / IMPLEMENTED | readiness/progress certified | consumer proof PASS |
| 012 | ACCEPTED / RECONCILED / IMPLEMENTED | historical Player aggregate 27/27; QA-001 coverage in current recertification report | architecture accepted; current full Player certification open |
| 013 | ACCEPTED / EXPERIMENTAL / IMPLEMENTED / VALIDATED | QA-NEW-005 12/12 PASS post-IF-ADR-040; historical 44/44 retained | consumer gate PASS; maturity remains Experimental |
| 014 | ACCEPTED / IMPLEMENTED | certified | current |
| 015 | ACCEPTED / RECONCILED / IMPLEMENTED | public surface aggregate + Manager functional 14/14 are dated evidence; QA-001 Slices bounded | Observer/commands implemented; Player recertification open |
| 016 | ACCEPTED / IMPLEMENTED | historical Player aggregate 27/27; Slice 1 default-selection path user-reported | policies remain Experimental/current recertification open |
| 017 | ACCEPTED / RECONCILED / IMPLEMENTED | frame-rate matrices certified | current |
| 018 | ACCEPTED / RECONCILED / IMPLEMENTED | persistence/backend certifications | FIRSTGAME usability proof remains feature-owned |
| 019 | ACCEPTED / RECONCILED / IMPLEMENTED | historical aggregate + physical-lifetime certification | architecture closed; current Player recertification open |
| 020 | ACCEPTED / RECONCILED / IMPLEMENTED | ADR020-H + historical aggregate/certification | architecture closed; current Player recertification open |
| 021 | ACCEPTED / RECONCILED / IMPLEMENTED | historical Route 18/18 + Activity 23/23 + aggregate 27/27 | Model B architecture current; Player recertification for current occurrence boundary open |
| 023 | SUPERSEDED by IF-ADR-038; dated certification retained as evidence | Historical Manager/Scene-Provided evidence only | Optional visual-content lifecycle remains; Actor root owns spatial state |
| 024 | ACCEPTED / RECONCILED / IMPLEMENTED — Manager-Provisioned V1 | historical Full Player QA 16/16 including `actor-replace`; not rerun in QA-001 | `RequestReplacePreparedActor(...)` implemented; current replacement recertification open |
| 025 | ACCEPTED / IMPLEMENTATION STATUS OWNED BY PLAYER TRACK | feature-owned | Camera remains outside the Player input contract |
| 032 | SUPERSEDED / HISTORICAL | prior Camera QA/certifications remain historical only | superseded by IF-ADR-038 |
| 038 | ACCEPTED; current Assignment/Output model implemented | Framework EditMode 175/175; Camera Editor 78/78 PASS; QA-NEW-004 Session Camera 9/9 PASS; IF-ADR-039 GameFlow command consumer Play Mode PASS | broader IF-ADR-038 recertification remains separately scoped |
| 039 | ACCEPTED / IMPLEMENTED / TESTED / INTEGRATED / VALIDATED | Framework EditMode 169/169; RouteLifecycle 6/6; Camera 74/74; Route-scoped GameFlow Play Mode PASS | command boundary and migrated consumer proof closed |
| 041 | ACCEPTED / IMPLEMENTED / TESTED / INTEGRATED / VALIDATED | Framework EditMode 169/169; RouteLifecycle observer 6/6; GameFlow `Hub -> A -> B -> A -> C -> B -> Hub` Play Mode PASS | one Route-scoped adapter in `SCN_GameFlow_Basic`; ActivityFlow remains authority |
| 042 | ACCEPTED / IMPLEMENTED / QA CERTIFIED | Dedicated QAFramework Play Mode certification PASS 7/7 on 2026-10-07 with `BaselineRestored`; Local Multiplayer manual consumer validation PASS 2026-10-06; Camera Editor 78/78 PASS (test sources, execution scope recorded separately) | focused SharedGroup gate closed; broader IF-ADR-038 recertification remains open |
| 043 | ACCEPTED / IMPLEMENTED / TESTED / INTEGRATED / VALIDATED | Individual Output participation manually validated; Framework EditMode 175/175, Camera 78/78, PlayerCameraOutputIntegration 3/3, SessionCameraAssignmentRuntime 37/37; focused QAFramework 7/7 PASS on 2026-10-07 with 10/10 monitored frames covered and `BaselineRestored` | focused physical-participation gate closed; broader IF-ADR-038 recertification remains separate |
| 044 | ACCEPTED / IMPLEMENTED / INTEGRATED / VALIDATED | Public occurrence-scoped block/release token, Session-lifetime composition, canonical input adapter projection and Leave cleanup; QAFramework IF-ADR-044 **8/8 PASS** with `BaselineRestored`; `PlayerGameplayAvailabilityBlockTrigger` manually validated in Local Multiplayer on 2026-10-08 for P1/P2 Block/Release, independent ownership, teardown cleanup and Pause composition | current; gameplay turn/active-player cycling remains consumer-owned and outside Framework scope |
| 045 | ACCEPTED / D IMPLEMENTED / D2 STATIC AUDIT COMPLETE / D4-D5 TEST FIXES IMPLEMENTED / D7 PARTIAL QA CERTIFICATION 6/7 / D8 PACKAGE-LEVEL / D9 EDIT MODE RESULTS USER-REPORTED / D10 DOCUMENTS RECONCILED | Persistent Content uses path/name, explicit idempotent migration and path-driven Scene Picker; runtime retains source roots through Session composition before application-lifetime transfer. D7: Prepare/cleanup PASS, scoped migration 4/4 PASS, and six runtime checkpoints PASS including exact unload/root transfer, Route/Activity transitions, and Route A baseline restoration; Session shutdown/release remains BLOCKED, so QAFramework stays 6/7. D8 assigns that internal Session release contract to PACKAGE-LEVEL tests because consumers have no supported shutdown request/result. D9 source adds release-failure aggregation/retry, participant-clear, root-survival and repeated-release assertions. D10 records the user-reported Unity Edit Mode result: 194 passed, 0 failed, 0 ignored; named evidence: `SessionCompositionReleaseTests` PASS, Audio 5/5, `PersistentContentSceneReferenceTests` 12/12. | Implemented, test source, reported execution and Play Mode evidence are distinct; this is not full IF-ADR-045 certification. `FrameworkRuntimeHost.OnDestroy → ReleaseSessionScope` integration remains uncovered. Package transfer Edit Mode regression, other ADR-045 gates, four legacy QA setup references, Standalone/WebGL Player Build validation and independent reproduction of the consumer incident remain pending. Do not change QAFramework 6/7 or rewrite the approved plan; it directs progress records to this tracker. |
| 046 | ACCEPTED — bounded Player Join closure APPROVED Stable; I1/I2/I3 implemented; public entrypoint retirement committed; I4 accepted by composed evidence; physical compensation accepted under Amendment 002; partial provenance expressly accepted under Amendment 003 | Only `PlayerSessionJoinTrigger` and the explicitly listed Join contract closure are Stable; Session/Slot/Actor/Leave/participation/readiness, broad scoped access and the Framework remain at their separately declared maturities | See [Stable-promotion decision](../ADRs/IF-ADR-046-STABLE-PROMOTION-DECISION.md), convergence audit and [canonical Player matrix](../Reconciliation/IF-PLAYER-FULL-RECERTIFICATION-2026-10-11.md). Preserve the accepted residuals and provenance scope recorded there; this bounded decision does not promote adjacent APIs or the Framework. |
| 047 | ACCEPTED — 1.1.0 first official Stable baseline; forward SemVer policy | No Runtime behavior; per-contract maturity unchanged | Release publication pending; apply the four GO/NO-GO gates above. |
| 048 | ACCEPTED — ordered 1.1.0 Stable readiness roadmap | No Runtime/API behavior; no maturity changes | Execution pending; no phase is certified complete; current 1.1.0 disposition remains NO-GO. |
| 049 | ACCEPTED / EXPERIMENTAL / PUBLISHED in `1.2.0-preview.2` | Project Owner reports 11/11 PASS for `Immersive.Framework.ActorReferences.Editor.Tests` in Unity EditMode. Source `d588e1c481692393360e93962cd587fe60a031d3`, tag `v1.2.0-preview.2`, workflow [#22](https://github.com/ImmersiveGames/com.immersive.framework/actions/runs/38998090165) succeeded; Stable promotion job was skipped. [GitHub Release](https://github.com/ImmersiveGames/com.immersive.framework/releases/tag/v1.2.0-preview.2) is a prerelease with `com.immersive.framework-1.2.0-preview.2.tgz`, SHA-256 `af3408453bd238f9ed8f0f786419c729eb8cd65fae5f07026b1c1da0e3d816c5`. OpenUPM status: `succeeded`, signed, published version `1.2.0-preview.2`; downloaded registry tarball SHA-256 matches the Release asset. | PEGA consumer integration remains pending; see [Actor Occurrence References](../Guides/Actor-Occurrence-References.md). |
| 050 | ACCEPTED — FUTURE ARCHITECTURE; PLAN v1 APPROVED; D00–D03 COMPLETE; D04 CANDIDATE IMPLEMENTED / UNITY GATE PENDING; D05–D08 UNSTARTED | Target accepted 2026-10-17; [contract migration](../Reconciliation/IF-ADR-050-ACCEPTED-TARGET-CONTRACT-MIGRATION-2026-10-17.md) and [reconciliation crosswalk](../Reconciliation/IF-ADR-050-ACTOR-LIFECYCLE-COMPOSITION-RECONCILIATION-2026-10-17.md) recorded. D01 changes only the existing managed visual release order. D02 shifts Player occurrence-ID issuance to physical preparation commit and preserves physical resolver availability semantics; M02 keeps `ActorId` with the logical Actor across root replacement unless an explicit logical switch is requested. Its identity-policy request remains Experimental and non-serialized. No Stable schema changed. The Project Owner's latest pre-D04 QAFramework screenshot reports 411/411 EditMode tests, including `Immersive.Composition.Editor.Tests.dll` 11/11; it is D03 evidence only. | [IF-ADR-050](../ADRs/IF-ADR-050-Actor-Occurrence-Lifecycle-Authority-and-Commit.md) / [IF-SPEC-050](../Specifications/IF-SPEC-050-Actor-Lifecycle-Participation-and-Composition.md) accepted target. [Implementation plan v1](../Plans/IF-PLAN-ADR-050-ACTOR-COMPOSITION-AND-OCCURRENCE-LIFECYCLE.v1.md) is **Approved — implementation task-gated**. [D00 baseline](../Reconciliation/IF-ADR-050-D00-IMPLEMENTATION-BASELINE-2026-10-17.md) completed at `086efe9cd5f5aa3b52d3e0757c4fe2a7c25f617b`. D01, D02 and D03 are complete. D04 uses the pinned local Composition candidate `0.1.0-preview.1`; its acceptance gate awaits Framework import/compile and focused Editor evidence. D05–D08 remain unstarted. |

## IF-ADR-050 implementation entry — D00 — 2026-10-17

**Plan v1:** Approved — implementation task-gated. The plan body is unchanged except for its status; the Plans index reflects the approval.

**D00:** Complete — static source/documentation inventory only. Framework revision: `086efe9cd5f5aa3b52d3e0757c4fe2a7c25f617b`. See [D00 baseline and contract gates](../Reconciliation/IF-ADR-050-D00-IMPLEMENTATION-BASELINE-2026-10-17.md).

The record maps M01–M12 to architectural owners and code paths, classifies Stable/Experimental/Internal contracts, identifies Stable serialized schemas and known consumer assets, fixes the future `com.immersive.framework → com.immersive.composition` dependency boundary, and records future deliverable dependencies, reusable evidence and focused test needs. It confirms the current resolver allocates/drops Player `ActorOccurrenceId` with physical-resolution changes, replacement selection changes before B activation, and release detaches the visual after deactivating the host.

QAFramework resolves Framework from the local package path; `planet-devourer` has Git lock hash `10aeb459df42e27159f375ee6e8c6b154f1ef985`; PEGA resolves registry `1.2.0-preview.2`. All three projects declare Unity `6000.5.0f1`. Their Git HEAD/status could not be read (`git -C` permission denied); manifests, locks and representative serialized assets were readable. No consumer/QA files changed. No Unity, test or QA execution was performed.

**Blockers:** none for D00. Residual traceability limit: external consumer Git revisions unavailable in this environment; this does not establish a consumer validation result. **Next eligible:** D01 as a separately issued task. D02 may be prepared independently. No later deliverable was started.

## IF-ADR-050 implementation entry — D01 — 2026-10-17

**D01:** In progress. `AttachedPlayerActorMaterializationAdapter.TryReleaseMaterialization` now deactivates an owned visual before detaching it from the inactive Actor root, preventing an intervening `OnEnable` before deferred `Destroy`. The Scene-owned release branch remains distinct and does not destroy its borrowed Actor or visual.

Focused evidence source is in `Tests/Editor/Camera/Assignments/ActorOccurrenceOptionalVisualContentTests.cs`: a Play Mode regression records canonical Framework logger events, visual `OnEnable`/`OnDisable`, exact RuntimeContent owner and logical release outcome; an Edit Mode case asserts externally owned Actor and visual survive release. Static compilation exposed ambiguous `Object` references in this test assembly, resolved with an explicit `UnityEngine.Object` alias. `Tests/Editor/Camera/Immersive.Framework.Camera.Editor.Tests.asmdef` references the existing `Immersive.Logging.Runtime` assembly for the logger.

**Validation:** the current `Immersive.Framework.Runtime` and `Immersive.Framework.Camera.Editor.Tests` sources compiled to a disposable output directory with Unity 6000.5.0f1-generated Bee response files and the bundled Mono Roslyn compiler (both exit code 0). Analyzer load warnings remain because this compiler is Roslyn 3.7 while the installed analyzers target 4.x. Unity batch import against a disposable 6000.5.0f1 project returned exit code 1 after setting the project path and before Package Manager resolution; `Library/ScriptAssemblies` remained empty. No EditMode or Play Mode tests ran. `git diff --check` and source review pass. This static compile does not close D01's focused behavior gate or claim deferred `Destroy` as physical-destruction confirmation.

The Project Owner's initial Unity Test Runner result showed `ManagedVisualRelease_DoesNotReactivateVisualWhenDetachingFromInactiveActor` cancelled with `Unexpected assembly reload happened while running tests`. In the corresponding QAFramework `Editor.log`, Unity `6000.5.0f1` reported a successful Tundra script build before the runner failure; `ProjectSettings/EditorSettings.asset` had Enter Play Mode Options enabled with value `0` (normal reload), while this `EnterPlayMode(expectDomainReload: false)` fixture did not configure those options. Commit `13059b24bbd88fe8bb74615875005c29e013616f` corrected the fixture SetUp/UnityTearDown to disable domain and scene reload for its test and restore both original Editor settings.

The Project Owner's subsequent Unity Test Runner screenshot reports **QAFramework EditMode 399/399 passed, 0 failed**; `Immersive.Framework.Camera.Editor.Tests.dll` is green at **88/88**. The named release regression is in that assembly. This is user-reported full-suite evidence that the corrected fixture was imported and the runner completed without the prior reload abort; the screenshot does not show a Unity version, exact Framework source SHA, or per-case detail. **D01: Complete** on its stated acceptance, with the screenshot's provenance limit retained.

**Blockers at this checkpoint:** no D01 code or behavior blocker remained based on the reported run. D02's concrete M02 logical `ActorId` producer/migration contract was still open at this earlier 399/399 checkpoint; its later implementation and validation are recorded below. D03–D08 have not started.

## IF-ADR-050 implementation entry — D02 progress — 2026-10-17

Commit `d526f00d7227985a95dd0bbf6c11508c2ca7664e` implements the M03/M04 Player occurrence-ID source change. `PlayerActorPreparationRuntimeContext` now requests an ID from the Framework Host's shared occurrence sequence when an active Player materialization is committed into its authoritative prepared record. NonPlayer owner publication uses the same sequence. `PlayerActorMaterializationHandle` retains that ID through temporary deactivation/reactivation and clears it only when physical release is marked successful. `PlayerActorPhysicalOccurrence` carries the committed ID; `ActorOccurrenceReferenceRuntime` returns it and no longer creates or caches Player IDs during physical lookup. The existing physical source still requires the exact prepared handle to be Active and the root to be `activeInHierarchy`, so physical resolution remains unavailable while inactive.

The initial `PlayerOccurrenceIdentitySurvivesTemporaryPhysicalUnavailability` Actor References test used a `FakePlayerSource` and a manually assembled physical occurrence, so it did not prove identity retention in the production materialization lifecycle. That claim is superseded by the test-source correction below. The resolver replacement case remains a source-level unit test; it has **not** executed in Unity.

**Static compile evidence:** using the Unity 6000.5.0f1-generated Bee response files already present under QAFramework's Library, with all outputs redirected to a disposable temp directory and the current Runtime reference assembly, these compiled with exit code 0: `Immersive.Framework.Runtime`, `Immersive.Framework.ActorReferences.Editor.Tests` and `Immersive.Framework.Camera.Editor.Tests`. Roslyn 3.7 was used; analyzer response entries were omitted because the installed Unity analyzers require newer Roslyn versions. No QAFramework files were changed. `git diff --check` passed. This proves source/assembly compatibility at the C# compiler boundary only; it is not Unity import or test execution evidence.

**M02 decision and implementation — Project Owner, 2026-10-17:** Framework Core preserves `ActorId` for the same logical Actor across physical-root replacement; only an explicit logical Actor switch issues another ID. `PlayerActorPreparationRuntimeContext.CreateLogicalActorId` is now the issuer for initial preparation and Scene-Provided adoption. `AttachedPlayerActorMaterializationAdapter.TryMaterialize` requires that ID and now creates only its physical `RuntimeContentId`; it no longer mints `ActorId` from the materialization revision, Slot or owner. Prepared physical replacement retains the current ID by default. `PlayerPreparedActorReplacementRequest.IdentityPolicy = StartNewLogicalActor` explicitly creates another logical Actor, including when the selected Profile is unchanged. The pre-preparation `RequestReplaceActorSelection` remains selection-only.

The API addition is Experimental and non-serialized. The existing request constructor signature remains and defaults to `PreserveLogicalActor`; precompiled consumers of this public value type should recompile to consume the expanded struct layout. No Stable schema, `ActorProfileId`, `PlayerSlotId` or `PlayerOccurrenceId` is used to derive Player `ActorId`. Runtime `PlayerActorDeclaration.actorId` assignment remains part of the existing instance materialization/adoption transaction; adoption restores the authored Scene value during release.

**Validation at this checkpoint:** static C# compilation succeeded for `Immersive.Framework.Runtime` and `Immersive.Framework.Camera.Editor.Tests` using the current Framework sources and the QAFramework Unity-generated project/reference set, with outputs redirected to a disposable temporary directory. At that point no Unity import or Editor tests had run; the 399/399 screenshot predated these changes. The focused test and subsequent Unity result are recorded below. D01 is closed as recorded above; D03–D08 have not started.

### D02 test-source correction — 2026-10-17

Removed the synthetic Actor References persistence test because its fake source could preserve a hand-built occurrence ID independently of production. The resolver fixtures now allocate simulated IDs with `ActorOccurrenceIdSequence`. Extended `MaterializationHandleActivationAndReleaseDoNotRequireVisualContent` to exercise the production `PlayerActorMaterializationHandle`: identity is committed only while active, retained through temporary deactivation/reactivation, cannot be reassigned, and is invalidated by terminal physical release. The production query path remains statically traced from `PlayerActorPreparationRuntimeHostModule` through `PlayerActorPreparationRuntimeContext.TryGetCurrentPhysicalActorOccurrence`, which reads that same handle ID and rejects inactive handles/roots.

Recompiled the current `Immersive.Framework.Runtime`, `Immersive.Framework.Camera.Editor.Tests` and `Immersive.Framework.ActorReferences.Editor.Tests` sources with the Unity 6000.5.0f1-generated Bee response files and bundled Mono Roslyn compiler; all exited 0. Actor References test fixtures and the Camera lifecycle test are compiled under their exact assembly names, preserving `InternalsVisibleTo` checks. `git diff --check` passed. No Unity import or Editor test execution was performed. This removes synthetic identity issuance from the modified tests and establishes compile compatibility, but the production preparation-context query and combined resolver behavior still require the focused Unity Editor run before D02 can close.

### D02 M02 logical Actor identity — 2026-10-17

Applied the Project Owner's explicit policy ruling. `ActorId` now has a Framework Core producer independent of physical materialization. Manager-created and Scene-adopted Player Actors use that producer; physical replacements retain the committed `ActorId` unless the caller explicitly selects `StartNewLogicalActor`. The old request constructor continues to mean same logical Actor. IDs remain opaque and retain the existing `player-actor:` diagnostic prefix. No serialized Asset, prefab or authoring field changed, and no consumer project was modified.

Static compilation passed for `Immersive.Framework.Runtime` and `Immersive.Framework.Camera.Editor.Tests`. The first attempted compiler invocations were invalid because the generated QAFramework project references a missing Foundation project file and because the test project initially pointed at the prior runtime assembly; the final invocation replaced those references only in temporary project copies and completed both C# compilations with exit code 0. Neither compile invoked Unity or executed tests. No QAFramework files were changed.

The new focused Camera Editor test verifies the replacement identity policy and the adapter's explicit ActorId input. **At this checkpoint D02 was still open:** the Unity import and focused Editor test had not run against the live package checkout. The user-reported 399/399 run covered the earlier D01 revision only. Its later validation and closure are recorded below; D03 remains unstarted.

### D02 Unity validation and closure — 2026-10-17

**Status: Complete.** The Project Owner's latest Unity Test Runner screenshot reports QAFramework EditMode **400/400 passed**, with **0 failed and 0 cancelled**. `Immersive.Framework.Camera.Editor.Tests.dll` is green at **89/89** and `Immersive.Framework.ActorReferences.Editor.Tests.dll` at **11/11**. The increase from the earlier 399/399 and Camera 88/88 run is consistent with the added `PreparedActorReplacementPreservesLogicalIdentityUnlessExplicitlyChanged` case. The Camera suite also includes `MaterializationHandleActivationAndReleaseDoNotRequireVisualContent`, which exercises committed occurrence identity through activation, temporary deactivation/reactivation and terminal release. The Actor References suite exercises committed publication, replacement, stale exact references, owner scope and Player leave behavior.

Together with the static source trace from `PlayerActorPreparationRuntimeHostModule` through `PlayerActorPreparationRuntimeContext.TryGetCurrentPhysicalActorOccurrence`, the current Unity suite result closes the changed-import and focused-Editor-test gate. This is user-reported working-checkout evidence; the screenshot does not expose the Unity version, Framework SHA, raw per-case results or runner XML, so it is not immutable certification. The current implementation revision recorded for this D02 cut is `f02bacf5a004aeb1f851055c84e4a7a5708017d6`.

**D02: Complete** for its M01–M04 scope and acceptance gate. The M02 replacement identity policy is documented above and committed; the new request policy remains Experimental and non-serialized. No Stable serialized schema was changed. Residual compatibility requirement: precompiled consumers of the expanded public value type must recompile. No concrete D02 blocker remains. D03 is the next eligible independent deliverable under plan v1, but has not started and requires its own task authorization; D04–D08 have not started.

### D03 — generic Composition kernel — 2026-10-17

**Status: Complete — Project Owner reports clean QAFramework import/compile and focused EditMode tests 11/11 passed.** The separate local package repository `com.immersive.composition` has candidate version `0.1.0-preview.1`, current commit `d3524b2e06f2c071a62989cb3c2aec03d767e225`, branch `main` (initial candidate: `f88e7803c224368acb8d5ecbe57554940f784354`). Its runtime assembly has `noEngineReferences: true` and no Framework, Unity, Actor, Player or RuntimeContent references. It contains the generic typed-key/definition/materializer/composer/result-delta contracts, owner priority resolution, dependency validation/order, stale-operation invalidation, explicit acquired/adopted release outcomes, and isolated EditMode test sources. All candidate consumer contracts are documented as Experimental; no serialized schema or Framework dependency is introduced.

**Static evidence:** package manifest and both asmdefs parse as JSON; test asmdef references the Composition runtime and Unity Test Framework directly; all 22 Unity asset GUIDs are unique; runtime-source boundary scan is clean; package `git diff --cached --check` passed before commit. The local environment has `dotnet.exe` but `dotnet --list-sdks` returned no SDK, and no separate C# compiler was available. Unity validation was supplied by the Project Owner as described below.

**Compile correction and result:** the Project Owner reported two `CS1061` errors from the first candidate at `CompositionComposer.cs` lines 891 and 957: the accumulation target is `IList<CompositionIssue>`, which has no `AddRange`. Commit `d3524b2e06f2c071a62989cb3c2aec03d767e225` replaces those calls with explicit iteration and preserves the `IList` contract. The Project Owner subsequently confirmed that everything compiles correctly in the existing QAFramework project. Unity version and raw compiler output were not provided; this is owner-reported import/compile evidence, not an independently run build.

**Project Owner evidence:** the supplied Package Manager screenshot shows `Immersive Composition` version `0.1.0-preview.1` with source `Local`, installed from `C:/Projetos/ImmersivePackages/com.immersive.composition`, in the existing QAFramework project. The Project Owner reported the corrected package compiles successfully. The latest Test Runner screenshot shows QAFramework EditMode **411/411 passed, 0 failed**, with `Immersive.Composition.Editor.Tests.dll` **11/11 passed**. The image does not expose Unity version, package source SHA or raw test XML; this is owner-reported working-project evidence, not immutable candidate certification. In task context, the local package checkout is commit `d3524b2e06f2c071a62989cb3c2aec03d767e225`.

**Test discovery configuration:** added `com.immersive.composition` to the existing `testables` array in `C:/Projetos/QAFramework/Packages/manifest.json`, preserving the existing `com.immersive.framework` entry and dependency declarations. The modified manifest parses as JSON; the later Test Runner screenshot confirms the Composition test assembly is now discovered.

**D03 acceptance:** satisfied for local candidate `0.1.0-preview.1`: package boundary and static checks pass; Project Owner reports QAFramework compile success; focused Composition EditMode suite reports 11/11 passed. No Unity or package source changes are included in this D03 closeout.

**Residual gate and next step:** D04 is the next eligible deliverable but is not started by this update. It must consume a concrete, pinned D03 candidate; the current candidate remains local and has no remote or version tag. D04 requires a separate implementation task authorization. D05–D08 remain unstarted.

### D04 — Framework Base visual integration — 2026-10-17

**Status: In progress — implementation candidate prepared; D04 acceptance remains pending the existing QAFramework Unity import/compile and focused Editor evidence.** Framework base revision: `3347b5c57968cc9f7482ab5ea581ebd467faad99`, branch `codex/adr-050-d01-release-safety`; implementation commit `47c00a85`, compile-feedback corrections `3316cbb9` and `ce479981`. Composition dependency is pinned in `package.json` to `0.1.0-preview.1`, candidate repository commit `d3524b2e06f2c071a62989cb3c2aec03d767e225` (`main`). No Composition package source, QAFramework, consumer project, Unity asset, or `Packages/manifest.json` was changed.

| Contract / M-item | Current integration and authority | Maturity / disposition |
|---|---|---|
| Actor Profile Base content and `presentationPrefab` migration — M08 | `ActorProfile.compositionParts`; `ActorCompositionPart`; explicit Editor migration in `ActorProfileVisualContentMigration`; `ActorProfileVisualContentMigrationTests`. Framework translates profile intent into a contribution keyed `visual`; no Runtime fallback reads the legacy field. | `ActorProfile` remains Experimental. The old serialized field is retained with `FormerlySerializedAs` solely as migration input. Stable `SceneProvidedLocalPlayerAuthoring` references and Stable schemas remain untouched. |
| Acquired part materialization, provenance and release — M09/M11 | `ActorCompositionRuntimeBinding.TryCreate`, `ActorCompositionPartMaterializer.PrepareAsync/Apply/Detach/ReleaseAsync`, owned by the Framework adapter at `AttachedPlayerActorMaterializationAdapter.TryMaterialize/TryReleaseMaterialization`; `PlayerActorMaterializationHandle` retains the binding. | Internal Framework integration over Composition's Experimental kernel. RuntimeContent and the Actor producer keep root/logical ownership. Acquired parts are deactivated, detached and released through Composer; adopted parts return `Retained` and are never instantiated or destroyed by the part materializer. |
| Scene-Provided adoption — M08/M11 | `SceneProvidedLocalPlayerCompositionResolver` validates the exact authored child and mount; `PlayerActorPreparationRuntimeContext.TryAdoptScenePlayerActor` registers that exact object with Composer using operation/profile/key/mount provenance. | `SceneProvidedLocalPlayerAuthoring` remains Stable with its serialized fields unchanged. Current D04 Scene-Provided binding intentionally supports only the exact Base `visual` part; additional keyed scene-part bindings are not part of this cut. |
| Authoring diagnostics | `PlayerActorSelectionAuthoringValidator` checks legacy data, valid/unique keys, dependencies, prefab references and forbidden Actor/PlayerInput/RuntimeHost components. Scene validation rejects missing/mismatched mounts. `LocalPlayerProvisioningValidator` cross-checks configured default Actor Profiles against the Manager Host Runtime Prefab mount; Runtime Host Inspector explains the runtime mount constraint. A dynamically selected Profile without an authored default pairing remains checked at materialization. | Editor tooling / Experimental profile diagnostics. No maturity promotion or new validation policy. |

The existing direct visual `Instantiate`/`Destroy` provider was removed from `AttachedPlayerActorMaterializationAdapter`. Empty Base content still creates no Composer and requires no mount. `RuntimeContentRuntime` continues owning the physical Actor root. The migration copies the existing Prefab object reference to key `visual`, clears only the legacy value, preserves the Actor Profile/Prefab GUIDs and leaves Player Slot/default/selected Profile references unchanged. Conflicting or duplicate visual entries are reported without overwriting either reference.

**Evidence produced:** focused test sources cover (a) empty and unmigrated legacy profiles, (b) Manager Base visual placement exactly once, (c) Manager default-profile mount validation with missing/present mount, (d) Composer retaining the exact adopted Scene object without duplication/destruction, and (e) migration idempotency, null content, conflict preservation, GUIDs and default-profile reference preservation. Static checks in this worktree: JSON parsing for `package.json` and affected asmdefs; 1,233 Unity `.meta` GUIDs checked unique; `git diff --check`; source search confirms no Framework runtime direct instantiation/release of `presentationPrefab`. No Unity import, compilation, or test execution has been claimed; the previous 411/411 screenshot predates D04 and is not D04 evidence.

**Compile feedback:** the Project Owner reported `CS0246` for `PlayerActorRuntimeHost` in `ActorCompositionProfileContract.cs`. The type is in the same Runtime assembly under `Immersive.Framework.PlayerParticipation`; the contract lacked that namespace import. Commit `3316cbb9` adds the import. The corrected candidate has not yet been recompiled in QAFramework.

The next QAFramework compile reported `CS0103` at three `SetActorVisualContent` calls in `ActorOccurrenceOptionalVisualContentTests.cs`: the helper was scoped only inside the PlayMode fixture while the Editor fixture also called it. Commit `ce479981` shares the helper with the Editor fixture and routes the PlayMode fixture through it. Recompilation is pending. Reported `CS0618` warnings in preexisting `FrameworkRuntimeHostControlledStopTests.cs` and QA-IF-ADR-044 consumer code were not changed.

**Compatibility and residual risks:** (1) QAFramework must resolve the exact local Composition candidate `0.1.0-preview.1`; its package source currently has no published/tagged distribution. (2) Legacy ActorProfile assets must be explicitly migrated after Unity import/reload before selection; the retained API getter returns the migrated `visual` prefab only and is not a fallback provider. (3) Unity must confirm serialization/import of the inline list and `FormerlySerializedAs` field, Editor migration persistence, and that the existing prefab/scene authoring fixtures compile and validate. (4) Manager authoring can statically pair only configured default Actor Profiles with the provisioning Host; a Profile selected through a dynamic command has its mount requirement enforced at materialization. (5) Scene-Provided D04 accepts one authored Base `visual`; keyed scene binding is deferred to its separately authorized scope. Composer close retains adopted content; terminal Session Leave may release its containing root under the current IF-ADR-003/020 ownership contract. No Stable serialized contract changed.

**M11 ownership ruling:** successful Scene-Provided admission promotes the physical Host to Session ownership under current IF-ADR-003/020. D04's Composer adoption preserves the exact authored visual and `TryClose` does not destroy or replace it. The Session producer may later destroy the containing root during explicit terminal Leave; this is producer root release, not Composer release of the adopted part. The focused binding test proves the Composer boundary only; existing Session Leave evidence remains the root-release authority.

**Ruling:** Unity compilation and test execution were not invoked because the repository's `AGENTS.md` prohibits Unity builds/tests in this workflow. The source tests and import checklist are prepared for the Project Owner's existing QAFramework project. Cost if this restriction were misapplied: Unity-specific source/import or lifecycle errors would remain undetected until that manual gate.

**Current blocker:** no code-boundary or ownership blocker found in static review; the D04 acceptance gate is pending owner-run QAFramework package import/compile and focused tests/Inspector evidence. **Next step:** finish D04 validation only. D05 has not started and is not eligible before the D04 gate is reviewed.

## Current Activity content / visibility closure — IF-ADR-009 — 2026-08-30

Current architecture:

```text
ActivityContentContribution
  -> Activity ownership
  -> Local Content Id
  -> Required / Optional
  -> Activity content lifecycle

ActivityVisibilityRule
  -> presentation only
  -> no ownership
  -> no Requiredness
  -> no Activity content lifecycle authority
```

Current post-split QA:

```text
Contribution Authority     3/3  PASS
Visibility Isolation       2/2  PASS
Lifecycle regression      16/16 PASS
------------------------------------
Current post-split evidence 21/21 PASS
```

The lifecycle regression explicitly proves that Visibility membership does not broaden
Contribution ownership. Presentation may change with zero Contribution callbacks, and
Contribution lifecycle may exit while presentation remains visible for another listed
Activity.

The historical ADR-009 `46`-case certification remains dated evidence for the earlier
combined boundary only.

Certification record:

[IF-ADR-009 Contribution / Visibility Technical Certification — 2026-08-30](../Reconciliation/IF-ADR-009-CONTRIBUTION-VISIBILITY-TECHNICAL-CERTIFICATION-2026-08-30.md)

## Current Player scoped closure — IF-ADR-023 / IF-ADR-023A / IF-ADR-024 — 2026-09-02

Current architecture:

```text
Local Player Host composition
  -> reusable PlayerActorRuntimeHost

ActorProfile
  -> Actor occurrence selection/configuration (implemented; optional visual content is subordinate)

PlayerActorDeclaration
  -> authored occurrence ID empty
  -> runtime occurrence identity established by physical preparation
```

Removed current authority:

```text
ActorProfile.LogicalActorHostPrefab
LogicalActorHost
SceneLogicalPlayerActorEvidence
HasLogicalActor
persistent authored PlayerActorDeclaration occurrence IDs
```

`LogicalActorsPrepared` remains semantic readiness terminology.

Current occurrence-identity invariant:

```text
before physical preparation boundary
  typed Player Actor occurrence ActorId unavailable

after identity establishment boundary
  typed PlayerActorDeclaration.ActorId valid

after preparation commit
  retained physical evidence is authoritative
```

Current Scene-Provided Play Mode proof:

```text
LogicalActorsPrepared
  Activity readiness = Ready
  projected = 1
  selected = 1
  prepared = 1
  failed = 0

GameplayReady
  Activity readiness = Ready
  projected = 1
  selected = 1
  prepared = 1
  failed = 0
```

Current Manager-Provisioned prepared replacement invariant:

```text
A Prepared + GameplayReady
  -> release contextual gameplay A
  -> release occupancy A by exact preparation ownership
  -> physical replacement A -> B
  -> canonical gameplay projection B
  -> same Activity occurrence readiness
```

Recoverable pre-commit failures restore A through canonical gameplay projection.
Post-commit gameplay failure keeps B authoritative and reports the committed
degraded state. `Slot.Revision` is mutable revision evidence and is not treated as
immutable Player occurrence identity.

Current ADR-024 integrated proof:

```text
[QA_PLAYER_FULL]
status = Passed
verdict = PLAYER QA CERTIFIED
cases = 16/16
```

Current scoped-access reconciliation:

```text
Route scope     = Route lifecycle ownership
Activity scope  = Activity lifecycle ownership
scene location  != scope authority
```

Current teardown rule:

```text
consumer may die before persistent runtime owner
→ consumer-side binding releases on OnDestroy
→ later owner release tolerates destroyed Unity wrapper
→ diagnostics do not dereference destroyed object
```

Certification and reconciliation records:

- [IF-ADR-023 Player Actor Runtime Technical Certification — 2026-08-29](../Reconciliation/IF-ADR-023-PLAYER-ACTOR-RUNTIME-TECHNICAL-CERTIFICATION-2026-08-29.md)
- [IF-ADR-023A Player Actor Occurrence Identity Boundary — 2026-08-31](../Reconciliation/IF-ADR-023A-PLAYER-ACTOR-OCCURRENCE-IDENTITY-BOUNDARY-2026-08-31.md)
- [IF-ADR-024 Prepared Actor Replacement Technical Certification — 2026-09-02](../Reconciliation/IF-ADR-024-PREPARED-ACTOR-REPLACEMENT-TECHNICAL-CERTIFICATION-2026-09-02.md)

## Current Camera and Actor occurrence target — IF-ADR-038 — 2026-09-30

~~~text
GameApplication startup -> Session Camera Assignment -> Occurrence -> Membership / Subject -> Output
Empty SharedGroup -> Output fallback
~~~

Session Camera authority explicitly activates Assignments. Route and Activity do not
own Assignment state, membership, or Output routing; Join/Leave changes membership
only. SharedGroup retains one occurrence per Assignment/Output and reconciles its
current eligible Actor Subjects into the TargetGroup. The Actor occurrence root owns
Player physical/spatial state and pose; its explicit ObservationTransform owns Camera
observation, and Actor replacement updates Subject evidence while preserving Player
and Camera Occurrence identity. Output, fallback, and PlayerInputManager physical
split-layout ownership remain.

The Player runtime ownership cut and the Session Camera runtime path are present in
source. `FrameworkRuntimeHost` materializes Outputs, creates Assignment occurrences,
integrates Player membership and routes transition coverage through Output fallback.
The assignment runtime supports Session-scoped, Individual and Shared Group modes,
membership/Subject reconciliation, and transactional replacement.

| Cut | Source status | Repository evidence | Remaining gate |
|---|---|---|---|
| CAMERA-038-A — Actor occurrence authority | Implemented in source | Actor-root ownership, optional visual content and explicit Subject authoring; Editor coverage exists | SceneProvided and ManagerProvisioned Unity revalidation |
| CAMERA-038-B — Definition / Assignment / Occurrence | Implemented in source | Definition, Assignment, mode-specific identity and identity/validation coverage exist | Unity import/compile and runtime certification |
| CAMERA-038-C — Outputs / Fallback | Implemented in source | Per-Output state, Fallback coverage and same-occurrence recovery have Editor coverage | Unity runtime certification across multiple Outputs |
| CAMERA-038-D — Zero-Player Session Camera | Targetless/fixed path implemented / validated; explicit Session/world target deferred | QA-NEW-004 current Session-scoped, Player-free continuity certification PASS 9/9 with `BaselineRestored`; FirstGame uses `NoSubject`; same Output/Assignment survives Route/Activity transitions | targetless cut closed; implement explicit world target only for a concrete consumer; broader IF-ADR-038 recertification remains separate |
| CAMERA-038-E — Membership / Subjects | Implemented in source | Current Player/Actor occurrence reconciliation and Subject updates have Editor coverage | Unity Join/Leave/replacement evidence |
| CAMERA-038-F — Individual per Player | Implemented / Integrated / Validated | Manual P1/P2 validation plus Framework EditMode 175/175 / Camera 78/78; IF-ADR-043 focused QAFramework 7/7 PASS proves `0 -> P1 -> P1+P2 -> P2 -> 0 -> P1 -> P1+P2`, exact `PlayerInput.camera`, stable Assignment/Output identity and zero frames without physical coverage | cut closed; broader IF-ADR-038 recertification remains separate |
| CAMERA-038-G — Shared group | Implemented / QA certified; Local Multiplayer consumer validation PASS 2026-10-06 | IF-ADR-042 dedicated QAFramework Play Mode 7/7 PASS on 2026-10-07 with `BaselineRestored`; consumer verified 1-player follow without orbit, 2-player framing, dolly/FOV on separation, and stable Subject under Actor rotation; Camera Editor 78/78 PASS (test suite scope remains distinct from QAFramework result) | focused SharedGroup gate closed; broader IF-ADR-038 recertification and transactional Assignment failure paths remain |
| CAMERA-038-H — Transactional Assignment change | Implemented in source | Candidate replacement, rollback and Output preservation have Editor coverage | Unity failure-path and multi-Output evidence |
| CAMERA-038-I — Remove Presentation / Request / Game Flow ownership | Package code removed | Old Presentation/Request symbols are absent from Runtime/Editor | Migrate and revalidate remaining serialized QA/consumer assets |
| CAMERA-038-J — Authoring / samples / assets | Partial | Getting Started, Local Multiplayer SharedGroup, Character Selection Fixed Follow and Character Selection Multiplayer Split Screen are migrated; the Character Selection samples and Local Multiplayer have manual Play Mode PASS | Migrate remaining consumer/QA assets and complete authoring validation |
| CAMERA-038-K — QA / regressions / documentation cleanup | Partial | Framework EditMode 175/175 and Camera Editor 78/78 PASS; QA-NEW-004 Play Mode 9/9 PASS; IF-ADR-042 SharedGroup 7/7 and IF-ADR-043 physical-participation 7/7 PASS; sample docs record current manual Play Mode evidence | focused SharedGroup and individual-participation QA gates closed; transactional Assignment failure paths and broader IF-ADR-038 recertification remain |

Current Framework EditMode execution is **175/175 PASS**, including Camera Editor
**78/78 PASS**. Manual Play Mode passed for Local Multiplayer SharedGroup,
Character Selection Fixed Follow, Character Selection Multiplayer Split Screen,
and IF-ADR-043 Output participation. QA-NEW-004 now provides current Play Mode
certification for the zero-Player Session-scoped Camera cut at **9/9 PASS** with
`BaselineRestored`. Focused SharedGroup QA is closed. Framework Editor tests cover invalid-candidate preservation/rollback, but Unity validation of transactional Assignment failure paths and broader
IF-ADR-038 recertification, remain pending. Historical Camera certifications stay dated evidence for their exact
former boundaries.

Previous IF-ADR-032 and IF-ADR-029/030 certifications remain historical evidence
for the exact former boundaries; they do not certify IF-ADR-038.

Historical records:

- [IF-ADR-029/030 Camera Composition and Framing Technical Certification — 2026-09-21](../Reconciliation/IF-ADR-029-030-CAMERA-COMPOSITION-FRAMING-TECHNICAL-CERTIFICATION-2026-09-21.md)
- [IF-ADR-030 Local Multiplayer Consumer Unity Proof — 2026-09-20](../Reconciliation/IF-ADR-030-LOCAL-MULTIPLAYER-CONSUMER-UNITY-PROOF-2026-09-20.md)
- [Camera Full Technical Certification — 2026-09-12](../Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)

## Current Stage B / FIRSTGAME priorities

1. **Player** — Scene Player physical/contextual lifecycle has historical proof through `GameplayReady`; Actor occurrence now owns physical/spatial state and replacement pose. Character Selection and Character Selection Multiplayer Split Screen migration/Play Mode are complete; broader Unity recertification and remaining sample migrations are pending.
2. **Loading / Readiness** — positive Game Flow consumer lane proven; negative/terminal robustness remains QA-owned.
3. **Camera** — IF-ADR-038 is Accepted. Assignment/Occurrence/Fallback runtime cuts A-H remain present; CAMERA-038-L derives `PlayerInput.camera` topology from Individual Assignments. Local Multiplayer SharedGroup, Character Selection Fixed Follow, and Character Selection Multiplayer Split Screen are migrated and have manual Play Mode PASS. IF-ADR-043 physical Output participation passed manual 0/1/2/Leave/Rejoin validation and current automated regressions. Framework EditMode is 175/175 PASS, Camera Editor 78/78 PASS, and QA-NEW-004 Session Camera continuity is 9/9 PASS. Focused SharedGroup QA is closed; transactional Assignment failure paths and broader IF-ADR-038 recertification remain pending.
4. **Pause** — runtime certified; remaining work is consumer authoring/usability only.
5. **Audio** — IF-ADR-040 Audio composition migration validated: EditMode 5/5 + QA-NEW-005 12/12 with `BaselineRestored`; historical 44/44 remains pre-migration evidence. Consumer integration is proven; API maturity promotion is separate.
6. **Progression Save** — real consumer persistence/usability proof remains.
7. **Editor/Product Surface** — continue feature-owned Inspector/workflow evidence under ADR-010.

## Future / deferred contracts

- exact-Slot public Join;
- public Slot/device/InputUser/control-scheme ownership observation;
- device disconnect/reconnect and reassignment semantics;
- heterogeneous per-Slot Host Provisioning;
- Scene-Provided prepared Actor replacement, pending an explicit physical-ownership contract;
- generic respawn/checkpoint/dynamic Spawn beyond ADR-021;
- application-scoped stable-ID resolver;
- Session-scoped frame-rate override;
- persisted frame-rate preference integration;
- advanced BGM simultaneous-source/crossfade semantics unless separately accepted.

`PLAYER-COMMAND-SURFACE-READINESS / DEFERRED` remains a product-availability concern: valid authored commands may be runtime-unbound and must reject without fallback until live scoped access exists.

## Current architecture / reconciliation records

- [IF-ADR-038 — Session Player Camera Assignments and Occurrence Lifecycle](../ADRs/IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md)
- [IF-ADR-043 — Individual Player Camera Output Physical Participation](../ADRs/IF-ADR-043-Individual-Player-Camera-Output-Physical-Participation.md)
- [IF-ADR-044 — Consumer-Controlled Player Runtime Gameplay Availability](../ADRs/IF-ADR-044-Consumer-Controlled-Player-Runtime-Gameplay-Availability.md)
- [IF-ADR-045 — Persistent Content Scene Reference Must Survive Player Builds](../ADRs/IF-ADR-045-Persistent-Content-Scene-Reference-Player-Builds.md)
- [IF-ADR-041 — Route-Scoped Activity Transition Observation](../ADRs/IF-ADR-041-Route-Scoped-Activity-Transition-Observation.md)

- [IF-ADR-009 — Activity Local Visibility Rules](../ADRs/IF-ADR-009-Activity-Local-Visibility-Rules.md)
- [IF-ADR-015 — Player Provisioning Commands and Consumer Observation Surface](../ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md)
- [IF-ADR-019 — Session Player Lifetime and Activity Representation Authority](../ADRs/IF-ADR-019-Session-Player-Lifetime-and-Activity-Representation-Authority.md)
- [IF-ADR-020 — Session Player Leave and Resource Release Authority](../ADRs/IF-ADR-020-Session-Player-Leave-and-Resource-Release-Authority.md)
- [IF-ADR-021 — Route Spatial Entry and Activity Explicit Relocation](../ADRs/IF-ADR-021-Activity-Player-Actor-Initial-Placement-Authority.md)
- [IF-ADR-029/030 Camera Composition and Framing Technical Certification — 2026-09-21](../Reconciliation/IF-ADR-029-030-CAMERA-COMPOSITION-FRAMING-TECHNICAL-CERTIFICATION-2026-09-21.md)
- [IF-ADR-030 Local Multiplayer Consumer Unity Proof — 2026-09-20](../Reconciliation/IF-ADR-030-LOCAL-MULTIPLAYER-CONSUMER-UNITY-PROOF-2026-09-20.md)
- [Camera Output Participation and Layout Authority Reconciliation — 2026-09-12](../Reconciliation/IF-CAMERA-OUTPUT-LAYOUT-AUTHORITY-RECONCILIATION-2026-09-12.md)
- [IF-ADR-028 Focused Physical Presentation / PlayerInput Validation — 2026-09-17](../Reconciliation/IF-ADR-028-FOCUSED-VALIDATION-2026-09-17.md)
- [Camera Full Technical Certification — 2026-09-12](../Reconciliation/IF-CAMERA-FULL-TECHNICAL-CERTIFICATION-2026-09-12.md)
- [IF-ADR-026 Shared Camera Technical Certification — 2026-09-09](../Reconciliation/IF-ADR-026-SHARED-CAMERA-TECHNICAL-CERTIFICATION-2026-09-09.md)
- [IF-ADR-023 — Player Actor Runtime Host and Presentation Authority](../ADRs/IF-ADR-023-Player-Actor-Runtime-Host-and-Presentation-Authority.md)
- [IF-ADR-024 — Prepared Actor Replacement Public Contract](../ADRs/IF-ADR-024-Prepared-Actor-Replacement-Public-Contract.md)
- [IF-ADR-009 Contribution / Visibility Technical Certification — 2026-08-30](../Reconciliation/IF-ADR-009-CONTRIBUTION-VISIBILITY-TECHNICAL-CERTIFICATION-2026-08-30.md)
- [Historical Player Aggregate Recertification — 2026-08-24](../Reconciliation/IF-PLAYER-CURRENT-AGGREGATE-RECERTIFICATION-2026-08-24.md)
- [IF-ADR-015B — Player Actor Selection Public Surface Certification — 2026-08-26](../Reconciliation/IF-ADR-015B-Player-Actor-Selection-Public-Surface-Certification-2026-08-26.md)
- [IF-ADR-023 Player Actor Runtime Technical Certification — 2026-08-29](../Reconciliation/IF-ADR-023-PLAYER-ACTOR-RUNTIME-TECHNICAL-CERTIFICATION-2026-08-29.md)
- [IF-ADR-023A Player Actor Occurrence Identity Boundary — 2026-08-31](../Reconciliation/IF-ADR-023A-PLAYER-ACTOR-OCCURRENCE-IDENTITY-BOUNDARY-2026-08-31.md)
- [IF-ADR-024 Prepared Actor Replacement Technical Certification — 2026-09-02](../Reconciliation/IF-ADR-024-PREPARED-ACTOR-REPLACEMENT-TECHNICAL-CERTIFICATION-2026-09-02.md)

## Documentation maintenance

- accepted ADRs remain normative;
- reconciliation records hold dated technical evidence;
- this tracker holds current mutable state, not full execution history;
- FIRSTGAME evidence is consumer/product evidence;
- historical certification counts are preserved rather than rewritten to imply later coverage.

## IF-PLAYER-MATURITY-004 — Public contract maturity

Stable, bounded contracts: PlayerGameplayAvailabilityBlockTrigger, PlayerGameplayAvailabilityBlockToken, PlayerGameplayAvailabilityBlockResult, PlayerGameplayAvailabilityBlockStatus; ISessionCameraAssignmentCommandPort and ISessionCameraAssignmentCommandConsumer.

Still Experimental: IPlayerSessionScopedAccess as a whole, Manager-Provisioned/Session profiles and Actor profiles, SessionCameraAssignmentCommandTrigger, SessionCameraAssignmentAsset, CameraOutputDefinition, and ActorCameraSubjectAuthoring. Availability methods remain callable through the Experimental scoped-access interface. Stable Camera command contracts accept Experimental Assignment assets; their status does not extend to those dependencies.

Evidence boundary: IF-ADR-044 QA 8/8 (BaselineRestored) plus FirstGame manual P1/P2 ownership, teardown and Pause composition; IF-ADR-039 Framework Editor/GameFlow evidence for Activate/Replace/Clear, including invalid-candidate state preservation in Editor tests. These results cover recorded scenarios, not Transition composition for Availability or every Unity Assignment failure topology. SharedGroup IF-ADR-042 QA 7/7 is closed; broader IF-ADR-038 recertification remains open.

Historical QA/consumer runs in QAFramework and FirstGame resolved Framework through a local file: dependency and did not record an immutable Framework SHA. This does not invalidate results for tested behavior. Under IF-GOV-001 §8, routine future records capture the scenario/result, known Unity/package source and version, cleanup, and revision when available. Exact execution identity and additional artifacts are required only by an applicable gate or a candidate-specific claim that depends on them.

## IF-ADR-048 Phase 2 closeout review — 2026-10-14

**Disposition: Phase 2 remains open; do not advance to Phase 3.** The project
workspace rules prohibit this executor from running Unity, NUnit, Play Mode,
smokes or batchmode. This review used the Project Owner's supplied Test Runner
screenshot and terminal lines, static source inspection, package-resolution
inspection and `git diff --check`; no execution result was produced by this
review.

The Project Owner supplied the QAFramework EditMode screenshot showing
**357/357 passed, 0 failed**. It includes the Framework Persistent Content
(24 tests), Route Lifecycle (8), and Progression Save (8) assemblies. The same
reported suite includes both `ActivityCommitAuthorityProjectionTests` and the
three `FrameworkRuntimeHostControlledStopTests` UnityTest cases. This is valid
user-reported evidence for that run, but its screenshot has no raw XML/runner
log, Unity version or source manifest. The later I4.2 OBSERVABLE terminal is
recorded above as user-reported **PASS 8/8**, `cleanup='BaselineRestored'`; it
also lacks Unity version and Framework source identity. No historical LEASE
BLOCKED result is reclassified.

Current working-source identifiers, calculated for a reproducible next run:

- Framework `HEAD`: `861620ca00b662a31d3542ff79b5c1f632d843ee` (dirty working tree).
- Framework manifest SHA-256: `418396ffa4374dd1dd295ca21fd200a1218e43fa05d0207e8373760856516564`
  over 2,049 files under `Runtime/`, `Tests/` and `package.json`; each file is
  SHA-256 hashed, normalized relative paths use `/`, records are sorted by
  ordinal path and hashed as UTF-8/LF `digest  path` lines. This identifies the
  current working snapshot only; it does not retroactively bind the screenshot
  or prior terminals to that snapshot.
- QAFramework `HEAD`: `669b0e93ef63eadb6babd4a76498364f3319ef44` (dirty working
  tree). The seven in-scope QA asset trees have manifest SHA-256
  `80e9bf026f3277bcc6c91fbbfc05edb92e2bc7fda56dd7f2ba36255cf93487fd`
  (383 files, same manifest algorithm). `Packages/manifest.json` SHA-256 is
  `320431e00f9a3d04706b79c5c981b6f4b846d45bb6d937db2fd3af1108690cd8` and
  `Packages/packages-lock.json` SHA-256 is
  `4af5e8592ef6e1cff2e72bde24b65263b4cb5a42ad21ec708663dfec1e08ebe8`;
  Framework resolves through `file:C:/Projetos/ImmersivePackages/com.immersive.framework`.

Static inspection also found these named Phase 2 gaps:

1. The Host shutdown fixtures prove startup, Pause restoration, Route release
   rejection/retry, Persistent Content Session release rejection/retry and
   same-Host ownership. They do not exercise I-04's required single integrated
   shutdown with two joined Session occurrences, Camera Assignment/Output and
   Pause/Input receipts, injected cleanup rejection and retry to baseline.
2. I-05 has two reported-green Activity authority projection NUnit cases, but
   no current-source integrated QA terminal was supplied for the planned
   pre-commit and post-commit Route/Activity failure matrix with owner, result
   and restored-baseline assertions.
3. I-06's Progression Save NUnit suite is present in the screenshot, while the
   Phase 2 matrix still has no QAFramework Stable Store scenario. Static source
   review found no active Save consumer in QAFramework; a fabricated backend
   smoke would not establish behavior of a real consumer implementation.
4. The 357/357 report and supplied QA terminals have not been tied to the
   current Framework/QA manifests above. Candidate-specific execution identity
   remains unproven, and the full working-tree `git diff --check` currently
   reports trailing whitespace in Unity-serialized empty fields and generated
   `.meta` files.

The 357/357 screenshot and supplied scenario terminals are preserved as
user-reported evidence; they are not discarded or promoted beyond their
recorded scope. The current code changes remain implemented, but these gaps
prevent the required reviewed, source-identified Phase 2 exit. Publication
remains **NO-GO**.

## IF-ADR-048 Phase 2 test-gap follow-up — 2026-10-14

**Disposition: Phase 2 remains open / NO-GO.** The Project Owner authorized
creating missing tests and attempting their Unity execution in this session.
No test result is claimed from the attempts below.

Added current-source NUnit coverage:

- I-05: `RouteCommitAuthorityProjectionTests` checks that a pre-commit Route
  failure does not claim destination authority and that a post-commit reveal
  failure projects the committed Route and Activity. These are result/state
  projection tests, not injected lifecycle failures or integrated QA evidence.
- I-04: `ControlledStop_TwoJoinedPlayersCameraAndPause_RetriesSameHostToBaseline`
  joins two QAFramework Session Players, waits for their exact individual
  Camera Assignment/Output and `PlayerInput.camera` bindings, verifies the
  supported Pause/Input binding and pause restoration, injects one Session
  participant release rejection, and checks canonical Leave, cleared physical
  owners, exact Host/root retention and same-Host retry. This covers the
  integrated two-Player shutdown slice; it does not replace the remaining
  per-stage Camera fault matrix or every Activity/Route teardown injection.
- I-06: `JsonStore_ImplementsStableReadWriteDeleteContractWithExactPayload`
  exercises the real JSON implementation through `IProgressionSaveStore` for
  Missing -> Written -> Found -> Deleted -> Missing and exact Binary payload
  preservation. `JsonStore_WriteFailureDoesNotClaimTheSlotWasWritten` checks
  that a real backend write failure is not reported as success. Existing
  payload immutability, value equality, formats and result-code cases remain.
  The concrete JSON adapter is Experimental; these tests do not promote its
  construction API or establish a QA consumer smoke.

Unity execution status: **BLOCKED before compilation/Test Runner**. Both the
batch invocation and the GUI Editor attempt stopped when the Unity Licensing
Client IPC channel `LicenseClient-renat` refused the connection; no focused
test XML/result file was produced. No Unity window was available to control.
The available `dotnet` command has no installed SDK, so it could not provide a
separate C# build result. These are environment blocks, not test failures.
Scoped `git diff --check` completed without whitespace errors and the changed
test asmdef parses as JSON.

Current working-source identity for a subsequent run:

- Framework HEAD `861620ca00b662a31d3542ff79b5c1f632d843ee` (dirty working tree).
- Framework manifest SHA-256 `983fbd4b2b041d29f244e5b0ed22f036b544569eabbef104bb8a26a2c46697ac`
  over 2,051 files under `Runtime/`, `Tests/` and `package.json`, using the
  manifest algorithm recorded in the closeout above.
- QAFramework remains at HEAD `669b0e93ef63eadb6babd4a76498364f3319ef44`
  (dirty working tree); no QAFramework asset or scenario changed in this
  follow-up.

Remaining Phase 2 evidence gaps include I-05's current-source integrated
Route/Activity pre-commit and post-commit failure terminal with owner, result
and restored-baseline assertions; I-06's exact-candidate QA terminal; and
candidate-specific execution provenance. The new I-04 and I-06 tests must run
in Unity before they can be recorded as PASS. Keep Phase 2 and publication
**NO-GO** until those evidence requirements are reviewed; this follow-up does
not close a workpackage or phase.

## IF-ADR-048 Phase 2 user-run validation follow-up — 2026-10-14

The Project Owner supplied a Unity Test Runner screenshot showing **362 passed,
0 failed** in QAFramework EditMode. `FrameworkRuntimeHostControlledStopTests`
shows 4/4 green, including
`ControlledStop_TwoJoinedPlayersCameraAndPause_RetriesSameHostToBaseline`;
the Progression Save assembly also shows 10/10 green. This supersedes the
execution-status statements above that those newly added I-04/I-06 NUnit tests
were still awaiting a Unity run. The integrated I-04 two-Player Camera/Pause
controlled-stop slice is now user-reported **PASS**; its other per-stage
Camera/Leave/Activity/Route failure injections remain outside this test.

The Project Owner also supplied terminal results:

- `QA-NEW-002`: **PASS**, submitted/completed Activity readiness requests,
  pending state proved, release issued, baseline restored, empty
  `firstDivergence` and `cleanupIssue`.
- `QA-NEW-003`: **PASS**, Route A → B → restored A, exact owner survived,
  baseline restored, empty `firstDivergence` and `cleanupIssue`.

These close only the reported Activity-readiness and successful Route
replacement executions. They do not establish the planned integrated
pre-commit and post-commit Route/Activity failure matrix; in particular,
`RouteCommitAuthorityProjectionTests` remains projection coverage rather than
an injected lifecycle-failure terminal. The Progression Save NUnit PASS also
does not substitute for the planned QAFramework Stable Store terminal.

Evidence is user-reported. The screenshot has no raw XML, Unity version or
runner-generated candidate manifest. The QAFramework lock resolves
`com.immersive.framework` through
`file:C:/Projetos/ImmersivePackages/com.immersive.framework`, but the supplied
run itself does not print source identity. No Runtime, Tests, package, QA
asset, Packages or ProjectSettings files were changed after the supplied
results; only this tracker was updated. On that shared-workspace basis, the
run is associated with these current working-byte manifests:

- Framework HEAD `861620ca00b662a31d3542ff79b5c1f632d843ee` (dirty); SHA-256
  `bf507bd7c3c43e4087309694e41d774c95f71b9bb7244eca0d183c18f9946e6d`
  over 2,051 files in `Runtime/`, `Tests/` and `package.json`.
- QAFramework HEAD `669b0e93ef63eadb6babd4a76498364f3319ef44` (dirty); SHA-256
  `6dadfec4fb62edd82fa3bb246b693629a55c104cc19b09f9cf9cebc5fada3060`
  over 711 files in `Assets/`, `Packages/` and `ProjectSettings/`.

The Unity version and raw XML remain unavailable, so these are user-reported
results associated with exact shared-workspace source bytes, not independently
reproduced publication certification. Phase 2 remains **in progress / NO-GO**;
publication remains **NO-GO**.

## IF-ADR-048 Phase 2 post-commit failure regression — 2026-10-14

The Project Owner's previously supplied **362/362 EditMode** screenshot and
`QA-NEW-002` / `QA-NEW-003` terminals remain valid at the source identity
recorded in the preceding section. After those runs, one additional I-05
integration test was authored in
`Tests/Editor/PersistentContent/FrameworkRuntimeHostControlledStopTests.cs`:

- `PostCommitReadinessFailure_RouteStartupActivityRetainsCommittedRouteAndRestoresBaseline`
  starts the real Framework Host on its authored Route/Activity, requests a
  second persisted Route whose Startup Activity materializes a persisted
  Activity-owned content scene with one Required readiness participant, and
  fails that participant only after its occurrence reaches `Preparing`. It
  asserts `FailedCommittedTargetNotReady` with
  `DestinationAuthoritative=true` while the exact target Route and its Startup
  Activity remain current. It then requests the original Route and asserts the
  original Route/Activity, restored active scene, one readiness release
  callback, and unloaded failed Route and Activity content.
- The test changes no Runtime implementation, public API, serialized product
  asset or Stable contract. Its QA-only readiness participant remains
  Experimental; the test does not promote that surface.
- This new test has **not** been compiled or executed in Unity. The earlier
  362-test screenshot does not cover it. The current Framework working source
  is HEAD `861620ca00b662a31d3542ff79b5c1f632d843ee` (dirty), with manifest
  SHA-256 `a8b5ef0a5d622bac672b3352685fa2a82944471d1008eede51747821875d567d`
  over 2,051 files in `Runtime/`, `Tests/` and `package.json`, using the
  manifest procedure recorded above. This is the candidate identity for the
  next run, not an execution identity.

The latest user-reported Save assembly result remains **10/10** and covers the
current NUnit payload/store contract suite, including payload immutability,
real JSON read/write/delete through `IProgressionSaveStore`, backend write
failure and distinct result-code preservation. QAFramework has no active
Progression Save consumer; no fake QA consumer or duplicate runtime smoke was
added. That consumer-level evidence remains absent and must stay distinct from
the package-contract PASS.

`git diff --check -- Tests/Editor/PersistentContent/FrameworkRuntimeHostControlledStopTests.cs`
completed without whitespace errors. No Unity compile/test was run in this
follow-up. I-05 remains open pending this new integration test's Unity result.
The current integrated evidence still does not cover Route pre-commit failure
or a direct Activity-request post-commit failure; the new test covers a Route
request whose Startup Activity fails readiness after Route commit. Phase 2 and
publication remain **in progress / NO-GO**.

## IF-ADR-048 Phase 2 user-run 363-test result and I-05 extension — 2026-10-14

The Project Owner supplied a Unity Test Runner screenshot showing
**QAFramework 363/363 passed, 0 failed**. The screenshot shows
`FrameworkRuntimeHostControlledStopTests` at **5/5 green**, including the
Route Startup Activity post-commit readiness regression described above. This
supersedes the previous statement that the Route integration test had not been
run. The result is user-reported: the screenshot does not include Unity
version, raw runner XML or an execution-generated source manifest, so it is
not candidate-specific release certification.

After that screenshot, the I-05 test was extended and renamed to
`PostCommitReadinessFailure_RouteAndDirectActivityRetainCommittedOwnersAndRestoreBaseline`.
It retains the already-reported Route case and adds a direct Activity request
whose required readiness participant fails after commit, then checks retained
Route/Activity authority and recovery to the exact baseline Activity. The
extension has not yet been compiled or run in Unity; the screenshot does not
cover these new assertions. It changes no Runtime implementation, public API,
serialized product asset or Stable contract.

I-05 remains open pending Unity validation of the extended test and integrated
pre-commit failure evidence. Candidate-specific Phase 2 evidence and the
remaining approved evidence matrix are also outstanding. Phase 2 and
publication remain **in progress / NO-GO**.

## IF-ADR-048 Phase 2 readiness regression diagnosis — 2026-10-14

The Project Owner ran the combined Route + direct Activity version and
reported a failure after 7.220 seconds: the expected Route committed-readiness
error did not appear. The supplied log excerpt contains Host startup and
controlled-stop cleanup only; it does not establish whether the test ever
captured the target readiness participant or issued `FailPreparation`.

The regression was split to preserve the previously reported-green Route test
under its original name and isolate direct Activity failure in a separate
UnityTest. Both cases now enqueue the expected error only immediately before
the corresponding `FailPreparation` call. If the readiness phase is not
reached, the failure assertion reports the loaded scene, participant state
and request task status instead of being masked by an unconsumed expected-log
entry. This is a test-diagnostic correction; it does not change Runtime or
claim that the missing log was caused by a Runtime defect.

These latest test-source changes have only passed scoped static whitespace
validation; they have not been compiled or run in Unity. I-05 remains open,
and Phase 2/publication remain **in progress / NO-GO**.

## IF-ADR-048 Phase 2 follow-up: Activity fixture identity lost on import — 2026-10-14

The Project Owner reran the separated Route Startup Activity integration and
reported that the request faulted before the persisted Activity content scene
loaded (`Scene valid=False`, no readiness participant, `requestStatus=Faulted`).
The supplied direct-Activity log identifies the same fixture failure more
precisely: `RequestActivityAsync` rejected the target because its Activity ID
was missing, and the diagnostic showed the default Activity display name. The
readiness failure was therefore never injected; this output does not show a
Runtime post-commit readiness regression.

The fixture now saves/imports the Activity identity and display name before
assigning the Activity Content Profile, then checks those serialized values
again after profile assignment and a second import. It also verifies the exact
reloaded profile reference and rechecks identity after entering Play Mode,
immediately before each request. These assertions localize any repeated asset
round-trip loss to fixture preparation instead of letting a later request
fault obscure it. No Runtime, public API or Stable contract changed.

This correction has only received static source/diff inspection; Unity compile,
EditMode and PlayMode execution remain pending. Rerun both
`PostCommitReadinessFailure_RouteStartupActivityRetainsCommittedRouteAndRestoresBaseline`
and `PostCommitReadinessFailure_DirectActivityRetainsCommittedOwnerAndRestoresBaseline`.
I-05 remains open; Phase 2 and publication remain **in progress / NO-GO**.

## IF-ADR-048 Phase 2 follow-up: direct Activity recovery release count — 2026-10-14

The Project Owner reran the two post-commit readiness tests and reported one
remaining failure: the direct Activity test expected two readiness release
callbacks but observed one. Its trace confirms the intended behavior: the
failed target Activity was occurrence 2, the recovery request restored the
baseline Activity, and that operation released one Activity-owned scene. This
test has one failed committed Activity occurrence; the Route variant is a
separate test and its callback counter is not shared.

The stale direct-test assertion was corrected from two callbacks to one and
its diagnostic now refers to the single occurrence. The Project Owner then
supplied a Unity Test Runner screenshot showing **QAFramework 364/364 passed,
0 failed**, with `FrameworkRuntimeHostControlledStopTests` at **6/6 green**;
both the direct Activity and Route Startup Activity post-commit tests appear
green. This supersedes the pending-rerun status above. The result is
user-reported evidence; the screenshot does not include Unity version, raw
runner XML or a source manifest/hash for the execution.

No Runtime or Stable contract changed in the assertion correction. The two
post-commit regressions are now user-validated, but I-05 remains open for its
other recorded integration evidence. Phase 2 and publication remain **in
progress / NO-GO**.

## IF-ADR-048 I-05 Route pre-commit projection follow-up — 2026-10-14

The preceding 364/364 screenshot remains user-reported evidence for the source
that existed before this follow-up. It does not cover the changes below.

Static tracing of `RouteLifecycleRuntime.StartRouteAsync` found a pre-commit
partial outcome: the current Activity can be cleared before Scene Lifecycle
rejects an unavailable target Primary Scene. The canonical Route remains the
previous Route, but the Host previously kept its pre-request Activity snapshot
because it projected Route results only on success or when the destination
Route was authoritative. This left Host state stale relative to the live
GameFlow authority.

The Host now projects the current `RouteLifecycleStartResult` after a
non-authoritative Route result when the exact previous Route remains live. The
new integration regression,
`RoutePrimarySceneUnavailableBeforeCommit_ProjectsRetainedRouteAndCanRestoreActivity`,
uses a valid Route identity with an unavailable Primary Scene, asserts the
failed destination is not authoritative, checks that Host state matches the
retained Route and cleared Activity, verifies the primary scene and Transition
Gate, then restores the baseline Activity through the same Host.

This is a scoped Runtime state-projection correction and integrated test for
the observed partial Route outcome; it does not add blanket rollback or alter
Stable API. The test and correction have not been compiled or run in Unity.
`git diff --check` passed for the changed Runtime and test files. Current
Framework working-source manifest SHA-256 at that checkpoint was
`7b340c1753191fde554e56e088eb2e7fcea9ee20d253cc3a58e734df1275b092` over
2,051 files under `Runtime/`, `Tests/` and `package.json`; it identifies the
source before the subsequently reported Unity run and follow-up below, not an
execution identity.

I-05 remains open pending Unity validation of this pre-commit regression and
review of the remaining applicable failure paths. Phase 2 and publication
remain **in progress / NO-GO**.

### User-run follow-up — unavailable Route diagnostic log — 2026-10-14

The Project Owner ran
`RoutePrimarySceneUnavailableBeforeCommit_ProjectsRetainedRouteAndCanRestoreActivity`.
The failure was an unconsumed Unity `Error` log emitted for the deliberately
unavailable target Primary Scene, rather than a reported assertion about Route
or Activity ownership. The same log exposed a diagnostics defect: for a
non-authoritative `FailedInvalidConfig` result, the Host labeled the rejected
target Route as `currentRoute`, even though the previous Route remained the
live authority.

The regression now expects the specific Scene Composition error and also
requires its structured fields to identify the retained `host-stop.route` as
current. Route request summary and diagnostic fields now project current Route,
Activity and readiness from the Host's canonical state. For non-authoritative
outcomes, `previousRoute` is taken from that retained state; authoritative
results continue to report the previous Route carried by their lifecycle
result. No lifecycle policy or public API changed.

Static `git diff --check HEAD` passed for the changed Runtime, state and test
files. The 2,051-file Runtime/Tests/package manifest for this updated source is
`be660ae6cbfd643f4b39b52e8cde6ed9d427f100744bef779ec182639dd02ce9`.
Unity compilation and rerun remain pending; no PASS is claimed. I-05, Phase 2
and release remain **in progress / NO-GO**.

### User-run follow-up — retained Activity projection remained stale — 2026-10-14

The Project Owner reran the unavailable-Route regression. The expected error
log was consumed and correctly reported `host-stop.route` as current, but the
test then found `Host.State.CurrentActivity` still pointing to `Host Stop
Activity` after the runtime had cleared it. The trace shows the clear/release
completed before the target Primary Scene composition failed.

`RouteLifecycleRuntime.StartRouteAsync` now updates its retained Route result
immediately after the previous Activity clear completes. When Game Flow sees
that the target Route did not start, it refreshes its flow-context projection
from the Route Lifecycle owner. The regression also checks that the retained
Route result and Host state both expose the baseline Route with no current
Activity, then verifies recovery to the baseline Activity.

`git diff --check HEAD` passed for the changed Runtime and test files. The
2,051-file Runtime/Tests/package manifest for this source is
`1aeb4fef20539a4cb163f2f606787481e4efceaf68c8926ab1ad13725ec1d178`.
Unity compilation and rerun remain pending; I-05, Phase 2 and release remain
**in progress / NO-GO**.

### User-run validation — Route pre-commit projection — 2026-10-14

The Project Owner supplied a Unity Test Runner screenshot showing **QAFramework
365/365 passed, 0 failed**. It shows `Immersive.Framework.PersistentContent.Editor.Tests.dll`
at 28/28 and `FrameworkRuntimeHostControlledStopTests` at **7/7**, including
the unavailable-Route pre-commit regression and the two post-commit readiness
recovery cases. This resolves the reported assertion failure for that run and
supersedes the prior pending-rerun status for these integrated regressions.

The result is user-reported from the screenshot. It does not include Unity
version, raw NUnit XML or a runner-generated source fingerprint. The Framework
working-source manifest remains
`1aeb4fef20539a4cb163f2f606787481e4efceaf68c8926ab1ad13725ec1d178` over 2,051
Runtime/Tests/package files; the screenshot itself does not bind the run to
that fingerprint. Record the three I-05 integration cases as user-reported
PASS, but keep I-05/Phase 2 open for source-identified certification and the
remaining readiness evidence review. Release remains **NO-GO**.

## IF-ADR-048 Phase 2 closure — composed evidence accepted — 2026-10-14

**Disposition: Phase 2 CLOSED; proceed to Phase 3 entry preparation.** The
Project Owner directed that behavior already exercised by complementary
component, integration and QA scenario proofs be accepted compositionally;
one bespoke test is not required when the combined evidence covers the same
contract boundary and recovery outcome. This disposition supersedes earlier
tracker notes that kept Phase 2 open solely for a separately named test or
because evidence was split across focused suites.

The Project Owner supplied the Unity Test Runner screenshot showing
**QAFramework 365/365 passed, 0 failed**. It includes all displayed Editor
assemblies, including Persistent Content **28/28** and
`FrameworkRuntimeHostControlledStopTests` **7/7**. The latter covers startup
and controlled stop, retained roots, same-Host release retry, two joined
players with Camera/Pause cleanup, post-commit direct Activity and Route
readiness failures, and pre-commit unavailable Route recovery. These results
compose with the previously supplied component suite and QA scenario evidence
recorded in this tracker for Player/Session, Pause/Input, Camera, Route and
Activity behavior. The Progression Save suite is **10/10** for its Stable
`IProgressionSaveStore` payload/result contract. QAFramework has no active
Progression Save consumer, so a fabricated consumer smoke is not required.

The composite evidence is accepted at its reported scope; no additional
Phase 2-specific test remains to be requested. The historical IF-ADR-046 I4.2
LEASE result remains **BLOCKED 0/3** and is not relabeled PASS: it is outside
the accepted Stable Join claim and does not block this phase. Accepted bounded
residuals remain: terminal Unity callbacks are best-effort; asynchronous
GameFlow has no arbitrary timeout; partial Route outcomes can be legitimate;
Progression Save guarantees remain within the Stable store/payload boundary;
Experimental APIs and evidence do not promote or enlarge Stable contracts.

The 365/365 result is user-reported against QAFramework's local `file:` package
resolution. The Framework source manifest associated with the run in the
preceding checkpoint is
`1aeb4fef20539a4cb163f2f606787481e4efceaf68c8926ab1ad13725ec1d178` over 2,051
Runtime/Tests/package files. Subsequent local cleanup changed only whitespace
in Unity `.meta` files; it did not change Runtime or test source. This closes
operational integrity but does **not** certify an immutable release candidate
or exact distributed artifact.

Phase 3 is the next gate. Its entry action is to review and freeze the current
mission source as a clean Git revision, then complete the Stable contract to
test/consumer matrix for QAFramework, FirstGame and PEGA. The current package
tree is still dirty, so no candidate SHA is claimed and no candidate-specific
recertification has begun. The generated `obj/Debug` outputs discovered during
preparation have been removed from the candidate index and workspace.
Phase 4 exact-artifact consumer validation and Phase 5 release GO remain
pending; publication stays **NO-GO**. No Unity execution was performed in this
documentation update.

## Phase 3 — candidate recertification — 2026-10-14

The approved Stable inventory remains the 107 Runtime types reconciled in
Phase 1. The Project Owner supplied the QAFramework Test Runner result after
the candidate SHA was provided. It shows **365/365 passed**, including
Persistent Content 28/28, Host Admission 6/6, Host Controlled Stop 7/7,
Player Participation 106/106, Progression Save 10/10 and Identity 19/19. The
QAFramework manifest and lock resolve the Framework from the local package
checkout. At intake, this checkout was clean at the frozen SHA above. The
screenshot has no raw NUnit XML or embedded SHA; this is user-reported
candidate evidence linked by the requested exact revision and local dependency.

The supplied QA terminals report **PASS** and `BaselineRestored` for IF-ADR-042
(6/6), IF-ADR-043 (7/7), IF-ADR-044 (8/8), IF-ADR-046 I4 Minimal (1/1), and
IF-ADR-046 I4.2 Observable (8/8). Previously supplied Player Session Join and
Leave/rejoin evidence is accepted compositionally with the Player Participation
suite. The separate I4.2 LEASE result remains Experimental and BLOCKED; it is
not included in the Stable claim.

The matrix records the evidence and boundary without requiring a bespoke test
where complementary package and scenario results establish the same guarantee.

| Workpackage / guarantee group | Candidate evidence supplied | Phase 3 disposition |
|---|---|---|
| I-01 — Scene Lifecycle participant ledger / Persistent Content | `SessionCompositionReleaseTests` and Host startup/shutdown/retry; Persistent Content 28/28 and Host group 7/7. | **PASS** — retained scope, receipts and retry asserted by candidate Editor run. |
| I-02 — scoped release, Availability and physical receipts | Player Participation 106/106, Pause suite green, IF-ADR-044 8/8 and supplied Join/Leave composition. | **PASS** — scoped ownership, release and baseline evidence; artifact consumers remain Phase 4. |
| I-03 — Host admission, bootstrap and controlled stop | Host Admission 6/6 and `FrameworkRuntimeHostControlledStopTests` 7/7. | **PASS** — startup, retained ownership, failure and same-Host retry. |
| I-04 — player, Camera, Activity and Route teardown | Host two-Player/Camera/Pause cleanup test within 7/7; Camera, Pause and Player Participation suites; IF-ADR-043 7/7. | **PASS** — receipt release and baseline restoration covered compositionally. |
| I-05 — GameFlow partial failure and recovery | Three Host integration cases within 7/7 cover pre-commit Route failure, post-commit readiness failure and direct Activity readiness failure. | **PASS** — committed authority retention and recovery; legitimate partial Route outcomes preserved. |
| I-06 — Progression Save payload/store contract | Progression Save Editor 10/10; no active QAFramework Save consumer exists. | **PASS** — payload/store scope; consumer smoke is N/A. |
| I-07 — identity and maturity metadata | Identity Editor 19/19; accepted Stable inventory and API status conformance checks from Phase 1. | **PASS** — no Experimental promotion or serialized schema drift claimed. |
| I-08 — Observer/Command migration and Provisioning gate audit | IF-ADR-042 6/6, IF-ADR-043 7/7, IF-ADR-044 8/8, IF-ADR-046 I4 Minimal 1/1 and I4.2 Observable 8/8; supplied Player Session evidence. | **PASS within declared Stable scope** — I4.2 LEASE remains Experimental/BLOCKED and excluded. |

### Consumer resolution and Phase 4 evidence route

| Consumer | Unity | Current Framework resolution | Candidate evidence required |
|---|---|---|---|
| QAFramework | `6000.5.0f1` | Persisted manifest/lock use the local Framework checkout, whose `HEAD` is candidate commit `a0d4f809fea5c0d7781e2161024eb3c0bcc532fd`; `Runtime/` and `Tests/` match that commit. | 365/365 Editor tests were user-reported against this source. This supplies candidate-source QA evidence; exact tarball was not retained in QAFramework. |
| FirstGame (`planet-devourer`) | `6000.5.0f1` | After the temporary validation, the persisted workspace manifest/lock have been restored to their prior package resolution. | Project Owner reports temporary Preview 11 tarball installation/import/compile with no errors, followed by restoration of the normal Git package reference. The transient lock was not retained. |
| PEGA | `6000.5.0f1` | Saved package files are inaccessible to this task; Project Owner reports restoration of its normal Git package reference. | Project Owner reports temporary installation/import/compile of the same Preview 11 tarball with no errors. The transient lock was not retained. |

The tested source revision is `e98b530a961bd4d5c9795398a2ff42096a4ea95d`.
Package metadata now advances to the unused prerelease `1.1.0-preview.11` for
Phase 4; the published preview remains `.8`. GitHub run 17 rejected immutable
tag `.9` before starting any job because `runner.temp` appeared in job-level
`env`. Run 18 for immutable tag `.10` resolved and installed Unity UPM CLI
`v9.34.0`, but its version command returned `v9.34.0(x64)` and the workflow's
strict string comparison stopped before packing/signing. Neither run produced
an artifact or GitHub Release. The workflow now scopes temporary paths to
step-level contexts and validates the exact semantic version with an optional
architecture suffix. The prerelease workflow's `publish` job remains excluded
for versions containing `-`. Phase 4 still requires the exact signed artifact
installed/imported/compiled in QAFramework, FirstGame and PEGA, with targeted
QA and matching SHA-256. Phase 5 still required explicit release GO for the
exact stable version, source SHA and artifact hash at this checkpoint. The
Project Owner subsequently approved Stable `v1.1.0` on 2026-10-14; the current
release execution is tracked in the current-status section above.

### Phase 4 candidate build — 2026-10-14

The Project Owner authorized pushing the exact immutable tag
`v1.1.0-preview.11`. It points to source commit
`a0d4f809fea5c0d7781e2161024eb3c0bcc532fd`. GitHub Actions run [#19](https://github.com/ImmersiveGames/com.immersive.framework/actions/runs/38617237048)
completed with conclusion `success`: package metadata and installed UPM CLI
were verified, pack/sign completed, candidate and provenance verification
passed, and the candidate was uploaded. Uploaded artifact
`signed-upm-candidate-v1.1.0-preview.11-38617237048-attempt-1` has GitHub
artifact digest `sha256:1f6833f1f95a39de3721fb7fef86e9f0f3d007b60532bb13db1edc81f345cff7`.
The promotion job was skipped. This establishes a reproducible candidate
artifact, not consumer certification or stable publication; independent
cryptographic signature verification is not inferred from the workflow step.

At this candidate-build checkpoint, the exact-artifact consumer import was
still outstanding, so Phase 4 remained **in progress / NO-GO**. The subsequent
consumer-validation closeout below supersedes that intermediate disposition.

### Consumer resolution reconciliation — 2026-10-14

The Project Owner clarified that QAFramework uses the local `.11` source
checkout, while FirstGame and PEGA remain on published `.8` because `.11` has
not been published. The Project Owner also directed that already-passing
gameplay smokes not be repeated because the candidate changes do not alter
Runtime, test or consumer-script behavior. The source diff confirms there are
no changes under `Runtime/` or `Tests/` between the Phase 3 frozen SHA and
candidate commit; existing composed evidence is retained.

Read-only inspection confirms QAFramework's manifest/lock still select the
local Framework checkout (metadata `.11`) and FirstGame's manifest/lock select
published registry `.8`. PEGA's saved package files are inaccessible to this
task; its `.8` resolution is Project Owner-reported. These expected version
states are not yet proof that all three consumers resolved the exact `.11`
Actions artifact. No consumer projects were modified and no Unity tests or
smokes were run. To close Phase 4 without redundant gameplay execution, record
the candidate `.tgz` provenance/hash and its exact resolution/import in each
consumer, reusing the existing composed QA evidence. Phase 4 remains **in
progress** until that artifact identity is established for the required
projects.

### Phase 4 consumer validation closeout — 2026-10-14

The Project Owner reports that the same Preview 11 candidate tarball was
temporarily installed in both Git-based consumer projects and Unity imported
and compiled them without errors. After that validation, the tarball changes
were discarded and both projects were returned to their normal Git package
references. The current manifests therefore represent the restored state,
not the transient candidate validation state. The transient Unity locks and
raw import logs were not retained; this consumer evidence is explicitly
owner-reported.

The evidence is accepted compositionally: candidate workflow run #19 binds
the signed archive to commit `a0d4f809fea5c0d7781e2161024eb3c0bcc532fd`;
QAFramework's 365/365 Editor result was run against that same source, and the
candidate `Runtime/` and `Tests/` are unchanged; both Git-based consumers
reported clean import/compile of the Preview 11 tarball. No gameplay smoke or
Editor test was repeated. On that evidence, Phase 4 is **closed**. Phase 5
was **pending / NO-GO** at this checkpoint. The Project Owner subsequently
approved Stable `v1.1.0` on 2026-10-14; the current release execution is
tracked in the current-status section above.
