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
        }

        private readonly Dictionary<RuntimeContentOwner, OwnerRegistration> _stagedOwners = new();
        private readonly Dictionary<RuntimeContentOwner, OwnerRegistration> _committedOwners = new();
        private readonly Dictionary<ActorGroupAccessBinding, RuntimeContentOwner> _bindingOwners = new();
        private readonly Dictionary<GroupId, GroupRecord> _groups = new();

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

            if (profile != null && !profile.TryGetGroupProfileId(out _, out diagnostic))
            {
                diagnostic = "Actor Group Profile is invalid. " + diagnostic;
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
                _groups.Remove(releasedGroups[index]);
        }

        private void RemoveBindingReservations(OwnerRegistration registration)
        {
            for (int index = 0; index < registration.Bindings.Count; index++)
                _bindingOwners.Remove(registration.Bindings[index]);
        }
    }
}
