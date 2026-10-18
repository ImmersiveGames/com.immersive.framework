# Changelog

All notable package changes are documented in this file.

## [1.2.0-preview.3] - 2026-10-18 — IF-ADR-050 Actor lifecycle and composition candidate; unpublished

- Adds Experimental committed Actor occurrence identity, guarded prepared Actor replacement, in-place keyed composition, root-stable Camera observation and owner-scoped NonPlayer composition integration. Existing Stable contracts and serialized schemas are not promoted or changed by this candidate.
- Pins the Framework dependency on `com.immersive.composition` `0.1.0-preview.2`. QAFramework source EditMode evidence is Project Owner-reported at 430/430 passed, including Camera 99/99; import and validation of the immutable local package artifacts and populated consumer Actor Profile migration remain pending.
- This is a local candidate only. No tag, GitHub Release, registry publication or Stable promotion is included.

## [1.2.0-preview.2] - 2026-10-17 — Experimental Actor occurrence references; PEGA integration pending

- Implemented IF-ADR-049 Experimental Actor occurrence references for Player Slot and ActorId selection, owner-scoped NonPlayer publication, committed Route/Activity/Persistent Content lifecycles, read-only change observation and stale/ambiguous outcomes.
- The Project Owner reports 11/11 PASS for `Immersive.Framework.ActorReferences.Editor.Tests` in Unity EditMode. PEGA consumer integration remains pending; this preview does not promote Stable contracts.

## [1.2.0-preview.1] - 2026-10-16 — experimental Camera Authoring candidate; OpenUPM publication pending

- Evolution candidate: added Experimental Screen Composer Follow through Cinemachine Position Composer while keeping Fixed Offset as the default. No Stable contract promotion is included.
- Refactored Camera Rig Preview to use the original materialized CinemachineCamera with only a temporary Unity Camera and CinemachineBrain output. Enable/Disable restores target and channel state; uncaptured edits revert to the latest captured state, and Apply / Rebuild retains captured values.
- Added read-only materialization preflight, focused Editor tests, and Camera Usage/Public API guidance for original-camera identity, FixedOffset/ScreenComposer ownership, external OrbitalFollow conflicts, Save Configuration, Prefab Stage, shared Behavior Definitions and Play Mode limits. This tag produced a signed candidate artifact but no GitHub Release or OpenUPM version; IF-ADR-049 is first included in `1.2.0-preview.2`.

## [1.1.0] - 2026-10-14 — first official Stable baseline

This release establishes the compatibility baseline defined by IF-ADR-047.
SemVer guarantees apply to APIs and contracts classified Stable; Experimental
and Internal surfaces keep their existing maturity and guarantees. Earlier
versions remain historical and do not create a generic migration obligation
to 1.1.0.

- Stable API scope is the reviewed per-contract inventory in
  `Documentation~/API/Public-API.md` and the owning ADRs. The package version
  does not promote every public or implemented feature.
- The bounded `PlayerSessionJoinTrigger` Join request closure is Stable under
  IF-ADR-046; Manager-Provisioned setup and adjacent Player APIs keep their
  existing classifications.
- Breaking removals from the preview line include `UnityResetSubjectAdapter`
  and the public `InputMode` subsystem. Consumers migrating from those preview
  surfaces should follow the migration notes in [Reset Usage](Documentation~/Guides/Reset-Usage.md)
  and [Pause Usage](Documentation~/Guides/Pause-Usage.md). No compatibility
  shim is included.
- Package dependencies remain Foundation `0.2.2`, Logging `0.2.3`, Cinemachine
  `3.1.7` and Input System `1.19.0`.

See IF-ADR-047, IF-GOV-001 and the framework tracker for the baseline decision,
validation scope and exact release evidence.

## [1.1.0-preview.11] - 2026-10-14 — candidate, not published

This candidate preserves the approved 1.1.0 stabilization source and corrects
the signed-release workflow after two immutable prerelease tags failed before
producing an artifact.

- The workflow now scopes `runner.temp` to supported step-level contexts and
  compares the installed UPM CLI's exact semantic version while accepting its
  platform suffix, such as `(x64)`. No Runtime, public API or serialized
  authoring behavior changes are intended relative to the `.9` source.
- `v1.1.0-preview.9` was rejected during workflow validation, and
  `v1.1.0-preview.10` stopped at the UPM CLI version check before packing.
  Neither tag produced a package artifact or Release. This `.11` candidate
  remains unpublished until its signed artifact is built and consumer
  certification is complete.

## [1.1.0-preview.10] - 2026-10-14 — failed workflow, not published

