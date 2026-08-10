# IF-ADR-010 — Editor and Inspector Product Surface Authority

Status: **Accepted**  
Last updated: 2026-08-09  
Normative classification: **Minimum product-surface standard accepted**  
Implementation classification: **Broad foundation exists; portfolio conformity audit remains**  
Historical implementation estimate: **70%**  
Current evidence-backed maturity: **55%** at the 2026-08-09 portfolio rebaseline  
Related decisions: IF-ADR-002, IF-ADR-008, IF-ADR-012, IF-ADR-015, IF-ADR-016  

Current review baseline:

```text
com.immersive.framework
  eb39c574e9ca04db0f88c4eb8e0eb704a1902194
  P0 — Serialized Player Migration Integrity

QAFramework
  b6a45728285ddb2ce08269fc1f88ae3f1a4235e4
  P0 — Serialized Player Migration Integrity
```

> ADR acceptance freezes the product-surface rules below. It does **not** claim
> that every existing framework feature already conforms to them.
>
> Portfolio conformity is evaluated separately. FIRSTGAME remains the
> real-consumer usability proof when a consumer cut is active.

---

## 1. Context

The Unity Editor and Inspector are the primary product surfaces through which
framework consumers discover, configure, understand and diagnose framework
features.

Technical correctness alone is insufficient. A feature can have correct runtime
contracts and still be difficult to use when its consumer-facing surface exposes
internal implementation details, hides invalid configuration, mixes authored
intent with runtime state, or requires knowledge of framework internals.

At the same time, product maturity must **not** be measured by the amount of
Editor automation provided.

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

---

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
Risk-Appropriate QA
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

are **conditional capabilities**, not universal requirements.

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

without any Wizard, Composer or Apply/Rebuild flow.

---

## 3. Product-surface principles

### 3.1 Designer edits intent

The normal product surface represents user-authored intent and configuration.

It should not require the user to understand internal runtime modules, private
composition contracts, hidden handles, revisions, occurrence IDs or generated
technical bindings in order to perform normal configuration.

### 3.2 Framework may derive technical facts

The framework may derive technical configuration when the derivation is
deterministic and contains no new gameplay decision.

Example:

```text
authored Supported Slots = 4
        ↓
derived technical player limit = 4
```

The derived technical value is materialization, not a second user-authored
authority.

### 3.3 Runtime remains runtime authority

Editor convenience must never weaken runtime architecture.

Runtime authority remains in the appropriate:

```text
Runtime Context
Session
scoped Service
typed binding
runtime module
specific adapter
```

with explicit lifetime and ownership.

Editor UX must not introduce:

```text
global manager
service locator
implicit static registry
FindObjectOfType authority
object-name authority
silent fallback
```

to make authoring appear easier.

### 3.4 No accidental gameplay from authoring

Authoring components and Editor tooling must not execute gameplay behavior by
accident.

Runtime commands exposed by an Inspector must be explicit, clearly identified as
runtime operations and restricted to an appropriate Play Mode context.

---

## 4. Minimum universal product-surface contract

The following requirements apply to recurrent consumer-facing framework features
when relevant to their lifecycle.

---

### 4.1 Official Path

A feature must have a clear official path through which a consumer can find and
configure it.

The path may be:

```text
Add Component
Assets > Create
GameObject menu
Project Settings
existing asset Inspector
existing authoring component
documented scene/prefab composition path
```

A dedicated Create menu is **not** required.

The requirement is discoverability and ownership, not menu count.

A consumer should not need to know internal class names, namespaces or technical
containers to discover the intended product surface.

---

### 4.2 Intent First

The normal Inspector must communicate what the feature is responsible for before
technical implementation details dominate the surface.

Preferred semantic structure, only where sections contain real information:

```text
Intent / purpose
Configuration
Configuration Status / Validation
Runtime Status
Explicit Actions
Advanced / Debug
```

This is a semantic vocabulary, not a mandatory visual template.

Existing shared Editor presentation helpers should be reused when appropriate
rather than introducing parallel Inspector grammars.

---

### 4.3 Configuration Status

