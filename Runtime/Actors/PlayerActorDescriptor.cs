using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Passive description of a framework-recognized PlayerActor.
    /// This is evidence only; it does not read input, switch action maps, move an actor or spawn an actor.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F31A PlayerActor passive descriptor.")]
    public readonly struct PlayerActorDescriptor : IEquatable<PlayerActorDescriptor>
    {
        public PlayerActorDescriptor(
            ActorId actorId,
            bool hasPlayerInputEvidence,
            string displayName,
            string sceneName,
            string objectName,
            string source,
            string reason)
        {
            if (!actorId.IsValid)
            {
                throw new ArgumentException("PlayerActor descriptor requires a valid actor id.", nameof(actorId));
            }

            ActorId = actorId;
            HasPlayerInputEvidence = hasPlayerInputEvidence;
            DisplayName = displayName.NormalizeTextOrFallback(actorId.StableText);
            SceneName = sceneName.NormalizeText();
            ObjectName = objectName.NormalizeText();
            Source = source.NormalizeTextOrFallback(nameof(PlayerActorDescriptor));
            Reason = reason.NormalizeText();
        }

        public ActorId ActorId { get; }

        public ActorKind ActorKind => ActorKind.Player;

        public bool HasPlayerInputEvidence { get; }

        public string DisplayName { get; }

        public string SceneName { get; }

        public string ObjectName { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool SwitchesActionMaps => false;

        public bool AppliesInputBehavior => false;

        public bool SpawnsActor => false;

        public bool Equals(PlayerActorDescriptor other)
        {
            return ActorId.Equals(other.ActorId)
                && HasPlayerInputEvidence == other.HasPlayerInputEvidence
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && string.Equals(SceneName, other.SceneName, StringComparison.Ordinal)
                && string.Equals(ObjectName, other.ObjectName, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerActorDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = ActorId.GetHashCode();
                hash = (hash * 397) ^ HasPlayerInputEvidence.GetHashCode();
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
