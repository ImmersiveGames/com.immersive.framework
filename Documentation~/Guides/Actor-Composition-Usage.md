# Actor Composition Usage

Status: **Experimental — IF-ADR-050**. This guide describes the Actor composition
source model following the ScriptableObject-per-part serialization decision in
IF-SPEC-050 §3.4. The model is present in Framework `master`, whose current
source candidate is `1.2.0-preview.4`; that candidate is not tagged or released.
Composition `.3` has a signed GitHub Release, but OpenUPM registry installation
has not been confirmed. These Experimental surfaces are not a Stable API
promise.

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

The Player contribution contracts and `PlayerActorCompositionAccess` component
are Experimental. Add the access component to the consumer's Route or Activity
content and set its **Scope** to that same owner scope. The Framework binds it
to that exact Route/Activity occurrence and releases it with the scope. Each
opened contribution is validated against the exact current Player and Actor
occurrence IDs in its request, so a retained contribution cannot mutate a
replaced Actor. Pair it with the public read-only `PlayerSessionObserver` and
an `ActorOccurrenceReferenceBinding` configured for the same Player Slot. Do
not construct identity by parsing diagnostic strings or cache occurrence IDs
across Actor replacement.
The gameplay component can hold a serialized reference to
`PlayerActorCompositionAccess`; it does not implement or inherit an Internal
Framework consumer type.

This example opens a contribution for the exact joined Player and physical
Actor currently observed, then applies and removes only the `visual` key:

```csharp
if (!sessionObserver.TryGetObservation(
        out PlayerSessionScopedObservationSnapshot observation))
{
    throw new InvalidOperationException(sessionObserver.Diagnostic);
}

PlayerSessionScopedSlotObservation slot = default;
for (int index = 0; index < observation.Slots.Count; index++)
{
    PlayerSessionScopedSlotObservation candidate = observation.Slots[index];
    if (candidate.Slot.PlayerSlotId == playerSlot.PlayerSlotId)
    {
        slot = candidate;
        break;
    }
}

if (!slot.IsJoined || !slot.Slot.PlayerOccurrenceId.IsValid)
    throw new InvalidOperationException("The configured Player Slot is not joined.");

if (!actorReference.TryGetCurrent(out ActorOccurrenceResolution currentActor) ||
    !currentActor.Succeeded)
    throw new InvalidOperationException(currentActor.Diagnostic);

var request = new PlayerActorCompositionContributionRequest(
    slot.Slot.PlayerSlotId,
    slot.Slot.PlayerOccurrenceId,
    currentActor.Occurrence.ActorId,
    currentActor.Occurrence.Id,
    priority: 20,
    source: "AppearanceSelection",
    reason: "Apply Petra's alternate visual");

if (!compositionAccess.TryOpenContribution(
        request,
        out PlayerActorCompositionContribution contribution,
        out string issue))
    throw new InvalidOperationException(issue);

PlayerActorCompositionOperationResult applied = await contribution.SetPartAsync(
    new PlayerActorCompositionPart("visual", visualPrefab),
    "AppearanceSelection",
    "Apply Petra's alternate visual");
RequireCommitted(applied.Status, applied.Issue);
visualContribution = contribution; // Mantenha este handle até a rotina de restauração.

PlayerActorCompositionOperationResult restored = await visualContribution.RemovePartAsync(
    "visual",
    "AppearanceSelection",
    "Restore the profile's visual");
RequireCommitted(restored.Status, restored.Issue);

static void RequireCommitted(
    PlayerActorCompositionOperationStatus status,
    string issue)
{
    if (status is not (PlayerActorCompositionOperationStatus.Committed or
        PlayerActorCompositionOperationStatus.NoChange))
        throw new InvalidOperationException(issue);
}
```

`SetPartAsync` changes one key while preserving this owner's other keys.
`SetPartsAsync` replaces this owner's complete intent set; `RemovePartAsync`
removes one of this owner's keys. Close the contribution when its feature/scope
ends so its parts are withdrawn. Check the returned status and `Issue`; a stale,
cancelled, rejected or faulted result did not commit the requested change.

## Change a NonPlayer Actor already materialized by an Activity

For a static Actor already included in Activity content, keep
`ActorDeclaration` and `ActorOccurrencePublication` on the same physical Actor
root and assign its explicit `CompositionMount`. The scene lifecycle binds the
publication to the active Framework composition. Once the Activity owner has
committed and that lifecycle binding is active, open the contribution directly
on the root component; this path does not require an
`ActorOccurrencePublicationScope` or dynamic publication token.

If the Activity-authored visual is the previous composition to restore, author
that instance below the mount and adopt it as a lower-priority contribution.
Then a temporary higher-priority contribution can replace and remove the same
`visual` key. Reuse the `RequireCommitted` helper from the Player example to
reject results that did not commit:

```csharp
if (!publication.TryOpenCompositionContribution(
        priority: -10,
        source: "ActivityActorBase",
        reason: "Register the Activity-authored visual",
        out ActorOccurrenceCompositionContribution baseContribution,
        out string issue))
    throw new InvalidOperationException(issue);

ActorOccurrenceCompositionOperationResult baseResult = await baseContribution.SetPartAsync(
    ActorOccurrenceCompositionPart.AdoptExisting("visual", authoredVisual),
    "ActivityActorBase",
    "Adopt the Activity-authored visual");
RequireCommitted(baseResult.Status, baseResult.Issue);

if (!publication.TryOpenCompositionContribution(
        priority: 20,
        source: "QuestPresentation",
        reason: "Show the temporary Actor visual",
        out ActorOccurrenceCompositionContribution temporaryVisual,
        out issue))
    throw new InvalidOperationException(issue);

ActorOccurrenceCompositionOperationResult applied = await temporaryVisual.SetPartAsync(
    new ActorOccurrenceCompositionPart("visual", temporaryVisualPrefab),
    "QuestPresentation",
    "Apply the temporary Actor visual");
RequireCommitted(applied.Status, applied.Issue);

ActorOccurrenceCompositionOperationResult restored = await temporaryVisual.RemovePartAsync(
    "visual",
    "QuestPresentation",
    "Restore the Activity-authored visual");
RequireCommitted(restored.Status, restored.Issue);
```

`ActorOccurrenceCompositionOperationResult` reports the exact
`ActorOccurrenceId`, `RuntimeContentOwner`, composition revision, status and
issue for each operation. The physical root and occurrence remain the same;
the adopted object remains owned by the Activity.

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
- For the released OpenUPM graph, install Framework only after the required
  Framework and Composition versions are available there; Framework declares
  Composition transitively. For Framework development from Git, list Framework,
  Composition and its other custom Git dependencies directly in the project
  manifest as described in the [package README](../../README.md). Unity's lock
  file records exact commits, so a default-branch Git URL does not update itself.

Framework `1.2.0-preview.4` already declares Composition `.3` as a UPM package
dependency. The registry path will use the `com.immersive` OpenUPM scope and
declare only Framework in `Packages/manifest.json`; Unity will resolve
Composition transitively when both package versions are available from that
registry. Composition is not a parallel Actor runtime. The feature remains
optional for an Actor Profile with no Base parts.

Decision: [IF-ADR-050](../Architecture/ADRs/IF-ADR-050-Actor-Occurrence-Lifecycle-Authority-and-Commit.md).
