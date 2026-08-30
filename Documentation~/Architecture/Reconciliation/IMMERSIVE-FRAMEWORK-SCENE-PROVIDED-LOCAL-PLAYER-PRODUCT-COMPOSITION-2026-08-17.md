# Scene-Provided Local Player Product Composition — 2026-08-17

Status: **DECIDED / CREATE TOOL IMPLEMENTED / PACKAGE PREFAB PENDING**  
Classification: **Stage B product-authoring correction**  
Runtime authority change: **None**  
Primary related decisions: IF-ADR-002, IF-ADR-003, IF-ADR-005, IF-ADR-010, IF-ADR-012, IF-ADR-015, IF-ADR-016  
Governance: IF-GOV-001

> Current-name note (2026-08-30): this historical record predates the canonical
> vocabulary migration. The current Runtime type is
> `SceneProvidedLocalPlayerAuthoring`; the Unity path is
> `Immersive Framework/Player/Scene-Provided/Local Player`; and the Creator path
> is `GameObject/Immersive Framework/Player/Scene-Provided/Create Local Player`.
> Earlier type and menu names below are preserved where they document the state at
> the time of this record.

## 1. Trigger

FIRSTGAME Sample 00 exposed a concrete product-composition gap while integrating a
Scene-Provided local Player with Unity Input.

The Sample initially reached only `LogicalActorsPrepared`, so no current gameplay
binding was expected. After the Activity requirement was correctly strengthened to
`GameplayReady`, the Player lifecycle failed closed because the authored Local Player
Host did not contain the required `UnityPlayerInputGateAdapter`.

The authored object already contained:

```text
SceneLocalPlayerAdmissionAuthoring
LocalPlayerHostAuthoring
PlayerInput
```

and appeared to be a complete local Player product surface. Runtime gameplay endpoint
resolution, however, additionally requires exactly one `UnityPlayerInputGateAdapter`
on that stable Local Player Host, targeting the Host's own `PlayerInput`.

After the Gate adapter was added and configured, Sample 00 reached gameplay input and
the first-person Player moved.

Disposition:

```text
runtime gameplay chain          CORRECT / FAIL-CLOSED
locomotion                      NOT THE CAUSE
FIRSTGAME integration           CORRECTED
product authoring composition   UX GAP CONFIRMED
```

## 2. Current implementation evidence

### Package Create tool

The development-only Scene Local Player creator was replaced in package commit:

```text
ImmersiveGames/com.immersive.framework
5c9dab5661c95cf712d8cfce124a5d730d0dd1f1
feat(player): replace development creator with canonical local player tool
```

The implemented menu is:

```text
GameObject
  > Immersive Framework
    > Player
      > Create Scene-Provided Local Player
```

The command creates and wires the deterministic technical composition:

```text
Scene-Provided Local Player
├─ PlayerInput
├─ LocalPlayerHostAuthoring
├─ SceneLocalPlayerAdmissionAuthoring
├─ UnityPlayerInputGateAdapter
└─ ActorMount
```

It explicitly wires:

```text
LocalPlayerHostAuthoring.playerInput -> same-root PlayerInput
LocalPlayerHostAuthoring.actorMount  -> ActorMount
UnityPlayerInputGateAdapter.playerInput -> same-root PlayerInput
```

It does not choose consumer intent:

```text
Player Slot Profile
Actor Profile
Scene Actor
InputActionAsset
Gameplay Action Map
```

The legacy hidden gameplay-map name hint is cleared by the creator so assigning a later
`InputActionAsset` does not silently turn a historical map name into current gameplay
intent.

### FIRSTGAME prefab proof

The latest FIRSTGAME Sample 00 product-authoring proof is:

```text
ImmersiveGames/planet-devourer
facb6e2d9b763b7200e670a029c06100505d7c06
Prefab localPlayer
```

That commit replaces the previously inline Local Player scene composition with a prefab
instance and establishes two distinct prefab roles:

```text
Scene-Provided Local Player.prefab
  technical Local Player product composition

Scene-Provided Logical Player.prefab
  ActorProfile-owned Logical Actor / gameplay representation
```

The scene composes them as:

```text
Scene-Provided Local Player [prefab instance]
└─ ActorMount
   └─ Scene-Provided Logical Player [prefab instance]
```

and the scene instance binds the exact `PlayerActorDeclaration` from the Logical Player
prefab to `SceneLocalPlayerAdmissionAuthoring.sceneLogicalPlayerActor`.

