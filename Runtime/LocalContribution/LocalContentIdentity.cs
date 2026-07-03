using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.Identity;

namespace Immersive.Framework.LocalContribution
{
    /// <summary>
    /// API status: Experimental. Functional identity for one local content contribution inside a known content owner.
    /// This identity is explicit and deterministic; it must not be derived from GameObject names, scene names or hierarchy paths.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Local content identity introduced by F5B.")]
    public readonly struct LocalContentIdentity : IEquatable<LocalContentIdentity>
    {
        public LocalContentIdentity(
            FrameworkContentScope contentScope,
            FrameworkIdentityKey scopeOwner,
            LocalContentScopeKind localScopeKind,
            LocalContentId localId)
        {
            if (!IsSupportedContentScope(contentScope))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(contentScope),
                    contentScope,
                    "Local content identity currently supports Session, Route and Activity scopes only.");
            }

            if (!scopeOwner.IsValid)
            {
                throw new ArgumentException("Local content owner identity must be valid.", nameof(scopeOwner));
            }

            var expectedOwnerDomain = GetExpectedOwnerDomain(contentScope);
            if (scopeOwner.Domain != expectedOwnerDomain)
            {
                throw new ArgumentException(
                    $"Local content owner domain '{scopeOwner.Domain}' does not match content scope '{contentScope}'. Expected '{expectedOwnerDomain}'.",
                    nameof(scopeOwner));
            }

            if (localScopeKind == LocalContentScopeKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(localScopeKind), localScopeKind, "Local content scope kind must be explicit.");
            }

            if (!localId.IsValid)
            {
                throw new ArgumentException("Local content id must be valid.", nameof(localId));
            }

            ContentScope = contentScope;
            ScopeOwner = scopeOwner;
            LocalScopeKind = localScopeKind;
            LocalId = localId;
        }

        public FrameworkContentScope ContentScope { get; }

        public FrameworkIdentityKey ScopeOwner { get; }

        public LocalContentScopeKind LocalScopeKind { get; }

        public LocalContentId LocalId { get; }

        public bool IsValid => IsSupportedContentScope(ContentScope)
            && ScopeOwner.IsValid
            && ScopeOwner.Domain == GetExpectedOwnerDomain(ContentScope)
            && LocalScopeKind != LocalContentScopeKind.Unknown
            && LocalId.IsValid;

        public string StableText => $"local:{ContentScope}:{ScopeOwner.Value.Value}:{LocalScopeKind}:{LocalId.StableText}";

        public bool Equals(LocalContentIdentity other)
        {
            return ContentScope == other.ContentScope
                && ScopeOwner.Equals(other.ScopeOwner)
                && LocalScopeKind == other.LocalScopeKind
                && LocalId.Equals(other.LocalId);
        }

        public override bool Equals(object obj)
        {
            return obj is LocalContentIdentity other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)ContentScope;
                hashCode = hashCode * 397 ^ ScopeOwner.GetHashCode();
                hashCode = hashCode * 397 ^ (int)LocalScopeKind;
                hashCode = hashCode * 397 ^ LocalId.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return StableText;
        }

        public static LocalContentIdentity From(
            FrameworkContentScope contentScope,
            FrameworkIdentityKey scopeOwner,
            LocalContentScopeKind localScopeKind,
            string localId)
        {
            return new LocalContentIdentity(
                contentScope,
                scopeOwner,
                localScopeKind,
                new LocalContentId(localId));
        }

        public static LocalContentIdentity FromOwnerValue(
            FrameworkContentScope contentScope,
            string ownerId,
            LocalContentScopeKind localScopeKind,
            string localId)
        {
            return From(
                contentScope,
                FrameworkIdentityKey.From(GetExpectedOwnerDomain(contentScope), ownerId),
                localScopeKind,
                localId);
        }

        public static bool IsSupportedContentScope(FrameworkContentScope contentScope)
        {
            return contentScope is FrameworkContentScope.Session or FrameworkContentScope.Route or FrameworkContentScope.Activity;
        }

        public static FrameworkIdentityDomain GetExpectedOwnerDomain(FrameworkContentScope contentScope)
        {
            switch (contentScope)
            {
                case FrameworkContentScope.Session:
                    return FrameworkIdentityDomain.Session;
                case FrameworkContentScope.Route:
                    return FrameworkIdentityDomain.Route;
                case FrameworkContentScope.Activity:
                    return FrameworkIdentityDomain.Activity;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(contentScope),
                        contentScope,
                        "Local content identity owner domain can only be inferred for Session, Route and Activity scopes.");
            }
        }

        public static bool operator ==(LocalContentIdentity left, LocalContentIdentity right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LocalContentIdentity left, LocalContentIdentity right)
        {
            return !left.Equals(right);
        }
    }
}
