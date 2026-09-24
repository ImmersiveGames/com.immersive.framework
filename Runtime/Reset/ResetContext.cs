using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Context supplied to one reset participant invocation.
    /// The participant invocation remains synchronous; orchestration awaits only at the executor boundary.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A Reset context primitive for synchronous participants.")]
    public readonly struct ResetContext : IEquatable<ResetContext>
    {
        public ResetContext(
            ResetSubject subject,
            ResetParticipantDescriptor participant,
            string source,
            string reason)
        {
            if (!subject.IsValid)
            {
                throw new ArgumentException("Reset context requires a valid subject.", nameof(subject));
            }

            if (!participant.IsValid)
            {
                throw new ArgumentException("Reset context requires a valid participant descriptor.", nameof(participant));
            }

            if (participant.SubjectId != subject.SubjectId)
            {
                throw new ArgumentException(
                    $"Reset context participant subject '{participant.SubjectId.StableText}' does not match subject '{subject.SubjectId.StableText}'.",
                    nameof(participant));
            }

            Subject = subject;
            Participant = participant;
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
        }

        public ResetSubject Subject { get; }

        public ResetParticipantDescriptor Participant { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsValid => Subject.IsValid && Participant.IsValid && Subject.SubjectId == Participant.SubjectId;

        public bool Equals(ResetContext other)
        {
            return Subject.Equals(other.Subject)
                && Participant.Equals(other.Participant)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ResetContext other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Subject.GetHashCode();
                hashCode = hashCode * 397 ^ Participant.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            return $"subjectId='{Subject.SubjectId.StableText}' participantId='{Participant.ParticipantId.StableText}' source='{sourceText}' reason='{reasonText}'";
        }
    }
}
