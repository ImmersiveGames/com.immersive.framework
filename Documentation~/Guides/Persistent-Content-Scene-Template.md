# Persistent Content Scene Template

Status: **Current contract / package source template refresh required for IF-ADR-004D**  
Last updated: **2026-08-17**

## Purpose

Persistent Content uses Unity Scene Templates as an explicit Editor authoring
surface for application-persistent composition.

The current package ships a **minimal baseline template**. Additional templates
covering optional persistent presentation modules such as Pause, Loading and
Transition are expected future product work; they are not part of the current
baseline and are not required by the Persistent Content contract.

The 2026-08-17 Camera Default-output authority cut adds one required Camera authoring
reference to the runtime contract: `CameraOutputAuthoring` must explicitly reference
its persistent Default `CameraRigComposer`.

## Authority

```text
Physical source scene
  authored and validated in Unity

Scene Template
  reusable Editor creation surface

Created game scene
  concrete application composition

GameApplicationAsset
  reference, navigation and validation only
```

The Game Application never creates scene objects, scenes, prefabs or templates.

## Frozen rule

```text
Assets do not create other assets.
```

This applies to:

```text
GameApplicationAsset
RouteAsset
ActivityAsset
Recipes
Profiles
Templates referenced by other assets
```

Asset Inspectors may:

```text
edit references
open referenced assets
select referenced assets
run explicit validation
show stored diagnostics
```

They may not:

```text
create sibling assets
create scene content
materialize another asset
repair a referenced asset automatically
save another asset silently
```

## Current Camera contract for the minimal template

The required persistent Camera shape after IF-ADR-004D is:

```text
Persistent Camera
├── Camera Output
│   ├── Camera
│   ├── CinemachineBrain
│   ├── CameraOutputAuthoring
│   │   └── Default Camera Rig -> Session Camera Rig / CameraRigComposer
│   └── SessionCameraOverride [optional real Session request]
├── Session Camera Target
└── Session Camera Rig
    ├── CinemachineCamera
    ├── supported local presentation components
    └── CameraRigComposer

EventSystem
├── EventSystem
└── InputSystemUIInputModule
```

The canonical persistent Camera Output ID is:

```text
camera.output.main
```

The Default Camera Rig is not a request and is not derived from
`SessionCameraOverride`.

The current baseline does **not** require:

```text
Global Canvas
Transition surface
Loading surface
Pause surface
Session Camera Override request
other game-specific persistent presentation
```

Their absence does not make the template incomplete. Those systems remain
optional composition and explicit NoOp where the corresponding product contract
allows it.

## Minimum source-scene contracts after IF-ADR-004D

The minimal template contract requires:

```text
exactly one CameraOutputAuthoring
exactly one explicit Default Camera Rig on that binding
exactly one EventSystem
exactly one InputSystemUIInputModule
zero or one SessionCameraOverride
```

The Camera Output contains its explicit Output ID and references to:

```text
physical Unity Camera
Cinemachine Brain
persistent Default Camera Rig
```

`SessionCameraOverride` remains optional. Omit it when Persistent Content
does not need a real Session-scoped Camera request. Player, Activity and Route Camera
publication continue to use the explicit output without requiring an implicit Session
request.

When authored, `SessionCameraOverride` intentionally does not reference a
consumer application asset. Session ownership is explicit through its Scope ID,
which keeps the template reusable across projects.

The EventSystem and Input System UI module live on the same root GameObject.

## Default Camera semantics

The output selection contract is:

```text
force-default system presentation active
  -> Default Camera Rig

otherwise normal Camera request winner exists
  -> winner rig

otherwise
  -> Default Camera Rig
```

Consequences for Persistent Content authoring:

- the Default is required even when no Session override exists;
- the Default has no precedence or tie-break ID;
- `SessionCameraOverride` must never be authored merely to keep a baseline Camera
  visible;
- normal no-winner state does not clear the physical output;
- missing Default must block validation rather than trigger discovery/repair.

## Migration note — scenes created before 2026-08-17

Existing consumer Persistent Content scenes created before IF-ADR-004D must explicitly
assign their intended persistent Default rig.

Typical migration:

```text
Camera Output
  CameraOutputAuthoring
    Default Camera Rig -> existing Session Camera Rig / CameraRigComposer
```

Recommended verification:

1. assign the existing persistent rig through the normal Inspector;
2. run `Validate Configuration`;
3. save the scene;
4. close and reopen the scene;
5. verify `Default Camera Rig` still points to the intended `CameraRigComposer`;
6. run Play Mode consumer proof.

A missing reference fails explicitly at runtime:

```text
Camera Output Session Binding requires an explicit Default Camera Rig.
```

## Package source-template status at the IF-ADR-004D merge

The IF-ADR-004D runtime/editor implementation was merged to `master` at:

```text
8591385d14b646b612b32defc7180e71f21a2beb
Merge branch 'camera/default-output-authority-cut'
```

At that merge, the package source scene:

```text
Editor/SceneTemplates/PersistentContent/PersistentContentTemplateSource.unity
```

still serialized the pre-004D `CameraOutputSessionBinding` shape:

```text
outputId
unityCamera
cinemachineBrain
```

and did not yet serialize `defaultCameraRig`.

It also still contains the historical `SessionCameraOverrideBinding` that previously
represented the persistent Session Camera composition.

Therefore the runtime/Inspector contract is current, but the reusable source scene and
resulting Scene Template artifact require a **separate refresh** before new consumer
scenes created from that template can be called IF-ADR-004D-conformant.

Do not hide this mismatch by restoring Default semantics to the Session request. The
correct follow-up is to update the source/template authoring artifact to the accepted
output contract.

