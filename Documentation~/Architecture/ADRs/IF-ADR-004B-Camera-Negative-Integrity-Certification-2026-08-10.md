# IF-ADR-004B — Camera Negative Integrity Certification

Status: **PROPOSED — NEXT TECHNICAL GATE**  
Date: 2026-08-10  
Type: **Technical QA**  
Primary repository: **QAFramework**  
Normative target: **IF-ADR-004 — Camera Requests and Output Authority**

## Objective

Certify the negative and transactional integrity boundaries of the current
official Camera implementation **before changing the package**.

The cut must attempt to break the accepted single-output architecture across:

- deterministic arbitration;
- request identity/output validation;
- publish/release idempotence;
- restoration ordering;
- logical/physical rollback integrity;
- owner lifecycle cleanup;
- persistent output authoring validation.

The cut exists to determine whether the current package is correct, not to assume
that a new Camera architecture is required.

## Existing QA baseline

The current QAFramework already contains Camera coverage, including the C9R
Camera Override Authority surface and focused Camera authoring/persistent
composition regressions.

Existing evidence includes the canonical product precedence convention:

```text
Local Player  50
Activity     100
Route        200
Session      300
```

and positive request/release/restoration/lifecycle behavior.

IF-ADR-004B extends that evidence. It must not duplicate the entire existing C9R
suite merely under a new name.

## Scope

### Runtime integrity

- request admission validation;
- deterministic equal-precedence handling;
- duplicate identity blocking;
- output mismatch blocking;
- repeated Publish/Release idempotence;
- winner restoration and out-of-order release;
- physical apply failure rollback;
- release replacement failure rollback;
- explicit rollback-failure evidence;
- lifecycle cleanup ownership;
- abnormal owner-loss behavior.

### Editor / authoring integrity

- duplicate Persistent Camera Output composition blocking;
- missing/invalid output reference blocking;
- invalid/unmaterialized local rig failure where it affects request application;
- actionable diagnostics.

## Out of scope

This cut does not authorize:

- package redesign before a failing regression proves need;
- FIRSTGAME changes;
- multi-output;
- split-screen;
- concurrent per-player outputs;
- a global `CameraManager`;
- a service locator;
- a static Camera request registry;
- a generic cross-feature request broker;
- a new Camera runtime context beyond the existing scoped output architecture;
- a Recipe/Profile/Wizard/second Composer;
- new Cinemachine presentation modes;
- broad Camera Inspector redesign;
- AudioListener ownership changes.

## Required QA matrix

| # | Case | Required result |
|---:|---|---|
| 1 | Higher precedence | Higher-precedence valid request becomes winner. |
| 2 | Equal precedence + distinct deterministic tie-breakers | Admission succeeds and winner is deterministic independent of publication/enumeration timing. |
| 3 | Equal precedence + missing tie-breaker | Conflicting admission is blocked with explicit tie-break diagnostic. Existing winner/state is preserved. |
| 4 | Equal precedence + duplicate tie-breaker | Conflicting admission is blocked. Existing winner/state is preserved. |
| 5 | Duplicate RequestId | Duplicate admission is blocked explicitly; no replacement or hidden mutation occurs. |
| 6 | Wrong OutputId | Request is blocked; output state is unchanged. |
| 7 | Repeated Publish | Second Publish is `Preserved`/equivalent and does not admit another request or mutate physical output. |
| 8 | Repeated Release | Second Release is `Preserved`/equivalent and does not mutate context/output. |
| 9 | Release current winner | Next valid admitted request is restored and physically applied. |
| 10 | Out-of-order release | Releasing a non-winning request preserves the correct winner/output. |
| 11 | Admission physical-apply failure | Logical admission is rolled back; previous logical/physical state is restored; result is not success. |
| 12 | Release replacement physical-apply failure | Released request is re-admitted and previous state restored; result is not success. |
| 13 | Rollback failure | Explicit `RollbackFailed`/blocking evidence is returned; failure is not hidden as normal state. |
| 14 | Activity lifecycle exit | Only the Activity-owned request is cleaned; other valid requests remain/restored correctly. |
| 15 | Route lifecycle exit | Only the Route-owned request is cleaned; other valid requests remain/restored correctly. |
| 16 | Abnormal owner disable/destruction | Prove the accepted higher-level cleanup invariant or reproduce an orphan request. This case decides whether 004C is needed. |
| 17 | Duplicate Persistent Camera Output | Composition validation blocks the invalid single-output authoring state. |
| 18 | Missing/invalid output binding references | Initialization/validation fails explicitly with actionable diagnostics and no fallback lookup. |

## Evidence requirements

Each case must produce machine-readable or otherwise objective evidence that can
be reviewed without relying only on visual Camera movement.

Where applicable, evidence should include:

```text
case id
operation
request id
owner/lifetime scope
output id
precedence
tie-break id
previous winner
resulting winner
context operation result
physical apply result
rollback attempted
rollback result
issue code/message
final admitted request count/ids
PASS / FAIL
```

