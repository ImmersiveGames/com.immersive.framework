using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Immutable diagnostic issue emitted by passive Player binding readiness summary.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49K passive Player binding readiness diagnostic issue.")]
    public readonly struct PlayerBindingReadinessIssue : IEquatable<PlayerBindingReadinessIssue>
    {
        public PlayerBindingReadinessIssue(
            PlayerBindingReadinessIssueKind kind,
            string source,
            string message,
            bool blocking)
        {
            if (!Enum.IsDefined(typeof(PlayerBindingReadinessIssueKind), kind) || kind == PlayerBindingReadinessIssueKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Player binding readiness issue kind must be explicit.");
            }

            Kind = kind;
            Source = source.NormalizeTextOrFallback(nameof(PlayerBindingReadinessIssue));
            Message = message.NormalizeText();
            Blocking = blocking;
        }

        public PlayerBindingReadinessIssueKind Kind { get; }

        public string Source { get; }

        public string Message { get; }

        public bool Blocking { get; }

        public bool Equals(PlayerBindingReadinessIssue other)
        {
            return Kind == other.Kind
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && Blocking == other.Blocking;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerBindingReadinessIssue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                hash = (hash * 397) ^ Blocking.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return Kind.ToString();
        }

        public static PlayerBindingReadinessIssue BlockingIssue(
            PlayerBindingReadinessIssueKind kind,
            string source,
            string message)
        {
            return new PlayerBindingReadinessIssue(kind, source, message, true);
        }

        public static PlayerBindingReadinessIssue NonBlockingIssue(
            PlayerBindingReadinessIssueKind kind,
            string source,
            string message)
        {
            return new PlayerBindingReadinessIssue(kind, source, message, false);
        }
    }
}
