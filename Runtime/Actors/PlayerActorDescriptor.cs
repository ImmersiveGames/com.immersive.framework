using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Passive description of a framework-recognized PlayerActor.
    /// This is evidence only; it does not read input, switch action maps, move an actor or spawn an actor.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F31A/F45A PlayerActor passive descriptor.")]
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
            : this(
                actorId,
                ActorRole.Protagonist,
                hasPlayerInputEvidence,
                displayName,
                sceneName,
                objectName,
                source,
                reason)
        {
        }

        public PlayerActorDescriptor(
            ActorId actorId,
            ActorRole actorRole,
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

            if (!Enum.IsDefined(typeof(ActorRole), actorRole))
            {
                throw new ArgumentOutOfRangeException(nameof(actorRole), actorRole, "Actor role must be defined.");
            }

            ActorId = actorId;
            ActorRole = actorRole;
            HasPlayerInputEvidence = hasPlayerInputEvidence;
            DisplayName = displayName.NormalizeTextOrFallback(actorId.StableText);
            SceneName = sceneName.NormalizeText();
            ObjectName = objectName.NormalizeText();
            Source = source.NormalizeTextOrFallback(nameof(PlayerActorDescriptor));
            Reason = reason.NormalizeText();
        }

        public ActorId ActorId { get; }

        public ActorKind ActorKind => ActorKind.Player;

        public ActorRole ActorRole { get; }

        public bool HasPlayerInputEvidence { get; }

        public string DisplayName { get; }

        public string SceneName { get; }

        public string ObjectName { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool SwitchesActionMaps => false;

        public bool AppliesInputBehavior => false;

        public bool SpawnsActor => false;

        public ActorDescriptor ToActorDescriptor()
        {
            return new ActorDescriptor(
                ActorId,
                ActorKind,
                ActorRole,
                DisplayName,
                SceneName,
                ObjectName,
                Source,
                Reason);
        }

        public bool Equals(PlayerActorDescriptor other)
        {
            return ActorId.Equals(other.ActorId)
                && ActorRole == other.ActorRole
                && HasPlayerInputEvidence == other.HasPlayerInputEvidence;
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
                hash = (hash * 397) ^ (int)ActorRole;
                hash = (hash * 397) ^ HasPlayerInputEvidence.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return ActorId.StableText;
        }
    }
}
