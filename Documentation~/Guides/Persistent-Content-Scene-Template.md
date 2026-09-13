# Persistent Content Scene Template

Status: **Current template contract with Camera reconciliation pending implementation — IF-ADR-026/027/028**  
Last updated: **2026-09-12**

## Purpose

Persistent Content uses Unity Scene Templates as an explicit Editor authoring surface for
application-persistent composition.

The Scene Template is authoring convenience. The concrete consumer `.unity` scene remains
the runtime product.

Camera details in this guide defer to the current normative Camera decisions:

- [IF-ADR-026 — Camera Subjects, Assignment and Multi-Output Topology](../Architecture/ADRs/IF-ADR-026-Camera-Subjects-Assignment-and-Multi-Output-Topology.md)
- [IF-ADR-027 — Camera Authoring Definitions and Composition Authority](../Architecture/ADRs/IF-ADR-027-Camera-Authoring-Definitions-and-Composition-Authority.md)
- [IF-ADR-028 — Camera Output Participation and Presentation Layout Authority](../Architecture/ADRs/IF-ADR-028-Camera-Output-Participation-and-Presentation-Layout-Authority.md)
- [Camera Usage](Camera-Usage.md)

The current package implementation still contains the previous viewport-bearing Camera
boundary until the pending 026/027/028 cuts are implemented. The template must not be used as
architectural authority for that superseded behavior.

## Authority

```text
Physical package source scene
  authored and validated in Unity

Scene Template
  reusable Editor creation surface

Created game scene
  concrete consumer-owned application composition

GameApplicationAsset
  reference, navigation and validation only
```

The Game Application never creates scene objects, scenes, prefabs or templates.

## Frozen authoring rule

```text
Assets do not create other assets.
```

Asset Inspectors may edit/open references, run explicit validation and expose diagnostics.
They must not silently create sibling assets, scene content or derived authoring state.

## Camera contract for Persistent Content

Persistent Content may host one or more explicit physical Camera Outputs.

The canonical physical Output authoring boundary is:

```text
CameraOutputAuthoring
  Output Definition
  Unity Camera
  CinemachineBrain
  Default Camera Rig -> CameraRigComposer
```

The exact `CameraOutputDefinition` reference is normal authoring authority. Its stable
`CameraOutputId` projection is runtime/diagnostic evidence; consumers should not copy raw
Output ID text between normal Inspectors.

The Default Camera Rig remains explicit persistent Output authority and is not a normal
Camera Request.

```text
force-default owner active
  -> Default Camera Rig

otherwise normal request winner exists
  -> winner Rig

otherwise
  -> Default Camera Rig
```

`SessionCameraOverride` remains optional when a real Session-scoped Camera request is
required. It must not be authored merely to keep the baseline persistent Camera visible.

## Output availability is not active View participation

Corrected IF-ADR-026/028 defines:

```text
Session available Outputs       -> 1..N
current View→Output associations -> 0..N subset
```

Therefore a Persistent Content scene may contain physical Output capacity that is not
currently associated with a Camera View.

The template must not require every authored Output to participate continuously merely
because the physical Output exists.

Player count never creates Outputs implicitly.

## Screen layout is not Camera topology

The Persistent Content Camera structure does not own viewport/display policy merely because it
owns physical Camera Outputs.

The following belong to Output Presentation / Layout authority under IF-ADR-028:

```text
viewport rectangle
split-screen partition
RenderTexture destination
target display
PiP / spectator placement
```

The current package still contains viewport-bearing Camera bindings until CAMERA-027-D2 and
CAMERA-028-B are implemented. New template work must not deepen that dependency.

When layout is required, exactly one explicit layout authority must own each physical
presentation property. PlayerInputManager may be one selected layout authority after
CAMERA-028-D; it does not become Camera topology authority.

## Minimal template contract

A minimal Persistent Content template should provide the smallest reusable application
composition required by the selected product baseline.

For the Camera/EventSystem baseline, the intended contract is:

```text
Persistent Camera
  one or more explicit CameraOutputAuthoring components
    unique Output Definition identity
    Unity Camera
    CinemachineBrain
    explicit Default Camera Rig

EventSystem
  exactly one EventSystem
  exactly one InputSystemUIInputModule
```

Optional systems remain optional:

```text
Transition presentation
Loading presentation
Pause presentation
Session Camera Override
additional physical Camera Outputs
Output Presentation / Layout policy
Audio integration
Player provisioning
```

Their absence does not make the minimal template incomplete unless the selected template
variant explicitly owns those contracts.

## Scene Template pipeline

The official baseline uses a verification-only Scene Template pipeline.

```text
BeforeTemplateInstantiation
  no mutation

AfterTemplateInstantiation
  validate the instantiated scene
  report PASS or explicit contract errors
```

The pipeline must never:

```text
create GameObjects
repair references
save the new scene
assign a GameApplication
modify the Build Profile Scene List
create or clone assets
infer Camera Outputs by name/hierarchy
```

Validation reports invalid authored state; it does not silently repair it.

## Consumer workflow

1. Create a concrete Persistent Content scene from the intended template.
2. Save it as a game-owned `.unity` scene.
3. Inspect and configure explicit Camera definitions/physical references required by that game.
4. Configure an Output Presentation/Layout authority only when the game requires one.
5. Assign the concrete scene to `GameApplicationAsset > Persistent Content > Content Scene`.
6. Add/enable the scene in the active Build Profile Scene List through the explicit Inspector action.
7. Run owning validation.
8. Run Play Mode consumer proof.

The Scene Template itself is never a runtime reference from `GameApplicationAsset`.

## Camera reconciliation and template refresh

The previous package template lineage predates the corrected IF-ADR-026/027/028 layout
boundary. Do not update the template by preserving obsolete viewport ownership simply to match
the current implementation.

Template refresh belongs **after** the corrected runtime/authoring cuts:

```text
CAMERA-026-H2  partial Output participation
CAMERA-026-I   Player→Camera Subject integration boundary
CAMERA-027-D2  logical View→Output authoring without viewport
CAMERA-028-A   available Output vs active participation
CAMERA-028-B   remove viewport from Camera topology
CAMERA-028-C   explicit Output Presentation / Layout authority
CAMERA-028-D   PlayerInput layout integration
```

After those cuts compile and pass focused QA:

```text
1. audit the physical package source scene;
2. migrate it to typed Output/View authoring where applicable;
3. remove stale viewport-bearing Camera topology authoring;
4. author layout only through the selected new layout authority when required;
5. validate the source scene;
6. refresh the SceneTemplateAsset explicitly;
7. instantiate a fresh consumer scene;
8. verify the created scene and Play Mode behavior.
```

Do not silently mutate existing consumer scenes as part of the template refresh.

## Product direction

Future template variants may add reusable optional presentation compositions such as Pause,
Loading, Transition or selected layout policies when a concrete product need exists.

A variant must preserve:

```text
Scene Template
  Editor-only reusable creation surface

Created .unity scene
  consumer-owned runtime composition

GameApplicationAsset
  references the concrete scene only

Template pipeline
  verifies owned contracts
  does not materialize/repair consumer content
```

The minimal template remains the smallest baseline; optional modules do not become mandatory
merely because the Framework supports them.

## Current implementation note

Until IF-ADR-028 is implemented, existing scenes/templates may still serialize the previous
Camera viewport topology. That is compatibility with the current codebase, not the target
architecture.

For new implementation work, use IF-ADR-026, IF-ADR-027, IF-ADR-028 and `Camera-Usage.md` as
the authoritative Camera baseline.