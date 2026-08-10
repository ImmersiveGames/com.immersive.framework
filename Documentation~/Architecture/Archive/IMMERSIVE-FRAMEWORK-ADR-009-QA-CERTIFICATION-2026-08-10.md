# Immersive Framework — ADR-009 QA Certification

**Date:** 2026-08-10  
**ADR:** IF-ADR-009 — Activity Local Visibility Rules  
**Status:** **Certified — current accepted boundary**  
**Package:** `com.immersive.framework`  
**QA harness:** `QAFramework`

## Purpose

Record the Unity execution evidence used to close the current technical boundary
of IF-ADR-009 after the focused package audit and corrections.

## Corrected gaps

The audit found and corrected two concrete gaps:

1. an invalid `Required` local-visibility binding could be diagnosed but continue toward commit;
2. two distinct authored definitions using the same stable `ActivityId` were not rejected.

The corrected behavior is:

```text
invalid Required binding
  -> diagnostic
  -> transition rejected before commit
  -> previous Activity preserves authority

invalid Optional binding
  -> diagnostic
  -> no visibility mutation
  -> Required behavior is not weakened

distinct definitions sharing ActivityId
  -> invalid collision
  -> stable authored ID does not become runtime ownership authority
```

The focused audit also confirmed that occurrence/revision, replacement and scope
disposal already use the existing serialized transaction model and
`RuntimeDefinitionToken`.

## Unity QA evidence

### Activity Local Visibility Rule Regression

```text
[QA_ACTIVITY_LOCAL_VISIBILITY_RULE]
status='Passed'
cases='28'
completed='positive,negative,no-active,invalid,idempotent,single-owner'
```

Source runner:

```text
QaActivityLocalVisibilityRuleRegression.Run()
```

### Activity Local Visibility Lifecycle Regression

```text
[QA_ACTIVITY_LOCAL_VISIBILITY_LIFECYCLE]
status='Passed'
cases='18'
completed='positive-single,positive-multiple,negative-single,negative-multiple,no-active-visible,required-invalid-blocks,optional-invalid-diagnostic,clear,idempotence'
```

Source runner:

```text
QaActivityLocalVisibilityLifecycleRegression
```

## Certified invariants

The recorded execution provides evidence for the current accepted boundary:

- explicit positive and negative visibility rules;
- no-active behavior;
- invalid configuration handling;
- idempotent rule/lifecycle behavior;
- single-owner authority;
- required-invalid transition blocking before commit;
- optional-invalid diagnostic behavior without mutation;
- clear/release behavior;
- single- and multiple-target lifecycle behavior.

Combined with the focused implementation audit, the accepted authority model
remains occurrence-scoped. Stable authored identity is not used as a substitute
for runtime occurrence, release or restoration ownership.

## Cross-ADR conformance

The executed evidence also confirms compatibility with the related decisions:

- **IF-ADR-001:** visibility authority remains scoped to the active lifecycle occurrence;
- **IF-ADR-002:** direct adapter authoring is sufficient for the accepted boundary;
- **IF-ADR-006:** invalid Required configuration rejects before transition commit and is diagnostic;
- **IF-ADR-007:** visibility behavior remains downstream of Activity lifecycle/readiness rather than replacing readiness authority;
- **IF-ADR-010:** no additional authoring layer is required for a valid official surface;
- **IF-ADR-014:** stable `ActivityId` collision is rejected and does not become occurrence/ownership authority.

These observations do not expand the certified case count; they record which
normative boundaries the 46 executed cases preserve.

## Scope note

FIRSTGAME was not used as a technical closure gate for this ADR. Real-game
integration may provide separate Consumer UX Evidence later, but it is not
required for the current technical boundary.

## Verdict

```text
IF-ADR-009
Architecture: Accepted
Package: Implemented
QA: Certified
FIRSTGAME: Not Applicable for current accepted boundary
Status: CLOSED — current accepted boundary
```
