using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.PlayerSlots
{
    /// <summary>
    /// API status: Experimental. Stable logical identity for a player participation seat.
    /// This is not a PlayerInput index, device id, ActorId, save slot id or camera channel id.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F45C1 PlayerSlot identity primitive.")]
    public readonly struct PlayerSlotId : IFrameworkIdentity, IEquatable<PlayerSlotId>
    {
        private readonly FrameworkIdentityValue _value;

        public PlayerSlotId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public PlayerSlotId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("PlayerSlot id must be valid.", nameof(value));
            }

            _value = value;
        }

        public static PlayerSlotId Player1 => new PlayerSlotId("player.1");

        public static PlayerSlotId Player2 => new PlayerSlotId("player.2");

        public static PlayerSlotId Player3 => new PlayerSlotId("player.3");

        public static PlayerSlotId Player4 => new PlayerSlotId("player.4");

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.PlayerSlot;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(PlayerSlotId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerSlotId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static PlayerSlotId From(string value)
        {
            return new PlayerSlotId(value);
        }

        public static bool operator ==(PlayerSlotId left, PlayerSlotId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PlayerSlotId left, PlayerSlotId right)
        {
            return !left.Equals(right);
        }
    }
}
