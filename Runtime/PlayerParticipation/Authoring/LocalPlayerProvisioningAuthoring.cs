
using Immersive.Framework.ApiStatus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Designer-facing declaration of the one Session-authorized local PlayerInputManager.
    /// This component never provisions a Player from Awake, OnEnable, Start or validation.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Player/Local Player Provisioning Authoring")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "P3G.2 local Player provisioning authoring surface.")]
    public sealed class LocalPlayerProvisioningAuthoring : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Explicit Session-authorized PlayerInputManager. Runtime code must not use PlayerInputManager.instance as a distributed lookup.")]
        private PlayerInputManager playerInputManager;

        public PlayerInputManager PlayerInputManager => playerInputManager;

        public bool HasPlayerInputManager => playerInputManager != null;

        public bool UsesManualJoin =>
            playerInputManager != null &&
            playerInputManager.joinBehavior == PlayerJoinBehavior.JoinPlayersManually;

        public GameObject PlayerPrefab =>
            playerInputManager != null ? playerInputManager.playerPrefab : null;

        public int TechnicalMaxPlayerCount =>
            playerInputManager != null ? playerInputManager.maxPlayerCount : 0;
    }
}
