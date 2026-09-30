# IF-ADR-038 — Session Player Camera Assignments and Occurrence Lifecycle

Status: **Proposed — normative consolidation in progress**
Proposed: **2026-09-27**
Last updated: **2026-09-30**
Type: architecture / Session Camera / Player membership / Output lifecycle  
Supersedes as normative Camera architecture: **IF-ADR-032, IF-ADR-037**  
Normative relationship: **This ADR defines the primary Session Camera model and the physical/spatial authority of the Player's Actor occurrence. Route and Activity do not own or select Camera. IF-ADR-023 is superseded; IF-ADR-032 and IF-ADR-037 remain superseded. Certifications are historical evidence only.**

## 1. Context

The Session needs a primary camera system that works before Players join, can follow individual Players or a shared group, and keeps physical Outputs valid during boot, loading, transitions, failures and teardown. Player count alone cannot determine which camera is valid or which camera should be shown.

The architecture must separate reusable camera configuration, Session assignment, runtime occurrence, occurrence lifetime, Player membership, current target Subjects and physical Outputs. In particular:

- one Definition may be reused by multiple assignments and runtime occurrences;
- a Session camera may be valid with zero Players and may remain active after Players join;
- individual cameras have independent occurrences per Player;
- a shared group camera has one occurrence per assignment and Output, with membership that changes over time;
- the Output fallback is technical coverage, not the normal zero-Player camera;
- additional gameplay cameras such as cutscenes, boss and vehicle cameras remain game/Cinemachine-owned.

Existing Camera Presentations, Camera Requests and Route/Activity selection describe an earlier model. They do not constrain this decision.

## 2. Decision

**The Session owns Camera Definitions, Camera Assignments, Camera Occurrences, Camera Outputs and one Fallback Camera per Output.** An Assignment independently declares occurrence mode/lifetime, membership policy, target sources and explicit Output destinations. A runtime occurrence is never inferred from Player count alone.

```text
Session
├── Camera Definitions (reusable configuration)
├── Camera Assignments (active configuration and occurrence policy)
├── Camera Occurrences (mutable runtime state)
└── Camera Outputs
    └── Fallback Camera

Player occurrence ── membership ──> Assignment / Occurrence
Actor occurrence  ── current Subject ──> Occurrence
Occurrence        ── exact physical destination ──> one Output
```

### 2.1 Terms and ownership

| Concept | Owns / declares | Does not own |
|---|---|---|
| **Camera Definition** | Reusable primary-camera configuration: supported camera behavior, target requirements, group/framing policy and presentation materialization intent. | Runtime membership, current Subjects, Output identity, occurrence state or Player identity. |
| **Session Camera Assignment** | A Definition reference, occurrence mode, lifetime, Player membership policy, target-source policy and explicit Output mapping. Session is the authority that activates, replaces and ends assignments. | Mutable rig state shared across occurrences or implicit Player-count-based camera switching. |
| **Camera Occurrence** | One live instance with mode-specific identity: Assignment + Output for Session-scoped/shared, or Assignment + exact PlayerOccurrence + Output for individual; owns its materialized primary rig, current membership/Subject projection, validity and teardown. | Definition authoring, physical Output ownership or global Player discovery. |
| **Player membership** | Whether an exact joined Session Player occurrence participates in an assignment and can contribute its current Actor target. | Camera occurrence lifetime except where the assignment explicitly selects Individual per Player. |
| **Camera Subject** | Typed evidence for one current target occurrence, including its observation/framing data as required. | Player membership, camera selection, Output routing or lifetime authority. |
| **Camera Output** | One physical Unity Camera/Brain destination, its channel isolation, active primary occurrence routing and one Fallback Camera. | Player identity, membership policy, group selection or split-screen layout. |
| **Activity / Route** | Participation policy that can make a Player's Actor target eligible or ineligible. | Player/Camera/Output ownership, Camera Assignment, Definition or occurrence selection. |
| **Game / Cinemachine** | Camera behavior details and additional gameplay cameras outside the primary Session Camera assignment system. | Session membership, Player occurrence identity or Framework Output ownership. |

The Framework owns primary-camera authoring/runtime contracts, Session assignment and occurrence lifecycle, target projection, Output routing and fallback coverage. Unity/Cinemachine adapters implement supported physical camera behavior. Game-owned additional cameras remain outside this assignment system and use their own game/Cinemachine control.

