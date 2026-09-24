using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Passive descriptor for one Cycle Reset participant.
    /// Descriptor data is functional identity and orchestration metadata only; it must not be derived from scene hierarchy or object names.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11A Cycle Reset participant descriptor; passive metadata only.")]
    public readonly struct CycleResetParticipantDescriptor : IEquatable<CycleResetParticipantDescriptor>
    {
        public CycleResetParticipantDescriptor(
            CycleResetParticipantId participantId,
            CycleResetScope participantScope,
            CycleResetParticipantRequiredness requiredness,
            int order,
            string displayName,
            string source,
            string reason)
        {
            if (!participantId.IsValid)
            {
                throw new ArgumentException("Cycle Reset participant descriptor requires a valid participant id.", nameof(participantId));
            }

            if (!Enum.IsDefined(typeof(CycleResetScope), participantScope) || participantScope == CycleResetScope.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(participantScope), participantScope, "Cycle Reset participant scope must be Route or Activity.");
            }

            if (!Enum.IsDefined(typeof(CycleResetParticipantRequiredness), requiredness)
                || requiredness == CycleResetParticipantRequiredness.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredness), requiredness, "Cycle Reset participant requiredness must be explicit.");
            }

            ParticipantId = participantId;
            ParticipantScope = participantScope;
            Requiredness = requiredness;
            Order = order;
            DisplayName = Normalize(displayName);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public CycleResetParticipantId ParticipantId { get; }

        public CycleResetScope ParticipantScope { get; }

        public CycleResetParticipantRequiredness Requiredness { get; }

        public int Order { get; }

        public string DisplayName { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsRequired => Requiredness == CycleResetParticipantRequiredness.Required;

        public bool IsOptional => Requiredness == CycleResetParticipantRequiredness.Optional;

        public bool IsRouteParticipant => ParticipantScope == CycleResetScope.Route;

        public bool IsActivityParticipant => ParticipantScope == CycleResetScope.Activity;

        public bool IsValid => ParticipantId.IsValid
            && ParticipantScope != CycleResetScope.Unknown
            && Requiredness != CycleResetParticipantRequiredness.Unknown;

        public bool SupportsRequest(CycleResetRequest request)
        {
            return request.IsValid && request.AcceptsParticipantScope(ParticipantScope);
        }

        public bool Equals(CycleResetParticipantDescriptor other)
        {
            return ParticipantId.Equals(other.ParticipantId)
                && ParticipantScope == other.ParticipantScope
                && Requiredness == other.Requiredness
                && Order == other.Order
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CycleResetParticipantDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ParticipantId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)ParticipantScope;
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
            return $"participantId='{ParticipantId.StableText}' participantScope='{ParticipantScope}' requiredness='{Requiredness}' order='{Order}' displayName='{displayNameText}' source='{sourceText}' reason='{reasonText}'";
        }

        public static CycleResetParticipantDescriptor Required(
            CycleResetParticipantId participantId,
            CycleResetScope participantScope,
            int order,
            string displayName,
            string source,
            string reason)
        {
            return new CycleResetParticipantDescriptor(
                participantId,
                participantScope,
                CycleResetParticipantRequiredness.Required,
                order,
                displayName,
                source,
                reason);
        }

        public static CycleResetParticipantDescriptor Optional(
            CycleResetParticipantId participantId,
            CycleResetScope participantScope,
            int order,
            string displayName,
            string source,
            string reason)
        {
            return new CycleResetParticipantDescriptor(
                participantId,
                participantScope,
                CycleResetParticipantRequiredness.Optional,
                order,
                displayName,
                source,
                reason);
        }

        public static bool operator ==(CycleResetParticipantDescriptor left, CycleResetParticipantDescriptor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CycleResetParticipantDescriptor left, CycleResetParticipantDescriptor right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
