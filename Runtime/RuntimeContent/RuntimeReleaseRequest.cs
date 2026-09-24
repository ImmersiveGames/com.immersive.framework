using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Explicit request to release one registered runtime content identity.
    /// It carries owner context, identity and logical release policy only; physical cleanup belongs to an adapter outside RuntimeContent core.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8J explicit runtime release request; no physical cleanup implementation.")]
    public readonly struct RuntimeReleaseRequest : IEquatable<RuntimeReleaseRequest>
    {
        public RuntimeReleaseRequest(
            RuntimeScopeContext context,
            RuntimeContentIdentity identity,
            RuntimeReleasePolicy policy,
            string source,
            string reason)
        {
            if (!context.IsValid)
            {
                throw new ArgumentException("Runtime release request context must be valid.", nameof(context));
            }

            if (!identity.IsValid)
            {
                throw new ArgumentException("Runtime release request identity must be valid.", nameof(identity));
            }

            if (identity.Owner != context.Owner)
            {
                throw new ArgumentException("Runtime release request identity owner must match the request context owner.", nameof(identity));
            }

            if (!Enum.IsDefined(typeof(RuntimeReleasePolicy), policy) || policy == RuntimeReleasePolicy.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(policy), policy, "Runtime release policy must be explicit.");
            }

            Context = context;
            Identity = identity;
            Policy = policy;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public RuntimeScopeContext Context { get; }

        public RuntimeContentOwner Owner => Context.Owner;

        public RuntimeContentScope Scope => Context.Scope;

        public RuntimeContentIdentity Identity { get; }

        public RuntimeContentId ContentId => Identity.ContentId;

        public RuntimeReleasePolicy Policy { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool ShouldUnregister => Policy == RuntimeReleasePolicy.MarkReleasedAndUnregister;

        public bool IsValid => Context.IsValid
            && Identity.IsValid
            && Identity.Owner == Context.Owner
            && Policy != RuntimeReleasePolicy.Unknown;

        public bool Equals(RuntimeReleaseRequest other)
        {
            return Context.Equals(other.Context)
                && Identity.Equals(other.Identity)
                && Policy == other.Policy
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is RuntimeReleaseRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Context.GetHashCode();
                hashCode = hashCode * 397 ^ Identity.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Policy;
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            return $"identity='{Identity.StableText}' owner='{Owner.StableText}' scope='{Scope}' policy='{Policy}' source='{sourceText}' reason='{reasonText}'";
        }

        public static RuntimeReleaseRequest From(
            RuntimeScopeContext context,
            RuntimeContentId contentId,
            RuntimeReleasePolicy policy,
            string source,
            string reason)
        {
            return new RuntimeReleaseRequest(
                context,
                context.CreateIdentity(contentId),
                policy,
                source,
                reason);
        }

        public static RuntimeReleaseRequest From(
            RuntimeScopeContext context,
            string contentId,
            RuntimeReleasePolicy policy,
            string source,
            string reason)
        {
            return From(
                context,
                RuntimeContentId.From(contentId),
                policy,
                source,
                reason);
        }

        public static bool operator ==(RuntimeReleaseRequest left, RuntimeReleaseRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RuntimeReleaseRequest left, RuntimeReleaseRequest right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
