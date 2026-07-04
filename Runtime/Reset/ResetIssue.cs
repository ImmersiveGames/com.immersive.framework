using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Reset
{
    /// <summary>
    /// API status: Experimental. Structured Reset module diagnostic issue.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "preview.12A Reset structured issue.")]
    public readonly struct ResetIssue : IEquatable<ResetIssue>
    {
        public ResetIssue(ResetIssueSeverity severity, ResetIssueKind kind, string message)
        {
            if (!Enum.IsDefined(typeof(ResetIssueSeverity), severity) || severity == ResetIssueSeverity.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(severity), severity, "Reset issue severity must be explicit.");
            }

            if (!Enum.IsDefined(typeof(ResetIssueKind), kind) || kind == ResetIssueKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Reset issue kind must be explicit.");
            }

            Severity = severity;
            Kind = kind;
            Message = message.NormalizeTextOrFallback(kind.ToString());
        }

        public ResetIssueSeverity Severity { get; }

        public ResetIssueKind Kind { get; }

        public string Message { get; }

        public bool IsBlocking => Severity == ResetIssueSeverity.Error;

        public bool Equals(ResetIssue other)
        {
            return Severity == other.Severity
                && Kind == other.Kind
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ResetIssue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Severity;
                hashCode = hashCode * 397 ^ (int)Kind;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            string messageText = Message.ToDiagnosticText();
            return $"severity='{Severity}' kind='{Kind}' message='{messageText}'";
        }

        public static ResetIssue Error(ResetIssueKind kind, string message)
        {
            return new ResetIssue(ResetIssueSeverity.Error, kind, message);
        }

        public static ResetIssue Warning(ResetIssueKind kind, string message)
        {
            return new ResetIssue(ResetIssueSeverity.Warning, kind, message);
        }

        public static ResetIssue Info(ResetIssueKind kind, string message)
        {
            return new ResetIssue(ResetIssueSeverity.Info, kind, message);
        }
    }
}
