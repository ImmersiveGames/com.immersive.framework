using Immersive.Framework.ApiStatus;
using Immersive.Framework.PlayerSlots;

namespace Immersive.Framework.PlayerViews
{
    /// <summary>
    /// API status: Experimental. Passive PlayerView contract that declares view evidence for one PlayerSlot.
    /// It does not activate cameras, select CameraDirector priority, bind control or own input behavior.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F49G passive PlayerView contract.")]
    public interface IPlayerView
    {
        PlayerSlotId PlayerSlotId { get; }

        PlayerViewState State { get; }

        bool HasCameraEvidence { get; }

        bool HasTargetEvidence { get; }

        bool HasPlayerEntryEvidence { get; }

        bool IsEligibleForActiveView { get; }

        PlayerViewSnapshot CreateSnapshot();
    }
}
