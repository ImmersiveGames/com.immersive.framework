using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Passive diagnostics issue emitted by Pause validation or result generation.
    /// Issues do not log by themselves and do not mutate Pause, Gate, UI, input or timescale.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F20B Pause issue primitive; diagnostics only.")]
    public readonly struct PauseIssue : IEquatable<PauseIssue>
    {
        public PauseIssue(
            string code,
            PauseIssueSeverity severity,
            string source,
            string reason,
            string message)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Pause issue code is required.", nameof(code));
            }

            if (!Enum.IsDefined(typeof(PauseIssueSeverity), severity) || severity == PauseIssueSeverity.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(severity), severity, "Pause issue severity must be explicit.");
            }

            Code = Normalize(code);
            Severity = severity;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public string Code { get; }

        public PauseIssueSeverity Severity { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool IsValid => !string.IsNullOrWhiteSpace(Code) && Severity != PauseIssueSeverity.Unknown;

        public bool BlocksRequest => Severity == PauseIssueSeverity.Blocking;

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public bool Equals(PauseIssue other)
        {
            return string.Equals(Code, other.Code, StringComparison.Ordinal)
                && Severity == other.Severity
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseIssue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = StringComparer.Ordinal.GetHashCode(Code ?? string.Empty);
                hashCode = hashCode * 397 ^ (int)Severity;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            return $"code='{Code}' severity='{Severity}' blocksRequest='{BlocksRequest}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static PauseIssue Info(string code, string source, string reason, string message)
        {
            return new PauseIssue(code, PauseIssueSeverity.Info, source, reason, message);
        }

        public static PauseIssue Warning(string code, string source, string reason, string message)
        {
            return new PauseIssue(code, PauseIssueSeverity.Warning, source, reason, message);
        }

        public static PauseIssue Blocking(string code, string source, string reason, string message)
        {
            return new PauseIssue(code, PauseIssueSeverity.Blocking, source, reason, message);
        }

        public static bool operator ==(PauseIssue left, PauseIssue right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PauseIssue left, PauseIssue right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
