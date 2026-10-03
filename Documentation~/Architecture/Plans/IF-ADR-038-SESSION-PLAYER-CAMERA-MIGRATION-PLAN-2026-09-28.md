# IF-ADR-038 — Session Player Camera Migration Plan

Status: **In progress — CAMERA-038-M Assignment asset migration authored in Framework and core consumers; GameFlow command integration and Unity/QA validation pending**
Date: **2026-10-03**
Architecture authority: **IF-ADR-038 — Session Player Camera Assignments and Occurrence Lifecycle**

## Objective

Migrate the Framework Camera architecture from Camera Presentation / CameraRequest / Route-Activity selection to the Session Camera model defined by IF-ADR-038.

The migration must not preserve the previous model through a Presentation-to-Assignment compatibility layer.

## Starting point

The current runtime is centered on Camera Presentation: `CameraPresentationDefinition`, `CameraPresentationRuntime`, `CameraRequest`, `CameraOutputContext` and `CameraPresentationLifecycleRuntime`. Game Flow currently participates in Camera lifetime/selection.

Useful technical pieces can be retained where their semantics still match the new architecture:

- physical Output materialization;
- explicit Player/Output correlation;
- Cinemachine channel isolation;
- PlayerInputManager integration and split-screen layout ownership;
- Fixed, Follow and Group rig behavior;
- typed Camera Subject evidence and stale-evidence protection.

The current model does not represent Session-scoped cameras without Players, membership independent from occurrence lifetime, mode-specific occurrence identity, or the distinction between configured Assignment, presented normal Occurrence and Fallback coverage.

## Execution cuts

### CAMERA-038-A — Player Actor occurrence authority migration

Migrate Player runtime contracts to the Actor occurrence authority defined by
IF-ADR-038 before migrating any additional samples/assets. The Actor occurrence
root owns physical state and spatial pose; optional visual content is subordinate.
`PresentationMount` and `ActorProfile.PresentationPrefab` are not required
architecture. Do not add generic mounts or compatibility aliases.

Cover the complete affected boundary:

- `PlayerActorRuntimeHost` and `ActorProfile` authoring/configuration;
- SceneProvided validation, composition, adoption and release;
- ManagerProvisioned candidate materialization, commit and release;
- materialization handle and prepared Actor occurrence/evidence;
- Route placement and Activity relocation against the Actor root;
- Actor replacement preserving Actor root pose, Player identity and the existing
  Camera Occurrence while publishing fresh Actor/Subject evidence;
- validators, Editor authoring utilities and affected tests/fixtures.

SceneProvided and ManagerProvisioned must converge on the same ownership and
runtime semantics. Retire the Actor Presentation spatial authority; do not confuse
it with Camera Presentation. Runtime and serialized changes remain for this cut's
implementation; this plan entry defines the migration boundary only.

Gate: static reference audit proves no active placement, replacement, prepared
Actor validity or lifecycle contract requires a separate Actor Presentation.
Required validation includes affected automated tests and manual SceneProvided /
ManagerProvisioned Unity validation in the implementation phase.

### CAMERA-038-B — Core Assignment / Occurrence

Create the minimum domain model for:

- Session Camera Assignment;
- occurrence mode/lifetime;
- membership policy;
- target policy;
- explicit Output mapping;
- Camera Occurrence identity.

Identity rules:

- Session-scoped / Shared group: `Assignment + Output`;
- Individual: `Assignment + exact PlayerOccurrence + Output`.

Keep mode, membership and target policy independent. Prefer plain C# domain types. Do not integrate with Game Flow or migrate assets yet. Do not create a Presentation compatibility facade.

Tests must cover identity/equality, distinct Player occurrences, shared Rig Prefab reuse without shared mutable runtime identity/state, and relevant invalid configurations.

### CAMERA-038-C — Outputs and Fallback

Replace the old Default-camera semantics with explicit Fallback coverage.

Preserve physical Output/Brain materialization and channel isolation where valid. The Output must distinguish:

1. configured active normal Assignment;
2. currently presented normal Occurrence;
3. temporary Fallback coverage.

Fallback must exist before normal content, must not deactivate an Assignment, and must return to the same valid Occurrence after temporary coverage.

Do not reuse `CameraOutputContext` as CameraRequest arbitration.

### CAMERA-038-D — Session-scoped camera with zero Players

Materialize a normal Session-scoped Assignment without requiring Player membership or Subject.

Prove the product requirement through Character Selection:

- the scene is visible before the first Player joins;
- the camera is normal Session Camera, not Fallback;
- Join does not implicitly select or replace the Assignment/Occurrence.

Support targetless, Fixed and explicit Session/World target sources as required by the Assignment's Rig Prefab.

