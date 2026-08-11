# IF-TRACK - Immersive Framework

Status: Active
Last updated: 2026-08-11

## Authority and status model

This is the single mutable summary of current delivery state. Its authority is
below accepted ADRs and current reconciliation records, and above historical
audits, completion summaries and plans.

| Dimension | Status vocabulary |
|---|---|
| Architecture | Proposed, Accepted, Reconciled, Superseded |
| Package | IMPLEMENTED, DIVERGENT, ABSENT, DEFERRED |
| Product Surface | IMPLEMENTED, Partial, Not applicable |
| Technical QA | CERTIFIED, QA GAP, Partial, Not applicable |
| FIRSTGAME / Stage B | Proven, Partial, Not proven, Not applicable |

`IMPLEMENTED`, `DIVERGENT`, `ABSENT`, `QA GAP`, `DOC/TRACKING GAP` and
`DEFERRED` retain the meanings defined in current reconciliation records.
Percentages are planning estimates, not certification scores.

## Reconciliation sequence

```text
Stage A - technical reconciliation
  ADR -> package -> technical QA -> reconciliation

Stage B - real consumer proof
  FIRSTGAME -> real integration -> usability/product proof
```

Stage B evidence does not reopen a Stage A technical boundary already closed by
a later reconciliation. It can identify a separate consumer or product issue.

## Current ADR status

