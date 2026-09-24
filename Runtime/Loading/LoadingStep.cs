using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Passive Loading step observation record.
    /// It reports a unit of work and progress; it does not load scenes, run transitions or display UI.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22B Loading step primitive; no execution or aggregation runtime.")]
    public readonly struct LoadingStep : IEquatable<LoadingStep>
    {
        public LoadingStep(
            LoadingStepId stepId,
            LoadingStepStatus status,
            LoadingWeightedProgress weightedProgress,
            string displayName,
            string source,
            string message)
        {
            if (!stepId.IsValid)
            {
                throw new ArgumentException("Loading step requires a valid step id.", nameof(stepId));
            }

            if (!Enum.IsDefined(typeof(LoadingStepStatus), status) || status == LoadingStepStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Loading step status must be explicit.");
            }

            if (!weightedProgress.IsValid)
            {
                throw new ArgumentException("Loading step requires valid weighted progress.", nameof(weightedProgress));
            }

            StepId = stepId;
            Status = status;
            WeightedProgress = weightedProgress;
            DisplayName = Normalize(displayName);
            Source = Normalize(source);
            Message = Normalize(message);
        }

        public LoadingStepId StepId { get; }

        public LoadingStepStatus Status { get; }

        public LoadingWeightedProgress WeightedProgress { get; }

        public string DisplayName { get; }

        public string Source { get; }

        public string Message { get; }

        public LoadingStepWeight Weight => WeightedProgress.Weight;

        public LoadingProgress Progress => WeightedProgress.Progress;

        public bool IsValid => StepId.IsValid && Status != LoadingStepStatus.Unknown && WeightedProgress.IsValid;

        public bool IsTerminal => Status is LoadingStepStatus.Completed or LoadingStepStatus.Failed or LoadingStepStatus.Skipped or LoadingStepStatus.Canceled;

        public bool BlocksCompletion => Status is LoadingStepStatus.Pending or LoadingStepStatus.Running;

        public bool HasDisplayName => !string.IsNullOrWhiteSpace(DisplayName);

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public bool Equals(LoadingStep other)
        {
            return StepId.Equals(other.StepId)
                && Status == other.Status
                && WeightedProgress.Equals(other.WeightedProgress)
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingStep other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StepId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ WeightedProgress.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(DisplayName ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
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
            string displayNameText = HasDisplayName ? DisplayName : "<none>";
            string sourceText = HasSource ? Source : "<none>";
            string messageText = HasMessage ? Message : "<none>";
            return $"step='{StepId.StableText}' status='{Status}' displayName='{displayNameText}' source='{sourceText}' message='{messageText}' progress=({WeightedProgress.ToDiagnosticString()})";
        }

        public static LoadingStep Pending(string stepId, float weight, string displayName, string source, string message)
        {
            return new LoadingStep(LoadingStepId.From(stepId), LoadingStepStatus.Pending, LoadingWeightedProgress.From(weight, 0f), displayName, source, message);
        }

        public static LoadingStep Running(string stepId, float weight, float normalizedProgress, string displayName, string source, string message)
        {
            return new LoadingStep(LoadingStepId.From(stepId), LoadingStepStatus.Running, LoadingWeightedProgress.From(weight, normalizedProgress), displayName, source, message);
        }

        public static LoadingStep Completed(string stepId, float weight, string displayName, string source, string message)
        {
            return new LoadingStep(LoadingStepId.From(stepId), LoadingStepStatus.Completed, LoadingWeightedProgress.From(weight, 1f), displayName, source, message);
        }

        public static LoadingStep Failed(string stepId, float weight, float normalizedProgress, string displayName, string source, string message)
        {
            return new LoadingStep(LoadingStepId.From(stepId), LoadingStepStatus.Failed, LoadingWeightedProgress.From(weight, normalizedProgress), displayName, source, message);
        }

        public static bool operator ==(LoadingStep left, LoadingStep right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingStep left, LoadingStep right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
