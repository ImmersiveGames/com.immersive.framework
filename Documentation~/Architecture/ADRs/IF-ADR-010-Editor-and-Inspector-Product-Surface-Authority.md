# IF-ADR-010 — Editor and Inspector Product Surface Authority

Status: **Accepted**  
Last updated: 2026-08-09  
Normative classification: **Minimum product-surface standard accepted**  
Package conformity audit: **CLOSED — IF-ADR-010B**  
Implementation classification: **Broad package surface is semantically conformant; no generalized missing-tooling implementation is required**  
IF-ADR-010C: **CANCELLED / NOT REQUIRED**  
Related decisions: IF-ADR-002, IF-ADR-008, IF-ADR-012, IF-ADR-015, IF-ADR-016  
Current package baseline: `43b96a4b100b8273da1190520536007ba82dc081` (`ADR-010B`)  
Current QA baseline inspected: `b6a45728285ddb2ce08269fc1f88ae3f1a4235e4`

> ADR-010 acceptance freezes the product-surface rules below.
>
> IF-ADR-010B has already audited the current package against those rules.
>
> No synthetic UX/Inspector certification program is required to close ADR-010.

## 1. Context

The Unity Editor and Inspector are primary product surfaces through which
framework consumers discover, configure, understand and diagnose framework
features.

Technical correctness alone is insufficient, but product maturity must not be
measured by the amount of Editor automation provided.

The framework is not required to create complete gameplay setups, add Composers,
introduce Wizards, generate Profiles or expose Apply/Rebuild merely to satisfy a
product checklist.

The default authoring model is:

```text
manual
explicit
inspectable
diagnostic
```

Additional authoring layers are justified only when they solve concrete lifecycle
complexity.

## 2. Decision

The package owns the canonical Editor and Inspector presentation for official
framework features.

The minimum product-surface contract is:

```text
Official Path
Intent First
Configuration Status
Actionable Diagnostics
Safe Explicit Remediation
Advanced / Debug Separation
Editor Write Safety
Runtime Discipline
Risk-Appropriate Technical QA
```

Manual explicit authoring is the preferred default.

Capabilities such as:

```text
Create action
Wizard
Recipe / Profile / Template
Composer
Apply / Rebuild
generated technical containers
aggregated validation sessions
materialization receipts
```

are conditional capabilities, not universal requirements.

### Governing rule

```text
ADR-010 compliance is not measured by the amount of tooling or automation.
```

A simple feature may be fully compliant with:

```text
Add Component
    ↓
clear Inspector
    ↓
manual configuration
    ↓
explicit validation
    ↓
runtime evidence
    ↓
Advanced / Debug
```

without Wizard, Composer or Apply/Rebuild.

## 3. Product-surface principles

### 3.1 Designer edits intent

Normal authoring represents user-authored intent and configuration.

Consumers should not need private runtime modules, hidden handles, occurrence
identifiers or generated technical bindings to perform normal configuration.

### 3.2 Framework may derive technical facts

Technical configuration may be derived when the derivation is deterministic and
contains no new gameplay decision.

Example:

```text
authored Supported Slots = 4
        ↓
derived technical player limit = 4
```

The derived value is technical materialization, not a second authored authority.

### 3.3 Runtime remains runtime authority

Editor convenience must not introduce:

```text
global manager
service locator
implicit static registry
FindObjectOfType authority
object-name authority
silent fallback
```

Runtime authority remains in the appropriate scoped Session, Context, Service,
runtime module or typed adapter.

### 3.4 No accidental gameplay from authoring

Authoring components and Editor tooling must not execute gameplay by accident.

Runtime commands exposed through Inspectors must be explicit, scoped and
Play-Mode appropriate.

## 4. Minimum universal product-surface contract

### 4.1 Official Path

A feature must have a clear official path.

Valid paths include:

```text
Add Component
Assets > Create
GameObject menu
Project Settings
existing asset Inspector
existing authoring component
documented scene/prefab composition path
```

A dedicated Create menu is not required.

### 4.2 Intent First

Normal Inspector semantics should prioritize, where applicable:

```text
Intent / purpose
Configuration
Configuration Status / Validation
Runtime Status
Explicit Actions
Advanced / Debug
```

This is semantic vocabulary, not a mandatory visual template.

### 4.3 Configuration Status

Required invalid/incomplete state must be explicit.

