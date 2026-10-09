# IF-ADR-039 — Session Camera Assignment Command Boundary

Status: **Accepted — Implemented / Tested / Integrated / Validated**
Proposed: **2026-10-03**
Type: architecture / Session Camera / public command boundary
Depends on: **IF-ADR-038**

## 1. Context

IF-ADR-038 establishes Session Camera Assignment as the authority for normal Camera selection and already requires explicit transactional Assignment replacement. The runtime implements replacement internally, but consumers currently have no supported command boundary to activate, replace or intentionally remove an active Assignment.

This blocks legitimate game composition such as:

```text
Hub       -> no normal Assignment -> Output Fallback
Gameplay A -> Assignment A
Gameplay B -> Assignment B
Gameplay C -> no normal Assignment -> Output Fallback
```

Route and Activity must not regain Camera ownership. A game may react to its own flow state and issue a Camera command, but the Camera Session remains the only authority that validates and commits the resulting Assignment state.

## 2. Decision

Expose one narrow Session Camera command boundary owned by Camera Session.

Supported intents:

1. **Activate** — make an authored Session Camera Assignment active on its explicitly mapped Output(s) when those Outputs currently have no active normal Assignment.
2. **Replace** — transactionally replace the currently active Assignment on the candidate's explicitly mapped Output(s).
3. **Clear** — explicitly remove one referenced active Assignment asset, leaving all Outputs owned by that Assignment on their own Fallback Camera.

Activation/replacement uses the existing IF-ADR-038 transaction contract:

```text
validate candidate
-> prepare candidate occurrence(s)
-> preserve current state on failure
-> commit Output routing
-> release previous occurrence(s) after commit
```

Clear is an explicit Session Camera mutation, not temporary fallback coverage. After successful Clear the affected Output has no configured active normal Assignment and presents its Fallback.

## 3. Consumer boundary

The Framework exposes a game-facing command port and a scene-local consumer injection contract, plus an optional Unity trigger/authoring adapter.

The consumer supplies explicit intent through references to authored Assignment assets. It does not:

- access `FrameworkRuntimeHost`;
- discover Camera state globally;
- select by hierarchy/name;
- manipulate Cinemachine directly;
- write `Camera.rect` / `pixelRect`;
- own Assignment or Occurrence lifetime.

A consumer implementing `ISessionCameraAssignmentCommandConsumer` receives the exact public `ISessionCameraAssignmentCommandPort` for the scope lifetime. Gameplay code can issue commands directly; contextual composition can make a decision from Route/Activity state and use the same port without adding Camera fields to those assets. `SessionCameraAssignmentCommandTrigger` is an optional consumer for Inspector and UnityEvent workflows. It serializes an Assignment asset for Activate, an expected Previous Assignment asset plus candidate Assignment asset for Replace, or the active Assignment asset for Clear. No consumer-authored identity string is required. Trigger binding follows the Scene Composition Binding Model in IF-ADR-040.

The command result must expose success/failure and a diagnostic suitable for samples and QA.

## 4. Non-goals

This ADR does not introduce:

- CameraRequest;
- precedence, winner arbitration or a pending selection queue;
- Route/Activity Camera fields;
- automatic Camera selection from Route/Activity identity;
- a global Camera service or service locator;
- a catalog of inactive Assignments in `GameApplicationAsset`;
- fallback as a competing Assignment.

`GameApplicationAsset.StartupCameraAssignments` remains the set of Assignments active at Session startup. Runtime command candidates are explicit consumer intent, not additional startup entries.

## 5. Ownership and semantics

- Camera Session is the single writer of active Assignment state.
- An Output has at most one active normal Assignment.
- Activate fails if a targeted Output already has an active Assignment; callers use Replace for that transition.
- Replace fails transactionally if the candidate cannot be prepared and preserves the previous Assignment.
- Clear targets one explicit active Assignment asset and clears all Outputs owned by that Assignment through Session Camera authority.
- Temporary transition fallback coverage never implies Clear.
- Route/Activity changes alone never issue Camera commands.
- SessionScoped, SharedGroup and IndividualPerPlayer retain the semantics defined by IF-ADR-038.

## 6. Initial consumer proof

The GameFlow sample owns the transition-to-command decision through one Route-scoped observer using IF-ADR-041; Activity/Route assets remain Camera-free. The command boundary remains the only path to Session Camera Assignment state.

```text
Session boot / Hub       -> Fallback; no Clear command
Hub -> Basic A           -> Activate A
A -> B                   -> Replace A -> B
B -> A                   -> Replace B -> A
A/B -> C                 -> Route observer clears A/B -> Fallback
C -> A/B                 -> Activate A/B
A/B -> Hub               -> Route observer receives Activity None and clears A/B -> Fallback
C -> Hub                 -> no command
```

A and B use distinct Fixed Rig Prefabs referenced directly by `CameraAssignment_GameFlow_A` and `CameraAssignment_GameFlow_B`. The sample derives the exact previous/current Assignment from `RouteActivityTransitionContext`; it does not read Camera state globally. Basic C remains content-less and requires no Activity-scene adapter.

## 7. Validation

Closed evidence — 2026-10-04:

- Framework EditMode aggregate: **169/169 PASS**;
- RouteLifecycle observer tests: **6/6 PASS**;
- Camera Editor tests: **74/74 PASS**;
- regression coverage still proves Activate, Replace and Clear while temporary Fallback coverage is owned;
- the migrated GameFlow consumer is one Route-scoped `IRouteActivityTransitionObserver` in `SCN_GameFlow_Basic`;
- GameFlow Play Mode executed `Hub -> A -> B -> A -> C -> B -> Hub`, with all requests completing with `blockingIssues=0`;
- Hub -> A closed with Assignment A active after temporary Fallback coverage released;
- A -> C closed with no active normal Assignment;
- C -> B restored Assignment B;
- B -> Hub closed with no active normal Assignment;
- Activity C remains content-less and owns no Camera adapter;
- no Route/Activity Camera fields, CameraRequest, global Camera lookup or UI-owned Camera decision were introduced.

Disposition:

```text
Implemented  YES
Tested       YES
Integrated   YES
Validated    YES
```

The IF-ADR-041 migration supersedes the earlier Activity-scoped consumer placement while preserving this command boundary unchanged. The broader IF-ADR-038 Camera surface keeps its own maturity and recertification status.

## API maturity disposition — 2026-10-09

The command port and injection contract are Stable in the bounded scope of this decision: ISessionCameraAssignmentCommandPort and ISessionCameraAssignmentCommandConsumer support explicit Activate/Replace/Clear through Session Camera authority and exact composition binding. SessionCameraAssignmentCommandTrigger remains Experimental: binding is covered by Framework Editor tests, but direct execution of its public UnityEvent command surface has no recorded consumer validation. This metadata consolidation does not change the accepted architecture or runtime semantics.

SessionCameraAssignmentAsset, CameraOutputDefinition, and ActorCameraSubjectAuthoring remain Experimental. Framework Editor tests exercise invalid candidate replacement preserving the previous occurrence/output state; the recorded GameFlow consumer proof demonstrates successful command transitions and clear/fallback. This evidence does not certify every Unity failure topology or broaden the Stable guarantee to referenced assets.

The historical Framework/QA/consumer evidence used local file: package dependencies without recording an immutable Framework SHA. Results remain valid for executed behavior; future certification records must capture the exact resolved Framework revision.