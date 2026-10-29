using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Identity of one reusable, authored Group Profile.
    /// This is not a GroupId and does not identify a live Group.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-051 reusable Group Profile identity.")]
    public readonly struct GroupProfileId : IFrameworkIdentity, IEquatable<GroupProfileId>
    {
        private readonly FrameworkIdentityValue _value;

        public GroupProfileId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public GroupProfileId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Group Profile id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.GroupProfile;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(GroupProfileId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is GroupProfileId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static GroupProfileId From(string value)
        {
            return new GroupProfileId(value);
        }

        public static bool operator ==(GroupProfileId left, GroupProfileId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GroupProfileId left, GroupProfileId right)
        {
            return !left.Equals(right);
        }
    }
}
