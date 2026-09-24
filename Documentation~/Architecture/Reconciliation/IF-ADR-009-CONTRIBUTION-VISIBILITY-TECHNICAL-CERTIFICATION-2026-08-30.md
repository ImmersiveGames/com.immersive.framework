# IF-ADR-009 — Contribution / Visibility Technical Certification — 2026-08-30

Status: **TECHNICAL QA CERTIFIED**

Normative authority: [IF-ADR-009 — Activity Local Visibility Rules](../ADRs/IF-ADR-009-Activity-Local-Visibility-Rules.md)

Historical pre-split evidence: [ADR-009 QA certification — 2026-08-10](../Archive/IMMERSIVE-FRAMEWORK-ADR-009-QA-CERTIFICATION-2026-08-10.md)

## Purpose

This record certifies the current post-split ADR-009 contract in which Activity-owned
content authority and Activity-conditioned presentation visibility are independent.
It does not relabel the historical 2026-08-10 `46`-case run, which certified the
previous combined boundary.

## Certified contract

```text
ActivityContentContribution
  = Activity ownership
  = Local Content Id
  = Required / Optional authority
  = Activity content Enter / Exit lifecycle
  = invalid Required contribution may reject before commit

ActivityVisibilityRule
  = presentation-only visibility
  = Activity list + match mode + no-active policy
  = no Activity ownership
  = no Required / Optional authority
  = no Activity content lifecycle ownership
  = invalid rule is diagnostic, ignored, non-mutating and non-blocking
```

A Rule may keep a GameObject visible for an Activity that does not own the sibling
Contribution. Visibility membership never broadens Contribution ownership.

## Framework evidence corrections preceding certification

Two runtime evidence-propagation defects were corrected without changing the accepted
ownership split:

- `b7d3af643526b49199dff3db2a1775b92acc3a0b` — preserve the actual failed
  `ActivityFlowStartResult` when the outer request is classified as
  `FailedInvalidConfig`;
- `f70f814e685594375a1d53edf9348b0bfbae53b6` — preserve inspected invalid
  Contribution evidence and Required / Optional counters on pre-commit failure.

These fixes preserve typed failure evidence. They do not make Visibility a transition
or ownership authority.

## QA proof corrections

The focused QA was reconciled with the current split before final execution:

- Required-invalid wrapper classification was corrected while preserving the nested
  `FailedBeforeCommit` proof;
- Visibility isolation stopped using global `ActivityContentCount == 0` as ownership
  evidence and instead checked structured Contribution-entry identity;
- the 16-case lifecycle regression was reconciled because Cases `5–12` still treated
  `ActivityVisibilityRule.activities` as implicit Contribution ownership.

The final lifecycle model is:

```text
Contribution membership
  -> decides Activity content Enter / Exit

Visibility membership
  -> decides GameObject presentation state
```

The current runtime operation order proven by the lifecycle regression is:

```text
validate / collect
-> Exit previous Contribution
-> lifecycle participants
-> apply target Visibility Rules
-> Enter target Contribution
```

Therefore lifecycle callbacks can legitimately observe a presentation state already
changed by the target Visibility Rule, and a visibility change can legitimately occur
with zero Contribution callbacks.

## Unity QA results

### Contribution Authority

Runner:

`QaActivityContentContributionTransitionAuthorityRegression`

Result:

```text
status = Passed
verdict = ADR009-ContributionAuthority
cases = 3/3
```

Certified cases:

1. Required invalid Contribution -> typed diagnostic + reject before commit + previous
   Activity remains canonical.
2. Optional invalid Contribution -> typed diagnostic + non-blocking transition.
3. Valid Contribution -> Activity ownership/lifecycle without relying on visibility.

### Visibility Isolation

Runner:

`QaActivityVisibilityRuleTransitionIsolationRegression`

Result:

```text
status = Passed
verdict = ADR009-VisibilityIsolation
cases = 2/2
```

Certified cases:

1. Invalid Rule -> diagnostic + non-mutating + non-blocking + no Contribution authority.
2. Valid Rule -> presentation control without Activity content ownership, Requiredness
   or lifecycle authority.

### Activity Local Visibility Lifecycle

Runner:

`QaActivityLocalVisibilityLifecycleRegression`

Result:

```text
status = Passed
cases = 16
completed = positive-single,positive-multiple,negative-single,negative-multiple,no-active-visible,clear,idempotence
```

The reconciled 16 cases explicitly prove independent Contribution lifecycle and
Visibility presentation semantics, including transitions where presentation remains
visible while Contribution ownership exits, and transitions where presentation changes
with no Contribution callback.

## Current post-split certification matrix

```text
Contribution Authority     3/3  PASS
Visibility Isolation       2/2  PASS
Lifecycle regression      16/16 PASS
------------------------------------
Current post-split evidence 21/21 PASS
```

`21/21` is the sum of three focused runners, not a separate aggregate runner.

## Historical boundary preservation

The historical 2026-08-10 ADR-009 `46`-case record remains valid only for the earlier
combined local-visibility boundary. It must not be described as testing the later
Contribution / Visibility split.

Current post-split certification authority is this record.

## Scope limits

This certification is technical QA evidence for ADR-009. It does not by itself claim:

- FIRSTGAME consumer/product certification;
- new readiness or transition authority;
- ownership inferred from scene location;
- a combined Contribution/Visibility component;
- any change to IF-ADR-001, IF-ADR-006, IF-ADR-007 or IF-ADR-010 authority.

The focused runners require Framework Game Flow to be ready before execution. A runner
started before bootstrap completion may report `Game Flow is not ready.`; that is a QA
harness timing precondition, not ADR-009 semantic failure.

## Verdict

```text
IF-ADR-009 POST-SPLIT TECHNICAL QA CERTIFIED
Contribution  3/3 PASS
Visibility    2/2 PASS
Lifecycle    16/16 PASS
```