| ADR | Architecture | Package / product surface | Technical QA | FIRSTGAME / Stage B | Current disposition |
|---|---|---|---|---|---|
| [001](../ADRs/IF-ADR-001-Core-Lifecycle-and-Runtime-Authority.md) | ACCEPTED / RECONCILED | IMPLEMENTED for current boundary | CERTIFIED | Proven for core lifecycle flows | Stage A closed |
| [002](../ADRs/IF-ADR-002-Product-Authoring-Model.md) | ACCEPTED / RECONCILED | IMPLEMENTED | Not applicable as a generic cross-cutting gate | Not applicable as a generic gate | Stage A closed |
| [003](../ADRs/IF-ADR-003-Player-Participation-and-Actor-Lifecycle.md) | ACCEPTED / RECONCILED | IMPLEMENTED | CERTIFIED | Not proven on current model | Stage A closed; consumer proof is separate |
| [004](../ADRs/IF-ADR-004-Camera-Requests-and-Output-Authority.md) | ACCEPTED / RECONCILED | IMPLEMENTED for current single-output boundary | CERTIFIED | Partial | Stage A closed; multi-output is future work |
| [005](../ADRs/IF-ADR-005-Input-Pause-Gate-and-Reset.md) | ACCEPTED / RECONCILED | IMPLEMENTED; focused Pause baseline defect corrected | CERTIFIED: Input Gate 9/9; Activity Restart 8/8; Pause 27/27 | Stage B separate | Stage A closed |
| [006](../ADRs/IF-ADR-006-Loading-Transition-Persistence-and-Diagnostics.md) | ACCEPTED / RECONCILED | IMPLEMENTED for current accepted boundary | CERTIFIED: focused ADR006 matrix 8/8; Progress 32/32; Terminal 34/34 | Partial | Stage A closed; remaining work is Stage B consumer proof |
| [007](../ADRs/IF-ADR-007-Activity-Entry-Readiness-and-Reveal-Gating.md) | ACCEPTED / RECONCILED | IMPLEMENTED; Activity Inspector aligned with runtime behavior | CERTIFIED: Foundation 18/18; Direct Policies 42/42; shared Progress 32/32; Terminal 34/34 | Partial | Stage A closed; remaining work is Stage B consumer proof |
| [008](../ADRs/IF-ADR-008-Persistent-Application-Content-Composition.md) | ACCEPTED / RECONCILED | IMPLEMENTED for current accepted product model | Not applicable by default | Not applicable as a technical closure gate | Stage A closed; reopen only on concrete contract failure |
| [009](../ADRs/IF-ADR-009-Activity-Local-Visibility-Rules.md) | ACCEPTED / RECONCILED | IMPLEMENTED | CERTIFIED | Not required for current boundary | Stage A closed |
| [010](../ADRs/IF-ADR-010-Editor-and-Inspector-Product-Surface-Authority.md) | ACCEPTED | IMPLEMENTED | Not applicable as generic UX QA | Per feature | Current audit is historical; adoption remains feature-owned |
| [011](../ADRs/IF-ADR-011-Participant-Aware-Activity-Readiness-Loading-Progress.md) | ACCEPTED / RECONCILED | IMPLEMENTED for participant-aware WaitCovered Loading progress | CERTIFIED: Progress 32/32; Terminal 34/34; Startup Route 25/25; Game Application 20/20 | Partial | Stage A closed; remaining work is Stage B consumer/product proof |
| [012](../ADRs/IF-ADR-012-Activity-Player-Participation-Profile-and-Readiness-Compatibility.md) | ACCEPTED / RECONCILED | IMPLEMENTED | CERTIFIED | Not proven on current model | Stage A closed; consumer proof is separate |
| [013](../ADRs/IF-ADR-013-Optional-Audio-BGM-Adapter.md) | ACCEPTED / Experimental | IMPLEMENTED for accepted technical boundary | CERTIFIED: Audio QA 26/26; ADR-013A 11/11 | Not proven | Stage A closed; FIRSTGAME real-consumer proof is the promotion gate |
| [014](../ADRs/IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md) | ACCEPTED | IMPLEMENTED | CERTIFIED | Proven | Closed for current boundary |
| [015](../ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md) | ACCEPTED | IMPLEMENTED for current boundary | CERTIFIED | Not proven on current model | Stage B consumer integration remains |
| [016](../ADRs/IF-ADR-016-Player-Session-Initial-Configuration-and-Provisioning-Profiles.md) | ACCEPTED | IMPLEMENTED | CERTIFIED | Not proven on current model | Stage B Scene-/Manager-Provisioned integration remains |
| [017](../ADRs/IF-ADR-017-Application-Frame-Rate-Project-Authority.md) | ACCEPTED / RECONCILED | IMPLEMENTED: Project Settings baseline, boot validation and explicit runtime application | CERTIFIED: Edit 13/13; Target 13/13; VSync 13/13; Defaults 13/13 | Not applicable for current project-baseline boundary | Stage A closed; Session override and Preferences integration remain future scope |
| [018](../ADRs/IF-ADR-018-Progression-Save-Backend-Independence-and-Persistence-Domain-Boundaries.md) | ACCEPTED / RECONCILED | A core STABLE; B JSON CERTIFIED; C product composition IMPLEMENTED | CERTIFIED: A backend conformance; B JSON recovery 18/18; C composition 12/12 | Not proven — ADR018-D next | Stage A closed; Built-in JSON vs Custom Provider and no-fallback composition certified |

## Planning estimates and attention order

The percentages below are planning estimates derived from current ADR and
reconciliation status. `Stage A` measures the accepted technical boundary.
`Portfolio` also discounts unproven Stage B consumer evidence where applicable.

