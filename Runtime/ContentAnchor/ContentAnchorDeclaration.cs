using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.Common;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Passive declaration for one authored Content Anchor.
    /// A declaration carries identity and authoring intent only; it does not discover,
    /// register, validate, materialize, bind or move runtime content.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Content Anchor declaration model introduced by F7C.")]
    public readonly struct ContentAnchorDeclaration : IEquatable<ContentAnchorDeclaration>
    {
        public ContentAnchorDeclaration(
            FrameworkIdentityKey owner,
            ContentAnchorScope scope,
            ContentAnchorKind kind,
            ContentAnchorId anchorId,
            ContentAnchorRequiredness requiredness,
            string displayName,
            string description,
            string resourceName,
            string resourcePath)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Content Anchor declaration owner must be explicit and valid.", nameof(owner));
            }

            FrameworkEnumValidation.ThrowIfUndefinedOr(scope, ContentAnchorScope.Unknown, nameof(scope), "Content Anchor declaration scope must be explicit.");

            FrameworkEnumValidation.ThrowIfUndefinedOr(kind, ContentAnchorKind.Unknown, nameof(kind), "Content Anchor declaration kind must be explicit.");

            if (!anchorId.IsValid)
            {
                throw new ArgumentException("Content Anchor declaration id must be explicit and valid.", nameof(anchorId));
            }

            FrameworkEnumValidation.ThrowIfUndefined(requiredness, nameof(requiredness), "Content Anchor declaration requiredness must be defined.");

            Owner = owner;
            Scope = scope;
            Kind = kind;
            AnchorId = anchorId;
            Requiredness = requiredness;
            DisplayName = Normalize(displayName);
            Description = Normalize(description);
            ResourceName = Normalize(resourceName);
            ResourcePath = Normalize(resourcePath);
        }

        public FrameworkIdentityKey Owner { get; }

        public ContentAnchorScope Scope { get; }

        public ContentAnchorKind Kind { get; }

        public ContentAnchorId AnchorId { get; }

        public ContentAnchorRequiredness Requiredness { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public string ResourceName { get; }

        public string ResourcePath { get; }

        public bool IsValid => Owner.IsValid
            && FrameworkEnumValidation.IsDefinedAndNot(Scope, ContentAnchorScope.Unknown)
            && FrameworkEnumValidation.IsDefinedAndNot(Kind, ContentAnchorKind.Unknown)
            && AnchorId.IsValid;

        public bool IsRequired => Requiredness == ContentAnchorRequiredness.Required;

        public bool IsOptional => Requiredness == ContentAnchorRequiredness.Optional;

        public bool HasDisplayName => !string.IsNullOrWhiteSpace(DisplayName);

        public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

        public bool HasResource => !string.IsNullOrWhiteSpace(ResourceName) || !string.IsNullOrWhiteSpace(ResourcePath);

        public string StableText => $"ContentAnchor:{Scope}:{Kind}:{Owner.StableText}:{AnchorId.StableText}";

        public bool Equals(ContentAnchorDeclaration other)
        {
            return Owner.Equals(other.Owner)
                && Scope == other.Scope
                && Kind == other.Kind
                && AnchorId.Equals(other.AnchorId)
                && Requiredness == other.Requiredness
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && string.Equals(Description, other.Description, StringComparison.Ordinal)
                && string.Equals(ResourceName, other.ResourceName, StringComparison.Ordinal)
                && string.Equals(ResourcePath, other.ResourcePath, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ContentAnchorDeclaration other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Owner.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Scope;
                hashCode = hashCode * 397 ^ (int)Kind;
                hashCode = hashCode * 397 ^ AnchorId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Requiredness;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(DisplayName ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Description ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(ResourceName ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(ResourcePath ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return StableText;
        }

        public string ToDiagnosticString()
        {
            string resource = !string.IsNullOrWhiteSpace(ResourceName) ? ResourceName : ResourcePath;
            if (string.IsNullOrWhiteSpace(resource))
            {
                resource = "<none>";
            }

            string label = DisplayName.ToDiagnosticText();
            return $"anchor='{StableText}' id='{AnchorId.StableText}' scope='{Scope}' kind='{Kind}' requiredness='{Requiredness}' owner='{Owner.StableText}' label='{label}' resource='{resource}'";
        }

        public static ContentAnchorDeclaration Root(
            FrameworkIdentityKey owner,
            ContentAnchorScope scope,
            string anchorId,
            ContentAnchorRequiredness requiredness,
            string displayName,
            string description,
            string resourceName,
            string resourcePath)
        {
            return Create(owner, scope, ContentAnchorKind.Root, anchorId, requiredness, displayName, description, resourceName, resourcePath);
        }

        public static ContentAnchorDeclaration Slot(
            FrameworkIdentityKey owner,
            ContentAnchorScope scope,
            string anchorId,
            ContentAnchorRequiredness requiredness,
            string displayName,
            string description,
            string resourceName,
            string resourcePath)
        {
            return Create(owner, scope, ContentAnchorKind.Slot, anchorId, requiredness, displayName, description, resourceName, resourcePath);
        }

        public static ContentAnchorDeclaration Point(
            FrameworkIdentityKey owner,
            ContentAnchorScope scope,
            string anchorId,
            ContentAnchorRequiredness requiredness,
            string displayName,
            string description,
            string resourceName,
            string resourcePath)
        {
            return Create(owner, scope, ContentAnchorKind.Point, anchorId, requiredness, displayName, description, resourceName, resourcePath);
        }

        public static ContentAnchorDeclaration Create(
            FrameworkIdentityKey owner,
            ContentAnchorScope scope,
            ContentAnchorKind kind,
            string anchorId,
            ContentAnchorRequiredness requiredness,
            string displayName,
            string description,
            string resourceName,
            string resourcePath)
        {
            return new ContentAnchorDeclaration(
                owner,
                scope,
                kind,
                ContentAnchorId.From(anchorId),
                requiredness,
                displayName,
                description,
                resourceName,
                resourcePath);
        }

        public static FrameworkIdentityKey CreateOwnerKey(ContentAnchorScope scope, string ownerId)
        {
            return FrameworkIdentityKey.From(GetOwnerDomain(scope), ownerId);
        }

        public static FrameworkIdentityDomain GetOwnerDomain(ContentAnchorScope scope)
        {
            switch (scope)
            {
                case ContentAnchorScope.Route:
                    return FrameworkIdentityDomain.Route;
                case ContentAnchorScope.Activity:
                    return FrameworkIdentityDomain.Activity;
                case ContentAnchorScope.Local:
                    return FrameworkIdentityDomain.Local;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), scope, "Content Anchor declaration owner domain cannot be inferred for an unknown scope.");
            }
        }

        public static bool operator ==(ContentAnchorDeclaration left, ContentAnchorDeclaration right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ContentAnchorDeclaration left, ContentAnchorDeclaration right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
