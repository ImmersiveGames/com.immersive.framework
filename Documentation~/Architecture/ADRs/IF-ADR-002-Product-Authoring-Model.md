# IF-ADR-002 — Product Authoring Model

Status: **Accepted**  
Last updated: 2026-08-09  
Implementation classification: **Mature package model; no cross-cutting authoring implementation gap currently identified**  
Current package assessment: **29/30** — local planning assessment from the 2026-08-09 package audit; not release certification  
Related decisions: IF-ADR-008, IF-ADR-010, IF-ADR-012, IF-ADR-015, IF-ADR-016  
Current package baseline: `43b96a4b100b8273da1190520536007ba82dc081` (`ADR-010B`)

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

## QA and FIRSTGAME

QA proves deterministic technical/editor contracts when a real contract exists.

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

FIRSTGAME is not required for technical framework completion and is not part of
the technical completion score.

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
Synthetic UX QA requirement CANCELLED
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
