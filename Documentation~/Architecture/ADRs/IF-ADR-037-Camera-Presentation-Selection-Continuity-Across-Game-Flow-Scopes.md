# IF-ADR-037 — Camera Presentation Selection Continuity Across Game Flow Scopes

Status: **Superseded by IF-ADR-038 — historical decision evidence only**
Accepted: **2026-09-27**
Superseded: **2026-09-29**
Superseded by: [IF-ADR-038 — Session Player Camera Assignments and Occurrence Lifecycle](IF-ADR-038-Session-Player-Camera-Assignments-and-Occurrence-Lifecycle.md)
Date: **2026-09-27**  
Type: architecture / Camera / Game Flow / lifecycle / ownership  
Amends: **IF-ADR-032**

## 1. Context

IF-ADR-032 currently couples Camera Presentation declaration and runtime lifetime to the declaring Game Flow scope:

    Session Presentation  -> Session owner/lifetime
    Route Presentation    -> Route owner/lifetime
    Activity Presentation -> Activity owner/lifetime

This is correct for Camera Presentations whose existence is genuinely scoped to that owner, but it also means that leaving a Route or Activity releases its Presentations even when the incoming scope declares no Camera Presentation.

That produces an undesirable product-authoring requirement. A Route or Activity that has no camera intent must repeat the current Camera Presentation merely to avoid falling back to the Output Default.

The desired semantic is:

    incoming scope declares a Camera Presentation -> camera intent changes
    incoming scope declares no Camera Presentation -> camera intent does not change

Therefore an empty Camera Presentation declaration is not equivalent to an explicit request for Default.

## 2. Decision

Absence of Camera Presentation declarations in an entering Route or Activity means **no camera change**.

A Game Flow scope changes the effective Camera Presentation only when it declares camera intent that replaces or overrides the currently effective presentation.

Examples:

    Route A [Camera A] -> Route B [none] -> Camera A remains effective
    Route B [none] -> Route C [Camera C] -> Camera C replaces Camera A

The same semantic applies to Activity transitions.

This decision does not permit a Route- or Activity-owned RuntimeContent occurrence to outlive its owner. Selection authority and effective Presentation lifetime authority are separate concepts when continuity across scope replacement is required.

## 3. Selection authority versus lifetime authority

Game Flow may select camera intent without necessarily owning the full lifetime of the resulting effective Presentation.

    Game Flow selection authority != effective Presentation lifetime authority

A Presentation that must remain effective after the selecting Route or Activity has ended must be represented by a Camera-owned occurrence with a lifetime capable of spanning that transition.

The implementation must not:

- retain a dead Route or Activity RuntimeScopeContext;
- leave a Presentation occurrence owned by an exited scope;
- transfer ownership implicitly to the next Route or Activity;
- copy Camera Presentation declarations into the incoming asset;
- bypass RuntimeContent ownership rules.

The concrete persistent occurrence boundary is an implementation concern to be established by the CAMERA-037 implementation cut, while preserving the invariants in this ADR.

## 4. Replacement semantics

Changing from one effective Presentation to another must be transactional.

    Presentation A effective
        -> materialize and validate Presentation B
        -> B becomes eligible/admitted
        -> B becomes effective
        -> A may be released

Failure to prepare the replacement must not destroy the currently valid effective Presentation prematurely.

CameraOutputContext remains the sole normal CameraRequest arbitration authority. This ADR does not introduce a second winner-selection mechanism.

## 5. Absence is not Default

These intents are distinct:

    no declaration = preserve current effective camera intent
    Presentation X = select Presentation X
    explicit Default intent = intentionally abandon the effective normal Presentation and use the Output Default

This ADR does not require a new public Explicit Default authoring API until a concrete consumer requires it. It only establishes that absence of declaration must not be interpreted as that intent.

### 5.1 Nested pending selections

A Route and its Startup Activity may each declare persistent Camera Presentation
selections during the same Route transition.

- selections for different `CameraOutputId` values may remain pending concurrently;
- selections from two different owners for the same `CameraOutputId` while both
  are pending are an invalid composition and must be rejected before either
  lifecycle commit becomes irreversible;
- rejection must preserve the first owner's pending candidate and ownership so
  its caller can still commit or roll it back normally;
- the rejected owner must leave no materialized candidate or admitted request;
- `CameraOutputContext` does not resolve this authoring conflict through request
  precedence. It remains the normal winner authority only for valid admitted
  requests.

This rule does not establish Route-over-Activity or Activity-over-Route
precedence. No such precedence exists for same-Output nested pending selections.

## 6. Scoped Presentations remain valid

IF-ADR-032 Session-, Route-, and Activity-owned Presentations remain valid.

A Presentation whose intended lifetime is genuinely contextual may still be owned and released with its declaring scope.

This ADR changes the assumption that every Game Flow camera selection must necessarily have the same lifetime as the scope that selected it.

Implementation must distinguish **Presentation belongs to this scope** from **this scope selects the effective Presentation**. The two concepts must not be inferred to be identical.

## 7. Invariants

- Camera Outputs and their Default Rigs remain Session-owned.
- CameraOutputContext remains the sole normal CameraRequest winner authority.
- Camera core does not gain Route- or Activity-specific gameplay semantics.
- No Camera Presentation may remain bound to a dead RuntimeScopeContext.
- One lifecycle owner must never release another owner's live Presentation.
- No declaration means no camera change; it does not mean Default.
- Camera configuration is not copied between RouteAsset or ActivityAsset instances.
- Route or Activity RuntimeContent scopes are never kept alive solely to preserve camera state.
- Player, Actor and Camera Subject lifetime remain independent from Camera selection lifetime.
- Replacement must not destroy the valid current Presentation before the replacement is ready.
- No singleton, global mutable registry, service locator, hierarchy lookup or GameObject-name lookup is introduced.

