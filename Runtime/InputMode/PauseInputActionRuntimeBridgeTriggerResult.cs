using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.Common.FlowTriggers;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Result produced when an opt-in Unity InputAction trigger submits to the Pause PlayerInput runtime bridge.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F33B InputAction trigger result for the Pause runtime PlayerInput bridge.")]
    public sealed class PauseInputActionRuntimeBridgeTriggerResult
    {
        public PauseInputActionRuntimeBridgeTriggerResult(
            PauseInputActionRuntimeBridgeTriggerStatus status,
            PauseRequestKind requestKind,
            string actionMapName,
            string actionName,
            bool actionEvidenceRequired,
            bool actionResolved,
            bool bridgeSubmitted,
            PauseInputModeUnityPlayerInputRuntimeBridgeResult bridgeResult,
            string source,
            string reason,
            string message)
        {
            Status = status;
            RequestKind = requestKind;
            ActionMapName = actionMapName.NormalizeText();
            ActionName = actionName.NormalizeText();
            ActionEvidenceRequired = actionEvidenceRequired;
            ActionResolved = actionResolved;
            BridgeSubmitted = bridgeSubmitted;
            BridgeResult = bridgeResult;
            Source = source.NormalizeTextOrFallback(nameof(PauseInputActionRuntimeBridgeTriggerResult));
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public PauseInputActionRuntimeBridgeTriggerStatus Status { get; }

        public PauseRequestKind RequestKind { get; }

        public string ActionMapName { get; }

        public string ActionName { get; }

        public bool ActionEvidenceRequired { get; }

        public bool ActionResolved { get; }

        public bool BridgeSubmitted { get; }

        public PauseInputModeUnityPlayerInputRuntimeBridgeResult BridgeResult { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status == PauseInputActionRuntimeBridgeTriggerStatus.Succeeded;

        public bool Ignored => Status == PauseInputActionRuntimeBridgeTriggerStatus.IgnoredBridgeResult;

        public bool Failed => !Succeeded && !Ignored;

        public InputModeKind RequestedMode => BridgeResult == null ? InputModeKind.Unknown : BridgeResult.RequestedMode;

        public InputModeUnityApplicationPlanOperation Operation => BridgeResult == null
            ? InputModeUnityApplicationPlanOperation.NoOperation
            : BridgeResult.Operation;

        public UnityInputActionMapName AppliedActionMapName => BridgeResult == null
            ? UnityInputActionMapName.From(string.Empty)
            : BridgeResult.AppliedActionMapName;

        public bool Applied => BridgeResult != null && BridgeResult.Applied;

        public bool ActivatedPlayerInput => BridgeResult != null && BridgeResult.ActivatedPlayerInput;

        public bool SelectedActionMap => BridgeResult != null && BridgeResult.SelectedActionMap;

        public bool DeactivatedPlayerInput => BridgeResult != null && BridgeResult.DeactivatedPlayerInput;

        public bool SwitchesActionMaps => BridgeResult != null && BridgeResult.SwitchesActionMaps;

        public bool AppliesInputBehavior => BridgeResult != null && BridgeResult.AppliesInputBehavior;

        public bool PauseRuntimeWiring => BridgeResult != null && BridgeResult.PauseRuntimeWiring;

        public bool CallsPlayerJoin => false;

        public bool SpawnsActor => false;

        public bool UsesCustomInputManager => false;

        public int IssueCount
        {
            get
            {
                if (BridgeResult != null)
                {
                    return BridgeResult.IssueCount;
                }

                return Failed ? 1 : 0;
            }
        }

        public int BlockingIssueCount
        {
            get
            {
                if (BridgeResult != null)
                {
                    return BridgeResult.BlockingIssueCount;
                }

                return Failed ? 1 : 0;
            }
        }

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "status", Status);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "requestKind", RequestKind);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "actionMap", ActionMapName);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "action", ActionName);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "actionEvidenceRequired", ActionEvidenceRequired);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "actionResolved", ActionResolved);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "bridgeSubmitted", BridgeSubmitted);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "requestedMode", RequestedMode);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "operation", Operation);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "appliedActionMap", AppliedActionMapName);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "applied", Applied);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "activatedPlayerInput", ActivatedPlayerInput);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "selectedActionMap", SelectedActionMap);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "deactivatedPlayerInput", DeactivatedPlayerInput);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "issues", IssueCount);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "blockingIssues", BlockingIssueCount);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "actionMapSwitching", SwitchesActionMaps);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "inputBehavior", AppliesInputBehavior);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "pauseRuntimeWiring", PauseRuntimeWiring);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "playerJoin", CallsPlayerJoin);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "actorSpawning", SpawnsActor);
            FrameworkFlowTriggerDiagnostics.AppendField(builder, "customInputManager", UsesCustomInputManager);

            if (!string.IsNullOrWhiteSpace(Message))
            {
                FrameworkFlowTriggerDiagnostics.AppendField(builder, "message", Message);
            }

            if (BridgeResult != null)
            {
                FrameworkFlowTriggerDiagnostics.AppendField(builder, "bridgeStatus", BridgeResult.Status);
                FrameworkFlowTriggerDiagnostics.AppendField(builder, "bridgePauseStatus", BridgeResult.PauseStatus);
            }

            return builder.ToString();
        }
    }
}
