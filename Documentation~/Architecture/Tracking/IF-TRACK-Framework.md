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
| [011](../ADRs/IF-ADR-011-Participant-Aware-Activity-Readiness-Loading-Progress.md) | ACCEPTED | Partial current tracking | Certified for current boundary | Partial | Focused public waiting/joining evidence remains |
| [012](../ADRs/IF-ADR-012-Activity-Player-Participation-Profile-and-Readiness-Compatibility.md) | ACCEPTED / RECONCILED | IMPLEMENTED | CERTIFIED | Not proven on current model | Stage A closed; consumer proof is separate |
| [013](../ADRs/IF-ADR-013-Optional-Audio-BGM-Adapter.md) | ACCEPTED / Experimental | IMPLEMENTED for accepted technical boundary | CERTIFIED: Audio QA 26/26; ADR-013A 11/11 | Not proven | Stage A closed; FIRSTGAME real-consumer proof is the promotion gate |
| [014](../ADRs/IF-ADR-014-Authored-Definition-and-Stable-Identity-Authority.md) | ACCEPTED | IMPLEMENTED | CERTIFIED | Proven | Closed for current boundary |
| [015](../ADRs/IF-ADR-015-Player-Provisioning-Commands-and-Consumer-Observation-Surface.md) | ACCEPTED | IMPLEMENTED for current boundary | CERTIFIED | Not proven on current model | Stage B consumer integration remains |
| [016](../ADRs/IF-ADR-016-Player-Session-Initial-Configuration-and-Provisioning-Profiles.md) | ACCEPTED | IMPLEMENTED | CERTIFIED | Not proven on current model | Stage B Scene-/Manager-Provisioned integration remains |

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
| 011 | 95% | 5% | 95% | Medium | Close focused public waiting/joining evidence. |
| 012 | 100% | 0% | 85% | Stage B | Prove the accepted participation model in a real consumer. |
| 013 | 100% | 0% | 85% | Stage B | Prove optional BGM integration and usability in FIRSTGAME. |
| 014 | 100% | 0% | 100% | None | Closed for the current boundary. |
| 015 | 100% | 0% | 85% | Stage B | Integrate the current provisioning commands in a real consumer. |
| 016 | 100% | 0% | 88% | Stage B | Prove Scene-/Manager-Provisioned integration in a real consumer. |

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
   divergence was reproduced. **ADR-011** retains its separate **5%** focused
   technical evidence. **ADR-010** retains **3%** of feature-owned adoption
   evidence and has no generic UX QA gate.
4. **ADR-003, ADR-004, ADR-005, ADR-006, ADR-007, ADR-012, ADR-013, ADR-015 and ADR-016**
   may still have Stage B portfolio work; that is not a Stage A technical
   regression.

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
- Continue ADR-011 only against its own focused tracked gap.
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
- ADR-011: only the focused gap recorded in its current tracking row; do not infer
  broader missing architecture.
- ADR-008 has no active QA gap by default. Add QA only when a concrete,
  deterministic Scene Template pipeline invariant or regression requires proof.

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
- ADR-005 Stage B can focus on consumer authoring/usability rather than technical
  Pause correctness.
- Reauthor current Player consumer integration against the accepted current model
  for ADR-003, ADR-012, ADR-015 and ADR-016.
- Treat Camera consumer proof as separate from the certified single-output
  technical boundary.

## Future contracts

The following are future contracts, not gaps in current ADR closure:

- Session-Persistent Player;
- Player Leave, disconnect and reconnect;
- heterogeneous per-Slot Host Provisioning;
- split-screen and multiple Camera outputs;
- exceptional post-commit compensation beyond the current accepted boundary;
- application-scoped stable-ID resolver.

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
