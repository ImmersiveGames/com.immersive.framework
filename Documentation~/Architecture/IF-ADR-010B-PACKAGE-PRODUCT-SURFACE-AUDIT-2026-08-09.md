# IF-ADR-010B — Package Product Surface Audit

Date: 2026-08-09  
Type: UX/Product audit / documentation  
Status: **CLOSED — current package surface rebaselined**  
Implementation changes: **none**  
FIRSTGAME changes: **none**  
QAFramework changes: **none**  
Post-audit correction: **IF-ADR-010C cancelled; synthetic UX/Inspector QA is not required**

## 1. Baseline

```text
com.immersive.framework
  43b96a4b100b8273da1190520536007ba82dc081
  ADR-010B

QAFramework
  b6a45728285ddb2ce08269fc1f88ae3f1a4235e4
  P0 — Serialized Player Migration Integrity
```

Normative basis:

```text
IF-ADR-010 — Editor and Inspector Product Surface Authority
Status: Accepted
Rule: manual explicit authoring is the default
Rule: ADR-010 compliance is not measured by tooling/automation quantity
```

This audit evaluates the package against the accepted ADR-010 standard.

Historical gap lists are not treated as current authority when the current
package exposes a different legitimate product lifecycle.

## 2. Corrected executive verdict

The package does **not** have a generalized lack of Editor tooling.

The dominant state is:

```text
broad product-surface implementation
+
stale historical documentation in some ADRs
+
a few optional presentation-normalization opportunities
```

The audit found no evidence that the framework needs a new generic:

```text
Wizard system
Composer framework
Auto Setup system
Fix Everything workflow
Create Everything workflow
global authoring manager
second validation architecture
```

Manual explicit authoring remains a canonical product path.

Important post-audit correction:

The original 010B draft interpreted uneven Editor QA as the largest cross-cutting
gap and proposed an IF-ADR-010C synthetic/canonical product-surface QA program.

That recommendation is **superseded**.

The corrected rule is:

```text
UX/product conformity
  = package/manual review
  + real consumer observation in FIRSTGAME when useful

technical/editor contract QA
  = system-specific
  + only when a deterministic technical invariant exists
```

There is no ADR-010-wide UX QA program.

## 3. Audit method

Each consumer-facing surface was evaluated against the applicable ADR-010
questions:

```text
Official Path
Intent First
Configuration Status
Actionable Diagnostics
Safe Explicit Remediation
Advanced / Debug
Editor Write Safety
Runtime Discipline
Risk-Appropriate Technical QA
```

Conditional capabilities were evaluated only where they actually exist:

```text
Profile / Recipe / Template
Composer
Apply / Rebuild
Create action
Wizard
materialization receipt
```

No feature was penalized for lacking a conditional capability.

Product-surface classification:

```text
COMPLIANT
PARTIAL
NON-COMPLIANT
REBASELINE REQUIRED
NOT APPLICABLE
```

## 4. Shared Editor foundations

### FrameworkAuthoringInspectorGui

Current shared vocabulary includes:

```text
ProductHeader
IntentSummary
Section
RuntimeBinding
AdvancedFoldout
ApplySuggestion
```

Classification:

```text
Role: FOUNDATION
State: STRONG
```

The helper is not normative by itself.

Consistency is semantic; not every Custom Editor must use the same helper or
identical visual layout.

### Authoring validation

Current `FrameworkAuthoringValidationIssue` contract:

```text
Severity
Message
Context
IsOptionalSkip
```

Current GUI behavior includes summary counts, issue HelpBoxes, context
selection/ping and report logging.

The audit does not justify adding typed `Category`, `Hint`, `Asset` or
`ContextLabel` fields preemptively.

## 5. Corrected product-surface matrix

| Area / Surface | Lifecycle | Package surface | Current disposition |
|---|---|---|---|
| Shared Inspector GUI | Support | N/A — foundation | KEEP |
| Shared Authoring Validation | Support | N/A — foundation | KEEP |
| Player Participation / Provisioning | A + B | COMPLIANT | KEEP |
| Activity asset/profile authoring | B | COMPLIANT | KEEP |
| Activity Request Trigger | A | COMPLIANT | KEEP; normalization optional |
| Route asset/profile authoring | B | COMPLIANT | KEEP |
| Route Request Trigger | A | COMPLIANT | KEEP; normalization optional |
| Game Application / Project Settings | B / Settings | COMPLIANT | KEEP |
| Camera Rig Composer | C | COMPLIANT / reference | KEEP; Camera work separate |
| Pause Request | A | COMPLIANT | KEEP |
| Unity Input Gate | A | COMPLIANT SEMANTICALLY | KEEP; normalization optional |
| Activity Restart | A | COMPLIANT | KEEP |
| Object Reset Group Trigger | A | COMPLIANT | KEEP |
| Activity Readiness Participant | A | COMPLIANT | KEEP |
| Persistent Content Scene Template | B / Template | COMPLIANT AT PACKAGE LEVEL | REBASELINE RESOLVED |
| Loading / Transition standalone authoring | unresolved | inspect only if a real product question arises | NO TOOLING ASSUMPTION |
| Diagnostics-only surfaces | Support | NOT APPLICABLE | KEEP |
| Camera technical bindings | Support | NOT APPLICABLE | KEEP |

The matrix separates:

```text
product-surface conformity
technical QA needs
consumer UX evidence
```

