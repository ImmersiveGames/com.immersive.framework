# IF-ADR-033 — Session Player Post-Admission Convergence and Contextual Binding Authority

Status: **Proposed / implementation not started / QA not certified**  
Proposed: **2026-09-24**  
Type: architecture / runtime authority / Player lifecycle / Activity representation / Scene-Provided admission  
Related decisions: IF-ADR-003, IF-ADR-005, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021, IF-ADR-023, IF-ADR-024, IF-ADR-025  
Normative relationship: **This ADR refines the post-admission implementation boundary of IF-ADR-019. It does not replace IF-ADR-019 or IF-ADR-020.**

## 1. Context

IF-ADR-019 already establishes the intended ownership boundary:

```text
successful Join + admission
        ↓
Session owns the admitted physical Player

Activity / Route
        ↓
own only contextual representation of that existing Session Player
```

It also establishes that:

```text
SceneProvided
and
ManagerProvisioned
```

differ before admission but converge on the same Session-owned physical lifetime after successful admission.

A defect found while exercising Activity Restart with a Scene-Provided Player exposed that the runtime implementation does not yet complete that convergence.

The observed failure is not caused by Activity Restart itself. Restart merely exposed a post-admission representation path that can also affect later Activity re-entry.

## 2. Confirmed defect

The current Scene-Provided sample composition places `SceneProvidedLocalPlayerAuthoring` under the same physical hierarchy as the `LocalPlayerHostAuthoring`.

During first successful Scene-Provided adoption:

```text
Scene candidate
    ↓
TryAdmit
    ↓
CurrentAssignment origin = SceneProvided
    ↓
TryAdoptScenePlayerActor
    ↓
Session physical evidence committed
    ↓
DontDestroyOnLoad(host.gameObject)
```

The Host hierarchy, including the Scene-Provided authoring surface, leaves the original Route / Activity scene.

Later Activity Exit correctly retires only contextual authority:

```text
CurrentAssignment released
contextual Host binding released

PreparationRecord retained
SceneAdoptionRecord retained
physical Host retained
PlayerInput retained
Actor retained
Player remains Joined
```

On a subsequent Activity Enter / Re-enter:

1. `TryResolveAutomaticActivityAuthoring` attempts to discover Scene-Provided candidates from loaded authored scenes.
2. The already-admitted original authoring surface is no longer in its authored scene and is therefore not rediscovered as an Activity / Route candidate.
3. No Scene-Provided contextual assignment is recreated.
4. `TryPrepareSelectedActor` calls `TryEnsureManagerContextualProjection`.
5. With no current assignment, that method creates a new contextual assignment with `ManagerProvisioned`.
6. The retained Session physical Actor evidence still records `SceneProvided`.
7. `TryResolveCurrentActorCorrelation` detects the divergence only after the invalid contextual assignment has already been written.

The invalid state is therefore currently representable:

```text
Session physical provenance = SceneProvided
Contextual AssignmentOrigin = ManagerProvisioned
```

This violates the intended post-admission convergence of IF-ADR-019.

## 3. Root architectural issue

The runtime currently carries physical provisioning provenance into contextual representation state.

Today, conceptually:

```text
Session physical evidence
    ProvisioningOrigin = SceneProvided | ManagerProvisioned

CurrentAssignment
    Owner = Activity | Route
    AssignmentOrigin = SceneProvided | ManagerProvisioned
    AssignmentToken
    HostBindingIdentity
```

This creates two writable representations of the same provenance fact.

The physical provenance is Session-scoped and stable for the admitted physical occurrence.

The contextual binding is Activity / Route scoped and recreated per contextual occurrence.

A contextual occurrence must not decide, infer, rewrite, or duplicate physical provenance.

## 4. Decision

### 4.1 Post-admission convergence is provider-neutral

After successful admission commit:

```text
SceneProvided ───────┐
                     ├──> Session Player
ManagerProvisioned ──┘
```

Normal Activity / Route representation no longer has separate SceneProvided and ManagerProvisioned paths.

The canonical post-admission flow is:

```text
Session Player
    ↓
Activity participation projection
    ↓
contextual Player binding
    ↓
readiness / gameplay / camera / relocation / interaction
```

No normal post-admission Activity / Route path may:

