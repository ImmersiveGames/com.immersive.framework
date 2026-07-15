using System;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Preview result that maps a logical InputMode request to Unity Input evidence.
    /// It does not switch action maps, activate PlayerInput, join players, move actors or own Unity input.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32A InputMode Unity application preview result.")]
    public sealed class InputModeUnityApplicationPreviewResult
    {
        public InputModeUnityApplicationPreviewResult(
            InputModeUnityApplicationPreviewStatus status,
            InputModeKind requestedMode,
            UnityInputTargetRole targetRole,
            bool targetRequired,
            bool targetAvailable,
            bool playerActorRequired,
            bool playerActorAvailable,
            bool localPlayerProvisioningRequired,
            bool localPlayerProvisioningAvailable,
            InputModeUnityApplicationPreviewIssue[] issues,
            string source,
            string reason)
        {
            if (!Enum.IsDefined(typeof(InputModeUnityApplicationPreviewStatus), status) || status == InputModeUnityApplicationPreviewStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "InputMode Unity application preview status must be explicit.");
            }

            Status = status;
            RequestedMode = requestedMode;
            TargetRole = targetRole;
            TargetRequired = targetRequired;
            TargetAvailable = targetAvailable;
            PlayerActorRequired = playerActorRequired;
            PlayerActorAvailable = playerActorAvailable;
            LocalPlayerProvisioningRequired = localPlayerProvisioningRequired;
            LocalPlayerProvisioningAvailable = localPlayerProvisioningAvailable;
            Issues = issues ?? Array.Empty<InputModeUnityApplicationPreviewIssue>();
            Source = source.NormalizeTextOrFallback(nameof(InputModeUnityApplicationPreviewResult));
            Reason = reason.NormalizeText();
        }

        public InputModeUnityApplicationPreviewStatus Status { get; }

        public InputModeKind RequestedMode { get; }

        public UnityInputTargetRole TargetRole { get; }

        public bool TargetRequired { get; }

        public bool TargetAvailable { get; }

        public bool PlayerActorRequired { get; }

        public bool PlayerActorAvailable { get; }

        public bool LocalPlayerProvisioningRequired { get; }

        public bool LocalPlayerProvisioningAvailable { get; }

        public InputModeUnityApplicationPreviewIssue[] Issues { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool Succeeded => Status == InputModeUnityApplicationPreviewStatus.Succeeded;

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
            builder.Append(" targetRole='").Append(TargetRole).Append("'");
            builder.Append(" targetRequired='").Append(TargetRequired).Append("'");
            builder.Append(" targetAvailable='").Append(TargetAvailable).Append("'");
            builder.Append(" playerActorRequired='").Append(PlayerActorRequired).Append("'");
            builder.Append(" playerActorAvailable='").Append(PlayerActorAvailable).Append("'");
            builder.Append(" localPlayerProvisioningRequired='").Append(LocalPlayerProvisioningRequired).Append("'");
            builder.Append(" localPlayerProvisioningAvailable='").Append(LocalPlayerProvisioningAvailable).Append("'");
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
