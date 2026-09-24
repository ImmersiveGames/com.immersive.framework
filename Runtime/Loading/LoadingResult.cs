using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Passive Loading result record that combines progress aggregation, readiness observations and issues.
    /// It does not execute lifecycle, retry, fallback, mutate readiness or show UI.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22H Loading result primitive; summary/reporting only.")]
    public readonly struct LoadingResult : IEquatable<LoadingResult>
    {
        private readonly LoadingReadinessObservation[] _readinessObservations;
        private readonly LoadingIssue[] _issues;

        public LoadingResult(
            LoadingOperationId operationId,
            LoadingResultStatus status,
            LoadingProgressAggregationResult progressAggregation,
            IReadOnlyList<LoadingReadinessObservation> readinessObservations,
            IReadOnlyList<LoadingIssue> issues,
            string source,
            string message)
        {
            if (!operationId.IsValid)
            {
                throw new ArgumentException("Loading result requires a valid operation id.", nameof(operationId));
            }

            if (!progressAggregation.OperationId.Equals(operationId))
            {
                throw new ArgumentException("Loading result operation id must match progress aggregation operation id.", nameof(progressAggregation));
            }

            if (!Enum.IsDefined(typeof(LoadingResultStatus), status) || status == LoadingResultStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Loading result status must be explicit.");
            }

            OperationId = operationId;
            Status = status;
            ProgressAggregation = progressAggregation;
            _readinessObservations = CopyReadiness(operationId, readinessObservations);
            _issues = CopyIssues(operationId, issues);
            Source = Normalize(source);
            Message = Normalize(message);
        }

        public LoadingOperationId OperationId { get; }

        public LoadingResultStatus Status { get; }

        public LoadingProgressAggregationResult ProgressAggregation { get; }

        public IReadOnlyList<LoadingReadinessObservation> ReadinessObservations => _readinessObservations ?? Array.Empty<LoadingReadinessObservation>();

        public IReadOnlyList<LoadingIssue> Issues => _issues ?? Array.Empty<LoadingIssue>();

        public string Source { get; }

        public string Message { get; }

        public LoadingProgress Progress => ProgressAggregation.Progress;

        public int ReadinessObservationCount => ReadinessObservations.Count;

        public int IssueCount => Issues.Count;

        public int BlockingIssueCount => CountBlockingIssues();

        public int WarningOrHigherIssueCount => CountWarningOrHigherIssues();

        public int WaitingReadinessCount => CountReadiness(LoadingReadinessStatus.Waiting);

        public int ReadyReadinessCount => CountReadiness(LoadingReadinessStatus.Ready) + CountReadiness(LoadingReadinessStatus.Skipped);

        public int BlockedReadinessCount => CountReadiness(LoadingReadinessStatus.Blocked);

        public int FailedReadinessCount => CountReadiness(LoadingReadinessStatus.Failed);

        public bool HasReadinessObservations => ReadinessObservationCount > 0;

        public bool HasIssues => IssueCount > 0;

        public bool Succeeded => Status is LoadingResultStatus.Succeeded or LoadingResultStatus.SucceededWithWarnings;

        public bool WaitingForReadiness => Status == LoadingResultStatus.WaitingForReadiness;

        public bool Failed => Status is LoadingResultStatus.Failed or LoadingResultStatus.Canceled;

        public bool Completed => Succeeded || Failed;

        public bool BlocksCompletion => WaitingForReadiness
            || ProgressAggregation.BlocksCompletion
            || BlockingIssueCount > 0
            || WaitingReadinessCount > 0
            || BlockedReadinessCount > 0;

        public bool IsValid => OperationId.IsValid && Status != LoadingResultStatus.Unknown;

        public bool Equals(LoadingResult other)
        {
            if (!OperationId.Equals(other.OperationId)
                || Status != other.Status
                || !ProgressAggregation.Equals(other.ProgressAggregation)
                || !string.Equals(Source, other.Source, StringComparison.Ordinal)
                || !string.Equals(Message, other.Message, StringComparison.Ordinal)
                || ReadinessObservationCount != other.ReadinessObservationCount
                || IssueCount != other.IssueCount)
            {
                return false;
            }

            for (int i = 0; i < ReadinessObservationCount; i++)
            {
                if (!ReadinessObservations[i].Equals(other.ReadinessObservations[i]))
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
            return obj is LoadingResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = OperationId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ ProgressAggregation.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                for (int i = 0; i < ReadinessObservationCount; i++)
                {
                    hashCode = hashCode * 397 ^ ReadinessObservations[i].GetHashCode();
                }

                for (int i = 0; i < IssueCount; i++)
                {
                    hashCode = hashCode * 397 ^ Issues[i].GetHashCode();
                }

                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string sourceText = Source.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            var builder = new StringBuilder();
            builder.Append($"operation='{OperationId.StableText}' status='{Status}' completed='{Completed}' failed='{Failed}' blocksCompletion='{BlocksCompletion}' readiness='{ReadinessObservationCount}' issues='{IssueCount}' blockingIssues='{BlockingIssueCount}' source='{sourceText}' message='{messageText}' progress=({ProgressAggregation.ToDiagnosticString()})");
            return builder.ToString();
        }

        public static LoadingResult SucceededResult(
            LoadingOperationId operationId,
            LoadingProgressAggregationResult progressAggregation,
            IReadOnlyList<LoadingReadinessObservation> readinessObservations,
            string source,
            string message)
        {
            return new LoadingResult(operationId, LoadingResultStatus.Succeeded, progressAggregation, readinessObservations, Array.Empty<LoadingIssue>(), source, message);
        }

        public static LoadingResult SucceededWithWarnings(
            LoadingOperationId operationId,
            LoadingProgressAggregationResult progressAggregation,
            IReadOnlyList<LoadingReadinessObservation> readinessObservations,
            IReadOnlyList<LoadingIssue> issues,
            string source,
            string message)
        {
            return new LoadingResult(operationId, LoadingResultStatus.SucceededWithWarnings, progressAggregation, readinessObservations, issues, source, message);
        }

        public static LoadingResult Waiting(
            LoadingOperationId operationId,
            LoadingProgressAggregationResult progressAggregation,
            IReadOnlyList<LoadingReadinessObservation> readinessObservations,
            IReadOnlyList<LoadingIssue> issues,
            string source,
            string message)
        {
            return new LoadingResult(operationId, LoadingResultStatus.WaitingForReadiness, progressAggregation, readinessObservations, issues, source, message);
        }

        public static LoadingResult FailedResult(
            LoadingOperationId operationId,
            LoadingProgressAggregationResult progressAggregation,
            IReadOnlyList<LoadingReadinessObservation> readinessObservations,
            IReadOnlyList<LoadingIssue> issues,
            string source,
            string message)
        {
            return new LoadingResult(operationId, LoadingResultStatus.Failed, progressAggregation, readinessObservations, issues, source, message);
        }

        public static bool operator ==(LoadingResult left, LoadingResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingResult left, LoadingResult right)
        {
            return !left.Equals(right);
        }

        private static LoadingReadinessObservation[] CopyReadiness(LoadingOperationId operationId, IReadOnlyList<LoadingReadinessObservation> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<LoadingReadinessObservation>();
            }

            var copy = new LoadingReadinessObservation[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                var observation = source[i];
                if (!observation.IsValid)
                {
                    throw new ArgumentException("Loading result cannot contain invalid readiness observations.", nameof(source));
                }

                if (!observation.OperationId.Equals(operationId))
                {
                    throw new ArgumentException("Loading result readiness observations must match the result operation id.", nameof(source));
                }

                copy[i] = observation;
            }

            return copy;
        }

        private static LoadingIssue[] CopyIssues(LoadingOperationId operationId, IReadOnlyList<LoadingIssue> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<LoadingIssue>();
            }

            var copy = new LoadingIssue[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                var issue = source[i];
                if (!issue.IsValid)
                {
                    throw new ArgumentException("Loading result cannot contain invalid issues.", nameof(source));
                }

                if (!issue.OperationId.Equals(operationId))
                {
                    throw new ArgumentException("Loading result issues must match the result operation id.", nameof(source));
                }

                copy[i] = issue;
            }

            return copy;
        }

        private int CountReadiness(LoadingReadinessStatus status)
        {
            int count = 0;
            for (int i = 0; i < ReadinessObservations.Count; i++)
            {
                if (ReadinessObservations[i].Status == status)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountBlockingIssues()
        {
            int count = 0;
            for (int i = 0; i < Issues.Count; i++)
            {
                if (Issues[i].BlocksCompletion)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountWarningOrHigherIssues()
        {
            int count = 0;
            for (int i = 0; i < Issues.Count; i++)
            {
                if (Issues[i].IsWarningOrHigher)
                {
                    count++;
                }
            }

            return count;
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
