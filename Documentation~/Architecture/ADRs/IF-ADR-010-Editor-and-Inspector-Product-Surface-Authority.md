# IF-ADR-010 — Editor and Inspector Product Surface Authority

Status: **Accepted / Reconciled for Camera ADR-022 2026-08-15**  
Last updated: **2026-08-15**  
Normative classification: **Minimum product-surface standard accepted**  
Package conformity audit: **CLOSED — IF-ADR-010B**  
Implementation classification: **Broad package surface is semantically conformant; Camera Class C materialization is implemented and technically certified**  
IF-ADR-010C: **CANCELLED / NOT REQUIRED**  
Related decisions: IF-ADR-002, IF-ADR-004, IF-ADR-008, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-022

> ADR-010 defines the product-surface rules.
> It does not require equal tooling depth for every feature.
> IF-ADR-022 is now a concrete Class C example of deterministic,
> ownership-safe Editor materialization.

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

Additional authoring layers are justified only when they solve concrete
lifecycle or materialization complexity.

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

Derived technical state is materialization, not a second authored authority.

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

Consumers should be able to distinguish, where the concepts exist:

```text
authored configuration
configuration validity
effective runtime state
last Editor/runtime operation
```

### 4.4 Actionable Diagnostics

Diagnostics should identify the actual problem and relevant context.

The existing framework validation infrastructure remains canonical where
applicable. ADR-010 does not create a second validation architecture.

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

Applicable risks include:

```text
Undo / Redo
Prefab Stage
dirty/save semantics
multi-object editing
asset duplication
domain reload
ownership preservation
partial-mutation prevention
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

Legitimate technical/editor contracts include:

```text
validation behavior
idempotent technical materialization
Undo/Redo for an Editor writer
Prefab Stage safety
ownership preservation
deterministic asset creation
runtime command gating
transactional no-partial-mutation behavior
```

A simple direct Inspector does not need a synthetic rendering/UX test.

Do not use QAFramework to certify that an Inspector is visually attractive or
subjectively understandable.

## 5. Conditional authoring capabilities

### 5.1 Recipe / Profile / Template

Use when reusable intent or standalone authored identity is real.

Do not create an asset layer merely because the architecture allows one.

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
Undo-aware where supported
non-destructive
ownership-safe
safe to repeat
diagnostic
```

### 5.5 Materialization ownership

Rebuild may remove or replace only content the framework can prove it owns.

Ownership must not be inferred solely from:

```text
object name
hierarchy location
generic component type
visual similarity to generated content
```

User-owned or unknown content must be preserved.

Compatible pre-existing content may be consumed only under the owning feature's
explicit contract; compatibility does not by itself establish Framework
ownership.

### 5.6 Materialization receipt / provenance

Significant materialization should expose enough evidence to explain:

```text
what was selected
what was created
what was reused
what is Framework-owned
what is external/unknown
what was blocked
what revision/result was committed
```

Evidence should be proportional to the risk.

### 5.7 Create actions

Create actions are valid when they improve discovery or remove repetitive,
deterministic setup.

They must not choose gameplay intent.

### 5.8 Wizard

Wizard is exceptional.

It must solve proven multi-step authoring complexity rather than act as a
maturity badge.

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

Class C carries stronger contracts around:

```text
idempotency
ownership
non-destructive writes
preflight before mutation
diagnostics
Editor safety
```

`CameraRigComposer` under IF-ADR-022 is the current canonical Class C example.

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

IF-ADR-010B remains closed.

Current package classification:

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
| Camera request/output bindings | SUPPORT / NOT PRIMARY RIG PRODUCT SURFACE |
| CameraRigComposer | CLASS C / COMPLIANT / TECHNICALLY CERTIFIED |

The original package audit found no generalized missing-tooling program.

IF-ADR-022 is an independently justified Camera-specific Class C expansion, not
a reversal of IF-ADR-010B.

## 9. Camera reconciliation — IF-ADR-022

The previous Camera note that Camera was only a future Class C reference is now
superseded by an implemented concrete surface.

### 9.1 Designer-first Presentation

`CameraRigComposer` exposes:

```text
Presentation
  Fixed
  Follow
  Mounted
  Third Person
```

The normal Inspector shows model-relevant fields rather than raw generic
Cinemachine component-type selection.

### 9.2 Model-specific authoring