- re-Join the Player;
- re-admit the Player;
- reprovision the physical Host;
- infer a provisioning origin;
- convert SceneProvided to ManagerProvisioned or vice versa;
- depend on the original candidate authoring surface to recreate contextual representation.

### 4.2 Physical provisioning provenance is Session-scoped only

`SceneProvided` / `ManagerProvisioned` remains valid physical provenance.

It is retained only in Session physical evidence such as the canonical prepared Actor / Host evidence.

It may be used for:

- diagnostics;
- validation of physical evidence;
- terminal physical release where the release mechanism genuinely differs;
- explicitly scoped physical operations such as prepared Actor replacement where the contract requires provenance-aware behavior.

It is not contextual Activity / Route state.

### 4.3 Contextual binding becomes provider-neutral

A contextual binding represents only:

```text
which Session Player
is represented by
which Activity / Route occurrence
under which binding token
```

Target conceptual shape:

```text
Player contextual binding
├── PlayerSlot / Session Player occurrence identity
├── Activity / Route owner
├── contextual token
└── HostBindingIdentity
```

It does not contain writable physical provisioning provenance.

Therefore the current `AssignmentOrigin` field is removed from the contextual assignment model.

Any diagnostic that needs provisioning provenance reads it from Session physical evidence instead of duplicating it into the contextual binding.

### 4.4 CurrentAssignment remains contextual authority, not physical authority

This ADR does not require a new global `SessionPlayer` manager or service.

The existing Session authorities remain responsible for their current domains:

```text
PlayerParticipationRuntimeContext
    Session membership / Slot / Actor selection

PlayerActorPreparationRuntimeContext
    prepared physical Actor evidence
    Scene adoption evidence

PlayerHostEvidenceProjection
    Session physical Host evidence
    current contextual Host correlation
```

The contextual-assignment storage may remain within `PlayerParticipationRuntimeContext` during this refactor provided its API becomes provider-neutral and cannot encode physical provenance.

Extraction into a separate runtime is not required unless implementation proves a real ownership conflict after the provider-specific state is removed.

### 4.5 SceneProvidedLocalPlayerAuthoring is candidate-only after this decision

`SceneProvidedLocalPlayerAuthoring` is an admission surface.

Its responsibilities are:

```text
candidate discovery
candidate validation
initial Slot / Actor intent correlation
initial Join / admission where required
physical Scene Actor adoption
admission commit
conflicting candidate detection
```

After successful admission commit it is not required to recreate normal Activity representation.

A later Activity representation resolves admitted Players from Session state and the Activity participation projection.

The original Scene-Provided authoring surface may remain alive because of Unity hierarchy / physical lifetime, but it is no longer semantic authority for contextual re-entry.

### 4.6 Activity participation intent remains Activity-owned

The canonical source for which Players an Activity represents remains:

```text
ActivityAsset
    PlayerParticipationProjectionMode
    Explicit Slot Profiles
    Zero Participant Policy
    PlayerParticipationRequirementLevel
        ↓
ActivityPlayerParticipationProjectionResolver
        ↓
current Session Player Slots
```

Scene-based candidate discovery does not replace or augment this authority after admission.

### 4.7 Later SceneProvided candidates remain validated as candidates

Removing Scene-Provided authoring from normal re-entry does not permit silent duplicate candidates.

If a later Activity / Route scene declares a new Scene-Provided candidate for a Slot whose Session Player is already admitted:

```text
existing Session Player
    +
new SceneProvided candidate
        ↓
validate as redundant / conflicting candidate evidence
        ↓
report or reject according to the existing Scene-Provided admission contract
        ↓
never replace the Session Player silently
```

Candidate discovery may therefore still run for conflict detection.

It must not be used to reconstruct the contextual binding of an already-admitted Player.

## 5. Target lifecycle

### 5.1 First Manager-Provisioned admission

```text
Join request
    ↓
provision Host / PlayerInput
    ↓
select / prepare Actor
    ↓
commit Session physical evidence
    ↓
Session Player admitted
    ↓
create contextual binding when projected
```

### 5.2 First Scene-Provided admission

