# P3M4B1 — Scene Local Player Admission Transaction

Status: Validated in Unity — PASS 10/10  
Type: Technical product foundation  
Depends on: P3M4A PASS

## Objective

Promote the first runtime authority behind `SceneLocalPlayerAdmissionAuthoring` without
creating a second Player gameplay lane.

This cut proves only:

```text
explicit Activity-scene authoring surface
-> Session-authorized exact ordered Slot reservation
-> externally owned Local Player Host admission
-> typed admission token
-> transactional release
-> Slot Available again
```

## Product surface affected

`SceneLocalPlayerAdmissionAuthoring` now exposes explicit Play Mode operations:

```text
RequestAdmission(source, reason)
RequestRelease(source, reason)
RequestRelease(expectedToken, source, reason)
```

The custom Inspector shows:

```text
Admit Now
Release Now
Runtime Ready
Active Admission
Runtime Status
Admission Token
```

No Unity lifecycle callback admits a Player automatically in P3M4B1.

## Runtime authority

`SceneLocalPlayerAdmissionRuntime` is a plain C# Session-scoped transaction owner.
`SceneLocalPlayerAdmissionRuntimeHostModule` is the scoped Unity composition adapter
attached directly to the existing `FrameworkRuntimeHost` from the Session participation
authority. Composition does not depend on `LocalPlayerProvisioningAuthoring`, a
`PlayerInputManager`, or the provisioned join path.

The module discovers only `SceneLocalPlayerAdmissionAuthoring` declarations inside each
loaded scene root. It does not discover Players, Hosts or Actors by name, tag, hierarchy
convention or static registry. Every functional reference still comes from the surface.

## Ordered Slot rule

The surface names an exact `PlayerSlotProfile`, but the Session still enforces canonical
configured order.

```text
requested Slot == current first Available configured Slot
  -> reserve

requested Slot != current first Available configured Slot
  -> reject explicitly
  -> do not allocate a fallback
  -> do not strand a reservation
```

Scene-authorized admission is independent from the public Press Start join gate. It may
reserve while `JoiningOpen == false`, because the Activity/product surface is the explicit
authorization source. Dynamic capacity and Slot state remain mandatory.

## Admission transaction

```text
validate explicit evidence
reserve exact ordered Slot
stage Host admission allowing the exact scene Actor
commit Slot Reserved -> Joined
commit Host evidence
publish typed SceneLocalPlayerAdmissionToken
```

The admission token identifies the Session admission record rather than freezing the
Slot's general revision forever. Later Actor-selection revisions do not invalidate the
same active admission; exact runtime record and Slot state still reject foreign/stale use.

Failure compensation:

```text
Host stage failure
  -> Reserved -> Available

Slot commit failure
  -> rollback Host stage
  -> Reserved -> Available

Host commit failure after Slot commit
  -> rollback Host stage
  -> Joined -> Available through explicit compensation
```

## Release transaction

```text
validate exact token and Host evidence
Joined -> Leaving
release Host admission evidence
Leaving -> Available
remove runtime binding record
```

If Host release fails:

```text
Leaving -> Joined
retain/update typed admission token
```

If final Slot release fails after Host release:

```text
rollback Leaving -> Joined
restore Host committed admission
report primary and compensation status explicitly
```

The physical Host, `PlayerInput`, Actor Mount and scene Logical Actor are never destroyed
or disabled by this cut.

## Out of scope

```text
On Activity Enter automatic execution
Activity exit ordering
ActorProfile selection
adopting the scene Actor into PlayerActorPreparationRuntimeContext
input binding
occupancy
camera eligibility/publication
gameplay admission
multi-binding Activity rollback
FIRSTGAME integration
```

Those belong to P3M4B2 after this transaction passes Unity QA.

## Technical acceptance

```text
compiles in framework and QA
P3M4B1 smoke PASS 10
P3M4A smoke PASS 7
C9M PASS 6
C9R PASS 11
P3 canonical PASS 31
no Missing Script
no Slot stranded in Reserved or Leaving
no external Host/Actor destruction or active-state mutation
```

## Product acceptance for this subcut

```text
surface shows explicit runtime state in Advanced / Debug
manual Play Mode transaction can be invoked from Inspector
failure is explicit and diagnostic
no silent Slot fallback
physical scene ownership is preserved
```

## Suggested commit

```text
Feat: add Scene Local Player admission transaction
```
