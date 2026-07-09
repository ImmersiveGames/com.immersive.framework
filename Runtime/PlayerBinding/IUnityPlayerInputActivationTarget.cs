using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Explicit target for Unity PlayerInput action-map activation.
    /// Implementations may switch one configured action map and restore the previous action map on clear, but must not route
    /// InputActions, enable movement, execute gameplay, spawn actors or own runtime lifecycle as part of F52C.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F52C Unity PlayerInput action-map activation target contract.")]
    public interface IUnityPlayerInputActivationTarget
    {
        string ActivationTargetName { get; }

        bool HasUnityPlayerInput { get; }

        string UnityPlayerInputName { get; }

        string ConfiguredActionMapName { get; }

        string CurrentActionMapName { get; }

        bool HasConfiguredActionMapName { get; }

        bool HasUnityPlayerInputActionAsset { get; }

        bool HasConfiguredActionMap { get; }

        bool TryGetExpectedPlayerSlotId(out PlayerSlotId playerSlotId);

        bool HasUnityPlayerInputActivation { get; }

        UnityPlayerInputActivationSnapshot CurrentUnityPlayerInputActivation { get; }

        bool BindsView { get; }

        bool BindsControl { get; }

        bool BridgesUnityPlayerInput { get; }

        bool ActivatesInput { get; }

        bool EnablesMovement { get; }

        bool SpawnsActor { get; }

        UnityPlayerInputActivationResult ApplyUnityPlayerInputActivation(
            UnityPlayerInputActivationSnapshot activation,
            string source = null,
            string reason = null);

        UnityPlayerInputActivationResult ClearUnityPlayerInputActivation(
            PlayerSlotId playerSlotId,
            string source = null,
            string reason = null);
    }
}