Run 18 installed Unity UPM CLI `v9.34.0`, while `upm --version` reported
`v9.34.0(x64)`. The strict comparison failed before package packing or signing;
no artifact or Release was created. The `.10` tag remains immutable.

## [1.1.0-preview.9] - 2026-10-14 — failed workflow, not published

This prerelease candidate contains the approved 1.1.0 stabilization work for
Phase 4 exact-artifact certification. It is not published and does not replace
the latest published preview, `1.1.0-preview.8`.

- Product decision approved: `1.1.0` will be the first official Stable
  baseline, pending release-candidate gates and publication. Compatibility
  guarantees begin there and remain per contract/API maturity. Earlier
  versions, including `1.0.2` and `1.1.0-preview.*`, stay in this changelog as
  history but create no compatibility or generic migration obligation to the
  baseline. See IF-ADR-047 and the tracker for objective GO/NO-GO criteria.
- Removed `UnityResetSubjectAdapter` and its Adapter-only ID/discovery contracts,
  registration port, binding, Scene Lifecycle integration and Editor authoring.
  Removed authored Reset ID suggestion methods and the descriptor-provider
  members used by that path; retained the shared `ResetAuthoringIdentityUtility`
  for prefab recording and retained canonical capability execution metadata.
  `Resettable` owner-aware Route/Activity transactions remain the sole
  registration path. This is a deliberate breaking change under IF-GOV-001;
  authored Adapter components and custom ID/descriptor implementations must
  migrate as documented in Reset Usage. There is no compatibility shim. The
  package version is unchanged in this source cut.
- Removed the public `Immersive.Framework.InputMode` subsystem, including its
  former Stable contracts and the unused `FrameworkIdentityDomain.InputMode`
  value. Pause now owns the only required internal
  transaction. The Pause/Input Gate/physical PlayerInput authority boundaries
  are unchanged. This is a breaking API change; see IF-ADR-005 and the Pause
  Usage migration section. Package version is unchanged in this source cut.
- User-reported Unity evidence for the current pre-release source: QAFramework
  EditMode 310/310 PASS, Pause EditMode 18/18 PASS, Player Participation
  EditMode 101/101 PASS, and QA-IF-ADR-036 Play Mode 8/8 PASS with request B
  accepted/completed, `BaselineRestored` cleanup and no divergences. Raw logs,
  exact execution SHA and immutable candidate resolution were not supplied;
  this evidence is not yet candidate-specific release certification.
- Hardened physical Gate and Pause release integrity: `AlreadyBlocked` now
  requires Action Map read-back; PlayerInput posture restoration compensates
  partial writes; binding tokens and receipts remain owned after recoverable
  release failure; destroyed targets terminate cleanup without claiming
  restoration. Gate automatic projection retries are bounded. Focused tests
  were added, but Unity execution for this source cut is pending.

## [1.1.0-preview.8] - 2026-10-12 — published

This preview release converges Manager-Provisioned Player Join on the
`PlayerSessionJoinTrigger`. IF-ADR-046 records **APPROVED — Player Join Stable**
for the bounded Join request closure only. The evidence amendment accepts
declared partial provenance for the supplied runs; it does not associate them
with a retrospective execution SHA. Unity import/compile of consumer projects
against an immutable package revision and PEGA compatibility remain unverified.

- Removed the replaced Experimental `PlayerSessionJoinCommandTrigger`,
  `ILocalPlayerJoinAccess`, `TryGetJoinAccess`, public
  `LocalPlayerProvisioningAuthoring.RequestJoin` overloads and public
  `LastJoinResult`/`LocalPlayerJoinResult` exposure. The canonical Manager,
  provisioning bridge, Session/Slot/Actor/Leave observation contracts and
  `PlayerSessionJoinTrigger` remain.
- Framework EditMode was reported as 292/292 PASS, PlayerParticipation
  101/101, zero failures. QAFramework IF-ADR-042/043/044 was reported as
  22/22 PASS with cleanup 3/3 `BaselineRestored`. These reports have no
  execution SHA, timestamp or original log/XML associated.
- FirstGame Local Multiplayer Join/Leave/Rejoin was reported functional.
  Character Selection was not independently validated. PEGA resolves the
  earlier `1.1.0-preview.7` and was not tested against this candidate.
- The original FirstGame log, Test Runner screenshot and QA terminal transcript
  are archived in QAFramework's Player Stable evidence set with member hashes
  verified. No execution SHA or timestamp is claimed.
- `PlayerSessionJoinTrigger` and its bounded Join request contracts are Stable
  under IF-ADR-046. Manager-Provisioned configuration and adjacent APIs retain
  their existing maturity. I4.2 LEASE remains
  historically BLOCKED 3/3; P1/P2 are documented residual risks supported by
  static review, not direct test claims.

