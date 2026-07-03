using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Diagnostic entry emitted by PlayerActor validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F31A PlayerActor validation issue.")]
    public readonly struct PlayerActorSetIssue : IEquatable<PlayerActorSetIssue>
    {
        public PlayerActorSetIssue(
            PlayerActorSetIssueKind kind,
            string actorIdText,
            string source,
            string message,
            bool blocking)
        {
            if (!Enum.IsDefined(typeof(PlayerActorSetIssueKind), kind) || kind == PlayerActorSetIssueKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "PlayerActor issue kind must be explicit.");
            }

            Kind = kind;
            ActorIdText = actorIdText.NormalizeText();
            Source = source.NormalizeTextOrFallback(nameof(PlayerActorSetIssue));
            Message = message.NormalizeText();
            Blocking = blocking;
        }

        public PlayerActorSetIssueKind Kind { get; }

        public string ActorIdText { get; }

        public string Source { get; }

        public string Message { get; }

        public bool Blocking { get; }

        public bool Equals(PlayerActorSetIssue other)
        {
            return Kind == other.Kind
                && string.Equals(ActorIdText, other.ActorIdText, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && Blocking == other.Blocking;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerActorSetIssue other && Equals(other);
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

        internal static PlayerActorSetIssue BlockingIssue(
            PlayerActorSetIssueKind kind,
            string actorIdText,
            string source,
            string message)
        {
            return new PlayerActorSetIssue(kind, actorIdText, source, message, true);
        }
    }
}
