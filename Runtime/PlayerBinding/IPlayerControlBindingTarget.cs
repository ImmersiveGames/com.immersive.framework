using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerBinding
{
    /// <summary>
    /// API status: Experimental. Explicit target for PlayerControl binding evidence.
    /// Implementations may store or expose the active PlayerControl binding, but must not route InputActions,
    /// switch PlayerInput action maps, activate input, enable movement, drive gameplay or spawn actors as part of F52A.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F52A PlayerControl binding target contract.")]
    public interface IPlayerControlBindingTarget
    {
        string BindingTargetName { get; }

        bool HasPlayerControlBinding { get; }

        PlayerControlBindingSnapshot CurrentPlayerControlBinding { get; }

        PlayerControlBindingResult ApplyPlayerControlBinding(PlayerControlBindingSnapshot binding, string source = null, string reason = null);

        PlayerControlBindingResult ClearPlayerControlBinding(PlayerSlotId playerSlotId, string source = null, string reason = null);
    }
}
