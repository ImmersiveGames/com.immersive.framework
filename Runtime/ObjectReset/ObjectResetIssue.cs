using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Structured Object Reset diagnostic issue.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14B Object Reset structured diagnostic issue.")]
    public readonly struct ObjectResetIssue : IEquatable<ObjectResetIssue>
    {
        public ObjectResetIssue(ObjectResetIssueSeverity severity, ObjectResetIssueKind kind, string message)
        {
            if (!Enum.IsDefined(typeof(ObjectResetIssueSeverity), severity) || severity == ObjectResetIssueSeverity.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(severity), severity, "Object Reset issue severity must be explicit.");
            }

            if (!Enum.IsDefined(typeof(ObjectResetIssueKind), kind) || kind == ObjectResetIssueKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Object Reset issue kind must be explicit.");
            }

            Severity = severity;
            Kind = kind;
            Message = message.NormalizeTextOrFallback(kind.ToString());
        }

        public ObjectResetIssueSeverity Severity { get; }

        public ObjectResetIssueKind Kind { get; }

        public string Message { get; }

        public bool IsBlocking => Severity == ObjectResetIssueSeverity.Error;

        public bool Equals(ObjectResetIssue other)
        {
            return Severity == other.Severity
                && Kind == other.Kind
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectResetIssue other && Equals(other);
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
            return $"severity='{Severity}' kind='{Kind}' message='{Message}'";
        }

        public static ObjectResetIssue Error(ObjectResetIssueKind kind, string message)
        {
            return new ObjectResetIssue(ObjectResetIssueSeverity.Error, kind, message);
        }

        public static ObjectResetIssue Warning(ObjectResetIssueKind kind, string message)
        {
            return new ObjectResetIssue(ObjectResetIssueSeverity.Warning, kind, message);
        }

        public static ObjectResetIssue Info(ObjectResetIssueKind kind, string message)
        {
            return new ObjectResetIssue(ObjectResetIssueSeverity.Info, kind, message);
        }
    }
}
