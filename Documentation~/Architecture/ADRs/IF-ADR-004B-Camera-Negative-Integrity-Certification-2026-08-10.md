# IF-ADR-004B — Camera Negative Integrity Certification

Status: **CLOSED — CERTIFIED 18/18**  
Date: **2026-08-10**  
Type: **Technical QA**  
Primary repository: **QAFramework**  
Normative target: **IF-ADR-004 — Camera Requests and Output Authority**

## Objective

Attempt to break the accepted Camera single-output architecture across:

- deterministic arbitration;
- request identity/output validation;
- publish/release idempotence;
- restoration ordering;
- logical/physical rollback integrity;
- owner lifecycle cleanup;
- persistent output authoring validation.

The cut started by testing the then-current package rather than assuming a
package redesign was required.

## Canonical QA composition

004B reuses existing Camera QA instead of creating a parallel runtime:

```text
C9R Camera Override Authority
  real positive Activity/Route lifecycle evidence

QaCameraAdr004BNegativeIntegrityRegression
  cases 01-13 and final aggregation

QaPersistentCameraPresentationCompositionRegression
  case 17

QaCameraOutputSessionBindingAuthoringRegression
  case 18
```

Case 16 is a focused abnormal owner-loss probe executed inside the same C9R
lifecycle boundary.

## Certified matrix

| # | Case | Final result |
|---:|---|---|
| 1 | Higher precedence wins | PASS |
| 2 | Equal precedence + distinct tie-breakers deterministic | PASS |
| 3 | Equal precedence + missing tie-breaker blocks | PASS |
| 4 | Equal precedence + duplicate tie-breaker blocks | PASS |
| 5 | Duplicate RequestId blocks | PASS |
| 6 | Wrong OutputId blocks | PASS |
| 7 | Repeated Publish preserves state | PASS |
| 8 | Repeated Release preserves state | PASS |
| 9 | Release current winner restores next | PASS |
| 10 | Out-of-order release preserves winner | PASS |
| 11 | Admission physical-apply failure rolls back | PASS |
| 12 | Release replacement failure rolls back | PASS |
| 13 | Rollback failure is explicit | PASS |
| 14 | Activity lifecycle exit cleans only Activity request | PASS |
| 15 | Route lifecycle exit cleans only Route request | PASS |
| 16 | Abnormal owner disable does not orphan request | PASS after 004C |
| 17 | Duplicate persistent output authoring blocks | PASS |
| 18 | Invalid output binding references block | PASS |

## Case 15 QA evidence correction

The first implementation incorrectly required the synthetic Session survivor to
be the resulting winner after Route exit. That was stronger than the contract:
the persistent Session Camera may legitimately be re-published during the
transition and win by precedence.

The corrected invariant is owner-scoped:

```text
Route request absent
+ synthetic Session survivor still admitted
+ winner remains arbitration-owned
```

This was a QA evidence correction, not a package change.

## Case 16 discovery and 004C handoff

The first valid 004B execution reproduced:

```text
case='16-abnormal-owner-loss'
operation='DisableRouteOwner'
admittedBefore='2'
admittedAfter='2'
orphan='True'
```

That proved a real package owner-lifetime defect and correctly opened
`IF-ADR-004C — Camera Owner Lifetime Integrity`.

004B was intentionally **not** made green by weakening the probe. The package was
fixed in the separate 004C cut and 004B was then rerun unchanged against the
resolved boundary.

## Final certification evidence

After 004C:

```text
[CAMERA_RUNTIME_HOST_INTEGRATION_REGRESSION]
status='Passed'
cases='11'

[QA_CAMERA_ADR004C]
status='Passed'
cases='10/10'
failed='0'

[QA_CAMERA_ADR004B]
status='Passed'
cases='18/18'
failed='0'
blocked='0'
verdict='ADR-004B CAMERA NEGATIVE INTEGRITY CERTIFIED'
```

The former case 16 now reports:

```text
admittedBefore='2'
admittedAfter='1'
orphan='False'
```

## Acceptance result

004B now proves that the current single-output Camera product:

- arbitrates deterministically;
- rejects ambiguous/invalid request identity and output state;
- preserves idempotent publication/release semantics;
- restores winners correctly;
- maintains transactional logical/physical integrity;
- exposes rollback failure explicitly;
- cleans normal and abnormal owner lifetime without orphaning admitted requests;
- blocks invalid persistent output composition and invalid physical references.

## Post-certification QA teardown hygiene

A later teardown log showed the QA-only synthetic Local Player binding attempting
a redundant release after its request had already disappeared from the output
context. This occurred **after** the C9R/004C/004B functional gates were green.

The QA cleanup patch reconciles local synthetic publisher state with the output
context before performing a second release. It is teardown hygiene and does not
change the 18-case certification contract.

A clean-log rerun of that QA-only hygiene patch may be recorded separately; it is
not a reason to reclassify 004B as technically uncertified.

## Current disposition

```text
IF-ADR-004B
  CLOSED
  CERTIFIED 18/18

Package defect discovered by first run
  RESOLVED by IF-ADR-004C

Broad Camera redesign
  NOT REQUIRED
```