### 2.1.1 Player Actor occurrence authority

The exact current Actor occurrence is the physical and spatial authority for its
Session Player. Movement, physics, Route placement, Activity relocation and
preserved pose operate on the Actor occurrence/root. Actor replacement preserves
the Actor root pose. An Actor Presentation is not a spatial authority: when visual
content exists, it is optional and subordinate to the Actor occurrence. Neither
`VisualContentMount` nor `ActorProfile.VisualContentPrefab` is an architectural
requirement. A Camera Presentation is a separate Camera concept and does not imply
an Actor Presentation object.

`PlayerActorRuntimeHost` may remain as a technical occurrence container, but it
does not displace the Actor occurrence as physical/spatial authority. SceneProvided
adoption and ManagerProvisioned materialization converge on the same Actor and
Camera Subject semantics; provisioning origin does not alter ownership. The exact
Actor/Subject evidence must be committed or rejected before Player/Camera mutation.
No additional generic mounts are introduced by this decision.

### 2.2 Assignment dimensions and occurrence identity

Occurrence mode/lifetime and membership/target policy are separate Assignment dimensions. The mode decides how many occurrences exist and when they begin/end. Membership policy decides which exact Player occurrences may participate. Target policy decides which current Subjects those members contribute. A Session-scoped mode does not imply any Player membership; it may have zero membership and use an explicit Scene/World target or no Subject.

Occurrence identity is mode-specific:

```text
Session-scoped or Shared group: Assignment + Output (within the owning Session)
Individual per Player:          Assignment + exact PlayerOccurrence + Output
```

Each identity is fresh for each new runtime lifetime. Stable Definition, Assignment, Slot or Output IDs do not substitute for runtime occurrence identity. Definition reuse creates independent occurrence state.

### 2.3 Assignment modes and occurrence lifetime

The Assignment explicitly declares occurrence mode/lifetime and, separately, membership/target policy. A Definition does not imply either. These modes specify occurrence cardinality/lifetime only; they do not imply that Player membership or Actor Subjects exist. Session-scoped and Shared group occurrences both live for the Assignment lifetime; “Shared group” additionally describes a shared membership/Subject set, not a lifetime that starts or ends with the group size.

| Mode | Occurrence cardinality | Lifetime | Zero-members behavior |
|---|---|---|---|
| **Session-scoped** | One occurrence per Assignment and mapped Output. | Assignment lifetime within the Session. Materialized during Session camera startup; survives Player Join/Leave/Rejoin and Activity/Route changes until explicit replacement/removal or Session shutdown. | Membership is optional and independently authored. With zero membership it can use a Scene/World target or no Subject and remain a normal camera when valid. |
| **Individual per Player** | One occurrence per Assignment + exact PlayerOccurrence + mapped Output included by membership policy. | From that Player's successful admission/membership until Leave, reassignment or Session shutdown. Rejoin creates a new occurrence. | No individual occurrences exist when membership is empty. An explicitly selected Assignment can therefore have no current normal occurrence; Output coverage is determined separately below. |
| **Shared group** | One occurrence per Assignment and mapped Output, regardless of member count. | Assignment lifetime within the Session, independent of Join/Leave and current group size. | Occurrence remains with empty membership. It is valid if its camera behavior supports the available targets, including zero Subjects; otherwise that Output uses its fallback while the occurrence remains diagnosable and recoverable. |

An Assignment may reference a Definition also used by other Assignments. Each Session-scoped/shared occurrence is identified by Assignment + Output; each individual occurrence adds the exact PlayerOccurrence to that identity. An individual Assignment creates a separate occurrence for each included member; a shared-group Assignment creates one occurrence shared by all current members on that Output.

### 2.4 Membership, target sources and Subjects

Assignments use explicit, typed Player Slot membership or another explicitly authored Session membership source. Membership policy is not implied by occurrence mode: a Session-scoped Assignment may have no Player membership, optional Player membership or authored membership. Runtime membership resolves each included Slot to its exact current Session Player occurrence. It must not use scene scans, hierarchy/name lookup, global Player lookup or string parsing. Join updates only Assignments whose membership policy includes that Player.

