using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Common;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Explicit logical request to bind runtime content to an authored Content Anchor.
    /// The request names the anchor identity, runtime scope context, runtime content id and resource descriptor only;
    /// it does not resolve scene objects, move transforms, instantiate prefabs or create fallback anchors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9A Content Anchor binding request contract; no physical placement implementation.")]
    public readonly struct ContentAnchorBindingRequest : IEquatable<ContentAnchorBindingRequest>
    {
        public ContentAnchorBindingRequest(
            RuntimeScopeContext runtimeContext,
            ContentAnchorScope anchorScope,
            FrameworkIdentityKey anchorOwner,
            ContentAnchorKind anchorKind,
            ContentAnchorId anchorId,
            RuntimeContentId runtimeContentId,
            RuntimeMaterializationResource resource,
            string source,
            string reason)
        {
            if (!runtimeContext.IsValid)
            {
                throw new ArgumentException("Content Anchor binding request runtime context must be valid.", nameof(runtimeContext));
            }

            if (anchorScope == ContentAnchorScope.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(anchorScope), anchorScope, "Content Anchor binding request scope must be explicit.");
            }

            if (!anchorOwner.IsValid)
            {
                throw new ArgumentException("Content Anchor binding request owner must be explicit and valid.", nameof(anchorOwner));
            }

            if (anchorKind == ContentAnchorKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(anchorKind), anchorKind, "Content Anchor binding request kind must be explicit.");
            }

            if (!anchorId.IsValid)
            {
                throw new ArgumentException("Content Anchor binding request id must be explicit and valid.", nameof(anchorId));
            }

            if (!runtimeContentId.IsValid)
            {
                throw new ArgumentException("Content Anchor binding request runtime content id must be valid.", nameof(runtimeContentId));
            }

            if (!resource.IsValid)
            {
                throw new ArgumentException("Content Anchor binding request resource must be valid.", nameof(resource));
            }

            RuntimeContext = runtimeContext;
            AnchorScope = anchorScope;
            AnchorOwner = anchorOwner;
            AnchorKind = anchorKind;
            AnchorId = anchorId;
            RuntimeContentId = runtimeContentId;
            Resource = resource;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public RuntimeScopeContext RuntimeContext { get; }

        public RuntimeContentOwner RuntimeOwner => RuntimeContext.Owner;

        public RuntimeContentScope RuntimeScope => RuntimeContext.Scope;

        public ContentAnchorScope AnchorScope { get; }

        public FrameworkIdentityKey AnchorOwner { get; }

        public ContentAnchorKind AnchorKind { get; }

        public ContentAnchorId AnchorId { get; }

        public RuntimeContentId RuntimeContentId { get; }

        public RuntimeContentIdentity RuntimeIdentity => RuntimeContext.CreateIdentity(RuntimeContentId);

        public RuntimeMaterializationResource Resource { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsValid => RuntimeContext.IsValid
            && AnchorScope != ContentAnchorScope.Unknown
            && AnchorOwner.IsValid
            && AnchorKind != ContentAnchorKind.Unknown
            && AnchorId.IsValid
            && RuntimeContentId.IsValid
            && Resource.IsValid;

        public string AnchorStableText => $"ContentAnchor:{AnchorScope}:{AnchorKind}:{AnchorOwner.StableText}:{AnchorId.StableText}";

        public bool Matches(ContentAnchorDeclaration declaration)
        {
            return declaration.IsValid
                && declaration.Scope == AnchorScope
                && declaration.Kind == AnchorKind
                && declaration.Owner == AnchorOwner
                && declaration.AnchorId == AnchorId;
        }

        public bool Equals(ContentAnchorBindingRequest other)
        {
            return RuntimeContext.Equals(other.RuntimeContext)
                && AnchorScope == other.AnchorScope
                && AnchorOwner.Equals(other.AnchorOwner)
                && AnchorKind == other.AnchorKind
                && AnchorId.Equals(other.AnchorId)
                && RuntimeContentId.Equals(other.RuntimeContentId)
                && Resource.Equals(other.Resource)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ContentAnchorBindingRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = RuntimeContext.GetHashCode();
                hashCode = hashCode * 397 ^ (int)AnchorScope;
                hashCode = hashCode * 397 ^ AnchorOwner.GetHashCode();
                hashCode = hashCode * 397 ^ (int)AnchorKind;
                hashCode = hashCode * 397 ^ AnchorId.GetHashCode();
                hashCode = hashCode * 397 ^ RuntimeContentId.GetHashCode();
                hashCode = hashCode * 397 ^ Resource.GetHashCode();
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
            return $"anchor='{AnchorStableText}' anchorScope='{AnchorScope}' anchorKind='{AnchorKind}' anchorOwner='{AnchorOwner.StableText}' anchorId='{AnchorId.StableText}' runtimeIdentity='{RuntimeIdentity.StableText}' runtimeOwner='{RuntimeOwner.StableText}' runtimeScope='{RuntimeScope}' {Resource.ToDiagnosticString()} source='{sourceText}' reason='{reasonText}'";
        }

        public static ContentAnchorBindingRequest FromDeclaration(
            RuntimeScopeContext runtimeContext,
            ContentAnchorDeclaration declaration,
            RuntimeContentId runtimeContentId,
            RuntimeMaterializationResource resource,
            string source,
            string reason)
        {
            if (!declaration.IsValid)
            {
                throw new ArgumentException("Content Anchor binding request requires a valid declaration.", nameof(declaration));
            }

            return new ContentAnchorBindingRequest(
                runtimeContext,
                declaration.Scope,
                declaration.Owner,
                declaration.Kind,
                declaration.AnchorId,
                runtimeContentId,
                resource,
                source,
                reason);
        }

        public static ContentAnchorBindingRequest FromDeclaration(
            RuntimeScopeContext runtimeContext,
            ContentAnchorDeclaration declaration,
            string runtimeContentId,
            RuntimeMaterializationResource resource,
            string source,
            string reason)
        {
            return FromDeclaration(
                runtimeContext,
                declaration,
                RuntimeContentId.From(runtimeContentId),
                resource,
                source,
                reason);
        }

        public static ContentAnchorBindingRequest ForAnchor(
            RuntimeScopeContext runtimeContext,
            ContentAnchorScope anchorScope,
            string anchorOwnerId,
            ContentAnchorKind anchorKind,
            string anchorId,
            string runtimeContentId,
            RuntimeMaterializationResource resource,
            string source,
            string reason)
        {
            return new ContentAnchorBindingRequest(
                runtimeContext,
                anchorScope,
                ContentAnchorDeclaration.CreateOwnerKey(anchorScope, anchorOwnerId),
                anchorKind,
                ContentAnchorId.From(anchorId),
                RuntimeContentId.From(runtimeContentId),
                resource,
                source,
                reason);
        }

        public static bool operator ==(ContentAnchorBindingRequest left, ContentAnchorBindingRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ContentAnchorBindingRequest left, ContentAnchorBindingRequest right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
