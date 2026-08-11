# IF-TRACK - Immersive Framework

Status: Active
Last updated: 2026-08-10

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
| [006](../ADRs/IF-ADR-006-Loading-Transition-Persistence-and-Diagnostics.md) | ACCEPTED | Partial current tracking | Certified for current boundary | Partial | Exceptional paths remain tracked separately |
| [007](../ADRs/IF-ADR-007-Activity-Entry-Readiness-and-Reveal-Gating.md) | ACCEPTED | Partial current tracking | Certified for current boundary | Partial | Focused readiness variants remain |
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
| 006 | 95% | 5% | 95% | Medium | Close only the tracked exceptional-path evidence. |
| 007 | 95% | 5% | 95% | Medium | Close only the tracked readiness variants. |
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

1. **ADR-005 Stage A is closed.** Focused evidence is Input Gate **9/9**,
   Activity Restart **8/8** and Pause Contract **27/27** across two passes in
   one Play Mode session, including terminal no-residual state.
2. **ADR-006, ADR-007 and ADR-011** each retain **5%** of focused technical
   evidence.
3. **ADR-010** retains **3%** of feature-owned adoption evidence and has no
   generic UX QA gate.
4. **ADR-003, ADR-004, ADR-005, ADR-012, ADR-013, ADR-015 and ADR-016** may
   still have Stage B portfolio work; that is not a Stage A technical regression.

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

- ADR-005 requires no further Stage A work for the current accepted boundary.
  Reopen only on a reproduced regression, a documented contract change or a new
  accepted scope.
- Keep ADR-008 out of the active technical queue while its accepted Scene
  Template + consumer-owned Scene + non-mutating verification boundary remains
  valid.
- Continue ADR-006, ADR-007 and ADR-011 only against their focused tracked gaps.
- Keep technical documentation aligned with current reconciliation records and
  preserve the Stage A / Stage B distinction.

## Focused QA gaps already identified

- ADR-005: **none remaining in the current accepted Stage A boundary**.
- Camera: a QA-only teardown clean-log retest remains nonblocking and does not
  reopen ADR-004 technical certification.
- ADR-006, ADR-007 and ADR-011: only the focused gaps recorded in their current
  tracking rows; do not infer broader missing architecture.
- ADR-008 has no active QA gap by default. Add QA only when a concrete,
  deterministic Scene Template pipeline invariant or regression requires proof.

## Documentation maintenance

- Keep ADRs normative and concise.
- Put current technical reconciliation and certification records in
  `Architecture/Reconciliation/`.
- Keep historical audits, rebaselines, completion reports and plans in
  `Architecture/Archive/`.

## Stage B / FIRSTGAME integration

- ADR-005 Stage B can now focus on consumer authoring/usability rather than
  technical Pause correctness.
- Reauthor current Player consumer integration against the accepted current
  model for ADR-003, ADR-012, ADR-015 and ADR-016.
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
- [ADR-008](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-008-RECONCILIATION-2026-08-10.md)
