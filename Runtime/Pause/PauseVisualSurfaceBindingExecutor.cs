using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Common;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Executes the logical binding portion of a Pause visual surface contract.
    /// It declares a logical RuntimeContent handle and binds it to an authored ContentAnchor through the host-owned binding runtime.
    /// It does not instantiate the Pause prefab, move transforms, change Pause state, change InputMode, alter Time.timeScale or wire into Route/Activity lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10D Pause visual surface ContentAnchor binding executor; logical explicit binding only.")]
    internal static class PauseVisualSurfaceBindingExecutor
    {
        private const string DefaultSource = nameof(PauseVisualSurfaceBindingExecutor);
        private const string DefaultReason = "pause.visual.surface.binding.execution";

        internal static PauseVisualSurfaceBindingExecutionResult Execute(
            PauseVisualSurfaceContract contract,
            ContentAnchorSet anchorSet,
            RuntimeScopeContext runtimeContext,
            FrameworkRuntimeHost runtimeHost,
            string source,
            string reason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(DefaultSource);
            string normalizedReason = reason.NormalizeTextOrFallback(DefaultReason);

            if (runtimeHost == null)
            {
                return PauseVisualSurfaceBindingExecutionResult.Failure(
                    PauseVisualSurfaceBindingExecutionStatus.FailedMissingRuntimeHost,
                    contract,
                    default,
                    null,
                    default,
                    false,
                    false,
                    normalizedSource,
                    normalizedReason,
                    "Pause visual surface binding execution failed because the FrameworkRuntimeHost is missing.");
            }

            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                return PauseVisualSurfaceBindingExecutionResult.Failure(
                    PauseVisualSurfaceBindingExecutionStatus.FailedMissingRuntimeContentRuntime,
                    contract,
                    default,
                    null,
                    default,
                    false,
                    false,
                    normalizedSource,
                    normalizedReason,
                    "Pause visual surface binding execution failed because RuntimeContentRuntime is missing.");
            }

            if (!contract.IsValid)
            {
                return PauseVisualSurfaceBindingExecutionResult.Failure(
                    PauseVisualSurfaceBindingExecutionStatus.FailedInvalidContract,
                    contract,
                    default,
                    null,
                    default,
                    false,
                    false,
                    normalizedSource,
                    normalizedReason,
                    "Pause visual surface binding execution rejected an invalid contract.");
            }

            if (!runtimeContext.IsValid || runtimeContext.Owner != contract.RuntimeOwner)
            {
                return PauseVisualSurfaceBindingExecutionResult.Failure(
                    PauseVisualSurfaceBindingExecutionStatus.FailedInvalidRuntimeContext,
                    contract,
                    default,
                    null,
                    default,
                    false,
                    false,
                    normalizedSource,
                    normalizedReason,
                    "Pause visual surface binding execution rejected an invalid or mismatched runtime context.");
            }

            var requestResult = PauseVisualSurfaceBindingRequestFactory.Create(
                contract,
                runtimeContext,
                normalizedSource,
                normalizedReason);
            if (!requestResult.Succeeded)
            {
                return PauseVisualSurfaceBindingExecutionResult.Failure(
                    PauseVisualSurfaceBindingExecutionStatus.FailedBindingRequest,
                    contract,
                    requestResult,
                    null,
                    default,
                    false,
                    false,
                    normalizedSource,
                    normalizedReason,
                    "Pause visual surface binding execution failed before binding because the binding request could not be created.");
            }

            var declarationResult = runtimeContentRuntime.DeclareHandle(
                runtimeContext,
                contract.RuntimeContentId,
                normalizedSource,
                normalizedReason);
            if (declarationResult == null || declarationResult.Status != RuntimeRootRegistryOperationStatus.HandleRegistered)
            {
                return PauseVisualSurfaceBindingExecutionResult.Failure(
                    PauseVisualSurfaceBindingExecutionStatus.FailedRuntimeHandleDeclaration,
                    contract,
                    requestResult,
                    declarationResult,
                    default,
                    false,
                    false,
                    normalizedSource,
                    normalizedReason,
                    "Pause visual surface binding execution failed because the logical RuntimeContent handle could not be declared.");
            }

            var bindingResult = runtimeHost.BindContentAnchor(
                anchorSet,
                requestResult.Request,
                normalizedSource,
                normalizedReason);
            if (!bindingResult.Succeeded)
            {
                runtimeContentRuntime.UnregisterHandle(
                    runtimeContext,
                    requestResult.Request.RuntimeIdentity,
                    normalizedSource,
                    normalizedReason + ".rollback");

                return PauseVisualSurfaceBindingExecutionResult.Failure(
                    PauseVisualSurfaceBindingExecutionStatus.FailedBindingExecution,
                    contract,
                    requestResult,
                    declarationResult,
                    bindingResult,
                    true,
                    false,
                    normalizedSource,
                    normalizedReason,
                    "Pause visual surface binding execution failed and the declared RuntimeContent handle was rolled back.");
            }

            return PauseVisualSurfaceBindingExecutionResult.Success(
                contract,
                requestResult,
                declarationResult,
                bindingResult,
                normalizedSource,
                normalizedReason,
                "Pause visual surface binding executed logically without visual materialization.");
        }
    }
}
