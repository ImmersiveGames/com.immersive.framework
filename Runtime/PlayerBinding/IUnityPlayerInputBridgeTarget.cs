using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Explicit target for Unity PlayerInput bridge evidence.
    /// Implementations may store the bridge between PlayerControl binding evidence and a configured PlayerInput, but must not
    /// enable input, switch action maps, route InputActions, enable movement, execute gameplay or spawn actors as part of F52B.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F52B Unity PlayerInput bridge target contract.")]
    public interface IUnityPlayerInputBridgeTarget
    {
        string BridgeTargetName { get; }

        bool HasUnityPlayerInput { get; }

        string UnityPlayerInputName { get; }

        bool TryGetExpectedPlayerSlotId(out PlayerSlotId playerSlotId);

        bool HasUnityPlayerInputBridge { get; }

        UnityPlayerInputBridgeSnapshot CurrentUnityPlayerInputBridge { get; }

        bool BindsView { get; }

        bool BindsControl { get; }

        bool BridgesUnityPlayerInput { get; }

        bool ActivatesInput { get; }

        bool EnablesMovement { get; }

        bool SpawnsActor { get; }

        UnityPlayerInputBridgeResult ApplyUnityPlayerInputBridge(
            UnityPlayerInputBridgeSnapshot bridge,
            string source = null,
            string reason = null);

        UnityPlayerInputBridgeResult ClearUnityPlayerInputBridge(
            PlayerSlotId playerSlotId,
            string source = null,
            string reason = null);
    }
}
