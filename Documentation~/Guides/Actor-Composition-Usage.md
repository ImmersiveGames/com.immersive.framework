# Actor Composition Usage

Status: **Experimental — IF-ADR-050**. This guide describes the Actor composition
source model following the ScriptableObject-per-part serialization decision in
IF-SPEC-050 §3.4. The model is present in the current local source and planned
for Framework `1.2.0-preview.4`; its package metadata will advance after signed
Composition `.3` is available in OpenUPM. Consumer import evidence for the
signed package graph remains pending. It is not a Stable API promise.

## What changed

| Area | Previous model | Current model |
|---|---|---|
| Actor visuals | `ActorProfile.presentationPrefab` described one optional visual prefab; the later keyed Base model stored each part inline in the profile. | `ActorProfile` stores an ordered list of references to one ScriptableObject per Base part. The optional visual uses key `visual`. Existing inline and remaining legacy data must be migrated before runtime use. |
| Change one visual or feature | The visual was a single optional Profile field with a dedicated materialization path; there was no shared keyed-owner contract for changing one Actor feature in place. | An owner changes a key in the existing Actor Composer. A successful keyed update preserves the Actor root, pose, `ActorId`, `ActorOccurrenceId`, Player selection and current `PlayerOccurrenceId`. |
| Actor identity | The Profile described selectable intent, but there was no public occurrence reference identifying the current physical Actor instance. | `ActorProfileId` identifies authored intent, `ActorId` identifies the logical Actor, `ActorOccurrenceId` identifies its current physical incarnation, and `PlayerOccurrenceId` identifies the joined Player lifetime. A physical root replacement gets a new `ActorOccurrenceId`. |
| Extra content | No shared owner/priority/dependency contract for independently supplied Actor parts. | Contributions declare keyed parts, owner, priority and dependencies. The Composer resolves winners and dependencies and commits a coherent delta; equal top-priority conflicts and invalid dependency graphs are rejected. |
| Runtime ownership | Visual cleanup was coupled to its dedicated materializer. | Each contributor removes its own intent with `RemovePartAsync` or `CloseAsync`; the Framework materializer releases acquired resources. NonPlayer publication also requires the producer's exact occurrence token and `RuntimeContentOwner`. |
| Camera and world placement | Visual hierarchy could be mistaken for the Actor's physical or spatial root. | The physical Actor root remains the authority for pose and placement. Camera `ObservationTransform` stays outside the replaceable composition mount. |
| Package boundary | Actor visual handling lived in Framework-specific code. | `com.immersive.composition` is the engine-independent keyed composition kernel; `com.immersive.framework` owns Actor/Player lifecycle, identity, Unity prefabs, mounts, materialization and cleanup. Consumers use Framework Actor surfaces rather than creating a second Composer for a Framework Actor. |

## Author Base content on an Actor Profile

1. Open the `ActorProfile` asset and find **Base Content Parts**.
2. Choose **Create Base Content Part Asset**. The Inspector creates a part asset
   in a profile-scoped folder under the editable `Assets/` tree and adds it to
   the ordered list. Select the new asset to edit its key, required prefab and
   dependency keys.
3. Use a unique key. Use `visual` for the optional visual part; use other keys
   for independently replaceable content. Every authored part requires a
   prefab. The profile may omit the `visual` part entirely.
4. Add dependency keys only when this part requires another part to be present.
   The Composer prepares dependencies before dependants and rejects missing keys,
   cycles and unresolved priority conflicts.
5. Reorder the references on the profile to control authored list order. Assign
   the same part asset to another profile only when shared edits are intended;
   edits to that asset affect all profiles that reference it.
6. Keep the Player Actor's `VisualContentMount` as the mount for Base parts. The
   mount is optional when there are no Base parts and no runtime contributions;
   provision it if runtime content may be added later.
7. Keep `PlayerInput`, `ActorDeclaration` and `PlayerActorRuntimeHost` on their
   owning host/Actor objects, not inside a composition-part prefab. Keep the
   Actor's `ActorCameraSubjectAuthoring.ObservationTransform` outside the
   replaceable mount.

For a Scene-Provided Player, author the matching `visual` instance below that
exact `VisualContentMount` and verify its prefab provenance in the Scene-Provided
Local Player Inspector. Scene-Provided adoption currently supports that one Base
`visual` part; additional keyed scene-part bindings are not authored yet.
Framework-owned Actor roots materialize the profile's Base parts through their
existing Composer.

### Migrate existing Actor Profiles

