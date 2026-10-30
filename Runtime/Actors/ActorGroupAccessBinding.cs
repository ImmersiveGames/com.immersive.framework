using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>Explicit access to the Session runtime's live Groups from a committed Framework content owner.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Actors/Actor Group Access")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-051 owner-bound Group creation, membership and query surface.")]
    public sealed class ActorGroupAccessBinding : MonoBehaviour
    {
        private ActorGroupRuntime _runtime;
        private RuntimeContentOwner _owner;

        public bool IsAccessAvailable => _runtime != null && _owner.IsValid;

        public bool TryCreateGroup(
            GroupProfile profile,
            out ActorGroupSnapshot group,
            out string diagnostic)
        {
            if (profile == null)
            {
                group = default;
                diagnostic = "A Group Profile is required.";
                return false;
            }

            if (!profile.TryGetGroupId(out _, out diagnostic))
            {
                group = default;
                return false;
            }

            if (!TryGetBoundRuntime(out ActorGroupRuntime runtime, out RuntimeContentOwner owner, out diagnostic))
            {
                group = default;
                return false;
            }

            return runtime.TryCreateGroup(owner, profile, out group, out diagnostic);
        }

        public bool TryCreateGroup(
            GroupId groupId,
            out ActorGroupSnapshot group,
            out string diagnostic)
        {
            if (!TryGetBoundRuntime(out ActorGroupRuntime runtime, out RuntimeContentOwner owner, out diagnostic))
            {
                group = default;
                return false;
            }

            return runtime.TryCreateGroup(owner, groupId, out group, out diagnostic);
        }

        public bool TryGetGroup(
            GroupId groupId,
            out ActorGroupSnapshot group,
            out string diagnostic)
        {
            if (!TryGetBoundRuntime(out ActorGroupRuntime runtime, out _, out diagnostic))
            {
                group = default;
                return false;
            }

            return runtime.TryGetGroup(groupId, out group, out diagnostic);
        }

        /// <summary>Joins the logical Actor to a live Group. Repeating an existing join succeeds without duplicating the membership.</summary>
        public bool TryJoinGroup(GroupId groupId, ActorId actorId, out string diagnostic)
        {
            if (!TryGetBoundRuntime(out ActorGroupRuntime runtime, out _, out diagnostic)) return false;
            return runtime.TryJoinGroup(groupId, actorId, out diagnostic);
        }

        /// <summary>Removes the logical Actor from a live Group. Repeating an absent leave succeeds without changing membership.</summary>
        public bool TryLeaveGroup(GroupId groupId, ActorId actorId, out string diagnostic)
        {
            if (!TryGetBoundRuntime(out ActorGroupRuntime runtime, out _, out diagnostic)) return false;
            return runtime.TryLeaveGroup(groupId, actorId, out diagnostic);
        }

        /// <summary>Queries the membership edge for a live Group and logical Actor.</summary>
        public bool TryIsMember(GroupId groupId, ActorId actorId, out bool isMember, out string diagnostic)
        {
            if (!TryGetBoundRuntime(out ActorGroupRuntime runtime, out _, out diagnostic))
            {
                isMember = false;
                return false;
            }

            return runtime.TryIsMember(groupId, actorId, out isMember, out diagnostic);
        }

        /// <summary>Returns a read-only copy of one Group's members when its count is within the caller's limit.</summary>
        public bool TryGetMembers(
            GroupId groupId,
            int maximumCount,
            out IReadOnlyList<ActorId> members,
            out string diagnostic)
        {
            if (!TryGetBoundRuntime(out ActorGroupRuntime runtime, out _, out diagnostic))
            {
                members = System.Array.Empty<ActorId>();
                return false;
            }

            return runtime.TryGetMembers(groupId, maximumCount, out members, out diagnostic);
        }

        /// <summary>Returns a read-only copy of one logical Actor's Groups when its count is within the caller's limit.</summary>
        public bool TryGetGroupsForActor(
            ActorId actorId,
            int maximumCount,
            out IReadOnlyList<GroupId> groups,
            out string diagnostic)
        {
            if (!TryGetBoundRuntime(out ActorGroupRuntime runtime, out _, out diagnostic))
            {
                groups = System.Array.Empty<GroupId>();
                return false;
            }

            return runtime.TryGetGroupsForActor(actorId, maximumCount, out groups, out diagnostic);
        }

        internal bool CanBindRuntime(
            ActorGroupRuntime runtime,
            RuntimeContentOwner owner,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (runtime == null || !owner.IsValid)
            {
                diagnostic = "Actor Group access binding requires an exact runtime and valid lifecycle owner.";
                return false;
            }

            if (_runtime != null)
            {
                diagnostic = _runtime == runtime && _owner == owner
                    ? "Actor Group access binding is already bound to this owner."
                    : $"Actor Group access binding is already bound to owner '{_owner.StableText}'.";
                return false;
            }

            return true;
        }

        internal bool TryBindRuntime(
            ActorGroupRuntime runtime,
            RuntimeContentOwner owner,
            out string diagnostic)
        {
            if (!CanBindRuntime(runtime, owner, out diagnostic)) return false;
            _runtime = runtime;
            _owner = owner;
            return true;
        }

        internal void UnbindRuntime(ActorGroupRuntime runtime, RuntimeContentOwner owner)
        {
            if (_runtime != runtime || _owner != owner) return;
            _runtime = null;
            _owner = default;
        }

        private bool TryGetBoundRuntime(
            out ActorGroupRuntime runtime,
            out RuntimeContentOwner owner,
            out string diagnostic)
        {
            runtime = _runtime;
            owner = _owner;
            if (runtime == null || !owner.IsValid)
            {
                diagnostic = "Actor Group access is unavailable until its Framework owner is committed.";
                return false;
            }

            diagnostic = string.Empty;
            return true;
        }
    }
}
