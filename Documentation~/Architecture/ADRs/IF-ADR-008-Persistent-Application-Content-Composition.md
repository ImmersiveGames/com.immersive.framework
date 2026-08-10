# IF-ADR-008 — Persistent Application Content Composition

Status: **Accepted**  
Last updated: 2026-08-10  
Package implementation: **COMPLETE FOR CURRENT ACCEPTED PRODUCT MODEL**  
Current package assessment: **30/30** — local package/product assessment; not release certification  
Product lifecycle: **Class B — reusable Scene Template with source-scene-owned composition**  
Related decisions: IF-ADR-002, IF-ADR-006, IF-ADR-010, IF-ADR-015  
Current package baseline: `baa5b00a004e81aec6f0080395cc2b8621d3d22c`  
Reconciliation record: [ADR-008 Reconciliation — 2026-08-10](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-008-RECONCILIATION-2026-08-10.md)

> This revision supersedes the former Recipe/Composer + Apply/Rebuild description.
>
> The current official product is a Scene Template whose physical source scene
> owns the composition. The package pipeline verifies the instantiated result and
> does not silently materialize or repair consumer content.

## Context

Application-persistent content hosts cross-Route/session presentation and
integration components such as persistent Camera, Transition and Loading
composition.

The framework needs a reusable, discoverable product surface without making the
persistent container a global runtime authority and without silently rewriting
consumer scenes.

## Decision

Persistent Application Content uses the following authoring model:

```text
Physical Source Scene
        ↓
SceneTemplateAsset
        ↓
consumer explicitly creates a .unity scene
        ↓
Game Application references that scene
        ↓
package pipeline performs non-mutating verification
```

The source scene owns the authored composition.

The pipeline does **not** own a generated technical graph.

The pipeline must not silently:

```text
create consumer objects
repair consumer objects
assign consumer references
save consumer scenes
choose gameplay intent
```

Validation reports problems. It does not turn invalid authored state into another
configuration.

## Product authority

The Scene Template is reusable authored product intent.

The instantiated consumer scene is user-owned authored content.

The runtime authority remains in the appropriate runtime systems referenced by
that scene.

Neither the scene name nor a technical container name creates runtime authority.

## Current package implementation

Current package evidence:

```text
Editor/SceneTemplates/PersistentContent/
  ImmersivePersistentContent.scenetemplate
  PersistentContentTemplateSource.unity
  PersistentContentSceneTemplatePipeline.cs
```

The official template describes application-persistent Camera, Transition and
Loading composition.

The pipeline explicitly follows:

```text
source scene owns the composition
pipeline performs non-mutating verification
pipeline never creates, repairs, saves or assigns consumer assets
```

After instantiation, the package validates the instantiated scene and reports the
result.

## Why there is no Composer / Apply flow

The former ADR revision described:

```text
Recipe
Composer
managed technical slots
Apply / Rebuild
materialization receipts
```

That description no longer matches the official lifecycle.

Under IF-ADR-002 and IF-ADR-010, a Composer/Apply flow is justified only when
authored intent deterministically produces framework-owned technical
materialization.

Persistent Content currently does not use that lifecycle.

Therefore:

```text
missing Composer      NOT A GAP
missing Apply/Rebuild NOT A GAP
missing managed slots NOT A GAP
```

Adding them solely to match the historical ADR would be over-authoring.

## Validation contract

Verification must remain:

```text
explicit
non-mutating
diagnostic
safe for user-owned scene content
```

Required invalid state should be reported before Play Mode where feasible.

No silent fallback is allowed.

## Product surface status

Current package classification:

```text
Lifecycle                 Class B — reusable Template
Official product surface  COMPLIANT AT PACKAGE LEVEL
Source composition owner  physical template source scene
Consumer scene owner      consumer
Automatic repair          NO
Automatic save            NO
Automatic assignment      NO
Runtime global authority  NO
```

## QA

The obsolete QA target was:

```text
prove Apply/Rebuild idempotency and preservation
```

That is not the current product contract because there is no Persistent Content
Apply/Rebuild materializer to certify.

Future QA should only be added for actual deterministic technical contracts of
the Scene Template pipeline, for example if a concrete regression risk is
identified in:

```text
template verification
required reference validation
non-mutating behavior
explicit failure reporting
```

Do not invent materialization QA for a lifecycle that does not materialize.

## FIRSTGAME

FIRSTGAME can evaluate:

```text
is the template discoverable?
is the source/consumer ownership understandable?
is the required scene-reference flow clear?
are validation messages sufficient?
```

Those are consumer UX observations.

They are not technical completion gates.

A real usability finding may justify a small documentation or product-surface
improvement without changing this composition authority model.

## Current assessment

The prior 90% estimate and later low evidence score were distorted by the stale
assumption that Persistent Content should be a Class C Composer/materialization
workflow.

Current package audit result:

```text
Package assessment 30 / 30
Product model       COMPLETE FOR CURRENT ACCEPTED SCOPE
```

No package implementation is justified by ADR-008 at this time.

## What remains

Only evidence or usability work driven by a real need:

```text
short usage documentation when useful
sample/reference scene when it materially improves discovery
technical QA only for real Scene Template pipeline invariants
consumer UX observation in FIRSTGAME when that cut is active
```

None of these reopens the package composition model by default.

## Completion criteria

The accepted model is complete when:

```text
the official template is discoverable
the source scene owns authored composition
consumer-created scenes remain user-owned
verification is non-mutating
required invalid state is explicit
runtime authority remains scoped and typed
no silent repair or gameplay-intent invention occurs
```

## Normative summary

```text
Persistent Content is a reusable Scene Template.

Source scene owns composition.
Consumer owns the instantiated scene.
Pipeline verifies; it does not materialize or repair.
Runtime systems keep runtime authority.

Composer / Apply / Rebuild is not part of the current accepted model.
```
