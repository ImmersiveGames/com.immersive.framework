using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.UnityInput;
using UnityEngine.InputSystem;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// Explicit end-to-end InputMode request application pipeline for one Unity PlayerInput.
    /// The optional persistent map list is applied together with the mode-specific primary map.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Experimental,
        "IC4 InputMode request to exact layered Unity PlayerInput posture.")]
    public static class InputModeUnityPlayerInputRequestApplication
    {
        public static InputModeUnityPlayerInputRequestApplicationResult Apply(
            InputModeState currentState,
            InputModeRequest request,
            UnityInputTargetSet targetSet,
            PlayerActorSet playerActorSet,
            LocalPlayerProvisioningValidationResult localPlayerProvisioningValidation,
            UnityInputActionMapEvidence actionMapEvidence,
            InputModeUnityActionMapBinding[] actionMapBindings,
            PlayerInput playerInput,
            string source,
            string reason,
            UnityInputActionMapName[] persistentActionMapNames = null)
        {
            string normalizedSource = source.NormalizeTextOrFallback(
                nameof(InputModeUnityPlayerInputRequestApplication));
            string normalizedReason = reason.NormalizeText();

            InputModeRequestResult requestResult = InputModeRequestEvaluator.Preview(
                currentState,
                request,
                normalizedSource);
            InputModeKind requestedMode = request.TargetMode;

            if (requestResult.Ignored)
            {
                return CreateResult(
                    InputModeUnityPlayerInputRequestApplicationStatus.IgnoredInputModeRequest,
                    requestedMode,
                    requestResult,
                    null,
                    null,
                    null,
                    null,
                    normalizedSource,
                    normalizedReason);
            }

            if (!requestResult.Succeeded)
            {
                return CreateResult(
                    InputModeUnityPlayerInputRequestApplicationStatus.FailedInputModeRequest,
                    requestedMode,
                    requestResult,
                    null,
                    null,
                    null,
                    null,
                    normalizedSource,
                    normalizedReason);
            }

            InputModeUnityApplicationPreviewResult applicationPreview =
                InputModeUnityApplicationPreviewEvaluator.Preview(
                    requestResult,
                    targetSet,
                    playerActorSet,
                    localPlayerProvisioningValidation,
                    normalizedSource,
                    normalizedReason);
            if (!applicationPreview.Succeeded)
            {
                return CreateResult(
                    InputModeUnityPlayerInputRequestApplicationStatus.FailedUnityApplicationPreview,
                    requestedMode,
                    requestResult,
                    applicationPreview,
                    null,
                    null,
                    null,
                    normalizedSource,
                    normalizedReason);
            }

            InputModeUnityActionMapPreviewResult actionMapPreview =
                InputModeUnityActionMapPreviewEvaluator.Preview(
                    applicationPreview,
                    actionMapEvidence,
                    actionMapBindings,
                    normalizedSource,
                    normalizedReason);
            if (!actionMapPreview.Succeeded)
            {
                return CreateResult(
                    InputModeUnityPlayerInputRequestApplicationStatus.FailedActionMapPreview,
                    requestedMode,
                    requestResult,
                    applicationPreview,
                    actionMapPreview,
                    null,
                    null,
                    normalizedSource,
                    normalizedReason);
            }

            InputModeUnityApplicationPlanResult plan =
                InputModeUnityApplicationPlanEvaluator.BuildPlan(
                    applicationPreview,
                    actionMapPreview,
                    normalizedSource,
                    normalizedReason);
            if (!plan.Succeeded)
            {
                return CreateResult(
                    InputModeUnityPlayerInputRequestApplicationStatus.FailedApplicationPlan,
                    requestedMode,
                    requestResult,
                    applicationPreview,
                    actionMapPreview,
                    plan,
                    null,
                    normalizedSource,
                    normalizedReason);
            }

            InputModeUnityPlayerInputApplicationResult application =
                InputModeUnityPlayerInputApplication.Apply(
                    plan,
                    playerInput,
                    normalizedSource,
                    normalizedReason,
                    persistentActionMapNames);

            return CreateResult(
                application.Succeeded
                    ? InputModeUnityPlayerInputRequestApplicationStatus.Succeeded
                    : InputModeUnityPlayerInputRequestApplicationStatus.FailedPlayerInputApplication,
                requestedMode,
                requestResult,
                applicationPreview,
                actionMapPreview,
                plan,
                application,
                normalizedSource,
                normalizedReason);
        }

        private static InputModeUnityPlayerInputRequestApplicationResult CreateResult(
            InputModeUnityPlayerInputRequestApplicationStatus status,
            InputModeKind requestedMode,
            InputModeRequestResult inputModeRequestResult,
            InputModeUnityApplicationPreviewResult applicationPreviewResult,
            InputModeUnityActionMapPreviewResult actionMapPreviewResult,
            InputModeUnityApplicationPlanResult applicationPlanResult,
            InputModeUnityPlayerInputApplicationResult playerInputApplicationResult,
            string source,
            string reason)
        {
            return new InputModeUnityPlayerInputRequestApplicationResult(
                status,
                requestedMode,
                inputModeRequestResult,
                applicationPreviewResult,
                actionMapPreviewResult,
                applicationPlanResult,
                playerInputApplicationResult,
                source,
                reason);
        }
    }
}
