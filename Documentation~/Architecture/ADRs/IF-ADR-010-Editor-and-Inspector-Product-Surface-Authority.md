# IF-ADR-010 — Editor and Inspector Product Surface Authority

Status: Proposed  
Last updated: 2026-07-30  
Supersedes: fragmented Editor and Inspector UX conventions  
Superseded by: none  
Related decisions: IF-ADR-002, IF-ADR-006, IF-ADR-009

## Context

The framework exposes product capabilities through Unity assets, authoring
components, adapters, bindings, triggers, composers and diagnostics.

These surfaces currently share architectural principles, but their Editor
behavior is not defined in one normative place. Without a common decision,
each new Custom Editor can make different choices about:

```text
which information appears first;
which fields are considered product intent;
when validation runs;
whether validation mutates data;
how Apply/Rebuild behaves;
where technical evidence is displayed;
how required and optional configuration is communicated;
how runtime status is presented;
how Undo and prefab overrides are preserved;
when a Custom Editor is justified.
```

The result can be technically correct but inconsistent authoring:

```text
permanent HelpBoxes competing with the actual configuration;
technical identifiers shown as the primary workflow;
validators executing during every Inspector repaint;
automatic repair or identity mutation;
adapters presented as runtime authorities;
Apply/Rebuild actions with unclear ownership;
runtime diagnostics mixed with serialized authoring;
different section order and status language for equivalent features.
```

IF-ADR-002 establishes the product authoring model:

```text
Profile
Recipe
Composer / Authoring
Apply / Rebuild
Runtime Context / Session / Module
Diagnostics / Validation
```

IF-ADR-006 separates typed results, structured facts and human diagnostics from
runtime authority.

IF-ADR-009 separates typed authored references, stable external identity,
human-readable names and runtime occurrences.

This ADR consolidates the expected behavior of Unity Editor product surfaces.

## Decision

Unity Editors and Inspectors are product surfaces.

Their primary responsibility is to let a designer or integrating developer
understand and author intent safely. They may expose technical evidence, but
they must not become a second runtime authority, a mutation path hidden behind
Inspector repaint, or a manual QA harness.

The framework adopts the following authority model:

```text
Serialized asset or component
  Owns authored intent.

Custom Editor / Inspector
  Presents, validates and explicitly materializes authored intent.

Runtime Context / Session / Module
  Owns mutable runtime state.

Advanced / Debug
  Presents technical evidence without becoming the normal workflow.

QAFramework
  Proves negative cases, regressions and contract permutations.
```

## Normative language

The terms below are normative:

```text
MUST
  Required for a compliant product surface.

SHOULD
  Expected by default. A deviation requires a documented reason.

MAY
  Optional behavior that must still preserve the other rules.
```

## 1. Product intent first

The normal Inspector MUST be organized by user intent, not by internal field
declaration order or runtime implementation structure.

The default section order SHOULD be:

```text
1. Primary Intent
2. Required Configuration
3. Optional Configuration
4. Product Actions
5. Validation Summary
6. Advanced / Debug
```

A surface MAY omit sections that do not apply.

The normal mode MUST answer:

```text
What does this asset or component represent?
What must the user select or configure?
What is optional?
What action can the user perform?
Is the authored configuration currently valid?
```

The normal mode MUST NOT require the user to understand:

```text
runtime ports;
registry internals;
transaction implementation;
ledger implementation;
normalized composite keys;
runtime handles;
internal module topology;
QA counters.
```

## 2. Designer-first normal mode

The normal Inspector MUST prioritize product vocabulary.

Examples:

```text
Target Route
Startup Activity
Player Slot
Default Actor
Camera Rig Recipe
Pause Surface
Release Policy
```

Internal vocabulary MAY appear only when it is itself the documented product
contract.

The normal Inspector SHOULD avoid:

```text
large permanent instructional HelpBoxes;
duplicated headings;
raw diagnostic dumps;
full validator reports;
internal enum names without product labels;
fields that are meaningful only to framework maintainers.
```

Short contextual guidance MAY appear when the user must make a decision that is
not self-evident. Guidance SHOULD be close to the relevant field and SHOULD not
dominate the Inspector.

Tooltips SHOULD explain the decision or consequence of a field, not merely
repeat its label.

## 3. Advanced / Debug

Every non-trivial framework surface SHOULD provide an `Advanced / Debug`
section when technical evidence exists.

The section MUST be collapsed or otherwise secondary by default.

It MAY contain:

```text
stable ID;
normalized ID;
asset path;
resolved authored definition;
runtime owner;
scope key;
binding state;
materialization state;
runtime occurrence or handle;
last typed result;
last diagnostic;
registry or ledger counts;
resolved Camera request;
output winner;
release evidence.
```

