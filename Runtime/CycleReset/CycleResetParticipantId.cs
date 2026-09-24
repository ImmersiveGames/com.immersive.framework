using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Functional identity for one Cycle Reset participant.
    /// This must not be derived from GameObject names, scene paths or hierarchy paths.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11A typed Cycle Reset participant identity; no GameObject-name fallback.")]
    public readonly struct CycleResetParticipantId : IFrameworkIdentity, IEquatable<CycleResetParticipantId>
    {
        private readonly FrameworkIdentityValue _value;

        public CycleResetParticipantId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public CycleResetParticipantId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Cycle Reset participant id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.CycleReset;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public string StableText => _value.Value;

        public bool Equals(CycleResetParticipantId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is CycleResetParticipantId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static CycleResetParticipantId From(string value)
        {
            return new CycleResetParticipantId(value);
        }

        public static bool operator ==(CycleResetParticipantId left, CycleResetParticipantId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CycleResetParticipantId left, CycleResetParticipantId right)
        {
            return !left.Equals(right);
        }
    }
}
