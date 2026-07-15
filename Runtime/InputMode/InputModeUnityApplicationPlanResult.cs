using System;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Side-effect-free plan describing what a Unity Input adapter would apply later.
    /// This result never switches action maps, activates PlayerInput, joins players or spawns actors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32C InputMode Unity application dry-run plan result.")]
    public sealed class InputModeUnityApplicationPlanResult
    {
        public InputModeUnityApplicationPlanResult(
            InputModeUnityApplicationPlanStatus status,
            InputModeKind requestedMode,
            InputModeUnityApplicationPlanOperation operation,
            UnityInputTargetRole targetRole,
            UnityInputActionMapName actionMapName,
            bool actionMapRequired,
            bool actionMapAvailable,
            bool playerActorRequired,
            bool localPlayerProvisioningRequired,
            InputModeUnityApplicationPlanIssue[] issues,
            string source,
            string reason)
        {
            if (!Enum.IsDefined(typeof(InputModeUnityApplicationPlanStatus), status) || status == InputModeUnityApplicationPlanStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "InputMode Unity application plan status must be explicit.");
            }

            if (!Enum.IsDefined(typeof(InputModeUnityApplicationPlanOperation), operation) || operation == InputModeUnityApplicationPlanOperation.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(operation), operation, "InputMode Unity application plan operation must be explicit.");
            }

            Status = status;
            RequestedMode = requestedMode;
            Operation = operation;
            TargetRole = targetRole;
            ActionMapName = actionMapName;
            ActionMapRequired = actionMapRequired;
            ActionMapAvailable = actionMapAvailable;
            PlayerActorRequired = playerActorRequired;
            LocalPlayerProvisioningRequired = localPlayerProvisioningRequired;
            Issues = issues ?? Array.Empty<InputModeUnityApplicationPlanIssue>();
            Source = source.NormalizeTextOrFallback(nameof(InputModeUnityApplicationPlanResult));
            Reason = reason.NormalizeText();
        }

        public InputModeUnityApplicationPlanStatus Status { get; }

        public InputModeKind RequestedMode { get; }

        public InputModeUnityApplicationPlanOperation Operation { get; }

        public UnityInputTargetRole TargetRole { get; }

        public UnityInputActionMapName ActionMapName { get; }

        public bool ActionMapRequired { get; }

        public bool ActionMapAvailable { get; }

        public bool PlayerActorRequired { get; }

        public bool LocalPlayerProvisioningRequired { get; }

        public InputModeUnityApplicationPlanIssue[] Issues { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool Succeeded => Status == InputModeUnityApplicationPlanStatus.Succeeded;

        public bool Failed => !Succeeded;

        public bool WouldSelectActionMap => Operation == InputModeUnityApplicationPlanOperation.SelectActionMap;

        public bool WouldLockInput => Operation == InputModeUnityApplicationPlanOperation.LockInput;

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

        public bool SwitchesActionMaps => false;

        public bool AppliesInputBehavior => false;

        public bool ActivatesPlayerInput => false;

        public bool CallsPlayerJoin => false;

        public bool SpawnsActor => false;

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append("status='").Append(Status).Append("'");
            builder.Append(" requestedMode='").Append(RequestedMode).Append("'");
            builder.Append(" operation='").Append(Operation).Append("'");
            builder.Append(" targetRole='").Append(TargetRole).Append("'");
            builder.Append(" actionMapRequired='").Append(ActionMapRequired).Append("'");
            builder.Append(" actionMapName='").Append(ActionMapName).Append("'");
            builder.Append(" actionMapAvailable='").Append(ActionMapAvailable).Append("'");
            builder.Append(" playerActorRequired='").Append(PlayerActorRequired).Append("'");
            builder.Append(" localPlayerProvisioningRequired='").Append(LocalPlayerProvisioningRequired).Append("'");
            builder.Append(" wouldSelectActionMap='").Append(WouldSelectActionMap).Append("'");
            builder.Append(" wouldLockInput='").Append(WouldLockInput).Append("'");
            builder.Append(" issues='").Append(IssueCount).Append("'");
            builder.Append(" blockingIssues='").Append(BlockingIssueCount).Append("'");
            builder.Append(" actionMapSwitching='").Append(SwitchesActionMaps).Append("'");
            builder.Append(" inputBehavior='").Append(AppliesInputBehavior).Append("'");
            builder.Append(" playerInputActivation='").Append(ActivatesPlayerInput).Append("'");
            builder.Append(" playerJoin='").Append(CallsPlayerJoin).Append("'");
            builder.Append(" actorSpawning='").Append(SpawnsActor).Append("'");
            for (int i = 0; i < Issues.Length; i++)
            {
                builder.Append(" issue[").Append(i).Append("]='").Append(Issues[i]).Append("'");
            }

            return builder.ToString();
        }
    }
}
