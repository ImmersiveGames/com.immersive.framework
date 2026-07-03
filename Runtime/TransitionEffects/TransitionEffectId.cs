using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.TransitionEffects
{
    /// <summary>
    /// API status: Experimental. Stable identity for one logical Transition Effect request or adapter-facing effect.
    /// This is not a scene name, GameObject name, Canvas name, prefab path, DOTween id or visual object reference.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F19B Transition Effect identity primitive; passive effect identity only.")]
    public readonly struct TransitionEffectId : IFrameworkIdentity, IEquatable<TransitionEffectId>
    {
        private readonly FrameworkIdentityValue _value;

        public TransitionEffectId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public TransitionEffectId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Transition Effect id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.TransitionEffect;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(TransitionEffectId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionEffectId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static TransitionEffectId From(string value)
        {
            return new TransitionEffectId(value);
        }

        public static bool operator ==(TransitionEffectId left, TransitionEffectId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TransitionEffectId left, TransitionEffectId right)
        {
            return !left.Equals(right);
        }
    }
}
