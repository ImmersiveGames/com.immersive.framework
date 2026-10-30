# Actor Groups

Status: **Experimental**<br>
Decision: [IF-ADR-051 — Actor Groups](../Architecture/ADRs/IF-ADR-051-Actor-Groups-Identity-Membership-and-Player-Slot-Associations.md)<br>
API: [Public API Reference — Player Participation and Actor](../API/Public-API.md#player-participation-and-actor)

## Contract

An Actor Group is an independently identified collective with a mutable set of
logical Actor memberships. Its `GroupId` is distinct from every `ActorId`,
lifecycle owner and physical Actor occurrence. One Actor may belong to several
Groups, and a Group may have several or no members.

`GroupProfile` is the single authoring asset for a Group. It owns the explicit
`GroupId` and optional game-neutral display name, description and icon.
Referencing or loading the asset does not create a live Group. The Group is
created only by an explicit runtime call; presentation does not define gameplay
behavior or membership.

Membership uses `ActorId`, not `ActorOccurrenceId`. It can be added before an
Actor is physically materialized and remains attached when a physical
occurrence is replaced with the same logical ID. A different `ActorId` does
not inherit membership automatically. The Framework does not decide game
admission or permanence rules.

## Author Group data

Create one asset from **Assets > Create > Immersive Framework > Groups > Group
Profile**. Set its non-empty `GroupId`, unique among live Groups in a Framework
Session, and fill in any presentation fields that are useful. The `GroupId` is
the only Group identity; asset name, hierarchy, membership and presentation
never generate or replace it. Referencing this asset from another component
does not materialize a live Group.

## Provide access in an owned scope

Add `ActorGroupAccessBinding` to a GameObject beneath roots explicitly
materialized by a supported Framework content owner: Session, Route or
Activity. The Framework discovers and binds it as that owner is committed.
`IsAccessAvailable` is false before commit and after owner release; calls made
while unbound fail with a diagnostic.

Creation associates a Group with the exact committed owner of the binding used
to create it. The Group is released when that owner closes. Choose the owner
whose lifetime matches the need: a Route- or Activity-owned Group ends with
that scope, while a Session-owned Group remains available across those shorter
scopes. Transient owners are not supported.

The binding accesses the Framework Session's live Group registry. Its owner
controls the lifetime of Groups it creates; it does not partition registry
access. A committed binding can address any live Group in that Session,
including a Group created under another owner. The owner is not an
authorization policy for membership changes. Consumers must apply their own
permissions and gameplay rules before calling membership operations.

## Create and find a live Group

After the binding's owner has committed, explicitly create a Group from its
authored Profile:

The snippets are call-site fragments. `groupAccess` and `groupProfile` are
references owned by the consumer. Invoke creation from consumer code that runs
after the owner commits; do not rely on Unity `Awake`/`Start` ordering as a
commit signal.

```csharp
using Immersive.Framework.Actors;

if (!groupAccess.TryCreateGroup(
        groupProfile,
        out ActorGroupSnapshot group,
        out string issue))
{
    // Handle the reported invalid Profile, unavailable owner or duplicate live GroupId.
    return;
}

GroupId groupId = group.GroupId;
```

For a runtime-created Group without authored presentation, supply an explicit
`GroupId` to `TryCreateGroup(GroupId, ...)`. Creating from a `GroupProfile`
uses the `GroupId` on that asset; callers do not supply a second identity. Both
paths require an explicit call and use the same identity checks and owner
lifecycle. A duplicate live `GroupId` in the Session registry is rejected.
`TryGetGroup` returns the snapshot for a live Group and fails with a diagnostic
when the ID is invalid or no such live Group exists.

An `ActorGroupSnapshot` returned by lookup exposes its `GroupId` and optional
`GroupProfile`. It does not contain membership or provide a live-liveness
check after it is returned. Query the binding again when current liveness is
required; query membership through the access binding.

## Change membership

Use the Actor's existing logical identity, usually from its
`ActorDeclaration.ActorId` or the consumer's logical Actor identity authority:

The example assumes the consumer has obtained the declaration and a live
Group ID through its own scope setup.

```csharp
using Immersive.Framework.Actors;

ActorId actorId = actorDeclaration.ActorId;

if (!groupAccess.TryJoinGroup(groupId, actorId, out string issue))
{
    // Handle an invalid identity or a Group that is no longer live.
    return;
}
```

`TryJoinGroup` and `TryLeaveGroup` are idempotent: repeating a join or leaving
an Actor who is already absent succeeds without changing the set. They require
a valid `ActorId` and a live Group. No Actor occurrence or Player Slot is
required. A leave removes only that Group–Actor edge; it does not close the
Group, including when the Group becomes empty.

The Framework does not impose exclusivity, quotas, faction policy or
authorization. Consumers own those rules and any Group capabilities such as
shared knowledge, instructions, modifiers or GOAP integration.

## Query membership

Query one edge with `TryIsMember`. A successful query distinguishes a live
Group with no such edge (`isMember == false`) from an invalid identity or
Group lookup failure (method returns `false`).

For a read-only membership list, provide an explicit maximum count:

```csharp
using System.Collections.Generic;
using Immersive.Framework.Actors;

if (groupAccess.TryGetMembers(
        groupId,
        maximumCount: 32,
        out IReadOnlyList<ActorId> members,
        out string issue))
{
    foreach (ActorId member in members)
    {
        // Use the logical ActorId; resolve a physical occurrence separately if needed.
    }
}
```

`TryGetGroupsForActor` provides the reverse query for one valid `ActorId` and
also requires a maximum count. Both methods return defensive, read-only,
deterministically ordered copies. If the requested result exceeds the limit,
the method fails with a diagnostic and returns no partial list. A valid Actor
with no memberships produces a successful empty Group list.

## Lifecycle and scope

Group lifetime follows its exact Session, Route or Activity owner, not member
presence. A Group remains queryable after its final member leaves. When an
owner closes, the Framework removes only the Groups created under that exact
owner, their membership edges, and their access bindings. Another owner's
Groups and memberships remain live. Membership is not persisted across a
Framework Session/process restart.

Do not infer Groups from common Profiles, Actor Kind/Role, hierarchy, Player
Slots, camera assignment or physical composition. Player Slot associations,
effective control, Group-specific gameplay and persistence are outside this
contract.