## [1.1.0-preview.7] - 2026-10-10 — published

This preview delivers the IF-ADR-045 Persistent Content scene-reference and
lifecycle cut. It does not promote additional API surfaces or certify the
Framework as a whole.

- Persistent Content authoring now stores a scene path/name reference, with an
  explicit Editor migration path, path-driven Scene Picker, and validation.
- Runtime composition retains source roots until Session composition succeeds,
  then transfers those roots to application lifetime. Focused regression tests
  cover scene-reference migration/resolution and Session release behavior.
- D7 QAFramework Play Mode evidence reported by the user is partial: 6/7
  checkpoints passed and Session shutdown remains BLOCKED. D8 classifies that
  checkpoint as PACKAGE-LEVEL because consumers have no supported shutdown
  request/result. D9 Edit Mode results (194 passed, 0 failed, 0 ignored) were
  reported by the user; no Unity tests were run for this release preparation.
- Unity package import/compile, complete host-shutdown integration, broader
  IF-ADR-038 Camera recertification, Standalone/WebGL Player Build validation,
  and independent reproduction of the consumer WebGL incident remain pending.

## [1.1.0-preview.6] - 2026-10-09 — published

This candidate consolidates bounded API maturity decisions and their existing
validation records. It changes no runtime behavior or public signatures and
does not certify the Framework as a whole.

- Promoted `PlayerGameplayAvailabilityBlockTrigger`,
  `PlayerGameplayAvailabilityBlockToken`, `PlayerGameplayAvailabilityBlockResult`
  and `PlayerGameplayAvailabilityBlockStatus` to Stable for the bounded
  IF-ADR-044 block/release contract.
- Promoted `ISessionCameraAssignmentCommandPort` and
  `ISessionCameraAssignmentCommandConsumer` to Stable for the bounded
  IF-ADR-039 Activate/Replace/Clear command flow.
- Consolidated the existing IF-ADR-039/042/043/044 evidence and corrected the
  Camera Usage maturity statement. Recorded QA outcomes remain scoped: IF-ADR-042
  7/7, IF-ADR-043 7/7 and IF-ADR-044 8/8; FirstGame results are manual consumer
  integration evidence, not automated certification of every operation or
  failure path.
- `IPlayerSessionScopedAccess`, broader Player Session and Actor surfaces,
  Manager-Provisioned Player, Session Camera Assignment assets,
  `CameraOutputDefinition`, `ActorCameraSubjectAuthoring` and
  `SessionCameraAssignmentCommandTrigger` remain Experimental. The broader
  IF-ADR-038 Camera surface remains separately unrecertified.
- Historical QA and FirstGame runs used a local `file:` Framework dependency
  without an immutable Framework SHA. They remain evidence for the recorded
  behavior, but do not certify this candidate revision; future certifications
  must record the resolved Framework revision.

Unity package import/compile and broader IF-ADR-038 recertification remain
pending.

## [1.1.0-preview.5] - 2026-10-08 — published

This preview adds the consumer-controlled Player gameplay availability block
and completes focused SharedGroup, Individual Output participation and Player
availability validation. It remains a preview: Unity package import/compile
and broader IF-ADR-038 recertification are still required before relying on the
full Camera surface in a production game.

- Added occurrence-scoped Player gameplay availability block commands,
  independent block tokens, scoped cleanup and the `PlayerGameplayAvailabilityBlockTrigger`
  authoring surface.
- Integrated consumer block state with the canonical Unity input gate while
  preserving Pause and other runtime gate ownership.
- Completed focused SharedGroup projection and Individual Player Output
  participation, including Player Leave when no Activity representation exists.
- Recorded IF-ADR-044 runtime QA and Local Multiplayer authoring validation,
  alongside focused IF-ADR-042/043 Camera evidence.
- Updated public API, authoring guides and architecture tracking for the
  validated contracts and remaining release limits.

## [1.1.0-preview.4] - 2026-10-06 — published

This candidate completes the Session Camera Assignment migration and adds
consumer-facing validation for the Local Multiplayer SharedGroup and
CharacterSelection Fixed Follow samples. Automated package QA, broader
IF-ADR-038 recertification and Framework Unity import/compile remain pending.

- Added the explicit Session Camera Assignment command boundary and trigger
  authoring for activation and clearing.
- Completed SharedGroup subject projection, group framing materialization and
  Individual Player Output physical participation with one Fallback Camera at
  zero bound Players.
- Added Route-scoped Activity transition observation while preserving
  ActivityFlow as transition authority.
