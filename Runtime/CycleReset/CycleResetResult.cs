using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Aggregate result for one Cycle Reset operation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11A Cycle Reset aggregate result; diagnostics only.")]
    public readonly struct CycleResetResult : IEquatable<CycleResetResult>
    {
        private readonly CycleResetParticipantResult[] _participantResults;
        private readonly CycleResetIssue[] _issues;

        public CycleResetResult(
            CycleResetRequest request,
            CycleResetStatus status,
            IReadOnlyList<CycleResetParticipantResult> participantResults,
            IReadOnlyList<CycleResetIssue> issues,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(CycleResetStatus), status) || status == CycleResetStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Cycle Reset result status must be explicit.");
            }

            Request = request;
            Status = status;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
            _participantResults = CopyResults(participantResults);
            _issues = CopyIssues(issues);
        }

        public CycleResetRequest Request { get; }

        public CycleResetStatus Status { get; }

        public IReadOnlyList<CycleResetParticipantResult> ParticipantResults => _participantResults ?? Array.Empty<CycleResetParticipantResult>();

        public IReadOnlyList<CycleResetIssue> Issues => _issues ?? Array.Empty<CycleResetIssue>();

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status is CycleResetStatus.Succeeded or CycleResetStatus.SucceededNoParticipants;

        public bool CompletedWithWarnings => Status == CycleResetStatus.CompletedWithWarnings;

        public bool Failed => Status is CycleResetStatus.Failed or CycleResetStatus.RejectedInvalidRequest or CycleResetStatus.RejectedInvalidPlan;

        public int ParticipantCount => _participantResults?.Length ?? 0;

        public int IssueCount => _issues?.Length ?? 0;

        public int SucceededCount => CountResults(result => result.Succeeded);

        public int SkippedCount => CountResults(result => result.WasSkipped);

        public int FailedCount => CountResults(result => result.Failed);

        public int BlockingFailureCount => CountResults(result => result.BlocksReset);

        public int NonBlockingFailureCount => FailedCount - BlockingFailureCount;

        public int BlockingIssueCount => CountIssues(blocking: true);

        public int NonBlockingIssueCount => CountIssues(blocking: false);

        public CycleResetParticipantResult[] SnapshotParticipantResults()
        {
            if (ParticipantCount == 0)
            {
                return Array.Empty<CycleResetParticipantResult>();
            }

            var snapshot = new CycleResetParticipantResult[ParticipantCount];
            for (int i = 0; i < ParticipantCount; i++)
            {
                snapshot[i] = ParticipantResults[i];
            }

            return snapshot;
        }

        public CycleResetIssue[] SnapshotIssues()
        {
            if (IssueCount == 0)
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

        public bool Equals(CycleResetResult other)
        {
            if (!Request.Equals(other.Request)
                || Status != other.Status
                || !string.Equals(Source, other.Source, StringComparison.Ordinal)
                || !string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                || !string.Equals(Message, other.Message, StringComparison.Ordinal)
                || ParticipantCount != other.ParticipantCount
                || IssueCount != other.IssueCount)
            {
                return false;
            }

            for (int i = 0; i < ParticipantCount; i++)
            {
                if (!ParticipantResults[i].Equals(other.ParticipantResults[i]))
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
            return obj is CycleResetResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Request.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ ParticipantCount;
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
            builder.Append($"status='{Status}' succeeded='{Succeeded}' warnings='{CompletedWithWarnings}' failed='{Failed}' {Request.ToDiagnosticString()} participants='{ParticipantCount}' participantSucceeded='{SucceededCount}' participantSkipped='{SkippedCount}' participantFailed='{FailedCount}' blockingFailures='{BlockingFailureCount}' nonBlockingFailures='{NonBlockingFailureCount}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}'");
            if (!string.IsNullOrWhiteSpace(Message))
            {
                builder.Append($" message='{Message}'");
            }

            return builder.ToString();
        }

        public static CycleResetResult FromResults(
            CycleResetRequest request,
            IReadOnlyList<CycleResetParticipantResult> results,
            IReadOnlyList<CycleResetIssue> issues,
            string source,
            string reason,
            string message)
        {
            string sourceText = Normalize(source);
            string reasonText = Normalize(reason);
            IReadOnlyList<CycleResetParticipantResult> safeResults = results ?? Array.Empty<CycleResetParticipantResult>();
            IReadOnlyList<CycleResetIssue> safeIssues = issues ?? Array.Empty<CycleResetIssue>();

            if (!request.IsValid)
            {
                return new CycleResetResult(
                    request,
                    CycleResetStatus.RejectedInvalidRequest,
                    Array.Empty<CycleResetParticipantResult>(),
                    safeIssues,
                    sourceText,
                    reasonText,
                    "Cycle Reset result rejected because the request is invalid.");
            }

            if (safeResults.Count == 0)
            {
                if (HasBlockingIssues(safeIssues))
                {
                    return new CycleResetResult(
                        request,
                        CycleResetStatus.Failed,
                        safeResults,
                        safeIssues,
                        sourceText,
                        reasonText,
                        "Cycle Reset failed because blocking issues were produced without executable participants.");
                }

                if (HasNonBlockingIssues(safeIssues))
                {
                    return new CycleResetResult(
                        request,
                        CycleResetStatus.CompletedWithWarnings,
                        safeResults,
                        safeIssues,
                        sourceText,
                        reasonText,
                        "Cycle Reset completed with warnings and no executable participants.");
                }

                return new CycleResetResult(
                    request,
                    request.AllowsNoParticipants ? CycleResetStatus.SucceededNoParticipants : CycleResetStatus.Failed,
                    safeResults,
                    safeIssues,
                    sourceText,
                    reasonText,
                    request.AllowsNoParticipants
                        ? "Cycle Reset succeeded with no participants."
                        : "Cycle Reset failed because no participants were available and policy does not allow it.");
            }

            int blockingFailures = 0;
            int nonBlockingFailures = 0;
            for (int i = 0; i < safeResults.Count; i++)
            {
                var result = safeResults[i];
                if (result.BlocksReset)
                {
                    blockingFailures++;
                }
                else if (result.Failed)
                {
                    nonBlockingFailures++;
                }
            }

            var status = CycleResetStatus.Succeeded;
            if (blockingFailures > 0)
            {
                status = CycleResetStatus.Failed;
            }
            else if (nonBlockingFailures > 0 || HasNonBlockingIssues(safeIssues))
            {
                status = CycleResetStatus.CompletedWithWarnings;
            }

            return new CycleResetResult(
                request,
                status,
                safeResults,
                safeIssues,
                sourceText,
                reasonText,
                string.IsNullOrWhiteSpace(message) ? "Cycle Reset executed and aggregated participant results." : message);
        }

        public static CycleResetResult RejectedInvalidRequest(
            CycleResetRequest request,
            IReadOnlyList<CycleResetIssue> issues,
            string source,
            string reason,
            string message)
        {
            return new CycleResetResult(
                request,
                CycleResetStatus.RejectedInvalidRequest,
                Array.Empty<CycleResetParticipantResult>(),
                issues ?? Array.Empty<CycleResetIssue>(),
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Cycle Reset rejected invalid request." : message);
        }

        public static CycleResetResult RejectedInvalidPlan(
            CycleResetRequest request,
            IReadOnlyList<CycleResetIssue> issues,
            string source,
            string reason,
            string message)
        {
            return new CycleResetResult(
                request,
                CycleResetStatus.RejectedInvalidPlan,
                Array.Empty<CycleResetParticipantResult>(),
                issues ?? Array.Empty<CycleResetIssue>(),
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Cycle Reset rejected invalid plan." : message);
        }

        public static bool operator ==(CycleResetResult left, CycleResetResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CycleResetResult left, CycleResetResult right)
        {
            return !left.Equals(right);
        }

        private int CountResults(Func<CycleResetParticipantResult, bool> predicate)
        {
            int count = 0;
            for (int i = 0; i < ParticipantCount; i++)
            {
                if (predicate(ParticipantResults[i]))
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

        private static bool HasBlockingIssues(IReadOnlyList<CycleResetIssue> issues)
        {
            if (issues == null)
            {
                return false;
            }

            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Blocking)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasNonBlockingIssues(IReadOnlyList<CycleResetIssue> issues)
        {
            if (issues == null)
            {
                return false;
            }

            for (int i = 0; i < issues.Count; i++)
            {
                if (!issues[i].Blocking)
                {
                    return true;
                }
            }

            return false;
        }

        private static CycleResetParticipantResult[] CopyResults(IReadOnlyList<CycleResetParticipantResult> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<CycleResetParticipantResult>();
            }

            var copy = new CycleResetParticipantResult[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                var result = source[i];
                if (!result.IsValid)
                {
                    throw new ArgumentException("Cycle Reset aggregate result requires valid participant results.", nameof(source));
                }

                copy[i] = result;
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
