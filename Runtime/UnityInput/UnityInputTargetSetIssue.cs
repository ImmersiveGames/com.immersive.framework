using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.UnityInput
{
    /// <summary>
    /// API status: Experimental. Diagnostic entry emitted by Unity Input target declaration validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F29A Unity Input target declaration issue.")]
    public readonly struct UnityInputTargetSetIssue : IEquatable<UnityInputTargetSetIssue>
    {
        public UnityInputTargetSetIssue(
            UnityInputTargetSetIssueKind kind,
            UnityInputTargetRole role,
            string targetIdText,
            string source,
            string message,
            bool blocking)
        {
            if (!Enum.IsDefined(typeof(UnityInputTargetSetIssueKind), kind) || kind == UnityInputTargetSetIssueKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unity Input target issue kind must be explicit.");
            }

            Kind = kind;
            Role = role;
            TargetIdText = targetIdText.NormalizeText();
            Source = source.NormalizeTextOrFallback(nameof(UnityInputTargetSetIssue));
            Message = message.NormalizeText();
            Blocking = blocking;
        }

        public UnityInputTargetSetIssueKind Kind { get; }

        public UnityInputTargetRole Role { get; }

        public string TargetIdText { get; }

        public string Source { get; }

        public string Message { get; }

        public bool Blocking { get; }

        public bool Equals(UnityInputTargetSetIssue other)
        {
            return Kind == other.Kind
                && Role == other.Role
                && string.Equals(TargetIdText, other.TargetIdText, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && Blocking == other.Blocking;
        }

        public override bool Equals(object obj)
        {
            return obj is UnityInputTargetSetIssue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ (int)Role;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(TargetIdText ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                hash = (hash * 397) ^ Blocking.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Kind}:{Role}:{TargetIdText}";
        }

        internal static UnityInputTargetSetIssue BlockingIssue(
            UnityInputTargetSetIssueKind kind,
            UnityInputTargetRole role,
            string targetIdText,
            string source,
            string message)
        {
            return new UnityInputTargetSetIssue(kind, role, targetIdText, source, message, true);
        }
    }
}