### CAMERA-038-E — Membership and Subjects

Separate membership from occurrence lifetime and reconcile only exact current Player/Actor evidence.

Join updates only Assignments whose membership policy includes that Player. Activity/Route may affect target eligibility but cannot select or own Camera.

Retain CameraSubject/group/framing infrastructure only where it serves the new Session Assignment model. Remove Presentation/GameFlow subject selection and global discovery.

### CAMERA-038-F — Individual per Player

Create one Occurrence for each:

`Assignment + exact PlayerOccurrence + Output`.

Integrate with admission, Leave and Rejoin independently of SceneProvided versus ManagerProvisioned origin.

Preserve explicit Output binding and PlayerInputManager ownership of viewport layout. Remove Player-to-Presentation binding.

Prove isolated P1/P2 occurrences, Leave cleanup, fresh identity on Rejoin, and no residue on failed admission.

### CAMERA-038-G — Shared group

Create one shared Occurrence per `Assignment + Output`, independent of current member count.

Membership/Subjects may move through `0 → 1 → N → N-1 → 0` without replacing the Occurrence. Group framing must use only current valid Subjects.

A target-required invalid group may be covered by Fallback without destroying/deactivating its Assignment/Occurrence. A targetless-valid group remains a normal camera with zero members.

### CAMERA-038-H — Transactional Assignment change

Implement Session Camera authority for explicit active-Assignment changes per Output.

Transaction:

1. validate;
2. prepare candidate while current camera remains valid;
3. preserve current Assignment/Occurrence if preparation fails;
4. commit Output routing only when candidate is ready;
5. release previous Occurrence after commit.

Do not introduce CameraRequest, precedence, winner arbitration or pending GameFlow selection. Multi-Output operations report per-Output results unless aggregate atomicity is explicitly implemented and proven.

### CAMERA-038-I — Remove Presentation / Request / GameFlow Camera ownership

After all consumers have migrated, remove:

- Route/Activity Camera fields and authoring;
- Camera Presentation lifecycle;
- pending Camera selections;
- CameraRequest publishing/winner arbitration;
- Player-to-Camera-Presentation selection/topology (not Actor visual ownership);
- Camera Presentation materialization that has no new consumer.

Reevaluate `CameraOutputContext` and other old types only after reference analysis. Retain Output, Fallback, Subject and group pieces that implement ADR-038.

No runtime compatibility facade.

### CAMERA-038-J — Authoring, samples and assets

Create the Session authoring flow:

`Session → Assignment assets / Outputs / Fallback`.

Migrate active Framework and planet-devourer assets/samples directly. Do not retain old serialized properties as aliases.

If Unity serialization makes direct migration unsafe, a one-shot Editor converter is allowed only when technically demonstrated and must not become runtime infrastructure.

Character Selection, multiplayer, split-screen and ManagerProvisioned samples are mandatory migration evidence.

### CAMERA-038-K — QA, regressions and documentation cleanup

Replace Presentation continuity tests/QA with Assignment/Occurrence/Fallback evidence.

Required coverage includes:

- zero-Player Session camera and Character Selection;
- targetless Session camera;
- individual and shared modes;
- Rig Prefab reuse across distinct Assignment assets;
- SceneProvided / ManagerProvisioned convergence;
- Join / Leave / Rejoin;
- Actor replacement;
- Activity/Route target eligibility;
- Restart / Reset;
- transactional Assignment replacement and failure;
- Fallback takeover/recovery;
- multiple Outputs/channel isolation;
- Session shutdown.

Only after runtime, authoring, samples and QA are validated, remove obsolete Camera ADRs/guides/certification records identified by IF-ADR-038 from active documentation.

### CAMERA-038-L — Assignment-owned Player Output topology consolidation

Remove `CameraSessionConfiguration.PlayerOutputBindings` and
`PlayerCameraOutputBindingAuthoring`. `CameraSessionConfiguration` owns physical
Output capacity only. Derive the exact Player Slot → Output topology exclusively
from `IndividualPerPlayer` Session Camera Assignments and use that projection for
`PlayerInput.camera`; refresh it transactionally when an Assignment is replaced.
SessionScoped and SharedGroup produce no individual Player binding. Multiple
Players may share one Output in SharedGroup.

Keep split-screen regime selection in the Framework and viewport geometry in
`PlayerInputManager`; Framework camera code must not write `Camera.rect` or
`Camera.pixelRect`. Automatic split-screen slot coverage is required only for
Individual Assignment topology. Invalid, conflicting, or unavailable mappings
fail before Assignment activation/replacement. Session shutdown clears exact
`PlayerInput.camera` associations before releasing Outputs.