| ADR | Stage A estimate | Technical remaining | Portfolio estimate | Attention now | Concrete next work |
|---|---:|---:|---:|---|---|
| 001 | 100% | 0% | 100% | None | Closed for the current boundary. |
| 002 | 100% | 0% | 100% | None | Closed for the current boundary. |
| 003 | 100% | 0% | 87% | Stage B | Prove the accepted Player model in a real consumer. |
| 004 | 100% | 0% | 91% | Stage B | Complete real-consumer Camera proof; multi-output is future scope. |
| 005 | 100% | 0% | 94% | Stage B | Stage A closed; remaining portfolio attention is real-consumer evidence only. |
| 006 | 100% | 0% | 95% | Stage B | Stage A closed; prove real Loading/Transition authoring, cover/wait/reveal behavior and diagnostics in FIRSTGAME. |
| 007 | 100% | 0% | 95% | Stage B | Stage A closed; prove real readiness-policy authoring, preparing/covered/visible behavior and diagnostics in FIRSTGAME. |
| 008 | 100% | 0% | 100% | None | No active package work; reopen only on concrete contract failure. |
| 009 | 100% | 0% | 100% | None | Closed for the current boundary. |
| 010 | 97% | 3% | 97% | Low | Complete feature-owned adoption evidence; no generic UX QA gate exists. |
| 011 | 100% | 0% | 95% | Stage B | Stage A closed; prove real participant-aware Loading progress and diagnostics in FIRSTGAME when useful. |
| 012 | 100% | 0% | 85% | Stage B | Prove the accepted participation model in a real consumer. |
| 013 | 100% | 0% | 85% | Stage B | Prove optional BGM integration and usability in FIRSTGAME. |
| 014 | 100% | 0% | 100% | None | Closed for the current boundary. |
| 015 | 100% | 0% | 85% | Stage B | Integrate the current provisioning commands in a real consumer. |
| 016 | 100% | 0% | 88% | Stage B | Prove Scene-/Manager-Provisioned integration in a real consumer. |
| 017 | 100% | 0% | 100% | None | Stage A closed for project-level Frame Rate authority; Session override and Preferences integration are future scope. |
| 018 | 100% | 0% | 75% | ADR018-D FIRSTGAME | Stage A closed. Prove real Built-in JSON persistence, backend replacement and game-facing scoped-runtime usability in FIRSTGAME. |

### Attention summary

1. **ADR-006 Stage A is closed.** The focused matrix is **8/8 PASS**, including
   Participant-Aware Loading Progress **32/32** and Loading Terminal **34/34**.
   No package divergence was reproduced by the closure matrix; remaining ADR-006
   work is Stage B consumer/product evidence only.
2. **ADR-005 Stage A is closed.** Focused evidence is Input Gate **9/9**,
   Activity Restart **8/8** and Pause Contract **27/27** across two passes in
   one Play Mode session, including terminal no-residual state.
3. **ADR-007 Stage A is closed.** The stale Activity Inspector warning was
   corrected; Foundation is **18/18** across two passes and Direct Policies is
   **42/42** with `WaitVisible` and `WaitCovered` both PASS. The initial Direct
   Policies blocker was a QA fixture collision, corrected by isolating the Player
   Surface content scene without weakening assertions. No package runtime
   divergence was reproduced.
4. **ADR-011 Stage A is closed.** Direct Progress is **32/32**, Terminal is
   **34/34**, Route Startup parity is **25/25** and Game Application Startup parity
   is **20/20**. Both startup parity sessions restored the canonical QA Hub and
   removed generated fixtures. The older public waiting/joining wording belongs to
   historical Player audit scope, not the ADR-011 Loading-progress contract.
5. **ADR-017 Stage A is closed.** Project Settings is the sole authored Frame Rate
   authority; Edit validation passed **13/13**, TargetFrameRate **13/13**,
   VerticalSync **13/13** and UseUnityDefaults **13/13**. The E2E runs proved the
   preboot sentinel `47/2` was observed by the official runtime and then either
   transformed by the selected policy or preserved by `UseUnityDefaults`.
6. **ADR-018 Stage A is closed.** Backend conformance is certified, the built-in
   JSON minimum backend passed recovery QA **18/18**, and Product Composition passed
   **12/12** including **7/7** negative cases, no-fallback, selection isolation and
   real runtime requests through the selected custom backend. Authoring/composition
   APIs remain Experimental pending ADR018-D FIRSTGAME usability proof; this is a
   Stage B promotion gate, not a Stage A technical gap.
7. **ADR-010** retains **3%** of feature-owned adoption evidence and has no generic
   UX QA gate. ADR-003, ADR-004, ADR-005, ADR-006, ADR-007, ADR-011, ADR-012,
   ADR-013, ADR-015 and ADR-016 may still have Stage B portfolio work; that is not a
   Stage A technical regression.

## ADR-006 closure evidence

The focused Stage A closure uses existing canonical regressions plus the two
ADR006-specific negative runners; no monolithic replacement smoke was introduced.

