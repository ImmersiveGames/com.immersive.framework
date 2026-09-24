using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Immutable result for one runtime release attempt.
    /// It reports logical release/registry outcome only; it does not destroy objects, unload scenes, return pools or release Addressables handles.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8J runtime release result; logical handle state and registry cleanup only.")]
    public readonly struct RuntimeReleaseResult : IEquatable<RuntimeReleaseResult>
    {
        public RuntimeReleaseResult(
            RuntimeReleaseRequest request,
            RuntimeReleaseStatus status,
            RuntimeContentHandle handle,
            RuntimeContentState previousState,
            RuntimeContentState currentState,
            bool handleUnregistered,
            string source,
            string reason,
            string message)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Runtime release result request must be valid.", nameof(request));
            }

            if (!Enum.IsDefined(typeof(RuntimeReleaseStatus), status) || status == RuntimeReleaseStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Runtime release status must be explicit.");
            }

            if (handle != null && handle.Identity != request.Identity)
            {
                throw new ArgumentException("Runtime release result handle identity must match the request identity.", nameof(handle));
            }

            if (handle != null)
            {
                ValidateState(previousState, nameof(previousState));
                ValidateState(currentState, nameof(currentState));
            }

            Request = request;
            Status = status;
            Handle = handle;
            PreviousState = previousState;
            CurrentState = currentState;
            HandleUnregistered = handleUnregistered;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public RuntimeReleaseRequest Request { get; }

        public RuntimeReleaseStatus Status { get; }

        public RuntimeContentHandle Handle { get; }

        public RuntimeContentState PreviousState { get; }

        public RuntimeContentState CurrentState { get; }

        public bool HandleUnregistered { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status is RuntimeReleaseStatus.Succeeded or RuntimeReleaseStatus.SucceededAlreadyReleased;

        public bool Failed => !Succeeded;

        public bool HasHandle => Handle != null;

        public RuntimeContentIdentity Identity => Request.Identity;

        public RuntimeContentOwner Owner => Request.Owner;

        public RuntimeContentScope Scope => Request.Scope;

        public bool Equals(RuntimeReleaseResult other)
        {
            return Request.Equals(other.Request)
                && Status == other.Status
                && ReferenceEquals(Handle, other.Handle)
                && PreviousState == other.PreviousState
                && CurrentState == other.CurrentState
                && HandleUnregistered == other.HandleUnregistered
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is RuntimeReleaseResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Request.GetHashCode();
                hashCode = hashCode * 397 ^ (int)Status;
                hashCode = hashCode * 397 ^ (Handle != null ? Handle.GetHashCode() : 0);
                hashCode = hashCode * 397 ^ (int)PreviousState;
                hashCode = hashCode * 397 ^ (int)CurrentState;
                hashCode = hashCode * 397 ^ HandleUnregistered.GetHashCode();
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
            return $"identity='{Identity.StableText}' owner='{Owner.StableText}' scope='{Scope}' policy='{Request.Policy}' status='{Status}' succeeded='{Succeeded}' previousState='{PreviousState}' currentState='{CurrentState}' handleUnregistered='{HandleUnregistered}' handle={handleText} source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static RuntimeReleaseResult Success(
            RuntimeReleaseRequest request,
            RuntimeContentHandle handle,
            RuntimeContentState previousState,
            RuntimeContentState currentState,
            bool handleUnregistered,
            string source,
            string reason,
            string message)
        {
            return new RuntimeReleaseResult(
                request,
                RuntimeReleaseStatus.Succeeded,
                handle,
                previousState,
                currentState,
                handleUnregistered,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Runtime content released logically."
                    : message);
        }

        public static RuntimeReleaseResult AlreadyReleased(
            RuntimeReleaseRequest request,
            RuntimeContentHandle handle,
            bool handleUnregistered,
            string source,
            string reason,
            string message)
        {
            var state = handle?.State ?? RuntimeContentState.Unknown;
            return new RuntimeReleaseResult(
                request,
                RuntimeReleaseStatus.SucceededAlreadyReleased,
                handle,
                state,
                state,
                handleUnregistered,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Runtime content was already released."
                    : message);
        }

        public static RuntimeReleaseResult Failure(
            RuntimeReleaseRequest request,
            RuntimeReleaseStatus status,
            RuntimeContentHandle handle,
            RuntimeContentState previousState,
            RuntimeContentState currentState,
            string source,
            string reason,
            string message)
        {
            if (status is RuntimeReleaseStatus.Succeeded or RuntimeReleaseStatus.SucceededAlreadyReleased)
            {
                throw new ArgumentException("Use Success or AlreadyReleased for successful runtime release results.", nameof(status));
            }

            return new RuntimeReleaseResult(
                request,
                status,
                handle,
                previousState,
                currentState,
                false,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Runtime content release failed."
                    : message);
        }

        public static RuntimeReleaseResult AdapterFailure(
            RuntimeReleaseRequest request,
            string source,
            string reason,
            string message)
        {
            return Failure(
                request,
                RuntimeReleaseStatus.FailedReleaseAdapter,
                null,
                RuntimeContentState.Unknown,
                RuntimeContentState.Unknown,
                source,
                reason,
                string.IsNullOrWhiteSpace(message)
                    ? "Runtime release adapter failed."
                    : message);
        }

        private static void ValidateState(RuntimeContentState state, string paramName)
        {
            if (!Enum.IsDefined(typeof(RuntimeContentState), state) || state == RuntimeContentState.Unknown)
            {
                throw new ArgumentOutOfRangeException(paramName, state, "Runtime content state must be explicit when a handle is present.");
            }
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
