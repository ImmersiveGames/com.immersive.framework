using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Unity Input System adapter for one explicit Session-authorized PlayerInputManager.
    /// It does not own Slot allocation or framework admission.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3G.3 Unity PlayerInputManager provisioning adapter.")]
    internal sealed class UnityLocalPlayerProvisioningBackend : ILocalPlayerProvisioningBackend
    {
        private readonly PlayerInputManager manager;

        internal UnityLocalPlayerProvisioningBackend(PlayerInputManager manager)
        {
            this.manager = manager;
        }

        public bool IsAvailable => manager != null;

        public bool UsesManualJoin =>
            manager != null &&
            manager.joinBehavior == PlayerJoinBehavior.JoinPlayersManually;

        public GameObject PlayerPrefab => manager != null ? manager.playerPrefab : null;

        public int CurrentPlayerCount => manager != null ? manager.playerCount : 0;

        public int TechnicalMaxPlayerCount => manager != null ? manager.maxPlayerCount : 0;

        public event Action<PlayerInput> PlayerJoined
        {
            add
            {
                if (manager != null)
                {
                    manager.onPlayerJoined += value;
                }
            }
            remove
            {
                if (manager != null)
                {
                    manager.onPlayerJoined -= value;
                }
            }
        }

        public PlayerInput JoinPlayer(LocalPlayerJoinRequest request)
        {
            if (manager == null)
            {
                throw new InvalidOperationException(
                    "Local Player provisioning backend has no PlayerInputManager.");
            }

            if (!request.TryValidate(out string issue))
            {
                throw new ArgumentException(issue, nameof(request));
            }

            return manager.JoinPlayer(
                playerIndex: -1,
                splitScreenIndex: -1,
                controlScheme: request.HasControlSchemeHint ? request.ControlScheme : null,
                pairWithDevice: request.PairWithDevice);
        }

        public void RejectPlayer(PlayerInput playerInput, string source, string reason)
        {
            if (playerInput == null)
            {
                return;
            }

            playerInput.DeactivateInput();
            UnityEngine.Object.Destroy(playerInput.gameObject);
        }
    }
}
