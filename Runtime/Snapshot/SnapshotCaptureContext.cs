using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.Common;

namespace Immersive.Framework.Snapshot
{
    /// <summary>
    /// API status: Experimental. Passive context passed to a Snapshot participant during capture.
    /// It identifies the runtime owner being captured and carries diagnostics only. It does not persist data.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21C Snapshot capture context; no backend or request runtime.")]
    public readonly struct SnapshotCaptureContext : IEquatable<SnapshotCaptureContext>
    {
        public SnapshotCaptureContext(
            SnapshotScope scope,
            FrameworkIdentityKey ownerIdentity,
            long capturedUtcTicks,
            string source,
            string reason)
        {
            ValidateScope(scope);
            ValidateOwner(scope, ownerIdentity);

            if (capturedUtcTicks <= 0 || capturedUtcTicks > DateTime.MaxValue.Ticks)
            {
                throw new ArgumentOutOfRangeException(nameof(capturedUtcTicks), capturedUtcTicks, "Snapshot capture UTC ticks must be a valid positive DateTime tick value.");
            }

            Scope = scope;
            OwnerIdentity = ownerIdentity;
            CapturedUtcTicks = capturedUtcTicks;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SnapshotScope Scope { get; }

        public FrameworkIdentityKey OwnerIdentity { get; }

        public long CapturedUtcTicks { get; }

        public string Source { get; }

        public string Reason { get; }

        public DateTime CapturedUtc => new DateTime(CapturedUtcTicks, DateTimeKind.Utc);

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

        public bool IsValid => Scope != SnapshotScope.Unknown
            && OwnerIdentity.IsValid
            && OwnerIdentity.Domain == SnapshotEnvelope.GetExpectedOwnerDomain(Scope)
            && CapturedUtcTicks > 0
            && CapturedUtcTicks <= DateTime.MaxValue.Ticks;

        public bool Matches(SnapshotParticipantDescriptor descriptor)
        {
            return descriptor.IsValid && descriptor.SupportsCaptureContext(this);
        }

        public bool Equals(SnapshotCaptureContext other)
        {
            return Scope == other.Scope
                && OwnerIdentity.Equals(other.OwnerIdentity)
                && CapturedUtcTicks == other.CapturedUtcTicks
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SnapshotCaptureContext other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Scope;
                hashCode = hashCode * 397 ^ OwnerIdentity.GetHashCode();
                hashCode = hashCode * 397 ^ CapturedUtcTicks.GetHashCode();
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
            return $"scope='{Scope}' owner='{OwnerIdentity.StableText}' capturedUtcTicks='{CapturedUtcTicks}' source='{sourceText}' reason='{reasonText}'";
        }

        public static SnapshotCaptureContext Create(
            SnapshotScope scope,
            FrameworkIdentityKey ownerIdentity,
            string source,
            string reason)
        {
            return new SnapshotCaptureContext(scope, ownerIdentity, DateTime.UtcNow.Ticks, source, reason);
        }

        public static bool operator ==(SnapshotCaptureContext left, SnapshotCaptureContext right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SnapshotCaptureContext left, SnapshotCaptureContext right)
        {
            return !left.Equals(right);
        }

        private static void ValidateScope(SnapshotScope scope)
        {
            if (!Enum.IsDefined(typeof(SnapshotScope), scope) || scope == SnapshotScope.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Snapshot capture scope must be explicit.");
            }
        }

        private static void ValidateOwner(SnapshotScope scope, FrameworkIdentityKey ownerIdentity)
        {
            if (!ownerIdentity.IsValid)
            {
                throw new ArgumentException("Snapshot capture owner identity must be valid.", nameof(ownerIdentity));
            }

            var expectedDomain = SnapshotEnvelope.GetExpectedOwnerDomain(scope);
            if (ownerIdentity.Domain != expectedDomain)
            {
                throw new ArgumentException(
                    $"Snapshot capture owner for scope '{scope}' must use identity domain '{expectedDomain}', but received '{ownerIdentity.Domain}'.",
                    nameof(ownerIdentity));
            }
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
