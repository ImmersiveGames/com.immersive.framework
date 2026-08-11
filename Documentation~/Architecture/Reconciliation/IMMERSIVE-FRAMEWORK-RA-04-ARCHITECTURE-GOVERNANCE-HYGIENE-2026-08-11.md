# Immersive Framework — RA-04 Architecture Governance Hygiene

Date: **2026-08-11**  
Status: **IMPLEMENTATION READY / QA PENDING**  
Type: **Architecture governance + focused authoring validation correctness fix**

## Objective

Reconcile framework-wide governance primitives discovered by the Package -> ADR
reverse audit without promoting them into artificial feature systems.

The cut covers:

- `FrameworkApiStatus` / `FrameworkApiStatusAttribute`;
- `FrameworkValidationMode` / `FrameworkValidationModePolicy`;
- the exact relationship between validation mode and ADR-010 authoring diagnostics;
- a focused correction for unknown validation-mode severity.

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
```

## Files created / edited / removed

### Package

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

## Expected technical smoke

QA menu:

```text
Immersive Framework
  -> QA
    -> Regressions
      -> Editor UX
        -> Run Framework Validation Mode Policy
```

Expected terminal evidence:

```text
[RA04_QA_VALIDATION_GOVERNANCE]
status='Passed'
cases='17'
unknownKnown='False'
unknownWarningsAsErrors='True'
```

## Technical acceptance criteria

- Package compiles in Unity 6.5.
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

## Product acceptance criteria

- Existing valid authoring flow is unchanged.
- An invalid serialized validation value cannot silently reduce diagnostic severity.
- The framework does not rewrite the invalid value automatically.
- Diagnostics remain actionable and consistent with ADR-010 principles.
- No new user-facing configuration burden is added.

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

Package implementation: **READY**  
Static policy reconciliation: **READY**  
Unity package compile: **PENDING USER EXECUTION**  
Focused QA regression: **PENDING USER EXECUTION**  
RA-04 certification: **PENDING**

The framework tracker is intentionally not marked certified by this artifact.
After the focused QA run passes, a small certification/tracker update can close
RA-04 without reopening ADR-010.

## Suggested commits

Package:

```text
fix(authoring): enforce validation governance semantics
```

QAFramework:

```text
qa: prove validation governance policy
```