```text
discover Scene candidate
    ↓
validate composition
    ↓
Join / admission
    ↓
adopt exact physical Host / Actor
    ↓
promote physical hierarchy to Session lifetime
    ↓
commit Session physical evidence
    ↓
Session Player admitted
    ↓
create contextual binding when projected
```

### 5.3 Later Activity Enter

For both provisioning origins:

```text
Activity participation projection
    ↓
resolve current Joined Session Players
    ↓
ensure physical Player evidence exists as required
    ↓
create provider-neutral contextual binding
    ↓
gameplay / readiness / camera / relocation
```

There is no post-admission `SceneProvided -> reprojection` admission path.

### 5.4 Activity Exit

```text
release gameplay / readiness / camera / contextual references
    ↓
release contextual binding
    ↓
Session Player remains Joined
physical Host remains
PlayerInput remains
selected Actor intent remains
prepared Actor remains
physical provenance remains
```

### 5.5 Activity Restart

```text
Reset
    ↓
Activity Clear
    ↓
contextual binding released
    ↓
same Session Player remains
    ↓
Activity Re-enter
    ↓
new provider-neutral contextual binding
```

Restart is not Join, Leave, admission or reprovisioning.

### 5.6 Route transition / restart

Route operations may retire and create contextual participation / placement authority.

They do not mutate Session Player physical lifetime merely because Route context changes.

### 5.7 Leave / Session termination

These remain terminal Session boundaries under IF-ADR-020.

They may release:

```text
contextual binding
prepared Actor
physical Host
PlayerInput / device association where applicable
Session occurrence
```

in the order required by the existing Leave / termination contracts.

### 5.8 Actor replacement

IF-ADR-024 remains authoritative.

Actor replacement may modify the prepared physical Actor within the same Session Player occurrence without converting Activity lifecycle into Join / Leave / readmission.

## 6. Required implementation changes

The implementation plan is intentionally breaking. Compatibility with the current post-admission assignment-origin model is not required.

### PLAYER-033-A — contextual assignment model becomes provider-neutral

Refactor:

- `PlayerParticipationRuntimeContext.CurrentAssignment`;
- `CurrentAssignmentRecord`;
- `PlayerSlotAssignmentSnapshot`;
- `BeginAssignment`;
- assignment result / diagnostic structures as required.

Remove contextual `AssignmentOrigin`.

`BeginAssignment` no longer accepts SceneProvided / ManagerProvisioned as caller-selected contextual state.

The single writer can therefore no longer materialize an origin that disagrees with Session physical evidence.

### PLAYER-033-B — Host contextual projection stops duplicating provenance

Refactor `PlayerHostEvidenceProjection`.

Keep Session physical provenance in the immutable physical portion of the retained Host evidence.

Contextual projection retains only contextual token / binding information needed to correlate the current Activity / Route occurrence.

Remove contextual `AssignmentOrigin` correlation.

### PLAYER-033-C — Actor correlation becomes provider-neutral

Refactor:

- `PlayerActorPreparationRuntimeContext.TryEnsureManagerContextualProjection`;
- `PlayerActorPreparationRuntimeContext.TryResolveCurrentActorCorrelation`;
- related call sites and diagnostics.

Rename the contextual ensure operation to a provider-neutral name, for example:

```text
TryEnsureContextualProjection
```

The operation:

1. resolves the existing Session physical Player evidence;
2. ensures one contextual binding for the current Activity / Route owner;
3. validates Host / token / binding correlation;
4. never chooses provisioning origin.

### PLAYER-033-D — SceneProvided admission becomes initial-candidate authority

Refactor:

- `SceneLocalPlayerAdmissionRuntime.TryAdmit`;
- `SceneLocalPlayerAdmissionActivityLifecycleRuntime.TryEnter`;
- `SceneLocalPlayerAdmissionCompositeLifecycleParticipant`;
- related host-module discovery / conflict handling.

Remove SceneProvided admission as the mechanism used to recreate contextual representation for an already-admitted Player.

The existing "Joined Slot -> SceneProvided reprojection" branch is removed or reduced to explicit redundant/conflicting-candidate validation as appropriate.

Candidate discovery remains available to detect newly authored conflicting Scene-Provided candidates.

### PLAYER-033-E — contextual representation uses Activity participation projection

Confirm and integrate the canonical path through:

