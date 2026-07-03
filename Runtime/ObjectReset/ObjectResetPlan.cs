using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ObjectEntry;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Deterministic execution plan for one Object Reset request.
    /// A plan contains only an already-resolved ObjectEntry target and already-known participants; it is not discovery, binding, materialization or Unity reset.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14D Object Reset deterministic participant plan; no Unity baseline adapter yet.")]
    public sealed class ObjectResetPlan
    {
        private readonly ObjectResetParticipantEntry[] _participants;
        private readonly ObjectResetIssue[] _issues;

        public ObjectResetPlan(
            ObjectResetRequest request,
            ObjectEntryDescriptor resolvedTarget,
            IReadOnlyList<ObjectResetParticipantEntry> participants,
            IReadOnlyList<ObjectResetIssue> issues)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Object Reset plan requires a valid request.", nameof(request));
            }

            if (!request.Target.Matches(resolvedTarget))
            {
                throw new ArgumentException("Object Reset plan requires a resolved target matching the request target.", nameof(resolvedTarget));
            }

            Request = request;
            ResolvedTarget = resolvedTarget;
            _participants = (participants ?? Array.Empty<ObjectResetParticipantEntry>())
                .OrderBy(entry => entry.Descriptor.Order)
                .ThenBy(entry => entry.SourceIndex)
                .ThenBy(entry => entry.Descriptor.ParticipantId.StableText, StringComparer.Ordinal)
                .ToArray();
            _issues = issues == null ? Array.Empty<ObjectResetIssue>() : issues.ToArray();
        }

        public ObjectResetRequest Request { get; }

        public ObjectEntryDescriptor ResolvedTarget { get; }

        public IReadOnlyList<ObjectResetParticipantEntry> Participants => _participants ?? Array.Empty<ObjectResetParticipantEntry>();

        public IReadOnlyList<ObjectResetIssue> Issues => _issues ?? Array.Empty<ObjectResetIssue>();

        public int ParticipantCount => Participants.Count;

        public int RequiredParticipantCount => Participants.Count(entry => entry.IsRequired);

        public int OptionalParticipantCount => Participants.Count(entry => entry.IsOptional);

        public int IssueCount => Issues.Count;

        public int BlockingIssueCount => Issues.Count(issue => issue.IsBlocking);

        public int NonBlockingIssueCount => IssueCount - BlockingIssueCount;

        public bool HasParticipants => ParticipantCount > 0;

        public bool HasBlockingIssues => BlockingIssueCount > 0;

        public bool IsExecutable => Request.IsValid
            && Request.Target.Matches(ResolvedTarget)
            && !HasBlockingIssues;

        public ObjectResetParticipantEntry[] SnapshotParticipants()
        {
            if (ParticipantCount == 0)
            {
                return Array.Empty<ObjectResetParticipantEntry>();
            }

            var snapshot = new ObjectResetParticipantEntry[ParticipantCount];
            for (int i = 0; i < ParticipantCount; i++)
            {
                snapshot[i] = Participants[i];
            }

            return snapshot;
        }

        public ObjectResetIssue[] SnapshotIssues()
        {
            if (IssueCount == 0)
            {
                return Array.Empty<ObjectResetIssue>();
            }

            var snapshot = new ObjectResetIssue[IssueCount];
            for (int i = 0; i < IssueCount; i++)
            {
                snapshot[i] = Issues[i];
            }

            return snapshot;
        }

        public string Summary => $"participants='{ParticipantCount}' required='{RequiredParticipantCount}' optional='{OptionalParticipantCount}' planIssues='{IssueCount}' planBlockingIssues='{BlockingIssueCount}' planNonBlockingIssues='{NonBlockingIssueCount}'";
    }
}