Advanced / Debug MUST remain evidence-only unless an action is explicitly
defined as a product or maintenance action.

It MUST NOT provide hidden command paths that bypass the normal runtime API.

Examples of valid explicit maintenance actions:

```text
Regenerate Stable ID...
Clear Cached Editor Preview
Open Referenced Asset
Select Materialized Root
```

Examples of invalid debug command paths:

```text
force runtime state to Ready;
mutate the active Route context directly;
inject a registry entry;
silently repair an invalid binding;
invoke internal transaction phases out of order.
```

## 4. Required, optional and conditional configuration

Editors MUST distinguish configuration semantics.

```text
Required and missing
  Blocking error.

Optional and absent
  Neutral or informational state, not an error.

Conditionally required and inactive
  Hidden or disabled only when the condition is clearly visible.

Conditionally required and active
  Blocking error when missing.
```

Color MUST NOT be the only indicator. Status MUST also use text, iconography or
another readable signal.

An optional reference MUST NOT be shown as a warning merely because it is
empty.

A required reference MUST NOT receive a silent fallback.

## 5. Typed references and stable identity

Normal Unity authoring MUST prefer typed object references for selecting
Profiles, Recipes, Routes, Activities and other authored definitions.

Stable IDs remain required where the domain contract requires them, but they
SHOULD be secondary in the normal workflow unless the user is explicitly
managing identity.

Editors MUST NOT silently regenerate or mutate stable identity through:

```text
Inspector repaint;
OnValidate;
asset import or reimport;
asset rename or move;
Play Mode entry;
automatic validation.
```

Identity-changing actions MUST be explicit and SHOULD require confirmation when
they may break external references.

Editors MUST distinguish:

```text
authored definition reference;
stable external identity;
display name;
runtime occurrence or handle.
```

These concepts MUST NOT be presented as interchangeable.

## 6. Validation

Validation MUST be non-mutating.

A validator MAY inspect authored data, reachable dependencies, scene state or
project state according to its documented scope, but it MUST NOT repair,
normalize, regenerate, add, remove or reorder authored content.

Every non-trivial product surface SHOULD provide an explicit `Validate` action.

Cheap local checks MAY update live when their cost and semantics are stable.
Heavy validation MUST NOT execute on every Inspector repaint.

Examples of heavy validation:

```text
project-wide AssetDatabase scans;
Build Profile audits;
cross-scene graph traversal;
catalog-wide identity collision scans;
runtime composition simulations.
```

Validation results SHOULD be presented as:

```text
Valid
Warning
Invalid
Not Validated
```

The summary SHOULD state the next corrective action in product language.

Full technical evidence SHOULD remain in Advanced / Debug or a dedicated report.

When serialized authoring changes, a previously cached validation result SHOULD
be marked stale or cleared.

Validation scope MUST be explicit:

```text
Selected Definition
Game Application Graph
Current Scene
Project Audit
```

A project-level issue unrelated to the selected object MUST NOT be presented as
if the selected object itself were locally invalid.

## 7. Explicit mutation and Undo

Editor actions that modify assets, scenes, prefabs or components MUST be
explicit.

All mutations MUST:

```text
participate in Unity Undo when supported;
mark the correct objects or scenes dirty;
preserve prefab override semantics;
avoid unrelated modifications;
produce a deterministic result;
report success or failure clearly.
```

Direct object writes from the Editor SHOULD use serialized properties for
normal fields.

Explicit structural operations MAY use direct APIs when necessary, but they
MUST record Undo and preserve ownership boundaries.

An Editor MUST NOT mutate authored content because the Inspector was opened,
repainted or focused.

Destructive actions SHOULD require confirmation and MUST describe what is
owned by the framework versus what is user-owned.

## 8. Apply / Rebuild

`Apply` and `Rebuild` are product actions for explicit technical
materialization.

They MUST be:

```text
idempotent;
deterministic;
safe to repeat;
Undo-compatible;
non-destructive outside documented ownership;
diagnostic;
available only in Edit Mode unless runtime use is explicitly designed.
```

The action MUST make ownership clear.

```text
Framework-owned materialization
  May be created, updated or removed by Apply/Rebuild.

User-owned content
  Must be preserved unless the user explicitly authorizes removal.
```

`Apply` SHOULD create or update missing materialization with the smallest
necessary change.

`Rebuild` MAY replace framework-owned materialization, but MUST NOT delete
unrelated user-authored children or components.