Target sources are declared by the Definition/Assignment and may include:

- no Subject for a camera behavior valid without targets, such as a fixed authored view;
- an explicit Session/world target source with its own materialization/content lifetime;
- the current Actor target of each eligible Player member;
- the set of eligible current Actor targets for a shared group.

A Subject is valid only for its exact Player and Actor occurrences and revisions. Player identity, Actor identity and Subject identity are separate typed domains. A stable Player Slot is not an occurrence identity; rejoin creates a fresh Player occurrence. Actor replacement creates a fresh Actor/Subject occurrence while preserving Player identity. Stale, foreign, duplicate or regressed target evidence is rejected and cannot restore an older Actor.

An Actor-backed Camera Subject belongs to the exact current Actor occurrence. The Actor occurrence explicitly provides its `ObservationTransform`; it may differ from the Actor root, and the selected Transform must belong to that Actor occurrence. The Actor root is used only when explicitly assigned as `ObservationTransform`; Camera never infers it as a fallback or resolves a Subject through names or hierarchy searches. Subject identity is fresh for each Actor occurrence/revision. Actor replacement preserves the Actor root pose, updates the member's Subject evidence in the existing Camera Occurrence and does not recreate that Camera Occurrence. SceneProvided and ManagerProvisioned providers use these same semantics; provisioning origin does not alter Subject ownership or resolution. Subject authoring is Actor-occurrence-owned and does not depend on Actor Presentation or Camera Presentation.

Activity/Route participation is projected separately from membership. A Player may remain assigned/member while its Actor target is temporarily ineligible. The ineligible Subject is excluded from the occurrence's target set; the Assignment and occurrence remain unchanged. On policy recovery, the current exact Subject can rejoin the target set.

For group behavior, the Definition states whether zero, one or multiple Subjects are supported and its framing requirements. Only current eligible Subjects contribute to group framing. A missing Actor target is not silently substituted. If the current set is below the Definition's minimum valid target count, the occurrence remains alive but invalid for presentation; the Output uses Fallback Camera and diagnostics identify the missing/insufficient target condition. If the Definition supports the current set, including an empty set, it remains the normal camera.

### 2.5 Outputs and cardinality

Every physical Camera Occurrence routes to exactly one exact Session Camera Output. A Session Assignment may map to one or more Outputs; the runtime creates a distinct occurrence per mapped Output so each Unity Camera/Brain has isolated Cinemachine channel state. A group Assignment can map multiple Players to the same Output. An individual Assignment maps at most one Player occurrence to each Output; individual split-screen maps each Player Slot explicitly to its own intended Output. Multiple Players sharing one Output use a Shared group Assignment.

An Output has exactly one configured active normal Assignment at a time. Session authoring/activation must reject overlapping active assignments to the same Output; there is no implicit winner, precedence or tie-break. The active Assignment may currently have no occurrence (for example, an Individual assignment with no included Player) or its occurrence may be invalid for presentation. A Player may participate in a shared group on one Output while another Output has a different assignment. A Player is not implicitly routed to every Output.

The assignment mapping is explicit and typed. Missing Outputs, duplicate/conflicting mappings, duplicate Player memberships and invalid per-mode cardinality fail validation. Output count is authored Session capacity and is never inferred from current Player count. `PlayerInputManager` remains the authority for local split-screen count and physical viewport layout; Camera does not write `Camera.rect` or `pixelRect`.

### 2.6 Explicit active-assignment change

Only Session Camera authority may change the configured active normal Assignment for an Output. Activity/Route, Player Join, Player count and target availability do not choose a replacement Assignment. Assignment activation and current physical presentation are separate state.

An explicit change is transactional per affected Output:

1. Validate the requested Assignment, Definition, Output mappings, membership and target-source configuration.
2. Prepare and validate the new occurrence(s) required by current membership, including rig/materialization and required target evidence, while the current normal occurrence remains current. Zero candidate occurrences is valid for an Individual Assignment with no included Players.
3. If preparation fails, release only the failed candidate and preserve the previous configured active Assignment and its occurrence. If no valid normal occurrence is currently presentable, the Output shows its Fallback Camera without clearing or replacing that Assignment.
4. Once required candidates are ready, atomically make the new Assignment configured active. Route its valid normal occurrence to the Output when one exists; otherwise keep Fallback coverage until membership creates a valid occurrence.
5. Release the previous Assignment's occurrences only after the Output switch commits, unless another Output/explicit Assignment ownership still requires them.

