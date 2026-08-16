# Framework Usage

Status: Current  
Last updated: 2026-08-16

## 1. Product workflow

A typical manual workflow is:

1. Create a `GameApplicationAsset`.
2. Configure the official application policies required by the game.
3. Create `RouteAsset` and `ActivityAsset` definitions.
4. Configure Route/Activity content, participation, transition and readiness policies.
5. Create one Persistent Content Scene from the official Scene Template, save it as
   a concrete game-owned `.unity` scene, assign it to the Game Application and add
   it to the Build Profile Scene List through the explicit Inspector action.
6. Author gameplay features through their official package surface: component,
   asset, Project Settings, Template or Composer as appropriate.
7. Use explicit Apply/Rebuild only for features that actually materialize derived
   technical state.
8. Validate through the owning product surface.
9. Enter Play Mode and inspect runtime evidence separately from authoring evidence.

Missing required contracts fail explicitly. The framework does not repair invalid
configuration through hidden lookup.

## 2. Authoring principle

Manual explicit authoring is the default.

A feature does **not** need a Wizard, Composer or Apply/Rebuild merely because it
belongs to the framework.

```text
Simple feature
  Add Component / Create Asset
  -> configure
  -> validate
  -> use

Reusable intent
  Profile / Recipe / Template when reuse is real

Materialized composition
  Composer + explicit Apply/Rebuild only when authored intent derives technical state
```

See `Editor-Authoring-Standard.md` and IF-ADR-002/010.

## 3. Authority model

```text
GameApplicationAsset
-> bootstrap
-> Persistent Content load and retention
-> internal FrameworkRuntimeHost
-> Session
-> Route lifecycle
-> Activity lifecycle
-> scoped feature contexts/modules
```

`FrameworkRuntimeHost` is internal. It is not a public service locator and must
not be resolved through static/global lookup.

## 4. Persistent Content

The Game Application references one concrete Persistent Content `.unity` scene.

Preferred creation path:

```text
File
  -> New Scene
  -> Immersive Persistent Content
```

The official product model is:

```text
package source scene
  -> Scene Template
  -> consumer-created .unity scene
  -> GameApplicationAsset reference
  -> explicit Build Profile Scene List entry
```

The Scene Template pipeline validates the instantiated scene but does not create,
repair, save or assign consumer assets.

The current minimal Scene Template starts with the framework-owned persistent
camera structure and the UI event authority required by the product baseline:

```text
Persistent Camera
├── Camera Output
├── Session Camera Target
└── Session Camera Rig

EventSystem
```

The concrete scene remains owned by the consumer game. Presentation Canvas,
Transition, Loading, Pause presentation, Player provisioning and Audio integration
are added only when the game needs them; they are not silently materialized by the
template pipeline.

Exactly one physical Camera Output is required for the current single-output
Camera product boundary. The current minimal template also carries one `EventSystem`
with `InputSystemUIInputModule`.

### 4.1 Game Application Inspector workflow

After creating and saving the concrete scene:

1. Assign it to `GameApplicationAsset > Persistent Content > Content Scene`.
2. Use `Open Content Scene` to inspect/edit the assigned scene.
3. Use `Add to Scene List` to append it, enabled, to the Scene List used by the
   active Build Profile.
4. If the scene already exists in that list but is disabled, use
   `Enable in Scene List` instead.
5. When it is already enabled, the Inspector reports `In Scene List` and does not
   create a duplicate.

This Build Profile operation is an explicit Editor authoring command. Assignment,
validation, Scene Template instantiation and runtime boot never modify the Scene
List automatically.

## 5. Authoring and materialization

Available layers are chosen by need:

```text
Recipe / Profile / Template
  reusable intent

Composer / Authoring Component
  concrete authoring surface when useful

Technical materialization
  deterministic derived components/bindings

Runtime Context / Session / Service
  scoped runtime authority

Diagnostics
  validation/reports/logs/Advanced evidence
```

When Apply/Rebuild exists it must be explicit, deterministic, idempotent,
Undo-aware, non-destructive and limited to technical materialization.

Authoring components do not execute gameplay by accident.

## 6. Runtime diagnostics

Normal Inspectors remain intent-first. Technical evidence belongs under
`Advanced / Debug` where appropriate.

Diagnostics project authority; they do not create a second authority or a hidden
command path.

## 7. Validation and evidence order

For a technical package change:

```text
1. package compiles
2. QAFramework imports/compiles
3. focused technical QA proves the contract
4. FIRSTGAME proves real-product integration when the feature boundary requires it
5. documentation records the accepted/current state
```

For a product-surface change discovered through real use:

```text
1. observe concrete friction
2. identify whether the issue is functional or UX-only
3. if functional, fix package contract and prove through QA + FIRSTGAME
4. if UX-only, make the smallest justified product improvement
5. do not change completion arithmetic merely because the UX became nicer
```

A smoke pass alone does not prove real integration. Conversely, an Inspector that
could be improved later does not make a technically and integrationally proven
feature incomplete.

## 8. FIRSTGAME role

FIRSTGAME is the real consumer of the official package.

It proves:

```text
real scene/asset composition
real lifecycle ordering
cross-system integration
real product behavior using public package contracts
```

It may also reveal UX friction. UX findings are qualitative and should be tracked
as product improvements, not as a separate functional completion percentage.

## 9. Do not introduce

- implicit managers;
- global service locators;
- static runtime host access;
- name/tag scene lookup;
- fallback Slot or Actor selection;
- hidden materialization;
- automatic gameplay configuration;
- runtime reflection without an explicit decision;
- consumer-owned substitutes for official package authority.