Editors MUST NOT automatically Apply/Rebuild on every property change unless a
separate accepted decision explicitly authorizes that workflow.

After Apply/Rebuild, the Inspector SHOULD expose:

```text
what was created;
what was updated;
what was preserved;
what was rejected;
where the materialized content is located.
```

## 9. Profiles and Recipes

Profiles and Recipes are immutable runtime inputs.

Their Editors MUST NOT present runtime state as serialized authoring.

A Profile Inspector SHOULD prioritize:

```text
identity;
display or selection data;
owner-local policy;
references to other authored definitions;
preview when recognition benefits from it.
```

A Recipe Inspector SHOULD prioritize:

```text
construction intent;
reusable parameters;
compatibility requirements;
preview or summary of expected materialization;
validation.
```

Runtime MUST NOT write state back into Profiles or Recipes.

Editor previews MUST NOT mutate the source asset.

## 10. Authoring components and composers

Authoring components MUST express intent and composition.

They MUST NOT execute gameplay in Edit Mode.

Their Editors SHOULD show:

```text
what will be authored or connected;
which references are required;
which scope owns the result;
whether materialization exists;
whether Apply/Rebuild is needed;
validation summary.
```

Technical components materialized by a Composer MUST remain inspectable.

They MUST NOT be hidden without an Advanced / Debug path that lets the user
understand the concrete result.

A Composer MUST NOT become runtime authority merely because it created runtime
components.

## 11. Adapters and bindings

An Adapter or Binding Inspector MUST explain the connection it establishes.

The normal Inspector SHOULD answer:

```text
What source does this component observe or publish?
What target does it control or connect?
What scope owns the binding?
Which references are required?
Is the authored binding valid?
```

In Play Mode, it MAY additionally show read-only evidence:

```text
Bound / Unbound
Current source
Current target
Last typed result
Last release result
Last diagnostic
```

The Editor MUST NOT imply that a presentation adapter owns the lifecycle,
loading, transition, Pause, Camera or Player domain when it only implements a
port or publishes a request.

## 12. Triggers and request components

A Trigger Inspector MUST prioritize the requested operation and its explicit
target.

Examples:

```text
Request Route
Request Activity
Restart Activity
Pause
Resume
Reset Object
Reset Group
```

The Inspector SHOULD expose:

```text
operation;
target;
user-facing reason when meaningful;
invocation surface;
validation.
```

Internal request metadata SHOULD remain in Advanced / Debug.

A Trigger MUST NOT execute automatically because it was selected, validated or
repainted.

Smoke menus and diagnostic facades MUST NOT be the primary authoring experience
for a Trigger.

## 13. Runtime status in Inspectors

Runtime evidence displayed by an Editor MUST be read-only.

The Editor MAY repaint during Play Mode to display changing state, but repaint
MUST NOT produce runtime side effects.

Runtime results MUST NOT be serialized back into authored assets unless the
field is explicitly an authored configuration field.

Edit Mode and Play Mode status MUST be visually distinguishable.

Examples:

```text
Edit Mode
  Authored configuration valid.

Play Mode
  Runtime binding active.
```

An Editor MUST NOT present an old Play Mode result as current authoring
validation.

## 14. Prefabs, overrides and hierarchy

Editors MUST preserve normal Unity prefab workflows.

A Custom Editor MUST NOT:

```text
silently apply prefab overrides;
silently revert overrides;
depend on hierarchy names as identity;
depend on a fixed child index;
restructure a prefab during repaint;
remove orphaned overrides without explicit user action.
```

When a structural operation changes components or children, the Editor SHOULD
warn that existing scene overrides may need review.

Hierarchy MAY organize technical components, but hierarchy alone MUST NOT
become runtime authority unless explicitly defined by another accepted
decision.

## 15. Multi-object editing

A Custom Editor SHOULD support multi-object editing when the operation is
unambiguous and safe.

When selected objects contain mixed values, the Editor MUST preserve Unity's
mixed-value semantics.

Actions MUST be disabled or require a clear batch confirmation when:

```text
targets differ in incompatible ways;
an identity-changing action would affect several assets;
Apply/Rebuild ownership differs;
runtime state is not comparable.
```

An Editor MUST NOT silently apply the first selected object's value to all
selected objects.

## 16. API status

Experimental or deprecated surfaces MUST communicate their status.

The status SHOULD be visible without overwhelming the normal workflow.

An Experimental notice SHOULD explain:

```text
what is stable enough to use;
what may change;
where the current limitation is documented.
```

Deprecated surfaces SHOULD provide the replacement path when one exists.

API status MUST NOT be used as a substitute for validation.

## 17. Empty states and creation assistance