The transaction is scoped per Output. A multi-Output change must report each Output's result and must not claim aggregate atomicity unless a separate implementation proves it. No CameraRequest, precedence or arbitration protocol is introduced.

### 2.7 Output presentation state and Fallback Camera authority

Each Output separately tracks (1) its configured active normal Assignment, (2) the valid normal Occurrence currently presented, if any, and (3) whether Fallback Camera is currently covering the Output. These states are not interchangeable. In particular, fallback coverage does not deactivate, replace or remove the configured active Assignment or destroy its live occurrence. The active Assignment can have no occurrence yet, or an occurrence can remain alive but temporarily not presentable.

Each Output owns one Fallback Camera that is prepared before normal camera activation. It is technical coverage, not a normal Session camera Definition/Assignment and not a request competing with normal cameras.

Fallback assumes the physical Output only when:

- boot has not yet produced a ready normal occurrence;
- an explicit transition/covered-loading operation temporarily requests fallback coverage;
- the configured active Assignment has no current occurrence or no valid occurrence can be presented on that Output;
- the active normal occurrence cannot provide a valid image due to a required target/materialization/runtime failure;
- an explicit assignment transaction has committed removal and no replacement normal occurrence is active.

Zero Players alone never activates fallback. A valid Session-scoped no-Player camera and a valid target-independent group camera remain the currently presented normal Occurrence at zero Players. An Individual Assignment with no included Players may have no occurrence; in that case fallback coverage results from the absence of a presentable occurrence, not from Player count as a selection rule.

When a transition ends or a required condition recovers, Output routing returns to the same still-current valid normal occurrence for the configured active Assignment unless Session Camera authority explicitly changed that Assignment. Transition coverage does not destroy the occurrence, clear its membership, deactivate the Assignment or create a second arbitration authority. If recovery validation fails, fallback continues covering the Output and the exact failure is reported.

## 3. Lifecycle contracts

### 3.1 Session startup and boot

1. Validate Session Camera Definitions, Assignments, Output capacity, Output mappings, fallback rigs, occurrence modes and authored membership.
2. Materialize each Output and its Fallback Camera first; each Output has a valid physical camera path before normal content is ready.
3. Materialize the normal Session-scoped and shared-group occurrences required by configured active Assignments, even when there are zero Players. Session-scoped occurrence creation does not add implicit Player membership. Individual occurrences wait for their exact Player membership.
4. Present each valid normal occurrence on its Output. Until one is presentable, fallback covers that Output while the configured active Assignment remains recorded.
5. A zero-Player Session is a normal state. It may present a valid Session-scoped or zero-target-capable group camera indefinitely; Player count does not change the Assignment or select fallback.

Invalid required Session authoring fails Session Camera initialization with typed diagnostics. It does not silently synthesize a Definition, Output, Subject or alternate Assignment.

### 3.2 Join, admission and provisioning origin

On successful admission, Session Player membership is reconciled against configured active Assignments:

- add the exact Player occurrence only to Assignments whose independent membership policy includes that Player, then refresh their target projections;
- create an individual occurrence for each active Individual Assignment that explicitly includes that Player and Output;
- do not add Player membership to a Session-scoped Assignment merely because the Assignment is Session-scoped; it may have no members and use a Scene/World target or no Subject;
- if Join creates a valid occurrence for the configured active Assignment on an Output currently covered by fallback, present that occurrence and end fallback coverage without changing the Assignment;
- validate target evidence independently from Player membership. A Player with no Actor/target may remain joined; affected occurrences become target-invalid only when their Definition requires that target.

SceneProvided candidates remain externally owned before successful adoption. ManagerProvisioned candidates remain under the provisioning authority before commit. After successful admission, both origins use the same Player occurrence identity and Camera membership rules. Admission failure leaves no committed membership or individual occurrence; candidate cleanup follows the original provisioning ownership contract.

The camera Output is assigned by Session authoring, not by provisioning origin. A missing required Player-to-Output mapping is an explicit admission/assignment configuration failure for that camera capability; the Framework does not choose an arbitrary Output.

### 3.3 Leave and Rejoin