```text
Fixed
  authored pose
  optional/required Look At

Follow
  Tracking target
  configurable Look At
  Follow Offset

Mounted
  Camera Mount / Tracking target
  Position Damping
  Rotation Damping

Third Person
  Tracking Pivot
  Shoulder Offset
  Vertical Arm Length
  Camera Side
  Camera Distance
  Damping
```

Nonsensical target requirements are not presented as normal authoring.

### 9.3 Safe Apply / Rebuild

The Camera Apply/Rebuild pipeline is:

```text
resolve typed targets
  -> validate selected model
  -> preflight Position + Rotation stages
  -> block before mutation on unknown conflict
  -> reconcile only Framework-owned technical controls
  -> configure selected model
  -> commit materialization evidence
```

The preflight-before-mutation rule prevents a model switch from removing one
owned stage and then discovering an external conflict in another stage.

### 9.4 Exact-reference ownership evidence

Camera materialization records:

```text
materialized Presentation
Framework-owned CinemachineCamera
Framework-owned Position Control
Framework-owned Rotation Control
materialization revision
last result / blocking issue
```

Only exact previously recorded references prove Framework ownership.

Pre-existing compatible components are `ExternalOrUnknown` unless explicit
provenance already exists.

### 9.5 Advanced / Diagnostics

The Camera Inspector exposes the technical Body/Aim pipeline and provenance in
Advanced/Diagnostics while preserving product-intent-first normal authoring.

This is the intended ADR-010 separation:

```text
normal Inspector
  product intent

Advanced / Diagnostics
  technical evidence
```

## 10. Camera technical QA under ADR-010 rules

ADR-010 still does **not** require synthetic Inspector UX certification.

The IF-ADR-022 QA exists because Camera Class C materialization has deterministic
technical risks:

```text
serialized compatibility
materialization completeness
model switching
idempotence
ownership preservation
external conflict protection
transactional no-partial-mutation
no output-authority mutation
no silent fallback
```

Those contracts passed:

```text
ADR-022 Presentation Models  14/14
Full Camera QA                53/53
```

This is technical QA for Camera's own invariants, not a visual UX score.

## 11. QA and FIRSTGAME boundary

The package owns the official product surface.

QAFramework proves deterministic technical/editor/runtime contracts.

FIRSTGAME evaluates real consumer ergonomics and integration.

```text
Technical Status
  contracts + runtime + relevant QA

Product Surface Status
  package authoring/diagnostic surface

Consumer UX Evidence
  real-game observation in FIRSTGAME
```

The Camera package surface and its technical materialization are complete for
IF-ADR-022 C1-C5.

FIRSTGAME C6 remains real-consumer proof and does not demote the technical
certification.

## 12. IF-ADR-010A / 010B / 010C disposition

```text
IF-ADR-010A — Product Surface Standard
  CLOSED

IF-ADR-010B — Current Package Surface Audit
  CLOSED

IF-ADR-010C — Canonical Editor Product-Surface QA
  CANCELLED / NOT REQUIRED
```

If a system needs technical Editor QA, that test belongs to the system's own
technical contract.

IF-ADR-022 follows that rule.

## 13. Consequences

Positive:

```text
manual explicit authoring remains first-class
tooling quantity is not a maturity metric
Camera gains justified Class C materialization
runtime authority remains explicit
technical evidence remains inspectable
ownership-safe rebuild is proven
future tooling still requires concrete justification
```

Tradeoff:

```text
semantic consistency is more important than identical Inspector layouts
real consumer ergonomics still requires FIRSTGAME observation
Class C features carry stronger technical QA obligations
```

## 14. Completion

The normative ADR remains complete.

The Camera-specific reconciliation is also complete for IF-ADR-022 C1-C5.

No generalized ADR-010 implementation program is reopened.

Future product work starts only from:

```text
a concrete package gap
or
a real FIRSTGAME consumer friction
or
an independently justified technical Editor invariant
```

## 15. Normative summary

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

Apply/Rebuild removes or replaces only proven Framework-owned state.
Unknown/external conflicts block rather than being silently destroyed.

Normal Inspector shows product intent and actionable state.
Advanced / Debug shows technical evidence.
Runtime remains runtime authority.

Additional authoring layers exist only when the lifecycle justifies them.

QA proves technical contracts.
FIRSTGAME reveals real consumer UX friction.

ADR-010 compliance is not measured by tooling or automation quantity.
Synthetic UX QA is not required.
```