```text
ADR006 Presentation Policy
  PASS — 5 cases
  QA-06 required presentation failure
  QA-07 explicit optional NoOp

ADR006 Transaction Behavioral Closure
  PASS — 15 cases
  passes='2/2'
  QA-01 pre-commit failure authority preservation
  QA-02 committed-target post-commit failure preservation
  QA-08 repeated terminal cleanup
  pass-1-terminal-clean
  pass-2-terminal-clean
  isolation-scene-cleaned
  official-authority-preserved

Identity Authority Regression
  PASS — 6/6
  executed twice
  QA-03 legitimate supersession preservation
  waitStatus='Superseded'
  executionStatus='Superseded'
  routeKind='SupersededCommittedTargetByRouteReplacement'

Participant-Aware Readiness Loading Progress
  PASS — 32/32
  QA-04 readiness-governed loading progress
  ordering='Technical<100,0/4,1/4,2/4,3/4,4/4=100,Hide,Reveal,GateRelease'

Participant-Aware Readiness Loading Terminal
  PASS — 34/34
  QA-05 pure Transition Gate / recovery separation
  required failure retains Loading/Transition presentation and recovery protection
  pure Transition Gate is released
  cleanup restores presentation, releases recovery gate and restores authority
```

The error-level required-readiness failure emitted by the Terminal regression is
deliberate negative-case evidence; the regression terminal report is
`status='Passed' cases='34'`.

No package divergence was reproduced by the complete focused matrix.

```text
ADR006 Stage A
  Architecture: ACCEPTED / RECONCILED
  Package: IMPLEMENTED
  Technical QA: CERTIFIED
  matrix: 8/8 PASS
  technical remaining: 0%
  Stage A: CLOSED
```

## ADR-007 closure evidence

The accepted readiness boundary is now technically certified without adding a
new monolithic smoke.

```text
Activity Entry Readiness Foundation
  PASS — 18/18
  passes='2/2'
  ObserveOnly compatibility
  occurrence identity / wrong-occurrence isolation
  manual required failure terminal
  cancellation terminal

Direct Activity Readiness Policies
  PASS — 42/42
  WaitVisible='Passed'
  WaitCovered='Passed'
  HostOwned presentation
  request/gate ordering confirmed
  initial authority restored
  presentation/evidence cleanup confirmed

Shared Participant-Aware Loading evidence
  Progress PASS — 32/32
  Terminal PASS — 34/34
```

The first Direct Policies closure attempt was blocked before behavior execution
because the Player Surface QA setup had polluted the neutral IF_READY_04 content
scene. QA fixture ownership was corrected by giving Player Surface a dedicated
content scene; the unchanged Direct Policies regression then passed 42/42. This
was a QAFramework defect, not package runtime divergence.

The package Editor correction removed the stale warning claiming runtime entry
waiting was inactive. No readiness runtime logic changed.

```text
ADR007 Stage A
  Architecture: ACCEPTED / RECONCILED
  Package / Product Surface: IMPLEMENTED
  Technical QA: CERTIFIED
  technical remaining: 0%
  Stage A: CLOSED
```

## ADR-011 closure evidence

The accepted participant-aware Loading-progress boundary is technically certified
across direct positive progression, terminal non-success semantics and both startup
paths.

```text
Participant-Aware Readiness Loading Progress
  PASS — 32/32
  Required=4
  Optional=1
  Optional failure non-blocking
  Technical<100 -> 0/4 -> 1/4 -> 2/4 -> 3/4 -> 4/4=100 -> Hide -> Reveal -> GateRelease

Participant-Aware Readiness Loading Terminal
  PASS — 34/34
  RequiredFailed
  RequiredReleased
  ReplacementRejected
  LateOldOccurrenceRejected
  DuplicateTerminal
  OwnedCancellation

Startup Loading Parity — Route Startup Activity
  PASS — 25/25
  required='4'
  optional='1'
  optionalOutcome='FailedNonBlocking'
  terminal='100BeforeHide'

Startup Loading Parity — Game Application Startup Activity
  PASS — 20/20
  required='4'
  optional='1'
  optionalOutcome='FailedNonBlocking'
  terminal='100BeforeHide'
```