A visual scene may remain useful for manual observation, but visual movement
alone is not certification for rollback/idempotence/negative-state contracts.

## Test design requirements

### Deterministic fault injection

Cases 11–13 require a deterministic way to force physical application failure
and, for case 13, rollback failure. Prefer a narrow QA seam or fixture-controlled
invalid rig/output state over timing-sensitive manipulation.

Do not change production architecture solely to make the test convenient unless
the current package exposes no reasonable test seam and that limitation itself
is documented.

### No global discovery in QA implementation

The QA harness may assemble explicit references, but should not certify Camera
by introducing a global manager, `Camera.main` authority, scene-name lookup or
service locator that the product forbids.

### Preserve existing C9R purpose

C9R remains the canonical positive authority/restoration proof. 004B should add
focused negative regressions or extend existing Camera regressions without
turning the QAFramework into parallel overlapping harnesses.

## Case 16 decision gate — abnormal owner loss

This is the most important diagnostic branch of 004B.

### Outcome A — no orphan; lifecycle invariant is sufficient

If QA proves that every supported owner destruction path necessarily causes the
official lifecycle exit/detach/release before Camera ownership becomes invalid:

```text
004B records the invariant
004C is NOT opened
package remains unchanged
```

### Outcome B — orphan request reproduced

If an owner can be disabled/destroyed while its admitted request remains valid in
`CameraOutputContext` beyond the owner's accepted lifetime:

```text
004B FAILS that case with reproducible evidence
open IF-ADR-004C — Camera Owner Lifetime Integrity
implement the smallest scoped package fix
rerun 004B
```

The existence of Outcome B still does not authorize a global cleanup manager.
The fix should remain at the narrow owner/binding/publisher/lifecycle boundary.

## Recommended QA file shape

These are proposed locations/names for a canonical extension; reuse or extend
existing Camera QA utilities where that avoids duplication:

```text
Assets/ImmersiveFrameworkQA/Camera/Scripts/Runtime/
  QaCameraOutputIntegrityRegression.cs

Assets/ImmersiveFrameworkQA/Camera/Scripts/Editor/
  QaCameraOutputIntegrityAuthoringRegression.cs

Assets/ImmersiveFrameworkQA/Camera/Documentation/
  ADR004B-CAMERA-NEGATIVE-INTEGRITY-QA.md
```

The exact file split is implementation-dependent. One coherent regression runner
is preferable to several overlapping manual smokes.

## Expected technical smoke

A canonical 004B runner should expose one unambiguous final verdict, for example:

```text
[QA_CAMERA_ADR004B]
status='Passed'
cases='18/18'
verdict='ADR-004B CAMERA NEGATIVE INTEGRITY CERTIFIED'
```

A failing case must retain its individual diagnostic evidence and prevent the
final certified verdict.

If some cases are intentionally delegated to existing C9R/editor regression
entrypoints, the final certification runner/report must reference those executed
results rather than silently assuming them.

## Technical acceptance

004B is certified only when:

- all applicable matrix cases pass on the current official package revision;
- equal-precedence behavior is deterministic and timing-independent;
- duplicate/mismatched requests fail explicitly and preserve valid state;
- Publish/Release idempotence is proven;
- winner restoration/out-of-order release are proven;
- physical application failures cannot silently commit logical state;
- admission/release rollback behavior is proven;
- rollback failure is explicit and diagnostic;
- lifecycle cleanup affects only the correct owner request;
- abnormal owner-loss behavior is resolved as a proven invariant or a
  reproducible package defect;
- persistent single-output authoring violations block explicitly;
- no global/discovery Camera authority is introduced by the test harness;
- final evidence identifies the package revision exercised.

## Product acceptance

This is not a product-UX implementation cut. Product acceptance is therefore:

- no regression to `CameraRigComposer` as the local rig authoring surface;
- no regression to `CameraOutputSessionBinding` as explicit persistent output
  authoring;
- no new mandatory authoring layer;
- no need for normal users to construct internal context/session/publisher
  machinery;
- failures exposed by the technical QA remain actionable through the supported
  diagnostics surfaces where consumer action is possible.

## Architectural gain

The cut converts Camera's strongest remaining uncertainty from source inspection
to executable evidence. It protects deterministic arbitration and transactional
logical/physical consistency without redesigning a runtime architecture that is
already structurally sound.

## Usability gain

Indirect but important: consumers can trust that explicit request/release and
restoration semantics will not leave the game on a stale Camera when invalid
configuration or exceptional lifecycle behavior occurs.

## Files expected to be created / altered / removed

### QAFramework

Exact implementation files may reuse existing utilities, but the cut should
produce:

- one canonical negative-integrity regression surface;
- supporting deterministic fault fixture/seam if required;
- concise QA documentation describing execution and expected evidence.

### com.immersive.framework

- none initially.

Package changes are allowed only after a failing 004B case proves a concrete
defect and the corresponding narrow implementation cut is approved.

### FIRSTGAME

- none.

### Removed

- none expected.

## Suggested commit message

```text
test(camera): certify ADR-004 negative integrity
```