When a required authored dependency is missing, an Editor SHOULD provide a
clear empty state.

Where safe and useful, it MAY offer explicit actions such as:

```text
Create New Profile...
Create New Recipe...
Assign Selected Object
Open Create Menu
Select Existing Asset
```

Creation actions MUST:

```text
let the user choose the destination when appropriate;
use canonical namespaces and asset types;
record Undo where supported;
avoid creating unrelated systems;
avoid silently wiring global authority.
```

The Editor MUST NOT create a large hidden composition merely to make an empty
field valid.

## 18. HelpBoxes, previews and visual presentation

HelpBoxes SHOULD be reserved for:

```text
blocking configuration;
important compatibility limitations;
Experimental or deprecated status;
results requiring user action.
```

They SHOULD NOT be used as permanent introductory documentation when labels,
tooltips or a short section description are sufficient.

Previews SHOULD be used when they materially improve recognition or reduce
selection errors.

Examples:

```text
Player Slot icon and accent;
Actor visual;
Camera rig summary;
referenced prefab thumbnail;
materialization target.
```

A missing preview MUST degrade gracefully without generating errors or
continuous expensive work.

The exact colors, spacing and drawing implementation are not architectural
contracts. They MAY evolve while preserving the behavioral rules in this ADR.

## 19. Performance and Editor safety

Custom Editors MUST avoid expensive work during repaint.

They MUST NOT perform repeated:

```text
project-wide scans;
scene-wide searches;
asset import operations;
preview regeneration loops;
runtime simulation;
reflection-heavy discovery.
```

without explicit user action or bounded caching.

Cached data MUST have a clear invalidation rule.

Editor exceptions MUST be contained and reported without leaving partially
mutated authoring.

Editor-only code MUST remain in Editor assemblies or Editor folders.

Runtime assemblies MUST NOT depend on UnityEditor.

## 20. Diagnostics and logging

Inspector status is not a replacement for typed runtime results.

Editors SHOULD present a compact human summary derived from typed state.

They MUST NOT parse human log message text as machine state.

When an operation fails, the Editor SHOULD show:

```text
status;
short explanation;
affected object;
recommended correction;
link or selection action when available.
```

Raw stack traces and large payloads SHOULD remain in the Console or dedicated
diagnostic reports.

## 21. Criteria for creating a Custom Editor

A Custom Editor is justified when at least one of the following is true:

```text
field order does not match product intent;
required and optional states need explicit presentation;
validation or product actions are required;
Advanced / Debug evidence exists;
preview materially improves authoring;
Apply/Rebuild exists;
runtime binding status is useful;
the default Inspector exposes misleading technical structure.
```

A Custom Editor SHOULD NOT be created merely to restyle a simple component.

The default Inspector remains acceptable when it already communicates intent
clearly and no product action or diagnostic surface is needed.

## 22. Documentation requirement

Every recurring product feature with a public Editor surface MUST document:

```text
how to create it;
how to configure it;
how to validate it;
how to Apply/Rebuild when applicable;
what occurs in Play Mode;
how to diagnose it;
which pieces are reusable;
which negative cases are covered by QAFramework.
```

FIRSTGAME demonstration models SHOULD evaluate the surface through happy-path
manual authoring.

QAFramework SHOULD prove negative, mutation-safety and regression contracts.

## Shared Inspector vocabulary

Framework Editors SHOULD use consistent action and section labels.

Recommended labels:

```text
Configuration
Required
Optional
Preview
Validate
Apply
Rebuild
Runtime Status
Last Result
Advanced / Debug
Open Asset
Select Object
Regenerate Stable ID...
```

Recommended status vocabulary:

```text
Not Configured
Not Validated
Valid
Warning
Invalid
Not Bound
Bound
Not Materialized
Materialized
Not Available in Edit Mode
Runtime Unavailable
```

Equivalent concepts SHOULD NOT use different labels without a domain reason.

## Accepted scope

- Unity Custom Editors for framework assets and components.
- Profiles, Recipes, authoring components, composers, adapters, bindings and
  triggers.
- Validation presentation and explicit Editor mutation.
- Advanced / Debug evidence.
- Edit Mode and Play Mode status separation.
- Undo, prefab override and multi-object safety.
- Apply/Rebuild behavior.
- Identity-management actions.
- API status and empty-state presentation.
- Performance constraints for Inspector execution.

## Rejected scope

