using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Stable identity for a logical Progression Save moment.
    /// A moment describes why a request is made; it does not schedule, trigger or own autosave execution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21G Progression Save moment identity primitive; no autosave scheduler.")]
    public readonly struct ProgressionSaveMomentId : IFrameworkIdentity, IEquatable<ProgressionSaveMomentId>
    {
        private readonly FrameworkIdentityValue _value;

        public ProgressionSaveMomentId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public ProgressionSaveMomentId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Progression Save moment id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.ProgressionSave;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(ProgressionSaveMomentId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressionSaveMomentId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static ProgressionSaveMomentId From(string value)
        {
            return new ProgressionSaveMomentId(value);
        }

        public static bool operator ==(ProgressionSaveMomentId left, ProgressionSaveMomentId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProgressionSaveMomentId left, ProgressionSaveMomentId right)
        {
            return !left.Equals(right);
        }
    }
}
