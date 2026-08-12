# Immersive Framework — Stage A Canonical Package Baseline Closure

Date: **2026-08-11**  
Status: **CLOSED / APPROVED FOR FIRSTGAME STAGE B**  
Type: **Documentation / certification / real-consumer handoff**

## Objective

Freeze an explicit canonical package baseline after the current architecture,
implementation, technical QA and reverse-audit reconciliation work, so the next
FIRSTGAME effort starts as a complete Stage B real-consumer task rather than as a
continuation of Stage A auditing.

This record does not claim the framework is feature-complete forever. It records
that the **currently accepted Stage A boundaries are the approved technical
baseline for the next FIRSTGAME**.

## Canonical baselines

### Framework package

```text
repository: ImmersiveGames/com.immersive.framework
commit: 7a20ec748e4e5f5f3764bdc34ee249c1fe1c1da6
message: fix(authoring): enforce validation governance semantics
```

This commit contains the current RA-04 package implementation and governance
records on top of the previously reconciled ADR/package baseline.

### QAFramework

```text
repository: rinnocenti/QAFramework
commit: d65c5a7a637d4545e8b52b031614f879595335a3
message: qa: prove validation governance policy
```

The focused RA-04 Unity regression was executed successfully against the current
package boundary.

Terminal evidence:

```text
[RA04_QA_VALIDATION_GOVERNANCE]
status='Passed'
cases='17'
unknownKnown='False'
unknownWarningsAsErrors='True'
```

### FIRSTGAME

FIRSTGAME is **not** part of this certification baseline.

The next FIRSTGAME is the Stage B consumer that must consume the approved package
and prove real-game usability, authoring flow and integration behavior.

## Scope

This closure records:

```text
accepted ADR boundaries through ADR-018
current package implementation aligned to those accepted boundaries
current focused technical QA evidence
RA-01 through RA-04 reverse-audit closure
API maturity governance
validation-mode governance
Stage A -> Stage B handoff
canonical starting point for the next FIRSTGAME
```

## Out of scope

```text
new runtime behavior
new Editor tooling
new authoring surface
new QA runner
new ADR
FIRSTGAME implementation
future contracts already identified by existing ADR/tracker records
claim that Experimental APIs are Stable
generic certification of designer usability without a real consumer
```

## Stage A disposition

The current framework model is separated as follows:

```text
ADRs
  -> normative accepted boundaries

Package
  -> official implementation/product surface

QAFramework
  -> technical contract and regression evidence

Reconciliation / Governance / Tracking
  -> current architecture status and certification

FIRSTGAME
  -> Stage B real-consumer product/usability proof
```

For the accepted current technical boundaries, Stage A is closed unless a reopen
condition listed below is met.

ADR-010 may continue to receive **feature-owned product-surface adoption evidence**.
That is not a generic technical Stage A blocker and is expected to be exercised by
real feature usage, including FIRSTGAME.

## Reverse-audit closure

```text
RA-CUT-01  Application Frame Rate / ADR-017       CLOSED / CERTIFIED
RA-CUT-02  Persistence / ADR-018                  CLOSED FOR STAGE A
RA-CUT-03  Object Entry Ownership Reconciliation CLOSED / DOC RECONCILIATION
RA-CUT-04  Architecture Governance Hygiene       CLOSED / CERTIFIED
```

Open reverse-audit technical cuts: **none**.

The reverse audit therefore stops being an active discovery program. Architecture
governance continues as a maintenance discipline.

## RA-04 certification

RA-04 is closed with:

```text
FrameworkApiStatus governance     explicit
FrameworkValidationMode policy   explicit
Unknown ValidationMode           invalid / conservative Strict semantics
Focused QA                        PASS — 17/17
Unknown known-state               False
Unknown warnings-as-errors        True
Runtime authority added           none
New ADR required                  no
```

The exact QA terminal marker is the evidence recorded above.

## Object Entry API disposition

The RA-03 handoff for `ObjectEntryRequest` and `ObjectEntryResult` is closed by
RA-04 governance.

```text
ObjectEntryRequest  -> Experimental / retained
ObjectEntryResult   -> Experimental / retained
```

Their retention does not promote them to Stable, create runtime authority or
require a new Object Entry system.

`Experimental` is a governed maturity state, not an unresolved architecture gap.
Future promotion or removal requires explicit evidence/decision when that work is
actually justified.

## Canonical package decision for FIRSTGAME

The next FIRSTGAME must treat the package baseline in this record as the official
framework product available to a real consumer.

