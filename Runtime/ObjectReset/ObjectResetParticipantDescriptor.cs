using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.Common;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Passive descriptor for one Object Reset participant.
    /// Descriptor data is functional identity and orchestration metadata only; it must not be derived from scene hierarchy or object names.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14C Object Reset participant descriptor; passive metadata only.")]
    public readonly struct ObjectResetParticipantDescriptor : IEquatable<ObjectResetParticipantDescriptor>
    {
        public ObjectResetParticipantDescriptor(
            ObjectResetParticipantId participantId,
            ObjectResetTarget target,
            ObjectResetParticipantRequiredness requiredness,
            int order,
            string displayName,
            string source,
            string reason)
        {
            if (!participantId.IsValid)
            {
                throw new ArgumentException("Object Reset participant descriptor requires a valid participant id.", nameof(participantId));
            }

            if (!target.IsValid)
            {
                throw new ArgumentException("Object Reset participant descriptor requires a valid target.", nameof(target));
            }

            if (!Enum.IsDefined(typeof(ObjectResetParticipantRequiredness), requiredness)
                || requiredness == ObjectResetParticipantRequiredness.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredness), requiredness, "Object Reset participant requiredness must be explicit.");
            }

            ParticipantId = participantId;
            Target = target;
            Requiredness = requiredness;
            Order = order;
            DisplayName = Normalize(displayName);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ObjectResetParticipantId ParticipantId { get; }

        public ObjectResetTarget Target { get; }

        public ObjectResetParticipantRequiredness Requiredness { get; }

        public int Order { get; }

        public string DisplayName { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsRequired => Requiredness == ObjectResetParticipantRequiredness.Required;

        public bool IsOptional => Requiredness == ObjectResetParticipantRequiredness.Optional;

        public bool IsValid => ParticipantId.IsValid && Target.IsValid && Requiredness != ObjectResetParticipantRequiredness.Unknown;

        public bool SupportsRequest(ObjectResetRequest request)
        {
            return request.IsValid && Target == request.Target;
        }

        public bool SupportsResolvedTarget(ObjectResetRequest request, ObjectEntryDescriptor resolvedTarget)
        {
            return SupportsRequest(request)
                && request.Target.Matches(resolvedTarget)
                && Target.Matches(resolvedTarget);
        }

        public bool Equals(ObjectResetParticipantDescriptor other)
        {
            return ParticipantId.Equals(other.ParticipantId)
                && Target.Equals(other.Target)
                && Requiredness == other.Requiredness
                && Order == other.Order
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectResetParticipantDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ParticipantId.GetHashCode();
                hashCode = hashCode * 397 ^ Target.GetHashCode();
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
            string displayNameText = DisplayName.ToDiagnosticText();
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            return $"participantId='{ParticipantId.StableText}' requiredness='{Requiredness}' order='{Order}' displayName='{displayNameText}' source='{sourceText}' reason='{reasonText}' {Target.ToDiagnosticString()}";
        }

        public static ObjectResetParticipantDescriptor Required(
            ObjectResetParticipantId participantId,
            ObjectResetTarget target,
            int order,
            string displayName,
            string source,
            string reason)
        {
            return new ObjectResetParticipantDescriptor(
                participantId,
                target,
                ObjectResetParticipantRequiredness.Required,
                order,
                displayName,
                source,
                reason);
        }

        public static ObjectResetParticipantDescriptor Optional(
            ObjectResetParticipantId participantId,
            ObjectResetTarget target,
            int order,
            string displayName,
            string source,
            string reason)
        {
            return new ObjectResetParticipantDescriptor(
                participantId,
                target,
                ObjectResetParticipantRequiredness.Optional,
                order,
                displayName,
                source,
                reason);
        }

        public static bool operator ==(ObjectResetParticipantDescriptor left, ObjectResetParticipantDescriptor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ObjectResetParticipantDescriptor left, ObjectResetParticipantDescriptor right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