The product surface must make required invalid or incomplete configuration
explicitly identifiable.

Required invalid configuration must not silently fall back to another behavior.

A consumer should be able to distinguish at least:

```text
authored configuration
configuration validity
effective runtime state
last Editor operation
```

when those concepts exist for the feature.

They must not be collapsed into one ambiguous status message.

---

### 4.4 Actionable Diagnostics

Diagnostics should identify the actual problem and, when possible, the relevant
object, asset, component or field.

Avoid generic messages such as:

```text
Invalid configuration.
Something is missing.
Failed.
```

Prefer specific diagnostics such as:

```text
Player Session requires at least one Supported Slot.
```

Diagnostics may also provide a corrective hint, but a hint must not silently make
a gameplay decision for the user.

The existing framework validation infrastructure remains the canonical validation
authority where applicable. ADR-010 does not create a parallel validation system.

---

### 4.5 Safe Explicit Remediation

Remediation is optional.

When provided, remediation must be:

```text
explicit
predictable
Undo-aware when it writes Editor state
non-destructive
diagnostic
limited to safe deterministic corrections
```

Good examples include:

```text
Ping Required Asset
Select Existing Binding
Open Relevant Settings
Add Deterministic Required Binding
Rebuild Framework-Owned Technical Materialization
```

Avoid broad actions such as:

```text
Fix Everything
Create Complete Setup
Auto Configure Gameplay
```

when multiple intent choices are involved.

The framework must not choose gameplay intent merely because it can infer a
possible configuration.

---

### 4.6 Advanced / Debug

Technical evidence should be available without dominating normal authoring.

