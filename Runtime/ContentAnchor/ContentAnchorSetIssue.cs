using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Passive diagnostic issue produced by ContentAnchorSet construction.
    /// It is not an authoring validator result and does not emit logs or block lifecycle by itself.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Content Anchor Set diagnostic issue introduced by F7E.")]
    public readonly struct ContentAnchorSetIssue : IEquatable<ContentAnchorSetIssue>
    {
        public ContentAnchorSetIssue(
            ContentAnchorSetIssueKind kind,
            string key,
            ContentAnchorDeclaration declaration,
            string message)
        {
            if (kind == ContentAnchorSetIssueKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Content Anchor Set issue kind must be explicit.");
            }

            Kind = kind;
            Key = Normalize(key);
            Declaration = declaration;
            Message = Normalize(message);
        }

        public ContentAnchorSetIssueKind Kind { get; }

        public string Key { get; }

        public ContentAnchorDeclaration Declaration { get; }

        public string Message { get; }

        public bool HasDeclaration => Declaration.IsValid;

        public bool Equals(ContentAnchorSetIssue other)
        {
            return Kind == other.Kind
                && string.Equals(Key, other.Key, StringComparison.Ordinal)
                && Declaration.Equals(other.Declaration)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ContentAnchorSetIssue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Kind;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Key ?? string.Empty);
                hashCode = hashCode * 397 ^ Declaration.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public string ToDiagnosticString()
        {
            string declaration = HasDeclaration ? Declaration.ToDiagnosticString() : "<invalid>";
            return $"kind='{Kind}' key='{Key}' message='{Message}' declaration=[{declaration}]";
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public static bool operator ==(ContentAnchorSetIssue left, ContentAnchorSetIssue right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ContentAnchorSetIssue left, ContentAnchorSetIssue right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