- `ActivityAsset`;
- `ActivityPlayerParticipationProjectionResolver`;
- `ActivityPlayerActorLifecycleParticipant`.

Every projected admitted Player follows the same contextual-binding path independently of physical provisioning provenance.

### PLAYER-033-F — Restart / transition failure propagation hardening

Activity Restart is not the cause of the provenance bug.

However, a committed Activity re-entry must not report a misleading nominal success if mandatory Player contextual representation failed.

Review:

- `FrameworkActivityRestartFlowResult`;
- `RestartActivityAsync`;
- Activity readiness / Player contextual failure propagation.

This cut is hardening and observability, not the architectural fix for provenance convergence.

## 7. Dependency direction

Target dependency direction:

```text
Session Player state / physical evidence
        ↑ read
Activity participation projection
        ↓
contextual binding authority
        ↓
Activity gameplay / readiness / camera / relocation
```

Activity / Route contextual code does not receive authority to:

- Join;
- Leave;
- terminate Session Player;
- release Session physical Host;
- reprovision PlayerInput;
- adopt a Scene Actor;
- choose physical provisioning provenance.

No service locator, global Player singleton or mutable global registry is introduced.

No provenance callback / compatibility bridge is required because contextual state no longer duplicates provenance.

## 8. Invariants

### 8.1 Enforced by model / API

After admission commit:

- Activity / Route cannot choose `SceneProvided` or `ManagerProvisioned`.
- Contextual binding cannot encode a provisioning origin.
- Activity transition cannot create a second Session Player occurrence.
- Activity Restart cannot Join or Leave the Player.
- Reset cannot mutate Session Player lifecycle unless an explicit Session command is invoked separately.
- candidate authoring cannot silently replace an admitted Session Player.
- Activity participation intent comes from Activity authoring, not candidate scene discovery.
- contextual re-entry does not require the original SceneProvided authoring surface.

### 8.2 Validated at runtime

Runtime validation still proves:

- current Slot is Joined;
- required Session physical Host exists;
- Host belongs to the expected Slot;
- PlayerInput evidence remains available where applicable;
- prepared Actor evidence is current;
- current contextual token belongs to the expected Activity / Route owner;
- Host binding identity matches the current contextual binding;
- stale contextual tokens are rejected;
- later conflicting Scene-Provided candidates are reported / rejected.

## 9. Rejected alternatives

### 9.1 Keep AssignmentOrigin and validate it against physical provenance

Rejected.

This preserves two representations of the same fact:

```text
physical provenance
contextual copied provenance
```

and requires a dependency from the contextual writer back into physical evidence merely to keep the duplicate synchronized.

The invalid state should be unrepresentable rather than detected after or during assignment creation.

### 9.2 Inject physical-provenance lookup into BeginAssignment

Rejected for the first implementation.

`PlayerParticipationRuntimeContext` is already a dependency of physical preparation / Host evidence. Making it query those higher-level components introduces an avoidable reverse dependency or a compatibility-style bridge.

Removing contextual provenance eliminates the need for that dependency.

### 9.3 Re-discover the original SceneProvided authoring after admission

Rejected.

Successful adoption explicitly promotes the physical Player out of Activity-scene lifetime.

Normal contextual re-entry must not depend on scene location or rediscovery of an admission candidate.

### 9.4 Fix only TryEnsureManagerContextualProjection

Rejected as incomplete.

Changing the fallback to SceneProvided would repair one call path while preserving duplicated provenance and provider-specific post-admission representation.

### 9.5 Make Activity Restart use the normal Activation Gate as the provenance fix

Rejected as causal fix.

The Activation Gate does not perform SceneProvided contextual reprojection. Restart / Request uniformity may be considered separately for failure propagation or transition consistency.

### 9.6 Introduce a global SessionPlayer manager / service locator

Rejected.

Existing domain authorities already own Session membership, physical preparation and Host evidence. The defect is post-admission contextual convergence, not absence of a global Player object.

## 10. QA certification plan

No implementation cut is certified by static review alone.

QA runs in the QAFramework environment.

### 10.1 Mandatory invariants across non-terminal operations

For both ManagerProvisioned and SceneProvided, prove across:

```text
Activity Exit -> Enter
Activity Restart
Activity A -> B -> A
Route transition
Route transition back
Reset
Cycle Reset
```

