# Player Usage

Status: **Current architecture implemented / Full Player QA certified**  
Last updated: **2026-08-16**  
Decision sources: IF-ADR-003, IF-ADR-007, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021  
Certification record: [Player Physical Lifetime Recertification — 2026-08-15](../Architecture/Reconciliation/IMMERSIVE-FRAMEWORK-PLAYER-PHYSICAL-LIFETIME-RECERTIFICATION-2026-08-15.md)

## 1. Product model

Keep these layers separate:

```text
Player Slot
  stable logical seat

Session Player occurrence
  one joined occurrence/revision in that Slot

Host Provisioning
  how the physical candidate is initially supplied

Actor Selection
  Session intent selecting ActorProfile

Admitted Physical Player
  Session-owned runtime physical representation after successful admission

Activity Representation
  contextual activation / gameplay / camera / readiness / bindings

Activity RuntimeContent
  Activity-owned scope while that Activity is current
  not Player physical ownership
```

## 2. Host Provisioning

Choose one Session-wide acquisition origin:

| Mode | Candidate source | Owner after successful admission |
|---|---|---|
| Scene Provided | exact consumer-authored scene object | Session Player occurrence |
| Manager Provisioned | Framework/PlayerInputManager creates candidate | Session Player occurrence |

The provisioning choice does not imply different post-admission lifetime semantics.

## 3. Scene-Provided

Scene-Provided authoring keeps consumer intent separate from deterministic Actor materialization.

The consumer authors the Player Host and selects the exact Player Slot and Actor Profile:

```text
Player_SceneProvided
  PlayerInput
  LocalPlayerHostAuthoring
  SceneLocalPlayerAdmissionAuthoring
    Player Slot
    Actor Profile
    Admission Timing
    Initial Placement
  Actor Mount

ActorProfile
  Logical Actor Host Prefab
```

`ActorProfile.LogicalActorHostPrefab` is the single authored prefab authority for the Logical Actor. The consumer does not author a second Scene Actor prefab authority.

`Apply / Rebuild` derives the canonical Logical Actor prefab from the selected Actor Profile and materializes or preserves the matching prefab instance under the exact `Actor Mount`:

```text
Player_SceneProvided
  PlayerInput
  LocalPlayerHostAuthoring
  SceneLocalPlayerAdmissionAuthoring
  Actor Mount
    Logical Actor Host [prefab instance]
      PlayerActorDeclaration
      gameplay components
```

The Scene Actor reference is derived technical evidence. `Apply / Rebuild` binds the exact `PlayerActorDeclaration` from the matching prefab instance; consumers do not need to manually instantiate the Actor prefab or manually assign the declaration.

Materialization is deterministic and non-destructive:

```text
Actor missing
  -> instantiate ActorProfile.LogicalActorHostPrefab under Actor Mount

matching Actor already present
  -> preserve and bind it

mismatched, unpacked or conflicting Actor content
  -> reject explicitly
  -> do not silently replace or destroy consumer content
```

Before admission, the scene owns this candidate.

After successful adoption:

```text
same physical object
  -> promoted/migrated to Session-owned runtime scope
```

The Activity that supplied it no longer owns its terminal lifetime.

If admission fails, Player ownership does not transfer.

## 4. Manager-Provisioned

Manager provisioning creates/provides the candidate through the official explicit Join path.

After successful admission, the physical Player is Session-owned.

## 5. Activity transitions

Normal Activity transition is contextual handoff:

```text
Activity A
  same physical Player
  context A
        ↓
release A gameplay/camera/readiness/bindings
        ↓
same physical Player remains
        ↓
Activity B
  new context B
```

Do not destroy/recreate the physical Actor for normal Activity transition.

Do not re-Join the Player.

This applies to seamless and non-seamless presentation modes.

The certified SceneProvided A -> B -> A path preserves the exact physical identity and ordinary gameplay pose while creating fresh contextual authority for each Activity occurrence.

## 6. No current Activity representation

A valid state is:

```text
Session Player = Joined
Physical Player = Exists
Activity representation = Absent
```

The Player should be inactive/non-participating rather than destroyed.

