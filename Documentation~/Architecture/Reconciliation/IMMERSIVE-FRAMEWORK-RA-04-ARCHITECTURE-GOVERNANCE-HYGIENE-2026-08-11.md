# Immersive Framework — RA-04 Architecture Governance Hygiene

Date: **2026-08-11**  
Status: **CLOSED / CERTIFIED**  
Type: **Architecture governance + focused authoring validation correctness fix**

## Objective

Reconcile framework-wide governance primitives discovered by the Package -> ADR
reverse audit without promoting them into artificial feature systems.

The cut covers:

- `FrameworkApiStatus` / `FrameworkApiStatusAttribute`;
- `FrameworkValidationMode` / `FrameworkValidationModePolicy`;
- the exact relationship between validation mode and ADR-010 authoring diagnostics;
- a focused correction for unknown validation-mode severity;
- closure of the RA-03 API-hygiene handoff for Experimental Object Entry request/result surfaces.

## Scope

### API maturity governance

Record the existing maturity categories and their compatibility meaning.

`FrameworkApiStatusAttribute` remains metadata only. It does not become runtime
authority.

### Validation governance

Record the Stable severity matrix:

```text
Strict    Required=Fail  Warning=Error    Info=Include
Standard  Required=Fail  Warning=Warning  Info=Include
Release   Required=Fail  Warning=Warning  Info=Suppress
```

Unknown values are invalid and must behave conservatively as Strict until the
asset is corrected.

### Correctness fix

Before this cut, `FrameworkValidationModePolicy.GetSummary(unknown)` stated that
unknown values should be treated as Strict, while
`TreatWarningsAsErrors(unknown)` returned `false`.

That produced this inconsistent effective policy:

```text
unknown
  RequiredConfigurationFails = true
  TreatWarningsAsErrors       = false   <- divergence
  IncludeInfoDiagnostics      = true
```

The cut changes only warning promotion for unknown values:

```text
TreatWarningsAsErrors(mode)
  = Strict || !IsKnown(mode)
```

Valid Strict/Standard/Release behavior is unchanged.

## Out of scope

```text
new ADR
new validation subsystem
new runtime context/session/service
singleton or service locator
Composer/Profile/Recipe
Inspector redesign
automatic rewriting of invalid enum values
FIRSTGAME work
generic synthetic UX certification
promotion of Experimental Object Entry APIs to Stable
new Object Entry runtime behavior
```

## Files created / edited / removed

### Package implementation cut

```text
EDIT   Runtime/Authoring/FrameworkValidationModePolicy.cs
EDIT   Documentation~/Architecture/README.md
CREATE Documentation~/Architecture/Governance/
       IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md
CREATE Documentation~/Architecture/Reconciliation/
       IMMERSIVE-FRAMEWORK-RA-04-ARCHITECTURE-GOVERNANCE-HYGIENE-2026-08-11.md
```

Removed files: **none**.

### QA companion cut

The QAFramework companion adds one focused regression in the existing Editor UX
technical QA assembly. No new QA assembly or alternate validation architecture is
introduced.

Package implementation baseline:

```text
repository: ImmersiveGames/com.immersive.framework
commit: 7a20ec748e4e5f5f3764bdc34ee249c1fe1c1da6
message: fix(authoring): enforce validation governance semantics
```

QA regression baseline:

```text
repository: rinnocenti/QAFramework
commit: d65c5a7a637d4545e8b52b031614f879595335a3
message: qa: prove validation governance policy
```

## Product surface affected

Only authoring-validation severity for an **invalid/unknown serialized
`FrameworkValidationMode`** is affected.

Normal valid-mode authoring and Inspector flow remain unchanged.

## Expected use flow

```text
GameApplicationAsset selects a valid ValidationMode
        ↓
canonical authoring validator produces issues
        ↓
FrameworkValidationModePolicy controls severity/noise
        ↓
Inspector/report exposes actionable diagnostics
```

