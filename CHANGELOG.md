# Changelog

All notable package changes are documented in this file.

## [1.1.0-preview.6] - 2026-10-09 — prepared, unpublished

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
pending. This candidate is prepared but unpublished.

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