This proves the product should not collapse the technical Local Player prefab and the
Logical Actor prefab into one asset authority.

The FIRSTGAME `Scene-Provided Local Player.prefab` contains sample-specific Slot,
ActorProfile, `InputActionAsset` and Gameplay Action Map configuration. Those values are
consumer configuration and are not framework defaults.

## 3. Product definition

The framework exposes **Scene-Provided Local Player** as one understandable
product-authoring composition.

For the Unity Input variant capable of reaching `GameplayReady`, the canonical scene
relationship is:

```text
Scene-Provided Local Player
├─ SceneLocalPlayerAdmissionAuthoring
├─ LocalPlayerHostAuthoring
├─ PlayerInput
├─ UnityPlayerInputGateAdapter
└─ ActorMount
     └─ Logical Player [ActorProfile.LogicalActorHostPrefab instance]
```

This is a product composition, not a new runtime authority.

The components retain their existing responsibilities:

```text
SceneLocalPlayerAdmissionAuthoring
  Scene-Provided admission intent and lifecycle entry

LocalPlayerHostAuthoring
  stable physical Local Player Host and ActorMount relationship

PlayerInput
  Unity physical input endpoint

UnityPlayerInputGateAdapter
  canonical Framework Gate integration for that PlayerInput

Logical Player / PlayerActorDeclaration
  gameplay representation selected/adopted through existing Player lifecycle authority
```

## 4. Prefab authority split

Two asset roles must remain distinct.

### 4.1 Scene-Provided Local Player prefab

Canonical product-facing name:

```text
Scene-Provided Local Player
```

Its reusable technical shape is:

```text
Scene-Provided Local Player
├─ PlayerInput
├─ LocalPlayerHostAuthoring
├─ SceneLocalPlayerAdmissionAuthoring
├─ UnityPlayerInputGateAdapter
└─ ActorMount
```

For a package-provided neutral prefab/template, consumer-specific choices remain
unassigned. In particular, the package must not invent:

```text
Player Slot Profile
Actor Profile
InputActionAsset
Gameplay Action Map
Logical Actor prefab
```

A consumer project may save a configured instance as its own product prefab, as
FIRSTGAME now does.

### 4.2 Logical Player prefab

The Logical Player prefab remains owned by `ActorProfile.LogicalActorHostPrefab`.

FIRSTGAME currently uses the clear product name:

```text
Scene-Provided Logical Player
```

for its Sample 00 Logical Actor prefab. That name is valid consumer/example vocabulary,
but it does not create a second Local Player Host authority and it is not implicitly the
package's generic gameplay prefab.

The Logical Player may contain consumer gameplay components such as:

```text
PlayerActorDeclaration
PlayerGameplayInputConsumerBinding
CharacterController
locomotion / interaction code
CameraMount or other gameplay-owned mounts
representation objects
```

`PlayerInput` remains on the Local Player Host, not on the Logical Player prefab.

## 5. Scope

The canonical statement is intentionally specific:

```text
Scene-Provided Local Player
+ Unity Input
+ Activity requiring GameplayReady
```

requires the complete technical Local Player composition above plus an exact Logical
Player relationship when that Activity participates with a Player Actor.

This does **not** mean every possible Player provisioning model must use the same
components. Manager-Provisioned and future accepted provisioning models may have other
product compositions while preserving the same underlying authority boundaries.

## 6. Official authoring path

The package provides the explicit Create action as the primary deterministic creation
surface.

The Create action owns only technical composition. It does not start runtime gameplay
or invent consumer gameplay intent.

Implemented behavior includes:

```text
required component set
same-root PlayerInput references
Host -> ActorMount reference
single Undo group
rollback on failed composition
unique sibling name
Play Mode guard
```

The consumer then configures the explicit project inputs and may save the configured
result as a project prefab.

A package-provided inspectable neutral prefab/template remains a separate pending
product artifact. When added, it must describe the same technical shape as the Create
action and must not contain hidden project-specific defaults.

## 7. No permanent Composer is introduced by this cut

The current problem is initial deterministic composition, not an ongoing derived
materialization lifecycle.

Therefore this cut does not introduce:

```text
LocalPlayerComposer
LocalPlayerCompositionRuntime
new Local Player manager/service
```

The existing `SceneLocalPlayerAdmissionAuthoringUtility.ApplyOrRebuild` continues to
operate within its existing Actor materialization/evidence responsibility. It is not a
new owner of the complete Local Player product graph.

A broader Local Player Composer requires separate evidence and a separate decision.

## 8. No validator proliferation