Migrate serialized sample/QA GameApplication assets away from the removed
configuration field and update `CharacterSelectionMultiplayerSplitScreen` to
author two explicit Individual Slot-to-Output mappings on its Assignment. Remove
Presentation-era sample data only where the active asset no longer has a consumer;
do not recreate CameraRequest/Presentation surfaces.

Required regression source covers P1→Output1/P2→Output2 and `PlayerInput.camera`,
Join/Leave/Rejoin, SharedGroup with multiple Players on one Output and no
individual topology, SessionScoped with zero Players, invalid/conflicting
mappings, Assignment replacement and shutdown cleanup. Unity compile/import,
Play Mode and QA remain manual gates.

## Execution gates

- Assignment and Occurrence are separate concepts.
- Occurrence mode does not imply membership or target policy.
- Player count never selects Camera or Fallback.
- Fallback never replaces/deactivates the configured normal Assignment.
- Individual identity uses exact PlayerOccurrence, never only Player Slot.
- Shared group allows multiple Players on one Output; Individual allows at most one exact Player occurrence per Output.
- SceneProvided physical Actor replacement support must not be fabricated by Camera.
- Do not maintain old and new Camera products in parallel through a compatibility API.
- Serialized migration must be explicit and validated.

## Sequence

1. CAMERA-038-A — Player Actor occurrence authority migration.
2. CAMERA-038-B — Core Assignment / Occurrence.
3. CAMERA-038-C — Outputs and Fallback.
4. CAMERA-038-D — Session-scoped camera with zero Players.
5. CAMERA-038-E — Membership and Subjects.
6. CAMERA-038-F — Individual per Player.
7. CAMERA-038-G — Shared group.
8. CAMERA-038-H — Transactional Assignment change.
9. CAMERA-038-I — Remove Camera Presentation / Request / GameFlow Camera ownership.
10. CAMERA-038-J — Authoring, samples and assets.
11. CAMERA-038-K — QA, regressions and documentation cleanup.
12. CAMERA-038-L — Assignment-owned Player Output topology consolidation.
13. CAMERA-038-M — Session Camera Assignment asset authority and Definition removal.

`Player Actor occurrence authority → Core domain → Outputs/Fallback → zero-Player Session camera → membership/Subjects → Individual → Shared group → Assignment transaction → remove Camera Presentation/Request/GameFlow Camera ownership → authoring/samples → QA/documentation cleanup → Assignment-owned Player Output topology consolidation → Assignment asset authoring and Definition removal`

### CAMERA-038-M — Session Camera Assignment asset authority and Definition removal

Replace inline `SessionCameraAssignmentAuthoring` values and the reusable
`CameraDefinition` asset with `SessionCameraAssignmentAsset : ScriptableObject`.
The Assignment asset owns a generated technical `AssignmentId`, direct
`RigPrefab`, occurrence/membership/target policies, member Slots, Output
references and Individual Slot-to-Output mappings. Consumers reference the
asset; they do not type identity values. A Rig Prefab can be referenced by
multiple Assignment assets, while each Assignment creates independent mutable
runtime occurrences.

Remove `CameraDefinition` and `CameraDefinitionId` without a runtime
compatibility layer. Migrate target/rig validation to the Assignment asset and
remove Definition-ID collision checks. Keep `CameraOutputDefinition` and its
identity as the physical Output destination contract.

`GameApplicationAsset` stores startup Assignment asset references. Command
triggers use asset references for Activate, Previous + candidate for Replace,
and the target asset for Clear. Preserve stable AssignmentIds in migrated
serialized data; generate IDs during asset creation and keep them out of normal
consumer editing. Assignment ID uniqueness is scoped to a Session, allowing the
same Assignment asset to be referenced by different Game Applications.

Migrate Framework tests, validators, Inspectors, docs, MinimalGame, Character
Selection, CharacterSelectionMultiplayerSplitScreen, GameFlow A/B and QA-NEW-004.
The GameFlow proof must exercise Hub→fallback, Activate A, Replace A→B,
Replace B→A, and Clear→fallback through explicit authored triggers; do not add
Camera fields to Route/Activity or write Output viewport geometry.

Required tests include invalid Rig Prefab and incompatible target policy,
independent occurrences for a shared Rig Prefab, startup/fallback, Activate,
Replace A↔B, failed replacement preserving the prior Assignment, Clear then
Activate, Individual Output mappings and `PlayerInput.camera`, SharedGroup with
no individual topology, SessionScoped with zero Players, and shutdown cleanup.
Unity compile/import, EditMode, Play Mode and QA remain manual validation gates.

Each cut must report **Implemented / Tested / Integrated / Validated** separately. Manual success is not sufficient for closure.
