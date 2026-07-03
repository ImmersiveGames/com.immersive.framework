using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.Common;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Declares who owns runtime-created content for one lifecycle scope.
    /// This is passive ownership data; it does not resolve roots, find objects or release content.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8B runtime content owner primitive; no registry lookup behavior.")]
    public readonly struct RuntimeContentOwner : IEquatable<RuntimeContentOwner>
    {
        public RuntimeContentOwner(
            RuntimeContentScope scope,
            FrameworkIdentityKey ownerIdentity,
            string ownerName)
        {
            ValidateScope(scope);
            ValidateOwner(scope, ownerIdentity);

            Scope = scope;
            OwnerIdentity = ownerIdentity;
            OwnerName = Normalize(ownerName);
        }

        public RuntimeContentScope Scope { get; }

        public FrameworkIdentityKey OwnerIdentity { get; }

        public string OwnerId => OwnerIdentity.Value.Value;

        public string OwnerName { get; }

        public bool IsValid => Scope != RuntimeContentScope.Unknown && OwnerIdentity.IsValid;

        public string StableText => $"{Scope}:{OwnerIdentity.StableText}";

        public bool Equals(RuntimeContentOwner other)
        {
            return Scope == other.Scope && OwnerIdentity.Equals(other.OwnerIdentity);
        }

        public override bool Equals(object obj)
        {
            return obj is RuntimeContentOwner other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (int)Scope * 397 ^ OwnerIdentity.GetHashCode();
            }
        }

        public override string ToString()
        {
            return StableText;
        }

        public static RuntimeContentOwner Session(string sessionId, string ownerName)
        {
            return new RuntimeContentOwner(
                RuntimeContentScope.Session,
                FrameworkIdentityKey.From(FrameworkIdentityDomain.Session, sessionId),
                ownerName);
        }

        public static RuntimeContentOwner Route(string routeId, string ownerName)
        {
            return new RuntimeContentOwner(
                RuntimeContentScope.Route,
                FrameworkIdentityKey.From(FrameworkIdentityDomain.Route, routeId),
                ownerName);
        }

        public static RuntimeContentOwner Activity(string activityId, string ownerName)
        {
            return new RuntimeContentOwner(
                RuntimeContentScope.Activity,
                FrameworkIdentityKey.From(FrameworkIdentityDomain.Activity, activityId),
                ownerName);
        }

        public static RuntimeContentOwner Transient(string runtimeOwnerId, string ownerName)
        {
            return new RuntimeContentOwner(
                RuntimeContentScope.Transient,
                FrameworkIdentityKey.From(FrameworkIdentityDomain.Runtime, runtimeOwnerId),
                ownerName);
        }

        public static FrameworkIdentityDomain GetExpectedOwnerDomain(RuntimeContentScope scope)
        {
            switch (scope)
            {
                case RuntimeContentScope.Session:
                    return FrameworkIdentityDomain.Session;
                case RuntimeContentScope.Route:
                    return FrameworkIdentityDomain.Route;
                case RuntimeContentScope.Activity:
                    return FrameworkIdentityDomain.Activity;
                case RuntimeContentScope.Transient:
                    return FrameworkIdentityDomain.Runtime;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scope), scope, "Runtime content owner domain cannot be inferred for an unknown scope.");
            }
        }

        public static bool operator ==(RuntimeContentOwner left, RuntimeContentOwner right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RuntimeContentOwner left, RuntimeContentOwner right)
        {
            return !left.Equals(right);
        }

        private static void ValidateScope(RuntimeContentScope scope)
        {
            if (!Enum.IsDefined(typeof(RuntimeContentScope), scope) || scope == RuntimeContentScope.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Runtime content scope must be explicit.");
            }
        }

        private static void ValidateOwner(RuntimeContentScope scope, FrameworkIdentityKey ownerIdentity)
        {
            if (!ownerIdentity.IsValid)
            {
                throw new ArgumentException("Runtime content owner identity must be valid.", nameof(ownerIdentity));
            }

            var expectedDomain = GetExpectedOwnerDomain(scope);
            if (ownerIdentity.Domain != expectedDomain)
            {
                throw new ArgumentException(
                    $"Runtime content owner for scope '{scope}' must use identity domain '{expectedDomain}', but received '{ownerIdentity.Domain}'.",
                    nameof(ownerIdentity));
            }
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
