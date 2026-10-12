# IF-ADR-046 — Minuta de decisão de promoção Stable

Status: **Draft for explicit acceptance; no API maturity change**
Date prepared: **2026-10-12**
Decision authority: **IF-GOV-001**
Scope: **`PlayerSessionJoinTrigger` public Manager-Provisioned Join contract only**

## Proposed decision

After the remaining candidate-linked certification evidence is reviewed and accepted, promote the bounded public contract below to `Stable`. This is a proposal for a separate explicit maturity decision under IF-GOV-001; it does not itself change source metadata, documentation status, compatibility guarantees, package version, or release state.

## Proposed Stable closure

- `PlayerSessionJoinTrigger`, including its public `Scope`, `LastOutcome`, `RequestJoin`, `Invoke`, and `InvokeFromDevice` members.
- The complete `LocalPlayerProvisioningConsumerScope` enum, with its existing names and numeric values.
- `LocalPlayerJoinRequest` and all current public members, including exact-reference semantics for `PairWithDevice`.
- `LocalPlayerJoinOutcome` and its current public properties; its constructor remains internal.
- The complete `LocalPlayerJoinOutcomeCode` and `LocalPlayerJoinAdmissionState` enums, with current names and numeric values.
- Compatibility dependencies on Unity `MonoBehaviour` and Input System `InputDevice` as already declared by the package.

The API closure is bounded to Manager-Provisioned technical Session admission and Host commit. It does not promise Actor preparation, materialization, Gameplay Readiness, gameplay admission, or observable success of Unity object destruction.

## Contracts that remain Experimental

This proposal does not promote `IPlayerSessionScopedAccess`, `PlayerSessionObserver`, Session/Slot/Actor profiles, Manager-Provisioned configuration/authoring, `LocalPlayerJoinStatus`, Host or `PlayerInput` evidence, assignment/ownership APIs, Leave/Joining commands, Input/Pause multiplayer behavior, or the Framework as a whole. Existing Stable contracts retain only their already documented scope.

## Preconditions for a later acceptance

1. Review one final evidence packet against IF-ADR-046 §7 and IF-PLAN-ADR-046 v3: candidate-linked Framework package suites, post-removal QA regression, SHA-fixed Slice 1 replay, and FirstGame import/compile/Join/Leave/Rejoin against the same immutable Framework candidate. Keep earlier user-reported results classified as partial provenance; do not backfill missing logs, timestamps or execution SHAs. This packet does not repeat I4 or the physical-compensation proof accepted by composition.
2. Keep the accepted composite decisions unchanged: I4 remains accepted by composed evidence with P1/P2 static-review residuals and I4.2 LEASE historically `BLOCKED 3/3`; physical compensation remains **accepted by composite evidence** under Amendment 002, not an integrated callback-then-throw Play Mode PASS.
3. Confirm the one-public-Join migration/retirement under Amendment 001 and the current public API closure. Reopen P1/P2 only under their documented triggers.
4. On explicit acceptance, update the `FrameworkApiStatus` declarations in `Runtime/PlayerParticipation/Authoring/PlayerSessionJoinTrigger.cs` and the five contract files for `LocalPlayerProvisioningConsumerScope`, `LocalPlayerJoinRequest`, `LocalPlayerJoinOutcome`, `LocalPlayerJoinOutcomeCode`, and `LocalPlayerJoinAdmissionState`. Update the maturity rows/text in `Documentation~/API/Public-API.md`, `Documentation~/Guides/Player-Usage.md`, `Documentation~/README.md`, and the release notes in `CHANGELOG.md`. Keep the bounded status separate from package versioning; `1.1.0-preview.8` does not itself imply Stable, and this draft does not change `package.json` or create a tag.

## Decision record

**Not yet accepted.** `PlayerSessionJoinTrigger` remains Experimental until an authorized decision records acceptance after the preconditions are satisfied. No adjacent Player or Framework API maturity changes by implication.
