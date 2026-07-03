using System;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Result for the explicit Pause result to Unity PlayerInput application bridge.
    /// It keeps Pause runtime ownership, PlayerInputManager join/spawn and movement outside this adapter.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32G Pause result to explicit Unity PlayerInput application result.")]
    public sealed class PauseInputModeUnityPlayerInputApplicationResult
    {
        public PauseInputModeUnityPlayerInputApplicationResult(
            PauseInputModeUnityPlayerInputApplicationStatus status,
            PauseResult pauseResult,
            InputModeRequest inputModeRequest,
            InputModeUnityPlayerInputRequestApplicationResult inputModeApplicationResult,
            string source,
            string reason)
        {
            if (!Enum.IsDefined(typeof(PauseInputModeUnityPlayerInputApplicationStatus), status) || status == PauseInputModeUnityPlayerInputApplicationStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Pause InputMode Unity PlayerInput application status must be explicit.");
            }

            Status = status;
            PauseResult = pauseResult;
            InputModeRequest = inputModeRequest;
            InputModeApplicationResult = inputModeApplicationResult;
            Source = source.NormalizeTextOrFallback(nameof(PauseInputModeUnityPlayerInputApplicationResult));
            Reason = reason.NormalizeText();
        }

        public PauseInputModeUnityPlayerInputApplicationStatus Status { get; }

        public PauseResult PauseResult { get; }

        public InputModeRequest InputModeRequest { get; }

        public InputModeUnityPlayerInputRequestApplicationResult InputModeApplicationResult { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool Succeeded => Status == PauseInputModeUnityPlayerInputApplicationStatus.Succeeded;

        public bool Ignored => Status == PauseInputModeUnityPlayerInputApplicationStatus.IgnoredInputModeRequest;

        public bool Failed => !Succeeded && !Ignored;

        public PauseState PauseState => PauseResult.IsValid ? PauseResult.CurrentState : PauseState.Unknown;

        public PauseRequestStatus PauseStatus => PauseResult.IsValid ? PauseResult.Status : PauseRequestStatus.Unknown;

        public InputModeKind RequestedMode => InputModeRequest.TargetMode;

        public InputModeUnityApplicationPlanOperation Operation => InputModeApplicationResult == null
            ? InputModeUnityApplicationPlanOperation.NoOperation
            : InputModeApplicationResult.Operation;

        public UnityInputActionMapName AppliedActionMapName => InputModeApplicationResult == null
            ? UnityInputActionMapName.From(string.Empty)
            : InputModeApplicationResult.AppliedActionMapName;

        public bool Applied => InputModeApplicationResult is { Applied: true };

        public bool ActivatedPlayerInput => InputModeApplicationResult is { ActivatedPlayerInput: true };

        public bool SelectedActionMap => InputModeApplicationResult is { SelectedActionMap: true };

        public bool DeactivatedPlayerInput => InputModeApplicationResult is { DeactivatedPlayerInput: true };

        public bool SwitchesActionMaps => InputModeApplicationResult is { SwitchesActionMaps: true };

        public bool AppliesInputBehavior => InputModeApplicationResult is { AppliesInputBehavior: true };

        public bool CallsPlayerJoin => false;

        public bool SpawnsActor => false;

        public bool UsesCustomInputManager => false;

        public int IssueCount
        {
            get
            {
                int count = InputModeApplicationResult == null ? 0 : InputModeApplicationResult.IssueCount;
                if (Status == PauseInputModeUnityPlayerInputApplicationStatus.FailedPauseResultNotCompleted)
                {
                    count += Math.Max(1, PauseResult.IssueCount);
                }

                return count;
            }
        }

        public int BlockingIssueCount
        {
            get
            {
                int count = InputModeApplicationResult == null ? 0 : InputModeApplicationResult.BlockingIssueCount;
                if (Status == PauseInputModeUnityPlayerInputApplicationStatus.FailedPauseResultNotCompleted)
                {
                    count += Math.Max(1, PauseResult.BlockingIssueCount);
                }

                return count;
            }
        }

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append("status='").Append(Status).Append("'");
            builder.Append(" pauseStatus='").Append(PauseStatus).Append("'");
            builder.Append(" pauseState='").Append(PauseState).Append("'");
            builder.Append(" requestedMode='").Append(RequestedMode).Append("'");
            builder.Append(" operation='").Append(Operation).Append("'");
            builder.Append(" appliedActionMap='").Append(AppliedActionMapName).Append("'");
            builder.Append(" applied='").Append(Applied).Append("'");
            builder.Append(" activatedPlayerInput='").Append(ActivatedPlayerInput).Append("'");
            builder.Append(" selectedActionMap='").Append(SelectedActionMap).Append("'");
            builder.Append(" deactivatedPlayerInput='").Append(DeactivatedPlayerInput).Append("'");
            builder.Append(" issues='").Append(IssueCount).Append("'");
            builder.Append(" blockingIssues='").Append(BlockingIssueCount).Append("'");
            builder.Append(" actionMapSwitching='").Append(SwitchesActionMaps).Append("'");
            builder.Append(" inputBehavior='").Append(AppliesInputBehavior).Append("'");
            builder.Append(" playerJoin='").Append(CallsPlayerJoin).Append("'");
            builder.Append(" actorSpawning='").Append(SpawnsActor).Append("'");
            builder.Append(" customInputManager='").Append(UsesCustomInputManager).Append("'");

            if (InputModeApplicationResult != null)
            {
                builder.Append(" inputModeApplication='").Append(InputModeApplicationResult.Status).Append("'");
            }

            return builder.ToString();
        }
    }
}
