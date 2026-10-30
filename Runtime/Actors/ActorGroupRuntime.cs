using System;
using System.Collections.Generic;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>Session-host-owned authority for Group records and exact lifecycle ownership.</summary>
    internal sealed class ActorGroupRuntime
    {
        private sealed class OwnerRegistration
        {
            internal OwnerRegistration(RuntimeContentOwner owner, List<ActorGroupAccessBinding> bindings)
            {
                Owner = owner;
                Bindings = bindings;
            }

            internal RuntimeContentOwner Owner { get; }

            internal List<ActorGroupAccessBinding> Bindings { get; }
        }

        private sealed class GroupRecord
        {
            internal GroupRecord(ActorGroupSnapshot snapshot, RuntimeContentOwner owner)
            {
                Snapshot = snapshot;
                Owner = owner;
            }

            internal ActorGroupSnapshot Snapshot { get; }

            internal RuntimeContentOwner Owner { get; }

            internal HashSet<ActorId> Members { get; } = new();
        }

        private readonly Dictionary<RuntimeContentOwner, OwnerRegistration> _stagedOwners = new();
        private readonly Dictionary<RuntimeContentOwner, OwnerRegistration> _committedOwners = new();
        private readonly Dictionary<ActorGroupAccessBinding, RuntimeContentOwner> _bindingOwners = new();
        private readonly Dictionary<GroupId, GroupRecord> _groups = new();
        private readonly Dictionary<ActorId, HashSet<GroupId>> _groupsByActor = new();

        internal bool TryRegisterOwnerContent(
            RuntimeContentOwner owner,
            IReadOnlyList<GameObject> roots,
            out string diagnostic)
        {
            diagnostic = string.Empty;
            if (!owner.IsValid || roots == null)
            {
                diagnostic = "Actor Group registration requires an exact valid owner and materialized roots.";
                return false;
            }

            if (!IsSupportedScope(owner.Scope))
            {
                diagnostic = $"Actor Groups support Session, Route and Activity owners; scope '{owner.Scope}' is unsupported.";
                return false;
            }

            if (_stagedOwners.ContainsKey(owner) || _committedOwners.ContainsKey(owner))
            {
                diagnostic = $"Actor Group owner '{owner.StableText}' is already staged or committed.";
                return false;
            }

            var bindings = new List<ActorGroupAccessBinding>();
            var seen = new HashSet<ActorGroupAccessBinding>();
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null)
                {
                    diagnostic = $"Actor Group owner '{owner.StableText}' contains a missing materialized root.";
                    return false;
                }

                ActorGroupAccessBinding[] candidates = root.GetComponentsInChildren<ActorGroupAccessBinding>(true);
                for (int index = 0; index < candidates.Length; index++)
                {
                    ActorGroupAccessBinding binding = candidates[index];
                    if (binding == null || !seen.Add(binding)) continue;

                    if (_bindingOwners.TryGetValue(binding, out RuntimeContentOwner registeredOwner))
                    {
                        diagnostic = $"Actor Group access binding '{binding.name}' is already staged or committed for owner '{registeredOwner.StableText}'.";
                        return false;
                    }

                    if (!binding.CanBindRuntime(this, owner, out diagnostic)) return false;
                    bindings.Add(binding);
                }
            }

            var registration = new OwnerRegistration(owner, bindings);
            _stagedOwners.Add(owner, registration);
            for (int index = 0; index < bindings.Count; index++)
                _bindingOwners.Add(bindings[index], owner);
            return true;
        }

        internal bool TryCommitOwner(RuntimeContentOwner owner, out string diagnostic)
        {
            diagnostic = string.Empty;
            if (!owner.IsValid || !_stagedOwners.TryGetValue(owner, out OwnerRegistration registration))
            {
                diagnostic = $"Actor Group owner '{owner.StableText}' has no staged registration.";
                return false;
            }

            var bound = new List<ActorGroupAccessBinding>();
            for (int index = 0; index < registration.Bindings.Count; index++)
            {
                ActorGroupAccessBinding binding = registration.Bindings[index];
                if (binding == null || !binding.TryBindRuntime(this, owner, out diagnostic))
                {
                    if (binding == null)
                        diagnostic = $"Actor Group access binding under owner '{owner.StableText}' was destroyed before commit.";
                    for (int boundIndex = 0; boundIndex < bound.Count; boundIndex++)
                        bound[boundIndex].UnbindRuntime(this, owner);
                    RemoveStagedOwner(owner, registration);
                    return false;
                }

                bound.Add(binding);
            }

            _stagedOwners.Remove(owner);
            _committedOwners.Add(owner, registration);
            return true;
        }

        internal bool TryRollbackOwner(RuntimeContentOwner owner, out string diagnostic)
        {
            diagnostic = string.Empty;
            if (!owner.IsValid)
            {
                diagnostic = "Actor Group rollback requires an exact valid owner.";
                return false;
            }
            if (!IsSupportedScope(owner.Scope))
            {
                diagnostic = $"Actor Group rollback does not support owner scope '{owner.Scope}'.";
                return false;
            }

            if (_stagedOwners.TryGetValue(owner, out OwnerRegistration staged))
                RemoveStagedOwner(owner, staged);
            if (_committedOwners.ContainsKey(owner))
                ReleaseCommittedOwner(owner);
            return true;
        }

        internal bool TryReleaseOwner(RuntimeContentOwner owner, out string diagnostic)
        {
            diagnostic = string.Empty;
            if (!owner.IsValid)
            {
                diagnostic = "Actor Group release requires an exact valid owner.";
                return false;
            }
            if (!IsSupportedScope(owner.Scope))
            {
                diagnostic = $"Actor Group release does not support owner scope '{owner.Scope}'.";
                return false;
            }

            if (_stagedOwners.TryGetValue(owner, out OwnerRegistration staged))
                RemoveStagedOwner(owner, staged);
            if (_committedOwners.ContainsKey(owner))
                ReleaseCommittedOwner(owner);
            return true;
        }

        internal bool TryCreateGroup(
            RuntimeContentOwner owner,
            GroupProfile profile,
            out ActorGroupSnapshot group,
            out string diagnostic)
        {
            if (profile == null)
            {
                group = default;
                diagnostic = "Actor Group creation requires a Group Profile.";
                return false;
            }

            if (!profile.TryGetGroupId(out GroupId groupId, out diagnostic))
            {
                group = default;
                diagnostic = "Actor Group Profile is invalid. " + diagnostic;
                return false;
            }

            return TryCreateGroup(owner, groupId, profile, out group, out diagnostic);
        }

        internal bool TryCreateGroup(
            RuntimeContentOwner owner,
            GroupId groupId,
            out ActorGroupSnapshot group,
            out string diagnostic)
        {
            return TryCreateGroup(owner, groupId, null, out group, out diagnostic);
        }

        private bool TryCreateGroup(
            RuntimeContentOwner owner,
            GroupId groupId,
            GroupProfile profile,
            out ActorGroupSnapshot group,
            out string diagnostic)
        {
            group = default;
            if (!owner.IsValid || !_committedOwners.ContainsKey(owner))
            {
                diagnostic = "Actor Group creation requires its exact committed lifecycle owner.";
                return false;
            }

            if (!groupId.IsValid)
            {
                diagnostic = "Actor Group creation requires a valid GroupId.";
                return false;
            }

            GroupId profileGroupId = default;
            if (profile != null && !profile.TryGetGroupId(out profileGroupId, out diagnostic))
            {
                diagnostic = "Actor Group Profile is invalid. " + diagnostic;
                return false;
            }

            if (profile != null && profileGroupId != groupId)
            {
                diagnostic = "Actor Group Profile has a different GroupId than the runtime Group.";
                return false;
            }

            if (_groups.ContainsKey(groupId))
            {
                diagnostic = $"A live Actor Group with identity '{groupId.StableText}' already exists.";
                return false;
            }

            group = new ActorGroupSnapshot(groupId, profile);
            _groups.Add(groupId, new GroupRecord(group, owner));
            diagnostic = string.Empty;
            return true;
        }

        internal bool TryGetGroup(
            GroupId groupId,
            out ActorGroupSnapshot group,
            out string diagnostic)
        {
            if (!groupId.IsValid)
            {
                group = default;
                diagnostic = "A valid GroupId is required for Actor Group lookup.";
                return false;
            }

            if (_groups.TryGetValue(groupId, out GroupRecord record))
            {
                group = record.Snapshot;
                diagnostic = string.Empty;
                return true;
            }

            group = default;
            diagnostic = $"No live Actor Group has identity '{groupId.StableText}'.";
            return false;
        }

        internal bool TryJoinGroup(GroupId groupId, ActorId actorId, out string diagnostic)
        {
            diagnostic = string.Empty;
            if (!TryGetLiveGroup(groupId, out GroupRecord record, out diagnostic)) return false;
            if (!actorId.IsValid)
            {
                diagnostic = "Actor Group membership requires a valid ActorId.";
                return false;
            }

            bool hasReverseIndex = _groupsByActor.TryGetValue(actorId, out HashSet<GroupId> groups);
            bool hasMembership = record.Members.Contains(actorId);
            bool hasReverseMembership = hasReverseIndex && groups.Contains(groupId);
            if (hasMembership != hasReverseMembership)
            {
                diagnostic = "Actor Group membership indexes are inconsistent; the join operation was rejected.";
                return false;
            }

            if (hasMembership) return true;
            if (!hasReverseIndex)
            {
                groups = new HashSet<GroupId>();
                _groupsByActor.Add(actorId, groups);
            }

            record.Members.Add(actorId);
            groups.Add(groupId);
            return true;
        }

        internal bool TryLeaveGroup(GroupId groupId, ActorId actorId, out string diagnostic)
        {
            diagnostic = string.Empty;
            if (!TryGetLiveGroup(groupId, out GroupRecord record, out diagnostic)) return false;
            if (!actorId.IsValid)
            {
                diagnostic = "Actor Group membership requires a valid ActorId.";
                return false;
            }

            bool hasMembership = record.Members.Contains(actorId);
            bool hasReverseMembership = _groupsByActor.TryGetValue(actorId, out HashSet<GroupId> groups) && groups.Contains(groupId);
            if (hasMembership != hasReverseMembership)
            {
                diagnostic = "Actor Group membership indexes are inconsistent; the leave operation was rejected.";
                return false;
            }
            if (!hasMembership) return true;

            record.Members.Remove(actorId);
            groups.Remove(groupId);
            if (groups.Count == 0) _groupsByActor.Remove(actorId);
            return true;
        }

        internal bool TryIsMember(
            GroupId groupId,
            ActorId actorId,
            out bool isMember,
            out string diagnostic)
        {
            isMember = false;
            if (!TryGetLiveGroup(groupId, out GroupRecord record, out diagnostic)) return false;
            if (!actorId.IsValid)
            {
                diagnostic = "Actor Group membership query requires a valid ActorId.";
                return false;
            }

            isMember = record.Members.Contains(actorId);
            return true;
        }

        internal bool TryGetMembers(
            GroupId groupId,
            int maximumCount,
            out IReadOnlyList<ActorId> members,
            out string diagnostic)
        {
            members = Array.Empty<ActorId>();
            if (!TryGetLiveGroup(groupId, out GroupRecord record, out diagnostic)) return false;
            if (maximumCount < 0)
            {
                diagnostic = "Actor Group membership snapshot limit cannot be negative.";
                return false;
            }
            if (record.Members.Count > maximumCount)
            {
                diagnostic = $"Actor Group membership snapshot contains {record.Members.Count} members, exceeding the requested limit of {maximumCount}.";
                return false;
            }

            var snapshot = new List<ActorId>(record.Members);
            snapshot.Sort((left, right) => StringComparer.Ordinal.Compare(left.StableText, right.StableText));
            members = Array.AsReadOnly(snapshot.ToArray());
            diagnostic = string.Empty;
            return true;
        }

        internal bool TryGetGroupsForActor(
            ActorId actorId,
            int maximumCount,
            out IReadOnlyList<GroupId> groups,
            out string diagnostic)
        {
            groups = Array.Empty<GroupId>();
            if (!actorId.IsValid)
            {
                diagnostic = "Actor Group lookup requires a valid ActorId.";
                return false;
            }
            if (maximumCount < 0)
            {
                diagnostic = "Actor Group membership snapshot limit cannot be negative.";
                return false;
            }
            if (!_groupsByActor.TryGetValue(actorId, out HashSet<GroupId> actorGroups))
            {
                diagnostic = string.Empty;
                return true;
            }
            if (actorGroups.Count > maximumCount)
            {
                diagnostic = $"Actor Group membership snapshot contains {actorGroups.Count} Groups, exceeding the requested limit of {maximumCount}.";
                return false;
            }

            var snapshot = new List<GroupId>(actorGroups);
            snapshot.Sort((left, right) => StringComparer.Ordinal.Compare(left.StableText, right.StableText));
            groups = Array.AsReadOnly(snapshot.ToArray());
            diagnostic = string.Empty;
            return true;
        }

        private bool TryGetLiveGroup(GroupId groupId, out GroupRecord record, out string diagnostic)
        {
            if (!groupId.IsValid)
            {
                record = null;
                diagnostic = "A valid GroupId is required for Actor Group membership.";
                return false;
            }

            if (_groups.TryGetValue(groupId, out record))
            {
                diagnostic = string.Empty;
                return true;
            }

            diagnostic = $"No live Actor Group has identity '{groupId.StableText}'.";
            return false;
        }

        private static bool IsSupportedScope(RuntimeContentScope scope) =>
            scope == RuntimeContentScope.Session ||
            scope == RuntimeContentScope.Route ||
            scope == RuntimeContentScope.Activity;

        private void RemoveStagedOwner(RuntimeContentOwner owner, OwnerRegistration registration)
        {
            _stagedOwners.Remove(owner);
            RemoveBindingReservations(registration);
        }

        private void ReleaseCommittedOwner(RuntimeContentOwner owner)
        {
            OwnerRegistration registration = _committedOwners[owner];
            _committedOwners.Remove(owner);
            for (int index = 0; index < registration.Bindings.Count; index++)
                registration.Bindings[index]?.UnbindRuntime(this, owner);
            RemoveBindingReservations(registration);

            var releasedGroups = new List<GroupId>();
            foreach (KeyValuePair<GroupId, GroupRecord> pair in _groups)
                if (pair.Value.Owner == owner) releasedGroups.Add(pair.Key);
            for (int index = 0; index < releasedGroups.Count; index++)
            {
                GroupId groupId = releasedGroups[index];
                GroupRecord record = _groups[groupId];
                foreach (ActorId actorId in record.Members)
                {
                    if (!_groupsByActor.TryGetValue(actorId, out HashSet<GroupId> groups)) continue;
                    groups.Remove(groupId);
                    if (groups.Count == 0) _groupsByActor.Remove(actorId);
                }

                _groups.Remove(groupId);
            }
        }

        private void RemoveBindingReservations(OwnerRegistration registration)
        {
            for (int index = 0; index < registration.Bindings.Count; index++)
                _bindingOwners.Remove(registration.Bindings[index]);
        }
    }
}
