using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Commands/Close Joining")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-07 explicit Player Session Close Joining command.")]
    public sealed class PlayerSessionCloseJoiningCommandTrigger : PlayerSessionCommandTriggerBase
    {
        private const string Source = nameof(PlayerSessionCloseJoiningCommandTrigger);
        public PlayerParticipationOperationResult LastCloseJoiningResult { get; private set; }

        [ContextMenu("Invoke Close Joining")]
        public override void Invoke()
        {
            LastCloseJoiningResult = null;
            string reason = BeginInvocation("CloseJoining");
            if (!TryGetAccess(out IPlayerSessionScopedAccess access, out string issue))
            {
                CompleteResult(PlayerParticipationOperationResult.RuntimeUnavailable(
                    "CloseJoining", Source, reason, issue));
                return;
            }

            CompleteResult(access.CloseJoining(Source, reason));
        }

        protected override bool TryValidateCommandConfiguration(out string issue)
        {
            issue = string.Empty;
            return true;
        }

        private void CompleteResult(PlayerParticipationOperationResult result)
        {
            LastCloseJoiningResult = result;
            Complete("CloseJoining", Outcome(result), Describe(result));
        }
    }
}
