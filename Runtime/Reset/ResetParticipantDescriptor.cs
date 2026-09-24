using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Passive descriptor for one reset participant registered under a ResetSubject.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A Reset participant descriptor without ObjectEntry target dependency.")]
    public readonly struct ResetParticipantDescriptor : IEquatable<ResetParticipantDescriptor>
    {
        public ResetParticipantDescriptor(
            ResetParticipantId participantId,
            ResetSubjectId subjectId,
            ResetParticipantRequiredness requiredness,
            int order,
            string displayName,
            string source,
            string reason)
        {
            if (!participantId.IsValid)
            {
                throw new ArgumentException("Reset participant descriptor requires a valid participant id.", nameof(participantId));
            }

            if (!subjectId.IsValid)
            {
                throw new ArgumentException("Reset participant descriptor requires a valid subject id.", nameof(subjectId));
            }

            if (!Enum.IsDefined(typeof(ResetParticipantRequiredness), requiredness)
                || requiredness == ResetParticipantRequiredness.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredness), requiredness, "Reset participant requiredness must be explicit.");
            }

            ParticipantId = participantId;
            SubjectId = subjectId;
            Requiredness = requiredness;
            Order = order;
            DisplayName = displayName.NormalizeText();
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
        }

        public ResetParticipantId ParticipantId { get; }

        public ResetSubjectId SubjectId { get; }

        public ResetParticipantRequiredness Requiredness { get; }

        public int Order { get; }

        public string DisplayName { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsRequired => Requiredness == ResetParticipantRequiredness.Required;

        public bool IsOptional => Requiredness == ResetParticipantRequiredness.Optional;

        public bool IsValid => ParticipantId.IsValid && SubjectId.IsValid && Requiredness != ResetParticipantRequiredness.Unknown;

        public bool Equals(ResetParticipantDescriptor other)
        {
            return ParticipantId.Equals(other.ParticipantId)
                && SubjectId.Equals(other.SubjectId)
                && Requiredness == other.Requiredness
                && Order == other.Order
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ResetParticipantDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ParticipantId.GetHashCode();
                hashCode = hashCode * 397 ^ SubjectId.GetHashCode();
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
            string displayNameText = DisplayName.ToDiagnosticText("<unnamed>");
            string sourceText = Source.ToDiagnosticText("<none>");
            string reasonText = Reason.ToDiagnosticText("<none>");
            return $"participantId='{ParticipantId.StableText}' subjectId='{SubjectId.StableText}' requiredness='{Requiredness}' order='{Order}' displayName='{displayNameText}' source='{sourceText}' reason='{reasonText}'";
        }
    }
}
