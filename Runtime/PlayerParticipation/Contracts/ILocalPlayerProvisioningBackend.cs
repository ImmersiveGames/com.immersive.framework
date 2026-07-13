using System;
using Immersive.Framework.ApiStatus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.PlayerParticipation
{
    /// <summary>
    /// Narrow technical adapter used by the Session provisioning bridge.
    /// The Unity implementation delegates physical local Player creation to PlayerInputManager.
    /// Public visibility exists only so the external QA harness can provide a synthetic backend;
    /// this is not a game-facing provisioning API.
    /// </summary>
    [FrameworkApiStatus(
        FrameworkApiStatus.Internal,
        "P3G.3 testable local Player provisioning backend boundary.")]
    public interface ILocalPlayerProvisioningBackend
    {
        bool IsAvailable { get; }

        bool UsesManualJoin { get; }

        GameObject PlayerPrefab { get; }

        int CurrentPlayerCount { get; }

        int TechnicalMaxPlayerCount { get; }

        event Action<PlayerInput> PlayerJoined;

        PlayerInput JoinPlayer(LocalPlayerJoinRequest request);

        void RejectPlayer(PlayerInput playerInput, string source, string reason);
    }
}