Leave targets the exact current Session Player occurrence, not only its stable Slot:

1. Stage the exact Player occurrence as Leaving and stop accepting new membership/target updates for it.
2. Remove its Subject from each Assignment membership/target projection that included that exact Player.
3. Release its individual occurrences and remove only its memberships/Output bindings after their exact assignment ownership is confirmed. Session-scoped/shared occurrences remain alive regardless of whether their member set becomes empty. If the removed Player was the last source of a valid normal occurrence for the configured active Assignment, fallback may cover the Output without deactivating that Assignment.
4. Commit Player Slot vacancy only after required release completes; report partial cleanup truthfully if teardown fails.
5. Keep Session-scoped and shared-group occurrences alive. They may continue normally with fewer or zero members if their target requirements permit it; otherwise their Outputs use fallback.

Rejoin creates a new Player occurrence and fresh membership/Subject tokens. It does not revive the previous individual occurrence or any stale target. Persistent Session/shared occurrences accept the new membership only after exact validation.

### 3.4 Actor replacement

Actor replacement is not Player Leave/Join and does not replace the Session Camera Assignment. For the exact Player occurrence:

1. Prepare the new Actor and its camera target evidence.
2. Keep the old Actor/Subject authoritative until the Actor replacement transaction commits.
3. On commit, atomically remove the old Subject and publish the new Actor occurrence/Subject to each relevant individual, shared and Session assignment.
4. Preserve Session Player identity, assignment membership and Output mapping. Individual and shared-group Camera Occurrences remain the same.
5. Reject stale old-Actor evidence after the replacement. If the new Actor has no valid required target, retain the camera occurrence but route fallback for the affected Output until a valid target or explicit Assignment change exists.

The replacement contract must support both SceneProvided and ManagerProvisioned ownership, or explicitly report the physical replacement as unsupported before mutating Player/Camera state. Camera membership must not fabricate physical ownership of a SceneProvided Actor.

### 3.5 Activity/Route participation and transitions

Entering, leaving or replacing an Activity/Route changes only its participation policy, readiness and other explicitly owned contextual state. For Camera:

- assignment identity and occurrence lifetime are unchanged;
- membership remains Session assignment membership;
- the current Actor Subject may become eligible/ineligible under participation policy;
- target-set changes reconcile against exact current Player/Actor occurrences;
- fallback may cover an Output during an explicit loading/transition interval, then routing returns to the same valid occurrence.

No Route/Activity asset declares Camera Definitions, Assignments, selections, requests or Output mappings.

### 3.6 Restart and Reset

Activity Restart exits/re-enters the Activity occurrence and reapplies its participation policy/readiness. It does not restart Session assignments or occurrences, Player membership, Output mapping or fallback lifetime.

Object/Cycle Reset operates on explicitly selected reset participants. Resetting Actor state may update the same Actor's target transform; replacing the Actor follows the replacement contract. Reset does not synthesize a Player Join/Leave, reset an Assignment or destroy/recreate a Session-scoped/shared Camera Occurrence.

Only an explicit Session Assignment change/removal or Session shutdown ends Session-scoped/shared occurrences. Individual occurrences additionally end when their exact Player membership ends or is explicitly reassigned. Assignment activation, occurrence existence and current Output presentation remain distinct throughout.

### 3.7 Session shutdown

Shutdown blocks assignment changes and membership updates, ends individual occurrences, then shared-group and Session-scoped occurrences, clears target publications, and finally releases Output resources and Fallback Cameras. Cleanup is correlated to exact Session/Assignment/Occurrence identities and is idempotent. A teardown failure is reported; it does not silently commit successful shutdown while retaining a live occurrence.

## 4. Authoring surface

The minimum Session authoring contains:

```text
GameApplication / Session Camera Configuration
  Outputs
    Output Definition
    Output Prefab
    Fallback Camera Rig
  Camera Definitions
    reusable primary-camera behavior/configuration
    target requirements and group framing rules
  Camera Assignments
    Definition
    Session-scoped | Individual per Player | Shared group
    lifetime (fixed by mode unless a specific authored Session lifetime is needed)
    explicit Player Slot membership/source
    target source and required Subject cardinality
    explicit Output mapping
  Configured active normal Assignment per Output
  Current Output presentation state (runtime): normal occurrence or Fallback coverage
```