Advanced / Debug may expose:

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
last operation/result
runtime diagnostics
```

Technical components may be removed from the primary flow, but they must not
become impossible to inspect.

Advanced / Debug is a disclosure mechanism, not a different authority.

---

### 4.7 Editor Write Safety

Editor operations that modify assets, scenes, prefabs or components must be safe
for the lifecycle they support.

Where applicable, this includes:

```text
Undo / Redo
Prefab Stage semantics
asset dirty/save semantics
multi-object editing
asset duplication
domain reload
```

A feature is not required to support every Unity Editor interaction.

If an operation is not safe for multi-object editing or another context, the
Editor must disable or reject it explicitly rather than partially applying a
destructive operation.

---

### 4.8 Runtime Discipline

In Play Mode, the Inspector should primarily present:

```text
effective runtime state
read-only evidence
diagnostics
explicit supported runtime commands
```

Runtime state should be read-only by default.

Any runtime command exposed to the user must be:

```text
explicit
typed through the official runtime surface
scoped correctly
diagnostic on failure
```

Editor-only configuration must not become an implicit runtime authority.

---

### 4.9 QA Contract

Product-surface QA is proportional to actual risk.

A simple direct Inspector does not need the same QA matrix as a materializing
Composer.

Applicable QA may include:

```text
validation behavior
Undo / Redo
Prefab Stage
multi-object behavior
asset duplication
domain reload
idempotent Apply / Rebuild
preservation of user-owned content
runtime read-only presentation
```

Only risks that exist for the feature should become mandatory test obligations.

QA proves technical/editor contracts.

FIRSTGAME proves whether the feature is understandable and usable in a real game.

---

## 5. Conditional authoring capabilities

The following capabilities are available to framework features, but none is
required by ADR-010 by default.

---

### 5.1 Recipe / Profile / Template

Use a Recipe, Profile or Template when there is a real need for reusable authored
intent, such as:

```text
shared configuration used by multiple instances
configuration with meaningful standalone identity
reusable product intent
configuration complexity that benefits from asset ownership
```

Do not create a Profile merely to satisfy an architectural diagram.

A direct component with a clear Inspector is valid when reuse does not justify a
separate asset.

---

### 5.2 Composer / Authoring Component

Use a Composer or higher-level Authoring Component when an authored instance must
coordinate several lower-level technical contracts and manual composition would
be repetitive, error-prone or require internal framework knowledge.

A Composer represents:

```text
concrete authored instance
+
product intent
```

It is not runtime gameplay authority.

A Composer must not be introduced merely because another framework system uses
one.

---

### 5.3 Apply / Rebuild

Apply/Rebuild is justified only when there is a real distinction between:

```text
authored intent
```

and:

```text
derived technical materialization
```

Examples include:

```text
derived bindings
managed technical child components
generated configuration
technical containers
derived package settings
```

If the feature has nothing to materialize, an Apply/Rebuild button is unnecessary
and should not be added.

---

### 5.4 Apply / Rebuild contract

When Apply/Rebuild exists, it must be:

```text
explicit
idempotent
deterministic
Undo-aware
non-destructive
safe to repeat
diagnostic
```

Repeated execution without an authored-intent change must converge on the same
technical state.

```text
Apply
Apply
Apply
```

must not continuously create duplicate technical materialization.

---

### 5.5 Materialization ownership

Rebuild may remove or replace only content the framework can prove it owns.

Ownership must not be inferred solely from:

```text
GameObject name
hierarchy position
generic component type
naming convention
```

Prefer explicit identity, marker or ownership contracts.

User-owned content must be preserved.

Technical containers such as:

```text
_Framework
_Bindings
_Runtime
```

may organize materialization, but they are not product intent and are never
runtime authority by name alone.

---

### 5.6 Materialization receipt

A significant materialization operation should expose enough evidence to explain
what happened.

Depending on risk, this may include:

```text
created
updated
repaired
already valid
preserved
removed
skipped
blocked
issues
```

Simple features do not need a persistent receipt subsystem.

The evidence should be proportional to the risk of the operation.

---

### 5.7 Create actions

A dedicated Create action is useful when it improves discovery or safely removes
repetitive technical setup.

It is justified when, for example:

```text
creation requires several assets or objects
there is a canonical initial structure
manual setup is repetitive but deterministic
the feature is recurrent and otherwise difficult to discover
```

If `Add Component`, `Assets > Create`, Project Settings or an existing Inspector
already provides a clear path, no additional Create tooling is required.

---

### 5.8 Wizard

Wizard is an exception, not the default framework authoring model.

A Wizard is justified only when several related initial decisions cannot be
presented clearly through a simpler creation flow and the Wizard measurably
reduces configuration error without hiding architecture.

A Wizard must not:

```text
invent gameplay intent
create hidden global authorities
silently choose configuration
hide technical materialization permanently
exist solely to improve ADR-010 compliance
```

---

## 6. Product-surface classes

For audit and implementation decisions, consumer-facing features are classified
by lifecycle rather than by a universal tooling checklist.

### Class A — Simple / Direct Authoring

Typical shape:

```text
component or asset
few authored fields
no meaningful technical materialization
```

Expected surface:

```text
official path
clear Inspector
validation
runtime evidence when applicable
Advanced / Debug when technical evidence exists
```

Normally unnecessary:

```text
Composer
Apply / Rebuild
Wizard
```

---

### Class B — Reusable Authored Intent

Typical shape:

```text
reusable Profile / Recipe / Template
+
one or more consumers
```

Use only when reuse or standalone authored identity is real.

A separate Composer is still conditional.

---

### Class C — Materialized Composition

Typical shape:

```text
authored intent
      ↓
validation
      ↓
explicit Apply / Rebuild
      ↓
framework-owned technical materialization
      ↓
