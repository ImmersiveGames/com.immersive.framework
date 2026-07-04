using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;
using UnityObject = UnityEngine.Object;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Runtime source of truth for reset subjects and reset participants.
    /// This registry does not consult ObjectEntryRuntimeContextSnapshot and does not resolve ObjectEntry targets.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A Reset subject registry independent from ObjectEntry.")]
    public sealed class ResetRegistry
    {
        private readonly Dictionary<ResetRegistrationHandle, ResetSubjectRecord> _subjectsByHandle = new();
        private readonly Dictionary<ResetSubjectRegistryKey, ResetRegistrationHandle> _subjectHandlesByKey = new();
        private readonly Dictionary<ResetSubjectId, List<ResetRegistrationHandle>> _subjectHandlesById = new();
        private readonly Dictionary<ResetRegistrationHandle, ResetParticipantRecord> _participantsByHandle = new();
        private readonly Dictionary<ResetParticipantRegistryKey, ResetRegistrationHandle> _participantHandlesByKey = new();
        private readonly Dictionary<string, int> _runtimeCountersByPrefix = new(StringComparer.Ordinal);
        private int _nextHandle;

        public int SubjectCount => _subjectsByHandle.Count;

        public int ParticipantCount => _participantsByHandle.Count;

        public bool HasSubjects => SubjectCount > 0;

        public bool HasParticipants => ParticipantCount > 0;

        public bool TryGenerateRuntimeSubjectId(
            string authoredPrefix,
            out ResetSubjectId subjectId,
            out ResetIssue issue)
        {
            subjectId = default;
            issue = default;

            string prefix = authoredPrefix.NormalizeText();
            if (string.IsNullOrWhiteSpace(prefix))
            {
                issue = ResetIssue.Error(
                    ResetIssueKind.InvalidSubject,
                    "Runtime reset subject id generation requires a non-empty authored prefix.");
                return false;
            }

            int next = _runtimeCountersByPrefix.TryGetValue(prefix, out int current) ? current + 1 : 1;
            _runtimeCountersByPrefix[prefix] = next;
            subjectId = ResetSubjectId.From($"{prefix}#{next}");
            return true;
        }

        public ResetRegistryOperationResult RegisterRuntimeSubject(
            string authoredPrefix,
            ResetSubjectScope scope,
            RuntimeContentOwner owner,
            object ownerObject,
            string displayName,
            string diagnosticTag,
            string source,
            string reason)
        {
            if (!TryGenerateRuntimeSubjectId(authoredPrefix, out ResetSubjectId subjectId, out ResetIssue issue))
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedInvalidSubject,
                    issue,
                    "Runtime reset subject registration rejected because id generation failed.");
            }

            ResetSubject subject;
            try
            {
                subject = new ResetSubject(
                    subjectId,
                    scope,
                    ResetSubjectOrigin.RuntimeRegistered,
                    owner,
                    displayName,
                    diagnosticTag);
            }
            catch (Exception exception)
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedInvalidSubject,
                    ResetIssue.Error(ResetIssueKind.InvalidSubject, exception.Message),
                    "Runtime reset subject registration rejected because descriptor creation failed.");
            }

            return RegisterSubject(subject, ownerObject, source, reason);
        }

        public bool TryRegisterRuntimeSubject(
            string authoredPrefix,
            ResetSubjectScope scope,
            RuntimeContentOwner owner,
            object ownerObject,
            string displayName,
            string diagnosticTag,
            string source,
            string reason,
            out ResetRegistrationHandle handle,
            out ResetSubject subject,
            out ResetIssue issue)
        {
            var result = RegisterRuntimeSubject(authoredPrefix, scope, owner, ownerObject, displayName, diagnosticTag, source, reason);
            handle = result.Handle;
            subject = result.Subject;
            issue = result.Issues.Count > 0 ? result.Issues[0] : default;
            return result.Succeeded;
        }

        public ResetRegistryOperationResult RegisterSubject(
            ResetSubject subject,
            object ownerObject,
            string source,
            string reason)
        {
            if (!subject.IsValid)
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedInvalidSubject,
                    ResetIssue.Error(ResetIssueKind.InvalidSubject, "Reset subject registration requires a valid subject."),
                    "Reset subject registration rejected because the subject is invalid.");
            }

            if (!HasLiveOwner(ownerObject))
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedInvalidRequest,
                    ResetIssue.Error(
                        ResetIssueKind.StaleOwner,
                        $"Reset subject '{subject.SubjectId.StableText}' registration requires a live owner object."),
                    "Reset subject registration rejected because owner object is missing or stale.");
            }

            var key = ResetSubjectRegistryKey.From(subject);
            if (_subjectHandlesByKey.ContainsKey(key))
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedDuplicateSubjectId,
                    ResetIssue.Error(
                        ResetIssueKind.DuplicateSubject,
                        $"Reset subject duplicate id '{subject.SubjectId.StableText}' in scope '{subject.Scope}' owner '{subject.OwnerStableText.ToDiagnosticText("<none>")}'."),
                    "Reset subject registration rejected because the same subject id is already registered in this context.");
            }

            var handle = NextHandle(ResetRegistrationKind.Subject);
            var record = new ResetSubjectRecord(handle, subject, ownerObject, source, reason);
            _subjectsByHandle.Add(handle, record);
            _subjectHandlesByKey.Add(key, handle);
            if (!_subjectHandlesById.TryGetValue(subject.SubjectId, out var handles))
            {
                handles = new List<ResetRegistrationHandle>();
                _subjectHandlesById.Add(subject.SubjectId, handles);
            }

            handles.Add(handle);
            return ResetRegistryOperationResult.RegisteredSubject(handle, subject, "Reset subject registered.");
        }

        public bool TryRegisterSubject(
            ResetSubject subject,
            object ownerObject,
            string source,
            string reason,
            out ResetRegistrationHandle handle,
            out ResetIssue issue)
        {
            var result = RegisterSubject(subject, ownerObject, source, reason);
            handle = result.Handle;
            issue = result.Issues.Count > 0 ? result.Issues[0] : default;
            return result.Succeeded;
        }

        public ResetRegistryOperationResult RegisterParticipant(
            ResetRegistrationHandle subjectHandle,
            IResetParticipant participant,
            object ownerObject,
            string source,
            string reason)
        {
            if (!TryGetSubjectRecord(subjectHandle, out var subjectRecord))
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedSubjectNotFound,
                    ResetIssue.Error(ResetIssueKind.SubjectNotFound, $"Reset participant registration requires a registered subject handle. handle='{subjectHandle}'."),
                    "Reset participant registration rejected because the subject handle is not registered.");
            }

            if (participant == null)
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedInvalidParticipant,
                    ResetIssue.Error(ResetIssueKind.InvalidParticipant, $"Reset participant registration for subject '{subjectRecord.Subject.SubjectId.StableText}' requires a participant instance."),
                    "Reset participant registration rejected because participant is missing.");
            }

            ResetParticipantDescriptor descriptor;
            try
            {
                if (!participant.TryCreateResetParticipantDescriptor(subjectRecord.Subject, out descriptor, out ResetIssue issue))
                {
                    return ResetRegistryOperationResult.Rejected(
                        ResetRegistryOperationStatus.RejectedInvalidParticipant,
                        issue.IsBlocking ? issue : ResetIssue.Error(ResetIssueKind.InvalidParticipant, issue.Message),
                        "Reset participant registration rejected because descriptor creation failed.");
                }
            }
            catch (Exception exception)
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedInvalidParticipant,
                    ResetIssue.Error(ResetIssueKind.Exception, exception.Message),
                    "Reset participant registration rejected because descriptor creation threw.");
            }

            return RegisterParticipant(subjectHandle, descriptor, participant, ownerObject, source, reason);
        }

        public ResetRegistryOperationResult RegisterParticipant(
            ResetRegistrationHandle subjectHandle,
            ResetParticipantDescriptor descriptor,
            IResetParticipant participant,
            object ownerObject,
            string source,
            string reason)
        {
            if (!TryGetSubjectRecord(subjectHandle, out var subjectRecord))
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedSubjectNotFound,
                    ResetIssue.Error(ResetIssueKind.SubjectNotFound, $"Reset participant registration requires a registered subject handle. handle='{subjectHandle}'."),
                    "Reset participant registration rejected because the subject handle is not registered.");
            }

            if (!descriptor.IsValid)
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedInvalidParticipant,
                    ResetIssue.Error(ResetIssueKind.InvalidParticipant, "Reset participant registration requires a valid descriptor."),
                    "Reset participant registration rejected because descriptor is invalid.");
            }

            if (descriptor.SubjectId != subjectRecord.Subject.SubjectId)
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedInvalidParticipant,
                    ResetIssue.Error(
                        ResetIssueKind.InvalidParticipant,
                        $"Reset participant subject '{descriptor.SubjectId.StableText}' does not match registered subject '{subjectRecord.Subject.SubjectId.StableText}'."),
                    "Reset participant registration rejected because descriptor belongs to another subject.");
            }

            if (participant == null)
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedInvalidParticipant,
                    ResetIssue.Error(ResetIssueKind.InvalidParticipant, $"Reset participant '{descriptor.ParticipantId.StableText}' registration requires a participant instance."),
                    "Reset participant registration rejected because participant is missing.");
            }

            if (!HasLiveOwner(ownerObject))
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedInvalidRequest,
                    ResetIssue.Error(
                        ResetIssueKind.StaleOwner,
                        $"Reset participant '{descriptor.ParticipantId.StableText}' registration requires a live owner object."),
                    "Reset participant registration rejected because owner object is missing or stale.");
            }

            var participantKey = new ResetParticipantRegistryKey(subjectHandle, descriptor.ParticipantId);
            if (_participantHandlesByKey.ContainsKey(participantKey))
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedDuplicateParticipantId,
                    ResetIssue.Error(
                        ResetIssueKind.DuplicateParticipant,
                        $"Reset participant duplicate id '{descriptor.ParticipantId.StableText}' for subject '{descriptor.SubjectId.StableText}'."),
                    "Reset participant registration rejected because this subject already has a participant with the same id.");
            }

            var participantHandle = NextHandle(ResetRegistrationKind.Participant);
            var record = new ResetParticipantRecord(
                participantHandle,
                subjectHandle,
                subjectRecord.Subject,
                descriptor,
                participant,
                ownerObject,
                source,
                reason);

            _participantsByHandle.Add(participantHandle, record);
            _participantHandlesByKey.Add(participantKey, participantHandle);
            subjectRecord.AddParticipant(participantHandle);
            return ResetRegistryOperationResult.RegisteredParticipant(participantHandle, subjectRecord.Subject, descriptor, "Reset participant registered.");
        }

        public ResetRegistryOperationResult RegisterParticipant(
            ResetParticipantDescriptor descriptor,
            IResetParticipant participant,
            object ownerObject,
            string source,
            string reason)
        {
            if (!descriptor.IsValid)
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedInvalidParticipant,
                    ResetIssue.Error(ResetIssueKind.InvalidParticipant, "Reset participant registration requires a valid descriptor."),
                    "Reset participant registration rejected because descriptor is invalid.");
            }

            if (!TryGetUniqueSubjectHandle(descriptor.SubjectId, out ResetRegistrationHandle subjectHandle))
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedSubjectNotFound,
                    ResetIssue.Error(ResetIssueKind.SubjectNotFound, $"Reset participant registration could not resolve a unique subject '{descriptor.SubjectId.StableText}'."),
                    "Reset participant registration rejected because the subject is missing or ambiguous.");
            }

            return RegisterParticipant(subjectHandle, descriptor, participant, ownerObject, source, reason);
        }

        public bool TryRegisterParticipant(
            ResetRegistrationHandle subjectHandle,
            IResetParticipant participant,
            object ownerObject,
            string source,
            string reason,
            out ResetRegistrationHandle participantHandle,
            out ResetParticipantDescriptor descriptor,
            out ResetIssue issue)
        {
            var result = RegisterParticipant(subjectHandle, participant, ownerObject, source, reason);
            participantHandle = result.Handle;
            descriptor = result.Participant;
            issue = result.Issues.Count > 0 ? result.Issues[0] : default;
            return result.Succeeded;
        }

        public ResetRegistryOperationResult Unregister(ResetRegistrationHandle handle, object ownerObject, string source, string reason)
        {
            if (!handle.IsValid)
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedInvalidHandle,
                    ResetIssue.Warning(ResetIssueKind.InvalidHandle, "Reset registry unregister received an invalid handle."),
                    "Reset registry unregister ignored an invalid handle.");
            }

            if (handle.IsSubject)
            {
                return UnregisterSubject(handle, ownerObject, source, reason);
            }

            if (handle.IsParticipant)
            {
                return UnregisterParticipant(handle, ownerObject, source, reason);
            }

            return ResetRegistryOperationResult.Rejected(
                ResetRegistryOperationStatus.RejectedInvalidHandle,
                ResetIssue.Warning(ResetIssueKind.InvalidHandle, $"Reset registry unregister received unsupported handle kind '{handle.Kind}'."),
                "Reset registry unregister ignored an unsupported handle.");
        }

        public bool TryGetSubject(ResetSubjectId subjectId, out ResetSubject subject)
        {
            CleanupStaleOwners();

            if (!subjectId.IsValid || !TryGetUniqueSubjectHandle(subjectId, out ResetRegistrationHandle handle))
            {
                subject = default;
                return false;
            }

            if (_subjectsByHandle.TryGetValue(handle, out var record) && record != null && record.HasLiveOwner)
            {
                subject = record.Subject;
                return true;
            }

            subject = default;
            return false;
        }

        public bool TryGetSubject(ResetRegistrationHandle handle, out ResetSubject subject)
        {
            CleanupStaleOwners();

            if (TryGetSubjectRecord(handle, out var record))
            {
                subject = record.Subject;
                return true;
            }

            subject = default;
            return false;
        }

        public bool TryGetParticipant(
            ResetRegistrationHandle handle,
            out ResetParticipantDescriptor descriptor,
            out IResetParticipant participant)
        {
            CleanupStaleOwners();

            if (handle.IsParticipant
                && _participantsByHandle.TryGetValue(handle, out var record)
                && record != null
                && record.HasLiveOwner)
            {
                descriptor = record.Descriptor;
                participant = record.Participant;
                return true;
            }

            descriptor = default;
            participant = null;
            return false;
        }

        public IReadOnlyList<ResetSubject> SnapshotSubjects()
        {
            CleanupStaleOwners();
            return _subjectsByHandle.Values
                .Where(record => record != null && record.HasLiveOwner)
                .Select(record => record.Subject)
                .ToArray();
        }

        public IReadOnlyList<ResetSubject> GetSubjectsByScope(ResetSubjectScope scope)
        {
            CleanupStaleOwners();
            return _subjectsByHandle.Values
                .Where(record => record != null && record.HasLiveOwner && record.Subject.Scope == scope)
                .Select(record => record.Subject)
                .ToArray();
        }

        public IReadOnlyList<ResetSubject> GetSubjectsByOrigin(ResetSubjectOrigin origin)
        {
            CleanupStaleOwners();
            return _subjectsByHandle.Values
                .Where(record => record != null && record.HasLiveOwner && record.Subject.Origin == origin)
                .Select(record => record.Subject)
                .ToArray();
        }

        public IReadOnlyList<ResetSubject> GetSubjectsByOwner(RuntimeContentOwner owner)
        {
            CleanupStaleOwners();
            if (!owner.IsValid)
            {
                return Array.Empty<ResetSubject>();
            }

            return _subjectsByHandle.Values
                .Where(record => record != null && record.HasLiveOwner && record.Subject.HasOwner && record.Subject.Owner.Equals(owner))
                .Select(record => record.Subject)
                .ToArray();
        }

        public IReadOnlyList<ResetSubject> GetSubjectsByScopeAndOwner(ResetSubjectScope scope, RuntimeContentOwner owner)
        {
            CleanupStaleOwners();
            if (!owner.IsValid)
            {
                return Array.Empty<ResetSubject>();
            }

            return _subjectsByHandle.Values
                .Where(record => record != null
                    && record.HasLiveOwner
                    && record.Subject.Scope == scope
                    && record.Subject.HasOwner
                    && record.Subject.Owner.Equals(owner))
                .Select(record => record.Subject)
                .ToArray();
        }

        public IReadOnlyList<ResetParticipantDescriptor> GetParticipants(ResetRegistrationHandle subjectHandle)
        {
            CleanupStaleOwners();

            if (!TryGetSubjectRecord(subjectHandle, out var subjectRecord))
            {
                return Array.Empty<ResetParticipantDescriptor>();
            }

            return subjectRecord.ParticipantHandles
                .Where(handle => _participantsByHandle.TryGetValue(handle, out var participantRecord)
                    && participantRecord != null
                    && participantRecord.HasLiveOwner)
                .Select(handle => _participantsByHandle[handle].Descriptor)
                .OrderBy(descriptor => descriptor.Order)
                .ThenBy(descriptor => descriptor.ParticipantId.StableText, StringComparer.Ordinal)
                .ToArray();
        }

        public IReadOnlyList<ResetParticipantDescriptor> GetParticipants(ResetSubjectId subjectId)
        {
            CleanupStaleOwners();

            if (!subjectId.IsValid || !_subjectHandlesById.TryGetValue(subjectId, out var subjectHandles))
            {
                return Array.Empty<ResetParticipantDescriptor>();
            }

            var participants = new List<ResetParticipantDescriptor>();
            for (int i = 0; i < subjectHandles.Count; i++)
            {
                participants.AddRange(GetParticipants(subjectHandles[i]));
            }

            return participants
                .OrderBy(descriptor => descriptor.Order)
                .ThenBy(descriptor => descriptor.ParticipantId.StableText, StringComparer.Ordinal)
                .ToArray();
        }


        internal bool TryGetSubjectHandle(
            ResetSubjectId subjectId,
            out ResetRegistrationHandle handle,
            out ResetSubject subject,
            out ResetIssue issue)
        {
            CleanupStaleOwners();
            handle = default;
            subject = default;
            issue = default;

            if (!subjectId.IsValid)
            {
                issue = ResetIssue.Error(ResetIssueKind.InvalidSubject, "Reset subject handle resolution requires a valid subject id.");
                return false;
            }

            if (!_subjectHandlesById.TryGetValue(subjectId, out var handles) || handles.Count == 0)
            {
                issue = ResetIssue.Error(ResetIssueKind.SubjectNotFound, $"Reset subject '{subjectId.StableText}' is not registered.");
                return false;
            }

            ResetRegistrationHandle found = default;
            ResetSubject foundSubject = default;
            int liveCount = 0;
            for (int i = 0; i < handles.Count; i++)
            {
                if (_subjectsByHandle.TryGetValue(handles[i], out var record) && record != null && record.HasLiveOwner)
                {
                    found = handles[i];
                    foundSubject = record.Subject;
                    liveCount++;
                }
            }

            if (liveCount != 1)
            {
                issue = ResetIssue.Error(
                    ResetIssueKind.SubjectNotFound,
                    $"Reset subject '{subjectId.StableText}' could not be resolved uniquely. liveMatches='{liveCount}'.");
                return false;
            }

            handle = found;
            subject = foundSubject;
            return true;
        }

        internal IReadOnlyList<ResetParticipantRuntimeEntry> GetParticipantRuntimeEntries(ResetRegistrationHandle subjectHandle)
        {
            CleanupStaleOwners();

            if (!TryGetSubjectRecord(subjectHandle, out var subjectRecord))
            {
                return Array.Empty<ResetParticipantRuntimeEntry>();
            }

            return subjectRecord.ParticipantHandles
                .Where(handle => _participantsByHandle.TryGetValue(handle, out var participantRecord)
                    && participantRecord != null
                    && participantRecord.HasLiveOwner
                    && participantRecord.Participant != null)
                .Select(handle => new ResetParticipantRuntimeEntry(
                    handle,
                    _participantsByHandle[handle].Descriptor,
                    _participantsByHandle[handle].Participant))
                .Where(entry => entry.IsValid)
                .OrderBy(entry => entry.Descriptor.Order)
                .ThenBy(entry => entry.Descriptor.ParticipantId.StableText, StringComparer.Ordinal)
                .ToArray();
        }

        public ResetRegistryCleanupResult CleanupStaleOwners()
        {
            var staleParticipants = new List<ResetRegistrationHandle>();
            var staleSubjects = new List<ResetRegistrationHandle>();

            foreach (var pair in _participantsByHandle)
            {
                if (pair.Value == null || !pair.Value.HasLiveOwner)
                {
                    staleParticipants.Add(pair.Key);
                }
            }

            foreach (var pair in _subjectsByHandle)
            {
                if (pair.Value == null || !pair.Value.HasLiveOwner)
                {
                    staleSubjects.Add(pair.Key);
                }
            }

            int removedParticipants = 0;
            for (int i = 0; i < staleParticipants.Count; i++)
            {
                if (RemoveParticipant(staleParticipants[i], removeFromSubject: true))
                {
                    removedParticipants++;
                }
            }

            int removedSubjects = 0;
            for (int i = 0; i < staleSubjects.Count; i++)
            {
                if (RemoveSubject(staleSubjects[i], out int subjectParticipantRemovals))
                {
                    removedSubjects++;
                    removedParticipants += subjectParticipantRemovals;
                }
            }

            if (removedSubjects == 0 && removedParticipants == 0)
            {
                return new ResetRegistryCleanupResult(0, 0, Array.Empty<ResetIssue>());
            }

            var issue = ResetIssue.Warning(
                ResetIssueKind.StaleOwner,
                $"Reset registry removed stale records. subjects='{removedSubjects}' participants='{removedParticipants}'.");
            return new ResetRegistryCleanupResult(removedSubjects, removedParticipants, new[] { issue });
        }

        private ResetRegistryOperationResult UnregisterSubject(
            ResetRegistrationHandle handle,
            object ownerObject,
            string source,
            string reason)
        {
            if (!_subjectsByHandle.TryGetValue(handle, out var record) || record == null)
            {
                return ResetRegistryOperationResult.AlreadyUnregistered(
                    handle,
                    ResetIssue.Warning(ResetIssueKind.InvalidHandle, $"Reset subject handle '{handle}' is not registered."),
                    "Reset subject unregister ignored because the handle is already unregistered.");
            }

            if (!MatchesOwner(record.OwnerObject, ownerObject))
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedForeignOwner,
                    ResetIssue.Error(ResetIssueKind.ForeignOwner, $"Reset subject handle '{handle}' is owned by another object."),
                    "Reset subject unregister rejected because owner does not match.");
            }

            ResetSubject subject = record.Subject;
            int removedParticipants;
            RemoveSubject(handle, out removedParticipants);
            return ResetRegistryOperationResult.Unregistered(handle, subject, default, "Reset subject unregistered.");
        }

        private ResetRegistryOperationResult UnregisterParticipant(
            ResetRegistrationHandle handle,
            object ownerObject,
            string source,
            string reason)
        {
            if (!_participantsByHandle.TryGetValue(handle, out var record) || record == null)
            {
                return ResetRegistryOperationResult.AlreadyUnregistered(
                    handle,
                    ResetIssue.Warning(ResetIssueKind.InvalidHandle, $"Reset participant handle '{handle}' is not registered."),
                    "Reset participant unregister ignored because the handle is already unregistered.");
            }

            if (!MatchesOwner(record.OwnerObject, ownerObject))
            {
                return ResetRegistryOperationResult.Rejected(
                    ResetRegistryOperationStatus.RejectedForeignOwner,
                    ResetIssue.Error(ResetIssueKind.ForeignOwner, $"Reset participant handle '{handle}' is owned by another object."),
                    "Reset participant unregister rejected because owner does not match.");
            }

            var descriptor = record.Descriptor;
            var subject = record.Subject;
            RemoveParticipant(handle, removeFromSubject: true);
            return ResetRegistryOperationResult.Unregistered(handle, subject, descriptor, "Reset participant unregistered.");
        }

        private ResetRegistrationHandle NextHandle(ResetRegistrationKind kind)
        {
            return new ResetRegistrationHandle(kind, ++_nextHandle);
        }

        private bool TryGetSubjectRecord(ResetRegistrationHandle handle, out ResetSubjectRecord record)
        {
            if (handle.IsSubject
                && _subjectsByHandle.TryGetValue(handle, out record)
                && record != null
                && record.HasLiveOwner)
            {
                return true;
            }

            record = null;
            return false;
        }

        private bool TryGetUniqueSubjectHandle(ResetSubjectId subjectId, out ResetRegistrationHandle handle)
        {
            handle = default;
            if (!subjectId.IsValid || !_subjectHandlesById.TryGetValue(subjectId, out var handles))
            {
                return false;
            }

            ResetRegistrationHandle found = default;
            int liveCount = 0;
            for (int i = 0; i < handles.Count; i++)
            {
                if (_subjectsByHandle.TryGetValue(handles[i], out var record) && record != null && record.HasLiveOwner)
                {
                    found = handles[i];
                    liveCount++;
                }
            }

            if (liveCount != 1)
            {
                return false;
            }

            handle = found;
            return true;
        }

        private bool RemoveSubject(ResetRegistrationHandle handle, out int removedParticipants)
        {
            removedParticipants = 0;
            if (!_subjectsByHandle.TryGetValue(handle, out var record) || record == null)
            {
                return false;
            }

            var participantHandles = record.ParticipantHandles.ToArray();
            for (int i = 0; i < participantHandles.Length; i++)
            {
                if (RemoveParticipant(participantHandles[i], removeFromSubject: false))
                {
                    removedParticipants++;
                }
            }

            _subjectsByHandle.Remove(handle);
            _subjectHandlesByKey.Remove(ResetSubjectRegistryKey.From(record.Subject));
            if (_subjectHandlesById.TryGetValue(record.Subject.SubjectId, out var handles))
            {
                handles.Remove(handle);
                if (handles.Count == 0)
                {
                    _subjectHandlesById.Remove(record.Subject.SubjectId);
                }
            }

            return true;
        }

        private bool RemoveParticipant(ResetRegistrationHandle handle, bool removeFromSubject)
        {
            if (!_participantsByHandle.TryGetValue(handle, out var record) || record == null)
            {
                return false;
            }

            _participantsByHandle.Remove(handle);
            _participantHandlesByKey.Remove(new ResetParticipantRegistryKey(record.SubjectHandle, record.Descriptor.ParticipantId));
            if (removeFromSubject && _subjectsByHandle.TryGetValue(record.SubjectHandle, out var subjectRecord) && subjectRecord != null)
            {
                subjectRecord.RemoveParticipant(handle);
            }

            return true;
        }

        private static bool HasLiveOwner(object ownerObject)
        {
            if (ownerObject == null)
            {
                return false;
            }

            if (ownerObject is UnityObject unityObject)
            {
                return unityObject != null;
            }

            return true;
        }

        private static bool MatchesOwner(object registeredOwner, object requestOwner)
        {
            if (requestOwner == null)
            {
                return true;
            }

            if (registeredOwner == null)
            {
                return false;
            }

            return ReferenceEquals(registeredOwner, requestOwner);
        }

        private readonly struct ResetSubjectRegistryKey : IEquatable<ResetSubjectRegistryKey>
        {
            private ResetSubjectRegistryKey(ResetSubjectId subjectId, ResetSubjectScope scope, RuntimeContentOwner owner)
            {
                SubjectId = subjectId;
                Scope = scope;
                Owner = owner;
            }

            private ResetSubjectId SubjectId { get; }

            private ResetSubjectScope Scope { get; }

            private RuntimeContentOwner Owner { get; }

            internal static ResetSubjectRegistryKey From(ResetSubject subject)
            {
                return new ResetSubjectRegistryKey(subject.SubjectId, subject.Scope, subject.Owner);
            }

            public bool Equals(ResetSubjectRegistryKey other)
            {
                return SubjectId.Equals(other.SubjectId) && Scope == other.Scope && Owner.Equals(other.Owner);
            }

            public override bool Equals(object obj)
            {
                return obj is ResetSubjectRegistryKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = SubjectId.GetHashCode();
                    hashCode = hashCode * 397 ^ (int)Scope;
                    hashCode = hashCode * 397 ^ Owner.GetHashCode();
                    return hashCode;
                }
            }
        }

        private readonly struct ResetParticipantRegistryKey : IEquatable<ResetParticipantRegistryKey>
        {
            internal ResetParticipantRegistryKey(ResetRegistrationHandle subjectHandle, ResetParticipantId participantId)
            {
                SubjectHandle = subjectHandle;
                ParticipantId = participantId;
            }

            private ResetRegistrationHandle SubjectHandle { get; }

            private ResetParticipantId ParticipantId { get; }

            public bool Equals(ResetParticipantRegistryKey other)
            {
                return SubjectHandle.Equals(other.SubjectHandle) && ParticipantId.Equals(other.ParticipantId);
            }

            public override bool Equals(object obj)
            {
                return obj is ResetParticipantRegistryKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return SubjectHandle.GetHashCode() * 397 ^ ParticipantId.GetHashCode();
                }
            }
        }

        private sealed class ResetSubjectRecord
        {
            private readonly List<ResetRegistrationHandle> _participantHandles = new();

            internal ResetSubjectRecord(
                ResetRegistrationHandle handle,
                ResetSubject subject,
                object ownerObject,
                string source,
                string reason)
            {
                Handle = handle;
                Subject = subject;
                OwnerObject = ownerObject;
                Source = source.NormalizeText();
                Reason = reason.NormalizeText();
            }

            internal ResetRegistrationHandle Handle { get; }

            internal ResetSubject Subject { get; }

            internal object OwnerObject { get; }

            internal string Source { get; }

            internal string Reason { get; }

            internal bool HasLiveOwner => ResetRegistry.HasLiveOwner(OwnerObject);

            internal IReadOnlyList<ResetRegistrationHandle> ParticipantHandles => _participantHandles;

            internal void AddParticipant(ResetRegistrationHandle handle)
            {
                if (handle.IsParticipant && !_participantHandles.Contains(handle))
                {
                    _participantHandles.Add(handle);
                }
            }

            internal void RemoveParticipant(ResetRegistrationHandle handle)
            {
                _participantHandles.Remove(handle);
            }
        }

        private sealed class ResetParticipantRecord
        {
            internal ResetParticipantRecord(
                ResetRegistrationHandle handle,
                ResetRegistrationHandle subjectHandle,
                ResetSubject subject,
                ResetParticipantDescriptor descriptor,
                IResetParticipant participant,
                object ownerObject,
                string source,
                string reason)
            {
                Handle = handle;
                SubjectHandle = subjectHandle;
                Subject = subject;
                Descriptor = descriptor;
                Participant = participant;
                OwnerObject = ownerObject;
                Source = source.NormalizeText();
                Reason = reason.NormalizeText();
            }

            internal ResetRegistrationHandle Handle { get; }

            internal ResetRegistrationHandle SubjectHandle { get; }

            internal ResetSubject Subject { get; }

            internal ResetParticipantDescriptor Descriptor { get; }

            internal IResetParticipant Participant { get; }

            internal object OwnerObject { get; }

            internal string Source { get; }

            internal string Reason { get; }

            internal bool HasLiveOwner => ResetRegistry.HasLiveOwner(OwnerObject);
        }
    }
}
