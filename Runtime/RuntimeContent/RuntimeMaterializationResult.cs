using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Immutable result for a runtime materialization attempt.
    /// It reports request/handle/status diagnostics only; it does not execute release, destroy objects or bind Content Anchors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8G runtime materialization result contract; no release execution or Content Anchor binding.")]
    public readonly struct RuntimeMaterializationResult : IEquatable<RuntimeMaterializationResult>
    {
        public RuntimeMaterializationResult(
            RuntimeMaterializationRequest request,
            RuntimeMaterializationStatus status,
            RuntimeContentHandle handle,
            string source,
            string reason,
            string message)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Runtime materialization result request must be valid.", nameof(request));
            }

            if (!Enum.IsDefined(typeof(RuntimeMaterializationStatus), status)
                || status == RuntimeMaterializationStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Runtime materialization status must be explicit.");
            }

            if (status == RuntimeMaterializationStatus.Succeeded)
            {
                if (handle == null)
                {
                    throw new ArgumentNullException(nameof(handle), "Successful runtime materialization result must include a handle.");
                }

                if (handle.Identity != request.Identity)
                {
                    throw new ArgumentException("Successful runtime materialization handle identity must match the request identity.", nameof(handle));
                }
            }

            Request = request;
            Status = status;
            Handle = handle;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public RuntimeMaterializationRequest Request { get; }

        public RuntimeMaterializationStatus Status { get; }

        public RuntimeContentHandle Handle { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status == RuntimeMaterializationStatus.Succeeded;

        public bool Failed => !Succeeded;

        public bool HasHandle => Handle != null;

        public RuntimeContentIdentity Identity => Request.Identity;

        public RuntimeContentOwner Owner => Request.Owner;

        public RuntimeContentScope Scope => Request.Scope;

        public bool Equals(RuntimeMaterializationResult other)
        {
            return Request.Equals(other.Request)
                && Status == other.Status
                && ReferenceEquals(Handle, other.Handle)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is RuntimeMaterializationResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Request.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ (Handle != null ? Handle.GetHashCode() : 0);
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
            string handleText = HasHandle ? Handle.ToDiagnosticString() : "<none>";
            return $"identity='{Identity.StableText}' owner='{Owner.StableText}' scope='{Scope}' status='{Status}' succeeded='{Succeeded}' handle={handleText} source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static RuntimeMaterializationResult Success(
            RuntimeMaterializationRequest request,
            RuntimeContentHandle handle,
            string source,
            string reason,
            string message)
        {
            return new RuntimeMaterializationResult(
                request,
                RuntimeMaterializationStatus.Succeeded,
                handle,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Runtime content materialized successfully."
                    : message);
        }

        public static RuntimeMaterializationResult Failure(
            RuntimeMaterializationRequest request,
            RuntimeMaterializationStatus status,
            string source,
            string reason,
            string message)
        {
            if (status == RuntimeMaterializationStatus.Succeeded)
            {
                throw new ArgumentException("Use Success for successful runtime materialization results.", nameof(status));
            }

            return new RuntimeMaterializationResult(
                request,
                status,
                null,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Runtime content materialization failed."
                    : message);
        }

        internal static RuntimeMaterializationResult FromRegistryResult(
            RuntimeMaterializationRequest request,
            RuntimeContentHandle handle,
            RuntimeRootRegistryOperationResult registryResult,
            string source,
            string reason)
        {
            if (registryResult == null)
            {
                return Failure(
                    request,
                    RuntimeMaterializationStatus.FailedRegistration,
                    source,
                    reason,
                    "Runtime materialization registry result was missing.");
            }

            if (registryResult.Applied || registryResult.Status == RuntimeRootRegistryOperationStatus.HandleAlreadyRegistered)
            {
                return Success(
                    request,
                    handle,
                    source,
                    reason,
                    registryResult.Message);
            }

            var status = registryResult.Status == RuntimeRootRegistryOperationStatus.RejectedMissingRoot
                ? RuntimeMaterializationStatus.FailedMissingRoot
                : registryResult.Status == RuntimeRootRegistryOperationStatus.RejectedMismatchedOwner
                    ? RuntimeMaterializationStatus.RejectedMismatchedIdentity
                    : RuntimeMaterializationStatus.FailedRegistration;

            return Failure(
                request,
                status,
                source,
                reason,
                registryResult.Message);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