For profiles that still contain inline Base parts or the legacy
`presentationPrefab`, run **Tools > Immersive Framework > Migrate Actor Profile
Visual Content**. The explicit migration validates the complete profile,
creates one asset per part in a profile-scoped folder, saves and verifies the
ordered references, then clears the inline/legacy data. Matching inline and
legacy `visual` values produce one part; conflicting values, invalid dependency
graphs, non-editable locations and mismatched existing generated assets are
reported without overwriting source data. Migration does not infer sharing
between profiles. There is no Runtime fallback for an unmigrated Profile. The
earlier D08 visual migration reports remain recorded in the
[Framework tracker](../Architecture/Tracking/IF-TRACK-Framework.md#if-adr-050-d08-closeout--consumer-actor-profile-migration--2026-10-18); this new serialized-data migration is a separate follow-up gate.

## Change Player Actor content at runtime

The Player contract is Experimental. A contribution targets one exact current
Player and physical Actor occurrence; do not construct identity by parsing
diagnostic strings or cache occurrence IDs across replacement.

When a Framework-scoped integration already holds its live
`IPlayerSessionScopedAccess`, the current Player Slot observation supplies the
`PlayerOccurrenceId`, and an `ActorOccurrenceReferenceBinding` for that Slot
supplies the exact `ActorId` and `ActorOccurrenceId`. The integration can then
open a contribution:

```csharp
var request = new PlayerActorCompositionContributionRequest(
    slot.Slot.PlayerSlotId,
    slot.Slot.PlayerOccurrenceId,
    occurrence.Occurrence.ActorId,
    occurrence.Occurrence.Id,
    priority: 20,
    source: "EquipmentLoadout",
    reason: "Apply active equipment visual");

if (!((IPlayerActorCompositionAccess)sessionAccess).TryOpenContribution(
        request,
        out PlayerActorCompositionContribution contribution,
        out string issue))
{
    throw new InvalidOperationException(issue);
}

PlayerActorCompositionOperationResult result = await contribution.SetPartAsync(
    new PlayerActorCompositionPart("equipment", equipmentPrefab),
    "EquipmentLoadout",
    "Apply active equipment visual");

if (result.Status is not (PlayerActorCompositionOperationStatus.Committed or
    PlayerActorCompositionOperationStatus.NoChange))
{
    throw new InvalidOperationException(result.Issue);
}
```

`SetPartAsync` changes one key while preserving this owner's other keys.
`SetPartsAsync` replaces this owner's complete intent set; `RemovePartAsync`
removes one of this owner's keys. Close the contribution when its feature/scope
ends so its parts are withdrawn. Check the returned status and `Issue`; a stale,
cancelled, rejected or faulted result did not commit the requested change.

**Consumer access limit:** the composition interface is Experimental and is
implemented by the scoped Player access object, but the built-in public
`PlayerSessionObserver` is read-only and does not expose that object. The current
package has no general public component/binding that hands
`IPlayerActorCompositionAccess` to arbitrary game code. The snippet therefore
applies to Framework feature integrations that already receive the scoped port;
it is not a recipe for global lookup or for deriving from an Internal access
consumer. External Player runtime contributions need a supported public binding
before they can use this interface directly.

## Add content to a dynamically produced NonPlayer Actor

For a NonPlayer root, place `ActorDeclaration` and
`ActorOccurrencePublication` on the same root GameObject. Assign an optional
`CompositionMount` on that root or one of its children, outside any camera
observation transform. A mount is required when adding composition parts. The
producer publishes only after its `RuntimeContent` owner has committed. Use the
returned token for the same publication's composition contribution:

```csharp
if (!publicationScope.TryPublish(
        publication, committedOwner, out ActorOccurrencePublicationToken token,
        out string issue))
{
    throw new InvalidOperationException(issue);
}

if (!publicationScope.TryOpenCompositionContribution(
        publication, token, priority: 20,
        source: "QuestPresentation", reason: "Show quest marker",
        out ActorOccurrenceCompositionContribution contribution, out issue))
{
    throw new InvalidOperationException(issue);
}

ActorOccurrenceCompositionOperationResult result = await contribution.SetPartAsync(
    new ActorOccurrenceCompositionPart("quest-marker", markerPrefab),
    "QuestPresentation", "Show quest marker");

if (result.Status is not (ActorOccurrenceCompositionOperationStatus.Committed or
    ActorOccurrenceCompositionOperationStatus.NoChange))
{
    throw new InvalidOperationException(result.Issue);
}
```

NonPlayer parts can also explicitly adopt a Scene-provided instance with
`ActorOccurrenceCompositionPart.AdoptExisting`; Player parts are prefab-backed.
Close a producer's contribution when that producer stops contributing, then
unpublish with the exact publication/token when the producer releases the Actor.
Owner release also invalidates its publication and outstanding work; cleanup
results must be checked rather than treated as successful rollback.

## Rules to keep

- One Composer remains attached to the Actor's existing composition mount. Do not
  instantiate a parallel visual path or replace the Actor root to change a keyed
  part.
- Higher priority wins for a key; equal highest priority is a conflict. Declare
  dependencies by key and treat a rejected result as no commit.
- A Player composition update preserves the current physical occurrence. Actor
  replacement is a separate lifecycle operation and changes
  `ActorOccurrenceId`.
- Camera follows the Actor's explicit observation transform; composition does not
  move that transform or become spatial authority.
- The Actor/Profile, occurrence-reference, publication and contribution APIs in
  this guide are Experimental. Follow
  [API maturity governance](../Architecture/Governance/IF-GOV-001-API-MATURITY-AND-VALIDATION-GOVERNANCE.md)
  and the [public API reference](../API/Public-API.md#player-participation-and-actor).
- Install both packages as described in the [package README](../../README.md).

When Framework `1.2.0-preview.4` is published, it will declare Composition as a
UPM dependency. Consumers will configure the `com.immersive` OpenUPM scope and
declare only Framework in `Packages/manifest.json`; Unity will resolve
Composition transitively and record the exact versions in
`Packages/packages-lock.json`. Composition is not a parallel Actor runtime.
The composition feature remains optional for an Actor Profile with no Base
parts.

Decision: [IF-ADR-050](../Architecture/ADRs/IF-ADR-050-Actor-Occurrence-Lifecycle-Authority-and-Commit.md).
