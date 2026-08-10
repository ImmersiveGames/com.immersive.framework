# IF-ADR-002 — Product Authoring Model

Status: **Accepted**  
Last updated: 2026-08-10  
Implementation classification: **Mature cross-cutting authoring model; current feature evidence confirms multiple valid authoring shapes with no generic tooling gap**  
Current package assessment: **29/30** — local planning assessment from the 2026-08-09 package audit; not release certification  
Related decisions: IF-ADR-001, IF-ADR-003–IF-ADR-016  
Current evidence source: `IF-TRACK-Framework.md` plus feature-specific QA/closure records; this normative ADR intentionally does not pin a mutable package SHA.

> This revision supersedes the older interpretation that recurrent framework
> features are expected to acquire Recipe/Profile + Composer + Apply/Rebuild +
> Wizard flows by default.
>
> The framework is a product, but product maturity is not measured by the amount
> of automation or by the number of authoring layers.

## Context

A technically correct collection of components, validators and smokes is not
sufficient as a framework product.

Consumers need a clear way to:

```text
discover the feature
understand its intent
configure it
identify invalid state
inspect effective runtime evidence
diagnose failures
```

However, solving those needs does not imply that every system requires a
separate Profile, Composer, Wizard or Apply/Rebuild pipeline.

Manual explicit authoring is a valid first-class product model.

## Decision

When the lifecycle genuinely requires them, framework systems may use the
following layers:

```text
Recipe / Profile / Template
  reusable authored intent

Composer / Authoring Component
  concrete authored instance or primary editing surface

Technical materialization
  explicit components, adapters and bindings derived from authored intent

Scoped Runtime Context / Session / Service
  runtime authority with explicit lifetime

Diagnostics
  validation, reports, logs, technical QA and Advanced / Debug evidence
```

These are architectural capabilities, not a universal checklist.

The default authoring direction is:

```text
manual
explicit
inspectable
diagnostic
```

A simple feature can be complete with:

```text
Add Component / select asset
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

## Authoring selection rules

### Direct authoring

Prefer direct/manual authoring when:

```text
the feature has few authored decisions
there is no meaningful reusable intent asset
there is no deterministic technical graph to materialize
manual configuration does not require private framework knowledge
```

Examples in the current package include Pause, Reset triggers, Activity Restart,
Readiness participants and other focused adapters/triggers.

### Recipe / Profile / Template

Use a reusable authored asset when there is real reusable intent or standalone
identity.

Examples include:

```text
PlayerSessionProfile
Activity / Route authored assets
framework settings/configuration assets
Persistent Content Scene Template
```

A separate Composer is still conditional.

### Composer

Use a Composer only when one concrete authored instance coordinates several
lower-level technical contracts and direct manual composition would be
repetitive, error-prone or dependent on internal framework knowledge.

A Composer is an authoring authority for its authored instance. It is not
gameplay runtime authority.

### Apply / Rebuild

Use Apply/Rebuild only when there is an actual split between:

```text
authored intent
```

and:

```text
derived technical materialization
```

If there is nothing meaningful to materialize, an Apply button is unnecessary.

When Apply/Rebuild exists, it must be:

```text
explicit
idempotent
deterministic
Undo-aware when Editor state is written
non-destructive
ownership-safe
diagnostic
safe to repeat
```

### Wizard

Wizard is exceptional.

It is justified only when multiple related initial decisions cannot be presented
clearly through a simpler surface and the Wizard demonstrably reduces error
without hiding architecture or inventing gameplay intent.

## Product-surface contract

The canonical minimum product-surface rules are owned by IF-ADR-010:

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

ADR-002 defines the available authoring shapes.

ADR-010 defines when those shapes are appropriate and what a usable Editor
surface must provide.

## Runtime authority

Editor convenience does not change runtime authority.

Runtime behavior belongs to the appropriate:

```text
Runtime Context
Session
scoped Service
runtime module
typed adapter
specific runtime component
```

Do not introduce:

```text
implicit global managers
service locators
object-name authority
hierarchy-name authority
silent fallback
runtime reflection as convenience glue
```

to make authoring appear easier.

## Current package evidence

The current package demonstrates several legitimate product shapes:

```text
Direct / manual authoring
  Pause
  Reset
  Activity Restart
  Unity Input Gate
  Activity / Route triggers
  Readiness participants

Reusable authored intent
  PlayerSessionProfile
  Activity / Route assets
  settings/configuration assets

Reusable Template
  Persistent Content Scene Template

Materialized composition
  Camera Rig
