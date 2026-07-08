using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Explicit target for PlayerView binding evidence.
    /// Implementations may store or expose the active PlayerView binding, but must not activate cameras,
    /// drive Cinemachine, bind input/control, enable movement or spawn actors as part of F51A.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F51A PlayerView binding target contract.")]
    public interface IPlayerViewBindingTarget
    {
        string BindingTargetName { get; }

        bool HasPlayerViewBinding { get; }

        PlayerViewBindingSnapshot CurrentPlayerViewBinding { get; }

        PlayerViewBindingResult ApplyPlayerViewBinding(PlayerViewBindingSnapshot binding, string source = null, string reason = null);

        PlayerViewBindingResult ClearPlayerViewBinding(PlayerSlotId playerSlotId, string source = null, string reason = null);
    }
}
