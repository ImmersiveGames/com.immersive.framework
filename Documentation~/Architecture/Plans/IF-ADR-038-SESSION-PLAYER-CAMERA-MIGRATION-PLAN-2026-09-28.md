# IF-ADR-038 — Session Player Camera Migration Plan

Status: **Planned**  
Date: **2026-09-28**  
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

### CAMERA-038-B — Core Definition / Assignment / Occurrence

Create the minimum domain model for:

- Camera Definition;
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

Tests must cover identity/equality, distinct Player occurrences, Definition reuse without shared mutable runtime identity/state, and relevant invalid configurations.

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

Support targetless, Fixed and explicit Session/World target sources as required by the Definition.

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
- Player-to-Presentation topology/selection;
- Presentation materialization that has no new consumer.

Reevaluate `CameraOutputContext` and other old types only after reference analysis. Retain Output, Fallback, Subject and group pieces that implement ADR-038.

No runtime compatibility facade.

### CAMERA-038-J — Authoring, samples and assets

Create the Session authoring flow:

`Session → Definitions / Assignments / Outputs / Fallback`.

Migrate active Framework and planet-devourer assets/samples directly. Do not retain old serialized properties as aliases.

If Unity serialization makes direct migration unsafe, a one-shot Editor converter is allowed only when technically demonstrated and must not become runtime infrastructure.

Character Selection, multiplayer, split-screen and ManagerProvisioned samples are mandatory migration evidence.

### CAMERA-038-K — QA, regressions and documentation cleanup

Replace Presentation continuity tests/QA with Assignment/Occurrence/Fallback evidence.

Required coverage includes:

- zero-Player Session camera and Character Selection;
- targetless Session camera;
- individual and shared modes;
- Definition reuse;
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

`Core domain → Outputs/Fallback → zero-Player Session camera → membership/Subjects → Individual → Shared group → Assignment transaction → remove Presentation/Request/GameFlow Camera → authoring/samples → QA/documentation cleanup`

Each cut must report **Implemented / Tested / Integrated / Validated** separately. Manual success is not sufficient for closure.
