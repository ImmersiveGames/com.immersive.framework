using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Diagnostic result for explicit Pause visual surface materialization.
    /// It records the request, materialization pipeline and explicit side effects, but it does not represent Pause state, InputMode or Time.timeScale behavior.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10E Pause visual surface materialization result; explicit visual proof only.")]
    internal readonly struct PauseVisualSurfaceMaterializationResult
    {
        internal PauseVisualSurfaceMaterializationResult(
            PauseVisualSurfaceMaterializationStatus status,
            PauseVisualSurfaceContract contract,
            PauseVisualSurfaceBindingRequestResult requestResult,
            UnityContentAnchorMaterializationPipelineResult pipelineResult,
            int registryEntries,
            int registryActive,
            bool materializationAttempted,
            string source,
            string reason,
            string message)
        {
            Status = status;
            Contract = contract;
            RequestResult = requestResult;
            PipelineResult = pipelineResult;
            RegistryEntries = registryEntries;
            RegistryActive = registryActive;
            MaterializationAttempted = materializationAttempted;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public PauseVisualSurfaceMaterializationStatus Status { get; }

        public PauseVisualSurfaceContract Contract { get; }

        public PauseVisualSurfaceBindingRequestResult RequestResult { get; }

        public UnityContentAnchorMaterializationPipelineResult PipelineResult { get; }

        public int RegistryEntries { get; }

        public int RegistryActive { get; }

        public bool MaterializationAttempted { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status == PauseVisualSurfaceMaterializationStatus.SucceededMaterialized;

        public bool Failed => !Succeeded;

        public bool RequestCreated => RequestResult.Succeeded && RequestResult.HasRequest;

        public bool PipelineSucceeded => PipelineResult.Succeeded;

        public bool PhysicalPlacementApplied => PipelineResult.PhysicalPlacementApplied;

        public bool LogicalBindingSucceeded => PipelineResult.HasBindingResult && PipelineResult.BindingResult.Succeeded;

        public bool LogicalRuntimeContentMaterialized => PipelineResult.HasAppliedMaterializationResult
            && PipelineResult.AppliedMaterializationResult.Succeeded
            && PipelineResult.AppliedMaterializationResult.HasHandle;

        public ContentAnchorBindingRequest Request => RequestResult.Request;

        public RuntimeContentIdentity RuntimeIdentity => RequestCreated ? Request.RuntimeIdentity : default;

        public string ToDiagnosticString()
        {
            string requestStatus = RequestResult.Status.ToString();
            string pipelineStatus = PipelineResult.Status.ToString();
            string sourceText = Source.ToDiagnosticText();
            string reasonText = Reason.ToDiagnosticText();
            string messageText = Message.ToDiagnosticText();
            return $"status='{Status}' succeeded='{Succeeded}' request='{requestStatus}' pipeline='{pipelineStatus}' registryEntries='{RegistryEntries}' registryActive='{RegistryActive}' materializationAttempted='{MaterializationAttempted}' physicalPlacementApplied='{PhysicalPlacementApplied}' logicalBinding='{LogicalBindingSucceeded}' logicalRuntimeContentMaterialized='{LogicalRuntimeContentMaterialized}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        internal static PauseVisualSurfaceMaterializationResult Success(
            PauseVisualSurfaceContract contract,
            PauseVisualSurfaceBindingRequestResult requestResult,
            UnityContentAnchorMaterializationPipelineResult pipelineResult,
            int registryEntries,
            int registryActive,
            string source,
            string reason,
            string message)
        {
            return new PauseVisualSurfaceMaterializationResult(
                PauseVisualSurfaceMaterializationStatus.SucceededMaterialized,
                contract,
                requestResult,
                pipelineResult,
                registryEntries,
                registryActive,
                true,
                source,
                reason,
                message);
        }

        internal static PauseVisualSurfaceMaterializationResult Failure(
            PauseVisualSurfaceMaterializationStatus status,
            PauseVisualSurfaceContract contract,
            PauseVisualSurfaceBindingRequestResult requestResult,
            UnityContentAnchorMaterializationPipelineResult pipelineResult,
            int registryEntries,
            int registryActive,
            bool materializationAttempted,
            string source,
            string reason,
            string message)
        {
            return new PauseVisualSurfaceMaterializationResult(
                status,
                contract,
                requestResult,
                pipelineResult,
                registryEntries,
                registryActive,
                materializationAttempted,
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