```

This diversity is intentional.

Camera is a Class C materialization example, not a template that other systems
must imitate.

Persistent Content proves that a Scene Template with non-mutating verification is
also a valid product model.

Activity Local Visibility now provides a closed example of focused direct
authoring: one explicit adapter surface, required/optional semantics, actionable
diagnostics and occurrence-scoped runtime behavior, with no Recipe, Composer,
Wizard or Apply/Rebuild layer required.

Optional BGM provides another direct-authoring example: Route/Activity authored
intent is configured directly while provider execution evidence remains a
technical/runtime concern. The accepted Audio QA certification does not justify
adding a generic BGM authoring layer.

## Cross-ADR relationship map — 2026-08-10

ADR-002 is a cross-cutting authoring decision. It does not own the runtime
contract of every feature, but it constrains how those contracts should be
exposed to consumers.

```text
IF-ADR-001
  runtime authority remains scoped; authoring convenience cannot create global authority

IF-ADR-003 / 012 / 015 / 016
  Player participation, provisioning and session profiles demonstrate direct authoring
  plus reusable Profile intent where reuse is real

IF-ADR-004
  Camera demonstrates justified technical materialization rather than a universal pattern

IF-ADR-005
  Pause / Input / Reset demonstrates small direct trigger/component authoring

IF-ADR-006 / 007 / 011
  lifecycle, diagnostics and readiness behavior remain runtime contracts; authoring
  surfaces expose intent without becoming transition/readiness authority

IF-ADR-008
  Persistent Content demonstrates Template-based reusable composition

IF-ADR-009
  Activity Local Visibility demonstrates direct component authoring with explicit
  required/optional semantics and no additional authoring layer

IF-ADR-010
  owns the minimum Editor/Inspector product-surface contract used to judge every
  authoring shape selected by ADR-002

IF-ADR-013
  Optional BGM demonstrates direct Route/Activity intent plus technical execution evidence

IF-ADR-014
  authored identity remains exact typed-definition authority; authoring convenience
  cannot replace definition/occurrence ownership with display names or stable IDs alone
```

These relationships are intentionally not a requirement for reciprocal links in
every feature ADR. ADR-002 is the governing authoring model; feature ADRs remain
authoritative for their own runtime behavior.

## QA and FIRSTGAME

QA proves deterministic technical/editor contracts when a real contract exists.

ADR-002 itself does not require a separate generic QA suite. Objective QA evidence
belongs to the feature ADR that owns the contract. The tracker therefore treats
generic Technical QA for ADR-002 as not applicable rather than subtracting points
for the deliberate absence of synthetic UX tests.

QA does **not** synthetically certify that an Inspector is understandable.

Do not create UX smokes that call `OnInspectorGUI()` merely to prove product
presentation.

FIRSTGAME is a separate real-consumer UX observation surface.

Its role is:

```text
discover real authoring friction
observe confusing configuration
observe repetitive manual work
observe missing explanation or diagnostics
```

FIRSTGAME is not a generic completion gate for ADR-002 and is not scored as a
separate cross-cutting requirement. Consumer observations remain valuable
feature-specific evidence and can justify a later authoring improvement.

A future FIRSTGAME observation may justify the smallest product-surface
improvement, but absence of that observation does not make a technically correct
framework feature incomplete.

## Current assessment

The previous 65% implementation estimate was based on an obsolete assumption that
more systems should acquire Composer/Wizard/Apply flows and that consumer proof
was part of the same completion percentage.

That model is retired.

Current package assessment:

```text
Package authoring model      MATURE
Cross-cutting tooling gap    NOT IDENTIFIED
Generic Composer gap         NO
Generic Wizard gap           NO
Generic Apply/Rebuild gap    NO
ADR-010 standard             ACCEPTED
ADR-010 package audit        CLOSED
ADR-009 direct authoring     CLOSED / QA CERTIFIED
ADR-013 direct BGM authoring IMPLEMENTED / QA CERTIFIED
Synthetic UX QA requirement CANCELLED
Generic FIRSTGAME gate       NOT APPLICABLE
```

Local planning assessment from the current package audit:

```text
29 / 30
```

The remaining point is not a mandate for more tooling. Future improvement must be
driven by a concrete system need or real consumer friction.

## What remains

No cross-cutting implementation cut is currently justified by ADR-002.

Future work is system-specific:

```text
inspect the current official surface
classify the real lifecycle
identify a concrete gap
implement only the smallest justified correction
```

Do not create a new authoring layer merely to increase an ADR score.

## Completion criteria

ADR-002 is satisfied when the framework:

```text
offers an understandable official authoring path
keeps user intent explicit
keeps runtime authority scoped
makes required invalid state explicit
keeps technical evidence inspectable
uses reusable intent only when reuse is real
uses materialization only when derivation is real
does not invent gameplay intent through convenience tooling
```

FIRSTGAME consumer UX evidence is tracked separately.

## Normative summary

```text
Manual explicit authoring is the default.

Recipe / Profile / Template is conditional.
Composer is conditional.
Apply / Rebuild is conditional.
Wizard is exceptional.

Designer edits intent.
Framework may derive deterministic technical facts.
Runtime remains runtime authority.
QA proves technical contracts.
FIRSTGAME reveals real consumer UX friction.

Product maturity is not measured by tooling quantity.
```