- Added the Scene Composition binding model and refreshed Camera authoring and
  Editor workflows.
- Integrated the Camera model into Local Multiplayer SharedGroup,
  CharacterSelection Fixed Follow and CharacterSelectionMultiplayerSplitScreen
  consumer samples; manual Play Mode evidence is recorded in the workspace
  tracker.
- Completed the Audio BGM composition/injection refinement and recorded its
  consumer validation.

## [1.1.0-preview.3] - 2026-10-02

This candidate consolidates the IF-ADR-035 Reset authoring and runtime cut.
Reset consumer scenarios 1-8 were integrated and manually validated in
`planet-devourer`; package import/compile and broader Camera/Player validation
remain pending. Published through GitHub Releases and OpenUPM as a preview.

- Reconciled Reset around owner-scoped `Resettable` registration,
  `ResetComposition` and explicit `ResetTarget`, and removed the superseded
  Object Reset Trigger/Group authoring path.
- Closed Activity and Route membership targeting semantics, including Route
  resets covering resettable Activity membership within the current Route.
- Preserved Activity Restart survivor filtering for Route-owned state and
  completed its Reset Target Inspector surface.
- Fixed scoped Object Entry identity duplicate detection and contextualized
  Reset Participant authoring for Resettable and legacy Adapter boundaries.
- Added Active reset baseline authoring and immediate verification diagnostics.

## [1.1.0-preview.2] - 2026-09-30

First preview of the Session Camera Assignment and Player occurrence authority
redesign defined by IF-ADR-038. This package version is a preview and remains
subject to API and serialized-authoring changes.

- Replaced the package Camera Presentation/Request runtime path with Session
  Camera Outputs, Assignments, mode-specific Occurrences and Output Fallback.
- Integrated Camera Assignment creation, Player membership reconciliation and
  transactional Assignment replacement into the Framework runtime host.
- Added Editor regression test sources for Session, Individual and Shared Group
  assignment behavior, membership changes and replacement.
- Only the project's own test fixture requires migration; no external consumer
  migration has been identified. Unity import/compile and runtime validation
  remain pending.

## [1.1.0-preview.1] - 2026-09-30 — unpublished

The tag was created, but release stopped during signed-archive verification
before creating a GitHub Release or publishing to OpenUPM. The verification
pipeline is corrected in `1.1.0-preview.2`.

## [1.0.3-preview.1] - 2026-09-24 — unpublished, superseded

This preview preparation was not included in a tagged package release. Its
distribution and dependency updates are included in the later
`1.1.0-preview.2` candidate.

- Added signed Unity UPM release distribution through GitHub Releases and OpenUPM.
- Updated Foundation to `0.2.2` and Logging to `0.2.3` for the signed registry graph.
- Updated Cinemachine to `3.1.7` for Unity `6000.5` compatibility.
- Preserved Framework runtime behavior and public APIs.

## [1.0.2] - 2026-09-24

- Added the public MIT license and OpenUPM distribution metadata.
- Updated `com.immersive.foundation` to `0.2.1` and
  `com.immersive.logging` to `0.2.2` for automatic registry resolution.
- Preserved Cinemachine `3.1.0`, Input System `1.19.0`, runtime behavior and
  public APIs.

## [1.0.1] - 2026-09-24

First documented GitHub Release of the stable package line. The pre-existing
`v1.0.0` Git tag is preserved as an immutable historical tag; no GitHub Release
was published for it.

### Included product boundaries

- Application bootstrap, Persistent Content, Session, Route and Activity
  lifecycle authorities.
- Route/Activity authoring, content contribution, visibility, readiness,
  loading and transition policies.
- Player Session observation and explicit command surfaces, Manager-Provisioned
  and Scene-Provided local Players, Actor selection, preparation, replacement,
  spatial entry and relocation.
- IF-ADR-032 Camera Outputs, Presentations, requests, rig materialization,
  Subjects, split-layout integration and lifecycle ownership.
- Pause/Input Mode, Reset, Progression Save, Scene Lifecycle Events and
  application frame-rate authoring.
- Optional Audio/BGM integration and Logging guidance.
- Designer-first Inspectors, validation, diagnostics and focused Unity Test
  Framework assemblies.

### Changed

- Rewrote the package README for the current public product surface.
- Added pinned Git installation and getting-started instructions.
- Documented per-surface API maturity and current validation limits.
- Linked the complete set of active usage guides.

### Validation note

This release-preparation cut changes package metadata and documentation only.
Camera aggregate Unity recertification after the IF-ADR-032 legacy removal
remains pending as recorded by the current tracker.
