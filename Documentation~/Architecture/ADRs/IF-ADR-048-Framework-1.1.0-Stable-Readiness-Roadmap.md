# IF-ADR-048 — Framework 1.1.0 Stable Readiness Roadmap

Status: **Accepted**
Accepted: **2026-10-12**
Type: release governance / architecture execution gates  
Related: [IF-ADR-047](IF-ADR-047-Initial-Stable-Baseline-and-Versioning-Policy.md), [IF-GOV-001](../Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md), [Public-API](../../API/Public-API.md), [Framework tracker](../Tracking/IF-TRACK-Framework.md#110-first-stable-release-readiness)

## 1. Context

[IF-ADR-047](IF-ADR-047-Initial-Stable-Baseline-and-Versioning-Policy.md)
establishes `1.1.0` as the first official Stable baseline, which has not yet
been published. It also removes any generic compatibility or migration
obligation to `1.0.2` and earlier previews. IF-GOV-001 makes maturity a
property of each consumer contract, rather than of the package as a whole.

The current source cut has user-reported Framework EditMode results of 310/310
and QA-IF-ADR-036 Play Mode results of 8/8. These are useful scoped evidence,
but are not tied to a fully certified immutable release candidate. The release
workflow has preparation changes, but has not completed a real candidate,
consumer-installation and promotion run. Known areas requiring investigation or
certification include Player Observer/Command, Provisioning, Leave, Actor
Selection, Camera and reconciliation of active documentation with Stable
attributes and implementation. Their mention is not a finding of defect or a
predetermination of maturity.

## 2. Proposed decision

Adopt the ordered readiness phases in §3 as the path to a 1.1.0 release
decision. **Contract maturity and architectural closure precede code freeze;
code freeze and evidence on an immutable artifact precede distribution.** A
package version label, passing source tests or an accepted ADR alone does not
establish release readiness.

The release remains **NO-GO** until every required phase exits with recorded
evidence and an authorized decision-maker records **GO** for the exact artifact
to be published. A missing result is not a pass. A failure must be fixed,
shown not to affect a Stable guarantee, or explicitly risk-accepted by the
authority responsible for that contract under IF-GOV-001 and its owning ADR.

### 2.1 Preserved decisions and boundaries

- IF-ADR-047 remains the authority for the initial baseline and forward SemVer
  policy. This proposal does not reopen it or create a compatibility
  requirement for any pre-1.1.0 version.
- IF-ADR-036 remains Accepted: structural `GameplayReady` is independent of
  operational `RuntimeGameplayAvailable`; the unsupported simultaneous
  Pause/Transition-Gate composition is not silently added to its guarantee.
- IF-ADR-046 remains Accepted for its explicitly bounded
  `PlayerSessionJoinTrigger` Stable closure, including its recorded residuals
  and provenance scope. It does not promote adjacent contracts or certify the
  whole package.
- The `ActivityPlayerAdmissionStage` removal and other completed cuts remain
  closed. This proposal does not introduce replacement contracts.
- Experimental, Internal and other non-Stable contracts retain their
  individual status. No type, feature or serialized surface is promoted merely
  to make the package releaseable. No broad legacy compatibility layer is
  required.

## 3. Ordered readiness phases

| Phase | Entry criteria | Required work | Verifiable exit criteria |
|---|---|---|---|
| **1 — Architectural and contract closure** | IF-ADR-047 is Accepted; the inventory source is `FrameworkApiStatusAttribute`, Public-API and owning ADRs. | Inventory **all currently Stable Framework contracts across every subsystem**, not only Player and Camera. Reconcile each contract's declared maturity, public/serialized guarantees, accepted architecture, implementation and known consumers. Apply investigation depth proportional to each Stable guarantee. Investigate the gaps listed in §4 at their owning boundaries. Do not presume a defect or promote Experimental APIs. | A reviewed inventory covers every current Stable contract and has no unexplained disagreement among attributes, Public-API, accepted ADRs and implementation. Each open finding is closed with evidence, shown outside the Stable boundary without weakening another Stable guarantee, or explicitly risk-accepted by its accountable authority. Required architecture decisions are Accepted before freeze. |
| **2 — Operational integrity** | Phase 1 identifies the Stable guarantees and material lifecycle paths whose integrity must be demonstrated. | Review and test the actual Runtime paths for authority, ownership, failure handling, rollback, retry, release and cleanup at those contract boundaries. Check package dependency and serialization integrity. Investigate failures rather than weakening assertions or inferring a Runtime defect from a failed fixture. | Required focused tests and static checks pass on the reviewed source revision; material failure/recovery paths have named evidence. No unexplained failure can violate a Stable guarantee. Remaining bounded risks have an explicit owner, rationale and acceptance under IF-GOV-001/owning ADR. |
| **3 — Transversal recertification** | Phases 1–2 exit; a reviewed candidate source revision is frozen and identified by SHA. | Validate that source revision with the required Framework test lanes and QAFramework scenarios. Complete and review the contract-to-test/consumer certification matrix: name each Stable guarantee, required scenario, consumer, Unity version and evidence to be collected in Phase 4. This phase validates the candidate revision and prepares the artifact certification; it does not claim that a packaged artifact has been installed or certified. | Candidate-revision validation passes at its stated scope. The approved matrix covers every Stable guarantee and material lifecycle path, identifies QAFramework and relevant consumers (including FirstGame and PEGA), and specifies exact-artifact identity/import/compile and targeted QA evidence for Phase 4. Failures, exclusions and unresolved gates are explicit; historical and user-reported results are not relabeled as candidate certification. |
| **4 — Immutable Release Candidate** | Phase 3 exits; the candidate SHA, approved certification matrix and package metadata are reviewed; publication remains disabled pending approval. | Build the RC/archive from the immutable revision, recording package version, dependency graph, build tool/workflow identity and artifact SHA-256. Install that exact artifact in QAFramework and each relevant consumer in the matrix, including FirstGame and PEGA, then run the required import/compile and targeted QA. Validate the prepared promotion path without publishing. If Stable metadata changes the artifact bytes, build the final-version candidate and repeat all artifact-dependent certification against those exact bytes. | Provenance ties artifact hash to source SHA, metadata, dependencies and build run. Every required project records that same artifact identity as installed/resolved and has passing import/compile and required QA evidence. The promotion mechanism is shown to publish the approved bytes without rebuilding or substitution. Any changed byte or source invalidates affected evidence and requires a new candidate. |
| **5 — Explicit publication decision** | Phases 1–4 exit; final artifact and evidence are immutable and reviewable. | The accountable release authority reviews the gate record and records **GO** or **NO-GO** for the exact version, source SHA and artifact hash. Confirm no prerelease tag can trigger Stable publication and that publication requires the approved gate. On GO only, publish and verify the distributed package identity against the certified artifact. | A dated decision identifies approver, package version, source SHA, artifact SHA-256, evidence record and residual risks. **NO-GO** leaves publication disabled and records the failed/missing gate. After GO, distribution is verified to match the approved hash and the tracker records the result. |

The phase order is a dependency order. Rework that changes a contract, Runtime,
package metadata or artifact returns the candidate to the earliest affected
phase. In particular, an RC label is not permission to publish, and RC test
results do not transfer to different package bytes.

## 4. Required investigations and certification scope

These items are investigation requirements, not presumed defects or mandated
redesigns. Their owning ADRs and current API classifications remain in force.

| Area | Required question / evidence | Boundary to preserve |
|---|---|---|
| Player Observer and Command | Verify the currently implemented observation/command contracts, ownership, lifecycle outcomes and real consumer needs against IF-ADR-015 and Public-API; determine which individual contracts belong in the Stable inventory. | Session remains the authority for Session Player membership and commands; observation does not become an alternate authority. |
| Provisioning and Join | Verify manager-provisioned and other supported provisioning paths, their Session authority, failure compensation and current attributes. Reconcile only actual scope and evidence. | Preserve IF-ADR-046's bounded Stable Join closure and accepted residuals; adjacent Session/Slot/Actor/Leave/Readiness contracts retain independent maturity. |
| Leave and release | Verify exact occurrence/identity handling, staged release, failure, retry and cleanup against IF-ADR-019/020 and current consumers. | Leave authority and scope stay with the accepted Session lifecycle decisions; Activity exit is not substituted for Session Leave. |
| Actor Selection and lifecycle | Reconcile selection, preparation, Actor occurrence and Activity projection/readiness semantics against IF-ADR-003, related ADRs, attributes and consumers. | Preserve Session and Actor authorities; do not infer Stable scope from a passing Player Join. |
| Camera | Certify only the declared Stable Camera contracts and their package/consumer integrations; resolve any live documentation/attribute/implementation mismatch against IF-ADR-038 and IF-ADR-043. | Accepted assignment and physical-output boundaries remain as written; unrelated or Experimental Camera surfaces are not automatically promoted. |
| Active documentation and API inventory | Compare the exact candidate's attributes, Public-API, accepted ADRs, serialized intent and observed implementation. Record contract-by-contract outcomes. | Accepted ADRs are normative; the tracker records evidence and status, not new architecture. |
| Distribution workflow | Execute the prepared candidate and promotion path in a controlled run; verify prerelease protection, immutable artifact identity, package/dependency resolution and the installed consumer source. | Workflow configuration or presence alone is not proof of secure promotion or reproducible distribution. |

## 5. Blockers, accepted risks and post-release work

### 5.1 Release blockers

- Any unresolved mismatch or failure that affects a contract included as Stable
  in 1.1.0, its declared serialized guarantee, or a required consumer path.
- Missing evidence for a required Stable guarantee, material rollback/retry/
  cleanup path, or required consumer integration; a reported test count without
  candidate identity cannot fill this gate.
- Candidate package import/compile failure, dependency-resolution mismatch,
  unreviewed package contents, or a mismatch between the tested, approved and
  distributed artifact.
- An unexecuted or ineffective approval gate, or any path by which a prerelease
  tag could publish Stable. The prepared workflow is not certified until run.
- Missing explicit GO for the exact artifact, or a post-GO artifact/source
  change without renewed affected certification.

### 5.2 Risks eligible for explicit acceptance

- A bounded, understood residual in a Stable contract only when its responsible
  authority accepts it under IF-GOV-001 and its owning ADR and it does not
  contradict the published guarantee. Preserve IF-ADR-046's accepted scope;
  do not generalize its evidence exception.
- Gaps confined to Experimental/Internal contracts when evidence demonstrates
  that they neither affect a Stable guarantee nor an explicit release gate.
- A documented, bounded consumer or platform exclusion only when it is outside
  the supported Stable claim and does not invalidate package compatibility or
  distribution reproducibility.

Risk acceptance does not convert missing evidence into PASS or silently change
an API's maturity.

### 5.3 Post-release improvements

Features, broader platform matrices, Experimental API improvements and
consumer-owned behavior may be scheduled after publication when they are
outside the Stable guarantee, are not an explicit gate, and do not conceal a
known release blocker. A post-release label cannot be used to defer a required
contract decision or evidence obligation.

## 6. Approval and records

This ADR is **Accepted** as the roadmap and release gate sequence. Its
acceptance does not mark any phase complete, approve the eventual Stable
contract inventory, accept every listed risk, certify an RC, or authorize
publication.

Before freeze, the responsible architecture authority must approve the exact
Stable inventory and any required owning-ADR decisions. The contract owner must
approve any individual Stable-risk acceptance. Before publication, the
accountable release authority must make a separate, explicit GO/NO-GO decision
for the immutable artifact under Phase 5. The tracker records phase evidence
and status; this ADR remains the decision source.

## 7. Consequences

- Maturity and contract boundaries are closed before source freeze and package
  distribution; the release label does not determine API maturity.
- Current reported test results remain useful scoped evidence, but do not
  certify a release candidate without immutable provenance and consumer
  resolution records.
- The exact approved bytes are installed and tested before they are eligible
  for publication. Rebuilding during promotion is not equivalent to promoting
  the certified artifact.
- `1.1.0` remains NO-GO until the phase exits and explicit decision are
  recorded. No version, tag, release or registry publication follows merely
  from accepting this ADR.
