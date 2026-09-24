using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Snapshot
{
    /// <summary>
    /// API status: Experimental. Passive context passed to a Snapshot participant during restore.
    /// It carries an envelope produced elsewhere and diagnostics only. It does not load data from storage.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21C Snapshot restore context; no backend or request runtime.")]
    public readonly struct SnapshotRestoreContext : IEquatable<SnapshotRestoreContext>
    {
        public SnapshotRestoreContext(SnapshotEnvelope envelope, string source, string reason)
        {
            if (!envelope.IsValid)
            {
                throw new ArgumentException("Snapshot restore context requires a valid envelope.", nameof(envelope));
            }

            Envelope = envelope;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SnapshotEnvelope Envelope { get; }

        public SnapshotScope Scope => Envelope.Scope;

        public string Source { get; }

        public string Reason { get; }

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

        public bool IsValid => Envelope.IsValid;

        public bool Matches(SnapshotParticipantDescriptor descriptor)
        {
            return descriptor.IsValid && descriptor.SupportsEnvelope(Envelope);
        }

        public bool Equals(SnapshotRestoreContext other)
        {
            return Envelope.Equals(other.Envelope)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SnapshotRestoreContext other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Envelope.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string sourceText = HasSource ? Source : "<none>";
            string reasonText = HasReason ? Reason : "<none>";
            return $"source='{sourceText}' reason='{reasonText}' envelope=({Envelope.ToDiagnosticString()})";
        }

        public static SnapshotRestoreContext FromEnvelope(SnapshotEnvelope envelope, string source, string reason)
        {
            return new SnapshotRestoreContext(envelope, source, reason);
        }

        public static bool operator ==(SnapshotRestoreContext left, SnapshotRestoreContext right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SnapshotRestoreContext left, SnapshotRestoreContext right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
