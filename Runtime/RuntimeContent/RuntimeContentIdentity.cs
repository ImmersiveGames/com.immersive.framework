using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Functional identity for one runtime-created content record.
    /// It combines owner scope, owner identity and runtime content id without referencing a Unity object.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8B runtime content identity primitive; no handle or materializer behavior yet.")]
    public readonly struct RuntimeContentIdentity : IEquatable<RuntimeContentIdentity>
    {
        public RuntimeContentIdentity(RuntimeContentOwner owner, RuntimeContentId contentId)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Runtime content owner must be valid.", nameof(owner));
            }

            if (!contentId.IsValid)
            {
                throw new ArgumentException("Runtime content id must be valid.", nameof(contentId));
            }

            Owner = owner;
            ContentId = contentId;
        }

        public RuntimeContentOwner Owner { get; }

        public RuntimeContentScope Scope => Owner.Scope;

        public RuntimeContentId ContentId { get; }

        public bool IsValid => Owner.IsValid && ContentId.IsValid;

        public string StableText => $"{Owner.StableText}:{ContentId.StableText}";

        public bool Equals(RuntimeContentIdentity other)
        {
            return Owner.Equals(other.Owner) && ContentId.Equals(other.ContentId);
        }

        public override bool Equals(object obj)
        {
            return obj is RuntimeContentIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Owner.GetHashCode() * 397 ^ ContentId.GetHashCode();
            }
        }

        public override string ToString()
        {
            return StableText;
        }

        public static RuntimeContentIdentity From(RuntimeContentOwner owner, string contentId)
        {
            return new RuntimeContentIdentity(owner, new RuntimeContentId(contentId));
        }

        public static bool operator ==(RuntimeContentIdentity left, RuntimeContentIdentity right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RuntimeContentIdentity left, RuntimeContentIdentity right)
        {
            return !left.Equals(right);
        }
    }
}