Both startup-parity sessions restored the canonical QA Hub automatically and removed
the generated fixture after Play Mode. No package divergence was reproduced and no
runtime, Editor or QA implementation change was required for closure.

```text
ADR011 Stage A
  Architecture: ACCEPTED / RECONCILED
  Package / Product Surface: IMPLEMENTED
  Technical QA: CERTIFIED
  technical remaining: 0%
  Stage A: CLOSED
```

## ADR-017 closure evidence

The accepted project-level Frame Rate boundary is technically certified across
authoring ownership, boot validation, runtime application and explicit no-override
behavior.

```text
Edit Validation
  PASS — 13/13
  invalidProjectPolicy='RejectedBeforeMutation'
  invalidApplier='RejectedWithoutPartialMutation'
  projectSettingsAuthority='Present'
  gameApplicationSerializedAuthority='Absent'
  gameApplicationApiAuthority='Absent'
  restored='True'

TargetFrameRate
  PASS — 13/13
  source='ProjectSettings'
  previous='47 / 2'
  applied='73 / 0'
  runtimeStatus='Applied'
  gameApplicationFrameRateAuthority='Absent'

VerticalSync
  PASS — 13/13
  source='ProjectSettings'
  previous='47 / 2'
  applied='-1 / 3'
  runtimeStatus='Applied'
  platform='WindowsEditor'
  gameApplicationFrameRateAuthority='Absent'

UseUnityDefaults
  PASS — 13/13
  source='ProjectSettings'
  previous='47 / 2'
  applied='47 / 2'
  runtimeStatus='SkippedUnityDefaults'
  gameApplicationFrameRateAuthority='Absent'
```

The focused harness seeds `Application.targetFrameRate=47` and
`QualitySettings.vSyncCount=2` before the framework bootstrap. The official
`FrameworkRuntimeHost` reported those values as its previous state in all three
positive E2E runs.

Each prepared Play Mode run restored the original Project Settings policy and Editor
frame pacing values after exit. The earlier manual restore was an explicit QA
recovery action before the successful VerticalSync run and is not a package failure.

No package divergence was reproduced.

```text
ADR017 Stage A
  Architecture: ACCEPTED / RECONCILED
  Package / Product Surface: IMPLEMENTED
  Technical QA: CERTIFIED
  technical remaining: 0%
  Stage A: CLOSED
```

## ADR-005 closure evidence

The focused Pause cut followed the intended QA boundary:

```text
canonical composition
  -> QA reproduced a real package defect
  -> package owner corrected exact pre-Pause PlayerInput posture restoration
  -> same QA passed without weaker assertions
```

Final evidence:

```text
Input Gate        PASS — 9/9
Activity Restart  PASS — 8/8
Pause Contract    PASS — 27/27
                  run-1 complete
                  run-2 complete
                  terminal-no-residual-pause-or-gate
```

The corrected package behavior preserves both previously enabled and previously
disabled Gameplay Action Map baselines across Pause -> Resume.

## Current technical reconciliation work

- ADR-006 requires no further Stage A work for the current accepted boundary.
  Reopen only on a reproduced regression, a documented contract change or a new
  accepted scope. Remaining work is Stage B consumer/product evidence.
- ADR-005 requires no further Stage A work for the current accepted boundary.
  Reopen only on a reproduced regression, a documented contract change or a new
  accepted scope.
- Keep ADR-008 out of the active technical queue while its accepted Scene
  Template + consumer-owned Scene + non-mutating verification boundary remains
  valid.
- ADR-007 requires no further Stage A work for the current accepted boundary.
  The stale `ActivityAssetEditor` warning is corrected; Foundation passed 18/18
  across two passes and Direct Policies passed 42/42 after isolating a QA-owned
  fixture collision. Reopen only on a reproduced contract regression, documented
  contract change or newly accepted scope. Do not invent timeout/retry runtime.
