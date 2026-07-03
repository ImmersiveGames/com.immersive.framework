using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.ObjectReset;
using UnityEngine;

namespace Immersive.Framework.RuntimeObjects
{
    /// <summary>
    /// API status: Internal. Runtime registry for objects instantiated or enabled after scene authoring.
    /// It supplies ObjectEntry descriptors and reset participants; it does not instantiate, pool, destroy, save or own actors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F44 runtime object participation registry; no PlayerActor or spawn manager semantics.")]
    internal sealed class RuntimeObjectParticipationRegistry : IObjectResetParticipantSource
    {
        private readonly Dictionary<RuntimeObjectParticipationHandle, RuntimeObjectParticipationRecord> _recordsByHandle = new();
        private readonly Dictionary<ObjectEntryId, RuntimeObjectParticipationHandle> _handlesByObjectEntryId = new();
        private int _nextHandle;

        internal int Count => _recordsByHandle.Count;

        internal bool HasRegistrations => _recordsByHandle.Count > 0;

        internal bool TryRegister(
            ObjectEntryDescriptor descriptor,
            IReadOnlyList<IObjectResetParticipant> resetParticipants,
            UnityEngine.Object owner,
            string source,
            string reason,
            out RuntimeObjectParticipationHandle handle,
            out string issue)
        {
            handle = default;
            issue = string.Empty;

            if (!descriptor.Id.IsValid)
            {
                issue = "Runtime object participation registration requires a valid ObjectEntry descriptor.";
                return false;
            }

            if (!descriptor.HasOwnerIdentity)
            {
                issue = $"Runtime object participation '{descriptor.Id.StableText}' requires a resolved owner identity.";
                return false;
            }

            if (owner == null)
            {
                issue = $"Runtime object participation '{descriptor.Id.StableText}' requires a live Unity owner.";
                return false;
            }

            if (_handlesByObjectEntryId.ContainsKey(descriptor.Id))
            {
                issue = $"Runtime object participation duplicate ObjectEntry Id '{descriptor.Id.StableText}'.";
                return false;
            }

            handle = new RuntimeObjectParticipationHandle(++_nextHandle);
            var record = new RuntimeObjectParticipationRecord(
                handle,
                descriptor,
                resetParticipants,
                owner,
                source,
                reason);
            _recordsByHandle.Add(handle, record);
            _handlesByObjectEntryId.Add(descriptor.Id, handle);
            return true;
        }

        internal bool TryUnregister(
            RuntimeObjectParticipationHandle handle,
            UnityEngine.Object owner,
            out ObjectEntryDescriptor descriptor,
            out string issue)
        {
            descriptor = default;
            issue = string.Empty;

            if (!handle.IsValid)
            {
                issue = "Runtime object participation unregister requires a valid handle.";
                return false;
            }

            if (!_recordsByHandle.TryGetValue(handle, out var record))
            {
                issue = $"Runtime object participation handle '{handle}' is not registered.";
                return false;
            }

            if (owner != null && record.Owner != null && !ReferenceEquals(record.Owner, owner))
            {
                issue = $"Runtime object participation handle '{handle}' is owned by another object.";
                return false;
            }

            descriptor = record.Descriptor;
            _recordsByHandle.Remove(handle);
            _handlesByObjectEntryId.Remove(record.Descriptor.Id);
            return true;
        }

        internal RuntimeObjectParticipationCollectionResult CollectScoped(ObjectEntryScopedCollectionContext context)
        {
            var descriptors = new List<ObjectEntryDescriptor>(_recordsByHandle.Count);
            var issues = new List<ObjectEntryIssue>();
            int candidateCount = 0;
            int filteredCount = 0;
            var staleHandles = new List<RuntimeObjectParticipationHandle>();

            foreach (var pair in _recordsByHandle)
            {
                var record = pair.Value;
                if (record == null)
                {
                    staleHandles.Add(pair.Key);
                    continue;
                }

                if (!record.HasLiveOwner)
                {
                    staleHandles.Add(pair.Key);
                    continue;
                }

                candidateCount++;
                ObjectEntryDescriptor descriptor = record.Descriptor;
                if (!context.TryResolveOwnerIdentity(descriptor.Scope, out FrameworkIdentityKey activeOwnerIdentity))
                {
                    filteredCount++;
                    continue;
                }

                if (!descriptor.HasOwnerIdentity || descriptor.OwnerIdentity.Value != activeOwnerIdentity)
                {
                    filteredCount++;
                    continue;
                }

                descriptors.Add(descriptor);
            }

            for (int i = 0; i < staleHandles.Count; i++)
            {
                if (_recordsByHandle.TryGetValue(staleHandles[i], out var staleRecord) && staleRecord != null)
                {
                    _handlesByObjectEntryId.Remove(staleRecord.Descriptor.Id);
                }

                _recordsByHandle.Remove(staleHandles[i]);
            }

            if (staleHandles.Count > 0)
            {
                issues.Add(ObjectEntryIssue.Warning(
                    ObjectEntryIssueKind.InvalidRequest,
                    $"Runtime object participation registry removed stale records. staleRecords='{staleHandles.Count}'."));
            }

            return new RuntimeObjectParticipationCollectionResult(
                descriptors,
                candidateCount,
                filteredCount,
                issues);
        }

        public IReadOnlyList<IObjectResetParticipant> ResolveObjectResetParticipants(
            ObjectResetRequest request,
            ObjectEntryDescriptor resolvedTarget)
        {
            if (!request.IsValid || !resolvedTarget.Id.IsValid)
            {
                return Array.Empty<IObjectResetParticipant>();
            }

            if (!_handlesByObjectEntryId.TryGetValue(resolvedTarget.Id, out RuntimeObjectParticipationHandle handle)
                || !_recordsByHandle.TryGetValue(handle, out RuntimeObjectParticipationRecord record)
                || record == null
                || !record.HasLiveOwner)
            {
                return Array.Empty<IObjectResetParticipant>();
            }

            var participants = new List<IObjectResetParticipant>(record.ResetParticipants.Count);
            for (int i = 0; i < record.ResetParticipants.Count; i++)
            {
                var participant = record.ResetParticipants[i];
                if (participant == null)
                {
                    continue;
                }

                ObjectResetParticipantDescriptor descriptor;
                try
                {
                    descriptor = participant.GetObjectResetDescriptor();
                }
                catch
                {
                    participants.Add(participant);
                    continue;
                }

                if (descriptor.SupportsResolvedTarget(request, resolvedTarget))
                {
                    participants.Add(participant);
                }
            }

            return participants;
        }
    }
}
