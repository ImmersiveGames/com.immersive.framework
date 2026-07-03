using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Passive issue reported by a Loading operation/result.
    /// It is diagnostics data only; it does not retry, fallback, execute lifecycle or show UI.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22H Loading issue primitive; diagnostics/reporting only.")]
    public readonly struct LoadingIssue : IEquatable<LoadingIssue>
    {
        public LoadingIssue(
            LoadingOperationId operationId,
            LoadingIssueSeverity severity,
            string code,
            string source,
            string message)
        {
            if (!operationId.IsValid)
            {
                throw new ArgumentException("Loading issue requires a valid operation id.", nameof(operationId));
            }

            if (!Enum.IsDefined(typeof(LoadingIssueSeverity), severity) || severity == LoadingIssueSeverity.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(severity), severity, "Loading issue severity must be explicit.");
            }

            OperationId = operationId;
            Severity = severity;
            Code = Normalize(code);
            Source = Normalize(source);
            Message = Normalize(message);
        }

        public LoadingOperationId OperationId { get; }

        public LoadingIssueSeverity Severity { get; }

        public string Code { get; }

        public string Source { get; }

        public string Message { get; }

        public bool IsValid => OperationId.IsValid && Severity != LoadingIssueSeverity.Unknown;

        public bool BlocksCompletion => Severity is LoadingIssueSeverity.Blocking or LoadingIssueSeverity.Error;

        public bool IsWarningOrHigher => Severity is LoadingIssueSeverity.Warning or LoadingIssueSeverity.Error or LoadingIssueSeverity.Blocking;

        public bool HasCode => !string.IsNullOrWhiteSpace(Code);

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public bool Equals(LoadingIssue other)
        {
            return OperationId.Equals(other.OperationId)
                && Severity == other.Severity
                && string.Equals(Code, other.Code, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingIssue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = OperationId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Severity;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Code ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
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
            string codeText = HasCode ? Code : "<none>";
            string sourceText = HasSource ? Source : "<none>";
            string messageText = HasMessage ? Message : "<none>";
            return $"operation='{OperationId.StableText}' severity='{Severity}' code='{codeText}' source='{sourceText}' message='{messageText}' blocksCompletion='{BlocksCompletion}'";
        }

        public static LoadingIssue Info(LoadingOperationId operationId, string code, string source, string message)
        {
            return new LoadingIssue(operationId, LoadingIssueSeverity.Info, code, source, message);
        }

        public static LoadingIssue Warning(LoadingOperationId operationId, string code, string source, string message)
        {
            return new LoadingIssue(operationId, LoadingIssueSeverity.Warning, code, source, message);
        }

        public static LoadingIssue Error(LoadingOperationId operationId, string code, string source, string message)
        {
            return new LoadingIssue(operationId, LoadingIssueSeverity.Error, code, source, message);
        }

        public static LoadingIssue Blocking(LoadingOperationId operationId, string code, string source, string message)
        {
            return new LoadingIssue(operationId, LoadingIssueSeverity.Blocking, code, source, message);
        }

        public static bool operator ==(LoadingIssue left, LoadingIssue right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingIssue left, LoadingIssue right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
