# Scene-Provided Local Player Product Composition — 2026-08-17

Status: **DECIDED / IMPLEMENTATION PENDING**  
Classification: **Stage B product-authoring correction**  
Runtime authority change: **None**  
Primary related decisions: IF-ADR-002, IF-ADR-003, IF-ADR-005, IF-ADR-010, IF-ADR-012, IF-ADR-015, IF-ADR-016  
Governance: IF-GOV-001

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
product authoring composition   INCOMPLETE / UX GAP CONFIRMED
```

## 2. Product definition

The framework will expose **Scene-Provided Local Player** as one understandable
product-authoring composition.

For the Unity Input variant capable of reaching `GameplayReady`, the canonical
composition is:

```text
Scene-Provided Local Player
├─ SceneLocalPlayerAdmissionAuthoring
├─ LocalPlayerHostAuthoring
├─ PlayerInput
├─ UnityPlayerInputGateAdapter
└─ ActorMount
     └─ Logical Actor
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

Logical Actor
  gameplay representation selected/adopted by existing Player lifecycle authority
```

## 3. Scope

The canonical statement is intentionally specific:

```text
Scene-Provided Local Player
+ Unity Input
+ Activity requiring GameplayReady
```

requires the complete composition above.

This does **not** mean every possible Player provisioning model must use the same
components. Manager-Provisioned and future accepted provisioning models may have
other product compositions while preserving the same underlying authority boundaries.

## 4. Official authoring path

The package will provide two complementary product surfaces.

### 4.1 Explicit Create action

Canonical product path:

```text
GameObject
  > Immersive Framework
    > Player
      > Create Scene-Provided Local Player
```

The Create action owns only deterministic technical composition. It must create and
wire the required product structure without starting runtime gameplay or inventing
consumer gameplay intent.

It may deterministically create/wire:

```text
SceneLocalPlayerAdmissionAuthoring
LocalPlayerHostAuthoring
PlayerInput
UnityPlayerInputGateAdapter
ActorMount
same-object PlayerInput references
Host -> ActorMount reference
```

Consumer-authored decisions remain explicit, including where applicable:

```text
Player Slot Profile
Actor Profile / Scene Actor
InputActionAsset
Gameplay Action Map
```

The operation must be Undo-aware and safe under the existing ADR-010 Editor-write
rules.

### 4.2 Canonical prefab/template

The package will also expose an inspectable canonical product composition representing
the same Scene-Provided Local Player shape.

Working product name:

```text
LocalPlayer_SceneProvided_UnityInput
```

The template/prefab exists to make the official product graph visible and reusable. It
must not become a second runtime authority and must not hide the concrete components it
contains.

The Create action and canonical template must describe the same composition; they must
not drift into separate Local Player definitions.

## 5. No permanent Composer is introduced by this cut

The current problem is initial deterministic composition, not an ongoing derived
materialization lifecycle.

Therefore this cut does not introduce:

```text
LocalPlayerComposer
Apply / Rebuild
LocalPlayerCompositionRuntime
new Local Player manager/service
```

A future Composer/Apply model requires separate evidence that continued materialization
is actually necessary.

## 6. No validator proliferation

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

## 7. Product naming family

The Editor/product language will use **Local Player** as the common family term so the
consumer can see that separate authorities participate in one product composition.

Canonical product term:

```text
Scene-Provided Local Player
```

Recommended Editor grouping:

```text
Immersive Framework
  > Player
    > Local Player
      > Scene-Provided Admission
      > Host
      > Unity Input Gate
```

The current `SceneLocalPlayerAdmissionAuthoring` Add Component label
`Scene-Provided Player Composer` is misleading because that component does not compose
the complete gameplay-capable Local Player. The product-facing label must describe its
actual authority as admission authoring, not imply ownership of the whole composition.

The current `LocalPlayerHostAuthoring` product label should be normalized into the same
`Player > Local Player` family.

`UnityPlayerInputGateAdapter` remains a technical Unity Input adapter and may continue
to serve its existing reusable contract; the official Local Player Create action must
include/wire it so normal Local Player authoring does not depend on discovering that
technical prerequisite manually.

## 8. Stable type-name boundary

This decision distinguishes **product/Editor naming** from **public C# type naming**.

Current public classes marked `Stable` are not renamed silently by this product cut.
IF-GOV-001 requires breaking changes to Stable consumer surfaces to have an explicit
architecture/migration decision.

Therefore the immediate implementation may normalize:

```text
Create menu labels
AddComponentMenu paths/labels
product/template names
documentation vocabulary
```

while retaining existing Stable class names.

If stronger source-level homogenization is later desired, for example renaming
`UnityPlayerInputGateAdapter` or other Stable Player authoring types, that work must be
handled as an explicit API migration with compatibility consequences reviewed first.

## 9. Existing partial composition evidence

The package already expresses most of this product graph structurally:

```text
SceneLocalPlayerAdmissionAuthoring
  RequireComponent(LocalPlayerHostAuthoring)

LocalPlayerHostAuthoring
  RequireComponent(PlayerInput)
```

The missing product-composition link discovered by FIRSTGAME is the gameplay Gate
adapter plus an official creation path that presents the complete product rather than
expecting consumers to infer it from runtime internals.

The existing development-only command
`Create Scene Local Player Test Surface` is therefore not the final product path. Its
current behavior creates only the admission surface and relies on component dependency
materialization plus manual product knowledge. It should be replaced or superseded by
the official Scene-Provided Local Player Create action.

## 10. Implementation cut

The smallest accepted implementation cut is:

```text
PLAYER-PRODUCT-1
  establish product-facing Local Player naming family
  replace/supersede development-only Scene Local Player creator
  create complete Scene-Provided Local Player Unity Input composition
  include and wire UnityPlayerInputGateAdapter
  create ActorMount
  provide canonical inspectable prefab/template
  keep consumer gameplay intent explicit
  preserve existing runtime authorities
  do not introduce Composer/Apply/Rebuild
  do not rename Stable C# types without separate migration decision
```

Technical Editor QA is justified only for deterministic creation invariants such as:

```text
required component set
same-object Host / PlayerInput / Gate wiring
ActorMount wiring
Undo safety
idempotent/safe creation behavior where applicable
no runtime side effects
```

FIRSTGAME remains the consumer proof for whether the resulting product path is actually
understandable.

## 11. Documentation follow-up

After `PLAYER-PRODUCT-1` is implemented, the package README/Guide should teach the
official path concisely:

```text
Create Scene-Provided Local Player
configure explicit Slot / Actor / Input intent
use GameplayReady when the Activity consumes gameplay input
```

The README/Guide is the discoverability layer. This reconciliation record remains the
reasoning and boundary record for why the composed product exists.

## 12. Normative summary

```text
Local Player is a product composition, not a new runtime authority.

Scene-Provided Local Player + Unity Input + GameplayReady requires:
  SceneLocalPlayerAdmissionAuthoring
  LocalPlayerHostAuthoring
  PlayerInput
  UnityPlayerInputGateAdapter
  ActorMount / Logical Actor relationship

Normal consumers should create that product through one official Create action or the
canonical template, not discover the graph by assembling framework internals manually.

Editor naming should present one coherent Local Player family while each component
retains its separate authority.

Stable public C# types are not silently renamed; any source-level rename is a separate
migration decision.
```
