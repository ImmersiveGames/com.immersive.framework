using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Common;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Passive correlation between an authored Content Anchor and a runtime content handle.
    /// This handle records logical binding identity only; it does not own, move, destroy or release physical content.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9A passive Content Anchor content handle; correlates anchor and runtime content without physical placement.")]
    public readonly struct ContentAnchorContentHandle : IEquatable<ContentAnchorContentHandle>
    {
        public ContentAnchorContentHandle(
            ContentAnchorBindingRequest request,
            ContentAnchorDeclaration anchor,
            RuntimeContentHandle runtimeHandle,
            string source,
            string reason)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Content Anchor content handle request must be valid.", nameof(request));
            }

            if (!anchor.IsValid)
            {
                throw new ArgumentException("Content Anchor content handle anchor must be valid.", nameof(anchor));
            }

            if (!request.Matches(anchor))
            {
                throw new ArgumentException("Content Anchor content handle anchor must match the binding request.", nameof(anchor));
            }

            if (runtimeHandle == null)
            {
                throw new ArgumentNullException(nameof(runtimeHandle));
            }

            if (runtimeHandle.Identity != request.RuntimeIdentity)
            {
                throw new ArgumentException("Content Anchor content handle runtime handle identity must match the binding request runtime identity.", nameof(runtimeHandle));
            }

            if (runtimeHandle.IsReleased)
            {
                throw new ArgumentException("Content Anchor content handle cannot bind a released runtime handle.", nameof(runtimeHandle));
            }

            Request = request;
            Anchor = anchor;
            RuntimeHandle = runtimeHandle;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public ContentAnchorBindingRequest Request { get; }

        public ContentAnchorDeclaration Anchor { get; }

        public RuntimeContentHandle RuntimeHandle { get; }

        public ContentAnchorScope AnchorScope => Anchor.Scope;

        public ContentAnchorKind AnchorKind => Anchor.Kind;

        public ContentAnchorId AnchorId => Anchor.AnchorId;

        public RuntimeContentIdentity RuntimeIdentity => RuntimeHandle?.Identity ?? default(RuntimeContentIdentity);

        public RuntimeContentOwner RuntimeOwner => Request.RuntimeOwner;

        public RuntimeContentScope RuntimeScope => Request.RuntimeScope;

        public string Source { get; }

        public string Reason { get; }

        public bool IsValid => Request.IsValid
            && Anchor.IsValid
            && Request.Matches(Anchor)
            && RuntimeHandle != null
            && RuntimeHandle.Identity == Request.RuntimeIdentity
            && !RuntimeHandle.IsReleased;

        public string StableText => $"ContentAnchorBinding:{Anchor.StableText}:{RuntimeIdentity.StableText}";

        public bool Equals(ContentAnchorContentHandle other)
        {
            return Request.Equals(other.Request)
                && Anchor.Equals(other.Anchor)
                && ReferenceEquals(RuntimeHandle, other.RuntimeHandle)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ContentAnchorContentHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Request.GetHashCode();
                hashCode = hashCode * 397 ^ Anchor.GetHashCode();
                hashCode = hashCode * 397 ^ (RuntimeHandle != null ? RuntimeHandle.GetHashCode() : 0);
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
            string runtimeHandleText = RuntimeHandle != null ? RuntimeHandle.ToDiagnosticString() : "<none>";
            return $"binding='{StableText}' anchor='{Anchor.StableText}' anchorKind='{AnchorKind}' runtimeIdentity='{RuntimeIdentity.StableText}' runtimeOwner='{RuntimeOwner.StableText}' runtimeScope='{RuntimeScope}' runtimeHandle={runtimeHandleText} source='{sourceText}' reason='{reasonText}'";
        }

        public static ContentAnchorContentHandle From(
            ContentAnchorBindingRequest request,
            ContentAnchorDeclaration anchor,
            RuntimeContentHandle runtimeHandle,
            string source,
            string reason)
        {
            return new ContentAnchorContentHandle(request, anchor, runtimeHandle, source, reason);
        }

        public static bool operator ==(ContentAnchorContentHandle left, ContentAnchorContentHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ContentAnchorContentHandle left, ContentAnchorContentHandle right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
