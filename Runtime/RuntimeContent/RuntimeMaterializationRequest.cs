using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Explicit request to materialize runtime-created content inside one runtime scope context.
    /// It declares identity, owner context, resource descriptor and scoped cancellation token only; it does not instantiate, destroy, register roots or bind anchors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8H explicit runtime materialization request with scoped cancellation token; no materializer implementation or UnityEngine reference.")]
    public readonly struct RuntimeMaterializationRequest : IEquatable<RuntimeMaterializationRequest>
    {
        public RuntimeMaterializationRequest(
            RuntimeScopeContext context,
            RuntimeContentId contentId,
            RuntimeMaterializationResource resource,
            RuntimeScopeCancellationToken cancellationToken,
            string source,
            string reason)
        {
            if (!context.IsValid)
            {
                throw new ArgumentException("Runtime materialization request context must be valid.", nameof(context));
            }

            if (!contentId.IsValid)
            {
                throw new ArgumentException("Runtime materialization request content id must be valid.", nameof(contentId));
            }

            if (!resource.IsValid)
            {
                throw new ArgumentException("Runtime materialization request resource must be valid.", nameof(resource));
            }

            if (!cancellationToken.IsValid)
            {
                throw new ArgumentException("Runtime materialization request cancellation token must be valid.", nameof(cancellationToken));
            }

            if (cancellationToken.Owner != context.Owner)
            {
                throw new ArgumentException("Runtime materialization request cancellation token owner must match the request context owner.", nameof(cancellationToken));
            }

            Context = context;
            ContentId = contentId;
            Resource = resource;
            CancellationToken = cancellationToken;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public RuntimeScopeContext Context { get; }

        public RuntimeContentOwner Owner => Context.Owner;

        public RuntimeContentScope Scope => Context.Scope;

        public RuntimeContentId ContentId { get; }

        public RuntimeContentIdentity Identity => Context.CreateIdentity(ContentId);

        public RuntimeMaterializationResource Resource { get; }

        public RuntimeScopeCancellationToken CancellationToken { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsValid => Context.IsValid
            && ContentId.IsValid
            && Resource.IsValid
            && CancellationToken.IsValid
            && CancellationToken.Owner == Owner;

        public bool Equals(RuntimeMaterializationRequest other)
        {
            return Context.Equals(other.Context)
                && ContentId.Equals(other.ContentId)
                && Resource.Equals(other.Resource)
                && CancellationToken.Equals(other.CancellationToken)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is RuntimeMaterializationRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Context.GetHashCode();
                hashCode = hashCode * 397 ^ ContentId.GetHashCode();
                hashCode = hashCode * 397 ^ Resource.GetHashCode();
                hashCode = hashCode * 397 ^ CancellationToken.GetHashCode();
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
            return $"identity='{Identity.StableText}' owner='{Owner.StableText}' scope='{Scope}' contentId='{ContentId.StableText}' {Resource.ToDiagnosticString()} token={CancellationToken.ToDiagnosticString()} source='{sourceText}' reason='{reasonText}'";
        }

        public static RuntimeMaterializationRequest From(
            RuntimeScopeContext context,
            string contentId,
            RuntimeMaterializationResource resource,
            RuntimeScopeCancellationToken cancellationToken,
            string source,
            string reason)
        {
            return new RuntimeMaterializationRequest(
                context,
                RuntimeContentId.From(contentId),
                resource,
                cancellationToken,
                source,
                reason);
        }

        public static bool operator ==(RuntimeMaterializationRequest left, RuntimeMaterializationRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RuntimeMaterializationRequest left, RuntimeMaterializationRequest right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