A later Activity can reactivate/rebind the same physical object.

### Observation rule

Do not resolve this physical truth from a current Activity reference or hierarchy shape.

Use the canonical Session/occurrence physical preparation evidence. `Contextual=Absent` means no current contextual authority; it does not mean the Session-owned physical Actor was destroyed.

Do not use:

```text
childCount
hierarchy shape
scene scan
FindObjectOfType*
first compatible Actor
```

as lifetime authority.

## 7. Activity participation

Activity policy decides whether the Joined Player participates now.

Excluded:

```text
Slot remains Joined
physical Player remains
Activity representation inactive/absent
```

Included:

```text
Activity creates new contextual representation occurrence
same physical Player is activated/bound
new readiness evidence is required
```

A failed contextual reprojection can leave the target Activity current and `NotReady` while Player contextual admission is absent. If that Activity owns a RuntimeContent root, the root remains until Activity exit/release; it is not a physical Player handoff.

## 8. Initial Placement

Do not interpret every Activity entry as Spawn.

For a continuous Activity-to-Activity handoff:

```text
Preserve Current Pose
```

is the default.

Initial/Activity placement applies only when explicit spatial-start intent requires it, such as first gameplay introduction or an explicitly authored repositioning transition.

First gameplay activation may require a valid current-Activity IF-ADR-021 placement gate. Do not bypass or fabricate that gate.

Dedicated Initial Placement QA is certified `9/9`.

## 9. Leave

`Request Leave` is the explicit individual terminal operation.

```text
validate exact Slot + occurrence
retire current Activity context when present
release Session-owned admitted physical Player
end Session Player occurrence
Slot -> Vacant / Available
```

This applies to both provisioning origins after successful admission.

A no-Activity Leave is valid. It does not require a fabricated Activity to resolve the retained physical Player.

## 10. Session termination

Session termination releases all remaining Session-owned admitted physical Players.

The certified matrix includes Manager-Provisioned termination and SceneProvided termination while no current Activity representation exists.

## 11. Route commit versus Activity readiness

Do not treat these as the same result.

A valid failed startup state may be:

```text
Route Request = Succeeded
current Activity = target Activity
ActivityState = Active
ActivityReadiness = NotReady
ActivityTransition = CommittedNotReady
blockingIssues > 0
```

The Route committed. The Activity did not become Ready.

When diagnosing Player failures, inspect Activity readiness/content evidence instead of interpreting Route success as Player admission success.

## 12. Observation

Useful diagnostics distinguish:

```text
Session occurrence
provisioning origin
physical Player identity
physical preparation token/evidence
physical owner
physical active/inactive state
Activity representation occurrence
Activity RuntimeContent owner
readiness
gameplay admission
camera/context bindings
```

During Activity A -> B, the physical identity should remain the same while the Activity representation occurrence changes.

Negative-path failure evidence belongs to its owning layer. Do not fabricate a public admission result when a failure occurred before such a public operation existed and is already exposed through canonical Activity lifecycle/readiness evidence.

## 13. Technical certification

The current Player boundary is certified by:

```text
[QA_PLAYER_FULL]
status='Completed'
verdict='PLAYER QA CERTIFIED'
mandatoryContracts='25'
executedContracts='25'
passedContracts='25'
```

The terminal matrix includes:

```text
SceneProvided identity/pose continuity
SceneProvided Leave with Activity
SceneProvided Leave without Activity
SceneProvided Session termination
Manager Provisioned / no-Activity / termination
Public Surface
Leave / rejoin / stale occurrence safety
Failed First Scene Adoption
Failed Contextual Reprojection
No Physical Handoff
```

## 14. Anti-patterns

Do not add:

- Actor destruction/recreation on ordinary Activity change;
- Scene unload as Player Leave;
- `DontDestroyOnLoad` as authority;
- Scene-Provided permanent external ownership after successful adoption;
- re-Join for Activity reprojection;
- automatic Initial Placement on every Activity entry;
- Activity-owned RuntimeContent treated as Player physical ownership;
- global Player manager/service locator;
- name/tag/hierarchy lookup as authority;
- silent fallback between provisioning modes.
