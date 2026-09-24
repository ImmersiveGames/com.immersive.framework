using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Snapshot
{
    /// <summary>
    /// API status: Experimental. Result of one Snapshot participant capture operation.
    /// Capturing an envelope does not persist it and does not select a backend or progression slot.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21C Snapshot participant capture result; no persistence side effects.")]
    public readonly struct SnapshotParticipantCaptureResult : IEquatable<SnapshotParticipantCaptureResult>
    {
        public SnapshotParticipantCaptureResult(
            SnapshotParticipantDescriptor descriptor,
            SnapshotParticipantResultStatus status,
            SnapshotEnvelope envelope,
            string message)
        {
            if (!descriptor.IsValid)
            {
                throw new ArgumentException("Snapshot capture result requires a valid participant descriptor.", nameof(descriptor));
            }

            ValidateStatus(status, allowRestored: false);

            if (status == SnapshotParticipantResultStatus.Captured)
            {
                if (!envelope.IsValid)
                {
                    throw new ArgumentException("Captured Snapshot participant result requires a valid envelope.", nameof(envelope));
                }

                if (!descriptor.SupportsEnvelope(envelope))
                {
                    throw new ArgumentException("Captured Snapshot envelope does not match the participant descriptor.", nameof(envelope));
                }
            }
            else if (envelope.IsValid)
            {
                throw new ArgumentException("Only a captured Snapshot participant result may carry an envelope.", nameof(envelope));
            }

            Descriptor = descriptor;
            Status = status;
            Envelope = envelope;
            Message = Normalize(message);
        }

        public SnapshotParticipantDescriptor Descriptor { get; }

        public SnapshotParticipantResultStatus Status { get; }

        public SnapshotEnvelope Envelope { get; }

        public string Message { get; }

        public bool Captured => Status == SnapshotParticipantResultStatus.Captured;

        public bool Skipped => Status == SnapshotParticipantResultStatus.Skipped;

        public bool Rejected => Status == SnapshotParticipantResultStatus.Rejected;

        public bool Failed => Status == SnapshotParticipantResultStatus.Failed;

        public bool Completed => Captured || Skipped;

        public bool HasEnvelope => Envelope.IsValid;

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public bool BlocksSnapshot => Descriptor.IsRequired && (Rejected || Failed);

        public bool IsValid => Descriptor.IsValid
            && Status != SnapshotParticipantResultStatus.Unknown
            && Status != SnapshotParticipantResultStatus.Restored
            && (Captured && Envelope.IsValid && Descriptor.SupportsEnvelope(Envelope) || !Captured && !Envelope.IsValid);

        public bool Equals(SnapshotParticipantCaptureResult other)
        {
            return Descriptor.Equals(other.Descriptor)
                && Status == other.Status
                && Envelope.Equals(other.Envelope)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SnapshotParticipantCaptureResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Descriptor.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ Envelope.GetHashCode();
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
            string messageText = HasMessage ? Message : "<none>";
            string envelopeText = HasEnvelope ? Envelope.ToDiagnosticString() : "<none>";
            return $"status='{Status}' blocksSnapshot='{BlocksSnapshot}' message='{messageText}' descriptor=({Descriptor.ToDiagnosticString()}) envelope=({envelopeText})";
        }

        public static SnapshotParticipantCaptureResult CapturedResult(
            SnapshotParticipantDescriptor descriptor,
            SnapshotEnvelope envelope,
            string message)
        {
            return new SnapshotParticipantCaptureResult(descriptor, SnapshotParticipantResultStatus.Captured, envelope, message);
        }

        public static SnapshotParticipantCaptureResult SkippedResult(
            SnapshotParticipantDescriptor descriptor,
            string message)
        {
            return new SnapshotParticipantCaptureResult(descriptor, SnapshotParticipantResultStatus.Skipped, default, message);
        }

        public static SnapshotParticipantCaptureResult RejectedResult(
            SnapshotParticipantDescriptor descriptor,
            string message)
        {
            return new SnapshotParticipantCaptureResult(descriptor, SnapshotParticipantResultStatus.Rejected, default, message);
        }

        public static SnapshotParticipantCaptureResult FailedResult(
            SnapshotParticipantDescriptor descriptor,
            string message)
        {
            return new SnapshotParticipantCaptureResult(descriptor, SnapshotParticipantResultStatus.Failed, default, message);
        }

        public static bool operator ==(SnapshotParticipantCaptureResult left, SnapshotParticipantCaptureResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SnapshotParticipantCaptureResult left, SnapshotParticipantCaptureResult right)
        {
            return !left.Equals(right);
        }

        private static void ValidateStatus(SnapshotParticipantResultStatus status, bool allowRestored)
        {
            if (!Enum.IsDefined(typeof(SnapshotParticipantResultStatus), status) || status == SnapshotParticipantResultStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Snapshot participant result status must be explicit.");
            }

            if (!allowRestored && status == SnapshotParticipantResultStatus.Restored)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Snapshot capture result cannot use Restored status.");
            }
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
