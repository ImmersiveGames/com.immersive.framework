using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.PlayerParticipation
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Commands/Join")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental,
        "IF-PLAYER-SURFACE-07 explicit Player Session Join command.")]
    public sealed class PlayerSessionJoinCommandTrigger : PlayerSessionCommandTriggerBase
    {
        private const string Source = nameof(PlayerSessionJoinCommandTrigger);

        [SerializeField]
        [Tooltip("Optional Unity Input System control scheme hint for this manual Join request.")]
        private string controlScheme;

        public string ControlScheme => controlScheme ?? string.Empty;
        public LocalPlayerJoinResult LastJoinResult { get; private set; }

        [ContextMenu("Invoke Join")]
        public override void Invoke()
        {
            LastJoinResult = null;
            string reason = BeginInvocation("Join");
            var request = new LocalPlayerJoinRequest(Source, reason, null, ControlScheme);
            if (!TryGetJoinAccess(out ILocalPlayerJoinAccess access, out string issue))
            {
                CompleteResult(LocalPlayerJoinResult.RuntimeUnavailable(request, issue));
                return;
            }

            CompleteResult(access.RequestJoin(request));
        }

        protected override bool TryValidateCommandConfiguration(out string issue)
        {
            issue = string.Empty;
            return true;
        }

        private void CompleteResult(LocalPlayerJoinResult result)
        {
            LastJoinResult = result;
            Complete("Join", Outcome(result), Describe(result));
        }
    }
}
