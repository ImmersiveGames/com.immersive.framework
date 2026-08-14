# Player Usage

Status: **Current architecture reconciled; physical lifetime implementation/QA reopen active**  
Last updated: **2026-08-14**  
Decision sources: IF-ADR-003, IF-ADR-012, IF-ADR-015, IF-ADR-016, IF-ADR-019, IF-ADR-020, IF-ADR-021

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
```

## 2. Host Provisioning

Choose one Session-wide acquisition origin:

| Mode | Candidate source | Owner after successful admission |
|---|---|---|
| Scene Provided | exact consumer-authored scene object | Session Player occurrence |
| Manager Provisioned | Framework/PlayerInputManager creates candidate | Session Player occurrence |

The provisioning choice no longer means different post-admission lifetime semantics.

## 3. Scene-Provided

Typical authored candidate:

```text
Player_SceneProvided
  PlayerInput
  LocalPlayerHostAuthoring
  SceneLocalPlayerAdmissionAuthoring
  Actor Mount
    Actor
      PlayerActorDeclaration
      gameplay components
```

Before admission, the scene owns this candidate.

After successful adoption:

```text
same physical object
  -> promoted/migrated to Session-owned runtime scope
```

The Activity that supplied it no longer owns its terminal lifetime.

If admission fails, ownership does not transfer.

## 4. Manager-Provisioned

Manager provisioning creates/provides the candidate through the official explicit Join
path.

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

## 6. No current Activity representation

A valid state is:

```text
Session Player = Joined
Physical Player = Exists
Activity representation = Absent
```

The Player should be inactive/non-participating rather than destroyed.

A later Activity can reactivate/rebind the same physical object.

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

## 8. Initial Placement

Do not interpret every Activity entry as Spawn.

For a continuous Activity-to-Activity handoff:

```text
Preserve Current Pose
```

is the default.

Initial/Activity placement applies only when explicit spatial-start intent requires it,
such as first gameplay introduction or an explicitly authored repositioning transition.

## 9. Leave

`Request Leave` is the explicit individual terminal operation.

```text
validate exact Slot + occurrence
retire current Activity context
release Session-owned admitted physical Player
end Session Player occurrence
Slot -> Vacant / Available
```

This applies to both provisioning origins after successful admission.

## 10. Observation

Useful diagnostics distinguish:

```text
Session occurrence
provisioning origin
physical Player identity
physical owner
physical active/inactive state
Activity representation occurrence
readiness
gameplay admission
camera/context bindings
```

During Activity A -> B, the physical identity should remain the same while the Activity
representation occurrence changes.

## 11. Anti-patterns

Do not add:

- Actor destruction/recreation on ordinary Activity change;
- Scene unload as Player Leave;
- `DontDestroyOnLoad` as authority;
- Scene-Provided permanent external ownership after successful adoption;
- re-Join for Activity reprojection;
- automatic Initial Placement on every Activity entry;
- global Player manager/service locator;
- name/tag/hierarchy lookup as authority;
- silent fallback between provisioning modes.
