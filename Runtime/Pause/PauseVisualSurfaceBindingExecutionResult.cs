using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Diagnostic result for explicit Pause visual surface ContentAnchor binding execution.
    /// The result records logical runtime handle declaration and ContentAnchor binding only; physical/materialized UI remains out of scope.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10D Pause ContentAnchor binding execution result; logical binding only.")]
    internal readonly struct PauseVisualSurfaceBindingExecutionResult
    {
        internal PauseVisualSurfaceBindingExecutionResult(
            PauseVisualSurfaceBindingExecutionStatus status,
            PauseVisualSurfaceContract contract,
            PauseVisualSurfaceBindingRequestResult requestResult,
            RuntimeRootRegistryOperationResult runtimeHandleDeclaration,
            ContentAnchorBindingResult bindingResult,
            bool runtimeHandleDeclared,
            bool bindingExecuted,
            string source,
            string reason,
            string message)
        {
            Status = status;
            Contract = contract;
            RequestResult = requestResult;
            RuntimeHandleDeclaration = runtimeHandleDeclaration;
            BindingResult = bindingResult;
            RuntimeHandleDeclared = runtimeHandleDeclared;
            BindingExecuted = bindingExecuted;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public PauseVisualSurfaceBindingExecutionStatus Status { get; }

        public PauseVisualSurfaceContract Contract { get; }

        public PauseVisualSurfaceBindingRequestResult RequestResult { get; }

        internal RuntimeRootRegistryOperationResult RuntimeHandleDeclaration { get; }

        public ContentAnchorBindingResult BindingResult { get; }

        public bool RuntimeHandleDeclared { get; }

        public bool BindingExecuted { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status == PauseVisualSurfaceBindingExecutionStatus.SucceededBound;

        public bool Failed => !Succeeded;

        public bool HasBinding => BindingResult.Succeeded && BindingResult.HasHandle;

        public bool RequestCreated => RequestResult.Succeeded && RequestResult.HasRequest;

        public ContentAnchorBindingRequest Request => RequestResult.Request;

        public string ToDiagnosticString()
        {
            string declarationStatus = RuntimeHandleDeclaration != null ? RuntimeHandleDeclaration.Status.ToString() : "<none>";
            string bindingStatus = BindingResult.Status.ToString();
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            return $"status='{Status}' succeeded='{Succeeded}' request='{RequestResult.Status}' runtimeHandleDeclaration='{declarationStatus}' binding='{bindingStatus}' runtimeHandleDeclared='{RuntimeHandleDeclared}' bindingExecuted='{BindingExecuted}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        internal static PauseVisualSurfaceBindingExecutionResult Success(
            PauseVisualSurfaceContract contract,
            PauseVisualSurfaceBindingRequestResult requestResult,
            RuntimeRootRegistryOperationResult runtimeHandleDeclaration,
            ContentAnchorBindingResult bindingResult,
            string source,
            string reason,
            string message)
        {
            return new PauseVisualSurfaceBindingExecutionResult(
                PauseVisualSurfaceBindingExecutionStatus.SucceededBound,
                contract,
                requestResult,
                runtimeHandleDeclaration,
                bindingResult,
                true,
                true,
                source,
                reason,
                message);
        }

        internal static PauseVisualSurfaceBindingExecutionResult Failure(
            PauseVisualSurfaceBindingExecutionStatus status,
            PauseVisualSurfaceContract contract,
            PauseVisualSurfaceBindingRequestResult requestResult,
            RuntimeRootRegistryOperationResult runtimeHandleDeclaration,
            ContentAnchorBindingResult bindingResult,
            bool runtimeHandleDeclared,
            bool bindingExecuted,
            string source,
            string reason,
            string message)
        {
            return new PauseVisualSurfaceBindingExecutionResult(
                status,
                contract,
                requestResult,
                runtimeHandleDeclaration,
                bindingResult,
                runtimeHandleDeclared,
                bindingExecuted,
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
