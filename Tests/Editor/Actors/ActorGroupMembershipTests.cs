using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.RuntimeContent;
using NUnit.Framework;
using UnityEngine;

namespace Immersive.Framework.Actors.Tests
{
    public sealed class ActorGroupMembershipTests
    {
        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            for (int index = _created.Count - 1; index >= 0; index--)
            {
                if (_created[index] != null) Object.DestroyImmediate(_created[index]);
            }

            _created.Clear();
        }

        [Test]
        public void Membership_IsManyToManyByActorIdWithoutOccurrence()
        {
            var runtime = new ActorGroupRuntime();
            RuntimeContentOwner owner = RuntimeContentOwner.Session("session.groups.membership", "Session");
            ActorGroupAccessBinding access = CreateAccess(runtime, owner);
            GroupId firstGroup = GroupId.From("group.membership.first");
            GroupId secondGroup = GroupId.From("group.membership.second");
            ActorId firstActor = ActorId.From("actor.membership.first");
            ActorId secondActor = ActorId.From("actor.membership.second");

            Assert.That(access.TryCreateGroup(firstGroup, null, out _, out string issue), Is.True, issue);
            Assert.That(access.TryCreateGroup(secondGroup, null, out _, out issue), Is.True, issue);
            Assert.That(access.TryJoinGroup(firstGroup, firstActor, out issue), Is.True, issue);
            Assert.That(access.TryJoinGroup(firstGroup, firstActor, out issue), Is.True, issue);
            Assert.That(access.TryJoinGroup(secondGroup, firstActor, out issue), Is.True, issue);
            Assert.That(access.TryJoinGroup(firstGroup, secondActor, out issue), Is.True, issue);

            Assert.That(access.TryIsMember(firstGroup, firstActor, out bool firstMembership, out issue), Is.True, issue);
            Assert.That(firstMembership, Is.True);
            Assert.That(access.TryIsMember(secondGroup, firstActor, out bool secondMembership, out issue), Is.True, issue);
            Assert.That(secondMembership, Is.True);
            Assert.That(access.TryGetMembers(firstGroup, 2, out IReadOnlyList<ActorId> members, out issue), Is.True, issue);
            Assert.That(members, Does.Contain(firstActor));
            Assert.That(members, Does.Contain(secondActor));
            Assert.Throws<System.NotSupportedException>(() => ((IList<ActorId>)members)[0] = ActorId.From("actor.membership.replacement"));
            Assert.That(access.TryGetGroupsForActor(firstActor, 2, out IReadOnlyList<GroupId> groups, out issue), Is.True, issue);
            Assert.That(groups, Does.Contain(firstGroup));
            Assert.That(groups, Does.Contain(secondGroup));
        }

        [Test]
        public void LeavingOneGroup_PreservesOtherMemberships()
        {
            var runtime = new ActorGroupRuntime();
            RuntimeContentOwner owner = RuntimeContentOwner.Session("session.groups.leave", "Session");
            ActorGroupAccessBinding access = CreateAccess(runtime, owner);
            GroupId firstGroup = GroupId.From("group.leave.first");
            GroupId secondGroup = GroupId.From("group.leave.second");
            ActorId actorId = ActorId.From("actor.leave.shared");
            Assert.That(access.TryCreateGroup(firstGroup, null, out _, out string issue), Is.True, issue);
            Assert.That(access.TryCreateGroup(secondGroup, null, out _, out issue), Is.True, issue);
            Assert.That(access.TryJoinGroup(firstGroup, actorId, out issue), Is.True, issue);
            Assert.That(access.TryJoinGroup(secondGroup, actorId, out issue), Is.True, issue);

            Assert.That(access.TryLeaveGroup(firstGroup, actorId, out issue), Is.True, issue);
            Assert.That(access.TryLeaveGroup(firstGroup, actorId, out issue), Is.True, issue);

            Assert.That(access.TryIsMember(firstGroup, actorId, out bool firstMembership, out issue), Is.True, issue);
            Assert.That(firstMembership, Is.False);
            Assert.That(access.TryIsMember(secondGroup, actorId, out bool secondMembership, out issue), Is.True, issue);
            Assert.That(secondMembership, Is.True);
        }

        [Test]
        public void Group_RemainsLiveAfterLastMemberLeaves()
        {
            var runtime = new ActorGroupRuntime();
            RuntimeContentOwner owner = RuntimeContentOwner.Session("session.groups.last-member", "Session");
            ActorGroupAccessBinding access = CreateAccess(runtime, owner);
            GroupId groupId = GroupId.From("group.last-member");
            ActorId actorId = ActorId.From("actor.last-member");
            Assert.That(access.TryCreateGroup(groupId, null, out _, out string issue), Is.True, issue);
            Assert.That(access.TryJoinGroup(groupId, actorId, out issue), Is.True, issue);

            Assert.That(access.TryLeaveGroup(groupId, actorId, out issue), Is.True, issue);

            Assert.That(access.TryGetGroup(groupId, out ActorGroupSnapshot group, out issue), Is.True, issue);
            Assert.That(group.GroupId, Is.EqualTo(groupId));
            Assert.That(access.TryGetMembers(groupId, 0, out IReadOnlyList<ActorId> members, out issue), Is.True, issue);
            Assert.That(members, Is.Empty);
        }

