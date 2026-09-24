using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.Common;

namespace Immersive.Framework.Snapshot
{
    /// <summary>
    /// API status: Experimental. Immutable backend-agnostic envelope for one captured Snapshot payload.
    /// This does not save, load, serialize to JSON, choose a progression slot or restore gameplay by itself.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21B Snapshot envelope primitive; no backend or participant execution.")]
    public readonly struct SnapshotEnvelope : IEquatable<SnapshotEnvelope>
    {
        public SnapshotEnvelope(
            SnapshotEnvelopeId envelopeId,
            SnapshotScope scope,
            FrameworkIdentityKey ownerIdentity,
            SnapshotSchemaId schemaId,
            SnapshotSchemaVersion schemaVersion,
            SnapshotPayload payload,
            long capturedUtcTicks,
            string source,
            string reason)
        {
            if (!envelopeId.IsValid)
            {
                throw new ArgumentException("Snapshot envelope requires a valid envelope id.", nameof(envelopeId));
            }

            ValidateScope(scope);
            ValidateOwner(scope, ownerIdentity);

            if (!schemaId.IsValid)
            {
                throw new ArgumentException("Snapshot envelope requires a valid schema id.", nameof(schemaId));
            }

            if (!schemaVersion.IsValid)
            {
                throw new ArgumentException("Snapshot envelope requires a valid schema version.", nameof(schemaVersion));
            }

            if (!payload.IsValid)
            {
                throw new ArgumentException("Snapshot envelope requires a valid payload. Use SnapshotPayload.Empty for explicit empty snapshots.", nameof(payload));
            }

            if (capturedUtcTicks <= 0 || capturedUtcTicks > DateTime.MaxValue.Ticks)
            {
                throw new ArgumentOutOfRangeException(nameof(capturedUtcTicks), capturedUtcTicks, "Snapshot captured UTC ticks must be a valid positive DateTime tick value.");
            }

            EnvelopeId = envelopeId;
            Scope = scope;
            OwnerIdentity = ownerIdentity;
            SchemaId = schemaId;
            SchemaVersion = schemaVersion;
            Payload = payload;
            CapturedUtcTicks = capturedUtcTicks;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SnapshotEnvelopeId EnvelopeId { get; }

        public SnapshotScope Scope { get; }

        public FrameworkIdentityKey OwnerIdentity { get; }

        public SnapshotSchemaId SchemaId { get; }

        public SnapshotSchemaVersion SchemaVersion { get; }

        public SnapshotPayload Payload { get; }

        public long CapturedUtcTicks { get; }

        public string Source { get; }

        public string Reason { get; }

        public DateTime CapturedUtc => new DateTime(CapturedUtcTicks, DateTimeKind.Utc);

        public bool HasPayloadBytes => Payload.HasBytes;

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

        public bool IsValid => EnvelopeId.IsValid
            && Scope != SnapshotScope.Unknown
            && OwnerIdentity.IsValid
            && OwnerIdentity.Domain == GetExpectedOwnerDomain(Scope)
            && SchemaId.IsValid
            && SchemaVersion.IsValid
            && Payload.IsValid
            && CapturedUtcTicks > 0
            && CapturedUtcTicks <= DateTime.MaxValue.Ticks;

        public string StableText => $"{EnvelopeId.StableText}@{Scope}:{OwnerIdentity.StableText}:{SchemaId.Value.Value}:{SchemaVersion.StableText}";

        public bool MatchesOwner(SnapshotScope scope, FrameworkIdentityKey ownerIdentity)
        {
            return Scope == scope && OwnerIdentity.Equals(ownerIdentity);
        }

        public bool MatchesSchema(SnapshotSchemaId schemaId, SnapshotSchemaVersion schemaVersion)
        {
            return SchemaId == schemaId && SchemaVersion == schemaVersion;
        }

        public bool Equals(SnapshotEnvelope other)
        {
            return EnvelopeId.Equals(other.EnvelopeId)
                && Scope == other.Scope
                && OwnerIdentity.Equals(other.OwnerIdentity)
                && SchemaId.Equals(other.SchemaId)
                && SchemaVersion.Equals(other.SchemaVersion)
                && Payload.Equals(other.Payload)
                && CapturedUtcTicks == other.CapturedUtcTicks
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SnapshotEnvelope other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = EnvelopeId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Scope;
                hashCode = hashCode * 397 ^ OwnerIdentity.GetHashCode();
                hashCode = hashCode * 397 ^ SchemaId.GetHashCode();
                hashCode = hashCode * 397 ^ SchemaVersion.GetHashCode();
                hashCode = hashCode * 397 ^ Payload.GetHashCode();
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
            return $"envelope='{EnvelopeId.StableText}' scope='{Scope}' owner='{OwnerIdentity.StableText}' schema='{SchemaId.StableText}' version='{SchemaVersion.StableText}' capturedUtcTicks='{CapturedUtcTicks}' source='{sourceText}' reason='{reasonText}' payload=({Payload.ToDiagnosticString()})";
        }

        public static SnapshotEnvelope Create(
            string envelopeId,
            SnapshotScope scope,
            FrameworkIdentityKey ownerIdentity,
            string schemaId,
            SnapshotSchemaVersion schemaVersion,
            SnapshotPayload payload,
            long capturedUtcTicks,
            string source,
            string reason)
        {
            return new SnapshotEnvelope(
                SnapshotEnvelopeId.From(envelopeId),
                scope,
                ownerIdentity,
                SnapshotSchemaId.From(schemaId),
                schemaVersion,
                payload,
                capturedUtcTicks,
                source,
                reason);
        }

        public static FrameworkIdentityDomain GetExpectedOwnerDomain(SnapshotScope scope)
        {
            switch (scope)
            {
                case SnapshotScope.Session:
                    return FrameworkIdentityDomain.Session;
                case SnapshotScope.Route:
                    return FrameworkIdentityDomain.Route;
                case SnapshotScope.Activity:
                    return FrameworkIdentityDomain.Activity;
                case SnapshotScope.Local:
                    return FrameworkIdentityDomain.Local;
                case SnapshotScope.Runtime:
                    return FrameworkIdentityDomain.Runtime;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), scope, "Snapshot owner domain cannot be inferred for an unknown scope.");
            }
        }

        public static bool operator ==(SnapshotEnvelope left, SnapshotEnvelope right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SnapshotEnvelope left, SnapshotEnvelope right)
        {
            return !left.Equals(right);
        }

        private static void ValidateScope(SnapshotScope scope)
        {
            if (!Enum.IsDefined(typeof(SnapshotScope), scope) || scope == SnapshotScope.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Snapshot scope must be explicit.");
            }
        }

        private static void ValidateOwner(SnapshotScope scope, FrameworkIdentityKey ownerIdentity)
        {
            if (!ownerIdentity.IsValid)
            {
                throw new ArgumentException("Snapshot owner identity must be valid.", nameof(ownerIdentity));
            }

            var expectedDomain = GetExpectedOwnerDomain(scope);
            if (ownerIdentity.Domain != expectedDomain)
            {
                throw new ArgumentException(
                    $"Snapshot owner for scope '{scope}' must use identity domain '{expectedDomain}', but received '{ownerIdentity.Domain}'.",
                    nameof(ownerIdentity));
            }
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
