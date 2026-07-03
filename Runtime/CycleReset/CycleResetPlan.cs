using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Passive deterministic plan for one Cycle Reset operation.
    /// It selects ordered participants for a Route or Activity reset; it does not execute reset, release content, reload scenes or mutate gameplay.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11A Cycle Reset passive plan; no execution side effects.")]
    public readonly struct CycleResetPlan : IEquatable<CycleResetPlan>
    {
        private readonly CycleResetParticipantEntry[] _entries;
        private readonly CycleResetIssue[] _issues;

        public CycleResetPlan(
            CycleResetRequest request,
            IReadOnlyList<CycleResetParticipantEntry> entries,
            IReadOnlyList<CycleResetIssue> issues,
            CycleResetPlanStatus status,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(CycleResetPlanStatus), status) || status == CycleResetPlanStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Cycle Reset plan status must be explicit.");
            }

            Request = request;
            Status = status;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
            _entries = CopyEntries(entries);
            _issues = CopyIssues(issues);
        }

        public CycleResetRequest Request { get; }

        public CycleResetPlanStatus Status { get; }

        public IReadOnlyList<CycleResetParticipantEntry> Entries => _entries ?? Array.Empty<CycleResetParticipantEntry>();

        public IReadOnlyList<CycleResetIssue> Issues => _issues ?? Array.Empty<CycleResetIssue>();

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool IsValid => Request.IsValid && Status != CycleResetPlanStatus.Unknown;

        public bool Planned => Status == CycleResetPlanStatus.Planned;

        public bool Skipped => Status == CycleResetPlanStatus.SkippedNoParticipants;

        public bool Rejected => Status is CycleResetPlanStatus.RejectedInvalidRequest or CycleResetPlanStatus.RejectedInvalidParticipants;

        public bool HasEntries => EntryCount > 0;

        public bool HasIssues => IssueCount > 0;

        public int EntryCount => _entries?.Length ?? 0;

        public int IssueCount => _issues?.Length ?? 0;

        public int RouteParticipantCount => CountEntriesByScope(CycleResetScope.Route);

        public int ActivityParticipantCount => CountEntriesByScope(CycleResetScope.Activity);

        public int RequiredParticipantCount => CountEntriesByRequiredness(CycleResetParticipantRequiredness.Required);

        public int OptionalParticipantCount => CountEntriesByRequiredness(CycleResetParticipantRequiredness.Optional);

        public int BlockingIssueCount => CountIssues(blocking: true);

        public int NonBlockingIssueCount => CountIssues(blocking: false);

        public CycleResetParticipantEntry[] SnapshotEntries()
        {
            if (!HasEntries)
            {
                return Array.Empty<CycleResetParticipantEntry>();
            }

            var snapshot = new CycleResetParticipantEntry[EntryCount];
            for (int i = 0; i < EntryCount; i++)
            {
                snapshot[i] = Entries[i];
            }

            return snapshot;
        }

        public CycleResetIssue[] SnapshotIssues()
        {
            if (!HasIssues)
            {
                return Array.Empty<CycleResetIssue>();
            }

            var snapshot = new CycleResetIssue[IssueCount];
            for (int i = 0; i < IssueCount; i++)
            {
                snapshot[i] = Issues[i];
            }

            return snapshot;
        }

        public bool Equals(CycleResetPlan other)
        {
            if (!Request.Equals(other.Request)
                || Status != other.Status
                || !string.Equals(Source, other.Source, StringComparison.Ordinal)
                || !string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                || !string.Equals(Message, other.Message, StringComparison.Ordinal)
                || EntryCount != other.EntryCount
                || IssueCount != other.IssueCount)
            {
                return false;
            }

            for (int i = 0; i < EntryCount; i++)
            {
                if (!Entries[i].Equals(other.Entries[i]))
                {
                    return false;
                }
            }

            for (int i = 0; i < IssueCount; i++)
            {
                if (!Issues[i].Equals(other.Issues[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is CycleResetPlan other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Request.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ EntryCount;
                hashCode = hashCode * 397 ^ IssueCount;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append($"status='{Status}' {Request.ToDiagnosticString()} entries='{EntryCount}' routeParticipants='{RouteParticipantCount}' activityParticipants='{ActivityParticipantCount}' required='{RequiredParticipantCount}' optional='{OptionalParticipantCount}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}'");
            if (!string.IsNullOrWhiteSpace(Message))
            {
                builder.Append($" message='{Message}'");
            }

            return builder.ToString();
        }

        public static CycleResetPlan FromParticipants(
            CycleResetRequest request,
            IReadOnlyList<ICycleResetParticipant> participants,
            string source,
            string reason)
        {
            string sourceText = Normalize(source);
            string reasonText = Normalize(reason);

            if (!request.IsValid)
            {
                return new CycleResetPlan(
                    request,
                    Array.Empty<CycleResetParticipantEntry>(),
                    new[]
                    {
                        CycleResetIssue.BlockingIssue(
                            CycleResetIssueKind.InvalidDescriptor,
                            default,
                            CycleResetScope.Unknown,
                            "Cycle Reset plan rejected because the request is invalid.")
                    },
                    CycleResetPlanStatus.RejectedInvalidRequest,
                    sourceText,
                    reasonText,
                    "Cycle Reset plan rejected because the request is invalid.");
            }

            if (participants == null || participants.Count == 0)
            {
                return new CycleResetPlan(
                    request,
                    Array.Empty<CycleResetParticipantEntry>(),
                    Array.Empty<CycleResetIssue>(),
                    CycleResetPlanStatus.SkippedNoParticipants,
                    sourceText,
                    reasonText,
                    "Cycle Reset plan skipped because no participants were supplied.");
            }

            var entries = new List<CycleResetParticipantEntry>(participants.Count);
            var issues = new List<CycleResetIssue>();
            var ids = new HashSet<CycleResetParticipantId>();
            bool hasBlockingIssue = false;

            for (int i = 0; i < participants.Count; i++)
            {
                var participant = participants[i];
                if (participant == null)
                {
                    issues.Add(CycleResetIssue.BlockingIssue(
                        CycleResetIssueKind.NullParticipant,
                        default,
                        CycleResetScope.Unknown,
                        "Cycle Reset participant source contained a null participant."));
                    hasBlockingIssue = true;
                    continue;
                }

                CycleResetParticipantDescriptor descriptor;
                try
                {
                    descriptor = participant.GetCycleResetDescriptor();
                }
                catch (Exception exception)
                {
                    issues.Add(CycleResetIssue.BlockingIssue(
                        CycleResetIssueKind.InvalidDescriptor,
                        default,
                        CycleResetScope.Unknown,
                        $"Cycle Reset participant descriptor threw an exception: {exception.GetType().Name}."));
                    hasBlockingIssue = true;
                    continue;
                }

                if (!descriptor.IsValid)
                {
                    issues.Add(CycleResetIssue.BlockingIssue(
                        CycleResetIssueKind.InvalidDescriptor,
                        descriptor.ParticipantId,
                        descriptor.ParticipantScope,
                        "Cycle Reset participant descriptor is invalid."));
                    hasBlockingIssue = true;
                    continue;
                }

                if (!request.AcceptsParticipantScope(descriptor.ParticipantScope))
                {
                    issues.Add(CycleResetIssue.NonBlockingIssue(
                        CycleResetIssueKind.UnsupportedScope,
                        descriptor.ParticipantId,
                        descriptor.ParticipantScope,
                        "Cycle Reset participant scope is not part of this request."));
                    continue;
                }

                if (!ids.Add(descriptor.ParticipantId))
                {
                    issues.Add(CycleResetIssue.BlockingIssue(
                        CycleResetIssueKind.DuplicateParticipantId,
                        descriptor.ParticipantId,
                        descriptor.ParticipantScope,
                        "Cycle Reset participant id is duplicated in the same plan."));
                    hasBlockingIssue = true;
                    continue;
                }

                entries.Add(new CycleResetParticipantEntry(participant, descriptor, i));
            }

            if (hasBlockingIssue)
            {
                return new CycleResetPlan(
                    request,
                    Array.Empty<CycleResetParticipantEntry>(),
                    issues,
                    CycleResetPlanStatus.RejectedInvalidParticipants,
                    sourceText,
                    reasonText,
                    "Cycle Reset plan rejected because participant discovery produced blocking issues.");
            }

            entries.Sort(CompareEntries);

            if (entries.Count == 0)
            {
                return new CycleResetPlan(
                    request,
                    Array.Empty<CycleResetParticipantEntry>(),
                    issues,
                    CycleResetPlanStatus.SkippedNoParticipants,
                    sourceText,
                    reasonText,
                    "Cycle Reset plan skipped because no participants matched the request scope.");
            }

            return new CycleResetPlan(
                request,
                entries,
                issues,
                CycleResetPlanStatus.Planned,
                sourceText,
                reasonText,
                "Cycle Reset plan created with deterministic participant order.");
        }

        public static bool operator ==(CycleResetPlan left, CycleResetPlan right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CycleResetPlan left, CycleResetPlan right)
        {
            return !left.Equals(right);
        }

        private int CountEntriesByScope(CycleResetScope scope)
        {
            int count = 0;
            for (int i = 0; i < EntryCount; i++)
            {
                if (Entries[i].ParticipantScope == scope)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountEntriesByRequiredness(CycleResetParticipantRequiredness requiredness)
        {
            int count = 0;
            for (int i = 0; i < EntryCount; i++)
            {
                if (Entries[i].Requiredness == requiredness)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountIssues(bool blocking)
        {
            int count = 0;
            for (int i = 0; i < IssueCount; i++)
            {
                if (Issues[i].Blocking == blocking)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CompareEntries(CycleResetParticipantEntry left, CycleResetParticipantEntry right)
        {
            int scopeCompare = GetScopeOrder(left.ParticipantScope).CompareTo(GetScopeOrder(right.ParticipantScope));
            if (scopeCompare != 0)
            {
                return scopeCompare;
            }

            int orderCompare = left.Order.CompareTo(right.Order);
            if (orderCompare != 0)
            {
                return orderCompare;
            }

            return left.SourceIndex.CompareTo(right.SourceIndex);
        }

        private static int GetScopeOrder(CycleResetScope scope)
        {
            return scope == CycleResetScope.Route ? 0 : 1;
        }

        private static CycleResetParticipantEntry[] CopyEntries(IReadOnlyList<CycleResetParticipantEntry> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<CycleResetParticipantEntry>();
            }

            var copy = new CycleResetParticipantEntry[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                var entry = source[i];
                if (!entry.IsValid)
                {
                    throw new ArgumentException("Cycle Reset plan entries must be valid.", nameof(source));
                }

                copy[i] = entry;
            }

            return copy;
        }

        private static CycleResetIssue[] CopyIssues(IReadOnlyList<CycleResetIssue> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<CycleResetIssue>();
            }

            var copy = new CycleResetIssue[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
