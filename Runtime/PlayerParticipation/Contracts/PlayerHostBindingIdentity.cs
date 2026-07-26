using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Opaque Session-scoped domain identity for one Local Player Host binding.
    /// It is never derived from a Unity object, hierarchy, name or PlayerInput index.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "CPSA-1 typed Local Player Host binding identity.")]
    public readonly struct PlayerHostBindingIdentity :
        IEquatable<PlayerHostBindingIdentity>
    {
        internal PlayerHostBindingIdentity(string sessionContextId, int sequence)
        {
            SessionContextId = sessionContextId.NormalizeText();
            Sequence = sequence;
        }

        internal string SessionContextId { get; }

        public int Sequence { get; }

        public bool IsValid =>
            !string.IsNullOrEmpty(SessionContextId) &&
            Sequence > 0;

        public string StableText => IsValid
            ? $"player-host-binding:{SessionContextId}:{Sequence}"
            : string.Empty;

        public bool Equals(PlayerHostBindingIdentity other)
        {
            return string.Equals(
                    SessionContextId,
                    other.SessionContextId,
                    StringComparison.Ordinal) &&
                Sequence == other.Sequence;
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerHostBindingIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return StringComparer.Ordinal.GetHashCode(
                           SessionContextId ?? string.Empty) * 397 ^
                    Sequence;
            }
        }

        public override string ToString() => StableText;

        public static bool operator ==(
            PlayerHostBindingIdentity left,
            PlayerHostBindingIdentity right) => left.Equals(right);

        public static bool operator !=(
            PlayerHostBindingIdentity left,
            PlayerHostBindingIdentity right) => !left.Equals(right);
    }
}
