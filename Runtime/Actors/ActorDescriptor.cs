using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Passive description of an actor declaration known to validation.
    /// It does not own actor lifetime, materialization, movement, input, reset, snapshot or save behavior.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F45A generic actor passive descriptor.")]
    public readonly struct ActorDescriptor : IEquatable<ActorDescriptor>
    {
        public ActorDescriptor(
            ActorId actorId,
            ActorKind actorKind,
            ActorRole actorRole,
            string displayName,
            string sceneName,
            string objectName,
            string source,
            string reason)
        {
            if (!actorId.IsValid)
            {
                throw new ArgumentException("Actor descriptor requires a valid actor id.", nameof(actorId));
            }

            if (!Enum.IsDefined(typeof(ActorKind), actorKind))
            {
                throw new ArgumentOutOfRangeException(nameof(actorKind), actorKind, "Actor kind must be defined.");
            }

            if (!Enum.IsDefined(typeof(ActorRole), actorRole))
            {
                throw new ArgumentOutOfRangeException(nameof(actorRole), actorRole, "Actor role must be defined.");
            }

            ActorId = actorId;
            ActorKind = actorKind;
            ActorRole = actorRole;
            DisplayName = displayName.NormalizeTextOrFallback(actorId.StableText);
            SceneName = sceneName.NormalizeText();
            ObjectName = objectName.NormalizeText();
            Source = source.NormalizeTextOrFallback(nameof(ActorDescriptor));
            Reason = reason.NormalizeText();
        }

        public ActorId ActorId { get; }

        public ActorKind ActorKind { get; }

        public ActorRole ActorRole { get; }

        public string DisplayName { get; }

        public string SceneName { get; }

        public string ObjectName { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool OwnsLifetime => false;

        public bool AppliesInputBehavior => false;

        public bool SpawnsActor => false;

        public bool Equals(ActorDescriptor other)
        {
            return ActorId.Equals(other.ActorId)
                && ActorKind == other.ActorKind
                && ActorRole == other.ActorRole
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && string.Equals(SceneName, other.SceneName, StringComparison.Ordinal)
                && string.Equals(ObjectName, other.ObjectName, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActorDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = ActorId.GetHashCode();
                hash = (hash * 397) ^ (int)ActorKind;
                hash = (hash * 397) ^ (int)ActorRole;
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(DisplayName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(SceneName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(ObjectName ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hash;
            }
        }

        public override string ToString()
        {
            return ActorId.StableText;
        }
    }
}
