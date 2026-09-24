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

        event Action<PlayerInput> PlayerJoined;

        PlayerInput JoinPlayer(LocalPlayerJoinRequest request);

        void RejectPlayer(PlayerInput playerInput, string source, string reason);
    }

    /// <summary>
    /// Narrow technical capability used to release a local PlayerInput that has already
    /// completed Manager-Provisioned admission and is owned by the current Session.
    /// </summary>
    /// <remarks>
    /// This contract is deliberately separate from <see cref="ILocalPlayerProvisioningBackend.RejectPlayer"/>.
    /// Rejection is admission compensation; releasing an admitted Player ends Framework ownership
    /// of an already accepted physical provisioning resource. This is not the public Player Leave API.
    /// The interface is public only so synthetic QA backends can validate orchestration without
    /// driving PlayerInputManager through the Input System.
    /// </remarks>
    public interface IAdmittedLocalPlayerReleaseBackend
    {
        void ReleaseAdmittedPlayer(
            PlayerInput playerInput,
            string source,
            string reason);
    }
}
