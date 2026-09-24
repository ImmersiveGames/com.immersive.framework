using System;

namespace Immersive.Framework.Common
{
    internal readonly struct ParticipantExecutionEntry<TParticipant> : IEquatable<ParticipantExecutionEntry<TParticipant>>
        where TParticipant : class
    {
        public ParticipantExecutionEntry(
            TParticipant participant,
            ParticipantRequiredness requiredness,
            int order,
            int sourceIndex,
            string label)
        {
            if (participant == null)
            {
                throw new ArgumentNullException(nameof(participant), "Participant execution entry requires a participant.");
            }

            ParticipantValidation.ThrowIfUndefined(requiredness, nameof(requiredness));

            if (sourceIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), sourceIndex, "Participant execution entry source index cannot be negative.");
            }

            Participant = participant;
            Requiredness = requiredness;
            Order = order;
            SourceIndex = sourceIndex;
            Label = label.NormalizeText();
        }

        public TParticipant Participant { get; }

        public ParticipantRequiredness Requiredness { get; }

        public int Order { get; }

        public int SourceIndex { get; }

        public string Label { get; }

        public bool IsRequired => Requiredness == ParticipantRequiredness.Required;

        public bool IsOptional => Requiredness == ParticipantRequiredness.Optional;

        public bool IsValid => Participant != null
            && ParticipantValidation.IsDefined(Requiredness)
            && SourceIndex >= 0;

        public bool Equals(ParticipantExecutionEntry<TParticipant> other)
        {
            return ReferenceEquals(Participant, other.Participant)
                && Requiredness == other.Requiredness
                && Order == other.Order
                && SourceIndex == other.SourceIndex
                && string.Equals(Label, other.Label, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ParticipantExecutionEntry<TParticipant> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Participant != null ? Participant.GetHashCode() : 0;
                hashCode = hashCode * 397 ^ (int)Requiredness;
                hashCode = hashCode * 397 ^ Order;
                hashCode = hashCode * 397 ^ SourceIndex;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Label ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string labelText = Label.ToDiagnosticText();
            return $"order='{Order}' sourceIndex='{SourceIndex}' requiredness='{Requiredness}' label='{labelText}'";
        }
    }
}
