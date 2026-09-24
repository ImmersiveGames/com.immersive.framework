using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Snapshot
{
    /// <summary>
    /// API status: Experimental. Semantic version for interpreting a Snapshot schema payload.
    /// This is not package version, save slot version or backend migration policy.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21B Snapshot schema version primitive; no migration runtime.")]
    public readonly struct SnapshotSchemaVersion : IEquatable<SnapshotSchemaVersion>
    {
        public SnapshotSchemaVersion(int major, int minor, int patch)
        {
            if (major <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(major), major, "Snapshot schema major version must be greater than zero.");
            }

            if (minor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minor), minor, "Snapshot schema minor version cannot be negative.");
            }

            if (patch < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(patch), patch, "Snapshot schema patch version cannot be negative.");
            }

            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public int Major { get; }

        public int Minor { get; }

        public int Patch { get; }

        public bool IsValid => Major > 0 && Minor >= 0 && Patch >= 0;

        public string StableText => $"{Major}.{Minor}.{Patch}";

        public bool Equals(SnapshotSchemaVersion other)
        {
            return Major == other.Major && Minor == other.Minor && Patch == other.Patch;
        }

        public override bool Equals(object obj)
        {
            return obj is SnapshotSchemaVersion other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Major;
                hashCode = hashCode * 397 ^ Minor;
                hashCode = hashCode * 397 ^ Patch;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return StableText;
        }

        public static SnapshotSchemaVersion Initial()
        {
            return new SnapshotSchemaVersion(1, 0, 0);
        }

        public static SnapshotSchemaVersion FromMajorMinor(int major, int minor)
        {
            return new SnapshotSchemaVersion(major, minor, 0);
        }

        public static bool operator ==(SnapshotSchemaVersion left, SnapshotSchemaVersion right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SnapshotSchemaVersion left, SnapshotSchemaVersion right)
        {
            return !left.Equals(right);
        }
    }
}
