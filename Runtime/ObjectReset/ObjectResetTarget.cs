using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.ObjectEntry;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Canonical logical target for Object Reset.
    /// It is not a Unity GameObject, Transform, hierarchy path, prefab, Player id or Actor id.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14B Object Reset target primitive: ObjectEntryId + scope + owner identity.")]
    public readonly struct ObjectResetTarget : IEquatable<ObjectResetTarget>
    {
        public ObjectResetTarget(
            ObjectEntryId objectEntryId,
            ObjectEntryScope scope,
            FrameworkIdentityKey ownerIdentity)
        {
            if (!objectEntryId.IsValid)
            {
                throw new ArgumentException("Object Reset target requires a valid ObjectEntryId.", nameof(objectEntryId));
            }

            if (!Enum.IsDefined(typeof(ObjectEntryScope), scope) || scope == ObjectEntryScope.Unspecified)
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Object Reset target scope must be explicit.");
            }

            if (!ownerIdentity.IsValid)
            {
                throw new ArgumentException("Object Reset target requires a valid owner identity.", nameof(ownerIdentity));
            }

            var expectedOwnerDomain = GetExpectedOwnerDomain(scope);
            if (ownerIdentity.Domain != expectedOwnerDomain)
            {
                throw new ArgumentException(
                    $"Object Reset target owner for scope '{scope}' must use identity domain '{expectedOwnerDomain}', but received '{ownerIdentity.Domain}'.",
                    nameof(ownerIdentity));
            }

            ObjectEntryId = objectEntryId;
            Scope = scope;
            OwnerIdentity = ownerIdentity;
        }

        public ObjectEntryId ObjectEntryId { get; }

        public ObjectEntryScope Scope { get; }

        public FrameworkIdentityKey OwnerIdentity { get; }

        public bool IsValid => ObjectEntryId.IsValid
            && Scope != ObjectEntryScope.Unspecified
            && OwnerIdentity.IsValid
            && OwnerIdentity.Domain == GetExpectedOwnerDomain(Scope);

        public bool Matches(ObjectEntryDescriptor descriptor)
        {
            return descriptor.Id == ObjectEntryId
                && descriptor.Scope == Scope
                && descriptor.HasOwnerIdentity
                && descriptor.OwnerIdentity.Value == OwnerIdentity;
        }

        public bool Equals(ObjectResetTarget other)
        {
            return ObjectEntryId.Equals(other.ObjectEntryId)
                && Scope == other.Scope
                && OwnerIdentity.Equals(other.OwnerIdentity);
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectResetTarget other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ObjectEntryId.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Scope;
                hashCode = hashCode * 397 ^ OwnerIdentity.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string objectEntryText = ObjectEntryId.IsValid ? ObjectEntryId.StableText : "<invalid>";
            string ownerText = OwnerIdentity.IsValid ? OwnerIdentity.StableText : "<invalid>";
            return $"objectEntry='{objectEntryText}' scope='{Scope}' owner='{ownerText}'";
        }

        public static ObjectResetTarget FromDescriptor(ObjectEntryDescriptor descriptor)
        {
            if (!descriptor.HasOwnerIdentity)
            {
                throw new ArgumentException("Object Reset target cannot be created from an ObjectEntryDescriptor without owner identity.", nameof(descriptor));
            }

            return new ObjectResetTarget(descriptor.Id, descriptor.Scope, descriptor.OwnerIdentity.Value);
        }

        public static bool operator ==(ObjectResetTarget left, ObjectResetTarget right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ObjectResetTarget left, ObjectResetTarget right)
        {
            return !left.Equals(right);
        }

        private static FrameworkIdentityDomain GetExpectedOwnerDomain(ObjectEntryScope scope)
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
                    return FrameworkIdentityDomain.Unspecified;
            }
        }
    }
}
