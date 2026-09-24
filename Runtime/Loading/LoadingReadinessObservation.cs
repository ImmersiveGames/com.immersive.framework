using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Passive Loading readiness observation record.
    /// It describes readiness state for an operation; it does not wait, schedule, mutate readiness or execute lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22G Loading readiness observation primitive; no lifecycle execution or readiness mutation.")]
    public readonly struct LoadingReadinessObservation : IEquatable<LoadingReadinessObservation>
    {
        public LoadingReadinessObservation(
            LoadingReadinessObservationId observationId,
            LoadingOperationId operationId,
            LoadingReadinessStatus status,
            string displayName,
            string source,
            string reason,
            string message)
        {
            if (!observationId.IsValid)
            {
                throw new ArgumentException("Loading readiness observation requires a valid observation id.", nameof(observationId));
            }

            if (!operationId.IsValid)
            {
                throw new ArgumentException("Loading readiness observation requires a valid operation id.", nameof(operationId));
            }

            if (!Enum.IsDefined(typeof(LoadingReadinessStatus), status) || status == LoadingReadinessStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Loading readiness status must be explicit.");
            }

            ObservationId = observationId;
            OperationId = operationId;
            Status = status;
            DisplayName = Normalize(displayName);
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public LoadingReadinessObservationId ObservationId { get; }

        public LoadingOperationId OperationId { get; }

        public LoadingReadinessStatus Status { get; }

        public string DisplayName { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool IsValid => ObservationId.IsValid && OperationId.IsValid && Status != LoadingReadinessStatus.Unknown;

        public bool IsReady => Status is LoadingReadinessStatus.Ready or LoadingReadinessStatus.Skipped;

        public bool IsWaiting => Status == LoadingReadinessStatus.Waiting;

        public bool IsBlocked => Status == LoadingReadinessStatus.Blocked;

        public bool Failed => Status == LoadingReadinessStatus.Failed;

        public bool IsTerminal => IsReady || IsBlocked || Failed || Status == LoadingReadinessStatus.NotObserved;

        public bool BlocksCompletion => Status is LoadingReadinessStatus.Waiting or LoadingReadinessStatus.Blocked;

        public bool HasDisplayName => !string.IsNullOrWhiteSpace(DisplayName);

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public bool Equals(LoadingReadinessObservation other)
        {
            return ObservationId.Equals(other.ObservationId)
                && OperationId.Equals(other.OperationId)
                && Status == other.Status
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingReadinessObservation other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ObservationId.GetHashCode();
                hashCode = hashCode * 397 ^ OperationId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(DisplayName ?? string.Empty);
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
            string displayNameText = HasDisplayName ? DisplayName : "<none>";
            string sourceText = HasSource ? Source : "<none>";
            string reasonText = HasReason ? Reason : "<none>";
            string messageText = HasMessage ? Message : "<none>";
            return $"readiness='{ObservationId.StableText}' operation='{OperationId.StableText}' status='{Status}' ready='{IsReady}' blocksCompletion='{BlocksCompletion}' displayName='{displayNameText}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static LoadingReadinessObservation Waiting(
            string observationId,
            LoadingOperationId operationId,
            string displayName,
            string source,
            string reason,
            string message)
        {
            return new LoadingReadinessObservation(
                LoadingReadinessObservationId.From(observationId),
                operationId,
                LoadingReadinessStatus.Waiting,
                displayName,
                source,
                reason,
                message);
        }

        public static LoadingReadinessObservation Ready(
            string observationId,
            LoadingOperationId operationId,
            string displayName,
            string source,
            string reason,
            string message)
        {
            return new LoadingReadinessObservation(
                LoadingReadinessObservationId.From(observationId),
                operationId,
                LoadingReadinessStatus.Ready,
                displayName,
                source,
                reason,
                message);
        }

        public static LoadingReadinessObservation Blocked(
            string observationId,
            LoadingOperationId operationId,
            string displayName,
            string source,
            string reason,
            string message)
        {
            return new LoadingReadinessObservation(
                LoadingReadinessObservationId.From(observationId),
                operationId,
                LoadingReadinessStatus.Blocked,
                displayName,
                source,
                reason,
                message);
        }

        public static LoadingReadinessObservation FailedObservation(
            string observationId,
            LoadingOperationId operationId,
            string displayName,
            string source,
            string reason,
            string message)
        {
            return new LoadingReadinessObservation(
                LoadingReadinessObservationId.From(observationId),
                operationId,
                LoadingReadinessStatus.Failed,
                displayName,
                source,
                reason,
                message);
        }

        public static LoadingReadinessObservation Skipped(
            string observationId,
            LoadingOperationId operationId,
            string displayName,
            string source,
            string reason,
            string message)
        {
            return new LoadingReadinessObservation(
                LoadingReadinessObservationId.From(observationId),
                operationId,
                LoadingReadinessStatus.Skipped,
                displayName,
                source,
                reason,
                message);
        }

        public static bool operator ==(LoadingReadinessObservation left, LoadingReadinessObservation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingReadinessObservation left, LoadingReadinessObservation right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
