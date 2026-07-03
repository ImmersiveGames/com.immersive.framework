using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Passive aggregate result for Loading progress observations.
    /// It combines LoadingStep records into one weighted progress value; it does not load scenes, run transitions, show UI or mutate readiness.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22C Loading progress aggregation result; no execution owner or visual adapter.")]
    public readonly struct LoadingProgressAggregationResult : IEquatable<LoadingProgressAggregationResult>
    {
        private readonly LoadingStep[] _steps;

        public LoadingProgressAggregationResult(
            LoadingOperationId operationId,
            IReadOnlyList<LoadingStep> steps,
            LoadingProgress progress,
            LoadingProgressAggregationStatus status,
            string source,
            string reason,
            string message)
        {
            if (!operationId.IsValid)
            {
                throw new ArgumentException("Loading progress aggregation requires a valid operation id.", nameof(operationId));
            }

            if (!Enum.IsDefined(typeof(LoadingProgressAggregationStatus), status)
                || status == LoadingProgressAggregationStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Loading progress aggregation status must be explicit.");
            }

            OperationId = operationId;
            Progress = progress;
            Status = status;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);

            _steps = FrameworkCollectionCopy.ToArrayOrEmpty(steps);
            for (int i = 0; i < _steps.Length; i++)
            {
                var step = _steps[i];
                if (!step.IsValid)
                {
                    throw new ArgumentException("Loading progress aggregation cannot contain invalid steps.", nameof(steps));
                }
            }
        }

        public LoadingOperationId OperationId { get; }

        public IReadOnlyList<LoadingStep> Steps => FrameworkCollectionCopy.IsNullOrEmpty(_steps) ? Array.Empty<LoadingStep>() : _steps;

        public LoadingProgress Progress { get; }

        public LoadingProgressAggregationStatus Status { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public int StepCount => _steps?.Length ?? 0;

        public bool HasSteps => StepCount > 0;

        public bool IsRunning => Status == LoadingProgressAggregationStatus.Running;

        public bool Completed => Status is LoadingProgressAggregationStatus.Completed or LoadingProgressAggregationStatus.CompletedWithSkippedSteps or LoadingProgressAggregationStatus.NoSteps;

        public bool Failed => Status is LoadingProgressAggregationStatus.Failed or LoadingProgressAggregationStatus.Canceled;

        public bool IsTerminal => Completed || Failed;

        public bool HasSkippedSteps => SkippedCount > 0;

        public bool BlocksCompletion => IsRunning;

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public float WeightedCompleted => SumWeightedCompleted();

        public float WeightedTotal => SumWeightedTotal();

        public int PendingCount => CountByStatus(LoadingStepStatus.Pending);

        public int RunningCount => CountByStatus(LoadingStepStatus.Running);

        public int CompletedCount => CountByStatus(LoadingStepStatus.Completed);

        public int SkippedCount => CountByStatus(LoadingStepStatus.Skipped);

        public int FailedCount => CountByStatus(LoadingStepStatus.Failed);

        public int CanceledCount => CountByStatus(LoadingStepStatus.Canceled);

        public bool Equals(LoadingProgressAggregationResult other)
        {
            if (!OperationId.Equals(other.OperationId)
                || !Progress.Equals(other.Progress)
                || Status != other.Status
                || !string.Equals(Source, other.Source, StringComparison.Ordinal)
                || !string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                || !string.Equals(Message, other.Message, StringComparison.Ordinal)
                || StepCount != other.StepCount)
            {
                return false;
            }

            for (int i = 0; i < StepCount; i++)
            {
                if (!Steps[i].Equals(other.Steps[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingProgressAggregationResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = OperationId.GetHashCode();
                hashCode = hashCode * 397 ^ Progress.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);

                for (int i = 0; i < StepCount; i++)
                {
                    hashCode = hashCode * 397 ^ Steps[i].GetHashCode();
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
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            var builder = new StringBuilder();
            builder.Append($"Loading Progress Aggregation operation='{OperationId.StableText}' status='{Status}' progress='{Progress.NormalizedValue:0.###}' percent='{Progress.PercentRounded}' steps='{StepCount}' pending='{PendingCount}' running='{RunningCount}' completed='{CompletedCount}' skipped='{SkippedCount}' failed='{FailedCount}' canceled='{CanceledCount}' weightedCompleted='{WeightedCompleted:0.###}' weightedTotal='{WeightedTotal:0.###}' source='{sourceText}' reason='{reasonText}' message='{messageText}' details=[");
            for (int i = 0; i < Steps.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(Steps[i].ToDiagnosticString());
            }

            builder.Append("]");
            return builder.ToString();
        }

        public static LoadingProgressAggregationResult FromSteps(
            LoadingOperationId operationId,
            IReadOnlyList<LoadingStep> steps,
            string source,
            string reason,
            string message)
        {
            var progress = CalculateProgress(steps);
            var status = DetermineStatus(steps);
            return new LoadingProgressAggregationResult(
                operationId,
                steps,
                progress,
                status,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? BuildDefaultMessage(status) : message);
        }

        private static LoadingProgress CalculateProgress(IReadOnlyList<LoadingStep> candidateSteps)
        {
            if (candidateSteps == null || candidateSteps.Count == 0)
            {
                return LoadingProgress.Zero;
            }

            float completed = 0f;
            float total = 0f;
            for (int i = 0; i < candidateSteps.Count; i++)
            {
                var step = candidateSteps[i];
                completed += step.WeightedProgress.WeightedCompleted;
                total += step.WeightedProgress.WeightedTotal;
            }

            if (total <= 0f)
            {
                return LoadingProgress.Zero;
            }

            float normalized = completed / total;
            if (normalized < 0f)
            {
                normalized = 0f;
            }
            else if (normalized > 1f)
            {
                normalized = 1f;
            }

            return LoadingProgress.FromNormalized(normalized);
        }

        private static LoadingProgressAggregationStatus DetermineStatus(IReadOnlyList<LoadingStep> candidateSteps)
        {
            if (candidateSteps == null || candidateSteps.Count == 0)
            {
                return LoadingProgressAggregationStatus.NoSteps;
            }

            bool hasPendingOrRunning = false;
            bool hasSkipped = false;
            bool hasFailed = false;
            bool hasCanceled = false;

            for (int i = 0; i < candidateSteps.Count; i++)
            {
                var status = candidateSteps[i].Status;
                if (status == LoadingStepStatus.Failed)
                {
                    hasFailed = true;
                }
                else if (status == LoadingStepStatus.Canceled)
                {
                    hasCanceled = true;
                }
                else if (status is LoadingStepStatus.Pending or LoadingStepStatus.Running)
                {
                    hasPendingOrRunning = true;
                }
                else if (status == LoadingStepStatus.Skipped)
                {
                    hasSkipped = true;
                }
            }

            if (hasFailed)
            {
                return LoadingProgressAggregationStatus.Failed;
            }

            if (hasCanceled)
            {
                return LoadingProgressAggregationStatus.Canceled;
            }

            if (hasPendingOrRunning)
            {
                return LoadingProgressAggregationStatus.Running;
            }

            return hasSkipped
                ? LoadingProgressAggregationStatus.CompletedWithSkippedSteps
                : LoadingProgressAggregationStatus.Completed;
        }

        private static string BuildDefaultMessage(LoadingProgressAggregationStatus status)
        {
            switch (status)
            {
                case LoadingProgressAggregationStatus.NoSteps:
                    return "Loading progress aggregation completed with no steps.";
                case LoadingProgressAggregationStatus.Running:
                    return "Loading progress aggregation is running.";
                case LoadingProgressAggregationStatus.Completed:
                    return "Loading progress aggregation completed.";
                case LoadingProgressAggregationStatus.CompletedWithSkippedSteps:
                    return "Loading progress aggregation completed with skipped steps.";
                case LoadingProgressAggregationStatus.Failed:
                    return "Loading progress aggregation failed.";
                case LoadingProgressAggregationStatus.Canceled:
                    return "Loading progress aggregation was canceled.";
                default:
                    return "Loading progress aggregation completed.";
            }
        }

        private int CountByStatus(LoadingStepStatus status)
        {
            if (FrameworkCollectionCopy.IsNullOrEmpty(_steps))
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < _steps.Length; i++)
            {
                if (_steps[i].Status == status)
                {
                    count++;
                }
            }

            return count;
        }

        private float SumWeightedCompleted()
        {
            if (FrameworkCollectionCopy.IsNullOrEmpty(_steps))
            {
                return 0f;
            }

            float sum = 0f;
            for (int i = 0; i < _steps.Length; i++)
            {
                sum += _steps[i].WeightedProgress.WeightedCompleted;
            }

            return sum;
        }

        private float SumWeightedTotal()
        {
            if (FrameworkCollectionCopy.IsNullOrEmpty(_steps))
            {
                return 0f;
            }

            float sum = 0f;
            for (int i = 0; i < _steps.Length; i++)
            {
                sum += _steps[i].WeightedProgress.WeightedTotal;
            }

            return sum;
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
