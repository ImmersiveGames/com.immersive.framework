using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Explicit Unity target for PlayerView camera-target binding evidence.
    /// Implementations may store the current Transform target, but must not activate cameras, drive Cinemachine,
    /// change priorities, bind input/control, enable movement or spawn actors as part of F51B.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51B PlayerView camera-target binding target contract.")]
    public interface IPlayerViewCameraTargetBindingTarget
    {
        string CameraTargetBindingTargetName { get; }

        bool HasCameraTargetBinding { get; }

        Transform CurrentCameraTarget { get; }

        PlayerViewCameraTargetBindingSnapshot CurrentCameraTargetBinding { get; }

        PlayerViewCameraTargetBindingResult ApplyPlayerViewCameraTargetBinding(
            PlayerViewCameraTargetBindingSnapshot binding,
            Transform cameraTarget,
            string source = null,
            string reason = null);

        PlayerViewCameraTargetBindingResult ClearPlayerViewCameraTargetBinding(
            PlayerSlotId playerSlotId,
            string source = null,
            string reason = null);
    }
}
