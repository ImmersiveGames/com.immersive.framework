using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.ContentAnchor;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Passive result for creating a ContentAnchor binding request from a Pause visual surface contract.
    /// It does not execute logical binding, materialization, physical placement, release, input mode switching or pause state changes.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10C Pause visual surface binding request result; passive request derivation only.")]
    public readonly struct PauseVisualSurfaceBindingRequestResult
    {
        private PauseVisualSurfaceBindingRequestResult(
            PauseVisualSurfaceBindingRequestStatus status,
            PauseVisualSurfaceContract contract,
            ContentAnchorBindingRequest request,
            string source,
            string reason,
            string message)
        {
            Status = status;
            Contract = contract;
            Request = request;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public PauseVisualSurfaceBindingRequestStatus Status { get; }

        public PauseVisualSurfaceContract Contract { get; }

        public ContentAnchorBindingRequest Request { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status == PauseVisualSurfaceBindingRequestStatus.SucceededCreated;

        public bool Failed => !Succeeded;

        public bool HasRequest => Request.IsValid;

        public string ToDiagnosticString()
        {
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            string requestText = HasRequest ? Request.ToDiagnosticString() : "<none>";
            return $"status='{Status}' succeeded='{Succeeded}' request=\"{requestText}\" source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static PauseVisualSurfaceBindingRequestResult Success(
            PauseVisualSurfaceContract contract,
            ContentAnchorBindingRequest request,
            string source,
            string reason,
            string message)
        {
            return new PauseVisualSurfaceBindingRequestResult(
                PauseVisualSurfaceBindingRequestStatus.SucceededCreated,
                contract,
                request,
                source,
                reason,
                message);
        }

        public static PauseVisualSurfaceBindingRequestResult Failure(
            PauseVisualSurfaceBindingRequestStatus status,
            PauseVisualSurfaceContract contract,
            string source,
            string reason,
            string message)
        {
            return new PauseVisualSurfaceBindingRequestResult(
                status,
                contract,
                default,
                source,
                reason,
                message);
        }

        private static string Normalize(string value)
        {
            return value.NormalizeText();
        }
    }
}