- A mandatory visual skin or fixed color palette.
- Replacing Unity's serialization, Undo or prefab systems.
- Custom Editors becoming runtime authorities.
- Automatic silent repair.
- Automatic stable-ID regeneration.
- Hidden mutation during repaint or validation.
- Project-wide scans during normal Inspector drawing.
- Smoke menus as the main product workflow.
- QA fault injection inside normal product Inspectors.
- Hiding technical materialization without an inspectable path.
- Runtime state serialized into immutable Profiles or Recipes.
- One universal base Editor that forces unrelated domains into the same layout.
- A Custom Editor requirement for every MonoBehaviour or ScriptableObject.

## Consequences

Framework product surfaces become predictable.

A user who understands one framework Inspector can reasonably expect:

```text
intent first;
required configuration clearly identified;
explicit actions;
non-mutating validation;
technical evidence under Advanced / Debug;
safe and repeatable materialization;
no silent repair;
read-only runtime evidence.
```

Editor implementation requires more discipline around:

```text
serialized properties;
Undo;
prefab ownership;
validation caching;
multi-object editing;
Play Mode repaint;
diagnostic summaries.
```

Some existing Editors will require migration.

The migration should prioritize frequently used product surfaces and Editors
that currently expose internal structure or run validation implicitly.

## Migration strategy

### Cut 1 — Shared vocabulary and audit

- Accept this ADR.
- Inventory existing Custom Editors.
- Classify each surface as Profile, Recipe, Composer/Authoring, Adapter/Binding,
  Trigger or primary intent asset.
- Record deviations from this ADR.
- Do not rewrite all Editors in one cut.

### Cut 2 — High-use asset Editors

Prioritize:

```text
GameApplicationAsset
RouteAsset
ActivityAsset
PlayerSlotProfile
ActorProfile
CameraRigRecipe
```

Prove:

```text
designer-first sections;
manual validation;
Advanced / Debug;
typed references;
explicit identity actions.
```

### Cut 3 — High-use component Editors

Prioritize:

```text
RouteRequestTrigger
ActivityRequestTrigger
ActivityRestartTrigger
SceneLocalPlayerAdmissionAuthoring
LocalPlayerProvisioningAuthoring
PlayerGameplayCameraAuthoring
PauseRequestTrigger
UnityResetSubjectAdapter
```

Prove:

```text
clear intent;
required references;
binding status;
last result;
no hidden authority.
```

### Cut 4 — Composer and materialization Editors

Prioritize:

```text
CameraRigComposer
other mature Apply/Rebuild flows
```

Prove:

```text
idempotence;
Undo;
ownership preservation;
minimal diff;
clear materialization evidence.
```

### Cut 5 — QA and FIRSTGAME evidence

QAFramework should prove:

```text
validation is non-mutating;
Apply/Rebuild is idempotent;
Undo restores the previous state;
multi-object actions are safe;
identity does not mutate implicitly;
repaint has no side effects;
prefab overrides are preserved;
invalid required configuration is explicit.
```

FIRSTGAME should prove:

```text
a designer can create and configure the feature;
the normal Inspector is understandable;
Advanced / Debug is sufficient;
the happy path works in Play Mode;
the resulting prefab or asset is reusable.
```

## Current implementation coverage

The package already demonstrates parts of this decision:

```text
Camera
  Recipe → Composer → Validate → Apply/Rebuild.

Route and Activity triggers
  designer-first target selection and validation work.

PlayerSlotProfile
  presentation preview, manual validation and Advanced / Debug.

Scene Local Player Admission and Pause
  explicit authoring and validation surfaces.

Stable identity
  explicit regeneration tooling exists for Route and Activity assets.
```

Coverage is not yet consistent across all Editors.

This ADR does not declare existing Editors compliant merely because they have a
Custom Editor.

## Acceptance criteria

This ADR may move to `Accepted` when the product direction is approved.

The first migration milestone is complete when:

- the shared vocabulary is adopted;
- high-use asset and trigger Editors follow the normal section order;
- validation is non-mutating and heavy validation is explicit;
- Advanced / Debug consistently contains technical evidence;
- Apply/Rebuild surfaces document ownership and are idempotent;
- stable identity cannot mutate implicitly;
- runtime status is read-only;
- Undo and prefab override behavior is covered by QA;
- FIRSTGAME authoring models record UX findings against this ADR;
- no Editor acts as a hidden runtime command or repair path.

## Pending decisions

- Whether shared Editor drawing utilities should live under
  `Editor/Common` or a narrower product-surface namespace.
- Whether a reusable validation-result view should be standardized.
- Whether all public framework components should show API status through a
  common Editor header.
- Exact UX for batch Apply/Rebuild across multi-object selection.
- Whether Editor screenshots or visual regression tests are worth maintaining.
- Which mature authoring workflows should later be distributed under
  `Samples~`.