Definitions contain reusable intent; Assignments contain Session policy; runtime state is never serialized back into either. Each Output has exactly one authored fallback. Assignment membership and Output topology are validated before Session startup. Player Slots in individual assignments map explicitly to Outputs; group assignments explicitly identify the group and shared Output(s). A multi-Output assignment produces separate occurrences per Output.

Authoring must make occurrence mode, targetless validity, minimum group size, membership source and initial Output route visible. It must not expose CameraRequest precedence, pending selection, Route/Activity camera fields or mutable runtime tokens as designer intent.

## 5. Invariants and failure semantics

- Session is the sole owner of Definitions, Assignments, Occurrences, Outputs and Fallback Cameras.
- Definition reuse never shares mutable occurrence state.
- Occurrence lifetime follows its explicit Assignment mode, not membership policy, Player count, Activity/Route lifetime or Subject count.
- Session-scoped mode does not imply Player membership; Join updates only Assignments whose independent membership policy includes that Player.
- Configured active Assignment, currently presented normal Occurrence and temporary Fallback coverage are separate Output states. Fallback coverage never deactivates or replaces the Assignment.
- Session/shared occurrence identity is Assignment + Output; individual occurrence identity is Assignment + exact PlayerOccurrence + Output.
- Zero Players is valid and does not imply fallback or Assignment change.
- Each physical occurrence has one exact Output; Outputs use isolated Unity Camera/Brain/Cinemachine channel state.
- Only Session Camera authority changes an Output's configured active normal Assignment.
- A successful replacement is prepared before the current occurrence is released; failed preparation preserves the current valid occurrence.
- Membership and target projection use typed identities and exact occurrence/revision evidence. No cross-domain ID comparison or string-derived identity is allowed.
- Missing required Definition, Assignment, Output, Fallback, membership source, target or rig fails clearly. No arbitrary Output, Player, Subject, Definition or camera is selected as fallback.
- Fallback covers physical image validity only. It does not make an invalid Assignment or Actor target valid/readied.
- Activity/Route policy may change target eligibility but cannot acquire or release Session camera ownership.
- RuntimeContent scope teardown cannot destroy a Session-owned occurrence or retain a dead Activity/Route scope for camera continuity.
- PlayerInputManager owns split-screen layout; Camera Output mapping does not write viewport rectangles.
- Additional gameplay cameras do not enter Session Assignment membership or Framework primary-camera arbitration.

Failure results identify Session, Assignment, Occurrence, Player occurrence, Actor/Subject occurrence and Output as applicable. Partial operations report committed membership, current Output routing, retained resources and cleanup failure separately; diagnostics do not claim rollback when physical teardown is ambiguous.

## 6. Accepted and rejected scope

### Accepted

- Session-authored reusable Camera Definitions and explicit Camera Assignments.
- Session-scoped cameras without Player membership or Subject requirement.
- Individual per-Player and shared-group occurrence modes.
- Membership and target updates independent of Route/Activity ownership.
- Typed current Actor/Subject projection, including group framing and zero-target validity rules.
- Explicit per-Output routing, fallback and transactional active-assignment changes.
- Unity/Cinemachine adapter for the primary Session camera configuration.

### Rejected

- Player occurrence as owner of the Camera Definition, Assignment or Session/shared occurrence.
- Player count as a camera-selection rule or fallback trigger.
- Route/Activity-owned or selected cameras.
- CameraRequest, precedence, winner arbitration, pending selections or a parallel request system for primary Session cameras.
- Scene/hierarchy/name/global searches to discover Players, Actors, Subjects, Definitions or Outputs.
- One shared mutable rig instance across multiple physical Outputs.
- Treating Fallback Camera as the normal camera for an empty Session.
- Framework ownership of game-specific cutscene, boss, vehicle, PiP or other additional cameras.

## 7. Current implementation coverage

The current package contains the Session Camera runtime path through Assignment replacement: `GameApplicationAsset` authors Session Outputs and Assignments; `FrameworkRuntimeHost` materializes Outputs, creates Assignment occurrences, integrates Player membership and routes transition coverage through Output Fallback. Runtime contracts distinguish Definition, Assignment, mode-specific Occurrence identity, membership, Subject evidence, Output routing and temporary Fallback coverage. Session-scoped, Individual and Shared Group paths and transactional replacement have Editor test sources.

