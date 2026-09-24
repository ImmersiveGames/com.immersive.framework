using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Stable identity for one logical Progression Save runtime request.
    /// This is not a slot id, file path, save filename, UI button name or backend key.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21G Progression Save request identity primitive; runtime request path only.")]
    public readonly struct ProgressionSaveRequestId : IFrameworkIdentity, IEquatable<ProgressionSaveRequestId>
    {
        private readonly FrameworkIdentityValue _value;

        public ProgressionSaveRequestId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public ProgressionSaveRequestId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Progression Save request id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.ProgressionSave;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(ProgressionSaveRequestId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressionSaveRequestId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static ProgressionSaveRequestId From(string value)
        {
            return new ProgressionSaveRequestId(value);
        }

        public static bool operator ==(ProgressionSaveRequestId left, ProgressionSaveRequestId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProgressionSaveRequestId left, ProgressionSaveRequestId right)
        {
            return !left.Equals(right);
        }
    }
}