Where the concepts exist, consumers should be able to distinguish:

```text
authored configuration
configuration validity
effective runtime state
last Editor/runtime operation
```

### 4.4 Actionable Diagnostics

Diagnostics should identify the actual problem and relevant context.

The existing framework validation infrastructure remains canonical where
applicable.

ADR-010 does not create a second validation architecture.

### 4.5 Safe Explicit Remediation

Remediation is optional.

When provided, it must be:

```text
explicit
predictable
Undo-aware when it writes Editor state
non-destructive
diagnostic
limited to deterministic safe corrections
```

Avoid broad actions that invent gameplay intent.

### 4.6 Advanced / Debug

Technical evidence should be inspectable without dominating normal authoring.

Possible evidence includes:

```text
stable authored IDs
scope
bindings
handles
revisions
occurrences
technical components
materialization details
receipts
last result
runtime diagnostics
```

Advanced/Debug is a disclosure mechanism, not another authority.

### 4.7 Editor Write Safety

Editor operations that write assets, scenes, prefabs or components must respect
the lifecycle they support.

Applicable risks can include:

```text
Undo / Redo
Prefab Stage
dirty/save semantics
multi-object editing
asset duplication
domain reload
```

Unsupported contexts must be rejected explicitly rather than partially applied.

### 4.8 Runtime Discipline

Play Mode Inspector surfaces should primarily expose:

```text
effective runtime state
read-only evidence
diagnostics
explicit supported runtime commands
```

Runtime state is read-only by default.

### 4.9 Technical QA

QA coverage is proportional to actual deterministic risk.

Examples of legitimate technical/editor contracts include:

```text
validation behavior
idempotent technical materialization
Undo/Redo for an Editor writer
Prefab Stage safety
ownership preservation
deterministic asset creation
runtime command gating
```

A simple direct Inspector does not need a synthetic rendering test.

Do not use QAFramework to certify that an Inspector is understandable or visually
consistent.

## 5. Conditional authoring capabilities

### 5.1 Recipe / Profile / Template

Use when reusable intent or standalone authored identity is real.

Do not create an asset layer just because the architecture allows one.

### 5.2 Composer

Use when one authored instance coordinates several lower-level technical
contracts and direct manual composition is repetitive, error-prone or requires
internal framework knowledge.

A Composer is not runtime gameplay authority.

### 5.3 Apply / Rebuild

Use only when there is a real distinction between authored intent and derived
framework-owned technical materialization.

If there is nothing to materialize, there should be no Apply/Rebuild button.

### 5.4 Apply / Rebuild contract

When present, it must be:

```text
explicit
idempotent
deterministic
Undo-aware
non-destructive
ownership-safe
safe to repeat
diagnostic
```

### 5.5 Materialization ownership

Rebuild may remove or replace only content the framework can prove it owns.

Ownership must not be inferred solely from object name, hierarchy location or
generic component type.

User-owned content must be preserved.

### 5.6 Materialization receipt

Significant materialization should expose enough evidence to explain what
happened, proportional to the risk.

### 5.7 Create actions

Create actions are valid when they improve discovery or remove repetitive
deterministic setup.

They must not choose gameplay intent.

### 5.8 Wizard

Wizard is exceptional.

It must solve proven multi-step authoring complexity rather than act as a maturity
badge.

## 6. Product-surface classes

### Class A — Simple / Direct Authoring

```text
component or asset
few authored fields
no meaningful technical materialization
```

Expected:

```text
official path
clear Inspector
validation
runtime evidence when applicable
Advanced / Debug where relevant
```

Normally unnecessary:

```text
Composer
Apply / Rebuild
Wizard
```

### Class B — Reusable Authored Intent

```text
reusable Profile / Recipe / Template
+
one or more consumers
```

A separate Composer remains conditional.

### Class C — Materialized Composition

```text
authored intent
      ↓
validation
      ↓
explicit Apply / Rebuild
      ↓
framework-owned technical materialization
      ↓
runtime/diagnostic evidence
```

Class C carries stronger technical contracts around idempotency, ownership,
non-destructive writes and Editor safety.

Camera Rig remains an example of this lifecycle, not a universal template.

## 7. Architecture by need, not checklist

Do not add:

```text
Profile with no meaningful reuse
Composer wrapping one simple field
Apply button that materializes nothing
Wizard whose only job is Add Component
automatic setup that chooses gameplay intent
```