These are not one score.

## 6. Key subsystem conclusions

### Player

Current command/profile surfaces are legitimate Class A/B product surfaces.

Historical statements that Player requires a Composer/Wizard to satisfy
ADR-010 are stale.

No Player Composer, Wizard or auto-setup cut is justified by ADR-010B.

### Pause

Direct authoring is sufficient.

A Pause Composer or Wizard would be over-authoring under current evidence.

### Activity Restart

Direct authoring is sufficient.

No additional authoring layer is justified.

### Reset

The inspected primary surface is compliant.

The broader Reset family may receive future technical hardening only for real
runtime/editor invariants.

No generic Reset Composer is justified.

### Activity and Route

Current authored assets, profiles and request triggers are semantically compliant.

Identical header/layout grammar is not required.

### Activity Readiness

Direct authoring is sufficient.

No Recipe/Composer/Apply flow is required by ADR-010.

### Game Application / Project Settings

The official Project Settings path is a legitimate Class B/settings product
surface.

Deterministic asset creation is valid convenience tooling because it does not
invent gameplay intent.

### Camera Rig

Camera is a valid Class C materialized-composition reference.

The previous 010B recommendation that ADR-010C must certify Camera Editor behavior
is retired.

Future Camera technical QA may still be valid if independently required by
Camera's own materialization contract.

That work belongs to Camera, not ADR-010 UX certification.

Camera redesign remains separate/deferred.

### Unity Input Gate

Current classification:

```text
Lifecycle: A
Product surface: COMPLIANT SEMANTICALLY
Presentation normalization: OPTIONAL
```

No Input Gate UX smoke is required.

If a deterministic Editor/runtime contract later proves risky, add technical QA
for that contract only.

### Persistent Content

Current lifecycle:

```text
Physical Source Scene
        ↓
Scene Template
        ↓
consumer-created .unity scene
        ↓
non-mutating package verification
```

Classification:

```text
Class B — reusable Template
Product surface: COMPLIANT AT PACKAGE LEVEL
```

The earlier `REBASELINE REQUIRED` state is resolved.

Persistent Content does not need a Composer/Apply flow.

## 7. Validation contract correction

Current validation contract is intentionally small:

```text
FrameworkAuthoringValidationIssue
  Severity
  Message
  Context
  IsOptionalSkip
```

Actionable diagnostics are a semantic requirement, not a requirement for a
larger issue DTO.

Do not add fields unless a concrete product case proves the current contract
insufficient.

## 8. What this audit did NOT find

No current evidence justifies:

```text
generic Wizard framework
Player Composer requirement
Pause Composer
Reset Composer
Input Gate Composer
automatic complete-game setup
automatic gameplay remediation
generic Apply/Rebuild for direct components
new runtime authority
second authoring-validation system
synthetic Inspector UX certification
```

No feature is incomplete merely because one of those layers does not exist.

## 9. Corrected remaining items

### System-specific technical hardening

Technical QA remains legitimate when a real invariant exists.

Examples:

```text
materialization idempotency
ownership preservation
Undo/Prefab safety for a writer
deterministic asset creation
runtime command gating
known negative runtime cases
```

These tests belong to the corresponding system.

They are not an ADR-010C program.

### Loading / Transition lifecycle

The audit did not fully classify a standalone Loading/Transition authoring
lifecycle outside Activity/Route/Persistent composition.

Do not create tooling from that uncertainty.

Map it only when a real product question or consumer workflow requires it.

### Presentation normalization

Differences in section headers/layout may be normalized during maintenance.

They are low priority unless a real user problem is demonstrated.

## 10. IF-ADR-010C disposition

```text
IF-ADR-010C — Canonical Editor Product-Surface QA
  CANCELLED / NOT REQUIRED
```

Reason:

```text
synthetic Inspector rendering is not useful UX certification
manual package review already establishes product-surface conformity
FIRSTGAME is the real consumer UX observation surface
technical Editor behavior belongs to system-specific QA when justified
```

Do not revive 010C as:

```text
ProductHeader smoke
IntentSummary smoke
Input Gate UX smoke
generic OnInspectorGUI smoke
Inspector screenshot-equivalence test
```

## 11. FIRSTGAME

FIRSTGAME is separate Consumer UX Evidence.

It may reveal:

```text
unclear fields
unclear asset creation order
confusing ownership
intent/debug mixing
repetitive manual setup
insufficient diagnostics
```

A confirmed friction may justify the smallest package improvement.

FIRSTGAME is not required for technical/package closure and does not own official
framework contracts.

## 12. ADR-010B acceptance

ADR-010B is closed because:

```text
current package surfaces are mapped
historical missing-tooling assumptions are rejected where stale
conditional tooling is not treated as mandatory
Camera is identified as a Class C reference, not a universal template
Persistent Content lifecycle is rebaselined
validation contract is described accurately
semantic compliance is distinguished from cosmetic normalization
synthetic UX QA is explicitly rejected as a closure requirement
```

Result:

```text
ADR-010 normative standard          ACCEPTED
ADR-010A product-surface standard   CLOSED
ADR-010B package audit              CLOSED
ADR-010C                            CANCELLED / NOT REQUIRED
general missing tooling finding     NOT CONFIRMED
new automation required             NO
FIRSTGAME required for closure      NO
```

## 13. Suggested commit message

```text
Rebaseline ADR product-surface documentation
```
