using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Resolved subject ids and diagnostics for one reset selection request.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12D reset selection resolution result.")]
    public readonly struct ResetSelectionResolution : IEquatable<ResetSelectionResolution>
    {
        private readonly IReadOnlyList<ResetSubjectId> _subjectIds;
        private readonly IReadOnlyList<ResetIssue> _issues;

        public ResetSelectionResolution(
            ResetSelectionResolutionStatus status,
            ResetSelectionMode mode,
            IReadOnlyList<ResetSubjectId> subjectIds,
            IReadOnlyList<ResetIssue> issues,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(ResetSelectionResolutionStatus), status)
                || status == ResetSelectionResolutionStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Reset selection resolution status must be explicit.");
            }

            if (!Enum.IsDefined(typeof(ResetSelectionMode), mode) || mode == ResetSelectionMode.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Reset selection mode must be explicit.");
            }

            Status = status;
            Mode = mode;
            _subjectIds = NormalizeSubjectIds(subjectIds);
            _issues = issues == null ? Array.Empty<ResetIssue>() : issues.ToArray();
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public ResetSelectionResolutionStatus Status { get; }

        public ResetSelectionMode Mode { get; }

        public IReadOnlyList<ResetSubjectId> SubjectIds => _subjectIds ?? Array.Empty<ResetSubjectId>();

        public IReadOnlyList<ResetIssue> Issues => _issues ?? Array.Empty<ResetIssue>();

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status == ResetSelectionResolutionStatus.Succeeded
            || Status == ResetSelectionResolutionStatus.SucceededNoSubjects;

        public bool Failed => !Succeeded;

        public bool HasSubjects => SubjectIds.Count > 0;

        public int SubjectCount => SubjectIds.Count;

        public int BlockingIssueCount => Issues.Count(issue => issue.IsBlocking);

        public int NonBlockingIssueCount => Issues.Count - BlockingIssueCount;

        public ResetExecutionRequest ToExecutionRequest(
            bool allowNoSubjects,
            bool allowNoParticipants,
            bool stopOnFailure,
            bool yieldBetweenSubjects)
        {
            return ResetExecutionRequest.ForSubjectIds(
                SubjectIds,
                allowNoSubjects,
                allowNoParticipants,
                stopOnFailure,
                Source,
                Reason,
                yieldBetweenSubjects);
        }

        public bool Equals(ResetSelectionResolution other)
        {
            return Status == other.Status
                && Mode == other.Mode
                && SubjectIds.SequenceEqual(other.SubjectIds)
                && Issues.SequenceEqual(other.Issues)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ResetSelectionResolution other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Status;
                hashCode = hashCode * 397 ^ (int)Mode;
                for (int i = 0; i < SubjectIds.Count; i++)
                {
                    hashCode = hashCode * 397 ^ SubjectIds[i].GetHashCode();
                }

                for (int i = 0; i < Issues.Count; i++)
                {
                    hashCode = hashCode * 397 ^ Issues[i].GetHashCode();
                }

                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            string subjects = string.Join(",", SubjectIds.Select(subjectId => subjectId.StableText));
            return $"status='{Status}' mode='{Mode}' subjects='{SubjectCount}' subjectIds='{subjects.ToDiagnosticText("<none>")}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}' source='{Source.ToDiagnosticText()}' reason='{Reason.ToDiagnosticText()}' message='{Message.ToDiagnosticText()}'";
        }

        public static ResetSelectionResolution SucceededResult(
            ResetSelectionMode mode,
            IReadOnlyList<ResetSubjectId> subjectIds,
            IReadOnlyList<ResetIssue> issues,
            string source,
            string reason,
            string message)
        {
            return new ResetSelectionResolution(
                subjectIds == null || subjectIds.Count == 0
                    ? ResetSelectionResolutionStatus.SucceededNoSubjects
                    : ResetSelectionResolutionStatus.Succeeded,
                mode,
                subjectIds,
                issues,
                source,
                reason,
                message);
        }

        public static ResetSelectionResolution FailedResult(
            ResetSelectionMode mode,
            ResetSelectionResolutionStatus status,
            ResetIssue issue,
            string source,
            string reason,
            string message)
        {
            return new ResetSelectionResolution(
                status,
                mode == ResetSelectionMode.Unknown ? ResetSelectionMode.ExplicitSubjects : mode,
                Array.Empty<ResetSubjectId>(),
                new[] { issue },
                source,
                reason,
                message);
        }

        private static IReadOnlyList<ResetSubjectId> NormalizeSubjectIds(IReadOnlyList<ResetSubjectId> subjectIds)
        {
            if (subjectIds == null || subjectIds.Count == 0)
            {
                return Array.Empty<ResetSubjectId>();
            }

            var normalized = new List<ResetSubjectId>(subjectIds.Count);
            var seen = new HashSet<ResetSubjectId>();
            for (int i = 0; i < subjectIds.Count; i++)
            {
                ResetSubjectId subjectId = subjectIds[i];
                if (!subjectId.IsValid || !seen.Add(subjectId))
                {
                    continue;
                }

                normalized.Add(subjectId);
            }

            return normalized.Count == 0 ? Array.Empty<ResetSubjectId>() : normalized.ToArray();
        }
    }
}
