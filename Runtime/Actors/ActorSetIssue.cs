using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Diagnostic entry emitted by generic actor validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F45A generic actor validation issue.")]
    public readonly struct ActorSetIssue : IEquatable<ActorSetIssue>
    {
        public ActorSetIssue(
            ActorSetIssueKind kind,
            string actorIdText,
            string source,
            string message,
            bool blocking)
        {
            if (!Enum.IsDefined(typeof(ActorSetIssueKind), kind) || kind == ActorSetIssueKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Actor issue kind must be explicit.");
            }

            Kind = kind;
            ActorIdText = actorIdText.NormalizeText();
            Source = source.NormalizeTextOrFallback(nameof(ActorSetIssue));
            Message = message.NormalizeText();
            Blocking = blocking;
        }

        public ActorSetIssueKind Kind { get; }

        public string ActorIdText { get; }

        public string Source { get; }

        public string Message { get; }

        public bool Blocking { get; }

        public bool Equals(ActorSetIssue other)
        {
            return Kind == other.Kind
                && string.Equals(ActorIdText, other.ActorIdText, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && Blocking == other.Blocking;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorSetIssue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(ActorIdText ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                hash = (hash * 397) ^ Blocking.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{Kind}:{ActorIdText}";
        }

        internal static ActorSetIssue BlockingIssue(
            ActorSetIssueKind kind,
            string actorIdText,
            string source,
            string message)
        {
            return new ActorSetIssue(kind, actorIdText, source, message, true);
        }

        internal static ActorSetIssue NonBlockingIssue(
            ActorSetIssueKind kind,
            string actorIdText,
            string source,
            string message)
        {
            return new ActorSetIssue(kind, actorIdText, source, message, false);
        }
    }
}
