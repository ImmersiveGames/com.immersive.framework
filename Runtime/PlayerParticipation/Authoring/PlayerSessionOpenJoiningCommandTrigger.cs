using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Commands/Open Joining")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-07 explicit Player Session Open Joining command.")]
    public sealed class PlayerSessionOpenJoiningCommandTrigger : PlayerSessionCommandTriggerBase
    {
        private const string Source = nameof(PlayerSessionOpenJoiningCommandTrigger);
        public PlayerParticipationOperationResult LastOpenJoiningResult { get; private set; }

        [ContextMenu("Invoke Open Joining")]
        public override void Invoke()
        {
            LastOpenJoiningResult = null;
            string reason = BeginInvocation("OpenJoining");
            if (!TryGetAccess(out IPlayerSessionScopedAccess access, out string issue))
            {
                CompleteResult(PlayerParticipationOperationResult.RuntimeUnavailable(
                    "OpenJoining", Source, reason, issue));
                return;
            }

            CompleteResult(access.OpenJoining(Source, reason));
        }

        protected override bool TryValidateCommandConfiguration(out string issue)
        {
            issue = string.Empty;
            return true;
        }

        private void CompleteResult(PlayerParticipationOperationResult result)
        {
            LastOpenJoiningResult = result;
            Complete("OpenJoining", Outcome(result), Describe(result));
        }
    }
}