solely to satisfy ADR-002/010.

## 8. Current package audit result

IF-ADR-010B is closed.

Current package classification from that audit:

| Area | Current interpretation |
|---|---|
| Shared Inspector GUI | FOUNDATION |
| Authoring Validation | FOUNDATION |
| Player Participation / Provisioning | COMPLIANT / mature |
| Route / Activity / Application | COMPLIANT |
| Pause | COMPLIANT |
| Activity Restart | COMPLIANT |
| Reset primary surface | COMPLIANT |
| Unity Input Gate | COMPLIANT SEMANTICALLY |
| Global Settings | COMPLIANT |
| Persistent Content Scene Template | COMPLIANT AT PACKAGE LEVEL |
| Activity Readiness Participant | COMPLIANT |
| Diagnostics-only surfaces | SUPPORT / NOT APPLICABLE |
| Camera technical bindings | SUPPORT / NOT PRIMARY PRODUCT SURFACE |

The audit found:

```text
general missing tooling finding  NOT CONFIRMED
generic Wizard requirement       NO
generic Composer requirement     NO
generic Apply/Rebuild requirement NO
new authoring authority          NO
```

Loading/Transition may still be inspected under its own system lifecycle when a
concrete product question exists. It is not an automatic ADR-010 implementation
program.

## 9. Resolved audit findings

### Unity Input Gate

The existing Inspector is semantically substantial.

Differences in visual/header grammar are optional normalization, not
non-compliance.

No synthetic Input Gate UX smoke is required.

### Persistent Content

The current official lifecycle is a Scene Template with non-mutating verification.

Its former `REBASELINE REQUIRED` state is resolved.

No Composer/Apply flow is required.

### Camera

Camera remains a Class C reference only.

Any future Camera technical QA must be justified by Camera's own technical
contracts.

ADR-010 does not require a Camera UX QA program.

The broader Camera redesign is outside this documentation cut.

## 10. QA and FIRSTGAME boundary

The package owns the official product surface.

QAFramework proves deterministic technical/editor contracts.

FIRSTGAME evaluates real consumer ergonomics.

The correct distinction is:

```text
Technical Status
  contracts + runtime + relevant QA

Product Surface Status
  package authoring/diagnostic surface

Consumer UX Evidence
  real-game observation in FIRSTGAME
```

Consumer UX Evidence is not part of technical completion.

A missing FIRSTGAME observation must not demote an otherwise correct framework
implementation.

Similarly, QA must not simulate Inspector UX merely to create a certification
score.

## 11. IF-ADR-010A / 010B / 010C disposition

```text
IF-ADR-010A — Product Surface Standard
  CLOSED

IF-ADR-010B — Current Package Surface Audit
  CLOSED

IF-ADR-010C — Canonical Editor Product-Surface QA
  CANCELLED / NOT REQUIRED
```

The attempted synthetic UX-smoke direction is explicitly retired.

If a system later needs technical Editor QA, that test belongs to the system's
technical QA because of a real invariant, not because ADR-010 requires UX
certification.

## 12. Consequences

Positive:

```text
manual explicit authoring remains first-class
tooling quantity is not a maturity metric
package surfaces may use different legitimate lifecycle shapes
runtime authority remains explicit
technical evidence remains inspectable
future tooling must be justified by concrete friction
```

Tradeoff:

```text
semantic consistency is more important than identical Inspector layouts
product evaluation requires judgment rather than a synthetic UX pass/fail suite
```

## 13. Completion

The normative ADR is complete.

The current package audit is also complete.

No further ADR-010 implementation or QA program is required now.

Future product work starts only from:

```text
a concrete package gap
or
a real FIRSTGAME consumer friction
or
an independently justified technical Editor invariant
```

FIRSTGAME is not a closure dependency.

## 14. Normative summary

```text
Manual explicit authoring is the default.

The framework should:
  present
  organize
  explain
  validate
  diagnose

The framework may automate technical materialization
only when derivation is deterministic and does not invent user intent.

Normal Inspector shows product intent and actionable state.
Advanced / Debug shows technical evidence.
Runtime remains runtime authority.

Additional authoring layers exist only when the lifecycle justifies them.

QA proves technical contracts.
FIRSTGAME reveals real consumer UX friction.

ADR-010 compliance is not measured by tooling or automation quantity.
Synthetic UX QA is not required.
```
