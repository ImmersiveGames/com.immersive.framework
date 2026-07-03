using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// API status: Experimental. Stable identity for a logical object entry known by the framework.
    /// This is not a Unity GameObject name, hierarchy path, prefab name, Player id or Actor id.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Object Entry identity primitive introduced by F13A.")]
    public readonly struct ObjectEntryId : IFrameworkIdentity, IEquatable<ObjectEntryId>
    {
        private readonly FrameworkIdentityValue _value;

        public ObjectEntryId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public ObjectEntryId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Object entry id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.ObjectEntry;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(ObjectEntryId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectEntryId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static ObjectEntryId From(string value)
        {
            return new ObjectEntryId(value);
        }

        public static bool operator ==(ObjectEntryId left, ObjectEntryId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ObjectEntryId left, ObjectEntryId right)
        {
            return !left.Equals(right);
        }
    }
}
