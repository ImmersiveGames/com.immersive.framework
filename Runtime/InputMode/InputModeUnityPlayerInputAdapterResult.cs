using System;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Result from the first explicit Unity PlayerInput adapter operation.
    /// Unlike F32A-F32C previews, this result may represent a real Unity Input side effect.
    /// It still never owns PlayerInputManager, joins players, spawns actors or moves PlayerActor objects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32D Unity PlayerInput adapter application result.")]
    public sealed class InputModeUnityPlayerInputAdapterResult
    {
        public InputModeUnityPlayerInputAdapterResult(
            InputModeUnityPlayerInputAdapterStatus status,
            InputModeKind requestedMode,
            InputModeUnityApplicationPlanOperation operation,
            UnityInputActionMapName requestedActionMapName,
            UnityInputActionMapName appliedActionMapName,
            bool applied,
            bool selectedActionMap,
            bool deactivatedPlayerInput,
            InputModeUnityPlayerInputAdapterIssue[] issues,
            string source,
            string reason)
        {
            if (!Enum.IsDefined(typeof(InputModeUnityPlayerInputAdapterStatus), status) || status == InputModeUnityPlayerInputAdapterStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unity PlayerInput adapter status must be explicit.");
            }

            if (!Enum.IsDefined(typeof(InputModeUnityApplicationPlanOperation), operation) || operation == InputModeUnityApplicationPlanOperation.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unity PlayerInput adapter operation must be explicit.");
            }

            Status = status;
            RequestedMode = requestedMode;
            Operation = operation;
            RequestedActionMapName = requestedActionMapName;
            AppliedActionMapName = appliedActionMapName;
            Applied = applied;
            SelectedActionMap = selectedActionMap;
            DeactivatedPlayerInput = deactivatedPlayerInput;
            Issues = issues ?? Array.Empty<InputModeUnityPlayerInputAdapterIssue>();
            Source = source.NormalizeTextOrFallback(nameof(InputModeUnityPlayerInputAdapterResult));
            Reason = reason.NormalizeText();
        }

        public InputModeUnityPlayerInputAdapterStatus Status { get; }

        public InputModeKind RequestedMode { get; }

        public InputModeUnityApplicationPlanOperation Operation { get; }

        public UnityInputActionMapName RequestedActionMapName { get; }

        public UnityInputActionMapName AppliedActionMapName { get; }

        public bool Applied { get; }

        public bool SelectedActionMap { get; }

        public bool DeactivatedPlayerInput { get; }

        public InputModeUnityPlayerInputAdapterIssue[] Issues { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool Succeeded => Status == InputModeUnityPlayerInputAdapterStatus.Succeeded;

        public bool Failed => !Succeeded;

        public bool SwitchesActionMaps => Succeeded && SelectedActionMap;

        public bool AppliesInputBehavior => Succeeded && (SelectedActionMap || DeactivatedPlayerInput);

        public bool ActivatesPlayerInput => false;

        public bool CallsPlayerJoin => false;

        public bool SpawnsActor => false;

        public bool UsesCustomInputManager => false;

        public int IssueCount => Issues.Length;

        public int BlockingIssueCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Issues.Length; i++)
                {
                    if (Issues[i].Blocking)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append("status='").Append(Status).Append("'");
            builder.Append(" requestedMode='").Append(RequestedMode).Append("'");
            builder.Append(" operation='").Append(Operation).Append("'");
            builder.Append(" requestedActionMap='").Append(RequestedActionMapName).Append("'");
            builder.Append(" appliedActionMap='").Append(AppliedActionMapName).Append("'");
            builder.Append(" applied='").Append(Applied).Append("'");
            builder.Append(" selectedActionMap='").Append(SelectedActionMap).Append("'");
            builder.Append(" deactivatedPlayerInput='").Append(DeactivatedPlayerInput).Append("'");
            builder.Append(" issues='").Append(IssueCount).Append("'");
            builder.Append(" blockingIssues='").Append(BlockingIssueCount).Append("'");
            builder.Append(" actionMapSwitching='").Append(SwitchesActionMaps).Append("'");
            builder.Append(" inputBehavior='").Append(AppliesInputBehavior).Append("'");
            builder.Append(" playerInputActivation='").Append(ActivatesPlayerInput).Append("'");
            builder.Append(" playerJoin='").Append(CallsPlayerJoin).Append("'");
            builder.Append(" actorSpawning='").Append(SpawnsActor).Append("'");
            builder.Append(" customInputManager='").Append(UsesCustomInputManager).Append("'");
            for (int i = 0; i < Issues.Length; i++)
            {
                builder.Append(" issue[").Append(i).Append("]='").Append(Issues[i]).Append("'");
            }

            return builder.ToString();
        }
    }
}
