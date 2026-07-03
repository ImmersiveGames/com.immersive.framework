using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Aggregated result for the Pause/InputMode apply boundary.
    /// It preserves the original Pause, preflight and PlayerInput application evidence.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F38 aggregated Pause/InputMode apply result.")]
    public sealed class PauseInputModeApplyResult
    {
        internal PauseInputModeApplyResult(
            PauseInputModeUnityPlayerInputRuntimeBridgeStatus status,
            PauseInputModeApplyStage failedStage,
            PauseRequestKind requestKind,
            PauseState previousPauseState,
            PauseState targetPauseState,
            PauseResult pauseResult,
            InputModeUnityApplicationPlanResult preflightPlanResult,
            PauseInputModeUnityPlayerInputApplicationResult applicationResult,
            UnityInputActionMapName previousActionMapName,
            string source,
            string reason,
            string message)
        {
            Status = status;
            FailedStage = failedStage;
            RequestKind = requestKind;
            PreviousPauseState = previousPauseState;
            TargetPauseState = targetPauseState;
            PauseResult = pauseResult;
            PreflightPlanResult = preflightPlanResult;
            ApplicationResult = applicationResult;
            PreviousActionMapName = previousActionMapName;
            Source = source.NormalizeTextOrFallback(nameof(PauseInputModeApplyResult));
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public PauseInputModeUnityPlayerInputRuntimeBridgeStatus Status { get; }

        public PauseInputModeApplyStage FailedStage { get; }

        public PauseRequestKind RequestKind { get; }

        public PauseState PreviousPauseState { get; }

        public PauseState TargetPauseState { get; }

        public PauseResult PauseResult { get; }

        public InputModeUnityApplicationPlanResult PreflightPlanResult { get; }

        public PauseInputModeUnityPlayerInputApplicationResult ApplicationResult { get; }

        public UnityInputActionMapName PreviousActionMapName { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status == PauseInputModeUnityPlayerInputRuntimeBridgeStatus.Succeeded;

        public bool Ignored => Status == PauseInputModeUnityPlayerInputRuntimeBridgeStatus.IgnoredInputModeRequest;

        public bool Failed => !Succeeded && !Ignored;

        public bool PauseRequestSubmitted => PauseResult.IsValid;

        public PauseRequestStatus PauseStatus => PauseResult.IsValid ? PauseResult.Status : PauseRequestStatus.Unknown;

        public PauseState CurrentPauseState => PauseResult.IsValid ? PauseResult.CurrentState : TargetPauseState;

        public InputModeKind RequestedMode
        {
            get
            {
                if (ApplicationResult != null)
                {
                    return ApplicationResult.RequestedMode;
                }

                if (PreflightPlanResult != null)
                {
                    return PreflightPlanResult.RequestedMode;
                }

                return MapPauseStateToInputMode(TargetPauseState);
            }
        }

        public InputModeUnityApplicationPlanOperation Operation
        {
            get
            {
                if (ApplicationResult != null)
                {
                    return ApplicationResult.Operation;
                }

                return PreflightPlanResult == null
                    ? InputModeUnityApplicationPlanOperation.NoOperation
                    : PreflightPlanResult.Operation;
            }
        }

        public UnityInputActionMapName AppliedActionMapName => ApplicationResult == null
            ? UnityInputActionMapName.From(string.Empty)
            : ApplicationResult.AppliedActionMapName;

        public InputModeUnityPlayerInputAdapterResult AdapterResult => ApplicationResult?.InputModeApplicationResult?.PlayerInputApplicationResult?.AdapterResult;

        public bool Applied => ApplicationResult is { Applied: true };

        public bool ActivatedPlayerInput => ApplicationResult is { ActivatedPlayerInput: true };

        public bool SelectedActionMap => ApplicationResult is { SelectedActionMap: true };

        public bool DeactivatedPlayerInput => ApplicationResult is { DeactivatedPlayerInput: true };

        public bool SwitchesActionMaps => ApplicationResult is { SwitchesActionMaps: true };

        public bool AppliesInputBehavior => ApplicationResult is { AppliesInputBehavior: true };

        public bool PauseRuntimeWiring => FailedStage != PauseInputModeApplyStage.MissingRuntimeHost
            && FailedStage != PauseInputModeApplyStage.MissingPauseRuntime;

        public bool CallsPlayerJoin => false;

        public bool SpawnsActor => false;

        public bool UsesCustomInputManager => false;

        public int IssueCount
        {
            get
            {
                if (ApplicationResult != null)
                {
                    return ApplicationResult.IssueCount;
                }

                if (PreflightPlanResult != null)
                {
                    return PreflightPlanResult.IssueCount;
                }

                return Failed ? 1 : 0;
            }
        }

        public int BlockingIssueCount
        {
            get
            {
                if (ApplicationResult != null)
                {
                    return ApplicationResult.BlockingIssueCount;
                }

                if (PreflightPlanResult != null)
                {
                    return PreflightPlanResult.BlockingIssueCount;
                }

                return Failed ? 1 : 0;
            }
        }

        public PauseInputModeUnityPlayerInputRuntimeBridgeResult ToRuntimeBridgeResult()
        {
            return new PauseInputModeUnityPlayerInputRuntimeBridgeResult(
                Status,
                RequestKind,
                PreviousPauseState,
                TargetPauseState,
                PauseResult,
                PreflightPlanResult,
                ApplicationResult,
                Source,
                Reason,
                Message,
                FailedStage,
                this);
        }

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append("status='").Append(Status).Append("'");
            builder.Append(" failedStage='").Append(FailedStage).Append("'");
            builder.Append(" requestKind='").Append(RequestKind).Append("'");
            builder.Append(" previousPauseState='").Append(PreviousPauseState).Append("'");
            builder.Append(" targetPauseState='").Append(TargetPauseState).Append("'");
            builder.Append(" currentPauseState='").Append(CurrentPauseState).Append("'");
            builder.Append(" pauseStatus='").Append(PauseStatus).Append("'");
            builder.Append(" pauseRequestSubmitted='").Append(PauseRequestSubmitted).Append("'");
            builder.Append(" requestedMode='").Append(RequestedMode).Append("'");
            builder.Append(" operation='").Append(Operation).Append("'");
            builder.Append(" previousActionMap='").Append(PreviousActionMapName).Append("'");
            builder.Append(" appliedActionMap='").Append(AppliedActionMapName).Append("'");
            builder.Append(" applied='").Append(Applied).Append("'");
            builder.Append(" activatedPlayerInput='").Append(ActivatedPlayerInput).Append("'");
            builder.Append(" selectedActionMap='").Append(SelectedActionMap).Append("'");
            builder.Append(" deactivatedPlayerInput='").Append(DeactivatedPlayerInput).Append("'");
            builder.Append(" issues='").Append(IssueCount).Append("'");
            builder.Append(" blockingIssues='").Append(BlockingIssueCount).Append("'");
            builder.Append(" actionMapSwitching='").Append(SwitchesActionMaps).Append("'");
            builder.Append(" inputBehavior='").Append(AppliesInputBehavior).Append("'");
            builder.Append(" pauseRuntimeWiring='").Append(PauseRuntimeWiring).Append("'");
            builder.Append(" playerJoin='").Append(CallsPlayerJoin).Append("'");
            builder.Append(" actorSpawning='").Append(SpawnsActor).Append("'");
            builder.Append(" customInputManager='").Append(UsesCustomInputManager).Append("'");

            if (!string.IsNullOrWhiteSpace(Message))
            {
                builder.Append(" message='").Append(Message.ToDiagnosticText()).Append("'");
            }

            return builder.ToString();
        }

        private static InputModeKind MapPauseStateToInputMode(PauseState state)
        {
            switch (state)
            {
                case PauseState.Running:
                    return InputModeKind.Gameplay;
                case PauseState.Paused:
                    return InputModeKind.PauseOverlay;
                default:
                    return InputModeKind.Unknown;
            }
        }
    }
}
