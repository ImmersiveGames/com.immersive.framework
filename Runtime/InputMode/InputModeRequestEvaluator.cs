using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Pure evaluator for passive InputMode requests.
    /// It returns deterministic result objects and does not own or mutate Unity Input System state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F30A pure InputMode request evaluator; no owner runtime.")]
    public static class InputModeRequestEvaluator
    {
        public static InputModeRequestResult Preview(InputModeState currentState, InputModeRequest request, string source)
        {
            string normalizedSource = source.NormalizeTextOrFallback(nameof(InputModeRequestEvaluator));

            if (!currentState.IsValid)
            {
                return Failure(
                    InputModeRequestStatus.FailedInvalidCurrentState,
                    request,
                    currentState,
                    InputModeRequestIssue.BlockingIssue(
                        InputModeRequestIssueKind.InvalidCurrentState,
                        currentState.CurrentKind,
                        normalizedSource,
                        "InputMode request requires a valid current state."),
                    normalizedSource);
            }

            if (!request.IsValid)
            {
                return Failure(
                    InputModeRequestStatus.FailedInvalidTargetMode,
                    request,
                    currentState,
                    InputModeRequestIssue.BlockingIssue(
                        InputModeRequestIssueKind.InvalidTargetMode,
                        request.TargetMode,
                        normalizedSource,
                        "InputMode request target mode must be explicit."),
                    normalizedSource);
            }

            if (currentState.CurrentKind == request.TargetMode)
            {
                return new InputModeRequestResult(
                    InputModeRequestStatus.IgnoredAlreadyInMode,
                    request,
                    currentState,
                    currentState,
                    System.Array.Empty<InputModeRequestIssue>(),
                    normalizedSource,
                    request.Reason);
            }

            InputModeDefinition nextMode = InputModeDefinitions.FromKind(request.TargetMode, normalizedSource, request.Reason);
            InputModeState nextState = currentState.WithMode(nextMode, normalizedSource, request.Reason);
            return new InputModeRequestResult(
                InputModeRequestStatus.Succeeded,
                request,
                currentState,
                nextState,
                System.Array.Empty<InputModeRequestIssue>(),
                normalizedSource,
                request.Reason);
        }

        private static InputModeRequestResult Failure(
            InputModeRequestStatus status,
            InputModeRequest request,
            InputModeState currentState,
            InputModeRequestIssue issue,
            string source)
        {
            return new InputModeRequestResult(
                status,
                request,
                currentState,
                currentState,
                new[] { issue },
                source,
                request.Reason);
        }
    }
}