that:

```text
Session Player occurrence unchanged
PlayerSlotId unchanged
Slot remains Joined
Host ReferenceEquals before / after
PlayerInput ReferenceEquals before / after
device ownership unchanged where applicable
Actor selection unchanged
prepared Actor occurrence / token unchanged
physical provisioning provenance unchanged
fresh contextual binding belongs to the new Activity / Route occurrence
```

### 10.2 SceneProvided independence contract

After first successful SceneProvided admission:

```text
Activity representation must no longer depend on
the original SceneProvidedLocalPlayerAuthoring surface.
```

Focused QA must prove re-entry / restart using only Session physical evidence plus Activity participation intent.

The test may disable or otherwise make the original admission surface unavailable while preserving the admitted physical Host, then prove successful provider-neutral contextual re-entry.

The proof must validate correct binding and control, not merely absence of exceptions.

### 10.3 Conflicting candidate contract

With an admitted SceneProvided Player already present:

```text
later Activity / Route declares a new SceneProvided candidate for same Slot
    ↓
explicit redundant/conflict diagnostic
    ↓
no replacement
no re-Join
no physical handoff
no second Actor
```

### 10.4 Negative assignment contract

There is no API path capable of expressing:

```text
physical provenance = SceneProvided
contextual provenance = ManagerProvisioned
```

because contextual provenance no longer exists.

### 10.5 Leave / termination

Re-run IF-ADR-020 coverage to prove the refactor does not weaken:

- Leave with current Activity;
- Leave without current Activity;
- SceneProvided terminal physical release;
- ManagerProvisioned terminal physical release;
- Session termination cleanup;
- rejoin creates a fresh Session occurrence.

### 10.6 Actor replacement

Re-run IF-ADR-024 coverage.

Actor replacement must remain an explicit physical operation and must not create a new Session Player occurrence or contextual admission path.

## 11. Documentation reconciliation

Implementation of this ADR requires reconciliation of IF-ADR-019 documentation where current text or implementation notes imply SceneProvided contextual reprojection through repeated admission.

IF-ADR-019 ownership rules remain valid:

```text
Session owns admitted physical Player lifetime.
Activity owns contextual representation.
SceneProvided and ManagerProvisioned converge after admission.
```

IF-ADR-033 makes that convergence operationally strict.

Historical QA claims such as SceneProvided A -> B -> A must be rechecked against the stronger invariant:

```text
physical identity preserved
AND
contextual representation recreated provider-neutrally
AND
no post-admission candidate dependency
```

Physical-identity-only proof is insufficient for PLAYER-033 certification.

## 12. Completion criteria

IF-ADR-033 may move to **Implemented** only when PLAYER-033-A through PLAYER-033-E are integrated.

It may move to **QA Certified** only when:

```text
ManagerProvisioned matrix                     PASS
SceneProvided matrix                          PASS
Activity Restart                              PASS
Activity A -> B -> A                          PASS
Route transition / return                     PASS
Reset / Cycle Reset invariants                PASS
SceneProvided authoring independence           PASS
conflicting later candidate                    PASS
IF-ADR-020 Leave / termination regression      PASS
IF-ADR-024 Actor replacement regression        PASS
canonical QA cleanup                           PASS
```

PLAYER-033-F is required if investigation confirms restart / re-entry result propagation can still report nominal success while mandatory contextual representation failed.

## 13. Expected outcome

The final post-admission model becomes:

```text
                 FIRST ADMISSION ONLY

SceneProvided candidate ─┐
                         ├──> Session Player physical occurrence
ManagerProvisioned ──────┘
                                      |
                                      | immutable physical provenance
                                      |
                                      v
                         Session physical evidence
                                      |
                                      | provider-neutral projection
                                      v
                         Activity / Route contextual binding
                                      |
                    ┌─────────────────┼─────────────────┐
                    v                 v                 v
                gameplay          readiness           camera
                                      |
                                      v
                                  relocation
```

The essential invariant is:

> Once a physical Player occurrence has been admitted to the Session, its provisioning origin is physical provenance only. Activity and Route representation operate on the admitted Session Player and cannot reinterpret, duplicate or mutate that provenance.
