using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.Common;

namespace Immersive.Framework.Snapshot
{
    /// <summary>
    /// API status: Experimental. Passive descriptor for one Snapshot participant.
    /// Descriptor data is identity, schema and ordering metadata only. It must not be derived from scene hierarchy, GameObject names, file names or backend keys.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21C Snapshot participant descriptor; passive metadata only.")]
    public readonly struct SnapshotParticipantDescriptor : IEquatable<SnapshotParticipantDescriptor>
    {
        public SnapshotParticipantDescriptor(
            SnapshotParticipantId participantId,
            SnapshotScope scope,
            FrameworkIdentityKey ownerIdentity,
            SnapshotSchemaId schemaId,
            SnapshotSchemaVersion schemaVersion,
            SnapshotParticipantRequiredness requiredness,
            int order,
            string displayName,
            string source,
            string reason)
        {
            if (!participantId.IsValid)
            {
                throw new ArgumentException("Snapshot participant descriptor requires a valid participant id.", nameof(participantId));
            }

            ValidateScope(scope);
            ValidateOwner(scope, ownerIdentity);

            if (!schemaId.IsValid)
            {
                throw new ArgumentException("Snapshot participant descriptor requires a valid schema id.", nameof(schemaId));
            }

            if (!schemaVersion.IsValid)
            {
                throw new ArgumentException("Snapshot participant descriptor requires a valid schema version.", nameof(schemaVersion));
            }

            if (!Enum.IsDefined(typeof(SnapshotParticipantRequiredness), requiredness)
                || requiredness == SnapshotParticipantRequiredness.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredness), requiredness, "Snapshot participant requiredness must be explicit.");
            }

            ParticipantId = participantId;
            Scope = scope;
            OwnerIdentity = ownerIdentity;
            SchemaId = schemaId;
            SchemaVersion = schemaVersion;
            Requiredness = requiredness;
            Order = order;
            DisplayName = Normalize(displayName);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public SnapshotParticipantId ParticipantId { get; }

        public SnapshotScope Scope { get; }

        public FrameworkIdentityKey OwnerIdentity { get; }

        public SnapshotSchemaId SchemaId { get; }

        public SnapshotSchemaVersion SchemaVersion { get; }

        public SnapshotParticipantRequiredness Requiredness { get; }

        public int Order { get; }

        public string DisplayName { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsRequired => Requiredness == SnapshotParticipantRequiredness.Required;

        public bool IsOptional => Requiredness == SnapshotParticipantRequiredness.Optional;

        public bool HasDisplayName => !string.IsNullOrWhiteSpace(DisplayName);

        public bool IsValid => ParticipantId.IsValid
            && Scope != SnapshotScope.Unknown
            && OwnerIdentity.IsValid
            && OwnerIdentity.Domain == SnapshotEnvelope.GetExpectedOwnerDomain(Scope)
            && SchemaId.IsValid
            && SchemaVersion.IsValid
            && Requiredness != SnapshotParticipantRequiredness.Unknown;

        public bool SupportsCaptureContext(SnapshotCaptureContext context)
        {
            return context.IsValid
                && Scope == context.Scope
                && OwnerIdentity.Equals(context.OwnerIdentity);
        }

        public bool SupportsEnvelope(SnapshotEnvelope envelope)
        {
            return envelope.IsValid
                && Scope == envelope.Scope
                && OwnerIdentity.Equals(envelope.OwnerIdentity)
                && SchemaId.Equals(envelope.SchemaId)
                && SchemaVersion.Equals(envelope.SchemaVersion);
        }

        public bool Equals(SnapshotParticipantDescriptor other)
        {
            return ParticipantId.Equals(other.ParticipantId)
                && Scope == other.Scope
                && OwnerIdentity.Equals(other.OwnerIdentity)
                && SchemaId.Equals(other.SchemaId)
                && SchemaVersion.Equals(other.SchemaVersion)
                && Requiredness == other.Requiredness
                && Order == other.Order
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is SnapshotParticipantDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ParticipantId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Scope;
                hashCode = hashCode * 397 ^ OwnerIdentity.GetHashCode();
                hashCode = hashCode * 397 ^ SchemaId.GetHashCode();
                hashCode = hashCode * 397 ^ SchemaVersion.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Requiredness;
                hashCode = hashCode * 397 ^ Order;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(DisplayName ?? string.Empty);
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
            string displayNameText = HasDisplayName ? DisplayName : "<none>";
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            return $"participant='{ParticipantId.StableText}' scope='{Scope}' owner='{OwnerIdentity.StableText}' schema='{SchemaId.StableText}' version='{SchemaVersion.StableText}' requiredness='{Requiredness}' order='{Order}' displayName='{displayNameText}' source='{sourceText}' reason='{reasonText}'";
        }

        public static SnapshotParticipantDescriptor Required(
            SnapshotParticipantId participantId,
            SnapshotScope scope,
            FrameworkIdentityKey ownerIdentity,
            SnapshotSchemaId schemaId,
            SnapshotSchemaVersion schemaVersion,
            int order,
            string displayName,
            string source,
            string reason)
        {
            return new SnapshotParticipantDescriptor(
                participantId,
                scope,
                ownerIdentity,
                schemaId,
                schemaVersion,
                SnapshotParticipantRequiredness.Required,
                order,
                displayName,
                source,
                reason);
        }

        public static SnapshotParticipantDescriptor Optional(
            SnapshotParticipantId participantId,
            SnapshotScope scope,
            FrameworkIdentityKey ownerIdentity,
            SnapshotSchemaId schemaId,
            SnapshotSchemaVersion schemaVersion,
            int order,
            string displayName,
            string source,
            string reason)
        {
            return new SnapshotParticipantDescriptor(
                participantId,
                scope,
                ownerIdentity,
                schemaId,
                schemaVersion,
                SnapshotParticipantRequiredness.Optional,
                order,
                displayName,
                source,
                reason);
        }

        public static bool operator ==(SnapshotParticipantDescriptor left, SnapshotParticipantDescriptor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SnapshotParticipantDescriptor left, SnapshotParticipantDescriptor right)
        {
            return !left.Equals(right);
        }

        private static void ValidateScope(SnapshotScope scope)
        {
            if (!Enum.IsDefined(typeof(SnapshotScope), scope) || scope == SnapshotScope.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Snapshot participant scope must be explicit.");
            }
        }

        private static void ValidateOwner(SnapshotScope scope, FrameworkIdentityKey ownerIdentity)
        {
            if (!ownerIdentity.IsValid)
            {
                throw new ArgumentException("Snapshot participant owner identity must be valid.", nameof(ownerIdentity));
            }

            var expectedDomain = SnapshotEnvelope.GetExpectedOwnerDomain(scope);
            if (ownerIdentity.Domain != expectedDomain)
            {
                throw new ArgumentException(
                    $"Snapshot participant owner for scope '{scope}' must use identity domain '{expectedDomain}', but received '{ownerIdentity.Domain}'.",
                    nameof(ownerIdentity));
            }
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