The package-side Camera Presentation / CameraRequest / Route-Activity selection runtime is absent. Migration is incomplete outside the package: QAFramework and planet-devourer still contain serialized assets referring to former Presentation fields/types, while the planet-devourer Getting Started MinimalGame contains the inspected new Definition/Assignment authoring. Editor test source is present but was not executed for this status update. Unity import/compile, Play Mode, consumer migration and Camera QA recertification remain open gates. Group framing remains partial/deferred. See the mutable cut-by-cut status in [IF-TRACK-Framework](../Tracking/IF-TRACK-Framework.md).

## 8. Historical documentation to remove after migration

After runtime, authoring, samples and QA have migrated and the new model has been validated, remove these obsolete Camera norms from active documentation/navigation; do not create an amendment chain:

- `IF-ADR-032-Camera-Unified-Authority-Session-Outputs-Presentations-Subjects-and-Lifecycle.md` and `IF-ADR-037-Camera-Presentation-Selection-Continuity-Across-Game-Flow-Scopes.md`;
- superseded Camera ADR predecessors and camera-only decision files for IF-ADR-004 and IF-ADR-022 through IF-ADR-031, where still present as active documentation;
- `Documentation~/Guides/Camera-Usage.md` and camera sections in Session, Route, Activity and sample authoring guides that teach Presentation/Request selection;
- CAMERA-032/CAMERA-037 reconciliation, certification and closure records, plus package continuity tests and `QA-NEW-004-Certification.md`, after their still-valid evidence has been ported into new Assignment QA;
- serialized Camera Presentation assets/fields in samples and QA after migration (these are content assets, not historical documentation, and are not removed by this ADR creation task).

Player lifecycle decisions about Session occurrence, Leave, provisioning and Actor replacement are not removed by this Camera migration; they remain evidence/input for their respective Player boundaries. Generic QA setup guidance remains if still used by humans.

## 9. Validation required before closing this ADR

The implementation must prove at minimum:

- zero-Player Session-scoped targetless/fixed camera is configured active and normally presented, not fallback, and remains the same Assignment/Occurrence after Join;
- Session-scoped camera with zero membership uses an explicit Scene/World target or no Subject without implicit membership being added on Join;
- configured active Assignment remains unchanged while fallback temporarily covers an Output; transition recovery presents the same valid occurrence, and fallback never deactivates the Assignment;
- active Individual Assignment with zero members may have no occurrence; later Join creates Assignment + PlayerOccurrence + Output occurrence and ends fallback coverage without selecting another Assignment;
- Session/shared occurrence identity is Assignment + Output; individual occurrence identity is Assignment + exact PlayerOccurrence + Output, including fresh identity on Rejoin;
- zero-member shared group remains alive; targetless-valid group stays normal, while target-required group uses fallback with exact diagnostics;
- individual occurrences are distinct per Player and exact Output;
- shared group has one occurrence per Assignment/Output and correct current Subject set through 0→1→N→N-1→0; occurrence identity and lifetime stay constant as membership changes;
- Definition reuse creates independent mutable occurrences;
- SceneProvided and ManagerProvisioned admission converge on the same membership behavior;
- Leave/Rejoin uses fresh Player/Subject occurrences and leaves no stale target or camera resource;
- Actor replacement preserves Assignment/Occurrence and reassociates group and individual targets;
- Activity/Route participation changes target eligibility only; restart/reset preserves Session assignments;
- explicit active-assignment replacement commits without a winner gap and preserves the current occurrence on candidate failure;
- transition fallback coverage returns to the same valid normal occurrence;
- each Output has isolated channels/fallback and split-screen layout remains PlayerInputManager-owned;
- Session shutdown releases occurrences before Outputs and fallbacks.

Unity import/compile, Play Mode and QA certification are manual validation gates. Static document review alone does not close implementation or certify runtime behavior.

## 10. Decision status

This ADR records the proposed canonical architecture. Runtime/API names, exact serialized shapes and adapter contracts must be designed within these ownership and lifecycle invariants. Acceptance requires review of the implementation boundary and its authoring/QA plan; historical acceptance of the replaced Camera model does not accept this ADR.
