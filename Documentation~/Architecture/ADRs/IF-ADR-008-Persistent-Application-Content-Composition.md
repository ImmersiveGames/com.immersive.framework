# IF-ADR-008 — Persistent Application Content Composition

Status: **Accepted**  
Last updated: 2026-08-16  
Package implementation: **COMPLETE FOR CURRENT ACCEPTED PRODUCT MODEL**  
Current package assessment: **30/30** — local package/product assessment; not release certification  
Product lifecycle: **Class B — reusable Scene Template with source-scene-owned composition**  
Related decisions: IF-ADR-002, IF-ADR-006, IF-ADR-010, IF-ADR-015  
Current package baseline at last reconciliation: `baa5b00a004e81aec6f0080395cc2b8621d3d22c`  
Reconciliation record: [ADR-008 Reconciliation — 2026-08-10](../Reconciliation/IMMERSIVE-FRAMEWORK-ADR-008-RECONCILIATION-2026-08-10.md)

> This revision supersedes the former Recipe/Composer + Apply/Rebuild description.
>
> The current official product is a Scene Template whose physical source scene
> owns the composition. The package pipeline verifies the instantiated result and
> does not silently materialize or repair consumer content.

## Context

Application-persistent content hosts cross-Route/session integration and
presentation components.

The framework needs a reusable, discoverable product surface without making the
persistent container a global runtime authority and without silently rewriting
consumer scenes.

The first official template deliberately establishes a **minimal baseline** rather
than pre-authoring every optional persistent presentation concern.

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

The official current template is intentionally minimal and provides the reusable
application-persistent baseline for:

```text
Persistent Camera / Camera Output
Session Camera target and rig structure
EventSystem + InputSystemUIInputModule
```

The canonical Camera Output ID is:

```text
camera.output.main
```

The current baseline does **not** require or bundle persistent visual surfaces for:

```text
Pause
Loading
Transition
Global Canvas
```

Those remain optional product composition.

The pipeline explicitly follows:

```text
source scene owns the composition
pipeline performs non-mutating verification
pipeline never creates, repairs, saves or assigns consumer assets
```

After instantiation, the package validates the instantiated scene and reports the
result.

## Template family direction

The minimal template is the first supported member of the Persistent Content Scene
Template product surface. It is not intended to be the only useful authoring
configuration forever.

Future product cuts may add dedicated template variants for reusable persistent
modules such as:

```text
Pause presentation
Loading presentation
Transition presentation
combined persistent presentation compositions
other reusable persistent framework modules backed by concrete product need
```

The exact variant names, combinations and implementation order are intentionally
**not frozen** by this ADR.

What is frozen is the composition authority model:

```text
Template variant
  Editor creation convenience

Concrete .unity scene
  consumer-owned runtime composition

Optional module
  remains optional unless another explicit contract requires it

Pipeline
  verifies the contracts authored by the selected variant
  never silently materializes or repairs consumer content
```

A future more complete template does not supersede the minimal template by
default. Consumers should be able to select the smallest persistent composition
that matches their game.

Adding Pause, Loading or Transition to a future template is therefore a new
Editor/product-surface cut, not a reopening of runtime authority and not evidence
that those modules were missing from the current minimal baseline.

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

For template variants, validation should be scoped to the contracts actually
owned by that variant. A minimal template must not fail because a future optional
presentation module is absent.

## Runtime integration evidence

The current minimal template has been instantiated into a concrete consumer scene
and exercised in Play Mode.

Observed integration evidence showed successful framework boot and
application-persistent materialization of the Persistent Camera structure and
EventSystem. In the current implementation those objects were observed under
Unity's `DontDestroyOnLoad` scene.

This observation confirms the current runtime integration path. `DontDestroyOnLoad`
itself is not the authoring authority described by this ADR; the architectural
contract is application-persistent lifetime with scoped runtime authorities.

## Product surface status

Current package classification:

```text
Lifecycle                 Class B — reusable Template
Official baseline         minimal Camera + EventSystem composition
Official product surface  COMPLIANT AT PACKAGE LEVEL
Source composition owner  physical template source scene
Consumer scene owner      consumer
Automatic repair          NO
Automatic save            NO
Automatic assignment      NO
Runtime global authority  NO
```

Planned template variants are future convenience/product work and do not reduce
completion of the accepted minimal baseline.

## QA

The obsolete QA target was:

```text
prove Apply/Rebuild idempotency and preservation
```

That is not the current product contract because there is no Persistent Content
Apply/Rebuild materializer to certify.

Future QA should only be added for actual deterministic technical contracts of
the Scene Template pipeline or a concrete template variant, for example when a
regression risk is identified in:

```text
template verification
required reference validation
non-mutating behavior
explicit failure reporting
variant-specific required contracts
```

Do not invent materialization QA for a lifecycle that does not materialize.

## FIRSTGAME

FIRSTGAME can evaluate:

```text
is the template discoverable?
is the source/consumer ownership understandable?
is the required scene-reference flow clear?
are validation messages sufficient?
which optional template variants would materially improve real consumer authoring?
```

Those are consumer UX observations.

They are not technical completion gates for the current minimal template.

A real usability finding may justify a new template variant, documentation or
product-surface improvement without changing this composition authority model.

## Current assessment

The prior 90% estimate and later low evidence score were distorted by the stale
assumption that Persistent Content should be a Class C Composer/materialization
workflow.

Current package audit result:

```text
Package assessment 30 / 30
Product model       COMPLETE FOR CURRENT ACCEPTED SCOPE
```

No package implementation is justified by ADR-008 for the current minimal
baseline at this time.

## What remains

Current baseline closure and future product evolution must be kept distinct.

Current baseline may still receive evidence/usability work driven by a real need:

```text
usage documentation
technical QA for real Scene Template pipeline invariants
consumer UX observation
```

Future product work may add:

```text
Pause-oriented Persistent Content template variant
Loading-oriented Persistent Content template variant
Transition-oriented Persistent Content template variant
useful combined variants
```

Those are **planned future authoring surfaces, not current implementation gaps**.
Each should be implemented and validated as its own deliberate cut when its scope
is activated.

## Completion criteria

The accepted current model is complete when:

```text
the official minimal template is discoverable
the source scene owns authored composition
consumer-created scenes remain user-owned
verification is non-mutating
required invalid state is explicit
runtime authority remains scoped and typed
no silent repair or gameplay-intent invention occurs
optional modules are not promoted to baseline requirements
```

Future variants preserve these criteria and add only their explicitly owned
composition contracts.

## Normative summary

```text
Persistent Content uses reusable Scene Templates.

The current official baseline is intentionally minimal:
  Camera / Session Camera structure
  EventSystem

Source scene owns composition.
Consumer owns the instantiated scene.
Pipeline verifies; it does not materialize or repair.
Runtime systems keep runtime authority.

Pause, Loading, Transition and combined persistent presentation templates are
valid future product variants, not current baseline requirements.

Composer / Apply / Rebuild is not part of the current accepted model.
```
