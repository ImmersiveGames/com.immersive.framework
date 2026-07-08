using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Explicit Unity target for activating one camera from PlayerView camera-target binding evidence.
    /// Implementations must not drive Cinemachine, CameraDirector, priority selection, input/control binding, movement or actor spawning as part of F51C.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51C PlayerView camera activation target contract.")]
    public interface IPlayerViewCameraActivationTarget
    {
        string CameraActivationTargetName { get; }

        UnityEngine.Camera ActivationCamera { get; }

        bool HasCameraActivation { get; }

        PlayerViewCameraActivationSnapshot CurrentCameraActivation { get; }

        PlayerViewCameraActivationResult ApplyPlayerViewCameraActivation(
            PlayerViewCameraActivationSnapshot activation,
            UnityEngine.Camera camera,
            string source = null,
            string reason = null);

        PlayerViewCameraActivationResult ClearPlayerViewCameraActivation(
            PlayerSlotId playerSlotId,
            string source = null,
            string reason = null);
    }
}
