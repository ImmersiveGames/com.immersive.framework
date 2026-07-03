using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// API status: Experimental. Functional identity for one content handle.
    /// The identity is composed from owner, lifecycle scope, content kind and content id.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Minimal content identity introduced by F1F.")]
    public readonly struct FrameworkContentIdentity : IEquatable<FrameworkContentIdentity>
    {
        public FrameworkContentIdentity(
            FrameworkIdentityKey owner,
            FrameworkContentScope scope,
            FrameworkContentKind kind,
            FrameworkContentId contentId)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Content owner identity must be valid.", nameof(owner));
            }

            if (scope == FrameworkContentScope.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Content scope must be explicit.");
            }

            if (kind == FrameworkContentKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Content kind must be explicit.");
            }

            if (!contentId.IsValid)
            {
                throw new ArgumentException("Content id must be valid.", nameof(contentId));
            }

            Owner = owner;
            Scope = scope;
            Kind = kind;
            ContentId = contentId;
        }

        public FrameworkIdentityKey Owner { get; }

        public FrameworkContentScope Scope { get; }

        public FrameworkContentKind Kind { get; }

        public FrameworkContentId ContentId { get; }

        public bool IsValid => Owner.IsValid
            && Scope != FrameworkContentScope.Unknown
            && Kind != FrameworkContentKind.Unknown
            && ContentId.IsValid;

        public string StableText => $"{Scope}:{Kind}:{Owner.StableText}:{ContentId.StableText}";

        public bool Equals(FrameworkContentIdentity other)
        {
            return Owner.Equals(other.Owner)
                && Scope == other.Scope
                && Kind == other.Kind
                && ContentId.Equals(other.ContentId);
        }

        public override bool Equals(object obj)
        {
            return obj is FrameworkContentIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Owner.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Scope;
                hashCode = hashCode * 397 ^ (int)Kind;
                hashCode = hashCode * 397 ^ ContentId.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return StableText;
        }

        public static FrameworkContentIdentity From(
            FrameworkIdentityKey owner,
            FrameworkContentScope scope,
            FrameworkContentKind kind,
            string contentId)
        {
            return new FrameworkContentIdentity(owner, scope, kind, new FrameworkContentId(contentId));
        }

        public static FrameworkContentIdentity FromOwnerValue(
            FrameworkContentScope scope,
            FrameworkContentKind kind,
            string ownerId,
            string contentId)
        {
            return From(CreateOwnerKey(scope, ownerId), scope, kind, contentId);
        }

        public static FrameworkIdentityKey CreateOwnerKey(FrameworkContentScope scope, string ownerId)
        {
            return FrameworkIdentityKey.From(GetOwnerDomain(scope), ownerId);
        }

        public static FrameworkIdentityDomain GetOwnerDomain(FrameworkContentScope scope)
        {
            switch (scope)
            {
                case FrameworkContentScope.Session:
                    return FrameworkIdentityDomain.Session;
                case FrameworkContentScope.Route:
                    return FrameworkIdentityDomain.Route;
                case FrameworkContentScope.Activity:
                    return FrameworkIdentityDomain.Activity;
                case FrameworkContentScope.ContentAnchor:
                    return FrameworkIdentityDomain.ContentAnchor;
                case FrameworkContentScope.RuntimeSpawned:
                    return FrameworkIdentityDomain.Runtime;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), scope, "Content owner domain cannot be inferred for an unknown content scope.");
            }
        }

        public static bool operator ==(FrameworkContentIdentity left, FrameworkContentIdentity right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FrameworkContentIdentity left, FrameworkContentIdentity right)
        {
            return !left.Equals(right);
        }
    }
}