Invalid unknown serialized mode:

```text
unknown authored value
        ↓
no silent normalization
        ↓
conservative Strict diagnostic semantics
        ↓
asset remains explicitly in need of correction
```

## Technical QA

QA menu:

```text
Immersive Framework
  -> QA
    -> Regressions
      -> Editor UX
        -> Run Framework Validation Mode Policy
```

Certified terminal evidence from the user-executed Unity run:

```text
[RA04_QA_VALIDATION_GOVERNANCE]
status='Passed'
cases='17'
unknownKnown='False'
unknownWarningsAsErrors='True'
```

The runner executes the complete 17-case governance matrix and throws on any
contract divergence before emitting the terminal success marker.

## Technical acceptance criteria

- Package compiles sufficiently for the focused Unity Editor regression to execute.
- Strict semantics remain unchanged.
- Standard semantics remain unchanged.
- Release semantics remain unchanged.
- Unknown mode is reported by `IsKnown` as false.
- Unknown mode keeps required-configuration failure semantics.
- Unknown mode promotes warnings to errors.
- Unknown mode includes info diagnostics.
- Unknown summary continues to identify conservative Strict treatment.
- `FrameworkApiStatusAttribute` remains metadata-only.
- No new runtime authority, lookup or fallback mechanism is introduced.
- Focused QA regression passes all 17 cases.

Result: **PASS / CERTIFIED**.

## Product acceptance criteria

- Existing valid authoring flow is unchanged.
- An invalid serialized validation value cannot silently reduce diagnostic severity.
- The framework does not rewrite the invalid value automatically.
- Diagnostics remain actionable and consistent with ADR-010 principles.
- No new user-facing configuration burden is added.

Result: **ACCEPTED for the current product boundary**.

## RA-03 API-hygiene handoff disposition

RA-03 explicitly deferred the necessity/disposition of `ObjectEntryRequest` and
`ObjectEntryResult` to RA-04 API/governance hygiene.

Final disposition:

```text
ObjectEntryRequest
ObjectEntryResult
  -> RETAIN AS EXPERIMENTAL
```

Rationale:

- their current status is explicitly governed by `IF-GOV-001`;
- they are not promoted to Stable consumer contracts by this cut;
- they do not become runtime authority;
- no accepted current contract requires a new Object Entry execution system;
- their Experimental status is not a Stage A blocker;
- future promotion requires concrete accepted runtime/consumer evidence;
- future removal remains an allowed Experimental compatibility decision and must
  be handled explicitly when justified.

This closes the RA-03 API-hygiene handoff without inventing a new feature cut.

## Architectural gain

The package now distinguishes two governance concerns explicitly:

```text
API maturity metadata
  -> compatibility/documentation governance

Validation mode
  -> product authoring diagnostic policy under ADR-010
```

This removes the temptation to invent a runtime subsystem or ADR for either
primitive.

## Usability gain

Corrupt/obsolete validation-mode data fails conservatively instead of making an
invalid asset appear less severe than Strict policy promises.

For valid authored values there is no UX change.

## QA / certification disposition

```text
Package implementation:       IMPLEMENTED
Static policy reconciliation: CLOSED
Focused QA regression:        PASSED — 17/17
Unknown known-state:           False
Unknown warnings-as-errors:   True
RA-04 certification:          CLOSED / CERTIFIED
```

No additional RA-04 implementation or QA run is required for this accepted
boundary.

## Reopen conditions

Reopen RA-04 only when at least one of the following occurs:

- a governance regression is reproduced;
- `FrameworkApiStatus` compatibility meaning changes;
- validation-mode semantics change;
- a new validation mode is accepted without explicit severity/noise semantics;
- a Stable API compatibility change requires migration handling.

FIRSTGAME usability findings do not reopen RA-04 by default.

## Suggested commit

```text
docs(architecture): certify RA-04 governance closure
```
