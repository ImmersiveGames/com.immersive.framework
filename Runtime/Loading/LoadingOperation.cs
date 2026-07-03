using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Passive Loading operation observation record.
    /// It identifies an operation and its current progress/status; it does not own SceneLifecycle, Transition, UI or readiness mutation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22B Loading operation primitive; no execution, aggregation smoke or visual adapter.")]
    public readonly struct LoadingOperation : IEquatable<LoadingOperation>
    {
        public LoadingOperation(
            LoadingOperationId operationId,
            LoadingOperationStatus status,
            LoadingProgress progress,
            string displayName,
            string source,
            string message)
        {
            if (!operationId.IsValid)
            {
                throw new ArgumentException("Loading operation requires a valid operation id.", nameof(operationId));
            }

            if (!Enum.IsDefined(typeof(LoadingOperationStatus), status) || status == LoadingOperationStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Loading operation status must be explicit.");
            }

            OperationId = operationId;
            Status = status;
            Progress = progress;
            DisplayName = Normalize(displayName);
            Source = Normalize(source);
            Message = Normalize(message);
        }

        public LoadingOperationId OperationId { get; }

        public LoadingOperationStatus Status { get; }

        public LoadingProgress Progress { get; }

        public string DisplayName { get; }

        public string Source { get; }

        public string Message { get; }

        public bool IsValid => OperationId.IsValid && Status != LoadingOperationStatus.Unknown;

        public bool IsTerminal => Status is LoadingOperationStatus.Completed or LoadingOperationStatus.Failed or LoadingOperationStatus.Canceled;

        public bool HasDisplayName => !string.IsNullOrWhiteSpace(DisplayName);

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public bool Equals(LoadingOperation other)
        {
            return OperationId.Equals(other.OperationId)
                && Status == other.Status
                && Progress.Equals(other.Progress)
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingOperation other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = OperationId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ Progress.GetHashCode();
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
            return $"operation='{OperationId.StableText}' status='{Status}' displayName='{displayNameText}' source='{sourceText}' message='{messageText}' progress=({Progress.ToDiagnosticString()})";
        }

        public static LoadingOperation Pending(string operationId, string displayName, string source, string message)
        {
            return new LoadingOperation(LoadingOperationId.From(operationId), LoadingOperationStatus.Pending, LoadingProgress.Zero, displayName, source, message);
        }

        public static LoadingOperation Running(string operationId, float normalizedProgress, string displayName, string source, string message)
        {
            return new LoadingOperation(LoadingOperationId.From(operationId), LoadingOperationStatus.Running, LoadingProgress.FromNormalized(normalizedProgress), displayName, source, message);
        }

        public static LoadingOperation Completed(string operationId, string displayName, string source, string message)
        {
            return new LoadingOperation(LoadingOperationId.From(operationId), LoadingOperationStatus.Completed, LoadingProgress.Complete, displayName, source, message);
        }

        public static LoadingOperation Failed(string operationId, float normalizedProgress, string displayName, string source, string message)
        {
            return new LoadingOperation(LoadingOperationId.From(operationId), LoadingOperationStatus.Failed, LoadingProgress.FromNormalized(normalizedProgress), displayName, source, message);
        }

        public static bool operator ==(LoadingOperation left, LoadingOperation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingOperation left, LoadingOperation right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