## 8. Consequences for IF-ADR-032

The IF-ADR-032 Session/Route/Activity owner-lifetime rule remains normative for Presentations whose lifetime actually belongs to those scopes.

It is no longer sufficient as the complete model for persistent Game Flow camera selection. IF-ADR-032 must henceforth be read with an explicit distinction between **Presentation ownership/lifetime** and **Game Flow Presentation selection**.

An entering scope with zero Camera Presentations does not revoke a valid effective Presentation merely because the previous selecting scope is exiting.

## 9. Implementation constraints

Before implementation, inspect the existing CameraPresentationLifecycleRuntime, CameraPresentationMaterializationRuntime, RuntimeContent ownership boundaries, CameraOutputContext arbitration and SessionCameraTransitionOrchestrator.

The implementation must make the smallest coherent change that provides continuity without weakening exact lifecycle ownership.

Do not implement continuity by suppressing TryExitRoute/TryExitActivity release of scope-owned resources.

Tests must prove identity and ownership, not only visual continuity:

- Route A selects A -> Route B declares none: effective camera remains A without a dead Route-owned occurrence.
- Route A selects A -> Route B selects B: B replaces A and A is released.
- Activity A selects A -> Activity B declares none: effective camera intent remains A under the same ownership rules.
- replacement failure preserves the last valid effective Presentation.
- explicit scope-owned contextual Presentations still release with their owner.
- Output Default remains available when no normal effective Presentation exists or when explicitly selected by supported policy.

## 10. CAMERA-037 implementation and closure

The implementation uses one shared `CameraPresentationLifecycleRuntime`
selection transaction for Route and Activity owners. Persistent selections are
materialized in the active Session scope; contextual `cameraPresentations`
remain owned by and released with their declaring Route or Activity scope.

| Cut | Contract | Evidence |
|---|---|---|
| B | Route empty continuity | `EmptyIncomingRoutePreservesSameSessionOwnedSelectionOccurrence` |
| C | Route replacement and rollback | `DifferentSelectionReplacesCurrentOnlyAfterIncomingBecomesWinner`; `FailedReplacementRestoresTheSameCurrentSelection` |
| D | Activity entry and empty continuity | `ActivityEntersPersistentSelectionAndBecomesSessionOwnedWinner`; `EmptyIncomingActivityPreservesSameSessionOwnedSelectionOccurrence` |
| E | Activity replacement and rollback | `DifferentActivitySelectionReplacesCurrentOnlyAfterIncomingBecomesWinner`; `FailedActivityReplacementRestoresTheSameCurrentSelection` |
| F | nested pending composition rule | `StartupActivitySelectionForSamePendingRouteOutputIsRejectedWithoutResidualCandidate`; `StartupActivitySelectionForDifferentPendingRouteOutputIsAllowedAndRollsBackIndependently` |
| B-E public integration | Route/Activity lifecycle, teardown and no normal-winner gap | `QA-NEW-004` |
| IF-ADR-032 retained contracts | contextual scope ownership/release, Default and Session shutdown | accepted CAMERA-032 focused and aggregate validation records |

CAMERA-037 is closed by cuts B-F. The closure does not add Explicit Default,
nested same-Output precedence or multi-Output atomicity.

## 10. Nested Route and Startup Activity selection conflict

A Route and the Startup Activity entered as part of that same Route-start transaction may both declare persistent Camera Presentation selection, but they must not declare persistent selections for the same `CameraOutputId` while the Route selection is still pending.

The accepted rule is:

    Route pending selection [Output A] + Startup Activity selection [Output B]
        -> valid; both may prepare independently

    Route pending selection [Output A] + Startup Activity selection [Output A]
        -> invalid composition; reject before either owner is irreversibly committed

This is intentionally different from a later Activity transition after the Route has committed. A later Activity may replace the currently effective persistent selection for the same Output using the normal transactional replacement semantics in section 4.

Rationale:

- Route selection and Startup Activity selection are two selection authorities participating in nested Game Flow transactions, not two ordinary admitted requests competing for winner status.
- `CameraRequest` precedence answers which already-admitted normal request wins; it does not define which pending Game Flow selection owns the persistent selection after nested transactions commit.
- Inferring "Activity wins" from contextual Presentation precedence would therefore conflate request arbitration with persistent selection ownership.
- No current consumer requires two simultaneously pending selectors for the same Output.
- Rejecting the ambiguous composition keeps ownership and rollback single-writer per Output and avoids introducing a second selection-arbitration protocol.

Transactional requirements:

- conflict detection is per `CameraOutputId`, not global across all Outputs;
- the conflict must fail while the entering operation is still reversible;
- rejecting the Startup Activity selection must not release or promote the Route candidate;
- Route failure still rolls back its own pending selection normally;
- Activity failure must not mutate another owner's pending selection;
- no pending-selection conflict may be resolved through `CameraOutputContext` precedence;
- diagnostics must identify the conflicting Output and owners sufficiently for authoring correction.

This restriction may be revisited only when a concrete consumer requires same-Output nested selection. Such a change must define explicit selection precedence, commit ordering and rollback ownership before implementation; request precedence alone is not sufficient.

