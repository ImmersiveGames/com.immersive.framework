using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// API status: Experimental. Structured diagnostic issue produced by object entry planning or validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Object Entry structured issue introduced by F13A.")]
    public readonly struct ObjectEntryIssue : IEquatable<ObjectEntryIssue>
    {
        public ObjectEntryIssue(ObjectEntryIssueSeverity severity, ObjectEntryIssueKind kind, string message)
        {
            Severity = severity;
            Kind = kind;
            Message = message.NormalizeTextOrFallback(kind.ToString());
        }

        public ObjectEntryIssueSeverity Severity { get; }

        public ObjectEntryIssueKind Kind { get; }

        public string Message { get; }

        public bool IsBlocking => Severity == ObjectEntryIssueSeverity.Error;

        public bool Equals(ObjectEntryIssue other)
        {
            return Severity == other.Severity
                && Kind == other.Kind
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectEntryIssue other && Equals(other);
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

        public static ObjectEntryIssue Error(ObjectEntryIssueKind kind, string message)
        {
            return new ObjectEntryIssue(ObjectEntryIssueSeverity.Error, kind, message);
        }

        public static ObjectEntryIssue Warning(ObjectEntryIssueKind kind, string message)
        {
            return new ObjectEntryIssue(ObjectEntryIssueSeverity.Warning, kind, message);
        }

        public static ObjectEntryIssue Info(ObjectEntryIssueKind kind, string message)
        {
            return new ObjectEntryIssue(ObjectEntryIssueSeverity.Info, kind, message);
        }
    }
}