- ADR-011 requires no further Stage A work for the current accepted boundary. Direct Progress passed 32/32, Terminal passed 34/34, Route Startup parity passed 25/25 and Game Application Startup parity passed 20/20. Reopen only on a reproduced contract regression, documented contract change or newly accepted scope; do not add Player-specific runtime scope to this ADR.
- ADR-018-A/B/C are closed/certified for the accepted Stage A boundary.
  Product Composition passed 12/12 with 7/7 negative cases, no-fallback,
  selection isolation and selected-backend runtime request proof. The active gate is
  ADR018-D FIRSTGAME real-consumer usability; do not add a global runtime accessor
  before that proof.
- Reverse-audit RA-03 Object Entry ownership is reconciled. Object Entry is a
  passive stable-identity/scoped-metadata layer under ADR-014 whose runtime context
  is derived from ADR-001 lifecycle authority. It is not a lifecycle owner, Reset
  authority, service registry or physical binding authority. No runtime change or
  focused QA is required for this reconciliation. Experimental Object Entry
  request/result API hygiene is deferred to RA-04.
- ADR-017 requires no further Stage A work for the accepted project-baseline
  boundary. Edit validation passed 13/13 and the TargetFrameRate, VerticalSync and
  UseUnityDefaults E2E paths each passed 13/13. Session-scoped override and
  Preferences persistence are future contracts and do not reopen Stage A.
- Keep technical documentation aligned with current reconciliation records and
  preserve the Stage A / Stage B distinction.

## Focused QA gaps already identified

- ADR-005: **none remaining in the current accepted Stage A boundary**.
- Camera: a QA-only teardown clean-log retest remains nonblocking and does not
  reopen ADR-004 technical certification.
- ADR-006: **none remaining in the current accepted Stage A boundary**. The
  focused matrix is 8/8 PASS and no package divergence was reproduced. Reopen
  only on a concrete accepted-contract regression or documented scope change.
- ADR-007: **none remaining in the current accepted Stage A boundary**.
  Foundation is 18/18 across two passes; Direct Activity Readiness Policies is
  42/42 with WaitVisible/WaitCovered PASS. Shared Progress 32/32 and Terminal
  34/34 evidence remains valid. The earlier Direct Policies blocker was a
  QAFramework fixture-ownership defect and was corrected without weakening the
  regression.
- ADR-011: **none remaining in the current accepted Stage A boundary**. Direct Progress is 32/32, Terminal is 34/34, Route Startup parity is 25/25 and Game Application Startup parity is 20/20. Public waiting/joining remains supporting Player-domain evidence, not an ADR-011 completion gate.
- ADR-018-A backend conformance, ADR018-B JSON recovery and ADR018-C Product
  Composition are closed/certified. C passed 12/12 including 7/7 negative cases,
  no-fallback, selection isolation and runtime request proof. No technical QA gap
  remains for the accepted Stage A Progression Save boundary.
- RA-03 Object Entry ownership reconciliation has **no focused QA gap** because
  it changes no runtime behavior or technical contract. Existing Object Entry
  runtime context remains subordinate to lifecycle authority; any future code-surface
  reduction requires a separate reference/API hygiene audit.
- ADR-017: **none remaining in the current accepted Stage A boundary**.
  Edit 13/13, Target 13/13, VSync 13/13 and Defaults 13/13 are PASS; Project
  Settings authority and absence of GameApplication Frame Rate authority are proven.
- ADR-008 has no active QA gap by default. Add QA only when a concrete,
  deterministic Scene Template pipeline invariant or regression requires proof.

## Package reverse-audit loose ends

```text
RA-CUT-01  Application Frame Rate / ADR017        CLOSED / CERTIFIED
RA-CUT-02  Persistence / ADR018                   CLOSED FOR STAGE A
RA-CUT-03  ObjectEntry Ownership Reconciliation  CLOSED / DOC RECONCILIATION
RA-CUT-04  Architecture Governance Hygiene       NEXT
```

RA-03 disposition:

