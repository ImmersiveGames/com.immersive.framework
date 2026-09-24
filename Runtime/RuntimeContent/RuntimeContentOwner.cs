using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.Common;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Declares who owns runtime-created content for one lifecycle scope.
    /// This is passive ownership data; it does not resolve roots, find objects or release content.
    /// <para>
    /// IF-ADR-014 / IF-ID-05: operational equality uses stable definition identity plus a required
    /// process-local <see cref="RuntimeDefinitionToken"/> for Route and Activity owners. Stable IDs
    /// remain boundary evidence; two distinct authored assets that share one stable ID never share
    /// release authority.
    /// </para>
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8B runtime content owner primitive; no registry lookup behavior.")]
    public readonly struct RuntimeContentOwner : IEquatable<RuntimeContentOwner>
    {
        public RuntimeContentOwner(
            RuntimeContentScope scope,
            FrameworkIdentityKey ownerIdentity,
            string ownerName,
            RuntimeDefinitionToken definitionToken = default)
        {
            ValidateScope(scope);
            ValidateOwner(scope, ownerIdentity);
            ValidateDefinitionToken(scope, definitionToken);

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
        /// Required for Route and Activity scopes. Default only for Session/Transient.
        /// </summary>
        public RuntimeDefinitionToken DefinitionToken { get; }

        public bool IsValid =>
            Scope != RuntimeContentScope.Unknown &&
            OwnerIdentity.IsValid &&
            (!RequiresDefinitionToken(Scope) || DefinitionToken.IsValid);

        public bool HasDefinitionToken => DefinitionToken.IsValid;

        public string StableText =>
            HasDefinitionToken
                ? $"{Scope}:{OwnerIdentity.StableText}#{DefinitionToken.StableText}"
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

        /// <summary>
        /// Creates a Route operational owner. <paramref name="definitionToken"/> is required and must be valid.
        /// </summary>
        public static RuntimeContentOwner Route(
            string routeId,
            string ownerName,
            RuntimeDefinitionToken definitionToken)
        {
            if (!definitionToken.IsValid)
            {
                throw new ArgumentException(
                    "Route runtime content owner requires a valid process-local definition token.",
                    nameof(definitionToken));
            }

            return new RuntimeContentOwner(
                RuntimeContentScope.Route,
                FrameworkIdentityKey.From(FrameworkIdentityDomain.Route, routeId),
                ownerName,
                definitionToken);
        }

        /// <summary>
        /// Creates an Activity operational owner. <paramref name="definitionToken"/> is required and must be valid.
        /// </summary>
        public static RuntimeContentOwner Activity(
            string activityId,
            string ownerName,
            RuntimeDefinitionToken definitionToken)
        {
            if (!definitionToken.IsValid)
            {
                throw new ArgumentException(
                    "Activity runtime content owner requires a valid process-local definition token.",
                    nameof(definitionToken));
            }

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

        public static bool RequiresDefinitionToken(RuntimeContentScope scope)
        {
            return scope == RuntimeContentScope.Route || scope == RuntimeContentScope.Activity;
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

        private static void ValidateDefinitionToken(
            RuntimeContentScope scope,
            RuntimeDefinitionToken definitionToken)
        {
            if (RequiresDefinitionToken(scope) && !definitionToken.IsValid)
            {
                throw new ArgumentException(
                    $"Runtime content owner for scope '{scope}' requires a valid process-local definition token.",
                    nameof(definitionToken));
            }
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
