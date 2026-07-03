# F44 ADR RUNTIME OBJECT 001 — Runtime Object Participation Foundation

## Status

Accepted for `v1.0.0-preview.12`.

## Context

The framework now supports Object Reset, Object Reset Group, Reset Selection Policy and Activity Restart. These flows were validated with scene-authored `ObjectEntryDeclaration` and serialized `ObjectResetUnityParticipantSource` references.

That is sufficient for static scenes, but weak for gamefirst gameplay. A first playable game needs objects that are instantiated or enabled at runtime: player prefabs, pickups, hazards, doors and activity-local objects. These objects must be visible to the same reset/restart policy without requiring pre-existing scene declarations.

## Decision

Add a small runtime participation layer:

```text
UnityRuntimeObjectParticipationAdapter
→ resolves current Route/Activity/Session owner
→ registers a RuntimeRegistered ObjectEntry descriptor
→ supplies local ObjectReset participants
→ invalidates/refreshes the ObjectEntry runtime context
→ unregisters on disable/destroy
```

This is not a `PlayerActor`, spawner, pool, save identity, presentation system, camera binding system or gameplay lifecycle system.

## Runtime shape

The Application Runtime owns a `RuntimeObjectParticipationRegistry`. The registry contributes descriptors to the existing ObjectEntry runtime snapshot and also acts as an ObjectReset participant source.

Scene-authored reset participant sources and runtime-registered participants are resolved through a composite participant source, so runtime participation does not replace scene-authored reset authoring.

## Scope

Included:

- Runtime object registration/unregistration.
- Runtime `ObjectEntryDescriptor` with `ObjectEntrySourceKind.RuntimeRegistered`.
- Reset participant contribution for runtime objects.
- Owner binding to current Session/Route/Activity identity.
- Snapshot invalidation/refresh on registration changes.

Excluded:

- PlayerActor.
- Actor lifecycle.
- Official spawn manager.
- Pooling integration.
- Save/progression identity.
- Addressables materialization.
- Camera/input ownership.

## Acceptance

A runtime-instantiated object can:

1. Register a runtime ObjectEntry.
2. Provide ObjectReset participants.
3. Appear in Reset Selection Policy.
4. Reset through ObjectResetGroup / ActivityRestart.
5. Unregister on destroy/disable so stale entries are not reset later.
