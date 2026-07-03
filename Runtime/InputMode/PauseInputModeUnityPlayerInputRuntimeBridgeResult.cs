using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Result produced by the explicit opt-in Pause runtime to Unity PlayerInput bridge.
    /// The result records Pause runtime wiring and PlayerInput application, but never reports PlayerInputManager join/spawn ownership.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F33A opt-in Pause runtime PlayerInput wiring result.")]
    public sealed class PauseInputModeUnityPlayerInputRuntimeBridgeResult
    {
        public PauseInputModeUnityPlayerInputRuntimeBridgeResult(
            PauseInputModeUnityPlayerInputRuntimeBridgeStatus status,
            PauseRequestKind requestKind,
            PauseState previousPauseState,
            PauseState targetPauseState,
            PauseResult pauseResult,
            InputModeUnityApplicationPlanResult preflightPlanResult,
            PauseInputModeUnityPlayerInputApplicationResult applicationResult,
            string source,
            string reason,
            string message,
            PauseInputModeApplyStage failedStage = PauseInputModeApplyStage.Unknown,
            PauseInputModeApplyResult applyResult = null)
        {
            Status = status;
            RequestKind = requestKind;
            PreviousPauseState = previousPauseState;
            TargetPauseState = targetPauseState;
            PauseResult = pauseResult;
            PreflightPlanResult = preflightPlanResult;
            ApplicationResult = applicationResult;
            Source = source.NormalizeTextOrFallback(nameof(PauseInputModeUnityPlayerInputRuntimeBridgeResult));
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
            FailedStage = failedStage == PauseInputModeApplyStage.Unknown
                ? ResolveFailedStage(status)
                : failedStage;
            ApplyResult = applyResult;
        }

        public PauseInputModeUnityPlayerInputRuntimeBridgeStatus Status { get; }

        public PauseRequestKind RequestKind { get; }

        public PauseState PreviousPauseState { get; }

        public PauseState TargetPauseState { get; }

        public PauseResult PauseResult { get; }

        public InputModeUnityApplicationPlanResult PreflightPlanResult { get; }

        public PauseInputModeUnityPlayerInputApplicationResult ApplicationResult { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public PauseInputModeApplyStage FailedStage { get; }

        public PauseInputModeApplyResult ApplyResult { get; }

        public bool Succeeded => Status == PauseInputModeUnityPlayerInputRuntimeBridgeStatus.Succeeded;

        public bool Ignored => Status == PauseInputModeUnityPlayerInputRuntimeBridgeStatus.IgnoredInputModeRequest;

        public bool Failed => !Succeeded && !Ignored;

        public bool PauseRuntimeWiring => Status != PauseInputModeUnityPlayerInputRuntimeBridgeStatus.FailedRuntimeUnavailable;

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

        public UnityInputActionMapName PreviousActionMapName => ApplyResult == null
            ? UnityInputActionMapName.From(string.Empty)
            : ApplyResult.PreviousActionMapName;

        public InputModeUnityPlayerInputAdapterResult AdapterResult => ApplyResult == null
            ? ApplicationResult?.InputModeApplicationResult?.PlayerInputApplicationResult?.AdapterResult
            : ApplyResult.AdapterResult;

        public bool Applied => ApplicationResult is { Applied: true };

        public bool ActivatedPlayerInput => ApplicationResult is { ActivatedPlayerInput: true };

        public bool SelectedActionMap => ApplicationResult is { SelectedActionMap: true };

        public bool DeactivatedPlayerInput => ApplicationResult is { DeactivatedPlayerInput: true };

        public bool SwitchesActionMaps => ApplicationResult is { SwitchesActionMaps: true };

        public bool AppliesInputBehavior => ApplicationResult is { AppliesInputBehavior: true };

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

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append("status='").Append(Status).Append("'");
            builder.Append(" requestKind='").Append(RequestKind).Append("'");
            builder.Append(" previousPauseState='").Append(PreviousPauseState).Append("'");
            builder.Append(" targetPauseState='").Append(TargetPauseState).Append("'");
            builder.Append(" currentPauseState='").Append(CurrentPauseState).Append("'");
            builder.Append(" pauseStatus='").Append(PauseStatus).Append("'");
            builder.Append(" pauseRequestSubmitted='").Append(PauseRequestSubmitted).Append("'");
            builder.Append(" failedStage='").Append(FailedStage).Append("'");
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

        private static PauseInputModeApplyStage ResolveFailedStage(PauseInputModeUnityPlayerInputRuntimeBridgeStatus status)
        {
            switch (status)
            {
                case PauseInputModeUnityPlayerInputRuntimeBridgeStatus.Succeeded:
                case PauseInputModeUnityPlayerInputRuntimeBridgeStatus.IgnoredInputModeRequest:
                    return PauseInputModeApplyStage.None;
                case PauseInputModeUnityPlayerInputRuntimeBridgeStatus.FailedRuntimeUnavailable:
                    return PauseInputModeApplyStage.MissingRuntimeHost;
                case PauseInputModeUnityPlayerInputRuntimeBridgeStatus.FailedConfiguration:
                    return PauseInputModeApplyStage.InvalidRequest;
                case PauseInputModeUnityPlayerInputRuntimeBridgeStatus.FailedPreflight:
                    return PauseInputModeApplyStage.PreflightRejected;
                case PauseInputModeUnityPlayerInputRuntimeBridgeStatus.FailedPauseRequest:
                    return PauseInputModeApplyStage.PauseRequestFailed;
                case PauseInputModeUnityPlayerInputRuntimeBridgeStatus.FailedInputModePlayerInputApplication:
                    return PauseInputModeApplyStage.AdapterApplyFailed;
                default:
                    return PauseInputModeApplyStage.Unknown;
            }
        }
    }
}
