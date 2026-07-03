using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Snapshot
{
    /// <summary>
    /// API status: Experimental. Result of one Snapshot participant restore operation.
    /// Restoring an envelope is local participant behavior and does not load files, PlayerPrefs or progression slots.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21C Snapshot participant restore result; no persistence side effects.")]
    public readonly struct SnapshotParticipantRestoreResult : IEquatable<SnapshotParticipantRestoreResult>
    {
        public SnapshotParticipantRestoreResult(
            SnapshotParticipantDescriptor descriptor,
            SnapshotParticipantResultStatus status,
            SnapshotEnvelope envelope,
            string message)
        {
            if (!descriptor.IsValid)
            {
                throw new ArgumentException("Snapshot restore result requires a valid participant descriptor.", nameof(descriptor));
            }

            ValidateStatus(status, allowCaptured: false);

            if (status == SnapshotParticipantResultStatus.Restored)
            {
                if (!envelope.IsValid)
                {
                    throw new ArgumentException("Restored Snapshot participant result requires a valid envelope.", nameof(envelope));
                }

                if (!descriptor.SupportsEnvelope(envelope))
                {
                    throw new ArgumentException("Restored Snapshot envelope does not match the participant descriptor.", nameof(envelope));
                }
            }
            else if (envelope.IsValid)
            {
                throw new ArgumentException("Only a restored Snapshot participant result may carry an envelope.", nameof(envelope));
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

        public bool Restored => Status == SnapshotParticipantResultStatus.Restored;

        public bool Skipped => Status == SnapshotParticipantResultStatus.Skipped;

        public bool Rejected => Status == SnapshotParticipantResultStatus.Rejected;

        public bool Failed => Status == SnapshotParticipantResultStatus.Failed;

        public bool Completed => Restored || Skipped;

        public bool HasEnvelope => Envelope.IsValid;

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public bool BlocksSnapshot => Descriptor.IsRequired && (Rejected || Failed);

        public bool IsValid => Descriptor.IsValid
            && Status != SnapshotParticipantResultStatus.Unknown
            && Status != SnapshotParticipantResultStatus.Captured
            && (Restored && Envelope.IsValid && Descriptor.SupportsEnvelope(Envelope) || !Restored && !Envelope.IsValid);

        public bool Equals(SnapshotParticipantRestoreResult other)
        {
            return Descriptor.Equals(other.Descriptor)
                && Status == other.Status
                && Envelope.Equals(other.Envelope)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SnapshotParticipantRestoreResult other && Equals(other);
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

        public static SnapshotParticipantRestoreResult RestoredResult(
            SnapshotParticipantDescriptor descriptor,
            SnapshotEnvelope envelope,
            string message)
        {
            return new SnapshotParticipantRestoreResult(descriptor, SnapshotParticipantResultStatus.Restored, envelope, message);
        }

        public static SnapshotParticipantRestoreResult SkippedResult(
            SnapshotParticipantDescriptor descriptor,
            string message)
        {
            return new SnapshotParticipantRestoreResult(descriptor, SnapshotParticipantResultStatus.Skipped, default, message);
        }

        public static SnapshotParticipantRestoreResult RejectedResult(
            SnapshotParticipantDescriptor descriptor,
            string message)
        {
            return new SnapshotParticipantRestoreResult(descriptor, SnapshotParticipantResultStatus.Rejected, default, message);
        }

        public static SnapshotParticipantRestoreResult FailedResult(
            SnapshotParticipantDescriptor descriptor,
            string message)
        {
            return new SnapshotParticipantRestoreResult(descriptor, SnapshotParticipantResultStatus.Failed, default, message);
        }

        public static bool operator ==(SnapshotParticipantRestoreResult left, SnapshotParticipantRestoreResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SnapshotParticipantRestoreResult left, SnapshotParticipantRestoreResult right)
        {
            return !left.Equals(right);
        }

        private static void ValidateStatus(SnapshotParticipantResultStatus status, bool allowCaptured)
        {
            if (!Enum.IsDefined(typeof(SnapshotParticipantResultStatus), status) || status == SnapshotParticipantResultStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Snapshot participant result status must be explicit.");
            }

            if (!allowCaptured && status == SnapshotParticipantResultStatus.Captured)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Snapshot restore result cannot use Captured status.");
            }
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
