using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Common;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Immutable result for a logical Content Anchor binding attempt.
    /// It reports anchor/runtime-content correlation diagnostics only; it does not execute placement or physical release.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9A Content Anchor binding result contract; no physical placement implementation.")]
    public readonly struct ContentAnchorBindingResult : IEquatable<ContentAnchorBindingResult>
    {
        public ContentAnchorBindingResult(
            ContentAnchorBindingRequest request,
            ContentAnchorBindingStatus status,
            ContentAnchorDeclaration anchor,
            ContentAnchorContentHandle handle,
            string source,
            string reason,
            string message)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Content Anchor binding result request must be valid.", nameof(request));
            }

            if (!Enum.IsDefined(typeof(ContentAnchorBindingStatus), status)
                || status == ContentAnchorBindingStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Content Anchor binding status must be explicit.");
            }

            if (status is ContentAnchorBindingStatus.Succeeded or ContentAnchorBindingStatus.SucceededAlreadyBound)
            {
                if (!anchor.IsValid)
                {
                    throw new ArgumentException("Successful Content Anchor binding result must include a valid anchor.", nameof(anchor));
                }

                if (!request.Matches(anchor))
                {
                    throw new ArgumentException("Successful Content Anchor binding result anchor must match the request.", nameof(anchor));
                }

                if (!handle.IsValid)
                {
                    throw new ArgumentException("Successful Content Anchor binding result must include a valid binding handle.", nameof(handle));
                }
            }

            Request = request;
            Status = status;
            Anchor = anchor;
            Handle = handle;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public ContentAnchorBindingRequest Request { get; }

        public ContentAnchorBindingStatus Status { get; }

        public ContentAnchorDeclaration Anchor { get; }

        public ContentAnchorContentHandle Handle { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status is ContentAnchorBindingStatus.Succeeded or ContentAnchorBindingStatus.SucceededAlreadyBound;

        public bool Failed => !Succeeded;

        public bool HasAnchor => Anchor.IsValid;

        public bool HasHandle => Handle.IsValid;

        public RuntimeContentIdentity RuntimeIdentity => Request.RuntimeIdentity;

        public RuntimeContentOwner RuntimeOwner => Request.RuntimeOwner;

        public RuntimeContentScope RuntimeScope => Request.RuntimeScope;

        public bool Equals(ContentAnchorBindingResult other)
        {
            return Request.Equals(other.Request)
                && Status == other.Status
                && Anchor.Equals(other.Anchor)
                && Handle.Equals(other.Handle)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ContentAnchorBindingResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Request.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ Anchor.GetHashCode();
                hashCode = hashCode * 397 ^ Handle.GetHashCode();
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = hashCode * 397 ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
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
            string messageText = Message.ToDiagnosticText();
            string anchorText = HasAnchor ? Anchor.ToDiagnosticString() : "<none>";
            string handleText = HasHandle ? Handle.ToDiagnosticString() : "<none>";
            return $"anchorRequest='{Request.AnchorStableText}' runtimeIdentity='{RuntimeIdentity.StableText}' status='{Status}' succeeded='{Succeeded}' anchor={anchorText} bindingHandle={handleText} source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static ContentAnchorBindingResult Success(
            ContentAnchorBindingRequest request,
            ContentAnchorDeclaration anchor,
            RuntimeContentHandle runtimeHandle,
            string source,
            string reason,
            string message)
        {
            var handle = ContentAnchorContentHandle.From(request, anchor, runtimeHandle, source, reason);
            return new ContentAnchorBindingResult(
                request,
                ContentAnchorBindingStatus.Succeeded,
                anchor,
                handle,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Content Anchor binding succeeded."
                    : message);
        }

        public static ContentAnchorBindingResult AlreadyBound(
            ContentAnchorBindingRequest request,
            ContentAnchorDeclaration anchor,
            ContentAnchorContentHandle handle,
            string source,
            string reason,
            string message)
        {
            return new ContentAnchorBindingResult(
                request,
                ContentAnchorBindingStatus.SucceededAlreadyBound,
                anchor,
                handle,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Content Anchor binding already exists."
                    : message);
        }

        public static ContentAnchorBindingResult Failure(
            ContentAnchorBindingRequest request,
            ContentAnchorBindingStatus status,
            ContentAnchorDeclaration anchor,
            string source,
            string reason,
            string message)
        {
            if (status is ContentAnchorBindingStatus.Succeeded or ContentAnchorBindingStatus.SucceededAlreadyBound)
            {
                throw new ArgumentException("Use Success or AlreadyBound for successful Content Anchor binding results.", nameof(status));
            }

            return new ContentAnchorBindingResult(
                request,
                status,
                anchor,
                default(ContentAnchorContentHandle),
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Content Anchor binding failed."
                    : message);
        }

        public static ContentAnchorBindingResult MissingAnchor(
            ContentAnchorBindingRequest request,
            string source,
            string reason,
            string message)
        {
            return Failure(
                request,
                ContentAnchorBindingStatus.FailedMissingAnchor,
                default(ContentAnchorDeclaration),
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Content Anchor binding failed because the requested anchor was not available."
                    : message);
        }

        public static ContentAnchorBindingResult FromMaterializationFailure(
            ContentAnchorBindingRequest request,
            RuntimeMaterializationResult materializationResult,
            string source,
            string reason)
        {
            if (!materializationResult.Failed)
            {
                throw new ArgumentException("Only failed runtime materialization results can be converted to Content Anchor binding failure.", nameof(materializationResult));
            }

            return Failure(
                request,
                ContentAnchorBindingStatus.FailedRuntimeMaterialization,
                default(ContentAnchorDeclaration),
                source,
                reason,
                materializationResult.Message);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