        [Test]
        public void PreservedActorIdRetainsMembership_NewActorIdDoesNotInherit()
        {
            var runtime = new ActorGroupRuntime();
            RuntimeContentOwner owner = RuntimeContentOwner.Session("session.groups.logical-identity", "Session");
            ActorGroupAccessBinding access = CreateAccess(runtime, owner);
            GroupId groupId = GroupId.From("group.logical-identity");
            ActorId preservedActorId = ActorId.From("actor.logical-identity.preserved");
            ActorId newActorId = ActorId.From("actor.logical-identity.new");
            Assert.That(access.TryCreateGroup(groupId, null, out _, out string issue), Is.True, issue);
            Assert.That(access.TryJoinGroup(groupId, preservedActorId, out issue), Is.True, issue);

            Assert.That(access.TryIsMember(groupId, preservedActorId, out bool preservedMembership, out issue), Is.True, issue);
            Assert.That(preservedMembership, Is.True);
            Assert.That(access.TryIsMember(groupId, newActorId, out bool newMembership, out issue), Is.True, issue);
            Assert.That(newMembership, Is.False);
        }

        [Test]
        public void OwnerRelease_ClearsOnlyItsGroupsAndMembershipEdges()
        {
            var runtime = new ActorGroupRuntime();
            RuntimeContentOwner firstOwner = RuntimeContentOwner.Session("session.groups.owner.first", "Session");
            RuntimeContentOwner secondOwner = RuntimeContentOwner.Activity(
                "activity.groups.owner.second",
                "Activity",
                RuntimeDefinitionToken.MintAnonymous());
            ActorGroupAccessBinding firstAccess = CreateAccess(runtime, firstOwner);
            ActorGroupAccessBinding secondAccess = CreateAccess(runtime, secondOwner);
            GroupId firstGroup = GroupId.From("group.owner.first");
            GroupId secondGroup = GroupId.From("group.owner.second");
            ActorId sharedActor = ActorId.From("actor.owner.shared");
            Assert.That(firstAccess.TryCreateGroup(firstGroup, null, out _, out string issue), Is.True, issue);
            Assert.That(secondAccess.TryCreateGroup(secondGroup, null, out _, out issue), Is.True, issue);
            Assert.That(firstAccess.TryJoinGroup(firstGroup, sharedActor, out issue), Is.True, issue);
            Assert.That(secondAccess.TryJoinGroup(secondGroup, sharedActor, out issue), Is.True, issue);

            Assert.That(runtime.TryReleaseOwner(firstOwner, out issue), Is.True, issue);

            Assert.That(firstAccess.TryIsMember(firstGroup, sharedActor, out _, out _), Is.False);
            Assert.That(secondAccess.TryIsMember(secondGroup, sharedActor, out bool secondMembership, out issue), Is.True, issue);
            Assert.That(secondMembership, Is.True);
            Assert.That(secondAccess.TryGetGroupsForActor(sharedActor, 1, out IReadOnlyList<GroupId> groups, out issue), Is.True, issue);
            Assert.That(groups, Is.EqualTo(new[] { secondGroup }));
        }

        [Test]
        public void MembershipOperations_RejectInvalidOrUnavailableSubjectsAndOversizedSnapshots()
        {
            var runtime = new ActorGroupRuntime();
            RuntimeContentOwner owner = RuntimeContentOwner.Session("session.groups.invalid-membership", "Session");
            ActorGroupAccessBinding access = CreateAccess(runtime, owner);
            GroupId groupId = GroupId.From("group.invalid-membership");
            ActorId firstActor = ActorId.From("actor.invalid-membership.first");
            ActorId secondActor = ActorId.From("actor.invalid-membership.second");
            Assert.That(access.TryCreateGroup(groupId, null, out _, out string issue), Is.True, issue);
            Assert.That(access.TryJoinGroup(groupId, firstActor, out issue), Is.True, issue);
            Assert.That(access.TryJoinGroup(groupId, secondActor, out issue), Is.True, issue);

            Assert.That(access.TryJoinGroup(groupId, default, out issue), Is.False);
            StringAssert.Contains("ActorId", issue);
            Assert.That(access.TryJoinGroup(default, firstActor, out issue), Is.False);
            StringAssert.Contains("GroupId", issue);
            Assert.That(access.TryGetMembers(groupId, 1, out IReadOnlyList<ActorId> members, out issue), Is.False);
            Assert.That(members, Is.Empty);
            StringAssert.Contains("limit", issue.ToLowerInvariant());
            Assert.That(access.TryGetMembers(groupId, -1, out _, out issue), Is.False);
            StringAssert.Contains("limit", issue.ToLowerInvariant());
            Assert.That(access.TryGetGroupsForActor(firstActor, 0, out IReadOnlyList<GroupId> groups, out issue), Is.False);
            Assert.That(groups, Is.Empty);
            StringAssert.Contains("limit", issue.ToLowerInvariant());
            Assert.That(access.TryIsMember(GroupId.From("group.not-live"), firstActor, out _, out issue), Is.False);
            StringAssert.Contains("live", issue.ToLowerInvariant());
        }

        private ActorGroupAccessBinding CreateAccess(ActorGroupRuntime runtime, RuntimeContentOwner owner)
        {
            var root = Track(new GameObject("Actor Group Membership Access Root"));
            ActorGroupAccessBinding access = root.AddComponent<ActorGroupAccessBinding>();
            Assert.That(runtime.TryRegisterOwnerContent(owner, new[] { root }, out string issue), Is.True, issue);
            Assert.That(runtime.TryCommitOwner(owner, out issue), Is.True, issue);
            return access;
        }

        private T Track<T>(T value) where T : Object
        {
            _created.Add(value);
            return value;
        }
    }
}