## Source-scene workflow

1. Maintain the physical package source scene for the desired template.
2. Validate the contracts owned by that template, including explicit Default Camera Rig.
3. Create or refresh the `SceneTemplateAsset` through explicit Editor tooling.
4. Consumer projects create their own Persistent Content `.unity` scene from the
   template.
5. The consumer saves and owns that concrete scene.
6. Assign the concrete scene to `GameApplicationAsset > Persistent Content > Content Scene`.
7. Add or enable that scene explicitly in the active Build Profile Scene List.
8. Run explicit validation and Play Mode integration proof as required by the
   consumer project.

The Scene Template itself is never a runtime reference from `GameApplicationAsset`.

## Planned template family

The minimal template is the baseline, not the intended maximum product surface.

Future authoring work may add dedicated Persistent Content template variants for
commonly reused optional modules, including:

```text
Pause presentation
Loading presentation
Transition presentation
combined persistent presentation compositions
other reusable persistent framework modules when a concrete product need exists
```

The exact variant names, combinations and delivery order are **not frozen** by
this guide.

The important architectural rule is that future variants extend authoring
convenience without changing runtime authority or making optional modules
mandatory.

A future variant must preserve:

```text
Scene Template
  Editor-only reusable creation surface

Created .unity scene
  consumer-owned runtime composition

GameApplicationAsset
  references the concrete scene only

Template pipeline
  verifies the contracts owned by that variant
  does not silently materialize or repair consumer content
```

A more complete template therefore does not supersede the minimal template.
Consumers should be able to choose the smallest authored composition that matches
their game.

## Future optional presentation templates

Pause, Loading and Transition already have framework runtime/product contracts,
but inclusion of their persistent visual composition in Scene Templates is a
separate Editor authoring concern.

When those template variants are implemented, each one should explicitly define:

```text
owned hierarchy
required adapters/bindings
required references
validation invariants
consumer-neutral dependencies
what remains optional
```

They must not introduce:

```text
silent scene repair
implicit GameApplication assignment
implicit Build Profile mutation
runtime lookup by hierarchy/name
hidden gameplay intent
mandatory presentation for games that do not use it
```

The current minimal template must not be expanded opportunistically merely because
those modules exist. New variants should be introduced as deliberate product cuts.

## Scene Template boundary

The template is not runtime authority.

```text
SceneTemplateAsset
  Editor-only reusable source

GameApplicationAsset
  never references it

created .unity scene
  runtime-loadable concrete composition
```

The template should be produced only after the source scene compiles and passes
its explicit validation.

## Package asset layout

Current baseline location:

```text
Editor/SceneTemplates/PersistentContent/
  PersistentContentTemplateSource.unity
  ImmersivePersistentContent.scenetemplate
  PersistentContentSceneTemplatePipeline.cs
```

The source scene is Editor-only package content. Consumer projects instantiate a
normal runtime `.unity` scene from the template.

Future template variants should remain under an explicit Persistent Content
Scene Template product surface rather than being generated by runtime assets.

The core source scene is render-pipeline-neutral. Render-pipeline-specific Camera
components belong to explicitly scoped template variants or consumer composition.

## Scene Template pipeline

The official baseline uses:

```text
PersistentContentSceneTemplatePipeline
```

The pipeline is verification-only:

```text
BeforeTemplateInstantiation
  no mutation

AfterTemplateInstantiation
  validate the instantiated scene
  log PASS or explicit contract errors
```

For the minimal template it must validate the Camera and EventSystem contracts owned by
that baseline, including the explicit Default Camera Rig after the template artifact is
refreshed for IF-ADR-004D.

Future template variants may validate additional contracts only when those
contracts are actually authored by the selected variant.

The pipeline never:

```text
creates GameObjects
repairs references
saves the new scene
assigns a GameApplication
adds the scene to the Build Profile
creates or clones assets
```

## Runtime evidence

A concrete Sample 00 Persistent Content scene has been migrated to the explicit Default
contract and exercised in Play Mode.

Observed evidence:

```text
CameraOutputAuthoring
  Initialized
  defaultRig = Session Camera Rig

Activity
  Ready
  blockingIssues = 0

MinimalFirstPersonLocomotion
  READY
  gameplayReady = true
  Move / Look consumed
```

This proves the consumer-scene authoring contract. It is separate from refreshing the
package's reusable Scene Template source/artifact.

`DontDestroyOnLoad` remains implementation evidence, not the authoring authority of the
Scene Template. The architectural contract is application-persistent lifetime and
scoped runtime authority.

## Validation

Validation remains button-driven:

```text
Inspector repaint
  no scene opening
  no component scan

Validate Configuration
  open scene additively when required
  inspect contracts
  close only when validator owns the load
```

Validation reports invalid authored state. It does not silently repair it.

For Camera Output, missing Default Camera Rig is a blocking issue.

## Product direction summary

```text
CURRENT RUNTIME / INSPECTOR CONTRACT
  Minimal Persistent Content
    one Camera Output
    explicit Default Camera Rig
    EventSystem
    optional real Session Camera Override

CURRENT PACKAGE AUTHORING ARTIFACT STATUS
  pre-004D source/template requires refresh

PLANNED, NOT YET IMPLEMENTED AS TEMPLATE VARIANTS
  Pause composition
  Loading composition
  Transition composition
  useful combined variants

ALWAYS
  concrete .unity scene is the game product
  template is Editor-only authoring convenience
  optional modules remain optional
  Default is output-owned, not a request
  pipeline verifies and does not materialize/repair
```