The correction should prevent the incomplete product from being the normal creation
path rather than compensate with a parallel validation subsystem.

Existing component-local validation remains valid, but this cut does not introduce a
new family such as:

```text
LocalPlayerCompositionValidator
LocalPlayerCompositionStatus
LocalPlayerCompositionRepairUtility
LocalPlayerCompositionRuntimeDiagnostics
```

solely to repair an avoidable creation UX problem.

Runtime remains fail-closed when an explicitly hand-authored or modified composition is
invalid.

## 9. Product naming family

Canonical product terms are now:

```text
Scene-Provided Local Player
  technical Local Player Host product composition

Scene-Provided Logical Player
  clear FIRSTGAME name for its ActorProfile-owned Logical Player prefab
```

The terms must not be treated as synonyms.

`SceneLocalPlayerAdmissionAuthoring` is one authority inside the Local Player product;
it is not itself the complete Player composer.

`LocalPlayerHostAuthoring` remains the technical Host authority.

`UnityPlayerInputGateAdapter` remains the reusable Unity Input Gate adapter; the official
Local Player Create action includes/wires it so consumers do not have to discover that
technical prerequisite manually.

Product/Editor labels may continue to be normalized into a coherent Local Player family
without changing Stable public C# type names.

## 10. Stable type-name boundary

This decision distinguishes **product/Editor naming** from **public C# type naming**.

Current public classes marked `Stable` are not renamed silently by this product cut.
IF-GOV-001 requires breaking changes to Stable consumer surfaces to have an explicit
architecture/migration decision.

Therefore this product correction may normalize:

```text
Create menu labels
AddComponentMenu paths/labels
product/prefab names
documentation vocabulary
```

while retaining existing Stable class names.

If stronger source-level homogenization is later desired, that work must be handled as
an explicit API migration with compatibility consequences reviewed first.

## 11. PLAYER-PRODUCT-1 disposition

Current state:

```text
PLAYER-PRODUCT-1

DONE
  replace development-only creator
  expose Create Scene-Provided Local Player
  create complete technical Unity Input composition
  include and wire UnityPlayerInputGateAdapter
  create ActorMount
  keep Slot / Actor / Input intent explicit
  preserve runtime authorities
  preserve Stable public C# type names

FIRSTGAME PROVEN
  Scene-Provided Local Player project prefab
  separate Scene-Provided Logical Player Actor prefab
  scene prefab composition through ActorMount
  exact sceneLogicalPlayerActor binding
  gameplay-capable consumer configuration

PENDING PACKAGE PRODUCT ARTIFACT
  optional neutral inspectable Scene-Provided Local Player prefab/template
  must match Create action technical shape
  must contain no project-specific Slot / Actor / Input defaults
```

Technical Editor QA is justified only for deterministic creation invariants such as:

```text
required component set
same-object Host / PlayerInput / Gate wiring
ActorMount wiring
Undo / rollback safety
no runtime side effects
no hidden consumer intent
```

FIRSTGAME remains the consumer proof for whether the resulting product path is actually
understandable.

## 12. Consumer guidance

The concise normal flow is:

```text
1. GameObject > Immersive Framework > Player > Create Scene-Provided Local Player
2. assign the project InputActionAsset to PlayerInput
3. assign the exact Gameplay Action Map to UnityPlayerInputGateAdapter
4. assign Player Slot Profile
5. assign Actor Profile
6. keep ActorProfile.LogicalActorHostPrefab as the Logical Player prefab authority
7. Apply / Rebuild or otherwise establish the exact scene Logical Player instance
8. use GameplayReady when the Activity consumes current gameplay input/camera authority
```

The Local Player Host prefab and Logical Player prefab remain different asset roles.

## 13. Normative summary

```text
Local Player is a product composition, not a new runtime authority.

Scene-Provided Local Player + Unity Input + GameplayReady requires:
  SceneLocalPlayerAdmissionAuthoring
  LocalPlayerHostAuthoring
  PlayerInput
  UnityPlayerInputGateAdapter
  ActorMount / exact Logical Player relationship

The canonical Create action is implemented.

The Local Player prefab and the ActorProfile Logical Player prefab are separate
asset authorities.

FIRSTGAME proves the split with:
  Scene-Provided Local Player.prefab
  Scene-Provided Logical Player.prefab

A package-neutral Local Player prefab/template, if provided, must match the Create
action's technical composition and must not invent Slot / Actor / Input intent.

Stable public C# types are not silently renamed; any source-level rename is a separate
migration decision.
```
