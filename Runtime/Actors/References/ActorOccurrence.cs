using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.Actors
{
    /// <summary>Evidência imutável de uma raiz física de Actor publicada e atual.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-049 current physical Actor occurrence evidence.")]
    public readonly struct ActorOccurrence
    {
        internal ActorOccurrence(
            ActorOccurrenceId id,
            ActorId actorId,
            GameObject actorRoot,
            RuntimeContentOwner owner,
            PlayerSlotId playerSlotId)
        {
            Id = id;
            ActorId = actorId;
            ActorRoot = actorRoot;
            Owner = owner;
            PlayerSlotId = playerSlotId;
        }

        public ActorOccurrenceId Id { get; }
        public ActorId ActorId { get; }
        public GameObject ActorRoot { get; }
        public RuntimeContentOwner Owner { get; }
        public PlayerSlotId PlayerSlotId { get; }
        public bool HasPlayerSlot => PlayerSlotId.IsValid;
        public bool IsValid => Id.IsValid && ActorId.IsValid && ActorRoot != null && Owner.IsValid;
    }

    /// <summary>Identidade local ao Host que distingue encarnações de Actor.</summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-049 Actor incarnation identity.")]
    public readonly struct ActorOccurrenceId : IEquatable<ActorOccurrenceId>
    {
        internal ActorOccurrenceId(long value) => Value = value;
        public long Value { get; }
        public bool IsValid => Value > 0;
        public bool Equals(ActorOccurrenceId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ActorOccurrenceId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => IsValid ? $"actor-occurrence:{Value}" : string.Empty;
        public static bool operator ==(ActorOccurrenceId left, ActorOccurrenceId right) => left.Equals(right);
        public static bool operator !=(ActorOccurrenceId left, ActorOccurrenceId right) => !left.Equals(right);
    }

    internal sealed class ActorOccurrenceIdSequence
    {
        private long _sequence;

        internal ActorOccurrenceId Next()
        {
            if (_sequence == long.MaxValue)
                throw new InvalidOperationException("Actor occurrence sequence exhausted for this Framework host.");
            return new ActorOccurrenceId(++_sequence);
        }
    }
}
