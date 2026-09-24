using System;

namespace Immersive.Framework.Common
{
    internal readonly struct ParticipantExecutionIssue : IEquatable<ParticipantExecutionIssue>
    {
        public ParticipantExecutionIssue(
            ParticipantExecutionIssueSeverity severity,
            string participantLabel,
            string source,
            string reason,
            string message,
            int issueCount)
        {
            FrameworkEnumValidation.ThrowIfUndefinedOr(
                severity,
                ParticipantExecutionIssueSeverity.Unknown,
                nameof(severity),
                "Participant execution issue severity must be explicit.");

            if (issueCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(issueCount), issueCount, "Participant execution issue count must be positive.");
            }

            Severity = severity;
            ParticipantLabel = participantLabel.NormalizeText();
            Source = source.NormalizeText();
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
            IssueCount = issueCount;
        }

        public ParticipantExecutionIssueSeverity Severity { get; }

        public string ParticipantLabel { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public int IssueCount { get; }

        public bool IsBlocking => Severity == ParticipantExecutionIssueSeverity.Error;

        public bool IsWarning => Severity == ParticipantExecutionIssueSeverity.Warning;

        public bool Equals(ParticipantExecutionIssue other)
        {
            return Severity == other.Severity
                && string.Equals(ParticipantLabel, other.ParticipantLabel, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && IssueCount == other.IssueCount;
        }

        public override bool Equals(object obj)
        {
            return obj is ParticipantExecutionIssue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Severity;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(ParticipantLabel ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                hashCode = hashCode * 397 ^ IssueCount;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string participantLabelText = ParticipantLabel.ToDiagnosticText();
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            return $"severity='{Severity}' participantLabel='{participantLabelText}' issueCount='{IssueCount}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }
    }
}
