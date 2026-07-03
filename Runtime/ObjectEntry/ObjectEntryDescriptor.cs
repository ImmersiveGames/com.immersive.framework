using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.Common;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// API status: Experimental. Passive descriptor for a logical object entry.
    /// It identifies the object entry and its lifecycle scope without creating, discovering or resetting anything.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Object Entry descriptor introduced by F13A.")]
    public readonly struct ObjectEntryDescriptor : IEquatable<ObjectEntryDescriptor>
    {
        public ObjectEntryDescriptor(
            ObjectEntryId id,
            ObjectEntryScope scope,
            ObjectEntrySourceKind sourceKind,
            ObjectEntryRequiredness requiredness,
            string displayName = null,
            FrameworkIdentityKey? ownerIdentity = null)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Object entry descriptor requires a valid id.", nameof(id));
            }

            if (!Enum.IsDefined(typeof(ObjectEntryScope), scope) || scope == ObjectEntryScope.Unspecified)
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Object entry scope must be explicit.");
            }

            if (!Enum.IsDefined(typeof(ObjectEntrySourceKind), sourceKind) || sourceKind == ObjectEntrySourceKind.Unspecified)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, "Object entry source kind must be explicit.");
            }

            if (!Enum.IsDefined(typeof(ObjectEntryRequiredness), requiredness) || requiredness == ObjectEntryRequiredness.Unspecified)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredness), requiredness, "Object entry requiredness must be explicit.");
            }

            if (ownerIdentity is { IsValid: false })
            {
                throw new ArgumentException("Owner identity must be valid when provided.", nameof(ownerIdentity));
            }

            if (ownerIdentity.HasValue)
            {
                var expectedOwnerDomain = GetExpectedOwnerDomain(scope);
                if (ownerIdentity.Value.Domain != expectedOwnerDomain)
                {
                    throw new ArgumentException(
                        $"Object entry owner for scope '{scope}' must use identity domain '{expectedOwnerDomain}', but received '{ownerIdentity.Value.Domain}'.",
                        nameof(ownerIdentity));
                }
            }

            Id = id;
            Scope = scope;
            SourceKind = sourceKind;
            Requiredness = requiredness;
            DisplayName = displayName.NormalizeTextOrFallback(id.StableText);
            OwnerIdentity = ownerIdentity;
        }

        public ObjectEntryId Id { get; }

        public ObjectEntryScope Scope { get; }

        public ObjectEntrySourceKind SourceKind { get; }

        public ObjectEntryRequiredness Requiredness { get; }

        public string DisplayName { get; }

        public FrameworkIdentityKey? OwnerIdentity { get; }

        public bool HasOwnerIdentity => OwnerIdentity.HasValue;

        public bool IsRequired => Requiredness == ObjectEntryRequiredness.Required;

        internal static FrameworkIdentityDomain GetExpectedOwnerDomain(ObjectEntryScope scope)
        {
            switch (scope)
            {
                case ObjectEntryScope.Session:
                    return FrameworkIdentityDomain.Session;
                case ObjectEntryScope.Route:
                    return FrameworkIdentityDomain.Route;
                case ObjectEntryScope.Activity:
                    return FrameworkIdentityDomain.Activity;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), scope, "Object entry owner domain cannot be inferred for an unspecified scope.");
            }
        }

        public bool Equals(ObjectEntryDescriptor other)
        {
            return Id.Equals(other.Id)
                && Scope == other.Scope
                && SourceKind == other.SourceKind
                && Requiredness == other.Requiredness
                && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
                && Nullable.Equals(OwnerIdentity, other.OwnerIdentity);
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectEntryDescriptor other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Id.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Scope;
                hashCode = hashCode * 397 ^ (int)SourceKind;
                hashCode = hashCode * 397 ^ (int)Requiredness;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(DisplayName ?? string.Empty);
                hashCode = hashCode * 397 ^ (OwnerIdentity.HasValue ? OwnerIdentity.Value.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return HasOwnerIdentity
                ? $"id='{Id.StableText}' scope='{Scope}' owner='{OwnerIdentity.Value.StableText}' sourceKind='{SourceKind}' requiredness='{Requiredness}'"
                : $"id='{Id.StableText}' scope='{Scope}' sourceKind='{SourceKind}' requiredness='{Requiredness}'";
        }
    }
}
