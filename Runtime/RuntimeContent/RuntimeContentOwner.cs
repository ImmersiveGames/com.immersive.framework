using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.Common;
using UnityEngine;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Declares who owns runtime-created content for one lifecycle scope.
    /// This is passive ownership data; it does not resolve roots, find objects or release content.
    /// <para>
    /// IF-ADR-014 / IF-ID-05: operational equality uses stable definition identity plus a process-local
    /// definition token (Unity <see cref="EntityId"/> of the authored asset). Stable IDs remain boundary
    /// evidence; two distinct authored assets that share one stable ID never share release authority.
    /// </para>
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8B runtime content owner primitive; no registry lookup behavior.")]
    public readonly struct RuntimeContentOwner : IEquatable<RuntimeContentOwner>
    {
        public RuntimeContentOwner(
            RuntimeContentScope scope,
            FrameworkIdentityKey ownerIdentity,
            string ownerName,
            EntityId definitionToken = default)
        {
            ValidateScope(scope);
            ValidateOwner(scope, ownerIdentity);

            Scope = scope;
            OwnerIdentity = ownerIdentity;
            OwnerName = Normalize(ownerName);
            DefinitionToken = definitionToken;
        }

        public RuntimeContentScope Scope { get; }

        /// <summary>
        /// Stable boundary identity for the owning definition (RouteId / ActivityId / session id, etc.).
        /// Not sufficient alone for operational release authority when definition tokens differ.
        /// </summary>
        public FrameworkIdentityKey OwnerIdentity { get; }

        public string OwnerId => OwnerIdentity.Value.Value;

        public string OwnerName { get; }

        /// <summary>
        /// Process-local token for the exact authored definition instance.
        /// Typically <c>UnityEngine.Object.GetEntityId()</c> for Route/Activity assets.
        /// Default means the caller does not distinguish definition instances (Session/Transient).
        /// </summary>
        public EntityId DefinitionToken { get; }

        public bool IsValid => Scope != RuntimeContentScope.Unknown && OwnerIdentity.IsValid;

        public bool HasDefinitionToken => !DefinitionToken.Equals(default(EntityId));

        public string StableText =>
            HasDefinitionToken
                ? $"{Scope}:{OwnerIdentity.StableText}#def-{DefinitionToken}"
                : $"{Scope}:{OwnerIdentity.StableText}";

        public bool Equals(RuntimeContentOwner other)
        {
            return Scope == other.Scope &&
                   OwnerIdentity.Equals(other.OwnerIdentity) &&
                   DefinitionToken.Equals(other.DefinitionToken);
        }

        public override bool Equals(object obj)
        {
            return obj is RuntimeContentOwner other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Scope * 397 ^ OwnerIdentity.GetHashCode();
                hashCode = hashCode * 397 ^ DefinitionToken.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            return StableText;
        }

        /// <summary>
        /// True when both owners share the same scope and stable boundary identity,
        /// ignoring definition-instance tokens. Useful for diagnostics only.
        /// </summary>
        public bool HasSameStableDefinition(RuntimeContentOwner other)
        {
            return Scope == other.Scope && OwnerIdentity.Equals(other.OwnerIdentity);
        }

        public static RuntimeContentOwner Session(string sessionId, string ownerName)
        {
            return new RuntimeContentOwner(
                RuntimeContentScope.Session,
                FrameworkIdentityKey.From(FrameworkIdentityDomain.Session, sessionId),
                ownerName);
        }

        public static RuntimeContentOwner Route(
            string routeId,
            string ownerName,
            EntityId definitionToken = default)
        {
            return new RuntimeContentOwner(
                RuntimeContentScope.Route,
                FrameworkIdentityKey.From(FrameworkIdentityDomain.Route, routeId),
                ownerName,
                definitionToken);
        }

        public static RuntimeContentOwner Activity(
            string activityId,
            string ownerName,
            EntityId definitionToken = default)
        {
            return new RuntimeContentOwner(
                RuntimeContentScope.Activity,
                FrameworkIdentityKey.From(FrameworkIdentityDomain.Activity, activityId),
                ownerName,
                definitionToken);
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
