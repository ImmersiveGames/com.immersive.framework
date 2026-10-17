# Actor Occurrence References

Status: **Experimental in `1.2.0-preview.2`. The Project Owner reports 11/11 Actor References EditMode tests passed; PEGA consumer integration validation is pending.**
Decision: [IF-ADR-049 — Public Actor Occurrence Reference and Resolution](../Architecture/ADRs/IF-ADR-049-Public-Actor-Occurrence-Reference-and-Resolution.md)

## Contract

An Actor occurrence reference selects a live Actor without keeping a `GameObject`
as the selector. A successful read returns an immutable `ActorOccurrence` with the
typed `ActorId`, physical Actor root, exact `RuntimeContentOwner` and a
host-local `ActorOccurrenceId` for that incarnation.

The result status is one of `Resolved`, `Unavailable`, `InvalidReference`,
`Stale` or `Ambiguous`. A reference by Slot or ActorId follows the current
occurrence. `ActorOccurrenceReference.ForOccurrence(evidence)` addresses one exact
incarnation and becomes `Stale` after replacement or release.

Physical occurrence does not imply Activity participation, gameplay readiness,
input availability, targeting eligibility or movement authority. The returned
root is the Actor spatial root; subordinate visuals, Camera Subjects and technical
Player Hosts are not substituted.

## Resolve a Player by Slot

Add `ActorOccurrenceReferenceBinding` to a consumer object inside a Framework
composed scene or Persistent Content root. Select `Player Slot` and assign the
target `PlayerSlotProfile`. The component exposes the initial/current state and
change notifications:

```csharp
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

public sealed class PlayerActorConsumer : MonoBehaviour
{
    [SerializeField] private ActorOccurrenceReferenceBinding referenceBinding;

    private void OnEnable()
    {
        referenceBinding.TrySetReference(
            ActorOccurrenceReference.ForPlayerSlot(PlayerSlotId.Player1),
            out string issue);
        ActorOccurrenceResolution current = referenceBinding.Current;
    }
}
```

The Inspector can configure the same binding by selecting `Player Slot` and
assigning the `PlayerSlotProfile`. An ordinary C# consumer can also use this
typed reference without deriving from a Framework component.

```csharp
using Immersive.Framework.Actors;
using UnityEngine;

public sealed class FollowTargetConsumer : MonoBehaviour
{
    [SerializeField] private ActorOccurrenceReferenceBinding target;

    private void OnEnable()
    {
        target.Changed += OnTargetChanged;
        ReadTarget();
    }

    private void OnDisable()
    {
        target.Changed -= OnTargetChanged;
    }

    private void ReadTarget()
    {
        ActorOccurrenceResolution result = target.Current;
        if (result.Succeeded)
        {
            GameObject actorRoot = result.Occurrence.ActorRoot;
            // Use the current physical root according to this consumer's own policy.
        }
    }

    private void OnTargetChanged(ActorOccurrenceReferenceChange change)
    {
        // Available, Replaced, Unavailable, Invalidated or BindingReleased.
    }
}
```

The Player source reads the existing Session preparation/materialization authority
and checks the current joined Slot, selected Actor and active physical handle. It
does not require a current Activity assignment. Activity exit therefore preserves
the reference while the Session-owned physical Player remains valid. Replacement
publishes a new occurrence identity; Leave and physical release make the current
reference unavailable. Consumers do not need to inherit from the binding component.

## Publish a NonPlayer Actor

For an Actor authored in materialized Route, Activity or Persistent Content,
place `ActorOccurrencePublication` on the exact physical Actor root beside its
`ActorDeclaration`. The declaration must be `NonPlayer`. An `ActorDeclaration`
alone does not publish an occurrence.

The Framework scans only the roots returned by the existing composition owner.
It stages marker evidence before the lifecycle commit, makes it resolvable after
commit, and removes it when that exact owner releases. A failed admission rolls
back the staged evidence. Duplicate ActorIds remain separate records and resolve
as `Ambiguous` unless an exact owner is selected.

For a producer that creates Actors at runtime, add `ActorOccurrencePublicationScope`
to a producer object inside the same explicitly composed lifetime. Add
`ActorOccurrencePublication` and `ActorDeclaration` to the spawned Actor root,
then publish and release with an owner token:

```csharp
RuntimeContentOwner activityOwner = RuntimeContentOwner.Activity(
    activity.ActivityId.StableText,
    activity.ActivityName,
    RuntimeDefinitionToken.FromUnityObject(activity));

if (publicationScope.TryPublish(publication, activityOwner,
        out ActorOccurrencePublicationToken token, out string issue))
{
    // Retain token with the producer's own lifetime record.
}

// When that producer releases this occurrence:
publicationScope.TryUnpublish(publication, token, out issue);
```

`activityOwner` must equal the exact committed Activity owner, including its
definition token. Route producers follow the same rule. Persistent Session
producers can use `TryPublishForSession`; it targets the active Framework Session
owner. Publication does not transfer physical lifecycle authority to the Framework.
The producer remains responsible for unpublishing before its own release.
When a scene scope is released and later composed again, the producer publishes a
new occurrence and receives a new token. If Scene Lifecycle rejects a scene release
and compensates by restoring the still-loaded scene, the existing publication and
occurrence identity are restored with it.

## Select by ActorId and owner

The Inspector binding can select an `ActorDeclaration` identity, then optionally
constrain it to the active Session, an exact Route asset or an exact Activity
asset. A typed C# consumer can construct a selector directly:

```csharp
ActorOccurrenceReference reference =
    ActorOccurrenceReference.ForActorId(actorId, activityOwner);
binding.TrySetReference(reference, out string issue);
```

An ActorId without an owner is valid only while it resolves to one current
occurrence. Multiple current occurrences produce `Ambiguous`; the resolver never
chooses by name, hierarchy, tags or first match. The owner selector must be the
same operational owner used by Route/Activity composition.

## Manual Unity validation

The implementation includes an Editor contract test assembly. After importing
the package into a Unity 6000.5 project:

1. Compile/import the package and confirm the Framework runtime assembly has no
   errors.
2. Run `Immersive.Framework.ActorReferences.Editor.Tests` in EditMode. Confirm
   owner staging/rollback, owner release, exact stale references, duplicate
   ambiguity, Activity A→B replacement observation, dynamic publish/unpublish,
   Player physical resolution across Activity exit, Player replacement and Leave.
3. In a small consumer scene, author two Activities and one persistent consumer
   binding targeting P1. Start in Activity A, transition to B, and confirm the
   same Player root remains resolved. Replace the admitted Player through the
   existing Player authority and confirm `Replaced` plus a new occurrence ID.
4. Leave P1 and confirm the binding reports `Unavailable`; retain the old exact
   occurrence selector and confirm it reports `Stale`.
5. Publish a NonPlayer Actor in Activity content and another with the same
   ActorId under a second live owner. Confirm the unscoped selector is
   `Ambiguous`, then constrain it to each exact owner and confirm the matching
   root. Release each owner and confirm its occurrence disappears.
6. Exercise a runtime-created NonPlayer through a composed
   `ActorOccurrencePublicationScope`; unpublish its token and confirm observers
   receive the availability change.
7. Repeat a Scene Lifecycle availability notification for one scope, then release
   it once and confirm the reference becomes unavailable. Simulate a rejected
   release with compensation and confirm the prior occurrence identity returns;
   after a successful release/recomposition, republish and confirm a new identity.

Do not treat source test presence or static checks as Unity compile/import,
EditMode, Play Mode or external consumer evidence. API maturity remains
Experimental until a separate decision records the required evidence.
