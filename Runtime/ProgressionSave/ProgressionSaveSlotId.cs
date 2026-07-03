using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Stable logical slot identity for Progression Save.
    /// This is not a file path, JSON property, PlayerPrefs key, Snapshot envelope id or UI label.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21E Progression Save slot identity primitive; backend-agnostic.")]
    public readonly struct ProgressionSaveSlotId : IFrameworkIdentity, IEquatable<ProgressionSaveSlotId>
    {
        private readonly FrameworkIdentityValue _value;

        public ProgressionSaveSlotId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public ProgressionSaveSlotId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Progression Save slot id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.ProgressionSave;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(ProgressionSaveSlotId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressionSaveSlotId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static ProgressionSaveSlotId From(string value)
        {
            return new ProgressionSaveSlotId(value);
        }

        public static bool operator ==(ProgressionSaveSlotId left, ProgressionSaveSlotId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProgressionSaveSlotId left, ProgressionSaveSlotId right)
        {
            return !left.Equals(right);
        }
    }
}
