using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.Actors
{
    /// <summary>Seletor Experimental para uma ocorrência atual de Actor ou uma encarnação exata.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-049 read-only Actor occurrence selector.")]
    public readonly struct ActorOccurrenceReference : IEquatable<ActorOccurrenceReference>
    {
        private ActorOccurrenceReference(
            ActorOccurrenceReferenceKind kind,
            PlayerSlotId playerSlotId,
            ActorId actorId,
            RuntimeContentOwner owner,
            bool hasOwner,
            ActorOccurrenceId expectedOccurrence)
        {
            Kind = kind;
            PlayerSlotId = playerSlotId;
            ActorId = actorId;
            Owner = owner;
            HasOwner = hasOwner;
            ExpectedOccurrence = expectedOccurrence;
        }

        public ActorOccurrenceReferenceKind Kind { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public ActorId ActorId { get; }
        public RuntimeContentOwner Owner { get; }
        public bool HasOwner { get; }
        public ActorOccurrenceId ExpectedOccurrence { get; }
        public bool IsExactOccurrence => ExpectedOccurrence.IsValid;
        public bool IsValid => Kind == ActorOccurrenceReferenceKind.PlayerSlot
            ? PlayerSlotId.IsValid
            : Kind == ActorOccurrenceReferenceKind.ActorId && ActorId.IsValid && (!HasOwner || Owner.IsValid);

        public static ActorOccurrenceReference ForPlayerSlot(PlayerSlotId playerSlotId)
        {
            return new ActorOccurrenceReference(
                ActorOccurrenceReferenceKind.PlayerSlot, playerSlotId, default, default, false, default);
        }

        public static ActorOccurrenceReference ForPlayerSlot(PlayerSlotProfile profile)
        {
            if (profile == null)
                throw new ArgumentException("Player Slot Profile is required.", nameof(profile));
            if (!profile.TryGetPlayerSlotId(out PlayerSlotId slot, out string issue))
                throw new ArgumentException(issue, nameof(profile));
            return ForPlayerSlot(slot);
        }

        public static ActorOccurrenceReference ForActorId(ActorId actorId)
        {
            return new ActorOccurrenceReference(
                ActorOccurrenceReferenceKind.ActorId, default, actorId, default, false, default);
        }

        public static ActorOccurrenceReference ForActorId(ActorId actorId, RuntimeContentOwner owner)
        {
            return new ActorOccurrenceReference(
                ActorOccurrenceReferenceKind.ActorId, default, actorId, owner, true, default);
        }

        /// <summary>Cria um seletor para a encarnação exata representada pela evidência.</summary>
        public static ActorOccurrenceReference ForOccurrence(ActorOccurrence occurrence)
        {
            if (!occurrence.IsValid)
                throw new ArgumentException("A valid Actor occurrence is required.", nameof(occurrence));
            return new ActorOccurrenceReference(
                ActorOccurrenceReferenceKind.ActorId,
                occurrence.HasPlayerSlot ? occurrence.PlayerSlotId : default,
                occurrence.ActorId,
                occurrence.Owner,
                true,
                occurrence.Id);
        }

        internal ActorOccurrenceReference WithOwner(RuntimeContentOwner owner)
        {
            return new ActorOccurrenceReference(Kind, PlayerSlotId, ActorId, owner, true, ExpectedOccurrence);
        }

        public bool Equals(ActorOccurrenceReference other) =>
            Kind == other.Kind && PlayerSlotId == other.PlayerSlotId && ActorId == other.ActorId &&
            Owner == other.Owner && HasOwner == other.HasOwner && ExpectedOccurrence == other.ExpectedOccurrence;
        public override bool Equals(object obj) => obj is ActorOccurrenceReference other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = hash * 397 ^ PlayerSlotId.GetHashCode();
                hash = hash * 397 ^ ActorId.GetHashCode();
                hash = hash * 397 ^ Owner.GetHashCode();
                hash = hash * 397 ^ HasOwner.GetHashCode();
                return hash * 397 ^ ExpectedOccurrence.GetHashCode();
            }
        }
    }

    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-049 Actor reference selector kind.")]
    public enum ActorOccurrenceReferenceKind
    {
        Invalid = 0,
        PlayerSlot = 10,
        ActorId = 20
    }
}