The consumer task should not begin by re-auditing every internal contract or by
recreating old project assets/settings as a foundation.

The Stage B question is:

```text
Can a user build a small real game with the current canonical package,
understand the authoring surfaces, diagnose failures and use the runtime
without reconstructing internal framework contracts manually?
```

FIRSTGAME findings must be classified before package work is opened:

```text
Product / UX finding
  -> authoring flow, Inspector clarity, discoverability, Apply/Rebuild,
     templates/samples, diagnostics, excessive internal knowledge

Real integration finding
  -> package works technically but consumer integration exposes a missing or
     awkward official product surface

Technical regression
  -> accepted package contract is reproducibly broken

Future scope
  -> desired capability is outside the currently accepted boundary
```

Only the third category automatically reopens a closed Stage A technical boundary.
Product/UX findings should become explicit product cuts and then be formalized in
the package when mature.

## Stage B areas already identified

The current tracker already identifies real-consumer proof for areas including:

```text
Player participation / provisioning / session profiles
Camera single-output consumer integration
Loading / Transition / readiness authoring and diagnostics
Pause consumer authoring/usability
participant-aware Loading progress
optional BGM integration
Progression Save built-in JSON / custom provider composition
feature-owned ADR-010 product-surface adoption
```

These are Stage B tasks, not evidence that Stage A is still open.

## Reopen rules

A closed Stage A boundary is reopened only by one of the following:

1. a reproducible regression against an accepted contract;
2. an accepted architecture/product contract change;
3. a newly accepted scope that extends the boundary;
4. a discovered contradiction between current package behavior and current
   normative documentation.

Do not reopen Stage A merely because:

```text
FIRSTGAME needs a clearer workflow
an Experimental API still exists
a designer-facing template/sample is missing
an Inspector is technically correct but difficult to understand
a future contract has not been implemented
```

Those are product/Stage B/future-scope findings unless they also reproduce an
accepted technical contract failure.

## Validation order after this closure

For technical package corrections:

```text
1. package official implementation
2. QAFramework focused technical proof
3. FIRSTGAME real integration when applicable
```

For Stage B UX/product findings:

```text
1. define the user-facing problem and expected workflow
2. prove the need/shape in FIRSTGAME when appropriate
3. preserve current technical contracts
4. formalize the mature solution in the package
5. add focused QA when a new official technical contract exists
```

## Files in this documentation closure

```text
EDIT   Documentation~/Architecture/Reconciliation/
       IMMERSIVE-FRAMEWORK-RA-04-ARCHITECTURE-GOVERNANCE-HYGIENE-2026-08-11.md

CREATE Documentation~/Architecture/Reconciliation/
       IMMERSIVE-FRAMEWORK-STAGE-A-CANONICAL-BASELINE-CLOSURE-2026-08-11.md

EDIT   Documentation~/Architecture/Tracking/
       IF-TRACK-Framework.md

EDIT   Documentation~/Architecture/README.md

EDIT   Documentation~/README.md
```

Runtime files changed: **none**.  
Editor implementation files changed: **none**.  
QA files changed by this documentation closure: **none**.  
Removed files: **none**.

## Acceptance criteria

### Technical documentation

- RA-04 no longer reports pending QA.
- RA-04 records the actual 17/17 terminal evidence.
- RA-03 Object Entry Experimental API handoff has an explicit disposition.
- RA-01 through RA-04 are marked closed.
- the tracker no longer presents RA-04 as `NEXT`.
- Stage A and Stage B remain explicitly distinct.
- future contracts remain future scope rather than false gaps.

### Product handoff

- a reader can identify the exact canonical package baseline for FIRSTGAME;
- FIRSTGAME is clearly identified as the next real-consumer phase;
- FIRSTGAME is not instructed to recreate internal framework contracts as its
  primary workflow;
- product/UX findings can be separated from technical regressions;
- the package remains the official destination for mature framework solutions.

## Final disposition

```text
PACKAGE -> ADR REVERSE AUDIT
  CLOSED

CURRENT STAGE A TECHNICAL BASELINE
  APPROVED

RA-04 GOVERNANCE
  CERTIFIED — 17/17

OPEN REVERSE-AUDIT CUTS
  NONE

NEXT PROGRAM PHASE
  FIRSTGAME / STAGE B REAL-CONSUMER PRODUCT VALIDATION
```

## Suggested commit

```text
docs(architecture): close Stage A canonical baseline
```
