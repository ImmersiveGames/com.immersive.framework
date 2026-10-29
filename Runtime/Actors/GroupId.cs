using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Actors
{
    /// <summary>
    /// API status: Experimental. Logical identity of one collective Actor Group.
    /// It is independent of members, profile, Player Slots and physical occurrences.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "IF-ADR-051 logical Group identity.")]
    public readonly struct GroupId : IFrameworkIdentity, IEquatable<GroupId>
    {
        private readonly FrameworkIdentityValue _value;

        public GroupId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public GroupId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Group id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Group;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(GroupId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is GroupId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static GroupId From(string value)
        {
            return new GroupId(value);
        }

        public static bool operator ==(GroupId left, GroupId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GroupId left, GroupId right)
        {
            return !left.Equals(right);
        }
    }
}
