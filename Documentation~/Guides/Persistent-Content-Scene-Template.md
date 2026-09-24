# Persistent Content Scene Template

Status: **IF-ADR-032 current contract / Persistent Content template has no gameplay Camera topology**
Last updated: **2026-09-22**

## Purpose

Persistent Content uses Unity Scene Templates as an explicit Editor authoring surface for application-persistent scene content.

The Scene Template is authoring convenience. The concrete consumer .unity scene remains the runtime product.

Current Camera authority:

- [IF-ADR-032 — Camera Unified Authority, Session Outputs, Presentations, Subjects and Lifecycle](../Architecture/ADRs/IF-ADR-032-Camera-Unified-Authority-Session-Outputs-Presentations-Subjects-and-Lifecycle.md)
- [Camera Usage](Camera-Usage.md)

## Authority

~~~text
Physical package source scene
  authored and validated in Unity

Scene Template
  reusable Editor creation surface

Created game scene
  consumer-owned persistent application composition

GameApplicationAsset
  references the concrete Persistent Content scene
~~~

The Scene Template does not become runtime authority.

## Authoring rule

~~~text
Assets do not silently create other assets.
~~~

Inspectors may edit/open references, run explicit validation and expose diagnostics. They must not silently create sibling assets, scenes or derived gameplay intent.

## Camera under IF-ADR-032

Persistent Content is no longer the target authority for Camera topology or normal gameplay Camera presentation.

Target Camera ownership is:

~~~text
GameApplication / Session Camera configuration
  -> physical Camera Outputs + Defaults

Session / Route / Activity
  -> reusable Camera Presentations
~~~

Therefore the target minimal Persistent Content scene does not require:

~~~text
CameraOutputAuthoring
CameraSharedComposition
SessionCameraOverride
PlayerCameraOutputPolicyAuthoring
PlayerCameraCompositionPolicyAuthoring
gameplay Camera Rigs
~~~

A game may still contain Camera-related scene evidence such as Camera Subjects/anchors when those objects genuinely belong to that scene, but the scene is not the normal Game Flow Camera configuration authority.

## Target minimal template

A minimal Persistent Content template should contain only systems that genuinely require application-persistent scene authoring.

Typical baseline:

~~~text
EventSystem
  exactly one EventSystem
  exactly one InputSystemUIInputModule
~~~

Optional application-persistent content may include:

~~~text
Transition presentation
Loading presentation
Pause presentation
Audio integration
Player provisioning
other global presentation/content
~~~

Optional modules do not become mandatory merely because the Framework supports them.

## Camera continuity no longer depends on Persistent Content

IF-ADR-032 keeps a Default Rig on every physical Camera Output.

Outputs are Session-owned capacity and are materialized before normal Route/Activity presentations participate.

Therefore scene replacement can remain visible through:

~~~text
Session Output
  -> Default Rig

covered transition
  -> force-default

old Route/Activity presentation release
new Route/Activity presentation materialization

release force-default
  -> current request winner or Default
~~~

A Persistent gameplay Camera is not required merely to bridge scene unload/load.

## Scene Template pipeline

The official baseline remains verification-oriented:

~~~text
BeforeTemplateInstantiation
  no mutation

AfterTemplateInstantiation
  validate instantiated scene
  report PASS or explicit contract errors
~~~

The pipeline must never silently:

- create gameplay Camera intent;
- repair Camera references;
- save the new scene;
- assign a GameApplication;
- modify the Build Profile Scene List;
- infer Camera Outputs from Player count;
- infer Camera authority from hierarchy/name.

## Consumer workflow

1. Create a concrete Persistent Content scene from the intended template.
2. Save it as a game-owned .unity scene.
3. Configure only application-persistent scene content actually required by that game.
4. Assign the concrete scene to GameApplicationAsset > Persistent Content > Content Scene.
5. Add/enable the scene in the active Build Profile Scene List through the explicit Inspector action.
6. Run owning validation.
7. Run Play Mode consumer proof.

Camera Session configuration and Game Flow Camera Presentations are authored through their IF-ADR-032 surfaces, not by extending the Persistent scene.

## Persistent Content Camera rule

Physical Output capacity and Player Slot bindings live on the GameApplication Camera Session.

The package Persistent Content template contains UI input, not gameplay Camera topology. Do not add these former authorities back:

~~~text
CameraOutputAuthoring
CameraSharedComposition
SessionCameraOverride
RouteCameraOverride
ActivityCameraOverride
PlayerCameraOutputPolicyAuthoring
PlayerCameraCompositionPolicyAuthoring
~~~

Consumer scenes that still contain them are migration input. The Framework does not discover Camera topology by scanning Persistent Content.

## Final target rule

~~~text
Persistent Content
  = persistent scene content

Camera Session
  = physical Camera capacity and Defaults

Session / Route / Activity
  = Camera Presentation intent

Scenes
  = world content and optional Camera Subject/anchor evidence
~~~
