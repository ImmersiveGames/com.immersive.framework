# Actor Occurrence References

Status: **Experimental in `1.2.0-preview.2`. The Project Owner reports 11/11 Actor References EditMode tests passed; PEGA consumer integration validation is pending. The on-demand query role (Amendment 001) is Experimental and unreleased: the Project Owner reports its EditMode tests passed, and its manual Play Mode confirmation is pending.**
Decision: [IF-ADR-049 — Public Actor Occurrence Reference and Resolution](../Architecture/ADRs/IF-ADR-049-Public-Actor-Occurrence-Reference-and-Resolution.md) and [Amendment 001 — Scoped On-Demand Actor Occurrence Query](../Architecture/ADRs/IF-ADR-049-AMENDMENT-001-SCOPED-ON-DEMAND-OCCURRENCE-QUERY.md)

## Contract

An Actor occurrence reference selects a live Actor without keeping a `GameObject`
as the selector. A successful read returns an immutable `ActorOccurrence` with the
typed `ActorId`, physical Actor root, exact `RuntimeContentOwner` and a
host-local `ActorOccurrenceId` for that incarnation.

The result status is one of `Resolved`, `Unavailable`, `InvalidReference`,
`Stale` or `Ambiguous`. A reference by Slot or ActorId follows the current
occurrence. `ActorOccurrenceReference.ForOccurrence(evidence)` addresses one exact
incarnation and becomes `Stale` when that incarnation is not physically available
now. See [Query on demand](#query-on-demand) for the full meaning of `Stale`.

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

## Query on demand

Use `ActorOccurrenceReferenceBinding` to follow one selector over time. Use the
query role when targets are only known at runtime. For example, `ActorId`s may
come from an Actor Group or from evidence a gameplay system holds, and the system
needs to read the current occurrence once, at a moment its own policy chooses.

| | Observation | Query |
|---|---|---|
| Surface | `ActorOccurrenceReferenceBinding` | `IActorOccurrenceQuery` from `ActorOccurrenceQueryBinding` |
| State | One selector, last result | None |
| Notifications | `Changed` | None |

Both roles use the same host resolver and the same selectors and result statuses.

### Author the anchor

Add `ActorOccurrenceQueryBinding` (**Immersive Framework > Actors > Actor
Occurrence Query**) to an object inside the roots of a Persistent Content
(Session), Route or Activity composition. The Framework binds the anchor when that
exact owner commits. It unbinds the anchor on the owner's rollback or release, and
at Framework host teardown even if no owner release ran. Transient owners are not
supported. An anchor under the roots of two owners fails the second owner's
registration. The Scene Lifecycle participant never binds the anchor, so a
component created after composition is not bound.

The anchor owner defines **lifetime only**. A Route-owned anchor can still resolve
a Session-owned Player or any other committed occurrence. Owner selection inside a
query is expressed by the selector.

### Hand off the query

Reference the anchor from the component that owns your system, and pass its
interface explicitly. Do not locate it through a static member, a scene search or
the host.

```csharp
using Immersive.Framework.Actors;
using UnityEngine;

public sealed class KnowledgeSystemHost : MonoBehaviour
{
    [SerializeField] private ActorOccurrenceQueryBinding occurrenceQuery;
    private KnowledgeSystem _knowledge;

    private void Awake()
    {
        _knowledge = new KnowledgeSystem(occurrenceQuery);
    }
}

public sealed class KnowledgeSystem
{
    private readonly IActorOccurrenceQuery _occurrences;

    public KnowledgeSystem(IActorOccurrenceQuery occurrences) => _occurrences = occurrences;

    public void OnEvidence(ActorOccurrence evidence)
    {
        ActorOccurrenceResolution result =
            _occurrences.Resolve(ActorOccurrenceReference.ForOccurrence(evidence));
        if (result.Status != ActorOccurrenceResolutionStatus.Resolved)
            return; // Stale, Unavailable or Ambiguous: record nothing.

        Vector3 position = result.Occurrence.ActorRoot.transform.position;
        // Store position with result.Occurrence.Id and this system's own timestamp.
        // Do not keep ActorRoot as a current reference.
    }
}
```

### Semantics

- **Point in time.** A result describes the occurrence at the moment of the query.
  To keep durable data, capture values together with the `ActorOccurrenceId`, and
  query again later instead of holding the root.
- **Side-effect free and reentrant.** A query changes no selector, binding,
  publication or occurrence identity, and dispatches nothing. It is valid inside a
  `Changed` callback and sees committed state only.
- **Lifetime.** Before bind, after release and after host teardown, every query
  returns `Unavailable` with a diagnostic. It never throws, including from a plain
  C# holder whose anchor was destroyed.
- **`Stale` for exact selectors.** For an exact-occurrence selector, `Stale` means
  the expected occurrence is not physically available **now**. Causes include
  replacement, release, transient inactivity of a still-committed occurrence (for
  example during a guarded A→B handoff) and suspension of a dynamic publication
  scope that may be compensated with the same `ActorOccurrenceId`. `Stale` is not
  proof of end of life. Non-exact selectors (Slot, ActorId) report the same
  transient as `Unavailable`. Consumers that need a durable value must record
  nothing on `Stale` and may query again later.
- **Ordering at Session release.** Observation bindings are released before the
  Session owner. During that interval, a Session-owned anchor can still return
  `Resolved` for content that is being released. This is valid committed state.
- **Physical is not authorization.** A resolved occurrence grants no participation,
  readiness, input, perception, targeting, knowledge legitimacy or movement
  authority. What a consumer may query or keep is its own policy.
- **Cost.** Each query scans current publications and physical Player occurrences
  and allocates. Query per event or decision, not per frame. Use observation for
  continuous tracking.

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

On-demand query (Amendment 001), in Play Mode with a composed scene:

8. Confirm `ActorOccurrenceReferenceBinding` is still bound only when its scene
   becomes available (`OnSceneAvailable`), with unchanged behavior.
9. Add `ActorOccurrenceQueryBinding` under Persistent Content, plus a consumer
   that queries P1 by Slot from `Awake` (before the Session owner commits) and
   again after startup. Confirm `Unavailable` from `Awake` and `Resolved` after
   startup (P1 joined).
10. Add anchors under a Route root and an Activity root. Confirm both resolve after
    their owner commits. Hand one anchor's `IActorOccurrenceQuery` to a plain C#
    object that outlives the Activity. Exit the Activity (and later the Route) and
    confirm that holder now gets `Unavailable`, while the Session anchor still
    resolves the Session-owned Player.
11. Keep a plain C# holder for a Route or Activity anchor in a static field of an
    Editor-only test script. Exit Play Mode while that owner is still active, so
    its owner release never runs and the host is destroyed. On
    `EditorApplication.playModeStateChanged == EnteredEditMode`, call `Resolve`.
    Confirm no exception and `Unavailable` with the "not bound" diagnostic. Then
    re-enter Play Mode and confirm the anchors bind again.

Do not treat source test presence or static checks as Unity compile/import,
EditMode, Play Mode or external consumer evidence. API maturity remains
Experimental until a separate decision records the required evidence.
