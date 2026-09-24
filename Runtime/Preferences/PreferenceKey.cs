using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Preferences
{
    /// <summary>
    /// API status: Experimental. Stable identity for one user/application preference.
    /// This is not a PlayerPrefs physical key, progression slot id, Snapshot schema id or JSON path.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21D Preferences key primitive; storage-agnostic.")]
    public readonly struct PreferenceKey : IFrameworkIdentity, IEquatable<PreferenceKey>
    {
        private readonly FrameworkIdentityValue _value;

        public PreferenceKey(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public PreferenceKey(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Preference key value must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Preferences;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(PreferenceKey other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is PreferenceKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static PreferenceKey From(string value)
        {
            return new PreferenceKey(value);
        }

        public static bool operator ==(PreferenceKey left, PreferenceKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PreferenceKey left, PreferenceKey right)
        {
            return !left.Equals(right);
        }
    }
}
