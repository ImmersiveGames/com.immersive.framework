using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Snapshot
{
    /// <summary>
    /// API status: Experimental. Stable identity for the schema used to interpret one Snapshot payload.
    /// This is not a C# type name, file extension, JSON contract name or backend-specific discriminator.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21B Snapshot schema identity primitive; backend-agnostic.")]
    public readonly struct SnapshotSchemaId : IFrameworkIdentity, IEquatable<SnapshotSchemaId>
    {
        private readonly FrameworkIdentityValue _value;

        public SnapshotSchemaId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public SnapshotSchemaId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Snapshot schema id must be valid.", nameof(value));
            }

            _value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Snapshot;

        public FrameworkIdentityValue Value => _value;

        public bool IsValid => _value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, _value);

        public string StableText => Key.StableText;

        public bool Equals(SnapshotSchemaId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is SnapshotSchemaId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static SnapshotSchemaId From(string value)
        {
            return new SnapshotSchemaId(value);
        }

        public static bool operator ==(SnapshotSchemaId left, SnapshotSchemaId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SnapshotSchemaId left, SnapshotSchemaId right)
        {
            return !left.Equals(right);
        }
    }
}