runtime evidence
```

This class carries the strongest obligations for:

```text
idempotency
Undo
ownership
non-destructive rebuild
diagnostic result
Advanced / Debug visibility
```

Camera Rig authoring is the current package reference for this lifecycle class.

It is a reference for **materialization behavior**, not a template every feature
must imitate.

---

## 7. Architecture by need, not by checklist

No product layer should be introduced solely because it appears in the framework
authoring model.

The following are examples of over-authoring unless a concrete feature need
justifies them:

```text
Profile with no meaningful reuse
Composer wrapping one simple field
Apply button that materializes nothing
Wizard whose only job is Add Component
automatic setup that chooses gameplay intent
```

The IF-ADR-002 layered model remains valid **when appropriate**.

ADR-010 defines how to decide whether those layers are appropriate.

---

## 8. Current package evidence

The 2026-08-09 package audit found broad existing product-surface infrastructure.
The problem is primarily consistency and normative classification, not a
generalized absence of Editor tooling.

Current evidence snapshot:

| Area | ADR-010 interpretation |
|---|---|
| Shared Inspector GUI | FOUNDATION |
| Authoring Validation | FOUNDATION |
| Camera Rig Authoring | REFERENCE — Class C materialization |
| Route / Activity / Application | CONFORMANT / substantial |
| Player Participation / Provisioning | CONFORMANT / mature |
| Pause | CONFORMANT |
| Activity Restart | CONFORMANT |
| Reset | substantial surface; fine-grained audit still required |
| Unity Input Gate | PARTIAL — functional surface, canonical presentation normalization candidate |
| Global Settings | substantial surface |
| Persistent Content | REBASELINE REQUIRED against current scene-template lifecycle |
| Diagnostics | FOUNDATION / support surface |
| Camera technical bindings | INTERNAL / SUPPORT, not the primary Camera product surface |

This table is an evidence snapshot, not permanent normative classification.

The follow-up package audit owns the detailed feature-by-feature classification.

---

## 9. Current divergences discovered by rebaseline

### 9.1 Historical gap lists can be stale

Earlier planning documents may describe a feature as lacking a product surface
even after the package has acquired one.

Such lists are historical evidence, not authority over current package state.

Before creating a new Editor, Composer, Wizard or authoring layer:

```text
inspect the current package
identify the current official surface
classify the lifecycle
prove the concrete gap
```

Only then implement the smallest necessary correction.

---

### 9.2 Unity Input Gate

The current Input Gate surface already provides meaningful configuration,
runtime evidence, Advanced/Debug information and Editor safety.

Its current issue is a likely presentation/grammar divergence from the shared
product-surface vocabulary, not absence of an Editor.

Therefore the next implementation should not begin with "create an Input Gate
product surface."

It should first prove the exact normalization gap.

---

### 9.3 Persistent Content

Persistent Content must be evaluated against its current scene-template pipeline
rather than historical assumptions about Create menus, validation sessions or
other previous authoring shapes.

Current classification:

```text
REBASELINE REQUIRED
```

This is deliberately different from:

```text
MISSING
```

No new Inspector or Composer is justified until the real lifecycle is mapped.

---

## 10. Compliance classification

Future package audits classify each applicable product surface as:

### COMPLIANT

The surface satisfies all minimum requirements applicable to its lifecycle and
all conditional capabilities it actually uses respect their contracts.

### PARTIAL

The official product surface exists and is usable, but one or more applicable
ADR-010 requirements are incomplete or inconsistent.

### NON-COMPLIANT

The consumer must rely on internal framework knowledge, invalid required state is
silently tolerated, Editor operations violate safety/ownership rules, or the
surface materially contradicts this ADR.

### REBASELINE REQUIRED

The current lifecycle or product surface differs sufficiently from historical
documentation that implementation should not begin until the current shape is
mapped.

### NOT APPLICABLE

The item is an internal/support component or the evaluated requirement does not
apply to the feature lifecycle.

---

## 11. Audit questions

A conforming feature should allow the package audit to answer, where applicable:

```text
1. Where does the user discover or create it?
2. Where is authored intent configured?
3. What is user-owned intent?
4. What, if anything, is technically derived?
5. How is invalid configuration identified?
6. How is the problem explained?
7. Is remediation explicit and safe?
8. What changes in Play Mode?
9. Where is runtime evidence shown?
10. Where are technical details exposed?
11. Is there technical materialization?
12. If so, is Apply/Rebuild safe and idempotent?
13. Which content is framework-owned?
14. Which Editor lifecycle risks apply?
15. What QA proves those risks?
```

Failure to answer a question does not automatically imply missing tooling.

The lifecycle determines which questions are applicable.

---

## 12. Non-goals

ADR-010 does not require:

```text
automatic gameplay setup
Wizard-first workflows
Composer for every feature
Profile for every feature
Apply/Rebuild for every feature
hidden technical components
automatic remediation of authored intent
global Editor or runtime managers
a second validation architecture
a dedicated Create menu for every capability
```

ADR-010 also does not promote Experimental or Preview APIs to Stable.

Editor polish and API stability are separate concerns.

---

## 13. Relationship to QA and FIRSTGAME

The package owns the official product surface.

QAFramework proves technical and Editor contracts.

FIRSTGAME proves real-game usability and can reveal product friction.

The correct sequence for a product-surface finding is:

```text
identify current package surface
        ↓
