
using System;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Preview result that resolves a logical InputMode to a Unity action map name without switching action maps.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32B InputMode Unity action map preview result.")]
    public sealed class InputModeUnityActionMapPreviewResult
    {
        public InputModeUnityActionMapPreviewResult(
            InputModeUnityActionMapPreviewStatus status,
            InputModeKind requestedMode,
            UnityInputActionMapName actionMapName,
            bool actionMapRequired,
            bool actionMapAvailable,
            bool hasActionAsset,
            int availableActionMapCount,
            InputModeUnityActionMapPreviewIssue[] issues,
            string source,
            string reason)
        {
            if (!Enum.IsDefined(typeof(InputModeUnityActionMapPreviewStatus), status) || status == InputModeUnityActionMapPreviewStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "InputMode Unity action map preview status must be explicit.");
            }

            Status = status;
            RequestedMode = requestedMode;
            ActionMapName = actionMapName;
            ActionMapRequired = actionMapRequired;
            ActionMapAvailable = actionMapAvailable;
            HasActionAsset = hasActionAsset;
            AvailableActionMapCount = availableActionMapCount < 0 ? 0 : availableActionMapCount;
            Issues = issues ?? Array.Empty<InputModeUnityActionMapPreviewIssue>();
            Source = source.NormalizeTextOrFallback(nameof(InputModeUnityActionMapPreviewResult));
            Reason = reason.NormalizeText();
        }

        public InputModeUnityActionMapPreviewStatus Status { get; }

        public InputModeKind RequestedMode { get; }

        public UnityInputActionMapName ActionMapName { get; }

        public bool ActionMapRequired { get; }

        public bool ActionMapAvailable { get; }

        public bool HasActionAsset { get; }

        public int AvailableActionMapCount { get; }

        public InputModeUnityActionMapPreviewIssue[] Issues { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool Succeeded => Status == InputModeUnityActionMapPreviewStatus.Succeeded;

        public bool Failed => !Succeeded;

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
            builder.Append(" actionMapRequired='").Append(ActionMapRequired).Append("'");
            builder.Append(" actionMapName='").Append(ActionMapName).Append("'");
            builder.Append(" actionMapAvailable='").Append(ActionMapAvailable).Append("'");
            builder.Append(" hasActionAsset='").Append(HasActionAsset).Append("'");
            builder.Append(" availableActionMaps='").Append(AvailableActionMapCount).Append("'");
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
