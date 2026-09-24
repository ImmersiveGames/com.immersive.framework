using Immersive.Framework.ApiStatus;
using UnityEngine;
using UnityEngine.InputSystem;

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
            InvokeCore(pairWithDevice: null, explicitDeviceRequired: false);
        }

        /// <summary>
        /// Encaminha o dispositivo de origem ao mesmo Join, rejeitando referência ausente ou removida.
        /// </summary>
        public void InvokeFromDevice(InputDevice device)
        {
            InvokeCore(pairWithDevice: device, explicitDeviceRequired: true);
        }

        private void InvokeCore(InputDevice pairWithDevice, bool explicitDeviceRequired)
        {
            LastJoinResult = null;
            string reason = BeginInvocation("Join");
            var request = new LocalPlayerJoinRequest(Source, reason, pairWithDevice, ControlScheme);
            if (explicitDeviceRequired && (pairWithDevice == null || !pairWithDevice.added))
            {
                CompleteResult(new LocalPlayerJoinResult(
                    status: LocalPlayerJoinStatus.RejectedInvalidRequest,
                    operationId: default,
                    request: request,
                    reservationResult: null,
                    commitResult: null,
                    rollbackResult: null,
                    slot: default,
                    playerInput: null,
                    localPlayerHost: null,
                    unityPlayerIndex: -1,
                    callbackConfirmation: LocalPlayerJoinCallbackConfirmation.None,
                    message: pairWithDevice == null
                        ? "Device-aware Join requires an explicit InputDevice."
                        : "Device-aware Join requires an InputDevice currently added to the Input System."));
                return;
            }

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