classify concrete gap
        ↓
define smallest package correction
        ↓
prove relevant technical/editor contract in QA
        ↓
prove real usability in FIRSTGAME when applicable
```

FIRSTGAME must not become the permanent owner of framework authoring contracts.

QA must not become the primary user-facing authoring experience.

---

## 14. Consequences

### Positive

This decision:

```text
makes manual explicit authoring a valid first-class product path
prevents tooling quantity from becoming a maturity metric
separates universal UX requirements from lifecycle-specific tooling
reduces speculative Wizards/Composers
keeps runtime authority explicit
creates an objective package-audit vocabulary
preserves technical inspectability
supports incremental normalization of existing Editors
```

### Tradeoffs

The framework must tolerate multiple legitimate authoring shapes.

Consistency is therefore semantic rather than identical visual structure.

Audits require lifecycle classification before determining which conditional
requirements apply.

Some product improvements will remain intentionally manual until real consumer
evidence justifies additional tooling.

---

## 15. Acceptance

IF-ADR-010 is Accepted because the normative decision is now sufficiently
specific to determine:

```text
what every applicable product surface must provide
what remains conditional
when Profile / Recipe is justified
when Composer is justified
when Apply / Rebuild is justified
when Create actions are useful
when Wizard is justified
what belongs in normal Inspector
what belongs in Advanced / Debug
what remediation may and may not do
how runtime authority remains separated
how future package conformity is classified
```

Acceptance does not mean portfolio completion.

Current implementation remains partial and must be measured by the package
conformity audit.

---

## 16. Next work

### IF-ADR-010B — Package Product Surface Audit

Next, audit the actual package against this accepted standard.

For each current feature:

```text
identify official path
classify lifecycle A / B / C
evaluate applicable minimum requirements
evaluate conditional capabilities already present
classify:
  COMPLIANT
  PARTIAL
  NON-COMPLIANT
  REBASELINE REQUIRED
  NOT APPLICABLE
identify the smallest concrete gap
```

The audit must inspect at least:

```text
Player
Activity
Route
Application
Camera
Persistent Content
Loading / Readiness
Pause
Input Gate
Reset / Restart
Global Settings
shared Editor infrastructure
```

No implementation should be justified solely by a historical gap list.

### After the audit

Implement only the first proven, highest-value concrete product-surface gap.

Then create canonical Editor QA for the lifecycle risks actually present across
the selected surfaces.

---

## 17. Completion criteria

The ADR decision itself is complete when this standard is accepted.

Portfolio implementation is complete only when:

```text
recurrent consumer-facing features have a clear official path
applicable normal Inspectors are intent-first
required invalid state is explicit and diagnostic
Advanced / Debug exposes relevant technical evidence
Editor writes respect applicable safety contracts
materialization is explicit, owned, idempotent and non-destructive where used
runtime authority remains scoped and typed
QA covers applicable Editor risks
real-consumer usability is proven where product closure requires it
```

No feature is considered incomplete merely because it lacks a Wizard, Composer,
Profile, Create menu or Apply/Rebuild.

---

## 18. Normative summary

```text
Manual explicit authoring is the default.

The framework should:
  present
  organize
  explain
  validate
  diagnose

The framework may automate technical materialization
only when the derivation is deterministic and does not invent user intent.

Normal Inspector shows product intent and actionable state.

Advanced / Debug shows technical evidence.

Runtime remains runtime authority.

Additional authoring layers exist only when the feature lifecycle justifies them.

ADR-010 compliance is not measured by the amount of tooling or automation.
```