```text
ObjectEntryId / declaration metadata
  -> ADR-014 stable identity / passive metadata

ObjectEntryRuntimeContextSnapshot
  -> derivative projection of ADR-001 lifecycle authority

Reset consumption
  -> ADR-005 downstream consumer only

new ObjectEntry lifecycle authority
  -> NONE

new ADR
  -> NOT REQUIRED
```

`ObjectEntryRequest` / `ObjectEntryResult` remain Experimental and are not promoted
or removed by RA-03. Their necessity belongs to RA-04 API/governance hygiene after a
reliable code-reference audit.

## Documentation maintenance

- Keep ADRs normative and concise.
- Put current technical reconciliation and certification records in
  `Architecture/Reconciliation/`.
- Keep historical audits, rebaselines, completion reports and plans in
  `Architecture/Archive/`.
- Keep mutable QA execution counts in reconciliation/tracking rather than in the
  normative ADR unless the ADR contract itself changes.

## Stage B / FIRSTGAME integration

- ADR-006 FIRSTGAME work is now Stage B consumer proof only: validate real
  Loading/Transition authoring, understandable cover/wait/reveal behavior and
  useful diagnostics without reconstructing internal contracts.
- ADR-011 FIRSTGAME work is Stage B consumer/product proof only: validate that real
  participant-aware Loading progress is understandable and diagnostic when a game
  uses `WaitCovered + FadeWithLoading`, without exposing internal denominator or
  occurrence mechanics as primary authoring UX.
- ADR-005 Stage B can focus on consumer authoring/usability rather than technical
  Pause correctness.
- Reauthor current Player consumer integration against the accepted current model
  for ADR-003, ADR-012, ADR-015 and ADR-016.
- Treat Camera consumer proof as separate from the certified single-output
  technical boundary.
- ADR-018 is ready for FIRSTGAME Stage B. Prove real Built-in JSON persistence,
  close/reopen/load behavior, backend replacement without changing game-facing
  Progression Save request semantics, and the usability of explicit scoped runtime
  delivery before freezing a game-facing access/binding API.
- ADR-017 has no required FIRSTGAME gate for the current project-baseline
  boundary. A future Session override/player-facing preference surface should be
  proven in FIRSTGAME when that future scope is accepted.


## Future contracts

The following are future contracts, not gaps in current ADR closure:

- Session-Persistent Player;
- Player Leave, disconnect and reconnect;
- heterogeneous per-Slot Host Provisioning;
- split-screen and multiple Camera outputs;
- exceptional post-commit compensation beyond the current accepted boundary;
- application-scoped stable-ID resolver;
- Session-scoped Frame Rate override;
- persisted Frame Rate preference integration after the Persistence/Preferences
  architecture is accepted.

## Current reconciliation records

- [ADR-001](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-001-RECONCILIATION-2026-08-10.md)
- [ADR-002](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-002-RECONCILIATION-2026-08-10.md)
- [ADR-002 and ADR-009](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-002-009-RECONCILIATION-2026-08-10.md)
- [ADR-003 and ADR-012](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-003-012-RECONCILIATION-2026-08-10.md)
- [ADR-004 Camera](../Reconciliation/IF-ADR-004A-Camera-Authority-Normative-Reconciliation-2026-08-10.md)
- [ADR-005](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-005-RECONCILIATION-2026-08-10.md)
- [ADR-006](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-006-RECONCILIATION-2026-08-10.md)
- [ADR-007](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-007-RECONCILIATION-2026-08-11.md)
- [ADR-008](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-008-RECONCILIATION-2026-08-10.md)
- [ADR-011](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-011-RECONCILIATION-2026-08-11.md)
- [ADR-017](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-017-RECONCILIATION-2026-08-11.md)
- [ADR-018](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-018-RECONCILIATION-2026-08-11.md)
- [ADR-018-A Certification](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-018-A-CERTIFICATION-2026-08-11.md)
- [ADR-018-C Certification](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-018-C-CERTIFICATION-2026-08-11.md)
- [RA-03 Object Entry Ownership](../Reconciliation/IMMERSIVE-FRAMEWORK-RA-03-OBJECT-ENTRY-OWNERSHIP-RECONCILIATION-2026-08-11.md)
