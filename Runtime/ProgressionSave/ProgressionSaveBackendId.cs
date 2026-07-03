using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Identity for the store/backend adapter behind the Progression Save port.
    /// It identifies the adapter, not a file path, provider implementation type or storage location.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21E Progression Save backend port identity primitive; backend-agnostic.")]
    public readonly struct ProgressionSaveBackendId : IFrameworkIdentity, IEquatable<ProgressionSaveBackendId>
    {
        private readonly FrameworkIdentityValue _value;

        public ProgressionSaveBackendId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public ProgressionSaveBackendId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Progression Save backend id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.ProgressionSave;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(ProgressionSaveBackendId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressionSaveBackendId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static ProgressionSaveBackendId From(string value)
        {
            return new ProgressionSaveBackendId(value);
        }

        public static bool operator ==(ProgressionSaveBackendId left, ProgressionSaveBackendId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProgressionSaveBackendId left, ProgressionSaveBackendId right)
        {
            return !left.Equals(right);
        }
    }
}
